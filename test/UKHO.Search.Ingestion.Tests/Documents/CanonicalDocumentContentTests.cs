using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Documents
{
    public sealed class CanonicalDocumentContentTests
    {
        [Fact]
        public void SetContent_sets_when_empty()
        {
            var doc = CreateDoc();

            doc.SetContent("First");

            doc.Content.ShouldBe("first");
        }

        [Fact]
        public void SetContent_appends_with_deterministic_separator_and_normalizes()
        {
            var doc = CreateDoc();

            doc.SetContent("Hello");
            doc.SetContent("WORLD");
            doc.SetContent("  again  ");

            doc.Content.ShouldBe("hello world again");
        }

        [Fact]
        public void SetContent_normalizes_to_lowercase_and_trims()
        {
            var doc = CreateDoc();

            doc.SetContent("  Hello WORLD  ");

            doc.Content.ShouldBe("hello world");
        }

        [Fact]
        public void SetContent_ignores_null_or_whitespace()
        {
            var doc = CreateDoc();

            doc.SetContent("hello");
            doc.SetContent(null);
            doc.SetContent(string.Empty);
            doc.SetContent("   ");

            doc.Content.ShouldBe("hello");
        }

        private static CanonicalDocument CreateDoc()
        {
            return CanonicalDocument.CreateMinimal("doc-1", Array.Empty<IngestionProperty>(), DateTimeOffset.UnixEpoch);
        }
    }
}
