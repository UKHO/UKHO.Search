using System.Text.Json.Serialization;

namespace UKHO.Search.Infrastructure.Query.Models
{
    /// <summary>
    /// Represents the Elasticsearch total-hit metadata returned in a search response.
    /// </summary>
    internal sealed class ElasticsearchSearchTotalEnvelope
    {
        /// <summary>
        /// Gets the total hit count value reported by Elasticsearch.
        /// </summary>
        [JsonPropertyName("value")]
        public long Value { get; init; }
    }
}
