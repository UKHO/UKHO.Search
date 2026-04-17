namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents one raw query-rule predicate before validation normalizes it into a runtime predicate.
    /// </summary>
    public sealed class QueryRulePredicateDto
    {
        /// <summary>
        /// Gets or sets the repository-owned predicate path that should be resolved at evaluation time.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the raw equality comparison value.
        /// </summary>
        public string Eq { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the raw phrase value used for contains-phrase matching.
        /// </summary>
        public string ContainsPhrase { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the child predicates used by an any-group predicate.
        /// </summary>
        public IReadOnlyCollection<QueryRulePredicateDto> Any { get; set; } = Array.Empty<QueryRulePredicateDto>();
    }
}
