using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Providers.FileShare.Enrichment.Handlers.Enrichers;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Enrichment.Handlers.Enrichers
{
    public sealed class S57EnricherTests
    {
        [Fact]
        public void TryEnrich_extracts_expected_coverage_polygon_and_text_for_sample_fixture()
        {
            var enricher = new BasicS57Enricher(NullLogger<BasicS57Enricher>.Instance);

            var doc = CanonicalDocument.CreateMinimal("doc-1", "file-share", new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["t"], DateTimeOffset.UtcNow, new IngestionFileList()), DateTimeOffset.UtcNow);

            var fixturePath = FindFixturePath("sample.000");

            var ok = enricher.TryParse(fixturePath, doc);

            ok.ShouldBeTrue();

            doc.SearchText.ShouldContain("produced by noaa");
            doc.GeoPolygons.Count.ShouldBeGreaterThan(0);
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
