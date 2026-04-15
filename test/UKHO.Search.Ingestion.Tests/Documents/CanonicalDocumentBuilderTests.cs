using Shouldly;
using UKHO.Search.Ingestion.Pipeline;
using UKHO.Search.Ingestion.Providers.FileShare.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Documents
{
    /// <summary>
    /// Verifies how the File Share canonical document builder creates the minimal canonical shape.
    /// </summary>
    public sealed class CanonicalDocumentBuilderTests
    {
        /// <summary>
        /// Confirms that the builder keeps a defensive source copy while also creating normalized canonical security tokens.
        /// </summary>
        [Fact]
        public void BuildForUpsert_sets_provider_and_copies_source()
        {
            // Arrange a request with mixed-case security tokens so the test can observe both
            // the preserved source payload and the normalized canonical state.
            var builder = new CanonicalDocumentBuilder();
            var request = new IngestionRequest(
                IngestionRequestType.IndexItem,
                new IndexRequest(
                    "doc-1",
                    [new IngestionProperty { Name = "Category", Type = IngestionPropertyType.String, Value = "A" }],
                    ["Token-B", "TOKEN-A"],
                    DateTimeOffset.UnixEpoch,
                    new IngestionFileList()),
                null,
                null);

            // Act.
            var document = builder.BuildForUpsert("doc-1", request, new ProviderParameters("file-share"));

            // Assert the provider, copied source payload, and normalized canonical token set.
            document.Provider.ShouldBe("file-share");
            document.Source.Properties.ShouldNotBeSameAs(request.IndexItem!.Properties);
            document.Source.Properties.Count.ShouldBe(1);
            document.Source.SecurityTokens.ShouldBe(["Token-B", "TOKEN-A"]);
            document.SecurityTokens.ShouldBe(["token-a", "token-b"]);
        }

        /// <summary>
        /// Confirms that the builder still rejects missing provider metadata before creating the canonical document.
        /// </summary>
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
