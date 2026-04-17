namespace UKHO.Search.Infrastructure.Query.Rules
{
    /// <summary>
    /// Represents one raw query-rule entry enumerated from configuration.
    /// </summary>
    internal sealed class QueryRuleEntry
    {
        /// <summary>
        /// Gets the namespace-aware key that identified the rule entry in configuration.
        /// </summary>
        public required string Key { get; init; }

        /// <summary>
        /// Gets the flat rule identifier derived from the configuration key.
        /// </summary>
        public required string RuleId { get; init; }

        /// <summary>
        /// Gets the raw JSON document payload stored for the rule.
        /// </summary>
        public string Json { get; init; } = string.Empty;
    }
}
