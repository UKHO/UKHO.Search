namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents one validated explicit boost emitted by a query rule.
    /// </summary>
    public sealed class QueryRuleBoostDefinition
    {
        /// <summary>
        /// Gets the target field that should receive the explicit boost.
        /// </summary>
        public required string FieldName { get; init; }

        /// <summary>
        /// Gets the validated matching mode that should be used when translating the boost.
        /// </summary>
        public QueryExecutionBoostMatchingMode MatchingMode { get; init; } = QueryExecutionBoostMatchingMode.ExactTerms;

        /// <summary>
        /// Gets the validated exact values that should be boosted when the field is exact-match.
        /// </summary>
        public IReadOnlyCollection<string> StringValues { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the validated integer values that should be boosted when the field is integer-backed.
        /// </summary>
        public IReadOnlyCollection<int> IntegerValues { get; init; } = Array.Empty<int>();

        /// <summary>
        /// Gets the analyzed text that should be boosted when the field is analyzed.
        /// </summary>
        public string Text { get; init; } = string.Empty;

        /// <summary>
        /// Gets the numeric weight that should be applied to the resulting boost clause.
        /// </summary>
        public double Weight { get; init; } = 1.0d;
    }
}
