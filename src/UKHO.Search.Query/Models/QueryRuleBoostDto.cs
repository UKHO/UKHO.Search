namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents one raw boost action authored in a query rule.
    /// </summary>
    public sealed class QueryRuleBoostDto
    {
        /// <summary>
        /// Gets or sets the target field that should receive the explicit rule-driven boost.
        /// </summary>
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the exact values that should be boosted when the boost targets an exact-match field.
        /// </summary>
        public IReadOnlyCollection<string> Values { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the analyzed text that should be boosted when the boost targets an analyzed text field.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional authored matching mode used by the boost action.
        /// </summary>
        public string MatchingMode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the numeric weight that should be applied to the boost clause.
        /// </summary>
        public double Weight { get; set; } = 1.0d;
    }
}
