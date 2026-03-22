using System.Text.Json;
using Shouldly;
using UKHO.Search.Geo;
using UKHO.Search.Infrastructure.Ingestion.Elastic;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Elastic
{
    public sealed class GeoJsonPolygonShapeMapperTests
    {
        [Fact]
        public void Map_WhenDocumentContainsSinglePolygon_ProducesGeoJsonPolygonWithLongitudeLatitudeCoordinates()
        {
            var document = CreateMinimalDocument("doc-1");
            document.AddGeoPolygon(GeoPolygon.Create(new[]
            {
                GeoCoordinate.Create(1d, 2d),
                GeoCoordinate.Create(3d, 2d),
                GeoCoordinate.Create(3d, 4d),
                GeoCoordinate.Create(1d, 2d)
            }));

            var indexDocument = ElasticsearchBulkIndexClient.CreateIndexDocument(document);

            indexDocument.GeoPolygons.ShouldNotBeNull();
            var shape = indexDocument.GeoPolygons.ShouldBeOfType<GeoJsonPolygonShape>();
            shape.Type.ShouldBe("Polygon");
            shape.Coordinates.Length.ShouldBe(1);
            shape.Coordinates[0].Length.ShouldBe(4);
            shape.Coordinates[0][0].ShouldBe([1d, 2d]);
            shape.Coordinates[0][1].ShouldBe([3d, 2d]);
            shape.Coordinates[0][2].ShouldBe([3d, 4d]);
            shape.Coordinates[0][3].ShouldBe([1d, 2d]);
        }

        [Fact]
        public void Map_WhenDocumentContainsMultiplePolygons_ProducesGeoJsonMultiPolygonWithStablePolygonAndRingNesting()
        {
            var document = CreateMinimalDocument("doc-1");
            document.AddGeoPolygon(GeoPolygon.Create(new[]
            {
                GeoCoordinate.Create(1d, 2d),
                GeoCoordinate.Create(3d, 2d),
                GeoCoordinate.Create(3d, 4d),
                GeoCoordinate.Create(1d, 2d)
            }));
            document.AddGeoPolygon(GeoPolygon.Create(new[]
            {
                new[]
                {
                    GeoCoordinate.Create(10d, 20d),
                    GeoCoordinate.Create(30d, 20d),
                    GeoCoordinate.Create(30d, 40d),
                    GeoCoordinate.Create(10d, 20d)
                },
                new[]
                {
                    GeoCoordinate.Create(11d, 21d),
                    GeoCoordinate.Create(12d, 21d),
                    GeoCoordinate.Create(12d, 22d),
                    GeoCoordinate.Create(11d, 21d)
                }
            }));

            var indexDocument = ElasticsearchBulkIndexClient.CreateIndexDocument(document);

            indexDocument.GeoPolygons.ShouldNotBeNull();
            var shape = indexDocument.GeoPolygons.ShouldBeOfType<GeoJsonMultiPolygonShape>();
            shape.Type.ShouldBe("MultiPolygon");
            shape.Coordinates.Length.ShouldBe(2);
            shape.Coordinates[0].Length.ShouldBe(1);
            shape.Coordinates[0][0][0].ShouldBe([1d, 2d]);
            shape.Coordinates[1].Length.ShouldBe(2);
            shape.Coordinates[1][0][0].ShouldBe([10d, 20d]);
            shape.Coordinates[1][1][0].ShouldBe([11d, 21d]);
        }

        [Fact]
        public void Map_WhenDocumentContainsNoPolygons_OmitsGeoShapePayload()
        {
            var document = CreateMinimalDocument("doc-1");

            var indexDocument = ElasticsearchBulkIndexClient.CreateIndexDocument(document);
            var json = JsonSerializer.Serialize(indexDocument);

            indexDocument.GeoPolygons.ShouldBeNull();
            json.ShouldNotContain("geoPolygons");
        }

        [Fact]
        public void Map_WhenDocumentContainsSinglePolygon_DoesNotEmitDomainShapeProperties()
        {
            var document = CreateMinimalDocument("doc-1");
            document.AddGeoPolygon(GeoPolygon.Create(new[]
            {
                GeoCoordinate.Create(1d, 2d),
                GeoCoordinate.Create(3d, 2d),
                GeoCoordinate.Create(3d, 4d),
                GeoCoordinate.Create(1d, 2d)
            }));

            var indexDocument = ElasticsearchBulkIndexClient.CreateIndexDocument(document);
            var json = JsonSerializer.Serialize(indexDocument);

            json.ShouldContain("\"geoPolygons\"");
            json.ShouldContain("\"type\":\"Polygon\"");
            json.ShouldContain("\"coordinates\"");
            json.ShouldNotContain("\"rings\"");
            json.ShouldNotContain("\"longitude\"");
            json.ShouldNotContain("\"latitude\"");
        }

        private static CanonicalDocument CreateMinimalDocument(string documentId)
        {
            return CanonicalDocument.CreateMinimal(documentId, "file-share", new IndexRequest(documentId, Array.Empty<IngestionProperty>(), ["t1"], DateTimeOffset.UnixEpoch, new IngestionFileList()), DateTimeOffset.UnixEpoch);
        }
    }
}
