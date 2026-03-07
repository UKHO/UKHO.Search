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
        private readonly IPipelineFatalErrorReporter? fatalErrorReporter;
        private readonly ChannelReader<Envelope<TIn>> input;
        private readonly ILogger? logger;
        private readonly BroadcastMode mode;
        private readonly IReadOnlyList<ChannelWriter<Envelope<TIn>>> optionalOutputs;
        private readonly IReadOnlyList<ChannelWriter<Envelope<TIn>>> requiredOutputs;
        private Task? completion;

        public BroadcastNode(string name, ChannelReader<Envelope<TIn>> input, IReadOnlyList<ChannelWriter<Envelope<TIn>>> requiredOutputs, IReadOnlyList<ChannelWriter<Envelope<TIn>>>? optionalOutputs = null, BroadcastMode mode = BroadcastMode.AllMustReceive, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null)
        {
            Name = name;
            this.input = input;
            this.requiredOutputs = requiredOutputs;
            this.optionalOutputs = optionalOutputs ?? Array.Empty<ChannelWriter<Envelope<TIn>>>();
            this.mode = mode;
            this.logger = logger;
            this.fatalErrorReporter = fatalErrorReporter;

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

            var allStrictOutputs = mode == BroadcastMode.AllMustReceive
                ? requiredOutputs.Concat(optionalOutputs)
                                 .ToArray()
                : null;

            if (allStrictOutputs is not null)
            {
                await WriteAllAsync(item, allStrictOutputs, cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            await WriteAllAsync(item, requiredOutputs, cancellationToken)
                .ConfigureAwait(false);

            foreach (var optionalOutput in optionalOutputs)
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
            foreach (var output in requiredOutputs)
            {
                output.TryComplete(error);
            }

            foreach (var output in optionalOutputs)
            {
                output.TryComplete(error);
            }
        }
    }
}