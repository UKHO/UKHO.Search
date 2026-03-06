using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion.Requests
{
    public sealed record IngestionRequest
    {
        [JsonConstructor]
        public IngestionRequest(IReadOnlyList<IngestionProperty> properties)
        {
            Properties = properties ?? throw new JsonException("IngestionRequest.Properties cannot be null.");

            var duplicates = Properties
                .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicates is not null)
                throw new JsonException(
                    $"IngestionRequest.Properties contains duplicate Name '{duplicates.Key}'. Names are case-insensitive.");
        }

        public IngestionRequest()
        {
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<IngestionProperty> Properties { get; init; } = Array.Empty<IngestionProperty>();
    }
}