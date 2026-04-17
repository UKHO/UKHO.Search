using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Infrastructure.Query.Models
{
    /// <summary>
    /// Represents one Elasticsearch hit entry in the raw search response payload.
    /// </summary>
    internal sealed class ElasticsearchSearchHitEnvelope
    {
        /// <summary>
        /// Gets the raw canonical source payload returned by Elasticsearch for the hit.
        /// </summary>
        [JsonPropertyName("_source")]
        public JsonElement Source { get; init; }

        /// <summary>
        /// Gets the named query clauses reported by Elasticsearch for the hit when clause names are present.
        /// </summary>
        [JsonPropertyName("matched_queries")]
        public IReadOnlyCollection<string> MatchedQueries { get; init; } = Array.Empty<string>();
    }
}
