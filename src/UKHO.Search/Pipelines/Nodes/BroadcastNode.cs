using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Metrics;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
    public sealed class BroadcastNode<TIn> : INode
    {
        private readonly IPipelineFatalErrorReporter? _fatalErrorReporter;
        private readonly ChannelReader<Envelope<TIn>> _input;
        private readonly ILogger? _logger;
        private readonly BroadcastMode _mode;
        private readonly IReadOnlyList<ChannelWriter<Envelope<TIn>>> _optionalOutputs;
        private readonly IReadOnlyList<ChannelWriter<Envelope<TIn>>> _requiredOutputs;
        private Task? _completion;

        public BroadcastNode(string name, ChannelReader<Envelope<TIn>> input, IReadOnlyList<ChannelWriter<Envelope<TIn>>> requiredOutputs, IReadOnlyList<ChannelWriter<Envelope<TIn>>>? optionalOutputs = null, BroadcastMode mode = BroadcastMode.AllMustReceive, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null)
        {
            Name = name;
            _input = input;
            _requiredOutputs = requiredOutputs;
            _optionalOutputs = optionalOutputs ?? Array.Empty<ChannelWriter<Envelope<TIn>>>();
            _mode = mode;
            _logger = logger;
            _fatalErrorReporter = fatalErrorReporter;

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

            var allStrictOutputs = _mode == BroadcastMode.AllMustReceive
                ? _requiredOutputs.Concat(_optionalOutputs)
                                  .ToArray()
                : null;

            if (allStrictOutputs is not null)
            {
                await WriteAllAsync(item, allStrictOutputs, cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            await WriteAllAsync(item, _requiredOutputs, cancellationToken)
                .ConfigureAwait(false);

            foreach (var optionalOutput in _optionalOutputs)
            {
                try
                {
                    var clone = item.Clone();
                    if (optionalOutput.TryWrite(clone))
                    {
                        Metrics.RecordOut(clone);
                    }
                }
                catch
                {
                    // Best-effort means optional sinks must not block or fault the pipeline.
                }
            }
        }

        private async ValueTask WriteAllAsync(Envelope<TIn> item, IReadOnlyList<ChannelWriter<Envelope<TIn>>> outputs, CancellationToken cancellationToken)
        {
            foreach (var output in outputs)
            {
                var canWrite = await output.WaitToWriteAsync(cancellationToken)
                                           .ConfigureAwait(false);
                if (!canWrite)
                {
                    throw new ChannelClosedException();
                }
            }

            foreach (var output in outputs)
            {
                var clone = item.Clone();
                await output.WriteAsync(clone, cancellationToken)
                            .ConfigureAwait(false);
                Metrics.RecordOut(clone);
            }
        }

        private void CompleteOutputs(Exception? error = null)
        {
            foreach (var output in _requiredOutputs)
            {
                output.TryComplete(error);
            }

            foreach (var output in _optionalOutputs)
            {
                output.TryComplete(error);
            }
        }
    }
}