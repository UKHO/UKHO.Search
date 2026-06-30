using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion.Contracts
{
    /// <summary>
    /// Represents the queue-message payload used to index or update a document.
    /// </summary>
    public sealed record IndexRequest : IJsonOnDeserialized
    {
        /// <summary>
        /// Initializes a validated indexing payload from a simple property sequence.
        /// </summary>
        /// <param name="id">
        /// The identifier of the document being indexed.
        /// </param>
        /// <param name="properties">
        /// The ingestion properties attached to the payload.
        /// </param>
        /// <param name="securityTokens">
        /// The non-empty security-token set supplied by the producer.
        /// </param>
        /// <param name="timestamp">
        /// The timestamp associated with the indexing payload.
        /// </param>
        /// <param name="files">
        /// The file collection carried by the payload.
        /// </param>
        public IndexRequest(string id, IReadOnlyList<IngestionProperty> properties, string[] securityTokens, DateTimeOffset timestamp, IngestionFileList files)
            : this(id, new IngestionPropertyList(properties), securityTokens, timestamp, files)
        {
        }

        /// <summary>
        /// Initializes a validated indexing payload.
        /// </summary>
        /// <param name="id">
        /// The identifier of the document being indexed.
        /// </param>
        /// <param name="properties">
        /// The canonical property list attached to the payload.
        /// </param>
        /// <param name="securityTokens">
        /// The non-empty security-token set supplied by the producer.
        /// </param>
        /// <param name="timestamp">
        /// The timestamp associated with the indexing payload.
        /// </param>
        /// <param name="files">
        /// The file collection carried by the payload.
        /// </param>
        public IndexRequest(string id, IngestionPropertyList properties, string[] securityTokens, DateTimeOffset timestamp, IngestionFileList files)
        {
            Id = id;
            Properties = properties;
            SecurityTokens = securityTokens;
            Timestamp = timestamp;
            Files = files;

            Validate();
        }

        /// <summary>
        /// Initializes an empty instance for JSON deserialization.
        /// </summary>
        public IndexRequest()
        {
        }

        /// <summary>
        /// Gets the identifier of the document being indexed.
        /// </summary>
        [JsonPropertyName("Id")]
        [JsonRequired]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Id { get; init; } = string.Empty;

        /// <summary>
        /// Gets the ingestion properties attached to the payload.
        /// </summary>
        [JsonPropertyName("Properties")]
        [JsonRequired]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IngestionPropertyList Properties { get; init; } = new();

        /// <summary>
        /// Gets the security tokens supplied by the producer.
        /// </summary>
        [JsonPropertyName("SecurityTokens")]
        [JsonRequired]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[] SecurityTokens { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the timestamp associated with the payload.
        /// </summary>
        [JsonPropertyName("Timestamp")]
        [JsonRequired]
        public DateTimeOffset Timestamp { get; init; }

        /// <summary>
        /// Gets the file entries attached to the payload.
        /// </summary>
        [JsonPropertyName("Files")]
        [JsonRequired]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IngestionFileList Files { get; init; } = new();

        /// <summary>
        /// Re-validates the payload after JSON deserialization has populated its properties.
        /// </summary>
        public void OnDeserialized()
        {
            Validate();
        }

        /// <summary>
        /// Validates the indexing payload according to the queue-message contract.
        /// </summary>
        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                throw new JsonException("IndexRequest.Id is required.");
            }

            if (Properties is null)
            {
                throw new JsonException("IndexRequest.Properties cannot be null.");
            }

            if (SecurityTokens is null || SecurityTokens.Length == 0)
            {
                throw new JsonException("IndexRequest.SecurityTokens is required and must be non-empty.");
            }

            if (SecurityTokens.Any(string.IsNullOrWhiteSpace))
            {
                throw new JsonException("IndexRequest.SecurityTokens cannot contain null/empty tokens.");
            }

            if (Files is null)
            {
                throw new JsonException("IndexRequest.Files cannot be null.");
            }

            if (Files.Any(file => file is null))
            {
                throw new JsonException("IndexRequest.Files cannot contain null entries.");
            }

            // The first-class Id field must not be duplicated inside the property bag.
            if (Properties.Any(property => string.Equals(property.Name, "Id", StringComparison.OrdinalIgnoreCase)))
            {
                throw new JsonException("IndexRequest.Properties cannot contain an IngestionProperty named 'Id'. Id is a first-class property.");
            }
        }
    }
}