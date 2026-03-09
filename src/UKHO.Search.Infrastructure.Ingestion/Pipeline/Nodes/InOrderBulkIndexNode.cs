using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Metrics;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Infrastructure.Ingestion.Pipeline.Nodes
{
    public sealed class InOrderBulkIndexNode : INode
    {
        private static readonly HashSet<int> _defaultTransientStatusCodes = new() { 429, 503 };

        private readonly TimeSpan _baseDelay;
        private readonly IBulkIndexClient<IndexOperation> _client;
        private readonly ChannelWriter<Envelope<IndexOperation>> _deadLetterOutput;
        private readonly Func<TimeSpan, CancellationToken, Task> _delay;
        private readonly IPipelineFatalErrorReporter? _fatalErrorReporter;
        private readonly ChannelReader<BatchEnvelope<IndexOperation>> _input;
        private readonly TimeSpan _jitter;
        private readonly ILogger? _logger;
        private readonly int _maxAttempts;
        private readonly TimeSpan _maxDelay;
        private readonly NodeMetrics _metrics;
        private readonly Random _random;
        private readonly ChannelWriter<Envelope<IndexOperation>> _successOutput;
        private readonly ISet<int> _transientStatusCodes;
        private Task? _completion;

        public InOrderBulkIndexNode(string name,
            ChannelReader<BatchEnvelope<IndexOperation>> input,
            IBulkIndexClient<IndexOperation> client,
            ChannelWriter<Envelope<IndexOperation>> successOutput,
            ChannelWriter<Envelope<IndexOperation>> deadLetterOutput,
            int maxAttempts,
            TimeSpan baseDelay,
            TimeSpan maxDelay,
            TimeSpan jitter,
            Func<TimeSpan, CancellationToken, Task>? delay = null,
            ISet<int>? transientStatusCodes = null,
            Random? random = null,
            ILogger? logger = null,
            IPipelineFatalErrorReporter? fatalErrorReporter = null,
            string? providerName = null)
        {
            if (maxAttempts < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAttempts));
            }

            if (baseDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(baseDelay));
            }

            if (maxDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDelay));
            }

            if (jitter < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(jitter));
            }

            Name = name;
            _input = input;
            _client = client;
            _successOutput = successOutput;
            _deadLetterOutput = deadLetterOutput;
            _maxAttempts = maxAttempts;
            _baseDelay = baseDelay;
            _maxDelay = maxDelay;
            _jitter = jitter;
            _delay = delay ?? ((d, ct) => Task.Delay(d, ct));
            _transientStatusCodes = transientStatusCodes ?? _defaultTransientStatusCodes;
            _random = random ?? Random.Shared;
            _logger = logger;
            _fatalErrorReporter = fatalErrorReporter;
            _metrics = new NodeMetrics(name, providerName);
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
                while (await _input.WaitToReadAsync(cancellationToken)
                                   .ConfigureAwait(false))
                {
                    while (_input.TryRead(out var batch))
                    {
                        _metrics.RecordIn(batch);
                        _metrics.IncrementInFlight();
                        var started = Stopwatch.GetTimestamp();
                        try
                        {
                            await HandleBatchAsync(batch, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        finally
                        {
                            var elapsed = Stopwatch.GetElapsedTime(started);
                            _metrics.RecordDuration(elapsed);
                            _metrics.DecrementInFlight();
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
                _metrics.Dispose();
            }
        }

        private async ValueTask HandleBatchAsync(BatchEnvelope<IndexOperation> batch, CancellationToken cancellationToken)
        {
            var pending = new List<Envelope<IndexOperation>>(batch.Items);
            foreach (var envelope in pending)
            {
                envelope.Context.AddBreadcrumb(Name);
            }

            PipelineError? lastTransientError = null;

            while (pending.Count > 0)
            {
                BulkIndexResponse response;
                try
                {
                    response = await _client.BulkIndexAsync(new BulkIndexRequest<IndexOperation>
                                            {
                                                BatchId = batch.BatchId,
                                                PartitionId = batch.PartitionId,
                                                Items = pending
                                            }, cancellationToken)
                                            .ConfigureAwait(false);
                }
                catch (Exception ex) when (IsTransientException(ex))
                {
                    lastTransientError = CreateBulkError("BULK_CALL_TRANSIENT", "Bulk call failed with a transient error.", true, ex, null);

                    if (!CanRetry(pending))
                    {
                        await FailAllPendingAsync(pending, lastTransientError, cancellationToken)
                            .ConfigureAwait(false);
                        return;
                    }

                    IncrementAttempt(pending, lastTransientError);

                    var delayForAttempt = GetRetryDelay(pending[0].Attempt);
                    await _delay(delayForAttempt, cancellationToken)
                        .ConfigureAwait(false);

                    continue;
                }

                var resultsByMessageId = new Dictionary<Guid, BulkIndexItemResult>(response.Items.Count);
                foreach (var result in response.Items)
                {
                    resultsByMessageId[result.MessageId] = result;
                }

                for (var i = pending.Count - 1; i >= 0; i--)
                {
                    var envelope = pending[i];

                    if (!resultsByMessageId.TryGetValue(envelope.MessageId, out var result))
                    {
                        throw new KeyNotFoundException($"Bulk index response did not include a result for MessageId '{envelope.MessageId}'.");
                    }

                    if (result.StatusCode >= 200 && result.StatusCode <= 299)
                    {
                        envelope.MarkOk();
                        await WriteSuccessAsync(envelope, cancellationToken)
                            .ConfigureAwait(false);
                        pending.RemoveAt(i);
                        continue;
                    }

                    var isTransient = _transientStatusCodes.Contains(result.StatusCode);
                    var error = CreateBulkError("BULK_INDEX_FAILED", result.ErrorReason ?? "Bulk index failed.", isTransient, null, result.StatusCode, result.ErrorType);

                    if (isTransient)
                    {
                        envelope.MarkRetrying(error);
                        lastTransientError = error;
                        continue;
                    }

                    envelope.MarkFailed(error);
                    await WriteDeadLetterAsync(envelope, cancellationToken)
                        .ConfigureAwait(false);
                    pending.RemoveAt(i);
                }

                if (pending.Count == 0)
                {
                    return;
                }

                if (lastTransientError is null)
                {
                    lastTransientError = CreateBulkError("BULK_INDEX_TRANSIENT", "Bulk index failed with transient errors.", true, null, null);
                }

                if (!CanRetry(pending))
                {
                    await FailAllPendingAsync(pending, lastTransientError, cancellationToken)
                        .ConfigureAwait(false);
                    return;
                }

                IncrementAttempt(pending, lastTransientError);

                var delayForNextAttempt = GetRetryDelay(pending[0].Attempt);
                await _delay(delayForNextAttempt, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private bool CanRetry(IReadOnlyList<Envelope<IndexOperation>> pending)
        {
            foreach (var envelope in pending)
            {
                if (envelope.Attempt >= _maxAttempts)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsTransientException(Exception ex)
        {
            return ex is TimeoutException or TaskCanceledException or OperationCanceledException or HttpRequestException or IOException;
        }

        private void IncrementAttempt(IEnumerable<Envelope<IndexOperation>> pending, PipelineError transientError)
        {
            foreach (var envelope in pending)
            {
                envelope.Attempt++;
                envelope.MarkRetrying(transientError);
            }
        }

        private async Task FailAllPendingAsync(List<Envelope<IndexOperation>> pending, PipelineError lastError, CancellationToken cancellationToken)
        {
            foreach (var envelope in pending)
            {
                var error = CreateBulkError("BULK_INDEX_RETRIES_EXHAUSTED", lastError.Message, false, null, lastError.Details.TryGetValue("status_code", out var sc) && int.TryParse(sc, out var parsed) ? parsed : null);

                envelope.MarkFailed(error);
                await WriteDeadLetterAsync(envelope, cancellationToken)
                    .ConfigureAwait(false);
            }

            pending.Clear();
        }

        private PipelineError CreateBulkError(string code, string message, bool isTransient, Exception? exception, int? statusCode, string? errorType = null)
        {
            var details = new Dictionary<string, string>();
            if (statusCode is not null)
            {
                details["status_code"] = statusCode.Value.ToString();
            }

            return new PipelineError
            {
                Category = PipelineErrorCategory.BulkIndex,
                Code = code,
                Message = message,
                ExceptionType = errorType ?? exception?.GetType()
                                                      .FullName,
                ExceptionMessage = exception?.Message,
                StackTrace = exception?.StackTrace,
                IsTransient = isTransient,
                OccurredAtUtc = DateTimeOffset.UtcNow,
                NodeName = Name,
                Details = details
            };
        }

        private TimeSpan GetRetryDelay(int attempt)
        {
            if (attempt < 2)
            {
                return TimeSpan.Zero;
            }

            var exponent = attempt - 2;
            var delayMs = _baseDelay.TotalMilliseconds * Math.Pow(2, exponent);
            var computed = TimeSpan.FromMilliseconds(delayMs);
            if (computed > _maxDelay)
            {
                computed = _maxDelay;
            }

            if (_jitter <= TimeSpan.Zero)
            {
                return computed;
            }

            var rangeMs = _jitter.TotalMilliseconds;
            var offsetMs = (_random.NextDouble() * 2.0 - 1.0) * rangeMs;
            var jittered = computed + TimeSpan.FromMilliseconds(offsetMs);

            if (jittered < TimeSpan.Zero)
            {
                return TimeSpan.Zero;
            }

            if (jittered > _maxDelay)
            {
                return _maxDelay;
            }

            return jittered;
        }

        private async ValueTask WriteSuccessAsync(Envelope<IndexOperation> envelope, CancellationToken cancellationToken)
        {
            await _successOutput.WriteAsync(envelope, cancellationToken)
                                .ConfigureAwait(false);
            _metrics.RecordOut(envelope);
        }

        private async ValueTask WriteDeadLetterAsync(Envelope<IndexOperation> envelope, CancellationToken cancellationToken)
        {
            await _deadLetterOutput.WriteAsync(envelope, cancellationToken)
                                   .ConfigureAwait(false);
            _metrics.RecordOut(envelope);
        }

        private void CompleteOutputs(Exception? error = null)
        {
            _successOutput.TryComplete(error);
            _deadLetterOutput.TryComplete(error);
        }
    }
}