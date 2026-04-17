using Microsoft.Extensions.Logging;
using UKHO.Search.Query.Abstractions;
using UKHO.Search.Query.Models;
using UKHO.Search.Query.Results;
using UKHO.Search.Services.Query.Abstractions;

namespace UKHO.Search.Services.Query.Execution
{
    /// <summary>
    /// Coordinates query planning and execution into one host-friendly application service.
    /// </summary>
    public sealed class QuerySearchService : IQuerySearchService
    {
        private readonly IQueryPlanService _queryPlanService;
        private readonly IQueryPlanExecutor _queryPlanExecutor;
        private readonly ILogger<QuerySearchService> _logger;

        /// <summary>
        /// Initializes the query search service with the planner and execution adapter required for end-to-end query handling.
        /// </summary>
        /// <param name="queryPlanService">The planner that converts raw query text into a repository-owned query plan.</param>
        /// <param name="queryPlanExecutor">The execution adapter that translates the query plan into search-engine behavior.</param>
        /// <param name="logger">The logger used to emit structured search diagnostics and failures.</param>
        public QuerySearchService(IQueryPlanService queryPlanService, IQueryPlanExecutor queryPlanExecutor, ILogger<QuerySearchService> logger)
        {
            // Capture the injected collaborators so each search request runs through the same planner and execution path.
            _queryPlanService = queryPlanService ?? throw new ArgumentNullException(nameof(queryPlanService));
            _queryPlanExecutor = queryPlanExecutor ?? throw new ArgumentNullException(nameof(queryPlanExecutor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Plans and executes a query from the supplied raw query text.
        /// </summary>
        /// <param name="queryText">The raw query text submitted by the caller.</param>
        /// <param name="cancellationToken">The cancellation token that stops planning or execution when the caller no longer needs the result.</param>
        /// <returns>The executed query result.</returns>
        public async Task<QuerySearchResult> SearchAsync(string? queryText, CancellationToken cancellationToken)
        {
            try
            {
                // Produce the repository-owned query plan first so the execution adapter never sees host-specific request types.
                var plan = await _queryPlanService.PlanAsync(queryText, cancellationToken)
                    .ConfigureAwait(false);

                // Reuse the shared execution path so raw-query and edited-plan execution produce the same orchestration and logging behavior.
                return await ExecutePlanCoreAsync(plan, isSuppliedPlan: false, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Log the failure at the application-service boundary so host callers can correlate planner and executor failures.
                _logger.LogError(ex, "Failed to execute a query search for the supplied query text.");
                throw;
            }
        }

        /// <summary>
        /// Executes a caller-supplied query plan without re-running the planning stage.
        /// </summary>
        /// <param name="plan">The repository-owned query plan supplied by the caller.</param>
        /// <param name="cancellationToken">The cancellation token that stops execution when the caller no longer needs the result.</param>
        /// <returns>The executed query result.</returns>
        public async Task<QuerySearchResult> ExecutePlanAsync(QueryPlan plan, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(plan);

            try
            {
                // Route supplied plans through the same execution adapter so the host never bypasses application-service orchestration.
                return await ExecutePlanCoreAsync(plan, isSuppliedPlan: true, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Log supplied-plan failures distinctly so contributors can tell whether planning or direct execution was being exercised.
                _logger.LogError(ex, "Failed to execute a caller-supplied query plan.");
                throw;
            }
        }

        /// <summary>
        /// Executes a query plan through the injected executor and emits a consistent structured log entry.
        /// </summary>
        /// <param name="plan">The repository-owned query plan that should be executed.</param>
        /// <param name="isSuppliedPlan"><see langword="true"/> when the caller supplied the plan directly; otherwise <see langword="false"/>.</param>
        /// <param name="cancellationToken">The cancellation token that stops execution when the caller no longer needs the result.</param>
        /// <returns>The executed query result.</returns>
        private async Task<QuerySearchResult> ExecutePlanCoreAsync(QueryPlan plan, bool isSuppliedPlan, CancellationToken cancellationToken)
        {
            // Execute the repository-owned plan through the injected infrastructure adapter so all host paths converge on the same runtime behavior.
            var result = await _queryPlanExecutor.SearchAsync(plan, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Executed query search. Source={Source} Total={Total} DurationMs={DurationMs}",
                isSuppliedPlan ? "edited-plan" : "raw-query",
                result.Total,
                result.Duration.TotalMilliseconds);

            return result;
        }
    }
}
