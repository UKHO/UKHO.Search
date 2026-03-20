using Shouldly;
using UKHO.Search.Ingestion.Pipeline;
using UKHO.Search.Ingestion.Providers.FileShare.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Documents
{
    public sealed class CanonicalDocumentBuilderTests
    {
        [Fact]
        public void BuildForUpsert_sets_provider_and_copies_source()
        {
            var builder = new CanonicalDocumentBuilder();
            var request = new IngestionRequest(
                IngestionRequestType.IndexItem,
                new IndexRequest(
                    "doc-1",
                    [new IngestionProperty { Name = "Category", Type = IngestionPropertyType.String, Value = "A" }],
                    ["t1"],
                    DateTimeOffset.UnixEpoch,
                    new IngestionFileList()),
                null,
                null);

            var document = builder.BuildForUpsert("doc-1", request, new ProviderParameters("file-share"));

            document.Provider.ShouldBe("file-share");
            document.Source.Properties.ShouldNotBeSameAs(request.IndexItem!.Properties);
            document.Source.Properties.Count.ShouldBe(1);
        }

        [Fact]
        public void BuildForUpsert_throws_when_provider_parameters_are_missing()
        {
            var builder = new CanonicalDocumentBuilder();
            var request = new IngestionRequest(
                IngestionRequestType.IndexItem,
                new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["t1"], DateTimeOffset.UnixEpoch, new IngestionFileList()),
                null,
                null);

            Should.Throw<ArgumentNullException>(() => builder.BuildForUpsert("doc-1", request, null!));
        }
    }
}
