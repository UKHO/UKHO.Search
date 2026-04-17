namespace UKHO.Search.Infrastructure.Query.Rules
{
    /// <summary>
    /// Centralizes the App Configuration key contract for query-authored rules.
    /// </summary>
    public static class QueryRuleConfigurationPath
    {
        internal const string RulesRoot = "rules";
        internal const string QueryNamespace = "query";

        /// <summary>
        /// Gets the App Configuration root used for flat query-authored rules.
        /// </summary>
        public const string QueryRulesRoot = RulesRoot + ":" + QueryNamespace;

        /// <summary>
        /// Builds the App Configuration key for one query-authored rule.
        /// </summary>
        /// <param name="ruleId">The flat rule identifier supplied by the caller.</param>
        /// <returns>The namespace-aware App Configuration key for the requested rule.</returns>
        public static string BuildRuleKey(string ruleId)
        {
            var normalizedRuleId = NormalizeRuleId(ruleId);

            // Keep the namespace segment explicit so flat query rules remain clearly separated from other rule families.
            return $"{QueryRulesRoot}:{normalizedRuleId}";
        }

        /// <summary>
        /// Attempts to parse a query-authored rule key into its rule identifier component.
        /// </summary>
        /// <param name="key">The App Configuration key to parse.</param>
        /// <param name="ruleId">The normalized flat rule identifier when parsing succeeds.</param>
        /// <returns><see langword="true" /> when the key matches the query rule key contract; otherwise, <see langword="false" />.</returns>
        internal static bool TryParseRuleKey(string key, out string ruleId)
        {
            ruleId = string.Empty;

            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            // Split the key into segments so the helper can validate the namespace separately from the flat rule identifier.
            var parts = key.Split(':', StringSplitOptions.TrimEntries);
            if (parts.Length != 3)
            {
                return false;
            }

            if (!string.Equals(parts[0], RulesRoot, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(parts[1], QueryNamespace, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(parts[2]))
            {
                return false;
            }

            ruleId = NormalizeRuleId(parts[2]);
            return true;
        }

        /// <summary>
        /// Normalizes a flat rule identifier before it is used in a configuration key.
        /// </summary>
        /// <param name="ruleId">The flat rule identifier supplied by the caller.</param>
        /// <returns>The trimmed lowercase rule identifier.</returns>
        private static string NormalizeRuleId(string ruleId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);

            // Flat query rules do not support nested segments, so a single lowercase identifier is the canonical form.
            return ruleId.Trim().ToLowerInvariant();
        }
    }
}
