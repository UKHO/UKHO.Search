using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Metrics;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
    public sealed class BulkIndexNode<TDocument> : INode
    {
        private static readonly HashSet<int> _defaultTransientStatusCodes = new() { 429, 503 };
        private readonly IBulkIndexClient<TDocument> _client;
        private readonly ChannelWriter<Envelope<TDocument>>? _errorOutput;
        private readonly IPipelineFatalErrorReporter? _fatalErrorReporter;

        private readonly ChannelReader<BatchEnvelope<TDocument>> _input;
        private readonly ILogger? _logger;
        private readonly NodeMetrics _metrics;
        private readonly ChannelWriter<Envelope<TDocument>>? _retryOutput;
        private readonly ChannelWriter<Envelope<TDocument>> _successOutput;
        private readonly ISet<int> _transientStatusCodes;
        private Task? _completion;

        public BulkIndexNode(string name,
            ChannelReader<BatchEnvelope<TDocument>> input,
            IBulkIndexClient<TDocument> client,
            ChannelWriter<Envelope<TDocument>> successOutput,
            ChannelWriter<Envelope<TDocument>>? retryOutput = null,
            ChannelWriter<Envelope<TDocument>>? errorOutput = null,
            ISet<int>? transientStatusCodes = null,
            ILogger? logger = null,
            IPipelineFatalErrorReporter? fatalErrorReporter = null)
        {
            Name = name;
            _input = input;
            _client = client;
            _successOutput = successOutput;
            _retryOutput = retryOutput;
            _errorOutput = errorOutput;
            _transientStatusCodes = transientStatusCodes ?? _defaultTransientStatusCodes;
            _logger = logger;
            _fatalErrorReporter = fatalErrorReporter;
            _metrics = new NodeMetrics(name);
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

        private async ValueTask HandleBatchAsync(BatchEnvelope<TDocument> batch, CancellationToken cancellationToken)
        {
            var request = new BulkIndexRequest<TDocument>
            {
                BatchId = batch.BatchId,
                PartitionId = batch.PartitionId,
                Items = batch.Items
            };

            var response = await _client.BulkIndexAsync(request, cancellationToken)
                                        .ConfigureAwait(false);

            var resultsByMessageId = new Dictionary<Guid, BulkIndexItemResult>();
            foreach (var result in response.Items)
            {
                resultsByMessageId[result.MessageId] = result;
            }

            foreach (var envelope in batch.Items)
            {
                envelope.Context.AddBreadcrumb(Name);

                if (!resultsByMessageId.TryGetValue(envelope.MessageId, out var result))
                {
                    throw new KeyNotFoundException($"Bulk index response did not include a result for MessageId '{envelope.MessageId}'.");
                }

                if (result.StatusCode >= 200 && result.StatusCode <= 299)
                {
                    await WriteSuccessAsync(envelope, cancellationToken)
                        .ConfigureAwait(false);
                    continue;
                }

                var isTransient = _transientStatusCodes.Contains(result.StatusCode);
                var error = new PipelineError
                {
                    Category = PipelineErrorCategory.BulkIndex,
                    Code = "BULK_INDEX_FAILED",
                    Message = result.ErrorReason ?? "Bulk index failed.",
                    ExceptionType = result.ErrorType,
                    ExceptionMessage = null,
                    StackTrace = null,
                    IsTransient = isTransient,
                    OccurredAtUtc = DateTimeOffset.UtcNow,
                    NodeName = Name,
                    Details = new Dictionary<string, string>
                    {
                        ["status_code"] = result.StatusCode.ToString()
                    }
                };

                if (isTransient)
                {
                    envelope.MarkRetrying(error);
                    await WriteRetryAsync(envelope, cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    envelope.MarkFailed(error);
                    await WriteErrorAsync(envelope, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        private async ValueTask WriteSuccessAsync(Envelope<TDocument> envelope, CancellationToken cancellationToken)
        {
            await _successOutput.WriteAsync(envelope, cancellationToken)
                                .ConfigureAwait(false);
            _metrics.RecordOut(envelope);
        }

        private async ValueTask WriteRetryAsync(Envelope<TDocument> envelope, CancellationToken cancellationToken)
        {
            if (_retryOutput is not null)
            {
                await _retryOutput.WriteAsync(envelope, cancellationToken)
                                  .ConfigureAwait(false);
                _metrics.RecordOut(envelope);
                return;
            }

            await WriteErrorAsync(envelope, cancellationToken)
                .ConfigureAwait(false);
        }

        private async ValueTask WriteErrorAsync(Envelope<TDocument> envelope, CancellationToken cancellationToken)
        {
            if (_errorOutput is not null)
            {
                await _errorOutput.WriteAsync(envelope, cancellationToken)
                                  .ConfigureAwait(false);
                _metrics.RecordOut(envelope);
                return;
            }

            await _successOutput.WriteAsync(envelope, cancellationToken)
                                .ConfigureAwait(false);
            _metrics.RecordOut(envelope);
        }

        private void CompleteOutputs(Exception? error = null)
        {
            _successOutput.TryComplete(error);
            _retryOutput?.TryComplete(error);
            _errorOutput?.TryComplete(error);
        }
    }
}