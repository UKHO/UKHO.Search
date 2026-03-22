using System.Text;
using System.Text.Json;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.DeadLetter;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.DeadLetter
{
    public sealed class DeadLetterSchemaConsistencyTests
    {
        [Fact]
        public async Task File_and_blob_deadletter_sinks_emit_the_same_logical_schema()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var filePath = Path.Combine(Path.GetTempPath(), "ukho-search", Guid.NewGuid()
                                                                               .ToString("N"), "deadletter.jsonl");
            var fileChannel = BoundedChannelFactory.Create<Envelope<int>>(1, true, true);
            var fileNode = new DeadLetterSinkNode<int>("dead-letter", fileChannel.Reader, filePath, logger: NullLogger.Instance);
            await fileNode.StartAsync(cts.Token);

            var blobTransport = new RecordingTransport(201);
            var options = new BlobClientOptions
            {
                Transport = blobTransport,
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
            var blobChannel = BoundedChannelFactory.Create<Envelope<int>>(1, true, true);
            var blobNode = new BlobDeadLetterSinkNode<int>("dead-letter", blobChannel.Reader, blobServiceClient, config, logger: NullLogger.Instance);
            await blobNode.StartAsync(cts.Token);

            var fileEnvelope = CreateEnvelope(123);
            var blobEnvelope = CreateEnvelope(123);

            await fileChannel.Writer.WriteAsync(fileEnvelope, cts.Token);
            fileChannel.Writer.TryComplete();
            await blobChannel.Writer.WriteAsync(blobEnvelope, cts.Token);
            blobChannel.Writer.TryComplete();

            await fileNode.Completion.WaitAsync(cts.Token);
            await blobNode.Completion.WaitAsync(cts.Token);

            using var fileJson = JsonDocument.Parse((await File.ReadAllLinesAsync(filePath, cts.Token)).Single());
            var blobRequest = blobTransport.Requests.Single(r => r.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase) && !r.Uri.Query.Contains("restype=container", StringComparison.OrdinalIgnoreCase));
            using var blobJson = JsonDocument.Parse(Encoding.UTF8.GetString(blobRequest.Body!));

            GetPropertyNames(fileJson.RootElement).ShouldBe(GetPropertyNames(blobJson.RootElement));
            GetPropertyNames(fileJson.RootElement.GetProperty("envelope")).ShouldBe(GetPropertyNames(blobJson.RootElement.GetProperty("envelope")));
            GetPropertyNames(fileJson.RootElement.GetProperty("payloadDiagnostics")).ShouldBe(GetPropertyNames(blobJson.RootElement.GetProperty("payloadDiagnostics")));
        }

        private static Envelope<int> CreateEnvelope(int payload)
        {
            var envelope = new Envelope<int>("doc-1", payload);
            envelope.MarkFailed(new PipelineError
            {
                Category = PipelineErrorCategory.Validation,
                Code = "TEST",
                Message = "failed",
                ExceptionType = null,
                ExceptionMessage = null,
                StackTrace = null,
                IsTransient = false,
                OccurredAtUtc = DateTimeOffset.UtcNow,
                NodeName = "test",
                Details = new Dictionary<string, string>()
            });

            return envelope;
        }

        private static string[] GetPropertyNames(JsonElement element)
        {
            return element.EnumerateObject()
                          .Select(x => x.Name)
                          .OrderBy(x => x, StringComparer.Ordinal)
                          .ToArray();
        }
    }
}
