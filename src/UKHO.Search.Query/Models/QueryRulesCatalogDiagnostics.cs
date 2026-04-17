namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents developer-facing diagnostics about the currently loaded query-rule snapshot.
    /// </summary>
    public sealed class QueryRulesCatalogDiagnostics
    {
        /// <summary>
        /// Gets the timestamp at which the current validated query-rule snapshot was loaded.
        /// </summary>
        public DateTimeOffset LoadedAtUtc { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets the count of validated rules retained on the current snapshot.
        /// </summary>
        public int RuleCount { get; init; }
    }
}
