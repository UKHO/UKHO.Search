using System.Text.Json.Serialization;
using UKHO.Search.Ingestion.Requests.Serialization;

namespace UKHO.Search.Ingestion.Requests
{
    [JsonConverter(typeof(IngestionPropertyJsonConverter))]
    public sealed record IngestionProperty
    {
        public string Name { get; init; } = string.Empty;

        public IngestionPropertyType Type { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Value { get; init; }
    }
}