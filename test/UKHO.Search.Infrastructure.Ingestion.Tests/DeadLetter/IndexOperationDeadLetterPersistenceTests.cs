using System.Text;
using System.Text.Json;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.Geo;
using UKHO.Search.Infrastructure.Ingestion.DeadLetter;
using UKHO.Search.Infrastructure.Ingestion.Queue;
using UKHO.Search.Infrastructure.Ingestion.Pipeline.Nodes;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.DeadLetter
{
    public sealed class IndexOperationDeadLetterPersistenceTests
    {
        [Fact]
        public async Task Failed_upsert_bulk_index_persists_deadletter_with_geojson_geo_polygons()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var transport = new RecordingTransport(201);
            var options = new BlobClientOptions
            {
                Transport = transport,
                Retry =
                {
                    MaxRetries = 0
                }
            };

            var credential = new StorageSharedKeyCredential("testaccount", Convert.ToBase64String(new byte[32]));
            var blobServiceClient = new BlobServiceClient(new Uri("https://example.blob.core.windows.net"), credential, options);
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                                                   {
                                                       ["ingestion:deadletterContainer"] = "ingestion-deadletter",
                                                       ["ingestion:deadletterBlobPrefix"] = "deadletter"
                                                   })
                                                   .Build();

            var input = BoundedChannelFactory.Create<BatchEnvelope<IndexOperation>>(1, true, true);
            var success = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);
            var deadLetter = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);

            var document = CreateCanonicalDocument("doc-1");
            var envelope = new Envelope<IndexOperation>("doc-1", new UpsertOperation("doc-1", document));
            var acker = new FakeQueueMessageAcker();
            envelope.Context.SetItem(QueueEnvelopeContextKeys.MessageAcker, acker);

            var bulkClient = new SingleResponseBulkClient(new BulkIndexResponse
            {
                Items =
                [
                    new BulkIndexItemResult
                    {
                        MessageId = envelope.MessageId,
                        StatusCode = 400,
                        ErrorType = "document_parsing_exception",
                        ErrorReason = "[1:13142] failed to parse field [geoPolygons] of type [geo_shape]"
                    }
                ]
            });

            var bulkNode = new InOrderBulkIndexNode("bulk", input.Reader, bulkClient, success.Writer, deadLetter.Writer, 1, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, static (_, _) => Task.CompletedTask);
            var deadLetterNode = new BlobDeadLetterSinkNode<IndexOperation>("deadletter", deadLetter.Reader, blobServiceClient, config, logger: NullLogger.Instance);

            await bulkNode.StartAsync(cts.Token);
            await deadLetterNode.StartAsync(cts.Token);

            await input.Writer.WriteAsync(new BatchEnvelope<IndexOperation>
            {
                BatchId = Guid.NewGuid(),
                PartitionId = 0,
                Items = [envelope],
                CreatedUtc = DateTimeOffset.UtcNow,
                FlushedUtc = DateTimeOffset.UtcNow
            }, cts.Token);
            input.Writer.TryComplete();

            await bulkNode.Completion.WaitAsync(cts.Token);
            await deadLetterNode.Completion.WaitAsync(cts.Token);

            success.Reader.TryRead(out _).ShouldBeFalse();
            acker.DeleteCalls.ShouldBe(1);

            var putBlob = transport.Requests.Single(r => r.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase) && !r.Uri.Query.Contains("restype=container", StringComparison.OrdinalIgnoreCase));
            using var json = JsonDocument.Parse(Encoding.UTF8.GetString(putBlob.Body!));

            json.RootElement.GetProperty("error")
                .GetProperty("code")
                .GetString()
                .ShouldBe("BULK_INDEX_FAILED");
            json.RootElement.GetProperty("error")
                .GetProperty("message")
                .GetString()
                .ShouldContain("geoPolygons");
            json.RootElement.GetProperty("payloadDiagnostics")
                .GetProperty("runtimePayloadType")
                .GetString()
                .ShouldBe(typeof(UpsertOperation).FullName);
            json.RootElement.GetProperty("payloadDiagnostics")
                .GetProperty("payloadSnapshot")
                .GetProperty("document")
                .GetProperty("id")
                .GetString()
                .ShouldBe("doc-1");
            json.RootElement.GetProperty("payloadDiagnostics")
                .GetProperty("payloadSnapshot")
                .GetProperty("document")
                .GetProperty("geoPolygons")
                .GetProperty("type")
                .GetString()
                .ShouldBe("Polygon");
            json.RootElement.GetProperty("payloadDiagnostics")
                .GetProperty("payloadSnapshot")
                .GetProperty("document")
                .GetProperty("geoPolygons")
                .GetProperty("coordinates")[0][0][0]
                .GetDouble()
                .ShouldBe(1d);
            json.RootElement.GetProperty("payloadDiagnostics")
                .GetProperty("payloadSnapshot")
                .GetProperty("document")
                .GetProperty("geoPolygons")
                .GetProperty("coordinates")[0][0][1]
                .GetDouble()
                .ShouldBe(2d);

            var deadLetterJson = Encoding.UTF8.GetString(putBlob.Body!);
            deadLetterJson.ShouldNotContain("\"rings\"");
            deadLetterJson.ShouldNotContain("\"longitude\"");
            deadLetterJson.ShouldNotContain("\"latitude\"");
        }

        private static CanonicalDocument CreateCanonicalDocument(string documentId)
        {
            var document = CanonicalDocument.CreateMinimal(documentId, "file-share", new IndexRequest(documentId, Array.Empty<IngestionProperty>(), ["t1"], DateTimeOffset.UnixEpoch, new IngestionFileList()), DateTimeOffset.UnixEpoch);
            document.AddGeoPolygon(GeoPolygon.Create(new[]
            {
                GeoCoordinate.Create(1d, 2d),
                GeoCoordinate.Create(3d, 2d),
                GeoCoordinate.Create(3d, 4d),
                GeoCoordinate.Create(1d, 2d)
            }));

            return document;
        }

        private sealed class SingleResponseBulkClient : IBulkIndexClient<IndexOperation>
        {
            private readonly BulkIndexResponse _response;

            public SingleResponseBulkClient(BulkIndexResponse response)
            {
                _response = response;
            }

            public ValueTask<BulkIndexResponse> BulkIndexAsync(BulkIndexRequest<IndexOperation> request, CancellationToken cancellationToken)
            {
                return ValueTask.FromResult(_response);
            }
        }
    }
}
