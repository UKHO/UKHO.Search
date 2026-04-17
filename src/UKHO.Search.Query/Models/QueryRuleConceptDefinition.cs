namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents one validated concept output emitted by a query rule.
    /// </summary>
    public sealed class QueryRuleConceptDefinition
    {
        /// <summary>
        /// Gets the stable concept identifier emitted by the rule.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// Gets the validated matched-text template retained by the rule.
        /// </summary>
        public string MatchedTextTemplate { get; init; } = string.Empty;

        /// <summary>
        /// Gets the validated keyword expansions emitted by the rule.
        /// </summary>
        public IReadOnlyCollection<string> KeywordExpansions { get; init; } = Array.Empty<string>();
    }
}
