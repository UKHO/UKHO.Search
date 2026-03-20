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

            var add = new IndexRequest("doc-1", [
                new IngestionProperty { Name = "Title", Type = IngestionPropertyType.String, Value = "Hello World" },
                new IngestionProperty { Name = "Department", Type = IngestionPropertyType.String, Value = "Hydro" }
            ], ["t1"], DateTimeOffset.UnixEpoch, new IngestionFileList());

            var request = new IngestionRequest(IngestionRequestType.IndexItem, add, null, null);
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

            await enricher.TryBuildEnrichmentAsync(request, document);

            document.Keywords.ShouldBe(new[] { "hello world", "hydro" });
        }

        [Fact]
        public async Task UpdateItem_copies_property_values_into_keywords()
        {
            var enricher = new BasicEnricher();

            var update = new IndexRequest("doc-1", [
                new IngestionProperty { Name = "Title", Type = IngestionPropertyType.String, Value = "Hello World" }
            ], ["t1"], DateTimeOffset.UnixEpoch, new IngestionFileList());

            var request = new IngestionRequest(IngestionRequestType.IndexItem, update, null, null);
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

            await enricher.TryBuildEnrichmentAsync(request, document);

            document.Keywords.ShouldBe(new[] { "hello world" });
        }

        [Fact]
        public async Task String_array_values_are_flattened_and_deduped()
        {
            var enricher = new BasicEnricher();

            var add = new IndexRequest("doc-1", [
                new IngestionProperty { Name = "Tags", Type = IngestionPropertyType.StringArray, Value = new[] { "A", "b", "A", "  " } }
            ], ["t1"], DateTimeOffset.UnixEpoch, new IngestionFileList());

            var request = new IngestionRequest(IngestionRequestType.IndexItem, add, null, null);
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

            await enricher.TryBuildEnrichmentAsync(request, document);

            document.Keywords.ShouldBe(new[] { "a", "b" });
        }

        [Fact]
        public async Task Null_or_whitespace_values_are_ignored()
        {
            var enricher = new BasicEnricher();

            var add = new IndexRequest("doc-1", [
                new IngestionProperty { Name = "Title", Type = IngestionPropertyType.String, Value = "  " }
            ], ["t1"], DateTimeOffset.UnixEpoch, new IngestionFileList());

            var request = new IngestionRequest(IngestionRequestType.IndexItem, add, null, null);
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

            await enricher.TryBuildEnrichmentAsync(request, document);

            document.Keywords.ShouldBeEmpty();
        }

        [Fact]
        public async Task Non_string_values_are_converted_deterministically()
        {
            var enricher = new BasicEnricher();

            var add = new IndexRequest("doc-1", [
                new IngestionProperty { Name = "Url", Type = IngestionPropertyType.Uri, Value = new Uri("https://Example.COM/Path") }
            ], ["t1"], DateTimeOffset.UnixEpoch, new IngestionFileList());

            var request = new IngestionRequest(IngestionRequestType.IndexItem, add, null, null);
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

            await enricher.TryBuildEnrichmentAsync(request, document);

            document.Keywords.ShouldBe(new[] { "https://example.com/path" });
        }

        [Fact]
        public async Task Keyword_deduplication_is_canonical_normalized_across_properties()
        {
            var enricher = new BasicEnricher();

            var add = new IndexRequest("doc-1", [
                new IngestionProperty { Name = "Title", Type = IngestionPropertyType.String, Value = "Alpha" },
                new IngestionProperty { Name = "Department", Type = IngestionPropertyType.String, Value = "ALPHA" }
            ], ["t1"], DateTimeOffset.UnixEpoch, new IngestionFileList());

            var request = new IngestionRequest(IngestionRequestType.IndexItem, add, null, null);
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

            await enricher.TryBuildEnrichmentAsync(request, document);

            document.Keywords.ShouldBe(new[] { "alpha" });
        }

        [Fact]
        public async Task Hyphenated_numeric_s_tokens_expand_to_support_search_recall()
        {
            var enricher = new BasicEnricher();

            var add = new IndexRequest("doc-1", [
                new IngestionProperty { Name = "Specification", Type = IngestionPropertyType.String, Value = "S-100" }
            ], ["t1"], DateTimeOffset.UnixEpoch, new IngestionFileList());

            var request = new IngestionRequest(IngestionRequestType.IndexItem, add, null, null);
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

            await enricher.TryBuildEnrichmentAsync(request, document);

            document.Keywords.ShouldBe(new[] { "s-100", "s100" });
        }
    }
}