namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents one raw sort-hint action authored in a query rule.
    /// </summary>
    public sealed class QueryRuleSortHintDto
    {
        /// <summary>
        /// Gets or sets the authored sort-hint identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the authored matched-text template.
        /// </summary>
        public string MatchedText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the authored canonical field order used for sorting.
        /// </summary>
        public IReadOnlyCollection<string> Fields { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the authored textual sort order.
        /// </summary>
        public string Order { get; set; } = string.Empty;
    }
}
