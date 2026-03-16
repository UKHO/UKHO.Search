using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion.Requests.Serialization
{
    public sealed class IngestionPropertyListJsonConverter : JsonConverter<IngestionPropertyList>
    {
        public override IngestionPropertyList? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var items = JsonSerializer.Deserialize<List<IngestionProperty>>(ref reader, options);
            if (items is null)
            {
                return null;
            }

            return new IngestionPropertyList(items);
        }

        public override void Write(Utf8JsonWriter writer, IngestionPropertyList value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value.ToArray(), options);
        }
    }
}
