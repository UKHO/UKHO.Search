using Shouldly;
using UKHO.Search.Ingestion.Pipeline;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Ingestion.Providers.FileShare.Pipeline.Documents;
using UKHO.Search.Ingestion.Providers.FileShare.Pipeline.Nodes;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Pipeline
{
    public sealed class IngestionRequestDispatchNodeTests
    {
        [Fact]
        public async Task AddItem_is_dispatched_to_upsert_with_canonical_document()
        {
            var input = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(1, true, true);
            var output = BoundedChannelFactory.Create<Envelope<IngestionPipelineContext>>(1, true, true);
            var deadLetter = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(1, true, true);

            var canonicalBuilder = new CanonicalDocumentBuilder();

            var node = new IngestionRequestDispatchNode("dispatch", input.Reader, output.Writer, deadLetter.Writer, canonicalBuilder);

            await node.StartAsync(CancellationToken.None);

            var p1 = new IngestionProperty { Name = "Category", Type = IngestionPropertyType.String, Value = "A" };
            var properties = new List<IngestionProperty> { p1 };
            var addTimestamp = new DateTimeOffset(2024, 01, 02, 03, 04, 05, TimeSpan.Zero);
            var add = new IndexRequest("doc-1", properties, new[] { "t1" }, addTimestamp, new IngestionFileList());
            var request = new IngestionRequest(IngestionRequestType.IndexItem, add, null, null);

            await input.Writer.WriteAsync(CreateEnvelope("doc-1", request));
            input.Writer.TryComplete();

            await node.Completion.WaitAsync(TimeSpan.FromSeconds(2));

            output.Reader.TryRead(out var envelope)
                  .ShouldBeTrue();

            envelope.Payload.Request.RequestType.ShouldBe(IngestionRequestType.IndexItem);

            var upsert = envelope.Payload.Operation.ShouldBeOfType<UpsertOperation>();
            upsert.DocumentId.ShouldBe("doc-1");
            upsert.Document.Id.ShouldBe("doc-1");
            upsert.Document.Provider.ShouldBe("file-share");

            upsert.Document.Source.Properties.ShouldNotBeSameAs(add.Properties);
            upsert.Document.Source.Properties.Count.ShouldBe(1);
            upsert.Document.Source.Properties[0].Name.ShouldBe("category");
            upsert.Document.Source.Properties[0].Type.ShouldBe(IngestionPropertyType.String);
            upsert.Document.Source.Properties[0].Value.ShouldBe("A");
            upsert.Document.Timestamp.ShouldBe(addTimestamp);
        }

        [Fact]
        public async Task UpdateItem_is_dispatched_to_upsert_with_canonical_document()
        {
            var input = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(1, true, true);
            var output = BoundedChannelFactory.Create<Envelope<IngestionPipelineContext>>(1, true, true);
            var deadLetter = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(1, true, true);

            var canonicalBuilder = new CanonicalDocumentBuilder();

            var node = new IngestionRequestDispatchNode("dispatch", input.Reader, output.Writer, deadLetter.Writer, canonicalBuilder);

            await node.StartAsync(CancellationToken.None);

            var p1 = new IngestionProperty { Name = "Department", Type = IngestionPropertyType.String, Value = "Hydro" };
            var properties = new List<IngestionProperty> { p1 };
            var updateTimestamp = new DateTimeOffset(2025, 02, 03, 04, 05, 06, TimeSpan.Zero);
            var update = new IndexRequest("doc-1", properties, new[] { "t1" }, updateTimestamp, new IngestionFileList());
            var request = new IngestionRequest(IngestionRequestType.IndexItem, update, null, null);

            await input.Writer.WriteAsync(CreateEnvelope("doc-1", request));
            input.Writer.TryComplete();

            await node.Completion.WaitAsync(TimeSpan.FromSeconds(2));

            output.Reader.TryRead(out var envelope)
                  .ShouldBeTrue();

            envelope.Payload.Request.RequestType.ShouldBe(IngestionRequestType.IndexItem);

            var upsert = envelope.Payload.Operation.ShouldBeOfType<UpsertOperation>();
            upsert.DocumentId.ShouldBe("doc-1");
            upsert.Document.Id.ShouldBe("doc-1");
            upsert.Document.Provider.ShouldBe("file-share");

            upsert.Document.Source.Properties.ShouldNotBeSameAs(update.Properties);
            upsert.Document.Source.Properties.Count.ShouldBe(1);
            upsert.Document.Source.Properties[0].Name.ShouldBe("department");
            upsert.Document.Source.Properties[0].Type.ShouldBe(IngestionPropertyType.String);
            upsert.Document.Source.Properties[0].Value.ShouldBe("Hydro");
            upsert.Document.Timestamp.ShouldBe(updateTimestamp);
        }

        [Fact]
        public async Task IndexItem_without_provider_context_is_failed_and_routed_to_deadletter()
        {
            var input = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(1, true, true);
            var output = BoundedChannelFactory.Create<Envelope<IngestionPipelineContext>>(1, true, true);
            var deadLetter = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(1, true, true);

            var canonicalBuilder = new CanonicalDocumentBuilder();

            var node = new IngestionRequestDispatchNode("dispatch", input.Reader, output.Writer, deadLetter.Writer, canonicalBuilder);

            await node.StartAsync(CancellationToken.None);

            var request = new IngestionRequest(IngestionRequestType.IndexItem, new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["t1"], DateTimeOffset.UnixEpoch, new IngestionFileList()), null, null);

            await input.Writer.WriteAsync(new Envelope<IngestionRequest>("doc-1", request));
            input.Writer.TryComplete();

            await node.Completion.WaitAsync(TimeSpan.FromSeconds(2));

            output.Reader.TryRead(out var _)
                  .ShouldBeFalse();

            deadLetter.Reader.TryRead(out var failed)
                      .ShouldBeTrue();
            failed.Status.ShouldBe(MessageStatus.Failed);
            failed.Error.ShouldNotBeNull();
            failed.Error!.ExceptionMessage.ShouldContain("Provider context is required");
        }

        [Fact]
        public async Task DeleteItem_is_dispatched_to_delete_operation()
        {
            var input = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(1, true, true);
            var output = BoundedChannelFactory.Create<Envelope<IngestionPipelineContext>>(1, true, true);
            var deadLetter = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(1, true, true);

            var canonicalBuilder = new CanonicalDocumentBuilder();

            var node = new IngestionRequestDispatchNode("dispatch", input.Reader, output.Writer, deadLetter.Writer, canonicalBuilder);

            await node.StartAsync(CancellationToken.None);

            var delete = new DeleteItemRequest("doc-1");
            var request = new IngestionRequest(IngestionRequestType.DeleteItem, null, delete, null);

            await input.Writer.WriteAsync(new Envelope<IngestionRequest>("doc-1", request));
            input.Writer.TryComplete();

            await node.Completion.WaitAsync(TimeSpan.FromSeconds(2));

            output.Reader.TryRead(out var envelope)
                  .ShouldBeTrue();
            envelope.Payload.Operation.ShouldBeOfType<DeleteOperation>()
                    .DocumentId.ShouldBe("doc-1");
        }

        [Fact]
        public async Task Unsupported_dispatch_is_failed_and_routed_to_deadletter()
        {
            var input = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(1, true, true);
            var output = BoundedChannelFactory.Create<Envelope<IngestionPipelineContext>>(1, true, true);
            var deadLetter = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(1, true, true);

            var canonicalBuilder = new CanonicalDocumentBuilder();

            var node = new IngestionRequestDispatchNode("dispatch", input.Reader, output.Writer, deadLetter.Writer, canonicalBuilder);

            await node.StartAsync(CancellationToken.None);

            var invalid = new IngestionRequest
            {
                RequestType = IngestionRequestType.IndexItem,
                IndexItem = null,
                DeleteItem = null,
                UpdateAcl = null
            };

            await input.Writer.WriteAsync(new Envelope<IngestionRequest>("doc-1", invalid));
            input.Writer.TryComplete();

            await node.Completion.WaitAsync(TimeSpan.FromSeconds(2));

            output.Reader.TryRead(out var _)
                  .ShouldBeFalse();

            deadLetter.Reader.TryRead(out var failed)
                      .ShouldBeTrue();
            failed.Status.ShouldBe(MessageStatus.Failed);
            failed.Error.ShouldNotBeNull();
            failed.Error!.Category.ShouldBe(PipelineErrorCategory.Transform);
        }

        [Fact]
        public async Task Envelope_metadata_is_preserved_when_mapping_to_ingestion_pipeline_context()
        {
            var input = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(1, true, true);
            var output = BoundedChannelFactory.Create<Envelope<IngestionPipelineContext>>(1, true, true);
            var deadLetter = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(1, true, true);

            var canonicalBuilder = new CanonicalDocumentBuilder();

            var node = new IngestionRequestDispatchNode("dispatch", input.Reader, output.Writer, deadLetter.Writer, canonicalBuilder);

            await node.StartAsync(CancellationToken.None);

            var messageId = Guid.NewGuid();
            var headers = new Dictionary<string, string>(StringComparer.Ordinal) { ["h"] = "v" };
            var context = new MessageContext();
            context.AddBreadcrumb("upstream");

            var delete = new DeleteItemRequest("doc-1");
            var request = new IngestionRequest(IngestionRequestType.DeleteItem, null, delete, null);

            var envelope = new Envelope<IngestionRequest>("doc-1", request)
            {
                MessageId = messageId,
                CorrelationId = "corr",
                Attempt = 7,
                Headers = headers,
                Context = context
            };

            await input.Writer.WriteAsync(envelope);
            input.Writer.TryComplete();

            await node.Completion.WaitAsync(TimeSpan.FromSeconds(2));

            output.Reader.TryRead(out var outEnvelope)
                  .ShouldBeTrue();

            outEnvelope.MessageId.ShouldBe(messageId);
            outEnvelope.CorrelationId.ShouldBe("corr");
            outEnvelope.Attempt.ShouldBe(7);
            outEnvelope.Headers.ShouldBeSameAs(headers);
            outEnvelope.Context.ShouldBeSameAs(context);

            outEnvelope.Context.Breadcrumbs.ShouldContain("upstream");
            outEnvelope.Context.Breadcrumbs.ShouldContain("dispatch");
        }

        private static Envelope<IngestionRequest> CreateEnvelope(string key, IngestionRequest request)
        {
            var envelope = new Envelope<IngestionRequest>(key, request);
            envelope.Context.SetItem(ProviderEnvelopeContextKeys.ProviderParameters, new ProviderParameters("file-share"));
            return envelope;
        }
    }
}