using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Metrics;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
    public abstract class NodeBase<TIn, TOut> : INode
    {
        private readonly CancellationMode _cancellationMode;
        private readonly IPipelineFatalErrorReporter? _fatalErrorReporter;
        private readonly ChannelReader<TIn> _input;
        private readonly ILogger? _logger;
        private readonly ChannelWriter<TOut> _output;
        private Task? _completion;

        protected NodeBase(string name, ChannelReader<TIn> input, ChannelWriter<TOut> output, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null, CancellationMode cancellationMode = CancellationMode.Immediate, string? providerName = null)
        {
            Name = name;
            _input = input;
            _output = output;
            _logger = logger;
            _fatalErrorReporter = fatalErrorReporter;
            _cancellationMode = cancellationMode;

            Func<long>? queueDepthProvider = null;
            if (input is IQueueDepthProvider qdp)
            {
                queueDepthProvider = () => qdp.QueueDepth;
            }

            Metrics = new NodeMetrics(name, providerName, queueDepthProvider);
        }

        protected NodeMetrics Metrics { get; }

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

        protected async ValueTask WriteAsync(TOut item, CancellationToken cancellationToken)
        {
            await _output.WriteAsync(item, cancellationToken)
                         .ConfigureAwait(false);
            Metrics.RecordOut(item);
        }

        protected virtual void CompleteOutputs(Exception? error = null)
        {
            _output.TryComplete(error);
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
                        await ProcessItemAsync(item, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }

                await _input.Completion.ConfigureAwait(false);

                CompleteOutputs();
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
                    await TryPropagateUpstreamCompletionAsync(_input.Completion)
                        .ConfigureAwait(false);
                }

                CompleteOutputs();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Node '{NodeName}' failed.", Name);
                _fatalErrorReporter?.ReportFatal(Name, ex);
                CompleteOutputs(ex);
                throw;
            }
            finally
            {
                Metrics.Dispose();
            }
        }

        private async ValueTask ProcessItemAsync(TIn item, CancellationToken cancellationToken)
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

        private async ValueTask DrainAvailableAsync()
        {
            while (_input.TryRead(out var item))
            {
                await ProcessItemAsync(item, CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }

        private static async Task TryPropagateUpstreamCompletionAsync(Task completion)
        {
            if (completion.IsCompleted)
            {
                await completion.ConfigureAwait(false);
                return;
            }

            var finished = await Task.WhenAny(completion, Task.Delay(TimeSpan.FromSeconds(1)))
                                     .ConfigureAwait(false);

            if (finished == completion)
            {
                await completion.ConfigureAwait(false);
            }
        }
    }
}