using System.Text.Json;

namespace UKHO.Search.Ingestion.Contracts.Serialization
{
    /// <summary>
    /// Provides package-owned serialization entry points for the ingestion queue-message contract.
    /// </summary>
    public static class IngestionContractSerializer
    {
        private static readonly JsonSerializerOptions _serializerOptions = IngestionJsonSerializerOptions.Create();

        /// <summary>
        /// Serializes an ingestion request envelope using the canonical queue-message serializer settings.
        /// </summary>
        /// <param name="request">
        /// The ingestion request envelope to serialize.
        /// </param>
        /// <returns>
        /// The canonical JSON representation of the supplied envelope.
        /// </returns>
        public static string Serialize(IngestionRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            // Always serialize through the package-owned options so callers cannot accidentally omit required converters.
            return JsonSerializer.Serialize(request, _serializerOptions);
        }

        /// <summary>
        /// Deserializes an ingestion request envelope using the canonical queue-message serializer settings.
        /// </summary>
        /// <param name="json">
        /// The JSON payload to deserialize.
        /// </param>
        /// <returns>
        /// The validated ingestion request envelope represented by the JSON payload.
        /// </returns>
        public static IngestionRequest DeserializeIngestionRequest(string json)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(json);

            // Use the same canonical serializer path that runtime and tests rely on so helper behavior matches the wire contract.
            var request = JsonSerializer.Deserialize<IngestionRequest>(json, _serializerOptions);
            if (request is null)
            {
                throw new JsonException("JSON payload could not be deserialized to IngestionRequest.");
            }

            return request;
        }
    }
}