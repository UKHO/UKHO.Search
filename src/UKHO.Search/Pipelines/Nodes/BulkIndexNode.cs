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
        private static readonly HashSet<int> DefaultTransientStatusCodes = new() { 429, 503 };
        private readonly IBulkIndexClient<TDocument> client;
        private readonly ChannelWriter<Envelope<TDocument>>? errorOutput;
        private readonly IPipelineFatalErrorReporter? fatalErrorReporter;

        private readonly ChannelReader<BatchEnvelope<TDocument>> input;
        private readonly ILogger? logger;
        private readonly NodeMetrics metrics;
        private readonly ChannelWriter<Envelope<TDocument>>? retryOutput;
        private readonly ChannelWriter<Envelope<TDocument>> successOutput;
        private readonly ISet<int> transientStatusCodes;
        private Task? completion;

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
            this.input = input;
            this.client = client;
            this.successOutput = successOutput;
            this.retryOutput = retryOutput;
            this.errorOutput = errorOutput;
            this.transientStatusCodes = transientStatusCodes ?? DefaultTransientStatusCodes;
            this.logger = logger;
            this.fatalErrorReporter = fatalErrorReporter;
            metrics = new NodeMetrics(name);
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
                while (await input.WaitToReadAsync(cancellationToken)
                                  .ConfigureAwait(false))
                {
                    while (input.TryRead(out var batch))
                    {
                        metrics.RecordIn(batch);
                        metrics.IncrementInFlight();
                        var started = Stopwatch.GetTimestamp();
                        try
                        {
                            await HandleBatchAsync(batch, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        finally
                        {
                            var elapsed = Stopwatch.GetElapsedTime(started);
                            metrics.RecordDuration(elapsed);
                            metrics.DecrementInFlight();
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
                metrics.Dispose();
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

            var response = await client.BulkIndexAsync(request, cancellationToken)
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

                var isTransient = transientStatusCodes.Contains(result.StatusCode);
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
            await successOutput.WriteAsync(envelope, cancellationToken)
                               .ConfigureAwait(false);
            metrics.RecordOut(envelope);
        }

        private async ValueTask WriteRetryAsync(Envelope<TDocument> envelope, CancellationToken cancellationToken)
        {
            if (retryOutput is not null)
            {
                await retryOutput.WriteAsync(envelope, cancellationToken)
                                 .ConfigureAwait(false);
                metrics.RecordOut(envelope);
                return;
            }

            await WriteErrorAsync(envelope, cancellationToken)
                .ConfigureAwait(false);
        }

        private async ValueTask WriteErrorAsync(Envelope<TDocument> envelope, CancellationToken cancellationToken)
        {
            if (errorOutput is not null)
            {
                await errorOutput.WriteAsync(envelope, cancellationToken)
                                 .ConfigureAwait(false);
                metrics.RecordOut(envelope);
                return;
            }

            await successOutput.WriteAsync(envelope, cancellationToken)
                               .ConfigureAwait(false);
            metrics.RecordOut(envelope);
        }

        private void CompleteOutputs(Exception? error = null)
        {
            successOutput.TryComplete(error);
            retryOutput?.TryComplete(error);
            errorOutput?.TryComplete(error);
        }
    }
}