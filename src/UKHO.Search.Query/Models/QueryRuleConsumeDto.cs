namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents the raw consume directives authored in a query rule.
    /// </summary>
    public sealed class QueryRuleConsumeDto
    {
        /// <summary>
        /// Gets or sets the authored tokens that should be removed from the residual token stream.
        /// </summary>
        public IReadOnlyCollection<string> Tokens { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the authored phrases that should be removed from the residual token stream.
        /// </summary>
        public IReadOnlyCollection<string> Phrases { get; set; } = Array.Empty<string>();
    }
}
