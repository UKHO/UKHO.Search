namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents one validated predicate node used by the query-rule runtime.
    /// </summary>
    public sealed class QueryRulePredicate
    {
        /// <summary>
        /// Gets the validated predicate kind.
        /// </summary>
        public QueryRulePredicateKind Kind { get; init; } = QueryRulePredicateKind.Equals;

        /// <summary>
        /// Gets the validated repository-owned path inspected by the predicate when the predicate kind requires one.
        /// </summary>
        public string Path { get; init; } = string.Empty;

        /// <summary>
        /// Gets the validated comparison value or phrase used by the predicate when the predicate kind requires one.
        /// </summary>
        public string Value { get; init; } = string.Empty;

        /// <summary>
        /// Gets the validated child predicates used by an any-group predicate.
        /// </summary>
        public IReadOnlyCollection<QueryRulePredicate> Any { get; init; } = Array.Empty<QueryRulePredicate>();
    }
}
