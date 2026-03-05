using System.Text.Json;

namespace UKHO.Search.Ingestion.Serialization;

public static class IngestionJsonSerializerOptions
{
    public static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        options.Converters.Add(new IngestionPropertyTypeJsonConverter());
        options.Converters.Add(new IngestionPropertyJsonConverter());

        return options;
    }
}
