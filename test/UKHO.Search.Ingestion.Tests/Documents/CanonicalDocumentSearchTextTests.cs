using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Documents
{
    public sealed class CanonicalDocumentSearchTextTests
    {
        [Fact]
        public void AddSearchText_normalizes_to_lowercase_and_trims()
        {
            var doc = CreateDoc();

            doc.AddSearchText("  Hello WORLD  ");

            doc.SearchText.ShouldBe("hello world");
        }

        [Fact]
        public void AddSearchText_appends_with_deterministic_separator_and_normalizes()
        {
            var doc = CreateDoc();

            doc.AddSearchText("Hello");
            doc.AddSearchText("WORLD");
            doc.AddSearchText("  again  ");

            doc.SearchText.ShouldBe("hello world again");
        }

        [Fact]
        public void AddSearchText_ignores_null_or_whitespace()
        {
            var doc = CreateDoc();

            doc.AddSearchText("hello");
            doc.AddSearchText(null);
            doc.AddSearchText(string.Empty);
            doc.AddSearchText("   ");

            doc.SearchText.ShouldBe("hello");
        }

        private static CanonicalDocument CreateDoc()
        {
            return CanonicalDocument.CreateMinimal("doc-1", "file-share", new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["t"], DateTimeOffset.UnixEpoch, new IngestionFileList()), DateTimeOffset.UnixEpoch);
        }
    }
}