namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents one raw concept action authored in a query rule.
    /// </summary>
    public sealed class QueryRuleConceptDto
    {
        /// <summary>
        /// Gets or sets the authored concept identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the authored matched-text template.
        /// </summary>
        public string MatchedText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the authored keyword expansions.
        /// </summary>
        public IReadOnlyCollection<string> KeywordExpansions { get; set; } = Array.Empty<string>();
    }
}
