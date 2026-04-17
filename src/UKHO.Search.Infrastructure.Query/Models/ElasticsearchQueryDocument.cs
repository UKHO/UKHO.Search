using System.Text.Json.Serialization;

namespace UKHO.Search.Infrastructure.Query.Models
{
    /// <summary>
    /// Represents the subset of the canonical Elasticsearch source document that the query UI needs to render search hits.
    /// </summary>
    internal sealed class ElasticsearchQueryDocument
    {
        /// <summary>
        /// Gets the indexed title values preserved for display.
        /// </summary>
        [JsonPropertyName("title")]
        public IReadOnlyCollection<string> Title { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the indexed region values preserved for display.
        /// </summary>
        [JsonPropertyName("region")]
        public IReadOnlyCollection<string> Region { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the indexed category values that provide the closest current type-like value for the query UI.
        /// </summary>
        [JsonPropertyName("category")]
        public IReadOnlyCollection<string> Category { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the indexed format values that can serve as a fallback type-like value when category is absent.
        /// </summary>
        [JsonPropertyName("format")]
        public IReadOnlyCollection<string> Format { get; init; } = Array.Empty<string>();
    }
}
