using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion.Requests
{
    public sealed record AddItemRequest : IJsonOnDeserialized
    {
        public AddItemRequest(string id, IReadOnlyList<IngestionProperty> properties, string[] securityTokens, DateTimeOffset timestamp, IngestionFileList files)
        {
            Id = id;
            Properties = properties;
            SecurityTokens = securityTokens;
            Timestamp = timestamp;
            Files = files;

            Validate();
        }

        public AddItemRequest()
        {
        }

        [JsonPropertyName("Id")]
        [JsonRequired]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("Properties")]
        [JsonRequired]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<IngestionProperty> Properties { get; init; } = Array.Empty<IngestionProperty>();

        [JsonPropertyName("SecurityTokens")]
        [JsonRequired]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[] SecurityTokens { get; init; } = Array.Empty<string>();

        [JsonPropertyName("Timestamp")]
        [JsonRequired]
        public DateTimeOffset Timestamp { get; init; }

        [JsonPropertyName("Files")]
        [JsonRequired]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IngestionFileList Files { get; init; } = new();

        public void OnDeserialized()
        {
            Validate();
        }

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                throw new JsonException("AddItemRequest.Id is required.");
            }

            if (Properties is null)
            {
                throw new JsonException("AddItemRequest.Properties cannot be null.");
            }

            if (SecurityTokens is null || SecurityTokens.Length == 0)
            {
                throw new JsonException("AddItemRequest.SecurityTokens is required and must be non-empty.");
            }

            if (SecurityTokens.Any(string.IsNullOrWhiteSpace))
            {
                throw new JsonException("AddItemRequest.SecurityTokens cannot contain null/empty tokens.");
            }

            if (Files is null)
            {
                throw new JsonException("AddItemRequest.Files cannot be null.");
            }

            if (Files.Any(f => f is null))
            {
                throw new JsonException("AddItemRequest.Files cannot contain null entries.");
            }

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
    }
}