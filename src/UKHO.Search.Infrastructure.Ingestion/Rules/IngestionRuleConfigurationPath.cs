namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    /// <summary>
    /// Centralizes the App Configuration key contract for ingestion-authored rules.
    /// </summary>
    public static class IngestionRuleConfigurationPath
    {
        internal const string RulesRoot = "rules";
        internal const string IngestionNamespace = "ingestion";

        /// <summary>
        /// Gets the App Configuration root used for ingestion-authored rules.
        /// </summary>
        public const string IngestionRulesRoot = RulesRoot + ":" + IngestionNamespace;

        /// <summary>
        /// Normalizes the logical provider name used within ingestion rule App Configuration keys.
        /// </summary>
        /// <param name="provider">The provider name supplied by the caller or configuration hierarchy.</param>
        /// <returns>The trimmed lowercase provider name.</returns>
        public static string NormalizeProvider(string provider)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(provider);

            // Normalize provider names so rule keys and runtime lookups consistently use the canonical provider identifier.
            return provider.Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Builds the App Configuration key for one ingestion-authored rule.
        /// </summary>
        /// <param name="provider">The logical provider that owns the rule.</param>
        /// <param name="ruleId">The provider-relative rule identifier, which may itself contain nested path segments separated by colons.</param>
        /// <returns>The namespace-aware App Configuration key for the requested rule.</returns>
        public static string BuildRuleKey(string provider, string ruleId)
        {
            var normalizedProvider = NormalizeProvider(provider);
            var normalizedRuleId = NormalizeRuleId(ruleId);

            // Keep the namespace segment explicit so the key clearly distinguishes ingestion-authored rules from other rule families.
            return $"{IngestionRulesRoot}:{normalizedProvider}:{normalizedRuleId}";
        }

        /// <summary>
        /// Attempts to parse an ingestion-authored rule key into provider and rule identifier components.
        /// </summary>
        /// <param name="key">The App Configuration key to parse.</param>
        /// <param name="provider">The normalized logical provider when parsing succeeds.</param>
        /// <param name="ruleId">The normalized provider-relative rule identifier when parsing succeeds.</param>
        /// <returns><see langword="true"/> when the key matches the ingestion rule key contract; otherwise, <see langword="false"/>.</returns>
        internal static bool TryParseRuleKey(string key, out string provider, out string ruleId)
        {
            provider = string.Empty;
            ruleId = string.Empty;

            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            // Split the key into segments so the helper can validate the namespace separately from the provider and rule identifier.
            var parts = key.Split(':', StringSplitOptions.TrimEntries);
            if (parts.Length < 4)
            {
                return false;
            }

            if (!string.Equals(parts[0], RulesRoot, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(parts[1], IngestionNamespace, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(parts[2]))
            {
                return false;
            }

            var ruleSegments = parts.Skip(3).Where(segment => !string.IsNullOrWhiteSpace(segment)).ToArray();
            if (ruleSegments.Length != parts.Length - 3)
            {
                return false;
            }

            provider = NormalizeProvider(parts[2]);
            ruleId = string.Join(':', ruleSegments.Select(segment => segment.Trim()));
            return true;
        }

        /// <summary>
        /// Normalizes a provider-relative rule identifier before it is used in a configuration key.
        /// </summary>
        /// <param name="ruleId">The provider-relative rule identifier supplied by the caller.</param>
        /// <returns>The normalized rule identifier with nested segments preserved.</returns>
        private static string NormalizeRuleId(string ruleId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);

            // Preserve nested rule-id segments while rejecting empty segments that would create ambiguous keys.
            var segments = ruleId.Split(':', StringSplitOptions.TrimEntries);
            if (segments.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Rule identifier segments must not be empty.", nameof(ruleId));
            }

            return string.Join(':', segments.Select(segment => segment.Trim()));
        }
    }
}
