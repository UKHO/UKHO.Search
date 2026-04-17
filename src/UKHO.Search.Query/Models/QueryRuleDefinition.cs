namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents one validated query rule ready for runtime evaluation.
    /// </summary>
    public sealed class QueryRuleDefinition
    {
        /// <summary>
        /// Gets the stable validated rule identifier.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// Gets the validated rule title.
        /// </summary>
        public required string Title { get; init; }

        /// <summary>
        /// Gets the optional validated rule description.
        /// </summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the rule is enabled.
        /// </summary>
        public bool Enabled { get; init; } = true;

        /// <summary>
        /// Gets the validated predicate used to decide whether the rule matches.
        /// </summary>
        public required QueryRulePredicate Predicate { get; init; }

        /// <summary>
        /// Gets the validated canonical-model mutations emitted by the rule.
        /// </summary>
        public IReadOnlyCollection<QueryRuleModelMutation> ModelMutations { get; init; } = Array.Empty<QueryRuleModelMutation>();

        /// <summary>
        /// Gets the validated concept outputs emitted by the rule.
        /// </summary>
        public IReadOnlyCollection<QueryRuleConceptDefinition> Concepts { get; init; } = Array.Empty<QueryRuleConceptDefinition>();

        /// <summary>
        /// Gets the validated sort-hint outputs emitted by the rule.
        /// </summary>
        public IReadOnlyCollection<QueryRuleSortHintDefinition> SortHints { get; init; } = Array.Empty<QueryRuleSortHintDefinition>();

        /// <summary>
        /// Gets the validated consume directives emitted by the rule.
        /// </summary>
        public QueryRuleConsumeDefinition Consume { get; init; } = new QueryRuleConsumeDefinition();

        /// <summary>
        /// Gets the validated execution-time filters emitted by the rule.
        /// </summary>
        public IReadOnlyCollection<QueryRuleFilterDefinition> Filters { get; init; } = Array.Empty<QueryRuleFilterDefinition>();

        /// <summary>
        /// Gets the validated execution-time boosts emitted by the rule.
        /// </summary>
        public IReadOnlyCollection<QueryRuleBoostDefinition> Boosts { get; init; } = Array.Empty<QueryRuleBoostDefinition>();
    }
}
