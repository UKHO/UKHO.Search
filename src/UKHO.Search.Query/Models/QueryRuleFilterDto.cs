using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents the raw filter block authored for a query rule.
    /// </summary>
    public sealed class QueryRuleFilterDto
    {
        /// <summary>
        /// Gets or sets the authored field-action objects keyed by canonical field name for execution-time filters.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JsonElement> FieldActions { get; set; } = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
    }
}
