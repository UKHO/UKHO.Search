using UKHO.Search.Query.Results;
using UKHO.Search.Query.Models;

namespace UKHO.Search.Services.Query.Abstractions
{
    /// <summary>
    /// Defines the application service that plans and executes a query in one host-friendly step.
    /// </summary>
    public interface IQuerySearchService
    {
        /// <summary>
        /// Plans and executes a query from the supplied raw query text.
        /// </summary>
        /// <param name="queryText">The raw query text submitted by the caller.</param>
        /// <param name="cancellationToken">The cancellation token that stops planning or execution when the caller no longer needs the result.</param>
        /// <returns>The executed query result.</returns>
        Task<QuerySearchResult> SearchAsync(string? queryText, CancellationToken cancellationToken);

        /// <summary>
        /// Executes a caller-supplied query plan without re-running the raw-query planning stage.
        /// </summary>
        /// <param name="plan">The repository-owned query plan supplied by the caller.</param>
        /// <param name="cancellationToken">The cancellation token that stops execution when the caller no longer needs the result.</param>
        /// <returns>The executed query result.</returns>
        Task<QuerySearchResult> ExecutePlanAsync(QueryPlan plan, CancellationToken cancellationToken);
    }
}
