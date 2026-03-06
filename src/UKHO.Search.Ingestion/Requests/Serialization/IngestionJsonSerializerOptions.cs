using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion.Requests.Serialization
{
    public static class IngestionJsonSerializerOptions
    {
        public static JsonSerializerOptions Create()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            options.Converters.Add(new IngestionPropertyTypeJsonConverter());
            options.Converters.Add(new IngestionPropertyJsonConverter());

            return options;
        }
    }
}