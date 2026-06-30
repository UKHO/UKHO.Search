using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.Ingestion.Contracts;
using UKHO.Search.Ingestion.Contracts.Serialization;
using UKHO.Search.Studio.Ingestion;
using UKHO.Search.Studio.Providers.FileShare;
using Xunit;

namespace UKHO.Search.Studio.Providers.FileShare.Tests
{
    public sealed class FileShareStudioProviderIngestionTests
    {
        private static readonly JsonSerializerOptions SerializerOptions = IngestionJsonSerializerOptions.Create();

        [Fact]
        public async Task FetchPayloadByIdAsync_translates_batch_data_into_a_provider_neutral_payload_envelope()
        {
            var batchId = Guid.NewGuid();
            var createdOn = new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);
            var source = new FileShareStudioBatchPayloadSource
            {
                BatchId = batchId,
                CreatedOn = createdOn,
                ActiveBusinessUnitName = "Admiralty",
                Attributes =
                [
                    new KeyValuePair<string, string>("Title", "Weekly update")
                ],
                Files =
                [
                    new IngestionFile("week-01.zip", 42, createdOn, "application/zip")
                ]
            };
            using var services = BuildServices(new StubBatchPayloadStore(source), new RecordingQueueWriter());
            var provider = new FileShareStudioProvider(
                services.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<FileShareStudioProvider>.Instance);

            var result = await provider.FetchPayloadByIdAsync(batchId.ToString("D"));

            result.Status.ShouldBe(StudioIngestionResultStatus.Success);
            result.Response.ShouldNotBeNull();
            result.Response.Id.ShouldBe(batchId.ToString("D"));

            var ingestionRequest = JsonSerializer.Deserialize<IngestionRequest>(result.Response.Payload.GetRawText(), SerializerOptions);

            ingestionRequest.ShouldNotBeNull();
            ingestionRequest.RequestType.ShouldBe(IngestionRequestType.IndexItem);
            ingestionRequest.IndexItem.ShouldNotBeNull();
            ingestionRequest.IndexItem.Id.ShouldBe(batchId.ToString("D"));
            ingestionRequest.IndexItem.Properties.Any(property => property.Name == "title" && string.Equals(property.Value?.ToString(), "Weekly update", StringComparison.Ordinal)).ShouldBeTrue();
            ingestionRequest.IndexItem.Properties.Any(property => property.Name == "businessunitname" && string.Equals(property.Value?.ToString(), "Admiralty", StringComparison.Ordinal)).ShouldBeTrue();
            ingestionRequest.IndexItem.SecurityTokens.ShouldContain("batchcreate_admiralty");
            ingestionRequest.IndexItem.SecurityTokens.ShouldContain("batchcreate");
            ingestionRequest.IndexItem.SecurityTokens.ShouldContain("public");
        }

