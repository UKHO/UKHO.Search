using UKHO.Search.Infrastructure.Query.Rules;

namespace UKHO.Search.Infrastructure.Query.Tests.TestSupport
{
    /// <summary>
    /// Provides a mutable in-memory query-rule source for infrastructure tests.
    /// </summary>
    internal sealed class MutableQueryRulesSource : IQueryRulesSource
    {
        private IReadOnlyList<QueryRuleEntry> _entries;

        /// <summary>
        /// Initializes the source with the initial raw query-rule entries.
        /// </summary>
        /// <param name="entries">The initial raw query-rule entries to expose.</param>
        public MutableQueryRulesSource(IReadOnlyList<QueryRuleEntry> entries)
        {
            // Capture the initial raw entries so tests can control the first catalog load deterministically.
            _entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }

        /// <summary>
        /// Replaces the raw query-rule entries exposed by the source.
        /// </summary>
        /// <param name="entries">The replacement raw query-rule entries to expose.</param>
        public void SetEntries(IReadOnlyList<QueryRuleEntry> entries)
        {
            // Replace the entry snapshot atomically from the test so reload behavior can be verified deterministically.
            _entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }

        /// <summary>
        /// Returns the current raw query-rule entries.
        /// </summary>
        /// <returns>The current raw query-rule entries.</returns>
        public IReadOnlyList<QueryRuleEntry> ListRuleEntries()
        {
            // Return the current test-controlled entry snapshot directly because the tests own all mutation timing.
            return _entries;
        }
    }
}
