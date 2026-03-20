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
        public void AddKeywords_is_a_simple_additive_wrapper_without_alias_expansion()
        {
            var doc = CreateDoc();

            doc.AddKeywords(new[] { "S-100" });

            doc.Keywords.ShouldBe(new[] { "s-100" });
            doc.Keywords.ShouldNotContain("s100");
        }

        [Fact]
        public void AddKeywordsFromTokens_preserves_hyphenated_tokens_ignores_repeated_delimiters_and_adds_aliases()
        {
            var doc = CreateDoc();

            doc.AddKeywordsFromTokens("One,, TWO; s-100 \nThree\t;S-100");

            doc.Keywords.ShouldBe(new[] { "one", "s-100", "s100", "three", "two" });
        }

        private static CanonicalDocument CreateDoc()
        {
            return CanonicalDocument.CreateMinimal("doc-1", "file-share", new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["t"], DateTimeOffset.UnixEpoch, new IngestionFileList()), DateTimeOffset.UnixEpoch);
        }
    }
}