using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion.Contracts
{
    /// <summary>
    /// Represents a queue-message payload that deletes a previously indexed document.
    /// </summary>
    public sealed record DeleteItemRequest
    {
        /// <summary>
        /// Initializes a validated delete payload.
        /// </summary>
        /// <param name="id">
        /// The identifier of the document to delete.
        /// </param>
        [JsonConstructor]
        public DeleteItemRequest(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new JsonException("DeleteItemRequest.Id is required.");
            }

            Id = id;
        }

        /// <summary>
        /// Initializes an empty instance for JSON deserialization.
        /// </summary>
        public DeleteItemRequest()
        {
        }

        /// <summary>
        /// Gets the identifier of the document to delete.
        /// </summary>
        [JsonPropertyName("Id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Id { get; init; } = string.Empty;
    }
}