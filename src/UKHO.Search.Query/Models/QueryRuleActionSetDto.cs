namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents the raw action groups authored beneath a query-rule then block.
    /// </summary>
    public sealed class QueryRuleActionSetDto
    {
        /// <summary>
        /// Gets or sets the raw canonical-model mutations authored for the rule.
        /// </summary>
        public QueryRuleModelDto? Model { get; set; }

        /// <summary>
        /// Gets or sets the raw concept outputs authored for the rule.
        /// </summary>
        public IReadOnlyCollection<QueryRuleConceptDto> Concepts { get; set; } = Array.Empty<QueryRuleConceptDto>();

        /// <summary>
        /// Gets or sets the raw sort-hint outputs authored for the rule.
        /// </summary>
        public IReadOnlyCollection<QueryRuleSortHintDto> SortHints { get; set; } = Array.Empty<QueryRuleSortHintDto>();

        /// <summary>
        /// Gets or sets the raw consume directives authored for the rule.
        /// </summary>
        public QueryRuleConsumeDto? Consume { get; set; }

        /// <summary>
        /// Gets or sets the raw execution-time filters authored for the rule.
        /// </summary>
        public QueryRuleFilterDto? Filters { get; set; }

        /// <summary>
        /// Gets or sets the raw execution-time boosts authored for the rule.
        /// </summary>
        public IReadOnlyCollection<QueryRuleBoostDto> Boosts { get; set; } = Array.Empty<QueryRuleBoostDto>();
    }
}
