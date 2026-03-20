using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Providers.FileShare.Enrichment.Handlers;
using System.Xml.Linq;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Enrichment
{
    public sealed class S100BatchContentHandlerS101ParsingTests
    {
        [Fact]
        public async Task HandleFiles_when_S101_catalog_adds_keywords_and_search_text()
        {
            var xml = await LoadS101SampleXmlAsync();
            var catalogPath = await WriteTempCatalogXmlAsync(xml);

            var handler = new S100BatchContentHandler(NullLogger<S100BatchContentHandler>.Instance);
            var request = CreateAddRequest("batch-s101");
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

            await handler.HandleFiles(new[] { catalogPath }, request, document, CancellationToken.None);

            document.Keywords.ShouldContain("s-101");
            document.Keywords.ShouldContain("s101");

            document.SearchText.ShouldContain("australian hydrographic office");
            document.SearchText.ShouldContain("this is an s-101 enc produced");

            document.GeoPolygons.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        public async Task HandleFiles_when_product_spec_is_not_S101_does_not_enrich()
        {
            var xml = (await LoadS101SampleXmlAsync()).Replace("<XC:name>S-101</XC:name>", "<XC:name>S-102</XC:name>", StringComparison.Ordinal);
            var catalogPath = await WriteTempCatalogXmlAsync(xml);

            var handler = new S100BatchContentHandler(NullLogger<S100BatchContentHandler>.Instance);
            var request = CreateAddRequest("batch-not-s101");
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

            await handler.HandleFiles(new[] { catalogPath }, request, document, CancellationToken.None);

            document.Keywords.ShouldBeEmpty();
            document.SearchText.ShouldBeEmpty();
            document.GeoPolygons.Count.ShouldBe(0);
        }

        [Fact]
        public async Task HandleFiles_when_posList_is_invalid_skips_polygon_and_still_enriches_keywords_and_search_text()
        {
            var xml = (await LoadS101SampleXmlAsync()).Replace("<gml:posList>", "<gml:posList> not-a-number ", StringComparison.Ordinal);
            var catalogPath = await WriteTempCatalogXmlAsync(xml);

            var handler = new S100BatchContentHandler(NullLogger<S100BatchContentHandler>.Instance);
            var request = CreateAddRequest("batch-invalid-poslist");
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

            await handler.HandleFiles(new[] { catalogPath }, request, document, CancellationToken.None);

            document.Keywords.ShouldContain("s-101");
            document.Keywords.ShouldContain("s101");
            document.SearchText.ShouldContain("australian hydrographic office");
            document.GeoPolygons.Count.ShouldBe(0);
        }

        private static IngestionRequest CreateAddRequest(string id)
        {
            return new IngestionRequest(
                IngestionRequestType.IndexItem,
                new IndexRequest
                {
                    Id = id,
                    Timestamp = DateTimeOffset.UtcNow,
                    Properties = new IngestionPropertyList()
                },
                deleteItem: null,
                updateAcl: null);
        }

        private static async Task<string> LoadS101SampleXmlAsync()
        {
            var baseDirectory = AppContext.BaseDirectory;
            var path = Path.Combine(baseDirectory, "TestData", "s101-CATALOG.XML");

            return await File.ReadAllTextAsync(path);
        }

        private static async Task<string> WriteTempCatalogXmlAsync(string content)
        {
            var directory = Path.Combine(Path.GetTempPath(), "ukho-search", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            var path = Path.Combine(directory, "catalog.xml");
            await File.WriteAllTextAsync(path, content);
            return path;
        }
    }
}
