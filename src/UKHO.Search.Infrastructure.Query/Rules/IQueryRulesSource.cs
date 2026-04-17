namespace UKHO.Search.Infrastructure.Query.Rules
{
    /// <summary>
    /// Defines the infrastructure abstraction that lists raw query-rule entries from configuration-backed storage.
    /// </summary>
    internal interface IQueryRulesSource
    {
        /// <summary>
        /// Lists the raw query-rule entries currently available in the backing store.
        /// </summary>
        /// <returns>The raw query-rule entries that should be loaded and validated.</returns>
        IReadOnlyList<QueryRuleEntry> ListRuleEntries();
    }
}
