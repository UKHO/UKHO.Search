using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Metrics;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
    public sealed class MicroBatchNode<TPayload> : INode
    {
        private readonly List<Envelope<TPayload>> buffer = new();
        private readonly CancellationMode cancellationMode;
        private readonly Func<TPayload, int>? estimateSizeBytes;
        private readonly IPipelineFatalErrorReporter? fatalErrorReporter;
        private readonly ChannelReader<Envelope<TPayload>> input;
        private readonly ILogger? logger;
        private readonly int? maxBytes;
        private readonly TimeSpan maxDelay;
        private readonly int maxItems;
        private readonly NodeMetrics metrics;
        private readonly ChannelWriter<BatchEnvelope<TPayload>> output;
        private readonly int partitionId;
        private DateTimeOffset? batchCreatedUtc;
        private int bufferedBytes;
        private Task? completion;

        public MicroBatchNode(string name, int partitionId, ChannelReader<Envelope<TPayload>> input, ChannelWriter<BatchEnvelope<TPayload>> output, int maxItems, TimeSpan maxDelay, int? maxBytes = null, Func<TPayload, int>? estimateSizeBytes = null, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null, CancellationMode cancellationMode = CancellationMode.Immediate)
        {
            if (maxItems <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxItems));
            }

            if (maxBytes is <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxBytes));
            }

            if (maxBytes is not null && estimateSizeBytes is null)
            {
                throw new ArgumentException("A size estimator must be provided when maxBytes is configured.", nameof(estimateSizeBytes));
            }

            if (maxDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDelay));
            }

            Name = name;
            this.partitionId = partitionId;
            this.input = input;
            this.output = output;
            this.maxItems = maxItems;
            this.maxDelay = maxDelay;
            this.maxBytes = maxBytes;
            this.estimateSizeBytes = estimateSizeBytes;
            this.cancellationMode = cancellationMode;
            this.logger = logger;
            this.fatalErrorReporter = fatalErrorReporter;
            metrics = new NodeMetrics(name, () => buffer.Count);
        }

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
                while (true)
                {
                    if (buffer.Count == 0)
                    {
                        if (!await input.WaitToReadAsync(cancellationToken)
                                        .ConfigureAwait(false))
                        {
                            await input.Completion.ConfigureAwait(false);
                            break;
                        }

                        DrainAvailable();
                        if (ShouldFlush())
                        {
                            await FlushAsync(GetFlushToken(cancellationToken))
                                .ConfigureAwait(false);
                        }

                        continue;
                    }

                    var deadlineUtc = batchCreatedUtc!.Value + maxDelay;
                    var nowUtc = DateTimeOffset.UtcNow;
                    var remaining = deadlineUtc - nowUtc;

                    if (remaining <= TimeSpan.Zero)
                    {
                        await FlushAsync(GetFlushToken(cancellationToken))
                            .ConfigureAwait(false);
                        continue;
                    }

                    var waitForRead = input.WaitToReadAsync(cancellationToken)
                                           .AsTask();
                    var waitForDelay = Task.Delay(remaining, cancellationToken);
                    var completed = await Task.WhenAny(waitForRead, waitForDelay)
                                              .ConfigureAwait(false);

                    if (completed == waitForDelay)
                    {
                        await FlushAsync(GetFlushToken(cancellationToken))
                            .ConfigureAwait(false);
                        continue;
                    }

                    if (!await waitForRead.ConfigureAwait(false))
                    {
                        await input.Completion.ConfigureAwait(false);
                        break;
                    }

                    DrainAvailable();
                    if (ShouldFlush())
                    {
                        await FlushAsync(GetFlushToken(cancellationToken))
                            .ConfigureAwait(false);
                    }
                }

                if (buffer.Count > 0)
                {
                    await FlushAsync(GetFlushToken(cancellationToken))
                        .ConfigureAwait(false);
                }

                output.TryComplete();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (cancellationMode == CancellationMode.Drain)
                {
                    if (buffer.Count > 0)
                    {
                        await FlushAsync(CancellationToken.None)
                            .ConfigureAwait(false);
                    }
                }

                output.TryComplete();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Node '{NodeName}' failed.", Name);
                output.TryComplete(ex);
                fatalErrorReporter?.ReportFatal(Name, ex);
                throw;
            }
            finally
            {
                metrics.Dispose();
            }
        }

        private CancellationToken GetFlushToken(CancellationToken cancellationToken)
        {
            return cancellationMode == CancellationMode.Drain ? CancellationToken.None : cancellationToken;
        }

        private bool ShouldFlush()
        {
            return buffer.Count >= maxItems || (maxBytes is not null && bufferedBytes >= maxBytes.Value);
        }

        private void DrainAvailable()
        {
            while (input.TryRead(out var item))
            {
                metrics.RecordIn(item);
                item.Context.AddBreadcrumb(Name);

                if (buffer.Count == 0)
                {
                    batchCreatedUtc = DateTimeOffset.UtcNow;
                }

                buffer.Add(item);

                if (estimateSizeBytes is not null)
                {
                    var estimated = estimateSizeBytes(item.Payload);
                    if (estimated > 0)
                    {
                        bufferedBytes += estimated;
                    }
                }

                if (ShouldFlush())
                {
                    return;
                }
            }
        }

        private async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (buffer.Count == 0)
            {
                return;
            }

            var createdUtc = batchCreatedUtc ?? DateTimeOffset.UtcNow;
            var flushedUtc = DateTimeOffset.UtcNow;
            var items = buffer.ToArray();
            var totalBytes = estimateSizeBytes is not null ? bufferedBytes : (int?)null;

            DateTimeOffset? minTimestampUtc = null;
            DateTimeOffset? maxTimestampUtc = null;
            foreach (var envelope in items)
            {
                var timestampUtc = envelope.TimestampUtc;
                if (minTimestampUtc is null || timestampUtc < minTimestampUtc.Value)
                {
                    minTimestampUtc = timestampUtc;
                }

                if (maxTimestampUtc is null || timestampUtc > maxTimestampUtc.Value)
                {
                    maxTimestampUtc = timestampUtc;
                }
            }

            buffer.Clear();
            batchCreatedUtc = null;
            bufferedBytes = 0;

            var batch = new BatchEnvelope<TPayload>
            {
                BatchId = Guid.NewGuid(),
                PartitionId = partitionId,
                Items = items,
                TotalEstimatedBytes = totalBytes,
                MinItemTimestampUtc = minTimestampUtc,
                MaxItemTimestampUtc = maxTimestampUtc,
                CreatedUtc = createdUtc,
                FlushedUtc = flushedUtc
            };

            metrics.IncrementInFlight();
            var started = Stopwatch.GetTimestamp();
            try
            {
                await output.WriteAsync(batch, cancellationToken)
                            .ConfigureAwait(false);
                metrics.RecordOut(batch);
            }
            finally
            {
                var elapsed = Stopwatch.GetElapsedTime(started);
                metrics.RecordDuration(elapsed);
                metrics.DecrementInFlight();
            }
        }
    }
}