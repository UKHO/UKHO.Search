using System.Text.Json.Serialization;
using UKHO.Search.Ingestion.Serialization;

namespace UKHO.Search.Ingestion;

[JsonConverter(typeof(IngestionPropertyTypeJsonConverter))]
public enum IngestionPropertyType
{
    String,
    Integer,
    Double,
    Decimal,
    Boolean,
    DateTime,
    TimeSpan,
    Id,
    Guid,
    Uri,
    StringArray,
}
