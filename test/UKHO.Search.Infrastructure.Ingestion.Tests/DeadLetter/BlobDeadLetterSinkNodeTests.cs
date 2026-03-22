using System.Text;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.DeadLetter;
using UKHO.Search.Infrastructure.Ingestion.Queue;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.DeadLetter
{
    public sealed class BlobDeadLetterSinkNodeTests
    {
        [Fact]
        public async Task Uploads_deadletter_record_with_runtime_payload_diagnostics_and_acks_on_success()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

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

            var channel = BoundedChannelFactory.Create<Envelope<int>>(1, true, true);

            var node = new BlobDeadLetterSinkNode<int>("deadletter", channel.Reader, blobServiceClient, config, logger: NullLogger.Instance);

            await node.StartAsync(cts.Token);

            var envelope = new Envelope<int>("doc-1", 123);
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

            var acker = new FakeQueueMessageAcker();
            envelope.Context.SetItem(QueueEnvelopeContextKeys.MessageAcker, acker);

            await channel.Writer.WriteAsync(envelope, cts.Token);
            channel.Writer.TryComplete();

            await node.Completion.WaitAsync(cts.Token);

            acker.DeleteCalls.ShouldBe(1);

            var putBlob = transport.Requests.Single(r => r.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase) && !r.Uri.Query.Contains("restype=container", StringComparison.OrdinalIgnoreCase));
            putBlob.Uri.AbsolutePath.ShouldContain("/ingestion-deadletter/deadletter/");
            putBlob.Uri.AbsolutePath.ShouldContain("/doc-1/");
            putBlob.Uri.AbsolutePath.ShouldContain($"/{envelope.MessageId:D}.json");

            putBlob.Body.ShouldNotBeNull();
            var bodyText = Encoding.UTF8.GetString(putBlob.Body!);
            bodyText.ShouldContain("\"envelope\"");
            bodyText.ShouldContain("\"key\":\"doc-1\"");
            bodyText.ShouldContain("\"payloadDiagnostics\"");
            bodyText.ShouldContain("\"runtimePayloadType\":\"System.Int32\"");
            bodyText.ShouldContain("\"payloadSnapshot\":123");
        }

        [Fact]
        public async Task Does_not_ack_when_upload_fails_and_non_fatal_mode_enabled()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var transport = new RecordingTransport(500);

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

            var channel = BoundedChannelFactory.Create<Envelope<int>>(1, true, true);

            var node = new BlobDeadLetterSinkNode<int>("deadletter", channel.Reader, blobServiceClient, config, false, logger: NullLogger.Instance);

            await node.StartAsync(cts.Token);

            var envelope = new Envelope<int>("doc-1", 123);
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

            var acker = new FakeQueueMessageAcker();
            envelope.Context.SetItem(QueueEnvelopeContextKeys.MessageAcker, acker);

            await channel.Writer.WriteAsync(envelope, cts.Token);
            channel.Writer.TryComplete();

            await node.Completion.WaitAsync(cts.Token);

            acker.DeleteCalls.ShouldBe(0);
        }

        [Fact]
        public async Task Uploads_fallback_deadletter_record_when_payload_cannot_be_serialized_and_acks_on_success()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

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

            var channel = BoundedChannelFactory.Create<Envelope<UnsupportedPayload>>(1, true, true);

            var node = new BlobDeadLetterSinkNode<UnsupportedPayload>("deadletter", channel.Reader, blobServiceClient, config, logger: NullLogger.Instance);

            await node.StartAsync(cts.Token);

            var envelope = new Envelope<UnsupportedPayload>("doc-1", new UnsupportedPayload());
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

            var acker = new FakeQueueMessageAcker();
            envelope.Context.SetItem(QueueEnvelopeContextKeys.MessageAcker, acker);

            await channel.Writer.WriteAsync(envelope, cts.Token);
            channel.Writer.TryComplete();

            await node.Completion.WaitAsync(cts.Token);

            acker.DeleteCalls.ShouldBe(1);

            var putBlob = transport.Requests.Single(r => r.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase) && !r.Uri.Query.Contains("restype=container", StringComparison.OrdinalIgnoreCase));
            putBlob.Body.ShouldNotBeNull();
            var bodyText = Encoding.UTF8.GetString(putBlob.Body!);
            bodyText.ShouldContain("\"serializationError\"");
            bodyText.ShouldContain("\"payloadDiagnostics\"");
            bodyText.ShouldContain("\"snapshotError\"");
            bodyText.ShouldContain("\"runtimePayloadType\":");
            bodyText.ShouldContain("UnsupportedPayload");
        }

        private sealed class UnsupportedPayload
        {
            public Action Callback { get; } = static () => { };
        }
    }
}