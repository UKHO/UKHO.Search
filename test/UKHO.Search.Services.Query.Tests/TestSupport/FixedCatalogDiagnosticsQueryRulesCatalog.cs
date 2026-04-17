using UKHO.Search.Query.Abstractions;
using UKHO.Search.Query.Models;

namespace UKHO.Search.Services.Query.Tests.TestSupport
{
    /// <summary>
    /// Provides a deterministic query-rule catalog with explicit diagnostics for service-layer tests.
    /// </summary>
    internal sealed class FixedCatalogDiagnosticsQueryRulesCatalog : IQueryRulesCatalog
    {
        private readonly QueryRulesSnapshot _snapshot;
        private readonly QueryRulesCatalogDiagnostics _diagnostics;

        /// <summary>
        /// Initializes the catalog with the snapshot and diagnostics that should be returned for every request.
        /// </summary>
        /// <param name="snapshot">The deterministic validated query-rule snapshot to return.</param>
        /// <param name="diagnostics">The deterministic diagnostics to return.</param>
        public FixedCatalogDiagnosticsQueryRulesCatalog(QueryRulesSnapshot snapshot, QueryRulesCatalogDiagnostics diagnostics)
        {
            // Capture the supplied state once so service-layer tests can evaluate planning diagnostics deterministically.
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
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
        /// Returns the predetermined query-rule catalog diagnostics.
        /// </summary>
        /// <returns>The predetermined query-rule catalog diagnostics.</returns>
        public QueryRulesCatalogDiagnostics GetDiagnostics()
        {
            // Return the fixed diagnostics directly because tests control the catalog state explicitly.
            return _diagnostics;
        }
    }
}
