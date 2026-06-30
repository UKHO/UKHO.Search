using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UKHO.Search.Ingestion.Contracts;
using UKHO.Search.Ingestion.Contracts.Serialization;
using UKHO.Search.Studio.Ingestion;
using UKHO.Search.Studio.Providers;

namespace UKHO.Search.Studio.Providers.FileShare
{
    public sealed class FileShareStudioProvider : IStudioIngestionProvider
    {
        private static readonly JsonSerializerOptions _serializerOptions = IngestionJsonSerializerOptions.Create();

        private readonly ILogger<FileShareStudioProvider> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public FileShareStudioProvider(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<FileShareStudioProvider> logger)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string ProviderName => "file-share";

        public async Task<StudioIngestionFetchPayloadResult> FetchPayloadByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return StudioIngestionFetchPayloadResult.Invalid("An id is required.");
            }

            if (!Guid.TryParse(id, out var batchId))
            {
                return StudioIngestionFetchPayloadResult.NotFound($"No payload was found for id '{id}'.");
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var batchPayloadStore = scope.ServiceProvider.GetRequiredService<IFileShareStudioBatchPayloadStore>();

            var payloadSource = await batchPayloadStore.TryGetPayloadSourceAsync(batchId, cancellationToken)
                                                       .ConfigureAwait(false);
            if (payloadSource is null)
            {
                return StudioIngestionFetchPayloadResult.NotFound($"No payload was found for id '{batchId:D}'.");
            }

            var ingestionRequest = FileShareStudioIngestionRequestFactory.Create(payloadSource);

            _logger.LogInformation("Fetched a file-share ingestion payload for batch {BatchId}.", batchId);

            return StudioIngestionFetchPayloadResult.Success(
                new StudioIngestionPayloadEnvelope
                {
                    Id = batchId.ToString("D"),
                    Payload = JsonSerializer.SerializeToElement(ingestionRequest, _serializerOptions)
                });
        }

