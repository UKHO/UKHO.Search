namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents developer-facing diagnostics emitted during query planning.
    /// </summary>
    public sealed class QueryPlanDiagnostics
    {
        /// <summary>
        /// Gets the identifiers of the rules that matched while shaping the query plan.
        /// </summary>
        public IReadOnlyCollection<string> MatchedRuleIds { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the filter descriptions derived while shaping the query plan.
        /// </summary>
        public IReadOnlyCollection<string> AppliedFilters { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the boost descriptions derived while shaping the query plan.
        /// </summary>
        public IReadOnlyCollection<string> AppliedBoosts { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the sort descriptions derived while shaping the query plan.
        /// </summary>
        public IReadOnlyCollection<string> AppliedSorts { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the timestamp at which the rule snapshot used for the current planning request was last loaded.
        /// </summary>
        public DateTimeOffset? RuleCatalogLoadedAtUtc { get; init; }
    }
}
