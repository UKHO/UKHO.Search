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
        private readonly List<Envelope<TPayload>> _buffer = new();
        private readonly CancellationMode _cancellationMode;
        private readonly Func<TPayload, int>? _estimateSizeBytes;
        private readonly IPipelineFatalErrorReporter? _fatalErrorReporter;
        private readonly ChannelReader<Envelope<TPayload>> _input;
        private readonly ILogger? _logger;
        private readonly int? _maxBytes;
        private readonly TimeSpan _maxDelay;
        private readonly int _maxItems;
        private readonly NodeMetrics _metrics;
        private readonly ChannelWriter<BatchEnvelope<TPayload>> _output;
        private readonly int _partitionId;
        private DateTimeOffset? _batchCreatedUtc;
        private int _bufferedBytes;
        private Task? _completion;

        public MicroBatchNode(string name,
            int partitionId,
            ChannelReader<Envelope<TPayload>> input,
            ChannelWriter<BatchEnvelope<TPayload>> output,
            int maxItems,
            TimeSpan maxDelay,
            int? maxBytes = null,
            Func<TPayload, int>? estimateSizeBytes = null,
            ILogger? logger = null,
            IPipelineFatalErrorReporter? fatalErrorReporter = null,
            CancellationMode cancellationMode = CancellationMode.Immediate,
            string? providerName = null)
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
            _partitionId = partitionId;
            _input = input;
            _output = output;
            _maxItems = maxItems;
            _maxDelay = maxDelay;
            _maxBytes = maxBytes;
            _estimateSizeBytes = estimateSizeBytes;
            _cancellationMode = cancellationMode;
            _logger = logger;
            _fatalErrorReporter = fatalErrorReporter;
            _metrics = new NodeMetrics(name, providerName, () => _buffer.Count);
        }

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
                while (true)
                {
                    if (_buffer.Count == 0)
                    {
                        if (!await _input.WaitToReadAsync(cancellationToken)
                                         .ConfigureAwait(false))
                        {
                            await _input.Completion.ConfigureAwait(false);
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

                    var deadlineUtc = _batchCreatedUtc!.Value + _maxDelay;
                    var nowUtc = DateTimeOffset.UtcNow;
                    var remaining = deadlineUtc - nowUtc;

                    if (remaining <= TimeSpan.Zero)
                    {
                        DrainAvailable();
                        await FlushAsync(GetFlushToken(cancellationToken))
                            .ConfigureAwait(false);
                        continue;
                    }

                    var waitForRead = _input.WaitToReadAsync(cancellationToken)
                                            .AsTask();
                    var waitForDelay = Task.Delay(remaining, cancellationToken);
                    var completed = await Task.WhenAny(waitForRead, waitForDelay)
                                              .ConfigureAwait(false);

                    if (completed == waitForDelay)
                    {
                        DrainAvailable();
                        await FlushAsync(GetFlushToken(cancellationToken))
                            .ConfigureAwait(false);
                        continue;
                    }

                    if (!await waitForRead.ConfigureAwait(false))
                    {
                        await _input.Completion.ConfigureAwait(false);
                        break;
                    }

                    DrainAvailable();
                    if (ShouldFlush())
                    {
                        await FlushAsync(GetFlushToken(cancellationToken))
                            .ConfigureAwait(false);
                    }
                }

                if (_buffer.Count > 0)
                {
                    await FlushAsync(GetFlushToken(cancellationToken))
                        .ConfigureAwait(false);
                }

                _output.TryComplete();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (_cancellationMode == CancellationMode.Drain)
                {
                    if (_buffer.Count > 0)
                    {
                        await FlushAsync(CancellationToken.None)
                            .ConfigureAwait(false);
                    }
                }

                _output.TryComplete();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Node '{NodeName}' failed.", Name);
                _output.TryComplete(ex);
                _fatalErrorReporter?.ReportFatal(Name, ex);
                throw;
            }
            finally
            {
                _metrics.Dispose();
            }
        }

        private CancellationToken GetFlushToken(CancellationToken cancellationToken)
        {
            return _cancellationMode == CancellationMode.Drain ? CancellationToken.None : cancellationToken;
        }

        private bool ShouldFlush()
        {
            return _buffer.Count >= _maxItems || (_maxBytes is not null && _bufferedBytes >= _maxBytes.Value);
        }

        private void DrainAvailable()
        {
            while (_input.TryRead(out var item))
            {
                _metrics.RecordIn(item);
                item.Context.AddBreadcrumb(Name);

                if (_buffer.Count == 0)
                {
                    _batchCreatedUtc = DateTimeOffset.UtcNow;
                }

                _buffer.Add(item);

                if (_estimateSizeBytes is not null)
                {
                    var estimated = _estimateSizeBytes(item.Payload);
                    if (estimated > 0)
                    {
                        _bufferedBytes += estimated;
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
            if (_buffer.Count == 0)
            {
                return;
            }

            var createdUtc = _batchCreatedUtc ?? DateTimeOffset.UtcNow;
            var flushedUtc = DateTimeOffset.UtcNow;
            var items = _buffer.ToArray();
            var totalBytes = _estimateSizeBytes is not null ? _bufferedBytes : (int?)null;

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

            _buffer.Clear();
            _batchCreatedUtc = null;
            _bufferedBytes = 0;

            var batch = new BatchEnvelope<TPayload>
            {
                BatchId = Guid.NewGuid(),
                PartitionId = _partitionId,
                Items = items,
                TotalEstimatedBytes = totalBytes,
                MinItemTimestampUtc = minTimestampUtc,
                MaxItemTimestampUtc = maxTimestampUtc,
                CreatedUtc = createdUtc,
                FlushedUtc = flushedUtc
            };

            _metrics.IncrementInFlight();
            var started = Stopwatch.GetTimestamp();
            try
            {
                await _output.WriteAsync(batch, cancellationToken)
                             .ConfigureAwait(false);
                _metrics.RecordOut(batch);
            }
            finally
            {
                var elapsed = Stopwatch.GetElapsedTime(started);
                _metrics.RecordDuration(elapsed);
                _metrics.DecrementInFlight();
            }
        }
    }
}