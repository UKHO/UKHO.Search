using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Elastic;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Elastic
{
    /// <summary>
    /// Verifies how canonical documents are projected into the Elasticsearch payload shape.
    /// </summary>
    public sealed class CanonicalIndexDocumentTests
    {
        /// <summary>
        /// Confirms that the projection keeps the canonical provider value unchanged.
        /// </summary>
        [Fact]
        public void Create_preserves_provider_from_canonical_document()
        {
            var document = CanonicalDocument.CreateMinimal(
                "doc-1",
                "file-share",
                new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["t1"], DateTimeOffset.UnixEpoch, new IngestionFileList()),
                DateTimeOffset.UnixEpoch);

            var indexDocument = CanonicalIndexDocument.Create(document);

            indexDocument.Provider.ShouldBe("file-share");
        }

        /// <summary>
        /// Confirms that title projection preserves the display casing already stored in canonical state.
        /// </summary>
        [Fact]
        public void Create_preserves_title_values_and_casing_from_canonical_document()
        {
            var document = CanonicalDocument.CreateMinimal(
                "doc-1",
                "file-share",
                new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["t1"], DateTimeOffset.UnixEpoch, new IngestionFileList()),
                DateTimeOffset.UnixEpoch);

            document.AddTitle("Zulu Notice");
            document.AddTitle("Alpha Notice");

            var indexDocument = CanonicalIndexDocument.Create(document);

            indexDocument.Title.ShouldBe(new[]
            {
                "Alpha Notice",
                "Zulu Notice"
            });
        }

        /// <summary>
        /// Confirms that the Elasticsearch payload exposes normalized canonical security tokens.
        /// </summary>
        [Fact]
        public void Create_projects_security_tokens_from_canonical_document()
        {
            var document = CanonicalDocument.CreateMinimal(
                "doc-1",
                "file-share",
                new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["Token-B", "TOKEN-A"], DateTimeOffset.UnixEpoch, new IngestionFileList()),
                DateTimeOffset.UnixEpoch);

            document.AddSecurityToken("token-c");

            var indexDocument = CanonicalIndexDocument.Create(document);

            indexDocument.SecurityTokens.ShouldBe(new[]
            {
                "token-a",
                "token-b",
                "token-c"
            });
        }
    }
}
