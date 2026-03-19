using System.Text.Json.Serialization;

namespace UKHO.Search.Infrastructure.Ingestion.Elastic
{
    internal sealed class GeoJsonMultiPolygonShape
    {
        [JsonPropertyName("type")]
        public required string Type { get; init; }

        [JsonPropertyName("coordinates")]
        public required double[][][][] Coordinates { get; init; }
    }
}
