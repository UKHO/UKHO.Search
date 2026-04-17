using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Documents
{
    /// <summary>
    /// Verifies how canonical search text is normalized and appended while the minimal canonical document retains its seeded shape.
    /// </summary>
    public sealed class CanonicalDocumentSearchTextTests
    {
        /// <summary>
        /// Confirms that search text is trimmed and lowercased before being stored.
        /// </summary>
        [Fact]
        public void AddSearchText_normalizes_to_lowercase_and_trims()
        {
            var doc = CreateDoc();

            doc.AddSearchText("  Hello WORLD  ");

            doc.SearchText.ShouldBe("hello world");
            doc.SecurityTokens.ShouldBe(["token-a", "token-b"]);
        }

        /// <summary>
        /// Confirms that multiple search-text fragments are appended in a deterministic normalized form.
        /// </summary>
        [Fact]
        public void AddSearchText_appends_with_deterministic_separator_and_normalizes()
        {
            var doc = CreateDoc();

            doc.AddSearchText("Hello");
            doc.AddSearchText("WORLD");
            doc.AddSearchText("  again  ");

            doc.SearchText.ShouldBe("hello world again");
        }

        /// <summary>
        /// Confirms that null or whitespace-only input does not change the canonical search surface.
        /// </summary>
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

        /// <summary>
        /// Creates the minimal canonical document used by the search-text tests.
        /// </summary>
        /// <returns>A canonical document seeded with normalized security tokens.</returns>
        private static CanonicalDocument CreateDoc()
        {
            // Seed mixed-case request tokens so the helper reflects the current canonical shape.
            return CanonicalDocument.CreateMinimal("doc-1", "file-share", new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["Token-B", "TOKEN-A"], DateTimeOffset.UnixEpoch, new IngestionFileList()), DateTimeOffset.UnixEpoch);
        }
    }
}