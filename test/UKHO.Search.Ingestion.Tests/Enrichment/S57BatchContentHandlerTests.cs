using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.Geo;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Providers.FileShare.Enrichment.Handlers;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Enrichment
{
    public sealed class S57BatchContentHandlerTests
    {
        [Fact]
        public async Task HandleFiles_when_s57_dataset_present_enriches_search_text_and_geo_polygon()
        {
            var handler = new S57BatchContentHandler(NullLogger<S57BatchContentHandler>.Instance);

            var doc = CanonicalDocument.CreateMinimal("doc-1", "file-share", new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["t"], DateTimeOffset.UtcNow, new IngestionFileList()), DateTimeOffset.UtcNow);
            var request = new IngestionRequest(IngestionRequestType.IndexItem, new IndexRequest("doc-1", new List<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UtcNow, new IngestionFileList()), null, null);

            var fixturePath = FindFixturePath("sample.000");
            var paths = new[] { fixturePath };

            await handler.HandleFiles(paths, request, doc, CancellationToken.None);

            doc.SearchText.ShouldContain("produced by noaa");
            doc.GeoPolygons.Count.ShouldBe(1);

            var ring = doc.GeoPolygons[0].Rings[0];
            ring.Count.ShouldBeGreaterThanOrEqualTo(4);

            // Ensure roughly matches the expected bounding polygon (lon/lat)
            ring[0].Longitude.ShouldBe(-79.2);
            ring[0].Latitude.ShouldBe(33.375);
        }

        private static string FindFixturePath(string fileName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                var candidate = System.IO.Path.Combine(dir.FullName, "test", "TestData", fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                dir = dir.Parent;
            }

            throw new FileNotFoundException($"Unable to locate test fixture '{fileName}' under a 'test/TestData' folder.");
        }
    }
}
