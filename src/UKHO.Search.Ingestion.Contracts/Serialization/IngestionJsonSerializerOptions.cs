using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion.Contracts.Serialization
{
    /// <summary>
    /// Creates the canonical <see cref="JsonSerializerOptions" /> instance for the ingestion queue-message contract.
    /// </summary>
    public static class IngestionJsonSerializerOptions
    {
        /// <summary>
        /// Creates serializer options configured for the ingestion queue-message wire contract.
        /// </summary>
        /// <returns>
        /// A configured <see cref="JsonSerializerOptions" /> instance.
        /// </returns>
        public static JsonSerializerOptions Create()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // Register the contract-specific converters that preserve the existing property token and typed value behavior.
            options.Converters.Add(new IngestionPropertyTypeJsonConverter());
            options.Converters.Add(new IngestionPropertyJsonConverter());
            options.Converters.Add(new IngestionPropertyListJsonConverter());

            return options;
        }
    }
}