namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents one execution-time boost directive carried by the query plan.
    /// </summary>
    public sealed class QueryExecutionBoostDirective
    {
        /// <summary>
        /// Gets the target field that should receive the explicit boost.
        /// </summary>
        public required string FieldName { get; init; }

        /// <summary>
        /// Gets the matching mode used to translate the boost into Elasticsearch behavior.
        /// </summary>
        public QueryExecutionBoostMatchingMode MatchingMode { get; init; } = QueryExecutionBoostMatchingMode.ExactTerms;

        /// <summary>
        /// Gets the exact string values that should be boosted when the field is string-backed.
        /// </summary>
        public IReadOnlyCollection<string> StringValues { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the exact integer values that should be boosted when the field is integer-backed.
        /// </summary>
        public IReadOnlyCollection<int> IntegerValues { get; init; } = Array.Empty<int>();

        /// <summary>
        /// Gets the analyzed text that should be boosted when the target field is analyzed.
        /// </summary>
        public string Text { get; init; } = string.Empty;

        /// <summary>
        /// Gets the numeric weight that should be applied to the boost clause.
        /// </summary>
        public double Weight { get; init; } = 1.0d;
    }
}
