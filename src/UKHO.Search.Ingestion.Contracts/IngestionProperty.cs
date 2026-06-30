using System.Text.Json.Serialization;
using UKHO.Search.Ingestion.Contracts.Serialization;

namespace UKHO.Search.Ingestion.Contracts
{
    /// <summary>
    /// Represents one typed name/value property inside an indexing payload.
    /// </summary>
    [JsonConverter(typeof(IngestionPropertyJsonConverter))]
    public sealed record IngestionProperty
    {
        /// <summary>
        /// Gets the property name used by rules, enrichers, and payload-path evaluation.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets the declared property type that controls JSON parsing and typed access.
        /// </summary>
        public IngestionPropertyType Type { get; init; }

        /// <summary>
        /// Gets the typed property value.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Value { get; init; }
    }
}