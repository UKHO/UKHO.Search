using UKHO.Search.Geo;

namespace UKHO.Search.Infrastructure.Ingestion.Elastic
{
    internal static class GeoJsonPolygonShapeMapper
    {
        public static object? Map(IReadOnlyList<GeoPolygon> polygons)
        {
            ArgumentNullException.ThrowIfNull(polygons);

            if (polygons.Count == 0)
            {
                return null;
            }

            if (polygons.Count == 1)
            {
                return new GeoJsonPolygonShape
                {
                    Type = "Polygon",
                    Coordinates = MapPolygon(polygons[0])
                };
            }

            return new GeoJsonMultiPolygonShape
            {
                Type = "MultiPolygon",
                Coordinates = polygons.Select(MapPolygon)
                                      .ToArray()
            };
        }

        private static double[][][] MapPolygon(GeoPolygon polygon)
        {
            return polygon.Rings
                          .Select(MapRing)
                          .ToArray();
        }

        private static double[][] MapRing(IReadOnlyList<GeoCoordinate> ring)
        {
            return ring.Select(point => new[] { point.Longitude, point.Latitude })
                       .ToArray();
        }
    }
}
