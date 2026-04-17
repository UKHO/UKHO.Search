namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents execution-time behavior that accompanies, but does not become part of, the canonical query model.
    /// </summary>
    public sealed class QueryExecutionDirectives
    {
        /// <summary>
        /// Gets the ordered filter directives that must be applied as non-scoring constraints when executing the query plan.
        /// </summary>
        public IReadOnlyCollection<QueryExecutionFilterDirective> Filters { get; init; } = Array.Empty<QueryExecutionFilterDirective>();

        /// <summary>
        /// Gets the ordered explicit boost directives that should contribute additional scoring clauses when executing the query plan.
        /// </summary>
        public IReadOnlyCollection<QueryExecutionBoostDirective> Boosts { get; init; } = Array.Empty<QueryExecutionBoostDirective>();

        /// <summary>
        /// Gets the ordered sort directives that should be applied when executing the query plan.
        /// </summary>
        public IReadOnlyCollection<QueryExecutionSortDirective> Sorts { get; init; } = Array.Empty<QueryExecutionSortDirective>();
    }
}
