namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents the validated flat query-rule snapshot loaded from configuration.
    /// </summary>
    public sealed class QueryRulesSnapshot
    {
        /// <summary>
        /// Gets the rule schema version used by the validated snapshot.
        /// </summary>
        public string SchemaVersion { get; init; } = string.Empty;

        /// <summary>
        /// Gets the ordered query rules that should be evaluated for each planning request.
        /// </summary>
        public IReadOnlyCollection<QueryRuleDefinition> Rules { get; init; } = Array.Empty<QueryRuleDefinition>();
    }
}
