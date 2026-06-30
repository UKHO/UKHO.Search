using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion.Contracts
{
    /// <summary>
    /// Represents a queue-message payload that updates canonical security-token state.
    /// </summary>
    public sealed record UpdateAclRequest
    {
        /// <summary>
        /// Initializes a validated ACL update payload.
        /// </summary>
        /// <param name="id">
        /// The identifier of the document whose security tokens should be updated.
        /// </param>
        /// <param name="securityTokens">
        /// The non-empty set of security tokens to apply.
        /// </param>
        [JsonConstructor]
        public UpdateAclRequest(string id, string[] securityTokens)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new JsonException("UpdateAclRequest.Id is required.");
            }

            if (securityTokens is null || securityTokens.Length == 0)
            {
                throw new JsonException("UpdateAclRequest.SecurityTokens is required and must be non-empty.");
            }

            if (securityTokens.Any(string.IsNullOrWhiteSpace))
            {
                throw new JsonException("UpdateAclRequest.SecurityTokens cannot contain null/empty tokens.");
            }

            Id = id;
            SecurityTokens = securityTokens;
        }

        /// <summary>
        /// Initializes an empty instance for JSON deserialization.
        /// </summary>
        public UpdateAclRequest()
        {
        }

        /// <summary>
        /// Gets the identifier of the document whose ACL should be updated.
        /// </summary>
        [JsonPropertyName("Id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Id { get; init; } = string.Empty;

        /// <summary>
        /// Gets the replacement security-token set.
        /// </summary>
        [JsonPropertyName("SecurityTokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[] SecurityTokens { get; init; } = Array.Empty<string>();
    }
}