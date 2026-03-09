using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Ingestion.Pipeline.Nodes
{
    public sealed class ApplyEnrichmentNode : NodeBase<Envelope<IngestionPipelineContext>, Envelope<IndexOperation>>
    {
        private static readonly TimeSpan _defaultBaseDelay = TimeSpan.FromMilliseconds(200);
        private static readonly TimeSpan _defaultMaxDelay = TimeSpan.FromMilliseconds(5000);
        private static readonly TimeSpan _defaultJitter = TimeSpan.FromMilliseconds(250);

        private readonly ChannelWriter<Envelope<IndexOperation>> _deadLetterOutput;
        private readonly Func<TimeSpan, CancellationToken, Task> _delay;
        private readonly TimeSpan _jitter;
        private readonly ILogger? _logger;
        private readonly int _maxAttempts;
        private readonly TimeSpan _maxDelay;
        private readonly Random _random;
        private readonly TimeSpan _retryBaseDelay;
        private readonly IServiceScopeFactory _scopeFactory;

        public ApplyEnrichmentNode(string name,
            ChannelReader<Envelope<IngestionPipelineContext>> input,
            ChannelWriter<Envelope<IndexOperation>> output,
            ChannelWriter<Envelope<IndexOperation>> deadLetterOutput,
            IServiceScopeFactory scopeFactory,
            ILogger? logger = null,
            IPipelineFatalErrorReporter? fatalErrorReporter = null,
            int retryMaxAttempts = 5,
            TimeSpan? retryBaseDelay = null,
            TimeSpan? retryMaxDelay = null,
            TimeSpan? retryJitter = null,
            Func<TimeSpan, CancellationToken, Task>? delay = null,
            Random? random = null,
            string? providerName = null) : base(name, input, output, logger, fatalErrorReporter, providerName: providerName)
        {
            ArgumentNullException.ThrowIfNull(scopeFactory);

            if (retryMaxAttempts < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retryMaxAttempts));
            }

            _deadLetterOutput = deadLetterOutput;
            _logger = logger;
            _scopeFactory = scopeFactory;

            _retryBaseDelay = retryBaseDelay ?? _defaultBaseDelay;
            _maxDelay = retryMaxDelay ?? _defaultMaxDelay;
            _jitter = retryJitter ?? _defaultJitter;
            _delay = delay ?? ((d, ct) => Task.Delay(d, ct));
            _random = random ?? Random.Shared;

            if (_retryBaseDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(retryBaseDelay));
            }

            if (_maxDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(retryMaxDelay));
            }

            if (_jitter < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(retryJitter));
            }

            _maxAttempts = retryMaxAttempts + 1;
        }

        protected override async ValueTask HandleItemAsync(Envelope<IngestionPipelineContext> item, CancellationToken cancellationToken)
        {
            item.Context.AddBreadcrumb(Name);

            var context = item.Payload;

            if (item.Status != MessageStatus.Ok)
            {
                var failedEnvelope = item.MapPayload(context.Operation);

                await _deadLetterOutput.WriteAsync(failedEnvelope, cancellationToken)
                                       .ConfigureAwait(false);
                Metrics.RecordOut(failedEnvelope);
                return;
            }

            if (context.Operation is not UpsertOperation upsert)
            {
                await WriteAsync(item.MapPayload(context.Operation), cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            using var scope = _scopeFactory.CreateScope();

            var enrichers = scope.ServiceProvider.GetServices<IIngestionEnricher>()
                                 .OrderBy(x => x.Ordinal)
                                 .ThenBy(x => x.GetType()
                                               .FullName, StringComparer.Ordinal)
                                 .ToArray();

            if (enrichers.Length == 0)
            {
                await WriteAsync(item.MapPayload(context.Operation), cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            _logger?.LogDebug("Enrichment starting. NodeName={NodeName} Key={Key} MessageId={MessageId} Attempt={Attempt} EnricherCount={EnricherCount}", Name, item.Key, item.MessageId, item.Attempt, enrichers.Length);

            foreach (var enricher in enrichers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var attempt = 1;

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        _logger?.LogDebug("Enricher starting. NodeName={NodeName} Enricher={EnricherType} Key={Key} MessageId={MessageId} Attempt={Attempt}/{MaxAttempts}", Name, enricher.GetType()
                                                                                                                                                                                          .FullName, item.Key, item.MessageId, attempt, _maxAttempts);

                        await enricher.TryBuildEnrichmentAsync(context.Request, upsert.Document, cancellationToken)
                                      .ConfigureAwait(false);

                        _logger?.LogDebug("Enricher finished. NodeName={NodeName} Enricher={EnricherType} Key={Key} MessageId={MessageId} Attempt={Attempt}/{MaxAttempts}", Name, enricher.GetType()
                                                                                                                                                                                          .FullName, item.Key, item.MessageId, attempt, _maxAttempts);
                        break;
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex) when (IsTransientException(ex, cancellationToken) && attempt < _maxAttempts)
                    {
                        var delayForNextAttempt = GetRetryDelay(attempt + 1);

                        _logger?.LogWarning(ex, "Enricher transient failure; retrying. NodeName={NodeName} Enricher={EnricherType} Key={Key} MessageId={MessageId} Attempt={Attempt}/{MaxAttempts} DelayMs={DelayMs}", Name, enricher.GetType()
                                                                                                                                                                                                                                     .FullName, item.Key, item.MessageId, attempt, _maxAttempts, delayForNextAttempt.TotalMilliseconds);

                        attempt++;

                        await _delay(delayForNextAttempt, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex) when (IsTransientException(ex, cancellationToken))
                    {
                        item.MarkFailed(new PipelineError
                        {
                            Category = PipelineErrorCategory.Transform,
                            Code = "ENRICHMENT_RETRIES_EXHAUSTED",
                            Message = "Failed to apply ingestion enrichment after exhausting retries.",
                            ExceptionType = ex.GetType()
                                              .FullName,
                            ExceptionMessage = ex.Message,
                            StackTrace = ex.StackTrace,
                            IsTransient = false,
                            OccurredAtUtc = DateTimeOffset.UtcNow,
                            NodeName = Name,
                            Details = new Dictionary<string, string>()
                        });

                        _logger?.LogWarning(ex, "Enrichment retries exhausted. NodeName={NodeName} Enricher={EnricherType} Key={Key} MessageId={MessageId} Attempts={Attempts}", Name, enricher.GetType()
                                                                                                                                                                                               .FullName, item.Key, item.MessageId, attempt);

                        var failedEnvelope = item.MapPayload(context.Operation);

                        await _deadLetterOutput.WriteAsync(failedEnvelope, cancellationToken)
                                               .ConfigureAwait(false);
                        Metrics.RecordOut(failedEnvelope);
                        return;
                    }
                    catch (Exception ex)
                    {
                        item.MarkFailed(new PipelineError
                        {
                            Category = PipelineErrorCategory.Transform,
                            Code = "ENRICHMENT_ERROR",
                            Message = "Failed to apply ingestion enrichment.",
                            ExceptionType = ex.GetType()
                                              .FullName,
                            ExceptionMessage = ex.Message,
                            StackTrace = ex.StackTrace,
                            IsTransient = false,
                            OccurredAtUtc = DateTimeOffset.UtcNow,
                            NodeName = Name,
                            Details = new Dictionary<string, string>()
                        });

                        _logger?.LogWarning(ex, "Enrichment failed. NodeName={NodeName} Enricher={EnricherType} Key={Key} MessageId={MessageId} Attempt={Attempt}", Name, enricher.GetType()
                                                                                                                                                                                  .FullName, item.Key, item.MessageId, item.Attempt);

                        var failedEnvelope = item.MapPayload(context.Operation);

                        await _deadLetterOutput.WriteAsync(failedEnvelope, cancellationToken)
                                               .ConfigureAwait(false);
                        Metrics.RecordOut(failedEnvelope);
                        return;
                    }
                }
            }

            _logger?.LogInformation("Enrichment finished. NodeName={NodeName} Key={Key} MessageId={MessageId} Attempt={Attempt}", Name, item.Key, item.MessageId, item.Attempt);

            await WriteAsync(item.MapPayload(context.Operation), cancellationToken)
                .ConfigureAwait(false);
        }

        internal static bool IsTransientException(Exception ex, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested && ex is OperationCanceledException)
            {
                return false;
            }

            if (ex is TimeoutException or HttpRequestException or IOException)
            {
                return true;
            }

            if (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested)
            {
                return true;
            }

            return false;
        }

        private TimeSpan GetRetryDelay(int attempt)
        {
            if (attempt < 2)
            {
                return TimeSpan.Zero;
            }

            var exponent = attempt - 2;
            var delayMs = _retryBaseDelay.TotalMilliseconds * Math.Pow(2, exponent);
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

        protected override void CompleteOutputs(Exception? error = null)
        {
            base.CompleteOutputs(error);
            _deadLetterOutput.TryComplete(error);
        }
    }
}