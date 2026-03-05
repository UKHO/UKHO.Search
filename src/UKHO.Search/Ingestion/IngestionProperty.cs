using System.Text.Json.Serialization;
using UKHO.Search.Ingestion.Serialization;

namespace UKHO.Search.Ingestion;

[JsonConverter(typeof(IngestionPropertyJsonConverter))]
public sealed record IngestionProperty
{
    public string Name { get; init; } = string.Empty;

    public IngestionPropertyType Type { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Value { get; init; }
}
