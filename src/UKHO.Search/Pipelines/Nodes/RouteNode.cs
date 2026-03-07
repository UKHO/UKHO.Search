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
        private readonly ChannelWriter<Envelope<TIn>>? errorOutput;
        private readonly IPipelineFatalErrorReporter? fatalErrorReporter;
        private readonly Func<Envelope<TIn>, string> getRoute;
        private readonly ChannelReader<Envelope<TIn>> input;
        private readonly ILogger? logger;
        private readonly IReadOnlyList<ChannelWriter<Envelope<TIn>>> outputs;
        private readonly IReadOnlyDictionary<string, ChannelWriter<Envelope<TIn>>> routes;
        private Task? completion;

        public RouteNode(string name, ChannelReader<Envelope<TIn>> input, IReadOnlyDictionary<string, ChannelWriter<Envelope<TIn>>> routes, Func<Envelope<TIn>, string> getRoute, ChannelWriter<Envelope<TIn>>? errorOutput = null, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null)
        {
            Name = name;
            this.input = input;
            this.routes = routes;
            this.getRoute = getRoute;
            this.errorOutput = errorOutput;
            this.logger = logger;
            this.fatalErrorReporter = fatalErrorReporter;
            outputs = routes.Values.Distinct()
                            .ToArray();
            Metrics = new NodeMetrics(name);
        }

        protected NodeMetrics Metrics { get; }

        public string Name { get; }

        public Task Completion => completion ?? Task.CompletedTask;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            completion ??= Task.Run(() => RunAsync(cancellationToken), CancellationToken.None);
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
                while (await input.WaitToReadAsync(cancellationToken)
                                  .ConfigureAwait(false))
                {
                    while (input.TryRead(out var item))
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

                await input.Completion.ConfigureAwait(false);
                CompleteOutputs();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (input.Completion.IsCompleted)
                {
                    await input.Completion.ConfigureAwait(false);
                }

                CompleteOutputs();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Node '{NodeName}' failed.", Name);
                CompleteOutputs(ex);
                fatalErrorReporter?.ReportFatal(Name, ex);
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

            var routeKey = getRoute(item);
            if (routes.TryGetValue(routeKey, out var output))
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

            if (errorOutput is not null)
            {
                await errorOutput.WriteAsync(item, cancellationToken)
                                 .ConfigureAwait(false);
                Metrics.RecordOut(item);
                return;
            }

            throw new KeyNotFoundException($"No route exists for key '{routeKey}' and no error output was configured.");
        }

        private void CompleteOutputs(Exception? error = null)
        {
            foreach (var output in outputs)
            {
                output.TryComplete(error);
            }

            errorOutput?.TryComplete(error);
        }
    }
}