namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents one validated sort-hint output emitted by a query rule.
    /// </summary>
    public sealed class QueryRuleSortHintDefinition
    {
        /// <summary>
        /// Gets the stable sort-hint identifier emitted by the rule.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// Gets the validated matched-text template retained by the rule.
        /// </summary>
        public string MatchedTextTemplate { get; init; } = string.Empty;

        /// <summary>
        /// Gets the validated canonical field order emitted by the rule.
        /// </summary>
        public IReadOnlyCollection<string> Fields { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the validated execution-time sort direction emitted by the rule.
        /// </summary>
        public QueryExecutionSortDirection Direction { get; init; } = QueryExecutionSortDirection.Descending;
    }
}
