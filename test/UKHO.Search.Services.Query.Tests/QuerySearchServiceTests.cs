using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.Query.Models;
using UKHO.Search.Query.Results;
using UKHO.Search.Services.Query.Execution;
using UKHO.Search.Services.Query.Tests.TestSupport;
using Xunit;

namespace UKHO.Search.Services.Query.Tests
{
    /// <summary>
    /// Verifies the orchestration behavior that coordinates raw-query planning and caller-supplied-plan execution.
    /// </summary>
    public sealed class QuerySearchServiceTests
    {
        /// <summary>
        /// Verifies that raw-query execution plans the supplied text first and then executes the resulting repository-owned plan.
        /// </summary>
        [Fact]
        public async Task SearchAsync_plans_the_raw_query_before_executing_the_generated_plan()
        {
            // Build a deterministic plan so the test can verify the planner output is passed directly into the executor.
            var plannedQuery = CreatePlan("wreck 42");
            QueryPlan? executedPlan = null;
            string? plannedQueryText = null;
            var service = new QuerySearchService(
                new DelegatingQueryPlanService((queryText, _) =>
                {
                    plannedQueryText = queryText;
                    return Task.FromResult(plannedQuery);
                }),
                new DelegatingQueryPlanExecutor((plan, _) =>
                {
                    executedPlan = plan;
                    return Task.FromResult(CreateResult(plan, total: 2));
                }),
                NullLogger<QuerySearchService>.Instance);

            // Execute the raw-query path end to end through the application service.
            var result = await service.SearchAsync("wreck 42", CancellationToken.None);

            plannedQueryText.ShouldBe("wreck 42");
            executedPlan.ShouldBeSameAs(plannedQuery);
            result.Plan.ShouldBeSameAs(plannedQuery);
            result.Total.ShouldBe(2);
        }

        /// <summary>
        /// Verifies that supplied-plan execution bypasses planning and executes the caller-provided plan directly.
        /// </summary>
        [Fact]
        public async Task ExecutePlanAsync_executes_the_supplied_plan_without_replanning()
        {
            // Build a deterministic supplied plan so the test can verify the application service does not replace it with a newly planned instance.
            var suppliedPlan = CreatePlan("edited wreck 42");
            var planServiceCallCount = 0;
            QueryPlan? executedPlan = null;
            var service = new QuerySearchService(
                new DelegatingQueryPlanService((_, _) =>
                {
                    planServiceCallCount++;
                    return Task.FromResult(CreatePlan("unexpected replanning"));
                }),
                new DelegatingQueryPlanExecutor((plan, _) =>
                {
                    executedPlan = plan;
                    return Task.FromResult(CreateResult(plan, total: 1));
                }),
                NullLogger<QuerySearchService>.Instance);

            // Execute the supplied-plan path and verify that the original plan instance flows straight into the executor.
            var result = await service.ExecutePlanAsync(suppliedPlan, CancellationToken.None);

            planServiceCallCount.ShouldBe(0);
            executedPlan.ShouldBeSameAs(suppliedPlan);
            result.Plan.ShouldBeSameAs(suppliedPlan);
            result.Total.ShouldBe(1);
        }

        /// <summary>
        /// Creates a deterministic repository-owned query plan for orchestration tests.
        /// </summary>
        /// <param name="rawText">The raw query text that should be preserved inside the query plan.</param>
        /// <returns>A deterministic query plan instance.</returns>
        private static QueryPlan CreatePlan(string rawText)
        {
            // Populate only the required sections so the orchestration tests stay focused on routing rather than planner richness.
            return new QueryPlan
            {
                Input = new QueryInputSnapshot
                {
                    RawText = rawText,
                    NormalizedText = rawText.ToLowerInvariant(),
                    CleanedText = rawText,
                    Tokens = rawText.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                    ResidualTokens = rawText.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                    ResidualText = rawText
                },
                Extracted = new QueryExtractedSignals(),
                Model = new CanonicalQueryModel(),
                Defaults = new QueryDefaultContributions(),
                Execution = new QueryExecutionDirectives(),
                Diagnostics = new QueryPlanDiagnostics()
            };
        }

        /// <summary>
        /// Creates a deterministic repository-owned query result for orchestration tests.
        /// </summary>
        /// <param name="plan">The plan that should be echoed back in the result.</param>
        /// <param name="total">The total number of matches that the result should report.</param>
        /// <returns>A deterministic query result shaped for orchestration testing.</returns>
        private static QuerySearchResult CreateResult(QueryPlan plan, long total)
        {
            // Return a small result payload because these tests care only about orchestration and not Elasticsearch projection details.
            return new QuerySearchResult
            {
                Plan = plan,
                Total = total,
                Duration = TimeSpan.FromMilliseconds(42),
                Hits =
                [
                    new QuerySearchHit
                    {
                        Title = "Wreck 42",
                        MatchedFields = ["keywords"]
                    }
                ]
            };
        }
    }
}
