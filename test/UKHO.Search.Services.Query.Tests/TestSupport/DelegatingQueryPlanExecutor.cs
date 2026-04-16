using UKHO.Search.Query.Abstractions;
using UKHO.Search.Query.Models;
using UKHO.Search.Query.Results;

namespace UKHO.Search.Services.Query.Tests.TestSupport
{
    /// <summary>
    /// Provides a delegate-backed query-plan executor so orchestration tests can control execution behavior without talking to Elasticsearch.
    /// </summary>
    internal sealed class DelegatingQueryPlanExecutor : IQueryPlanExecutor
    {
        private readonly Func<QueryPlan, CancellationToken, Task<QuerySearchResult>> _handler;

        /// <summary>
        /// Initializes the delegate-backed query-plan executor.
        /// </summary>
        /// <param name="handler">The delegate that should execute when the orchestration service runs a query plan.</param>
        public DelegatingQueryPlanExecutor(Func<QueryPlan, CancellationToken, Task<QuerySearchResult>> handler)
        {
            // Capture the delegate once so each test can fully control the execution result it wants to observe.
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <summary>
        /// Forwards the repository-owned query plan into the configured execution delegate.
        /// </summary>
        /// <param name="plan">The repository-owned query plan submitted by the orchestration service.</param>
        /// <param name="cancellationToken">The cancellation token supplied by the caller.</param>
        /// <returns>The repository-owned query result produced by the configured delegate.</returns>
        public Task<QuerySearchResult> SearchAsync(QueryPlan plan, CancellationToken cancellationToken)
        {
            // Delegate directly so each test can decide exactly how execution behaves for the scenario under test.
            return _handler(plan, cancellationToken);
        }
    }
}
