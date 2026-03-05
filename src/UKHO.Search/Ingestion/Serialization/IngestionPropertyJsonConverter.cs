using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion.Serialization;

public sealed class IngestionPropertyJsonConverter : JsonConverter<IngestionProperty>
{
    public override IngestionProperty Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("IngestionProperty must be a JSON object.");
        }

        string? name = null;
        IngestionPropertyType? type = null;
        object? value = null;
        var valueRead = false;
        JsonElement? rawValue = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Invalid JSON token while reading IngestionProperty.");
            }

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "Name":
                    name = reader.TokenType == JsonTokenType.String
                        ? reader.GetString()
                        : throw new JsonException("IngestionProperty.Name must be a string.");
                    break;

                case "Type":
                    type = JsonSerializer.Deserialize<IngestionPropertyType>(ref reader, options);
                    break;

                case "Value":
                    valueRead = true;
                    rawValue = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
                    break;

                default:
                    reader.Skip();
                    break;
            }
        }

        if (valueRead)
        {
            if (rawValue is null)
            {
                throw new JsonException("IngestionProperty.Value is required.");
            }

            value = ParseFromJsonElement(rawValue.Value, type);
        }

        Validate(name, type, valueRead, value);

        return new IngestionProperty
        {
            Name = name!,
            Type = type!.Value,
            Value = value,
        };
    }

    public override void Write(Utf8JsonWriter writer, IngestionProperty value, JsonSerializerOptions options)
    {
        Validate(value.Name, value.Type, valueRead: true, value.Value);

        writer.WriteStartObject();
        writer.WriteString("Name", value.Name);
        writer.WritePropertyName("Value");
        WriteValue(writer, value.Type, value.Value!, options);
        writer.WritePropertyName("Type");
        JsonSerializer.Serialize(writer, value.Type, options);
        writer.WriteEndObject();
    }

    private static void Validate(string? name, IngestionPropertyType? type, bool valueRead, object? value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new JsonException("IngestionProperty.Name is required.");
        }

        if (type is null)
        {
            throw new JsonException("IngestionProperty.Type is required.");
        }

        if (!valueRead)
        {
            throw new JsonException("IngestionProperty.Value is required.");
        }

        if (value is null)
        {
            throw new JsonException("IngestionProperty.Value cannot be null.");
        }

        if (type == IngestionPropertyType.Uri)
        {
            if (value is not Uri uri || !uri.IsAbsoluteUri)
            {
                throw new JsonException("IngestionProperty.Value must be an absolute URI when Type is 'uri'.");
            }
        }

        if (type == IngestionPropertyType.StringArray)
        {
            if (value is not string[] values || values.Any(v => v is null))
            {
                throw new JsonException("IngestionProperty.Value must be an array of strings (no null elements) when Type is 'string-array'.");
            }
        }
    }

    private static void WriteValue(Utf8JsonWriter writer, IngestionPropertyType type, object value, JsonSerializerOptions options)
    {
        switch (type)
        {
            case IngestionPropertyType.String:
            case IngestionPropertyType.Id:
                if (value is not string s)
                {
                    throw new JsonException($"Value must be a string for Type '{type}'.");
                }

                writer.WriteStringValue(s);
                return;

            case IngestionPropertyType.Integer:
                writer.WriteNumberValue(value is long l ? l : throw new JsonException("Value must be an Int64 for Type 'integer'."));
                return;

            case IngestionPropertyType.Double:
                writer.WriteNumberValue(value is double d ? d : throw new JsonException("Value must be a Double for Type 'double'."));
                return;

            case IngestionPropertyType.Decimal:
                writer.WriteNumberValue(value is decimal dec ? dec : throw new JsonException("Value must be a Decimal for Type 'decimal'."));
                return;

            case IngestionPropertyType.Boolean:
                writer.WriteBooleanValue(value is bool b ? b : throw new JsonException("Value must be a Boolean for Type 'boolean'."));
                return;

            case IngestionPropertyType.DateTime:
                writer.WriteStringValue(value is DateTimeOffset dto
                    ? dto.ToString("O", CultureInfo.InvariantCulture)
                    : throw new JsonException("Value must be a DateTimeOffset for Type 'datetime'."));
                return;

            case IngestionPropertyType.TimeSpan:
                writer.WriteStringValue(value is TimeSpan ts
                    ? ts.ToString("c", CultureInfo.InvariantCulture)
                    : throw new JsonException("Value must be a TimeSpan for Type 'timespan'."));
                return;

            case IngestionPropertyType.Guid:
                writer.WriteStringValue(value is Guid g ? g.ToString("D") : throw new JsonException("Value must be a Guid for Type 'guid'."));
                return;

            case IngestionPropertyType.Uri:
                if (value is not Uri uri || !uri.IsAbsoluteUri)
                {
                    throw new JsonException("Value must be an absolute Uri for Type 'uri'.");
                }

                writer.WriteStringValue(uri.AbsoluteUri);
                return;

            case IngestionPropertyType.StringArray:
                if (value is not string[] arr || arr.Any(v => v is null))
                {
                    throw new JsonException("Value must be a string[] (no null elements) for Type 'string-array'.");
                }

                writer.WriteStartArray();
                foreach (var item in arr)
                {
                    writer.WriteStringValue(item);
                }

                writer.WriteEndArray();
                return;

            default:
                throw new JsonException($"Unsupported IngestionPropertyType '{type}'.");
        }
    }

    private static object ParseFromJsonElement(JsonElement element, IngestionPropertyType? type)
    {
        if (type is null)
        {
            throw new JsonException("IngestionProperty.Type is required.");
        }

        return type.Value switch
        {
            IngestionPropertyType.String => element.ValueKind == JsonValueKind.String ? element.GetString()! : throw new JsonException("Value must be a JSON string for Type 'string'."),
            IngestionPropertyType.Id => element.ValueKind == JsonValueKind.String ? element.GetString()! : throw new JsonException("Value must be a JSON string for Type 'id'."),
            IngestionPropertyType.Integer => element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var l) ? l : throw new JsonException("Value must be a JSON number (int64) for Type 'integer'."),
            IngestionPropertyType.Double => element.ValueKind == JsonValueKind.Number ? element.GetDouble() : throw new JsonException("Value must be a JSON number for Type 'double'."),
            IngestionPropertyType.Decimal => element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var dec) ? dec : throw new JsonException("Value must be a JSON number (decimal) for Type 'decimal'."),
            IngestionPropertyType.Boolean => element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False ? element.GetBoolean() : throw new JsonException("Value must be a JSON boolean for Type 'boolean'."),
            IngestionPropertyType.DateTime => element.ValueKind == JsonValueKind.String ? ReadDateTimeOffset(element.GetString()) : throw new JsonException("Value must be a JSON string for Type 'datetime'."),
            IngestionPropertyType.TimeSpan => element.ValueKind == JsonValueKind.String ? ReadTimeSpan(element.GetString()) : throw new JsonException("Value must be a JSON string for Type 'timespan'."),
            IngestionPropertyType.Guid => element.ValueKind == JsonValueKind.String ? ReadGuid(element.GetString()) : throw new JsonException("Value must be a JSON string for Type 'guid'."),
            IngestionPropertyType.Uri => element.ValueKind == JsonValueKind.String ? ReadUri(element.GetString()) : throw new JsonException("Value must be a JSON string for Type 'uri'."),
            IngestionPropertyType.StringArray => element.ValueKind == JsonValueKind.Array ? ReadStringArray(element) : throw new JsonException("Value must be a JSON array for Type 'string-array'."),
            _ => throw new JsonException($"Unsupported IngestionPropertyType '{type}'."),
        };
    }

    private static DateTimeOffset ReadDateTimeOffset(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            throw new JsonException("Value is not a valid ISO 8601/RFC 3339 datetime string for Type 'datetime'.");
        }

        if (!DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
        {
            throw new JsonException("Value is not a valid ISO 8601/RFC 3339 datetime string for Type 'datetime'.");
        }

        return dto;
    }

    private static TimeSpan ReadTimeSpan(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            throw new JsonException("Value is not a valid TimeSpan constant format string for Type 'timespan'.");
        }

        if (!TimeSpan.TryParseExact(s, "c", CultureInfo.InvariantCulture, out var ts))
        {
            throw new JsonException("Value is not a valid TimeSpan constant format string for Type 'timespan'.");
        }

        return ts;
    }

    private static Guid ReadGuid(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            throw new JsonException("Value is not a valid GUID string for Type 'guid'.");
        }

        if (!Guid.TryParse(s, out var g))
        {
            throw new JsonException("Value is not a valid GUID string for Type 'guid'.");
        }

        return g;
    }

    private static Uri ReadUri(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            throw new JsonException("Value is not a valid absolute URI string for Type 'uri'.");
        }

        if (!Uri.TryCreate(s, UriKind.Absolute, out var uri))
        {
            throw new JsonException("Value is not a valid absolute URI string for Type 'uri'.");
        }

        return uri;
    }

    private static string[] ReadStringArray(JsonElement element)
    {
        var list = new List<string>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                throw new JsonException("All elements must be JSON strings for Type 'string-array'.");
            }

            var s = item.GetString();
            if (s is null)
            {
                throw new JsonException("String-array elements cannot be null.");
            }

            list.Add(s);
        }

        return list.ToArray();
    }
}
