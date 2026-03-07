using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Metrics;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
    public abstract class MultiInputNodeBase<T1, T2, TOut> : INode
    {
        private readonly CancellationMode cancellationMode;
        private readonly IPipelineFatalErrorReporter? fatalErrorReporter;
        private readonly ChannelReader<T1> input1;
        private readonly ChannelReader<T2> input2;
        private readonly ILogger? logger;
        private readonly ChannelWriter<TOut> output;
        private Task? completion;

        protected MultiInputNodeBase(string name, ChannelReader<T1> input1, ChannelReader<T2> input2, ChannelWriter<TOut> output, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null, CancellationMode cancellationMode = CancellationMode.Immediate)
        {
            Name = name;
            this.input1 = input1;
            this.input2 = input2;
            this.output = output;
            this.logger = logger;
            this.fatalErrorReporter = fatalErrorReporter;
            this.cancellationMode = cancellationMode;

            Func<long>? queueDepthProvider = null;
            if (input1 is IQueueDepthProvider || input2 is IQueueDepthProvider)
            {
                queueDepthProvider = () => ((input1 as IQueueDepthProvider)?.QueueDepth ?? 0) + ((input2 as IQueueDepthProvider)?.QueueDepth ?? 0);
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

        protected abstract ValueTask HandleInput1Async(T1 item, CancellationToken cancellationToken);

        protected abstract ValueTask HandleInput2Async(T2 item, CancellationToken cancellationToken);

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
                await RunFairLoopAsync(cancellationToken)
                    .ConfigureAwait(false);

                // Surface any upstream faults deterministically.
                await Task.WhenAll(input1.Completion, input2.Completion)
                          .ConfigureAwait(false);

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
                    if (input1.Completion.IsCompleted || input2.Completion.IsCompleted)
                    {
                        await Task.WhenAll(input1.Completion, input2.Completion)
                                  .ConfigureAwait(false);
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

        private async ValueTask DrainAvailableAsync()
        {
            var preferInput1 = true;

            while (true)
            {
                if (preferInput1)
                {
                    if (input1.TryRead(out var item1))
                    {
                        await ProcessInput1Async(item1, CancellationToken.None)
                            .ConfigureAwait(false);
                        preferInput1 = false;
                        continue;
                    }

                    if (input2.TryRead(out var item2))
                    {
                        await ProcessInput2Async(item2, CancellationToken.None)
                            .ConfigureAwait(false);
                        preferInput1 = true;
                        continue;
                    }
                }
                else
                {
                    if (input2.TryRead(out var item2))
                    {
                        await ProcessInput2Async(item2, CancellationToken.None)
                            .ConfigureAwait(false);
                        preferInput1 = true;
                        continue;
                    }

                    if (input1.TryRead(out var item1))
                    {
                        await ProcessInput1Async(item1, CancellationToken.None)
                            .ConfigureAwait(false);
                        preferInput1 = false;
                        continue;
                    }
                }

                break;
            }
        }

        private async Task RunFairLoopAsync(CancellationToken cancellationToken)
        {
            var preferInput1 = true;
            var input1Completed = false;
            var input2Completed = false;

            while (!input1Completed || !input2Completed)
            {
                if (preferInput1)
                {
                    if (input1.TryRead(out var item1))
                    {
                        await ProcessInput1Async(item1, cancellationToken)
                            .ConfigureAwait(false);
                        preferInput1 = false;
                        continue;
                    }

                    if (input2.TryRead(out var item2))
                    {
                        await ProcessInput2Async(item2, cancellationToken)
                            .ConfigureAwait(false);
                        preferInput1 = true;
                        continue;
                    }
                }
                else
                {
                    if (input2.TryRead(out var item2))
                    {
                        await ProcessInput2Async(item2, cancellationToken)
                            .ConfigureAwait(false);
                        preferInput1 = true;
                        continue;
                    }

                    if (input1.TryRead(out var item1))
                    {
                        await ProcessInput1Async(item1, cancellationToken)
                            .ConfigureAwait(false);
                        preferInput1 = false;
                        continue;
                    }
                }

                Task<bool>? wait1 = null;
                Task<bool>? wait2 = null;

                if (!input1Completed)
                {
                    wait1 = input1.WaitToReadAsync(cancellationToken)
                                  .AsTask();
                }

                if (!input2Completed)
                {
                    wait2 = input2.WaitToReadAsync(cancellationToken)
                                  .AsTask();
                }

                if (wait1 is null && wait2 is null)
                {
                    break;
                }

                if (wait1 is not null && wait2 is not null)
                {
                    var completed = await Task.WhenAny(wait1, wait2)
                                              .ConfigureAwait(false);
                    if (completed == wait1 && !await wait1.ConfigureAwait(false))
                    {
                        input1Completed = true;
                    }
                    else if (completed == wait2 && !await wait2.ConfigureAwait(false))
                    {
                        input2Completed = true;
                    }
                }
                else if (wait1 is not null)
                {
                    input1Completed = !await wait1.ConfigureAwait(false);
                }
                else
                {
                    input2Completed = !await wait2!.ConfigureAwait(false);
                }
            }
        }

        private async ValueTask ProcessInput1Async(T1 item, CancellationToken cancellationToken)
        {
            Metrics.RecordIn(item);
            Metrics.IncrementInFlight();
            var started = Stopwatch.GetTimestamp();
            try
            {
                await HandleInput1Async(item, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                var elapsed = Stopwatch.GetElapsedTime(started);
                Metrics.RecordDuration(elapsed);
                Metrics.DecrementInFlight();
            }
        }

        private async ValueTask ProcessInput2Async(T2 item, CancellationToken cancellationToken)
        {
            Metrics.RecordIn(item);
            Metrics.IncrementInFlight();
            var started = Stopwatch.GetTimestamp();
            try
            {
                await HandleInput2Async(item, cancellationToken)
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
}