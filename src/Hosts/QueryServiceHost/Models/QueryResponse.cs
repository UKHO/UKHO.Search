using UKHO.Search.Query.Models;

namespace QueryServiceHost.Models
{
    /// <summary>
    /// Represents the host-local query response consumed by the Blazor workspace shell.
    /// </summary>
    public class QueryResponse
    {
        /// <summary>
        /// Gets the repository-owned query plan retained for host-side diagnostics projection.
        /// </summary>
        public QueryPlan? Plan { get; init; }

        /// <summary>
        /// Gets the formatted generated query plan JSON shown in the Monaco workspace.
        /// </summary>
        public string GeneratedPlanJson { get; init; } = string.Empty;

        /// <summary>
        /// Gets the formatted final Elasticsearch request JSON shown in the diagnostics column.
        /// </summary>
        public string ElasticsearchRequestJson { get; init; } = string.Empty;

        /// <summary>
        /// Gets the projected result hits returned by the current query execution.
        /// </summary>
        public IReadOnlyList<Hit> Hits { get; init; } = Array.Empty<Hit>();

        /// <summary>
        /// Gets the projected facet groups available to the host shell.
        /// </summary>
        public IReadOnlyList<FacetGroup> Facets { get; init; } = Array.Empty<FacetGroup>();

        /// <summary>
        /// Gets the total number of matching results returned by the execution.
        /// </summary>
        public long Total { get; init; }

        /// <summary>
        /// Gets the wall-clock duration spent executing the current query.
        /// </summary>
        public TimeSpan Duration { get; init; }

        /// <summary>
        /// Gets the search-engine-reported execution duration, when Elasticsearch returned one.
        /// </summary>
        public TimeSpan? SearchEngineDuration { get; init; }

        /// <summary>
        /// Gets any non-blocking warnings returned by the repository-owned query pipeline.
        /// </summary>
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets a value indicating whether the response was produced by the edited-plan execution path.
        /// </summary>
        public bool UsedEditedPlan { get; init; }
    }
}
