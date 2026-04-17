namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents the validated consume directives emitted by a query rule.
    /// </summary>
    public sealed class QueryRuleConsumeDefinition
    {
        /// <summary>
        /// Gets the validated tokens that should be removed from the residual token stream.
        /// </summary>
        public IReadOnlyCollection<string> Tokens { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the validated phrases that should be removed from the residual token stream.
        /// </summary>
        public IReadOnlyCollection<string> Phrases { get; init; } = Array.Empty<string>();
    }
}
