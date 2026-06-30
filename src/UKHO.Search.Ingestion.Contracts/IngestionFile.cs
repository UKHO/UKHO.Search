using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion.Contracts
{
    /// <summary>
    /// Describes one file entry attached to an indexing payload.
    /// </summary>
    public sealed record IngestionFile : IJsonOnDeserialized
    {
        /// <summary>
        /// Initializes a validated file entry.
        /// </summary>
        /// <param name="filename">
        /// The file name supplied by the producer.
        /// </param>
        /// <param name="size">
        /// The file size in bytes.
        /// </param>
        /// <param name="timestamp">
        /// The timestamp associated with the file entry.
        /// </param>
        /// <param name="mimeType">
        /// The MIME type describing the file content.
        /// </param>
        public IngestionFile(string filename, long size, DateTimeOffset timestamp, string mimeType)
        {
            Filename = filename;
            Size = size;
            Timestamp = timestamp;
            MimeType = mimeType;

            Validate();
        }

        /// <summary>
        /// Initializes an empty instance for JSON deserialization.
        /// </summary>
        public IngestionFile()
        {
        }

        /// <summary>
        /// Gets the file name.
        /// </summary>
        [JsonPropertyName("Filename")]
        [JsonRequired]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Filename { get; init; } = string.Empty;

        /// <summary>
        /// Gets the file size in bytes.
        /// </summary>
        [JsonPropertyName("Size")]
        [JsonRequired]
        public long Size { get; init; }

        /// <summary>
        /// Gets the timestamp associated with the file.
        /// </summary>
        [JsonPropertyName("Timestamp")]
        [JsonRequired]
        public DateTimeOffset Timestamp { get; init; }

        /// <summary>
        /// Gets the MIME type for the file.
        /// </summary>
        [JsonPropertyName("MimeType")]
        [JsonRequired]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string MimeType { get; init; } = string.Empty;

        /// <summary>
        /// Re-validates the file after JSON deserialization has populated its properties.
        /// </summary>
        public void OnDeserialized()
        {
            Validate();
        }

        /// <summary>
        /// Validates the file contract state.
        /// </summary>
        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(Filename))
            {
                throw new JsonException("IngestionFile.Filename is required.");
            }

            if (Size < 0)
            {
                throw new JsonException("IngestionFile.Size must be >= 0.");
            }

            if (string.IsNullOrWhiteSpace(MimeType))
            {
                throw new JsonException("IngestionFile.MimeType is required.");
            }
        }
    }
}