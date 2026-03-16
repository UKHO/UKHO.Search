using Shouldly;
using UKHO.Search.Ingestion.Pipeline;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Ingestion.Providers.FileShare.Pipeline.Documents;
using UKHO.Search.Ingestion.Providers.FileShare.Pipeline.Nodes;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Pipeline
{
    public sealed class FileShareProviderDispatchBuildPathTests
    {
        [Fact]
        public async Task AddItem_produces_canonical_document_via_provider_dispatch_and_build_path()
        {
            var input = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(1, true, true);
            var output = BoundedChannelFactory.Create<Envelope<IngestionPipelineContext>>(1, true, true);
            var deadLetter = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(1, true, true);

            var canonicalBuilder = new CanonicalDocumentBuilder();

            var node = new IngestionRequestDispatchNode("dispatch", input.Reader, output.Writer, deadLetter.Writer, canonicalBuilder);

            await node.StartAsync(CancellationToken.None);

            var p1 = new IngestionProperty { Name = "Category", Type = IngestionPropertyType.String, Value = "A" };
            var properties = new List<IngestionProperty> { p1 };
            var add = new IndexRequest("doc-1", properties, new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList());
            var request = new IngestionRequest(IngestionRequestType.IndexItem, add, null, null);

            await input.Writer.WriteAsync(new Envelope<IngestionRequest>("doc-1", request));
            input.Writer.TryComplete();

            await node.Completion.WaitAsync(TimeSpan.FromSeconds(2));

            output.Reader.TryRead(out var envelope)
                  .ShouldBeTrue();
            deadLetter.Reader.TryRead(out var _)
                      .ShouldBeFalse();

            var upsert = envelope.Payload.Operation.ShouldBeOfType<UpsertOperation>();
            upsert.DocumentId.ShouldBe("doc-1");
            upsert.Document.Id.ShouldBe("doc-1");
            upsert.Document.Source.Properties.ShouldNotBeSameAs(add.Properties);
            upsert.Document.Source.Properties.Count.ShouldBe(1);
            upsert.Document.Source.Properties[0].Name.ShouldBe("category");
            upsert.Document.Source.Properties[0].Type.ShouldBe(IngestionPropertyType.String);
            upsert.Document.Source.Properties[0].Value.ShouldBe("A");
            upsert.Document.Timestamp.ShouldBe(add.Timestamp);
        }
    }
}