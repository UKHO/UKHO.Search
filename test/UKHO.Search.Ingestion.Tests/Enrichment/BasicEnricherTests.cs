using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Providers.FileShare.Enrichment;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Enrichment
{
    public sealed class BasicEnricherTests
    {
        [Fact]
        public void Ordinal_is_10()
        {
            var enricher = new BasicEnricher();

            enricher.Ordinal.ShouldBe(10);
        }

        [Fact]
        public async Task AddItem_copies_property_values_into_keywords()
        {
            var enricher = new BasicEnricher();

            var add = new AddItemRequest("doc-1", [
                new IngestionProperty { Name = "Title", Type = IngestionPropertyType.String, Value = "Hello World" },
                new IngestionProperty { Name = "Department", Type = IngestionPropertyType.String, Value = "Hydro" }
            ], ["t1"], DateTimeOffset.UnixEpoch, new IngestionFileList());

            var request = new IngestionRequest(IngestionRequestType.AddItem, add, null, null, null);
            var document = CanonicalDocument.CreateMinimal("doc-1", Array.Empty<IngestionProperty>(), DateTimeOffset.UnixEpoch);

            await enricher.TryBuildEnrichmentAsync(request, document);

            document.Keywords.ShouldBe(new[] { "hello world", "hydro" });
            document.Facets.ShouldBeEmpty();
        }

        [Fact]
        public async Task UpdateItem_copies_property_values_into_keywords()
        {
            var enricher = new BasicEnricher();

            var update = new UpdateItemRequest("doc-1", [
                new IngestionProperty { Name = "Title", Type = IngestionPropertyType.String, Value = "Hello World" }
            ], ["t1"], DateTimeOffset.UnixEpoch, new IngestionFileList());

            var request = new IngestionRequest(IngestionRequestType.UpdateItem, null, update, null, null);
            var document = CanonicalDocument.CreateMinimal("doc-1", Array.Empty<IngestionProperty>(), DateTimeOffset.UnixEpoch);

            await enricher.TryBuildEnrichmentAsync(request, document);

            document.Keywords.ShouldBe(new[] { "hello world" });
            document.Facets.ShouldBeEmpty();
        }

        [Fact]
        public async Task String_array_values_are_flattened_and_deduped()
        {
            var enricher = new BasicEnricher();

            var add = new AddItemRequest("doc-1", [
                new IngestionProperty { Name = "Tags", Type = IngestionPropertyType.StringArray, Value = new[] { "A", "b", "A", "  " } }
            ], ["t1"], DateTimeOffset.UnixEpoch, new IngestionFileList());

            var request = new IngestionRequest(IngestionRequestType.AddItem, add, null, null, null);
            var document = CanonicalDocument.CreateMinimal("doc-1", Array.Empty<IngestionProperty>(), DateTimeOffset.UnixEpoch);

            await enricher.TryBuildEnrichmentAsync(request, document);

            document.Keywords.ShouldBe(new[] { "a", "b" });
            document.Facets.ShouldBeEmpty();
        }

        [Fact]
        public async Task Null_or_whitespace_values_are_ignored()
        {
            var enricher = new BasicEnricher();

            var add = new AddItemRequest("doc-1", [
                new IngestionProperty { Name = "Title", Type = IngestionPropertyType.String, Value = "  " }
            ], ["t1"], DateTimeOffset.UnixEpoch, new IngestionFileList());

            var request = new IngestionRequest(IngestionRequestType.AddItem, add, null, null, null);
            var document = CanonicalDocument.CreateMinimal("doc-1", Array.Empty<IngestionProperty>(), DateTimeOffset.UnixEpoch);

            await enricher.TryBuildEnrichmentAsync(request, document);

            document.Keywords.ShouldBeEmpty();
            document.Facets.ShouldBeEmpty();
        }

        [Fact]
        public async Task Non_string_values_are_converted_deterministically()
        {
            var enricher = new BasicEnricher();

            var add = new AddItemRequest("doc-1", [
                new IngestionProperty { Name = "Url", Type = IngestionPropertyType.Uri, Value = new Uri("https://Example.COM/Path") }
            ], ["t1"], DateTimeOffset.UnixEpoch, new IngestionFileList());

            var request = new IngestionRequest(IngestionRequestType.AddItem, add, null, null, null);
            var document = CanonicalDocument.CreateMinimal("doc-1", Array.Empty<IngestionProperty>(), DateTimeOffset.UnixEpoch);

            await enricher.TryBuildEnrichmentAsync(request, document);

            document.Keywords.ShouldBe(new[] { "https://example.com/path" });
            document.Facets.ShouldBeEmpty();
        }

        [Fact]
        public async Task Keyword_deduplication_is_canonical_normalized_across_properties()
        {
            var enricher = new BasicEnricher();

            var add = new AddItemRequest("doc-1", [
                new IngestionProperty { Name = "Title", Type = IngestionPropertyType.String, Value = "Alpha" },
                new IngestionProperty { Name = "Department", Type = IngestionPropertyType.String, Value = "ALPHA" }
            ], ["t1"], DateTimeOffset.UnixEpoch, new IngestionFileList());

            var request = new IngestionRequest(IngestionRequestType.AddItem, add, null, null, null);
            var document = CanonicalDocument.CreateMinimal("doc-1", Array.Empty<IngestionProperty>(), DateTimeOffset.UnixEpoch);

            await enricher.TryBuildEnrichmentAsync(request, document);

            document.Keywords.ShouldBe(new[] { "alpha" });
            document.Facets.ShouldBeEmpty();
        }
    }
}