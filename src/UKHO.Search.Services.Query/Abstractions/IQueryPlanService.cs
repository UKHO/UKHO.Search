using UKHO.Search.Query.Models;

namespace UKHO.Search.Services.Query.Abstractions
{
    /// <summary>
    /// Defines the application service that produces repository-owned query plans from raw query text.
    /// </summary>
    public interface IQueryPlanService
    {
        /// <summary>
        /// Produces a repository-owned query plan from the supplied raw query text.
        /// </summary>
        /// <param name="queryText">The raw query text submitted by the caller.</param>
        /// <param name="cancellationToken">The cancellation token that stops planning when the caller no longer needs the result.</param>
        /// <returns>The repository-owned query plan that should later be executed.</returns>
        Task<QueryPlan> PlanAsync(string? queryText, CancellationToken cancellationToken);
    }
}
