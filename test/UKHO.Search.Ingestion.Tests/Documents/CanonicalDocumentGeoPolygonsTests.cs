using Shouldly;
using UKHO.Search.Geo;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Documents
{
    /// <summary>
    /// Verifies how canonical geo polygons coexist with the rest of the minimal canonical document shape.
    /// </summary>
    public sealed class CanonicalDocumentGeoPolygonsTests
    {
        /// <summary>
        /// Confirms that minimal canonical documents start without geo polygons while still carrying normalized security tokens.
        /// </summary>
        [Fact]
        public void CanonicalDocument_defaults_to_no_geo_polygons()
        {
            var doc = CanonicalDocument.CreateMinimal("doc-1", "file-share", new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["Token-B", "TOKEN-A"], DateTimeOffset.UtcNow, new IngestionFileList()), DateTimeOffset.UtcNow);

            doc.GeoPolygons.ShouldNotBeNull();
            doc.GeoPolygons.Count.ShouldBe(0);
            doc.SecurityTokens.ShouldBe(["token-a", "token-b"]);
        }

        /// <summary>
        /// Confirms that multiple geo polygons can be retained without altering the seeded canonical security state.
        /// </summary>
        [Fact]
        public void CanonicalDocument_can_store_multiple_geo_polygons()
        {
            var doc = CanonicalDocument.CreateMinimal("doc-1", "file-share", new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["Token-B", "TOKEN-A"], DateTimeOffset.UtcNow, new IngestionFileList()), DateTimeOffset.UtcNow);

            var polygonA = GeoPolygon.Create(new[]
            {
                GeoCoordinate.Create(0d, 0d),
                GeoCoordinate.Create(1d, 0d),
                GeoCoordinate.Create(1d, 1d),
                GeoCoordinate.Create(0d, 0d)
            });

            var polygonB = GeoPolygon.Create(new[]
            {
                GeoCoordinate.Create(10d, 10d),
                GeoCoordinate.Create(11d, 10d),
                GeoCoordinate.Create(11d, 11d),
                GeoCoordinate.Create(10d, 10d)
            });

            doc.AddGeoPolygons(new[] { polygonA, polygonB });

            doc.GeoPolygons.Count.ShouldBe(2);
            doc.SecurityTokens.ShouldBe(["token-a", "token-b"]);
        }
    }
}
