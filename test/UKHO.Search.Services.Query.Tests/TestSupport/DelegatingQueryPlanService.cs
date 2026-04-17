using UKHO.Search.Query.Models;
using UKHO.Search.Services.Query.Abstractions;

namespace UKHO.Search.Services.Query.Tests.TestSupport
{
    /// <summary>
    /// Provides a delegate-backed query-plan service so orchestration tests can control planning behavior without booting the full planner.
    /// </summary>
    internal sealed class DelegatingQueryPlanService : IQueryPlanService
    {
        private readonly Func<string?, CancellationToken, Task<QueryPlan>> _handler;

        /// <summary>
        /// Initializes the delegate-backed query-plan service.
        /// </summary>
        /// <param name="handler">The delegate that should execute when the orchestration service requests a planned query.</param>
        public DelegatingQueryPlanService(Func<string?, CancellationToken, Task<QueryPlan>> handler)
        {
            // Capture the delegate once so each test can fully control the planned result it wants to observe.
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <summary>
        /// Forwards the raw query text into the configured planning delegate.
        /// </summary>
        /// <param name="queryText">The raw query text submitted by the orchestration service.</param>
        /// <param name="cancellationToken">The cancellation token supplied by the caller.</param>
        /// <returns>The repository-owned query plan produced by the configured delegate.</returns>
        public Task<QueryPlan> PlanAsync(string? queryText, CancellationToken cancellationToken)
        {
            // Delegate directly so each test can decide exactly how planning behaves for the scenario under test.
            return _handler(queryText, cancellationToken);
        }
    }
}
