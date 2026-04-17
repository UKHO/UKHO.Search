using QueryServiceHost.Models;
using UKHO.Search.Query.Models;

namespace QueryServiceHost.Services
{
    /// <summary>
    /// Defines the host-local search client abstraction consumed by the interactive query UI state container.
    /// </summary>
    public interface IQueryUiSearchClient
    {
        /// <summary>
        /// Executes a query UI search request.
        /// </summary>
        /// <param name="request">The host-local query request containing the current search text and facet selections.</param>
        /// <param name="cancellationToken">The cancellation token that stops the search when the caller no longer needs the result.</param>
        /// <returns>The host-local query response projected for the UI.</returns>
        Task<QueryResponse> SearchAsync(QueryRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Executes a caller-supplied query plan through the repository-owned query pipeline.
        /// </summary>
        /// <param name="plan">The repository-owned query plan supplied by the host editor workflow.</param>
        /// <param name="cancellationToken">The cancellation token that stops the execution when the caller no longer needs the result.</param>
        /// <returns>The host-local query response projected for the UI.</returns>
        Task<QueryResponse> ExecutePlanAsync(QueryPlan plan, CancellationToken cancellationToken);
    }
}
