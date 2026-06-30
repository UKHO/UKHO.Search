using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion.Contracts.Serialization
{
    /// <summary>
    /// Converts the custom property-list wrapper to and from its JSON array representation.
    /// </summary>
    public sealed class IngestionPropertyListJsonConverter : JsonConverter<IngestionPropertyList>
    {
        /// <summary>
        /// Reads a property-list wrapper from a JSON array.
        /// </summary>
        public override IngestionPropertyList? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var items = JsonSerializer.Deserialize<List<IngestionProperty>>(ref reader, options);
            if (items is null)
            {
                return null;
            }

            return new IngestionPropertyList(items);
        }

        /// <summary>
        /// Writes the property-list wrapper as a JSON array.
        /// </summary>
        public override void Write(Utf8JsonWriter writer, IngestionPropertyList value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value.ToArray(), options);
        }
    }
}