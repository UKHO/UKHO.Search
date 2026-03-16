using System.Text.Json.Serialization;

namespace UKHO.Search.Infrastructure.Ingestion.Rules.Model
{
    internal sealed class RuleFileDocumentDto
    {
        [JsonPropertyName("SchemaVersion")]
        public string? SchemaVersion { get; set; }

        [JsonPropertyName("Rule")]
        public RuleDto? Rule { get; set; }
    }
}
