using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Pipeline.Documents
{
    public sealed class CanonicalDocumentTitleTests
    {
        [Fact]
        public void AddTitle_WhenValuesAreMixed_PreservesCasingTrimsDedupesAndSorts()
        {
            var document = CanonicalDocument.CreateMinimal(
                "doc-1",
                "file-share",
                new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["t1"], DateTimeOffset.UnixEpoch, new IngestionFileList()),
                DateTimeOffset.UnixEpoch);

            document.AddTitle("  Zebra Notice  ");
            document.AddTitle("Alpha Notice");
            document.AddTitle("Zebra Notice");
            document.AddTitle("   ");
            document.AddTitles(["Bravo Notice", null, "Alpha Notice"]);

            document.Title.ShouldBe(new[]
            {
                "Alpha Notice",
                "Bravo Notice",
                "Zebra Notice"
            });
        }
    }
}
