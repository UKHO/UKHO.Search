using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Documents
{
    public sealed class CanonicalDocumentKeywordsTests
    {
        [Fact]
        public void AddKeyword_normalizes_to_lowercase_and_dedupes()
        {
            var doc = CreateDoc();

            doc.AddKeyword("Foo");
            doc.AddKeyword("FOO");
            doc.AddKeyword(" foo ");

            doc.Keywords.ShouldBe(new[] { "foo" });
        }

        [Fact]
        public void AddKeyword_ignores_null_or_whitespace()
        {
            var doc = CreateDoc();

            doc.AddKeyword(null);
            doc.AddKeyword(string.Empty);
            doc.AddKeyword("   ");

            doc.Keywords.ShouldBeEmpty();
        }

        [Fact]
        public void AddKeywords_normalizes_and_dedupes()
        {
            var doc = CreateDoc();

            doc.AddKeywords(new[] { "Alpha", "BETA", "alpha", "  ", null });

            doc.Keywords.ShouldBe(new[] { "alpha", "beta" });
        }

        [Fact]
        public void SetKeywordsFromTokens_splits_on_whitespace_normalizes_and_dedupes()
        {
            var doc = CreateDoc();

            doc.SetKeywordsFromTokens("One  TWO\nThree\tTwo");

            doc.Keywords.ShouldBe(new[] { "one", "three", "two" });
        }

        private static CanonicalDocument CreateDoc()
        {
            return CanonicalDocument.CreateMinimal("doc-1", Array.Empty<IngestionProperty>(), DateTimeOffset.UnixEpoch);
        }
    }
}
