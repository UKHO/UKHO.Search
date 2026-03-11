using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion.Requests
{
    public sealed record IngestionFile : IJsonOnDeserialized
    {
        public IngestionFile(string filename, long size, DateTimeOffset timestamp, string mimeType)
        {
            Filename = filename;
            Size = size;
            Timestamp = timestamp;
            MimeType = mimeType;

            Validate();
        }

        public IngestionFile()
        {
        }

        [JsonPropertyName("Filename")]
        [JsonRequired]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Filename { get; init; } = string.Empty;

        [JsonPropertyName("Size")]
        [JsonRequired]
        public long Size { get; init; }

        [JsonPropertyName("Timestamp")]
        [JsonRequired]
        public DateTimeOffset Timestamp { get; init; }

        [JsonPropertyName("MimeType")]
        [JsonRequired]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string MimeType { get; init; } = string.Empty;

        public void OnDeserialized()
        {
            Validate();
        }

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