        [Fact]
        public async Task SubmitPayloadAsync_writes_the_wrapped_payload_to_the_queue_writer_before_returning_success()
        {
            var queueWriter = new RecordingQueueWriter();
            using var services = BuildServices(new StubBatchPayloadStore(null), queueWriter);
            var provider = new FileShareStudioProvider(
                services.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<FileShareStudioProvider>.Instance);
            var payload = JsonSerializer.SerializeToElement(
                new IngestionRequest
                {
                    RequestType = IngestionRequestType.IndexItem,
                    IndexItem = new IndexRequest(
                        "batch-123",
                        new IngestionPropertyList
                        {
                            new IngestionProperty
                            {
                                Name = "Title",
                                Type = IngestionPropertyType.String,
                                Value = "Weekly update"
                            },
                            new IngestionProperty
                            {
                                Name = "BusinessUnitName",
                                Type = IngestionPropertyType.String,
                                Value = "Admiralty"
                            }
                        },
                        ["batchcreate_admiralty", "batchcreate", "public"],
                        new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero),
                        [
                            new IngestionFile("week-01.zip", 42, new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero), "application/zip")
                        ])
                },
                SerializerOptions);

            var result = await provider.SubmitPayloadAsync(
                new StudioIngestionPayloadEnvelope
                {
                    Id = "batch-123",
                    Payload = payload
                });

            result.Status.ShouldBe(StudioIngestionResultStatus.Success);
            queueWriter.SubmittedPayloads.Count.ShouldBe(1);
            queueWriter.SubmittedPayloads[0].ShouldBe(payload.GetRawText());
        }

        [Fact]
        public async Task IndexAllAsync_submits_pending_file_share_batches_and_reports_coarse_progress()
        {
            var firstBatchId = Guid.NewGuid();
            var secondBatchId = Guid.NewGuid();
            var createdOn = new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);
            var payloadStore = new StubBatchPayloadStore(
                new Dictionary<Guid, FileShareStudioBatchPayloadSource>
                {
                    [firstBatchId] = CreatePayloadSource(firstBatchId, createdOn, "Admiralty", "Weekly update 1"),
                    [secondBatchId] = CreatePayloadSource(secondBatchId, createdOn.AddMinutes(1), "Admiralty", "Weekly update 2")
                },
                [firstBatchId, secondBatchId]);
            var queueWriter = new RecordingQueueWriter();
            using var services = BuildServices(payloadStore, queueWriter);
            var provider = new FileShareStudioProvider(
                services.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<FileShareStudioProvider>.Instance);
            var progressUpdates = new List<StudioIngestionOperationProgressUpdate>();

            var result = await provider.IndexAllAsync(new Progress<StudioIngestionOperationProgressUpdate>(update => progressUpdates.Add(update)));

            result.Succeeded.ShouldBeTrue();
            result.Completed.ShouldBe(2);
            result.Total.ShouldBe(2);
            queueWriter.SubmittedPayloads.Count.ShouldBe(2);
            payloadStore.MarkedBatchIds.ShouldBe([firstBatchId, secondBatchId]);
            progressUpdates.Count.ShouldBeGreaterThanOrEqualTo(2);
            progressUpdates[^1].Completed.ShouldBe(2);
            progressUpdates[^1].Total.ShouldBe(2);
        }

        [Fact]
        public async Task ResetIndexingStatusAsync_returns_success_after_provider_wide_reset()
        {
            var payloadStore = new StubBatchPayloadStore(new Dictionary<Guid, FileShareStudioBatchPayloadSource>(), [], resetCount: 5);
            using var services = BuildServices(payloadStore, new RecordingQueueWriter());
            var provider = new FileShareStudioProvider(
                services.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<FileShareStudioProvider>.Instance);
            var progressUpdates = new List<StudioIngestionOperationProgressUpdate>();

            var result = await provider.ResetIndexingStatusAsync(new Progress<StudioIngestionOperationProgressUpdate>(update => progressUpdates.Add(update)));

            result.Succeeded.ShouldBeTrue();
            result.Completed.ShouldBe(5);
            result.Total.ShouldBe(5);
            progressUpdates.ShouldHaveSingleItem();
            progressUpdates[0].Message.ShouldContain("5");
        }

        [Fact]
        public async Task IndexAllAsync_returns_queue_write_failed_when_queue_submission_throws()
        {
            var batchId = Guid.NewGuid();
            var payloadStore = new StubBatchPayloadStore(
                new Dictionary<Guid, FileShareStudioBatchPayloadSource>
                {
                    [batchId] = CreatePayloadSource(batchId, new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero), "Admiralty", "Weekly update")
                },
                [batchId]);
            using var services = BuildServices(payloadStore, new ThrowingQueueWriter());
            var provider = new FileShareStudioProvider(
                services.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<FileShareStudioProvider>.Instance);

            var result = await provider.IndexAllAsync(new Progress<StudioIngestionOperationProgressUpdate>(_ => { }));

            result.Succeeded.ShouldBeFalse();
            result.FailureCode.ShouldBe("queue-write-failed");
            result.Completed.ShouldBe(0);
            result.Total.ShouldBe(1);
        }

        [Fact]
        public async Task GetContextsAsync_maps_business_units_to_provider_neutral_contexts()
        {
            var payloadStore = new StubBatchPayloadStore(
                new Dictionary<Guid, FileShareStudioBatchPayloadSource>(),
                [],
                businessUnits:
                [
                    new FileShareStudioBusinessUnit { Id = 12, Name = "Admiralty" },
                    new FileShareStudioBusinessUnit { Id = 7, Name = "AVCS" }
                ]);
            using var services = BuildServices(payloadStore, new RecordingQueueWriter());
            var provider = new FileShareStudioProvider(
                services.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<FileShareStudioProvider>.Instance);

            var response = await provider.GetContextsAsync();

            response.Provider.ShouldBe("file-share");
            response.Contexts.Select(context => (context.Value, context.DisplayName)).ToArray().ShouldBe([
                ("12", "Admiralty"),
                ("7", "AVCS")
            ]);
            response.Contexts.All(context => context.IsDefault == false).ShouldBeTrue();
        }

        [Fact]
        public async Task IndexContextAsync_submits_pending_batches_for_the_selected_business_unit_and_reports_progress()
        {
            var firstBatchId = Guid.NewGuid();
            var secondBatchId = Guid.NewGuid();
            var createdOn = new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);
            var payloadStore = new StubBatchPayloadStore(
                new Dictionary<Guid, FileShareStudioBatchPayloadSource>
                {
                    [firstBatchId] = CreatePayloadSource(firstBatchId, createdOn, "Admiralty", "Weekly update 1"),
                    [secondBatchId] = CreatePayloadSource(secondBatchId, createdOn.AddMinutes(1), "Admiralty", "Weekly update 2")
                },
                [],
                businessUnits:
                [
                    new FileShareStudioBusinessUnit { Id = 12, Name = "Admiralty" }
                ],
                pendingBatchIdsByBusinessUnit: new Dictionary<int, IReadOnlyList<Guid>>
                {
                    [12] = [firstBatchId, secondBatchId]
                });
            var queueWriter = new RecordingQueueWriter();
            using var services = BuildServices(payloadStore, queueWriter);
            var provider = new FileShareStudioProvider(
                services.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<FileShareStudioProvider>.Instance);
            var progressUpdates = new List<StudioIngestionOperationProgressUpdate>();

            var result = await provider.IndexContextAsync("12", new Progress<StudioIngestionOperationProgressUpdate>(update => progressUpdates.Add(update)));

            result.Succeeded.ShouldBeTrue();
            result.Completed.ShouldBe(2);
            result.Total.ShouldBe(2);
            queueWriter.SubmittedPayloads.Count.ShouldBe(2);
            payloadStore.MarkedBatchIds.ShouldBe([firstBatchId, secondBatchId]);
            progressUpdates[^1].Completed.ShouldBe(2);
            progressUpdates[^1].Total.ShouldBe(2);
        }

        [Fact]
        public async Task ResetIndexingStatusForContextAsync_resets_batches_for_the_selected_business_unit()
        {
            var payloadStore = new StubBatchPayloadStore(
                new Dictionary<Guid, FileShareStudioBatchPayloadSource>(),
                [],
                resetCountByBusinessUnit: new Dictionary<int, int>
                {
                    [12] = 4
                },
                businessUnits:
                [
                    new FileShareStudioBusinessUnit { Id = 12, Name = "Admiralty" }
                ]);
            using var services = BuildServices(payloadStore, new RecordingQueueWriter());
            var provider = new FileShareStudioProvider(
                services.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<FileShareStudioProvider>.Instance);
            var progressUpdates = new List<StudioIngestionOperationProgressUpdate>();

            var result = await provider.ResetIndexingStatusForContextAsync("12", new Progress<StudioIngestionOperationProgressUpdate>(update => progressUpdates.Add(update)));

            result.Succeeded.ShouldBeTrue();
            result.Completed.ShouldBe(4);
            result.Total.ShouldBe(4);
            result.Message.ShouldContain("4");
        }

        private sealed class StubBatchPayloadStore : IFileShareStudioBatchPayloadStore
        {
            private readonly IReadOnlyList<FileShareStudioBusinessUnit> _businessUnits;
            private readonly IReadOnlyList<Guid> _pendingBatchIds;
            private readonly IReadOnlyDictionary<int, IReadOnlyList<Guid>> _pendingBatchIdsByBusinessUnit;
            private readonly IReadOnlyDictionary<Guid, FileShareStudioBatchPayloadSource> _payloadSources;
            private readonly int _resetCount;
            private readonly IReadOnlyDictionary<int, int> _resetCountByBusinessUnit;

            public StubBatchPayloadStore(FileShareStudioBatchPayloadSource? payloadSource)
                : this(
                    payloadSource is null
                        ? new Dictionary<Guid, FileShareStudioBatchPayloadSource>()
                        : new Dictionary<Guid, FileShareStudioBatchPayloadSource> { [payloadSource.BatchId] = payloadSource },
                    payloadSource is null ? [] : [payloadSource.BatchId])
            {
            }

            public StubBatchPayloadStore(
                IReadOnlyDictionary<Guid, FileShareStudioBatchPayloadSource> payloadSources,
                IReadOnlyList<Guid> pendingBatchIds,
                int resetCount = 0,
                IReadOnlyList<FileShareStudioBusinessUnit>? businessUnits = null,
                IReadOnlyDictionary<int, IReadOnlyList<Guid>>? pendingBatchIdsByBusinessUnit = null,
                IReadOnlyDictionary<int, int>? resetCountByBusinessUnit = null)
            {
                _businessUnits = businessUnits ?? [];
                _payloadSources = payloadSources;
                _pendingBatchIds = pendingBatchIds;
                _pendingBatchIdsByBusinessUnit = pendingBatchIdsByBusinessUnit ?? new Dictionary<int, IReadOnlyList<Guid>>();
                _resetCount = resetCount;
                _resetCountByBusinessUnit = resetCountByBusinessUnit ?? new Dictionary<int, int>();
            }

            public List<Guid> MarkedBatchIds { get; } = [];

            public Task<IReadOnlyList<FileShareStudioBusinessUnit>> GetBusinessUnitsAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_businessUnits);
            }

            public Task<IReadOnlyList<Guid>> GetPendingBatchIdsAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_pendingBatchIds);
            }

            public Task<IReadOnlyList<Guid>> GetPendingBatchIdsForBusinessUnitAsync(int businessUnitId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(
                    _pendingBatchIdsByBusinessUnit.TryGetValue(businessUnitId, out var batchIds)
                        ? batchIds
                        : (IReadOnlyList<Guid>)[]);
            }

            public Task MarkBatchIndexedAsync(Guid batchId, CancellationToken cancellationToken = default)
            {
                MarkedBatchIds.Add(batchId);
                return Task.CompletedTask;
            }

            public Task<int> ResetAllIndexingStatusAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_resetCount);
            }

            public Task<int> ResetIndexingStatusForBusinessUnitAsync(int businessUnitId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_resetCountByBusinessUnit.TryGetValue(businessUnitId, out var resetCount) ? resetCount : 0);
            }

            public Task<FileShareStudioBatchPayloadSource?> TryGetPayloadSourceAsync(Guid batchId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_payloadSources.TryGetValue(batchId, out var payloadSource) ? payloadSource : null);
            }
        }

        private sealed class RecordingQueueWriter : IFileShareStudioQueueWriter
        {
            public List<string> SubmittedPayloads { get; } = [];

            public Task SubmitAsync(string payloadJson, CancellationToken cancellationToken = default)
            {
                SubmittedPayloads.Add(payloadJson);
                return Task.CompletedTask;
            }
        }

        private sealed class ThrowingQueueWriter : IFileShareStudioQueueWriter
        {
            public Task SubmitAsync(string payloadJson, CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("Queue write failed.");
            }
        }

        private static FileShareStudioBatchPayloadSource CreatePayloadSource(
            Guid batchId,
            DateTimeOffset createdOn,
            string businessUnitName,
            string title)
        {
            return new FileShareStudioBatchPayloadSource
            {
                BatchId = batchId,
                CreatedOn = createdOn,
                ActiveBusinessUnitName = businessUnitName,
                Attributes =
                [
                    new KeyValuePair<string, string>("Title", title)
                ],
                Files =
                [
                    new IngestionFile("week-01.zip", 42, createdOn, "application/zip")
                ]
            };
        }

        private static ServiceProvider BuildServices(IFileShareStudioBatchPayloadStore payloadStore, IFileShareStudioQueueWriter queueWriter)
        {
            var services = new ServiceCollection();
            services.AddSingleton(payloadStore);
            services.AddSingleton(queueWriter);

            return services.BuildServiceProvider();
        }
    }
}
