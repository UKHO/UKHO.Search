using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Documents
{
    /// <summary>
    /// Verifies normalization and ordering behaviour for the canonical taxonomy fields.
    /// </summary>
    public sealed class CanonicalDocumentTaxonomyFieldsTests
    {
        /// <summary>
        /// Confirms that authority values are normalized, de-duplicated, and sorted while seeded security tokens remain present.
        /// </summary>
        [Fact]
        public void Authority_is_additive_normalizes_to_lowercase_dedupes_and_is_sorted()
        {
            var doc = CreateDoc();

            doc.AddAuthority("Bravo");
            doc.AddAuthority("alpha");
            doc.AddAuthority("ALPHA");
            doc.AddAuthority("  ");
            doc.AddAuthority(null);

            doc.Authority.ShouldBe(new[] { "alpha", "bravo" });
            doc.SecurityTokens.ShouldBe(["token-a", "token-b"]);
        }

        /// <summary>
        /// Confirms that region values are normalized, de-duplicated, and sorted.
        /// </summary>
        [Fact]
        public void Region_is_additive_normalizes_to_lowercase_dedupes_and_is_sorted()
        {
            var doc = CreateDoc();

            doc.AddRegion("Zulu");
            doc.AddRegion("echo");
            doc.AddRegion("ECHO");

            doc.Region.ShouldBe(new[] { "echo", "zulu" });
        }

        /// <summary>
        /// Confirms that format values are normalized, de-duplicated, and sorted.
        /// </summary>
        [Fact]
        public void Format_is_additive_normalizes_to_lowercase_dedupes_and_is_sorted()
        {
            var doc = CreateDoc();

            doc.AddFormat("Pdf");
            doc.AddFormat("epub");
            doc.AddFormat("PDF");

            doc.Format.ShouldBe(new[] { "epub", "pdf" });
        }

        /// <summary>
        /// Confirms that category values are normalized, de-duplicated, and sorted.
        /// </summary>
        [Fact]
        public void Category_is_additive_normalizes_to_lowercase_dedupes_and_is_sorted()
        {
            var doc = CreateDoc();

            doc.AddCategory("Charts");
            doc.AddCategory("aids");
            doc.AddCategory("CHARTS");

            doc.Category.ShouldBe(new[] { "aids", "charts" });
        }

        /// <summary>
        /// Confirms that series values are normalized, de-duplicated, and sorted.
        /// </summary>
        [Fact]
        public void Series_is_additive_normalizes_to_lowercase_dedupes_and_is_sorted()
        {
            var doc = CreateDoc();

            doc.AddSeries("B");
            doc.AddSeries("a");
            doc.AddSeries("A");

            doc.Series.ShouldBe(new[] { "a", "b" });
        }

        /// <summary>
        /// Confirms that instance values are normalized, de-duplicated, and sorted.
        /// </summary>
        [Fact]
        public void Instance_is_additive_normalizes_to_lowercase_dedupes_and_is_sorted()
        {
            var doc = CreateDoc();

            doc.AddInstance("2");
            doc.AddInstance("1");
            doc.AddInstance("2");

            doc.Instance.ShouldBe(new[] { "1", "2" });
        }

        /// <summary>
        /// Confirms that major-version values are de-duplicated and sorted numerically.
        /// </summary>
        [Fact]
        public void MajorVersion_is_additive_dedupes_and_is_sorted()
        {
            var doc = CreateDoc();

            doc.AddMajorVersion(2);
            doc.AddMajorVersion(1);
            doc.AddMajorVersion(2);
            doc.AddMajorVersion(null);

            doc.MajorVersion.ShouldBe(new[] { 1, 2 });
        }

        /// <summary>
        /// Confirms that minor-version values are de-duplicated and sorted numerically.
        /// </summary>
        [Fact]
        public void MinorVersion_is_additive_dedupes_and_is_sorted()
        {
            var doc = CreateDoc();

            doc.AddMinorVersion(10);
            doc.AddMinorVersion(2);
            doc.AddMinorVersion(10);
            doc.AddMinorVersion(null);

            doc.MinorVersion.ShouldBe(new[] { 2, 10 });
        }

        /// <summary>
        /// Creates the minimal canonical document used by the taxonomy tests.
        /// </summary>
        /// <returns>A canonical document seeded with normalized security tokens.</returns>
        private static CanonicalDocument CreateDoc()
        {
            // Seed mixed-case request tokens so the helper reflects the current canonical shape.
            return CanonicalDocument.CreateMinimal(
                "doc-1",
                "file-share",
                new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["Token-B", "TOKEN-A"], DateTimeOffset.UnixEpoch, new IngestionFileList()),
                DateTimeOffset.UnixEpoch);
        }
    }
}
