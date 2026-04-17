using UKHO.Search.Query.Abstractions;
using UKHO.Search.Query.Models;

namespace UKHO.Search.Services.Query.Tests.TestSupport
{
    /// <summary>
    /// Provides a deterministic query-rule catalog for service-layer tests.
    /// </summary>
    internal sealed class FixedQueryRulesCatalog : IQueryRulesCatalog
    {
        private readonly QueryRulesSnapshot _snapshot;

        /// <summary>
        /// Initializes the catalog with the snapshot that should be returned for every request.
        /// </summary>
        /// <param name="snapshot">The deterministic validated query-rule snapshot to return.</param>
        public FixedQueryRulesCatalog(QueryRulesSnapshot snapshot)
        {
            // Capture the supplied snapshot once so service-layer tests can evaluate rules deterministically.
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        /// <summary>
        /// Returns the predetermined validated query-rule snapshot.
        /// </summary>
        /// <returns>The predetermined validated query-rule snapshot.</returns>
        public QueryRulesSnapshot GetSnapshot()
        {
            // Return the fixed snapshot directly because tests control all rule input explicitly.
            return _snapshot;
        }

        /// <summary>
        /// Returns deterministic diagnostics for the fixed snapshot used by the tests.
        /// </summary>
        /// <returns>The deterministic diagnostics for the fixed snapshot.</returns>
        public QueryRulesCatalogDiagnostics GetDiagnostics()
        {
            // Return stable diagnostics so service-layer tests that do not care about catalog timing can still satisfy the full abstraction.
            return new QueryRulesCatalogDiagnostics
            {
                LoadedAtUtc = DateTimeOffset.UtcNow,
                RuleCount = _snapshot.Rules.Count
            };
        }
    }
}
