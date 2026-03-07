using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion.Requests
{
    public sealed record UpdateAclRequest
    {
        [JsonConstructor]
        public UpdateAclRequest(string id, string[] securityTokens)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new JsonException("UpdateAclRequest.Id is required.");

            if (securityTokens is null || securityTokens.Length == 0)
                throw new JsonException("UpdateAclRequest.SecurityTokens is required and must be non-empty.");

            if (securityTokens.Any(string.IsNullOrWhiteSpace))
                throw new JsonException("UpdateAclRequest.SecurityTokens cannot contain null/empty tokens.");

            Id = id;
            SecurityTokens = securityTokens;
        }

        public UpdateAclRequest()
        {
        }

        [JsonPropertyName("Id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("SecurityTokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[] SecurityTokens { get; init; } = Array.Empty<string>();
    }
}
