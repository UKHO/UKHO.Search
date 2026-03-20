using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Elastic;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Elastic
{
    public sealed class CanonicalIndexDocumentTests
    {
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
    }
}
