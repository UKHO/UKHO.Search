using QueryServiceHost.Models;
using QueryServiceHost.Services;
using UKHO.Search.Query.Models;

namespace QueryServiceHost.Tests
{
    /// <summary>
    /// Provides a delegate-backed host search client so tests can drive <see cref="IQueryUiSearchClient"/> behavior without a mocking framework.
    /// </summary>
    internal sealed class DelegatingQueryUiSearchClient : IQueryUiSearchClient
    {
        private readonly Func<QueryRequest, CancellationToken, Task<QueryResponse>> _searchHandler;
        private readonly Func<QueryPlan, CancellationToken, Task<QueryResponse>> _executePlanHandler;

        /// <summary>
        /// Initializes the delegate-backed search client.
        /// </summary>
        /// <param name="searchHandler">The delegate that should execute when the state container issues a raw-query search request.</param>
        /// <param name="executePlanHandler">The delegate that should execute when the state container issues an edited-plan execution request.</param>
        public DelegatingQueryUiSearchClient(
            Func<QueryRequest, CancellationToken, Task<QueryResponse>> searchHandler,
            Func<QueryPlan, CancellationToken, Task<QueryResponse>>? executePlanHandler = null)
        {
            // Capture the test delegates once so each invocation flows through the scenario configured by the test.
            _searchHandler = searchHandler ?? throw new ArgumentNullException(nameof(searchHandler));
            _executePlanHandler = executePlanHandler ?? ((_, _) => Task.FromException<QueryResponse>(new InvalidOperationException("No edited-plan handler was configured for this test.")));
        }

        /// <summary>
        /// Forwards the host-local search request into the configured test delegate.
        /// </summary>
        /// <param name="request">The search request issued by the host state container.</param>
        /// <param name="cancellationToken">The cancellation token supplied by the caller.</param>
        /// <returns>The query response produced by the configured test delegate.</returns>
        public Task<QueryResponse> SearchAsync(QueryRequest request, CancellationToken cancellationToken)
        {
            // Delegate the call directly so each test can fully control success, failure, and response-shape scenarios.
            return _searchHandler(request, cancellationToken);
        }

        /// <summary>
        /// Forwards the edited-plan execution request into the configured test delegate.
        /// </summary>
        /// <param name="plan">The query plan issued by the host state container.</param>
        /// <param name="cancellationToken">The cancellation token supplied by the caller.</param>
        /// <returns>The query response produced by the configured edited-plan delegate.</returns>
        public Task<QueryResponse> ExecutePlanAsync(QueryPlan plan, CancellationToken cancellationToken)
        {
            // Delegate the call directly so each test can fully control edited-plan execution behavior.
            return _executePlanHandler(plan, cancellationToken);
        }
    }
}
