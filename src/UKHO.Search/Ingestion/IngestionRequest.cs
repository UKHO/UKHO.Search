using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion;

public sealed record IngestionRequest
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<IngestionProperty> Properties { get; init; } = Array.Empty<IngestionProperty>();

    public Uri DataCallback { get; init; } = new("https://example.invalid");

    [JsonConstructor]
    public IngestionRequest(IReadOnlyList<IngestionProperty> properties, Uri dataCallback)
    {
        Properties = properties ?? throw new JsonException("IngestionRequest.Properties cannot be null.");
        DataCallback = dataCallback ?? throw new JsonException("IngestionRequest.DataCallback is required.");

        if (!DataCallback.IsAbsoluteUri)
        {
            throw new JsonException("IngestionRequest.DataCallback must be an absolute URI.");
        }

        var duplicates = Properties
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicates is not null)
        {
            throw new JsonException($"IngestionRequest.Properties contains duplicate Name '{duplicates.Key}'. Names are case-insensitive.");
        }
    }

    public IngestionRequest()
    {
    }
}
