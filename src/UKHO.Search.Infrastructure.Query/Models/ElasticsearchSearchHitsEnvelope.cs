using System.Text.Json.Serialization;

namespace UKHO.Search.Infrastructure.Query.Models
{
    /// <summary>
    /// Represents the Elasticsearch hits container in the raw search response payload.
    /// </summary>
    internal sealed class ElasticsearchSearchHitsEnvelope
    {
        /// <summary>
        /// Gets the total-hit metadata reported by Elasticsearch.
        /// </summary>
        [JsonPropertyName("total")]
        public ElasticsearchSearchTotalEnvelope? Total { get; init; }

        /// <summary>
        /// Gets the individual hits returned by Elasticsearch.
        /// </summary>
        [JsonPropertyName("hits")]
        public IReadOnlyCollection<ElasticsearchSearchHitEnvelope> Hits { get; init; } = Array.Empty<ElasticsearchSearchHitEnvelope>();
    }
}
