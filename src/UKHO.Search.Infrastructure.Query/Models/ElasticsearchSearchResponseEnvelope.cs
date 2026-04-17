using System.Text.Json.Serialization;

namespace UKHO.Search.Infrastructure.Query.Models
{
    /// <summary>
    /// Represents the top-level Elasticsearch search response payload consumed by the query executor.
    /// </summary>
    internal sealed class ElasticsearchSearchResponseEnvelope
    {
        /// <summary>
        /// Gets the engine-reported execution time in milliseconds.
        /// </summary>
        [JsonPropertyName("took")]
        public int? TookMilliseconds { get; init; }

        /// <summary>
        /// Gets the hits container returned by Elasticsearch.
        /// </summary>
        [JsonPropertyName("hits")]
        public ElasticsearchSearchHitsEnvelope? Hits { get; init; }
    }
}
