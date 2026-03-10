using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Documents
{
    public sealed class CanonicalDocumentFacetsTests
    {
        [Fact]
        public void AddFacetValue_normalizes_name_and_value_to_lowercase_and_dedupes()
        {
            var doc = CreateDoc();

            doc.AddFacetValue("Category", "Alpha");
            doc.AddFacetValue("CATEGORY", "ALPHA");
            doc.AddFacetValue(" category ", " alpha ");

            doc.Facets.Keys.ShouldBe(new[] { "category" });
            doc.Facets["category"].ShouldBe(new[] { "alpha" });
        }

        [Fact]
        public void AddFacetValue_ignores_null_or_whitespace_name_or_value()
        {
            var doc = CreateDoc();

            doc.AddFacetValue(null, "x");
            doc.AddFacetValue(" ", "x");
            doc.AddFacetValue("name", null);
            doc.AddFacetValue("name", " ");

            doc.Facets.ShouldBeEmpty();
        }

        [Fact]
        public void AddFacetValues_adds_multiple_values_and_dedupes()
        {
            var doc = CreateDoc();

            doc.AddFacetValues("tag", new[] { "A", "b", "A", null, "  " });

            doc.Facets["tag"].ShouldBe(new[] { "a", "b" });
        }

        private static CanonicalDocument CreateDoc()
        {
            return CanonicalDocument.CreateMinimal("doc-1", Array.Empty<IngestionProperty>(), DateTimeOffset.UnixEpoch);
        }
    }
}
