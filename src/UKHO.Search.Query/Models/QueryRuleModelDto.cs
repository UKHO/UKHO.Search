using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents the raw canonical-model mutation block authored for a query rule.
    /// </summary>
    public sealed class QueryRuleModelDto
    {
        /// <summary>
        /// Gets or sets the authored field-action objects keyed by canonical query-model field name.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JsonElement> FieldActions { get; set; } = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
    }
}
