using System.Text.Json.Serialization;
using UKHO.Search.Ingestion.Requests.Serialization;

namespace UKHO.Search.Ingestion.Requests
{
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
        StringArray
    }
}