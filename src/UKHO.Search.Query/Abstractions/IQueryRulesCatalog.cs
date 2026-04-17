using UKHO.Search.Query.Models;

namespace UKHO.Search.Query.Abstractions
{
    /// <summary>
    /// Defines the abstraction that exposes the currently loaded query-rule snapshot to the services layer.
    /// </summary>
    public interface IQueryRulesCatalog
    {
        /// <summary>
        /// Gets the current validated query-rule snapshot.
        /// </summary>
        /// <returns>The validated query rules that should be used for the current evaluation request.</returns>
        QueryRulesSnapshot GetSnapshot();

        /// <summary>
        /// Gets diagnostics about the currently loaded query-rule snapshot.
        /// </summary>
        /// <returns>The developer-facing diagnostics describing when the current snapshot was loaded and how many validated rules it contains.</returns>
        QueryRulesCatalogDiagnostics GetDiagnostics();
    }
}
