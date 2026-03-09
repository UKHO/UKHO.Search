using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Metrics;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
    public abstract class SinkNodeBase<TIn> : INode
    {
        private readonly CancellationMode _cancellationMode;
        private readonly IPipelineFatalErrorReporter? _fatalErrorReporter;
        private readonly ChannelReader<TIn> _input;
        private readonly ILogger? _logger;
        private readonly NodeMetrics _metrics;
        private Task? _completion;

        protected SinkNodeBase(string name, ChannelReader<TIn> input, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null, CancellationMode cancellationMode = CancellationMode.Immediate)
        {
            Name = name;
            _input = input;
            _logger = logger;
            _fatalErrorReporter = fatalErrorReporter;
            _cancellationMode = cancellationMode;

            Func<long>? queueDepthProvider = null;
            if (input is IQueueDepthProvider qdp)
            {
                queueDepthProvider = () => qdp.QueueDepth;
            }

            _metrics = new NodeMetrics(name, queueDepthProvider);
        }

        public string Name { get; }

        public Task Completion => _completion ?? Task.CompletedTask;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _completion ??= Task.Run(() => RunAsync(cancellationToken), CancellationToken.None);
            return Task.CompletedTask;
        }

        public virtual ValueTask StopAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        protected abstract ValueTask HandleItemAsync(TIn item, CancellationToken cancellationToken);

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (await _input.WaitToReadAsync(cancellationToken)
                                   .ConfigureAwait(false))
                {
                    while (_input.TryRead(out var item))
                    {
                        await ProcessItemAsync(item, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }

                await _input.Completion.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (_cancellationMode == CancellationMode.Drain)
                {
                    await DrainAvailableAsync()
                        .ConfigureAwait(false);
                }
                else
                {
                    // If cancellation races with a faulted upstream completion, prefer propagating the
                    // upstream exception so downstream reliably faults rather than completing cleanly.
                    if (_input.Completion.IsCompleted)
                    {
                        await _input.Completion.ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _fatalErrorReporter?.ReportFatal(Name, ex);
                _logger?.LogError(ex, "Node '{NodeName}' failed.", Name);
                throw;
            }
            finally
            {
                _metrics.Dispose();
            }
        }

        private async ValueTask ProcessItemAsync(TIn item, CancellationToken cancellationToken)
        {
            _metrics.RecordIn(item);
            _metrics.IncrementInFlight();
            var started = Stopwatch.GetTimestamp();
            try
            {
                await HandleItemAsync(item, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                var elapsed = Stopwatch.GetElapsedTime(started);
                _metrics.RecordDuration(elapsed);
                _metrics.DecrementInFlight();
            }
        }

        private async ValueTask DrainAvailableAsync()
        {
            while (_input.TryRead(out var item))
            {
                await ProcessItemAsync(item, CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }
    }
}