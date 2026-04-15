using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Ingestion.Rules;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Ingestion.Pipeline.Nodes
{
    /// <summary>
    /// Applies ingestion enrichers to pipeline items, validates the resulting canonical document, and routes failures to dead-letter output.
    /// </summary>
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
        private readonly string? _providerName;
        private readonly Random _random;
        private readonly TimeSpan _retryBaseDelay;
        private readonly IServiceScopeFactory _scopeFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplyEnrichmentNode"/> class.
        /// </summary>
        /// <param name="name">The pipeline node name used for diagnostics and breadcrumbs.</param>
        /// <param name="input">The channel reader that supplies ingestion pipeline items.</param>
        /// <param name="output">The normal output channel for validated index operations.</param>
        /// <param name="deadLetterOutput">The dead-letter output channel for failed index operations.</param>
        /// <param name="scopeFactory">The scope factory used to resolve enrichers and provider context per item.</param>
        /// <param name="logger">The optional logger used for node diagnostics.</param>
        /// <param name="fatalErrorReporter">The optional fatal error reporter used by the base node.</param>
        /// <param name="retryMaxAttempts">The number of retry attempts allowed after the initial enrichment attempt.</param>
        /// <param name="retryBaseDelay">The base retry delay used for exponential backoff.</param>
        /// <param name="retryMaxDelay">The maximum retry delay allowed by the backoff calculation.</param>
        /// <param name="retryJitter">The jitter window applied to computed retry delays.</param>
        /// <param name="delay">The delay function used between retries.</param>
        /// <param name="random">The random source used when applying retry jitter.</param>
        /// <param name="providerName">The provider name copied into the scoped ingestion provider context.</param>
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
            _providerName = providerName;
        }

        /// <summary>
        /// Handles one ingestion pipeline item by running enrichers, validating canonical state, and forwarding or dead-lettering the resulting operation.
        /// </summary>
        /// <param name="item">The pipeline envelope currently being processed.</param>
        /// <param name="cancellationToken">The cancellation token that stops pipeline processing.</param>
        protected override async ValueTask HandleItemAsync(Envelope<IngestionPipelineContext> item, CancellationToken cancellationToken)
        {
            // Record this node on the breadcrumb trail before any branching occurs so dead-letter output explains the path taken.
            item.Context.AddBreadcrumb(Name);

            var context = item.Payload;

            if (item.Status != MessageStatus.Ok)
            {
                // Propagate already-failed items directly to dead-letter output without attempting enrichment.
                var failedEnvelope = item.MapPayload(context.Operation);

                await _deadLetterOutput.WriteAsync(failedEnvelope, cancellationToken)
                                       .ConfigureAwait(false);
                Metrics.RecordOut(failedEnvelope);
                return;
            }

            if (context.Operation is not UpsertOperation upsert)
            {
                // Non-upsert operations do not use the canonical enrichment path, so forward them unchanged.
                await WriteAsync(item.MapPayload(context.Operation), cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            using var scope = _scopeFactory.CreateScope();

            var providerContext = scope.ServiceProvider.GetService<IIngestionProviderContext>();
            if (providerContext is not null)
            {
                // Stamp the provider name onto the scoped context so enrichers can reason about the active provider consistently.
                providerContext.ProviderName = _providerName;
            }

            var enrichers = scope.ServiceProvider.GetServices<IIngestionEnricher>()
                                 .OrderBy(x => x.Ordinal)
                                 .ThenBy(x => x.GetType()
                                               .FullName, StringComparer.Ordinal)
                                 .ToArray();

            if (enrichers.Length == 0)
            {
                if (!await TryWriteValidatedUpsertAsync(item, context.Operation, upsert, cancellationToken)
                    .ConfigureAwait(false))
                {
                    return;
                }

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

                        // Let each enricher mutate the shared canonical document in deterministic order.
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

            _ = await TryWriteValidatedUpsertAsync(item, context.Operation, upsert, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Determines whether an enrichment exception should be treated as transient for retry purposes.
        /// </summary>
        /// <param name="ex">The exception raised by enrichment.</param>
        /// <param name="cancellationToken">The pipeline cancellation token.</param>
        /// <returns><see langword="true"/> when the exception should be retried; otherwise, <see langword="false"/>.</returns>
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

        /// <summary>
        /// Computes the retry delay for the supplied attempt number using exponential backoff with optional jitter.
        /// </summary>
        /// <param name="attempt">The one-based attempt number that is about to run.</param>
        /// <returns>The delay to apply before the attempt starts.</returns>
        private TimeSpan GetRetryDelay(int attempt)
        {
            if (attempt < 2)
            {
                return TimeSpan.Zero;
            }

            // Convert the upcoming attempt number into exponential backoff, then clamp it to the configured maximum.
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

            // Apply symmetric jitter so concurrent retry storms are less likely to synchronize.
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

        /// <summary>
        /// Validates the canonical upsert document and writes either the normal output or the dead-letter output.
        /// </summary>
        /// <param name="item">The pipeline envelope that owns the operation.</param>
        /// <param name="operation">The index operation that will be forwarded when validation succeeds.</param>
        /// <param name="upsert">The upsert operation containing the canonical document to validate.</param>
        /// <param name="cancellationToken">The cancellation token that stops pipeline processing.</param>
        /// <returns><see langword="true"/> when validation succeeds and the operation is forwarded; otherwise, <see langword="false"/>.</returns>
        private async ValueTask<bool> TryWriteValidatedUpsertAsync(Envelope<IngestionPipelineContext> item, IndexOperation operation, UpsertOperation upsert, CancellationToken cancellationToken)
        {
            if (upsert.Document.Title.Count == 0)
            {
                await WriteCanonicalValidationFailureAsync(item, operation, upsert, cancellationToken, "CANONICAL_TITLE_REQUIRED", "Canonical document is missing a required title after enrichment.", "Canonical document rejected because no title was produced.")
                    .ConfigureAwait(false);
                return false;
            }

            if (upsert.Document.SecurityTokens.Count == 0)
            {
                await WriteCanonicalValidationFailureAsync(item, operation, upsert, cancellationToken, "CANONICAL_SECURITY_TOKENS_REQUIRED", "Canonical document is missing required security tokens after enrichment.", "Canonical document rejected because no canonical security tokens were retained.")
                    .ConfigureAwait(false);
                return false;
            }

            await WriteAsync(item.MapPayload(operation), cancellationToken)
                .ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Marks the current item as a canonical validation failure and writes the failed operation to the dead-letter channel.
        /// </summary>
        /// <param name="item">The pipeline envelope that failed validation.</param>
        /// <param name="operation">The index operation associated with the failed envelope.</param>
        /// <param name="upsert">The upsert operation containing the canonical document.</param>
        /// <param name="cancellationToken">The cancellation token that stops pipeline processing.</param>
        /// <param name="errorCode">The canonical validation error code to record.</param>
        /// <param name="errorMessage">The canonical validation error message to record.</param>
        /// <param name="logMessage">The warning message written to the logger.</param>
        private async ValueTask WriteCanonicalValidationFailureAsync(Envelope<IngestionPipelineContext> item, IndexOperation operation, UpsertOperation upsert, CancellationToken cancellationToken, string errorCode, string errorMessage, string logMessage)
        {
            item.MarkFailed(new PipelineError
            {
                Category = PipelineErrorCategory.Validation,
                Code = errorCode,
                Message = errorMessage,
                IsTransient = false,
                OccurredAtUtc = DateTimeOffset.UtcNow,
                NodeName = Name,
                Details = new Dictionary<string, string>
                {
                    ["DocumentId"] = upsert.Document.Id,
                    ["Provider"] = upsert.Document.Provider
                }
            });

            // Reuse the existing dead-letter path so canonical validation failures behave consistently regardless of the missing field.
            _logger?.LogWarning("{LogMessage} NodeName={NodeName} Key={Key} MessageId={MessageId} DocumentId={DocumentId} Provider={Provider}", logMessage, Name, item.Key, item.MessageId, upsert.Document.Id, upsert.Document.Provider);

            var failedEnvelope = item.MapPayload(operation);
            await _deadLetterOutput.WriteAsync(failedEnvelope, cancellationToken)
                                   .ConfigureAwait(false);
            Metrics.RecordOut(failedEnvelope);
        }

        protected override void CompleteOutputs(Exception? error = null)
        {
            base.CompleteOutputs(error);
            _deadLetterOutput.TryComplete(error);
        }
    }
}