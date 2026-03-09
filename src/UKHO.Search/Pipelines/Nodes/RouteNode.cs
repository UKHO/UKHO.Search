using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Metrics;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
    public sealed class RouteNode<TIn> : INode
    {
        private readonly ChannelWriter<Envelope<TIn>>? _errorOutput;
        private readonly IPipelineFatalErrorReporter? _fatalErrorReporter;
        private readonly Func<Envelope<TIn>, string> _getRoute;
        private readonly ChannelReader<Envelope<TIn>> _input;
        private readonly ILogger? _logger;
        private readonly IReadOnlyList<ChannelWriter<Envelope<TIn>>> _outputs;
        private readonly IReadOnlyDictionary<string, ChannelWriter<Envelope<TIn>>> _routes;
        private Task? _completion;

        public RouteNode(string name, ChannelReader<Envelope<TIn>> input, IReadOnlyDictionary<string, ChannelWriter<Envelope<TIn>>> routes, Func<Envelope<TIn>, string> getRoute, ChannelWriter<Envelope<TIn>>? errorOutput = null, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null)
        {
            Name = name;
            _input = input;
            _routes = routes;
            _getRoute = getRoute;
            _errorOutput = errorOutput;
            _logger = logger;
            _fatalErrorReporter = fatalErrorReporter;
            _outputs = routes.Values.Distinct()
                             .ToArray();
            Metrics = new NodeMetrics(name);
        }

        protected NodeMetrics Metrics { get; }

        public string Name { get; }

        public Task Completion => _completion ?? Task.CompletedTask;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _completion ??= Task.Run(() => RunAsync(cancellationToken), CancellationToken.None);
            return Task.CompletedTask;
        }

        public ValueTask StopAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (await _input.WaitToReadAsync(cancellationToken)
                                   .ConfigureAwait(false))
                {
                    while (_input.TryRead(out var item))
                    {
                        Metrics.RecordIn(item);
                        Metrics.IncrementInFlight();
                        var started = Stopwatch.GetTimestamp();
                        try
                        {
                            await HandleItemAsync(item, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        finally
                        {
                            var elapsed = Stopwatch.GetElapsedTime(started);
                            Metrics.RecordDuration(elapsed);
                            Metrics.DecrementInFlight();
                        }
                    }
                }

                await _input.Completion.ConfigureAwait(false);
                CompleteOutputs();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (_input.Completion.IsCompleted)
                {
                    await _input.Completion.ConfigureAwait(false);
                }

                CompleteOutputs();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Node '{NodeName}' failed.", Name);
                CompleteOutputs(ex);
                _fatalErrorReporter?.ReportFatal(Name, ex);
                throw;
            }
            finally
            {
                Metrics.Dispose();
            }
        }

        private async ValueTask HandleItemAsync(Envelope<TIn> item, CancellationToken cancellationToken)
        {
            item.Context.AddBreadcrumb(Name);

            var routeKey = _getRoute(item);
            if (_routes.TryGetValue(routeKey, out var output))
            {
                await output.WriteAsync(item, cancellationToken)
                            .ConfigureAwait(false);
                Metrics.RecordOut(item);
                return;
            }

            item.MarkFailed(new PipelineError
            {
                Category = PipelineErrorCategory.Validation,
                Code = "ROUTE_NOT_FOUND",
                Message = $"No route exists for key '{routeKey}'.",
                ExceptionType = null,
                ExceptionMessage = null,
                StackTrace = null,
                IsTransient = false,
                OccurredAtUtc = DateTimeOffset.UtcNow,
                NodeName = Name,
                Details = new Dictionary<string, string>
                {
                    ["route_key"] = routeKey
                }
            });

            if (_errorOutput is not null)
            {
                await _errorOutput.WriteAsync(item, cancellationToken)
                                  .ConfigureAwait(false);
                Metrics.RecordOut(item);
                return;
            }

            throw new KeyNotFoundException($"No route exists for key '{routeKey}' and no error output was configured.");
        }

        private void CompleteOutputs(Exception? error = null)
        {
            foreach (var output in _outputs)
            {
                output.TryComplete(error);
            }

            _errorOutput?.TryComplete(error);
        }
    }
}