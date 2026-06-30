using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion.Contracts
{
    /// <summary>
    /// Identifies which queue-message payload is active within an ingestion request envelope.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum IngestionRequestType
    {
        /// <summary>
        /// Indicates that the envelope contains an indexing payload.
        /// </summary>
        IndexItem,

        /// <summary>
        /// Indicates that the envelope contains a delete payload.
        /// </summary>
        DeleteItem,

        /// <summary>
        /// Indicates that the envelope contains an ACL update payload.
        /// </summary>
        UpdateAcl
    }
}