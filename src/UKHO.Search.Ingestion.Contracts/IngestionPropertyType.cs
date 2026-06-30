using System.Text.Json.Serialization;
using UKHO.Search.Ingestion.Contracts.Serialization;

namespace UKHO.Search.Ingestion.Contracts
{
    /// <summary>
    /// Describes the typed value carried by an ingestion property within the queue-message contract.
    /// </summary>
    [JsonConverter(typeof(IngestionPropertyTypeJsonConverter))]
    public enum IngestionPropertyType
    {
        /// <summary>
        /// A short string value.
        /// </summary>
        String,

        /// <summary>
        /// A text value intended for human-readable content.
        /// </summary>
        Text,

        /// <summary>
        /// A 64-bit integer value.
        /// </summary>
        Integer,

        /// <summary>
        /// A double-precision floating-point value.
        /// </summary>
        Double,

        /// <summary>
        /// A decimal numeric value.
        /// </summary>
        Decimal,

        /// <summary>
        /// A Boolean value.
        /// </summary>
        Boolean,

        /// <summary>
        /// A date/time value serialized as a round-trip string.
        /// </summary>
        DateTime,

        /// <summary>
        /// A duration value serialized in constant format.
        /// </summary>
        TimeSpan,

        /// <summary>
        /// A GUID value.
        /// </summary>
        Guid,

        /// <summary>
        /// An absolute URI value.
        /// </summary>
        Uri,

        /// <summary>
        /// An array of strings.
        /// </summary>
        StringArray
    }
}