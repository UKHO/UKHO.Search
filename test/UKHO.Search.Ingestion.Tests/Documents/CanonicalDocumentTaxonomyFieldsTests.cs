using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Documents
{
    public sealed class CanonicalDocumentTaxonomyFieldsTests
    {
        [Fact]
        public void Authority_is_additive_normalizes_to_lowercase_dedupes_and_is_sorted()
        {
            var doc = CreateDoc();

            doc.SetAuthority("Bravo");
            doc.AddAuthority("alpha");
            doc.AddAuthority("ALPHA");
            doc.AddAuthority("  ");
            doc.AddAuthority(null);

            doc.Authority.ShouldBe(new[] { "alpha", "bravo" });
        }

        [Fact]
        public void Region_is_additive_normalizes_to_lowercase_dedupes_and_is_sorted()
        {
            var doc = CreateDoc();

            doc.SetRegion("Zulu");
            doc.AddRegion("echo");
            doc.AddRegion("ECHO");

            doc.Region.ShouldBe(new[] { "echo", "zulu" });
        }

        [Fact]
        public void Format_is_additive_normalizes_to_lowercase_dedupes_and_is_sorted()
        {
            var doc = CreateDoc();

            doc.SetFormat("Pdf");
            doc.AddFormat("epub");
            doc.AddFormat("PDF");

            doc.Format.ShouldBe(new[] { "epub", "pdf" });
        }

        [Fact]
        public void Category_is_additive_normalizes_to_lowercase_dedupes_and_is_sorted()
        {
            var doc = CreateDoc();

            doc.SetCategory("Charts");
            doc.AddCategory("aids");
            doc.AddCategory("CHARTS");

            doc.Category.ShouldBe(new[] { "aids", "charts" });
        }

        [Fact]
        public void Series_is_additive_normalizes_to_lowercase_dedupes_and_is_sorted()
        {
            var doc = CreateDoc();

            doc.SetSeries("B");
            doc.AddSeries("a");
            doc.AddSeries("A");

            doc.Series.ShouldBe(new[] { "a", "b" });
        }

        [Fact]
        public void Instance_is_additive_normalizes_to_lowercase_dedupes_and_is_sorted()
        {
            var doc = CreateDoc();

            doc.SetInstance("2");
            doc.AddInstance("1");
            doc.AddInstance("2");

            doc.Instance.ShouldBe(new[] { "1", "2" });
        }

        [Fact]
        public void MajorVersion_is_additive_dedupes_and_is_sorted()
        {
            var doc = CreateDoc();

            doc.SetMajorVersion(2);
            doc.AddMajorVersion(1);
            doc.AddMajorVersion(2);
            doc.AddMajorVersion(null);

            doc.MajorVersion.ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public void MinorVersion_is_additive_dedupes_and_is_sorted()
        {
            var doc = CreateDoc();

            doc.SetMinorVersion(10);
            doc.AddMinorVersion(2);
            doc.AddMinorVersion(10);
            doc.AddMinorVersion(null);

            doc.MinorVersion.ShouldBe(new[] { 2, 10 });
        }

        private static CanonicalDocument CreateDoc()
        {
            return CanonicalDocument.CreateMinimal(
                "doc-1",
                new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["t"], DateTimeOffset.UnixEpoch, new IngestionFileList()),
                DateTimeOffset.UnixEpoch);
        }
    }
}
