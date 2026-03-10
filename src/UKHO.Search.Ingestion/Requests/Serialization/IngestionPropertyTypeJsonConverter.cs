using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion.Requests.Serialization
{
    public sealed class IngestionPropertyTypeJsonConverter : JsonConverter<IngestionPropertyType>
    {
        public override IngestionPropertyType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("IngestionPropertyType must be a JSON string.");
            }

            var token = reader.GetString();
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new JsonException("IngestionPropertyType cannot be null/empty.");
            }

            return token switch
            {
                "string" => IngestionPropertyType.String,
                "text" => IngestionPropertyType.Text,
                "integer" => IngestionPropertyType.Integer,
                "double" => IngestionPropertyType.Double,
                "decimal" => IngestionPropertyType.Decimal,
                "boolean" => IngestionPropertyType.Boolean,
                "datetime" => IngestionPropertyType.DateTime,
                "timespan" => IngestionPropertyType.TimeSpan,
                "guid" => IngestionPropertyType.Guid,
                "uri" => IngestionPropertyType.Uri,
                "string-array" => IngestionPropertyType.StringArray,
                var _ => throw new JsonException($"Unsupported IngestionPropertyType '{token}'.")
            };
        }

        public override void Write(Utf8JsonWriter writer, IngestionPropertyType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value switch
            {
                IngestionPropertyType.String => "string",
                IngestionPropertyType.Text => "text",
                IngestionPropertyType.Integer => "integer",
                IngestionPropertyType.Double => "double",
                IngestionPropertyType.Decimal => "decimal",
                IngestionPropertyType.Boolean => "boolean",
                IngestionPropertyType.DateTime => "datetime",
                IngestionPropertyType.TimeSpan => "timespan",
                IngestionPropertyType.Guid => "guid",
                IngestionPropertyType.Uri => "uri",
                IngestionPropertyType.StringArray => "string-array",
                var _ => throw new JsonException($"Unsupported IngestionPropertyType '{value}'.")
            });
        }
    }
}