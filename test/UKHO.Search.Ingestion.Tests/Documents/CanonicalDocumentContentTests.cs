using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Documents
{
    /// <summary>
    /// Verifies how canonical content text is normalized and appended without disturbing the seeded canonical shape.
    /// </summary>
    public sealed class CanonicalDocumentContentTests
    {
        /// <summary>
        /// Confirms that the first retained content value becomes the canonical content surface while the seeded security tokens remain intact.
        /// </summary>
        [Fact]
        public void AddContent_sets_when_empty()
        {
            var doc = CreateDoc();

            doc.AddContent("First");

            doc.Content.ShouldBe("first");
            doc.SecurityTokens.ShouldBe(["token-a", "token-b"]);
        }

        /// <summary>
        /// Confirms that later content fragments are appended in a deterministic normalized form.
        /// </summary>
        [Fact]
        public void AddContent_appends_with_deterministic_separator_and_normalizes()
        {
            var doc = CreateDoc();

            doc.AddContent("Hello");
            doc.AddContent("WORLD");
            doc.AddContent("  again  ");

            doc.Content.ShouldBe("hello world again");
        }

        /// <summary>
        /// Confirms that content values are trimmed and lowercased before they are retained.
        /// </summary>
        [Fact]
        public void AddContent_normalizes_to_lowercase_and_trims()
        {
            var doc = CreateDoc();

            doc.AddContent("  Hello WORLD  ");

            doc.Content.ShouldBe("hello world");
        }

        /// <summary>
        /// Confirms that null or whitespace-only content input does not change the canonical content surface.
        /// </summary>
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

        /// <summary>
        /// Creates the minimal canonical document used by the content tests.
        /// </summary>
        /// <returns>A canonical document seeded with normalized security tokens.</returns>
        private static CanonicalDocument CreateDoc()
        {
            // Seed mixed-case request tokens so the helper reflects the current canonical shape instead of the older token-light test shape.
            return CanonicalDocument.CreateMinimal("doc-1", "file-share", new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["Token-B", "TOKEN-A"], DateTimeOffset.UnixEpoch, new IngestionFileList()), DateTimeOffset.UnixEpoch);
        }
    }
}