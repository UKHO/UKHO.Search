using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion.Requests
{
    public sealed record AddItemRequest
    {
        [JsonConstructor]
        public AddItemRequest(string id, IReadOnlyList<IngestionProperty> properties, string[] securityTokens)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new JsonException("AddItemRequest.Id is required.");
            }

            Properties = properties ?? throw new JsonException("AddItemRequest.Properties cannot be null.");

            if (securityTokens is null || securityTokens.Length == 0)
            {
                throw new JsonException("AddItemRequest.SecurityTokens is required and must be non-empty.");
            }

            if (securityTokens.Any(string.IsNullOrWhiteSpace))
            {
                throw new JsonException("AddItemRequest.SecurityTokens cannot contain null/empty tokens.");
            }

            Id = id;
            SecurityTokens = securityTokens;

            if (Properties.Any(p => string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase)))
            {
                throw new JsonException("AddItemRequest.Properties cannot contain an IngestionProperty named 'Id'. Id is a first-class property.");
            }

            var duplicates = Properties.Where(p => !string.IsNullOrWhiteSpace(p.Name))
                                       .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                                       .FirstOrDefault(g => g.Count() > 1);

            if (duplicates is not null)
            {
                throw new JsonException($"AddItemRequest.Properties contains duplicate Name '{duplicates.Key}'. Names are case-insensitive.");
            }
        }

        public AddItemRequest()
        {
        }

        [JsonPropertyName("Id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("Properties")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<IngestionProperty> Properties { get; init; } = Array.Empty<IngestionProperty>();

        [JsonPropertyName("SecurityTokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[] SecurityTokens { get; init; } = Array.Empty<string>();
    }
}