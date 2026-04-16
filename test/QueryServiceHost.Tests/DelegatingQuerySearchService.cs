using UKHO.Search.Query.Results;
using UKHO.Search.Query.Models;
using UKHO.Search.Services.Query.Abstractions;

namespace QueryServiceHost.Tests
{
    /// <summary>
    /// Provides a delegate-backed repository search service so host-adapter tests can control the query result without booting the full query pipeline.
    /// </summary>
    internal sealed class DelegatingQuerySearchService : IQuerySearchService
    {
        private readonly Func<string?, CancellationToken, Task<QuerySearchResult>> _searchHandler;
        private readonly Func<QueryPlan, CancellationToken, Task<QuerySearchResult>> _executePlanHandler;

        /// <summary>
        /// Initializes the delegate-backed repository search service.
        /// </summary>
        /// <param name="searchHandler">The delegate that should execute when the host adapter invokes the raw-query repository search service.</param>
        /// <param name="executePlanHandler">The delegate that should execute when the host adapter invokes edited-plan execution.</param>
        public DelegatingQuerySearchService(
            Func<string?, CancellationToken, Task<QuerySearchResult>> searchHandler,
            Func<QueryPlan, CancellationToken, Task<QuerySearchResult>>? executePlanHandler = null)
        {
            // Capture the test delegates once so the adapter test controls the repository response precisely.
            _searchHandler = searchHandler ?? throw new ArgumentNullException(nameof(searchHandler));
            _executePlanHandler = executePlanHandler ?? ((_, _) => Task.FromException<QuerySearchResult>(new InvalidOperationException("No edited-plan handler was configured for this test.")));
        }

        /// <summary>
        /// Forwards the raw query text into the configured test delegate.
        /// </summary>
        /// <param name="queryText">The raw query text submitted by the host adapter.</param>
        /// <param name="cancellationToken">The cancellation token supplied by the caller.</param>
        /// <returns>The repository query result produced by the configured test delegate.</returns>
        public Task<QuerySearchResult> SearchAsync(string? queryText, CancellationToken cancellationToken)
        {
            // Delegate the call directly so the test can shape the executed plan and hit payload.
            return _searchHandler(queryText, cancellationToken);
        }

        /// <summary>
        /// Forwards the supplied query plan into the configured test delegate.
        /// </summary>
        /// <param name="plan">The repository-owned query plan submitted by the host adapter.</param>
        /// <param name="cancellationToken">The cancellation token supplied by the caller.</param>
        /// <returns>The repository query result produced by the configured edited-plan delegate.</returns>
        public Task<QuerySearchResult> ExecutePlanAsync(QueryPlan plan, CancellationToken cancellationToken)
        {
            // Delegate the call directly so the test can shape supplied-plan execution behavior precisely.
            return _executePlanHandler(plan, cancellationToken);
        }
    }
}
