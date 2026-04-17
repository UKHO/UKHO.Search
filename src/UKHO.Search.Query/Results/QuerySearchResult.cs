namespace UKHO.Search.Query.Results
{
    using Models;

    /// <summary>
    /// Represents the executed query result, including the plan that produced it.
    /// </summary>
    public sealed class QuerySearchResult
    {
        /// <summary>
        /// Gets the query plan that was executed.
        /// </summary>
        public required QueryPlan Plan { get; init; }

        /// <summary>
        /// Gets the final Elasticsearch request JSON derived from the executed query plan.
        /// </summary>
        public string ElasticsearchRequestJson { get; init; } = string.Empty;

        /// <summary>
        /// Gets the hits returned by Elasticsearch.
        /// </summary>
        public IReadOnlyCollection<QuerySearchHit> Hits { get; init; } = Array.Empty<QuerySearchHit>();

        /// <summary>
        /// Gets the total number of matching documents reported by Elasticsearch.
        /// </summary>
        public long Total { get; init; }

        /// <summary>
        /// Gets the wall-clock duration spent executing the query.
        /// </summary>
        public TimeSpan Duration { get; init; }

        /// <summary>
        /// Gets the search-engine-reported execution duration, when Elasticsearch returned one.
        /// </summary>
        public TimeSpan? SearchEngineDuration { get; init; }

        /// <summary>
        /// Gets any non-blocking warnings that should be surfaced beside the executed query diagnostics.
        /// </summary>
        public IReadOnlyCollection<string> Warnings { get; init; } = Array.Empty<string>();
    }
}
