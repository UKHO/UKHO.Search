using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Pipeline.Documents
{
    /// <summary>
    /// Verifies the special title normalization rules that preserve display casing while the rest of the minimal canonical shape stays intact.
    /// </summary>
    public sealed class CanonicalDocumentTitleTests
    {
        /// <summary>
        /// Confirms that title values preserve casing, trim whitespace, de-duplicate, sort deterministically, and keep seeded security tokens unchanged.
        /// </summary>
        [Fact]
        public void AddTitle_WhenValuesAreMixed_PreservesCasingTrimsDedupesAndSorts()
        {
            var document = CanonicalDocument.CreateMinimal(
                "doc-1",
                "file-share",
                new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["Token-B", "TOKEN-A"], DateTimeOffset.UnixEpoch, new IngestionFileList()),
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
            document.SecurityTokens.ShouldBe(["token-a", "token-b"]);
        }
    }
}
