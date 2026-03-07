using System.Text.Json.Serialization;
using UKHO.Search.Ingestion.Requests.Serialization;

namespace UKHO.Search.Ingestion.Requests
{
    [JsonConverter(typeof(IngestionPropertyTypeJsonConverter))]
    public enum IngestionPropertyType
    {
        String,
        Text,
        Integer,
        Double,
        Decimal,
        Boolean,
        DateTime,
        TimeSpan,
        Guid,
        Uri,
        StringArray
    }
}