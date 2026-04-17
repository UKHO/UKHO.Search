using UKHO.Search.Query.Models;
using UKHO.Search.Query.Results;

namespace UKHO.Search.Query.Abstractions
{
    /// <summary>
    /// Defines the abstraction that executes a repository-owned query plan against the backing search engine.
    /// </summary>
    public interface IQueryPlanExecutor
    {
        /// <summary>
        /// Executes the supplied query plan against the backing search engine.
        /// </summary>
        /// <param name="plan">The repository-owned query plan that should be translated into search-engine behavior.</param>
        /// <param name="cancellationToken">The cancellation token that stops execution when the caller no longer needs the result.</param>
        /// <returns>The executed search result.</returns>
        Task<QuerySearchResult> SearchAsync(QueryPlan plan, CancellationToken cancellationToken);
    }
}
