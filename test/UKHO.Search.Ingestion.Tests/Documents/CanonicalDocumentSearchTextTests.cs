using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Documents
{
    public sealed class CanonicalDocumentSearchTextTests
    {
        [Fact]
        public void SetSearchText_normalizes_to_lowercase_and_trims()
        {
            var doc = CreateDoc();

            doc.SetSearchText("  Hello WORLD  ");

            doc.SearchText.ShouldBe("hello world");
        }

        [Fact]
        public void SetSearchText_appends_with_deterministic_separator_and_normalizes()
        {
            var doc = CreateDoc();

            doc.SetSearchText("Hello");
            doc.SetSearchText("WORLD");
            doc.SetSearchText("  again  ");

            doc.SearchText.ShouldBe("hello world again");
        }

        [Fact]
        public void SetSearchText_ignores_null_or_whitespace()
        {
            var doc = CreateDoc();

            doc.SetSearchText("hello");
            doc.SetSearchText(null);
            doc.SetSearchText(string.Empty);
            doc.SetSearchText("   ");

            doc.SearchText.ShouldBe("hello");
        }

        private static CanonicalDocument CreateDoc()
        {
            return CanonicalDocument.CreateMinimal("doc-1", Array.Empty<IngestionProperty>(), DateTimeOffset.UnixEpoch);
        }
    }
}