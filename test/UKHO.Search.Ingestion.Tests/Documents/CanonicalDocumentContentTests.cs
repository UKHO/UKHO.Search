using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Documents
{
    public sealed class CanonicalDocumentContentTests
    {
        [Fact]
        public void AddContent_sets_when_empty()
        {
            var doc = CreateDoc();

            doc.AddContent("First");

            doc.Content.ShouldBe("first");
        }

        [Fact]
        public void AddContent_appends_with_deterministic_separator_and_normalizes()
        {
            var doc = CreateDoc();

            doc.AddContent("Hello");
            doc.AddContent("WORLD");
            doc.AddContent("  again  ");

            doc.Content.ShouldBe("hello world again");
        }

        [Fact]
        public void AddContent_normalizes_to_lowercase_and_trims()
        {
            var doc = CreateDoc();

            doc.AddContent("  Hello WORLD  ");

            doc.Content.ShouldBe("hello world");
        }

        [Fact]
        public void AddContent_ignores_null_or_whitespace()
        {
            var doc = CreateDoc();

            doc.AddContent("hello");
            doc.AddContent(null);
            doc.AddContent(string.Empty);
            doc.AddContent("   ");

            doc.Content.ShouldBe("hello");
        }

        private static CanonicalDocument CreateDoc()
        {
            return CanonicalDocument.CreateMinimal("doc-1", "file-share", new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["t"], DateTimeOffset.UnixEpoch, new IngestionFileList()), DateTimeOffset.UnixEpoch);
        }
    }
}