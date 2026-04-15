using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Documents
{
    /// <summary>
    /// Verifies keyword normalization and tokenization behaviour while the minimal canonical document retains its seeded security-token shape.
    /// </summary>
    public sealed class CanonicalDocumentKeywordsTests
    {
        /// <summary>
        /// Confirms that repeated keyword inputs collapse to one normalized retained value.
        /// </summary>
        [Fact]
        public void AddKeyword_normalizes_to_lowercase_and_dedupes()
        {
            var doc = CreateDoc();

            doc.AddKeyword("Foo");
            doc.AddKeyword("FOO");
            doc.AddKeyword(" foo ");

            doc.Keywords.ShouldBe(new[] { "foo" });
            doc.SecurityTokens.ShouldBe(["token-a", "token-b"]);
        }

        /// <summary>
        /// Confirms that null or whitespace-only keyword input is ignored.
        /// </summary>
        [Fact]
        public void AddKeyword_ignores_null_or_whitespace()
        {
            var doc = CreateDoc();

            doc.AddKeyword(null);
            doc.AddKeyword(string.Empty);
            doc.AddKeyword("   ");

            doc.Keywords.ShouldBeEmpty();
        }

        /// <summary>
        /// Confirms that the bulk keyword overload reuses the same normalization and de-duplication behaviour.
        /// </summary>
        [Fact]
        public void AddKeywords_normalizes_and_dedupes()
        {
            var doc = CreateDoc();

            doc.AddKeywords(new[] { "Alpha", "BETA", "alpha", "  ", null });

            doc.Keywords.ShouldBe(new[] { "alpha", "beta" });
        }

        /// <summary>
        /// Confirms that the bulk keyword overload adds values directly without alias expansion.
        /// </summary>
        [Fact]
        public void AddKeywords_is_a_simple_additive_wrapper_without_alias_expansion()
        {
            var doc = CreateDoc();

            doc.AddKeywords(new[] { "S-100" });

            doc.Keywords.ShouldBe(new[] { "s-100" });
            doc.Keywords.ShouldNotContain("s100");
        }

        /// <summary>
        /// Confirms that tokenized keyword input preserves hyphenated values, ignores repeated delimiters, and adds aliases.
        /// </summary>
        [Fact]
        public void AddKeywordsFromTokens_preserves_hyphenated_tokens_ignores_repeated_delimiters_and_adds_aliases()
        {
            var doc = CreateDoc();

            doc.AddKeywordsFromTokens("One,, TWO; s-100 \nThree\t;S-100");

            doc.Keywords.ShouldBe(new[] { "one", "s-100", "s100", "three", "two" });
        }

        /// <summary>
        /// Creates the minimal canonical document used by the keyword tests.
        /// </summary>
        /// <returns>A canonical document seeded with normalized security tokens.</returns>
        private static CanonicalDocument CreateDoc()
        {
            // Seed mixed-case request tokens so the helper reflects the current canonical shape.
            return CanonicalDocument.CreateMinimal("doc-1", "file-share", new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["Token-B", "TOKEN-A"], DateTimeOffset.UnixEpoch, new IngestionFileList()), DateTimeOffset.UnixEpoch);
        }
    }
}