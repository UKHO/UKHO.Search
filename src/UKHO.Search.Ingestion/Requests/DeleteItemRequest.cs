using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion.Requests
{
    public sealed record DeleteItemRequest
    {
        [JsonConstructor]
        public DeleteItemRequest(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new JsonException("DeleteItemRequest.Id is required.");
            }

            Id = id;
        }

        public DeleteItemRequest()
        {
        }

        [JsonPropertyName("Id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Id { get; init; } = string.Empty;
    }
}