        public async Task<StudioIngestionSubmitPayloadResult> SubmitPayloadAsync(StudioIngestionPayloadEnvelope request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Id))
            {
                return StudioIngestionSubmitPayloadResult.Invalid("A payload id is required.");
            }

            if (request.Payload.ValueKind != JsonValueKind.Object)
            {
                return StudioIngestionSubmitPayloadResult.Invalid("Payload is required and must be a JSON object.");
            }

            IngestionRequest ingestionRequest;

            try
            {
                ingestionRequest = JsonSerializer.Deserialize<IngestionRequest>(request.Payload.GetRawText(), _serializerOptions)
                    ?? throw new JsonException("Payload could not be deserialized to an ingestion request.");
            }
            catch (JsonException)
            {
                return StudioIngestionSubmitPayloadResult.Invalid("Payload is not a valid ingestion request.");
            }

            if (ingestionRequest.RequestType != IngestionRequestType.IndexItem || ingestionRequest.IndexItem is null)
            {
                return StudioIngestionSubmitPayloadResult.Invalid("Payload must be an index-item ingestion request.");
            }

            if (!string.Equals(ingestionRequest.IndexItem.Id, request.Id, StringComparison.OrdinalIgnoreCase))
            {
                return StudioIngestionSubmitPayloadResult.Invalid("Payload id must match the wrapped id.");
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var queueWriter = scope.ServiceProvider.GetRequiredService<IFileShareStudioQueueWriter>();

            await queueWriter.SubmitAsync(request.Payload.GetRawText(), cancellationToken)
                             .ConfigureAwait(false);

            _logger.LogInformation("Submitted a file-share ingestion payload for batch {BatchId}.", request.Id);

            return StudioIngestionSubmitPayloadResult.Success("Payload submitted successfully.");
        }

        public async Task<StudioIngestionContextsResponse> GetContextsAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var batchPayloadStore = scope.ServiceProvider.GetRequiredService<IFileShareStudioBatchPayloadStore>();
            var businessUnits = await batchPayloadStore.GetBusinessUnitsAsync(cancellationToken)
                                                      .ConfigureAwait(false);

            return new StudioIngestionContextsResponse
            {
                Provider = ProviderName,
                Contexts = businessUnits.Select(businessUnit => new StudioIngestionContextResponse
                {
                    Value = businessUnit.Id.ToString(),
                    DisplayName = businessUnit.Name,
                    IsDefault = false
                })
                .ToArray()
            };
        }

        public async Task<StudioIngestionOperationExecutionResult> IndexAllAsync(
            IProgress<StudioIngestionOperationProgressUpdate> progress,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(progress);

            using var scope = _serviceScopeFactory.CreateScope();
            var batchPayloadStore = scope.ServiceProvider.GetRequiredService<IFileShareStudioBatchPayloadStore>();
            var queueWriter = scope.ServiceProvider.GetRequiredService<IFileShareStudioQueueWriter>();

            IReadOnlyList<Guid> batchIds;

            try
            {
                batchIds = await batchPayloadStore.GetPendingBatchIdsAsync(cancellationToken)
                                                  .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to load pending file-share batches for studio ingestion.");
                return StudioIngestionOperationExecutionResult.Failed("Failed to load unindexed items.", StudioIngestionFailureCodes.DatabaseError);
            }

            if (batchIds.Count == 0)
            {
                progress.Report(new StudioIngestionOperationProgressUpdate
                {
                    Message = "No unindexed items were found.",
                    Completed = 0,
                    Total = 0
                });

                _logger.LogInformation("No unindexed file-share batches were available for studio ingestion.");

                return StudioIngestionOperationExecutionResult.Success("No unindexed items were found.", 0, 0);
            }

            progress.Report(new StudioIngestionOperationProgressUpdate
            {
                Message = $"Processing {batchIds.Count} unindexed items.",
                Completed = 0,
                Total = batchIds.Count
            });

            var completed = 0;

            foreach (var batchId in batchIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                FileShareStudioBatchPayloadSource? payloadSource;

                try
                {
                    payloadSource = await batchPayloadStore.TryGetPayloadSourceAsync(batchId, cancellationToken)
                                                         .ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Failed to load the payload source for file-share batch {BatchId}.", batchId);
                    return StudioIngestionOperationExecutionResult.Failed("Failed to load unindexed items.", StudioIngestionFailureCodes.DatabaseError, completed, batchIds.Count);
                }

                if (payloadSource is null)
                {
                    _logger.LogError("The payload source for pending file-share batch {BatchId} was not found.", batchId);
                    return StudioIngestionOperationExecutionResult.Failed("Failed to translate an unindexed item.", StudioIngestionFailureCodes.ProviderError, completed, batchIds.Count);
                }

                var ingestionRequest = FileShareStudioIngestionRequestFactory.Create(payloadSource);
                var payloadJson = JsonSerializer.Serialize(ingestionRequest, _serializerOptions);

                try
                {
                    await queueWriter.SubmitAsync(payloadJson, cancellationToken)
                                     .ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Failed to submit file-share batch {BatchId} during studio index-all ingestion.", batchId);
                    return StudioIngestionOperationExecutionResult.Failed("Failed to submit unindexed items for ingestion.", StudioIngestionFailureCodes.QueueWriteFailed, completed, batchIds.Count);
                }

                try
                {
                    await batchPayloadStore.MarkBatchIndexedAsync(batchId, cancellationToken)
                                          .ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Failed to update the indexing status for file-share batch {BatchId}.", batchId);
                    return StudioIngestionOperationExecutionResult.Failed("Failed to update indexing status.", StudioIngestionFailureCodes.DatabaseError, completed, batchIds.Count);
                }

                completed++;
                progress.Report(new StudioIngestionOperationProgressUpdate
                {
                    Message = $"Processed {completed} of {batchIds.Count}.",
                    Completed = completed,
                    Total = batchIds.Count
                });
            }

            _logger.LogInformation("Submitted {BatchCount} unindexed file-share batches for studio ingestion.", completed);

            return StudioIngestionOperationExecutionResult.Success($"Processed {completed} of {batchIds.Count}.", completed, batchIds.Count);
        }

        public async Task<StudioIngestionOperationExecutionResult> IndexContextAsync(
            string context,
            IProgress<StudioIngestionOperationProgressUpdate> progress,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(context);
            ArgumentNullException.ThrowIfNull(progress);

            using var scope = _serviceScopeFactory.CreateScope();
            var batchPayloadStore = scope.ServiceProvider.GetRequiredService<IFileShareStudioBatchPayloadStore>();
            var queueWriter = scope.ServiceProvider.GetRequiredService<IFileShareStudioQueueWriter>();

            var businessUnit = await GetBusinessUnitAsync(context, batchPayloadStore, cancellationToken)
                .ConfigureAwait(false);
            if (businessUnit is null)
            {
                return StudioIngestionOperationExecutionResult.Failed($"Unknown context '{context}'.", StudioIngestionFailureCodes.UnknownContext);
            }

            IReadOnlyList<Guid> batchIds;

            try
            {
                batchIds = await batchPayloadStore.GetPendingBatchIdsForBusinessUnitAsync(businessUnit.Id, cancellationToken)
                                                  .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to load pending file-share batches for business unit {BusinessUnitId}.", businessUnit.Id);
                return StudioIngestionOperationExecutionResult.Failed("Failed to load context items.", StudioIngestionFailureCodes.DatabaseError);
            }

            if (batchIds.Count == 0)
            {
                progress.Report(new StudioIngestionOperationProgressUpdate
                {
                    Message = $"No unindexed items were found for {businessUnit.Name}.",
                    Completed = 0,
                    Total = 0
                });

                return StudioIngestionOperationExecutionResult.Success($"No unindexed items were found for {businessUnit.Name}.", 0, 0);
            }

            progress.Report(new StudioIngestionOperationProgressUpdate
            {
                Message = $"Processing {batchIds.Count} items for {businessUnit.Name}.",
                Completed = 0,
                Total = batchIds.Count
            });

            var completed = 0;

            foreach (var batchId in batchIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                FileShareStudioBatchPayloadSource? payloadSource;

                try
                {
                    payloadSource = await batchPayloadStore.TryGetPayloadSourceAsync(batchId, cancellationToken)
                                                         .ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Failed to load the payload source for file-share batch {BatchId} in context {Context}.", batchId, context);
                    return StudioIngestionOperationExecutionResult.Failed("Failed to load context items.", StudioIngestionFailureCodes.DatabaseError, completed, batchIds.Count);
                }

                if (payloadSource is null)
                {
                    _logger.LogError("The payload source for file-share batch {BatchId} in context {Context} was not found.", batchId, context);
                    return StudioIngestionOperationExecutionResult.Failed("Failed to translate a context item.", StudioIngestionFailureCodes.ProviderError, completed, batchIds.Count);
                }

                var ingestionRequest = FileShareStudioIngestionRequestFactory.Create(payloadSource);
                var payloadJson = JsonSerializer.Serialize(ingestionRequest, _serializerOptions);

                try
                {
                    await queueWriter.SubmitAsync(payloadJson, cancellationToken)
                                     .ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Failed to submit file-share batch {BatchId} during studio context ingestion for {Context}.", batchId, context);
                    return StudioIngestionOperationExecutionResult.Failed("Failed to submit context items for ingestion.", StudioIngestionFailureCodes.QueueWriteFailed, completed, batchIds.Count);
                }

                try
                {
                    await batchPayloadStore.MarkBatchIndexedAsync(batchId, cancellationToken)
                                          .ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Failed to update the indexing status for file-share batch {BatchId} in context {Context}.", batchId, context);
                    return StudioIngestionOperationExecutionResult.Failed("Failed to update indexing status.", StudioIngestionFailureCodes.DatabaseError, completed, batchIds.Count);
                }

                completed++;
                progress.Report(new StudioIngestionOperationProgressUpdate
                {
                    Message = $"Processed {completed} of {batchIds.Count} for {businessUnit.Name}.",
                    Completed = completed,
                    Total = batchIds.Count
                });
            }

            return StudioIngestionOperationExecutionResult.Success($"Processed {completed} of {batchIds.Count} for {businessUnit.Name}.", completed, batchIds.Count);
        }

        public async Task<StudioIngestionOperationExecutionResult> ResetIndexingStatusAsync(
            IProgress<StudioIngestionOperationProgressUpdate> progress,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(progress);

            using var scope = _serviceScopeFactory.CreateScope();
            var batchPayloadStore = scope.ServiceProvider.GetRequiredService<IFileShareStudioBatchPayloadStore>();

            int resetCount;

            try
            {
                resetCount = await batchPayloadStore.ResetAllIndexingStatusAsync(cancellationToken)
                                                   .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to reset file-share indexing status for studio ingestion.");
                return StudioIngestionOperationExecutionResult.Failed("Failed to reset indexing status.", StudioIngestionFailureCodes.DatabaseError);
            }

            progress.Report(new StudioIngestionOperationProgressUpdate
            {
                Message = $"Reset indexing status for {resetCount} items.",
                Completed = resetCount,
                Total = resetCount
            });

            _logger.LogInformation("Reset file-share indexing status for {BatchCount} batches from studio.", resetCount);

            return StudioIngestionOperationExecutionResult.Success($"Reset indexing status for {resetCount} items.", resetCount, resetCount);
        }

        public async Task<StudioIngestionOperationExecutionResult> ResetIndexingStatusForContextAsync(
            string context,
            IProgress<StudioIngestionOperationProgressUpdate> progress,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(context);
            ArgumentNullException.ThrowIfNull(progress);

            using var scope = _serviceScopeFactory.CreateScope();
            var batchPayloadStore = scope.ServiceProvider.GetRequiredService<IFileShareStudioBatchPayloadStore>();

            var businessUnit = await GetBusinessUnitAsync(context, batchPayloadStore, cancellationToken)
                .ConfigureAwait(false);
            if (businessUnit is null)
            {
                return StudioIngestionOperationExecutionResult.Failed($"Unknown context '{context}'.", StudioIngestionFailureCodes.UnknownContext);
            }

            int resetCount;

            try
            {
                resetCount = await batchPayloadStore.ResetIndexingStatusForBusinessUnitAsync(businessUnit.Id, cancellationToken)
                                                   .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to reset file-share indexing status for context {Context}.", context);
                return StudioIngestionOperationExecutionResult.Failed("Failed to reset indexing status.", StudioIngestionFailureCodes.DatabaseError);
            }

            progress.Report(new StudioIngestionOperationProgressUpdate
            {
                Message = $"Reset indexing status for {resetCount} items in {businessUnit.Name}.",
                Completed = resetCount,
                Total = resetCount
            });

            return StudioIngestionOperationExecutionResult.Success($"Reset indexing status for {resetCount} items in {businessUnit.Name}.", resetCount, resetCount);
        }

        private static async Task<FileShareStudioBusinessUnit?> GetBusinessUnitAsync(
            string context,
            IFileShareStudioBatchPayloadStore batchPayloadStore,
            CancellationToken cancellationToken)
        {
            if (!int.TryParse(context, out var businessUnitId) || businessUnitId <= 0)
            {
                return null;
            }

            var businessUnits = await batchPayloadStore.GetBusinessUnitsAsync(cancellationToken)
                                                      .ConfigureAwait(false);

            return businessUnits.FirstOrDefault(businessUnit => businessUnit.Id == businessUnitId);
        }
    }
}
