using System.Text.Json.Serialization;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Infrastructure.Ingestion.Elastic
{
    internal sealed class CanonicalIndexDocument
    {
        [JsonPropertyName("id")]
        public required string Id { get; init; }

        [JsonPropertyName("source")]
        public required IndexRequest Source { get; init; }

        [JsonPropertyName("provider")]
        public required string Provider { get; init; }

        [JsonPropertyName("timestamp")]
        public required DateTimeOffset Timestamp { get; init; }

        [JsonPropertyName("keywords")]
        public required IReadOnlyCollection<string> Keywords { get; init; }

        [JsonPropertyName("authority")]
        public required IReadOnlyCollection<string> Authority { get; init; }

        [JsonPropertyName("region")]
        public required IReadOnlyCollection<string> Region { get; init; }

        [JsonPropertyName("format")]
        public required IReadOnlyCollection<string> Format { get; init; }

        [JsonPropertyName("majorVersion")]
        public required IReadOnlyCollection<int> MajorVersion { get; init; }

        [JsonPropertyName("minorVersion")]
        public required IReadOnlyCollection<int> MinorVersion { get; init; }

        [JsonPropertyName("category")]
        public required IReadOnlyCollection<string> Category { get; init; }

        [JsonPropertyName("series")]
        public required IReadOnlyCollection<string> Series { get; init; }

        [JsonPropertyName("instance")]
        public required IReadOnlyCollection<string> Instance { get; init; }

        [JsonPropertyName("searchText")]
        public required string SearchText { get; init; }

        [JsonPropertyName("content")]
        public required string Content { get; init; }

        [JsonPropertyName("geoPolygons")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? GeoPolygons { get; init; }

        public static CanonicalIndexDocument Create(CanonicalDocument document)
        {
            ArgumentNullException.ThrowIfNull(document);

            return new CanonicalIndexDocument
            {
                Id = document.Id,
                Source = document.Source,
                Provider = document.Provider,
                Timestamp = document.Timestamp,
                Keywords = document.Keywords.ToArray(),
                Authority = document.Authority.ToArray(),
                Region = document.Region.ToArray(),
                Format = document.Format.ToArray(),
                MajorVersion = document.MajorVersion.ToArray(),
                MinorVersion = document.MinorVersion.ToArray(),
                Category = document.Category.ToArray(),
                Series = document.Series.ToArray(),
                Instance = document.Instance.ToArray(),
                SearchText = document.SearchText,
                Content = document.Content,
                GeoPolygons = GeoJsonPolygonShapeMapper.Map(document.GeoPolygons)
            };
        }
    }
}
