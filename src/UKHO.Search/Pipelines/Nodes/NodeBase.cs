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
        private readonly CancellationMode cancellationMode;
        private readonly IPipelineFatalErrorReporter? fatalErrorReporter;
        private readonly ChannelReader<TIn> input;
        private readonly ILogger? logger;
        private readonly ChannelWriter<TOut> output;
        private Task? completion;

        protected NodeBase(string name, ChannelReader<TIn> input, ChannelWriter<TOut> output, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null, CancellationMode cancellationMode = CancellationMode.Immediate)
        {
            Name = name;
            this.input = input;
            this.output = output;
            this.logger = logger;
            this.fatalErrorReporter = fatalErrorReporter;
            this.cancellationMode = cancellationMode;

            Func<long>? queueDepthProvider = null;
            if (input is IQueueDepthProvider qdp)
            {
                queueDepthProvider = () => qdp.QueueDepth;
            }

            Metrics = new NodeMetrics(name, queueDepthProvider);
        }

        protected NodeMetrics Metrics { get; }

        public string Name { get; }

        public Task Completion => completion ?? Task.CompletedTask;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            completion ??= Task.Run(() => RunAsync(cancellationToken), CancellationToken.None);
            return Task.CompletedTask;
        }

        public virtual ValueTask StopAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        protected abstract ValueTask HandleItemAsync(TIn item, CancellationToken cancellationToken);

        protected async ValueTask WriteAsync(TOut item, CancellationToken cancellationToken)
        {
            await output.WriteAsync(item, cancellationToken)
                        .ConfigureAwait(false);
            Metrics.RecordOut(item);
        }

        protected virtual void CompleteOutputs(Exception? error = null)
        {
            output.TryComplete(error);
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
                        await ProcessItemAsync(item, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }

                await input.Completion.ConfigureAwait(false);

                CompleteOutputs();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (cancellationMode == CancellationMode.Drain)
                {
                    await DrainAvailableAsync()
                        .ConfigureAwait(false);
                }
                else
                {
                    // If cancellation races with a faulted upstream completion, prefer propagating the
                    // upstream exception so downstream reliably faults rather than completing cleanly.
                    if (input.Completion.IsCompleted)
                    {
                        await input.Completion.ConfigureAwait(false);
                    }
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
            while (input.TryRead(out var item))
            {
                await ProcessItemAsync(item, CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }
    }
}