using System.Globalization;
using System.Text.Json.Nodes;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Providers.FileShare.Pipeline.Documents
{
    public sealed class CanonicalDocumentBuilder
    {
        private readonly string _documentTypePlaceholder;

        public CanonicalDocumentBuilder(string documentTypePlaceholder)
        {
            _documentTypePlaceholder = documentTypePlaceholder;
        }

        public CanonicalDocument BuildForUpsert(string documentId, IngestionRequest request)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
            ArgumentNullException.ThrowIfNull(request);

            var source = new JsonObject
            {
                ["ingestionRequest"] = BuildIngestionRequestSnapshot(request)
            };

            return new CanonicalDocument
            {
                DocumentId = documentId,
                DocumentType = _documentTypePlaceholder,
                Source = source,
                Normalized = new JsonObject(),
                Descriptions = new JsonObject(),
                Search = new JsonObject(),
                Facets = new JsonObject(),
                Quality = new JsonObject(),
                Provenance = new JsonObject()
            };
        }

        private static JsonObject BuildIngestionRequestSnapshot(IngestionRequest request)
        {
            return new JsonObject
            {
                ["RequestType"] = request.RequestType.ToString(),
                ["AddItem"] = request.AddItem is null
                    ? null
                    : new JsonObject
                    {
                        ["Id"] = request.AddItem.Id,
                        ["Properties"] = BuildPropertiesSnapshot(request.AddItem.Properties),
                        ["SecurityTokens"] = BuildStringArraySnapshot(request.AddItem.SecurityTokens)
                    },
                ["UpdateItem"] = request.UpdateItem is null
                    ? null
                    : new JsonObject
                    {
                        ["Id"] = request.UpdateItem.Id,
                        ["Properties"] = BuildPropertiesSnapshot(request.UpdateItem.Properties),
                        ["SecurityTokens"] = BuildStringArraySnapshot(request.UpdateItem.SecurityTokens)
                    },
                ["DeleteItem"] = request.DeleteItem is null
                    ? null
                    : new JsonObject
                    {
                        ["Id"] = request.DeleteItem.Id
                    },
                ["UpdateAcl"] = request.UpdateAcl is null
                    ? null
                    : new JsonObject
                    {
                        ["Id"] = request.UpdateAcl.Id,
                        ["SecurityTokens"] = BuildStringArraySnapshot(request.UpdateAcl.SecurityTokens)
                    }
            };
        }

        private static JsonArray BuildPropertiesSnapshot(IReadOnlyList<IngestionProperty> properties)
        {
            var arr = new JsonArray();
            foreach (var property in properties)
            {
                var snapshot = new JsonObject
                {
                    ["Name"] = property.Name,
                    ["Value"] = BuildPropertyValueSnapshot(property),
                    ["Type"] = ToPropertyTypeToken(property.Type)
                };

                arr.Add(snapshot);
            }

            return arr;
        }

        private static JsonNode? BuildPropertyValueSnapshot(IngestionProperty property)
        {
            if (property.Value is null)
            {
                return null;
            }

            return property.Type switch
            {
                IngestionPropertyType.String => JsonValue.Create((string)property.Value),
                IngestionPropertyType.Text => JsonValue.Create((string)property.Value),
                IngestionPropertyType.Integer => JsonValue.Create(ToInt64(property.Value)),
                IngestionPropertyType.Double => JsonValue.Create(ToDouble(property.Value)),
                IngestionPropertyType.Decimal => JsonValue.Create(ToDecimal(property.Value)),
                IngestionPropertyType.Boolean => JsonValue.Create(ToBoolean(property.Value)),
                IngestionPropertyType.DateTime => JsonValue.Create(ToDateTimeOffset(property.Value)
                    .ToString("O")),
                IngestionPropertyType.TimeSpan => JsonValue.Create(ToTimeSpan(property.Value)
                    .ToString("c")),
                IngestionPropertyType.Guid => JsonValue.Create(ToGuid(property.Value)
                    .ToString("D")),
                IngestionPropertyType.Uri => JsonValue.Create(ToUri(property.Value)
                    .AbsoluteUri),
                IngestionPropertyType.StringArray => BuildStringArraySnapshot((string[])property.Value),
                var _ => JsonValue.Create(property.Value.ToString())
            };
        }

        private static long ToInt64(object value)
        {
            return value switch
            {
                long l => l,
                int i => i,
                short s => s,
                byte b => b,
                string str => Convert.ToInt64(str, CultureInfo.InvariantCulture),
                var v => Convert.ToInt64(v, CultureInfo.InvariantCulture)
            };
        }

        private static double ToDouble(object value)
        {
            return value switch
            {
                double d => d,
                float f => f,
                decimal m => Convert.ToDouble(m, CultureInfo.InvariantCulture),
                string str => Convert.ToDouble(str, CultureInfo.InvariantCulture),
                var v => Convert.ToDouble(v, CultureInfo.InvariantCulture)
            };
        }

        private static decimal ToDecimal(object value)
        {
            return value switch
            {
                decimal d => d,
                double dbl => Convert.ToDecimal(dbl, CultureInfo.InvariantCulture),
                float f => Convert.ToDecimal(f, CultureInfo.InvariantCulture),
                string str => Convert.ToDecimal(str, CultureInfo.InvariantCulture),
                var v => Convert.ToDecimal(v, CultureInfo.InvariantCulture)
            };
        }

        private static bool ToBoolean(object value)
        {
            return value switch
            {
                bool b => b,
                string str => Convert.ToBoolean(str, CultureInfo.InvariantCulture),
                var v => Convert.ToBoolean(v, CultureInfo.InvariantCulture)
            };
        }

        private static DateTimeOffset ToDateTimeOffset(object value)
        {
            return value switch
            {
                DateTimeOffset dto => dto,
                DateTime dt => new DateTimeOffset(dt),
                string str => DateTimeOffset.Parse(str, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                var v => DateTimeOffset.Parse(v.ToString() ?? string.Empty, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            };
        }

        private static TimeSpan ToTimeSpan(object value)
        {
            return value switch
            {
                TimeSpan ts => ts,
                string str => TimeSpan.Parse(str, CultureInfo.InvariantCulture),
                var v => TimeSpan.Parse(v.ToString() ?? string.Empty, CultureInfo.InvariantCulture)
            };
        }

        private static Guid ToGuid(object value)
        {
            return value switch
            {
                Guid g => g,
                string str => Guid.Parse(str),
                var v => Guid.Parse(v.ToString() ?? string.Empty)
            };
        }

        private static Uri ToUri(object value)
        {
            return value switch
            {
                Uri uri => uri,
                string str => new Uri(str, UriKind.Absolute),
                var v => new Uri(v.ToString() ?? string.Empty, UriKind.Absolute)
            };
        }

        private static JsonArray BuildStringArraySnapshot(string[]? values)
        {
            var arr = new JsonArray();
            if (values is null)
            {
                return arr;
            }

            foreach (var v in values)
            {
                arr.Add(v);
            }

            return arr;
        }

        private static string ToPropertyTypeToken(IngestionPropertyType type)
        {
            return type switch
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
                var _ => type.ToString()
                             .ToLowerInvariant()
            };
        }
    }
}