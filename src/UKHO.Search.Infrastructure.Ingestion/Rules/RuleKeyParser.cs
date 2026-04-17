namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    /// <summary>
    /// Parses App Configuration keys that represent ingestion-authored rules.
    /// </summary>
    public static class RuleKeyParser
    {
        /// <summary>
        /// Attempts to parse a namespace-aware ingestion rule key into provider and rule identifier components.
        /// </summary>
        /// <param name="key">The App Configuration key to parse.</param>
        /// <param name="provider">The parsed logical provider when parsing succeeds.</param>
        /// <param name="ruleId">The parsed provider-relative rule identifier when parsing succeeds.</param>
        /// <returns><see langword="true"/> when the key matches the ingestion rule contract; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(string key, out string provider, out string ruleId)
        {
            // Delegate to the shared path helper so parser, reader, and writer stay aligned on the same namespace-aware contract.
            return IngestionRuleConfigurationPath.TryParseRuleKey(key, out provider, out ruleId);
        }
    }
}
