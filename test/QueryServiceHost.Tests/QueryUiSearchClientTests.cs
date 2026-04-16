using Microsoft.Extensions.Logging.Abstractions;
using QueryServiceHost.Models;
using QueryServiceHost.Services;
using Shouldly;
using UKHO.Search.Query.Models;
using UKHO.Search.Query.Results;
using Xunit;

namespace QueryServiceHost.Tests
{
    /// <summary>
    /// Verifies that the host adapter projects the repository-owned query result into the richer response required by the query workspace shell.
    /// </summary>
    public sealed class QueryUiSearchClientTests
    {
        /// <summary>
        /// Verifies that a successful repository query result is projected into host hits and a formatted generated-plan payload for Monaco.
        /// </summary>
        [Fact]
        public async Task SearchAsync_projects_hits_duration_and_generated_plan_json()
        {
            // Return a repository query result that includes both a plan and a hit so the host projection can be verified end to end.
            var repositoryResult = new QuerySearchResult
            {
                Plan = CreatePlan(rawText: "wreck 42"),
                ElasticsearchRequestJson = "{\"query\":{\"bool\":{\"should\":[{\"terms\":{\"keywords\":[\"wreck\",\"42\"]}}]}}}",
                Hits =
                [
                    new QuerySearchHit
                    {
                        Title = "Wreck 42",
                        Type = "Wreck",
                        Region = "North Sea",
                        MatchedFields = ["title", "keywords"]
                    }
                ],
                Total = 1,
                Duration = TimeSpan.FromMilliseconds(123),
                SearchEngineDuration = TimeSpan.FromMilliseconds(61),
                Warnings = ["The query executed without any rule-authored boost directives."]
            };

            var searchService = new DelegatingQuerySearchService((queryText, _) =>
            {
                queryText.ShouldBe("wreck 42");
                return Task.FromResult(repositoryResult);
            });

            var client = new QueryUiSearchClient(searchService, NullLogger<QueryUiSearchClient>.Instance);

            // Execute the adapter path that feeds the host-local response model.
            var response = await client.SearchAsync(new QueryRequest { QueryText = "wreck 42" }, CancellationToken.None);

            response.Total.ShouldBe(1);
            response.Duration.ShouldBe(repositoryResult.Duration);
            response.SearchEngineDuration.ShouldBe(repositoryResult.SearchEngineDuration);
            response.Hits.Count.ShouldBe(1);
            response.Hits[0].Title.ShouldBe("Wreck 42");
            response.Hits[0].MatchedFields.ShouldBe(["title", "keywords"]);
            response.Plan.ShouldBeSameAs(repositoryResult.Plan);
            response.GeneratedPlanJson.ShouldContain("\"RawText\": \"wreck 42\"");
            response.GeneratedPlanJson.ShouldContain("\"MatchedRuleIds\"");
            response.ElasticsearchRequestJson.ShouldContain("\"query\"");
            response.ElasticsearchRequestJson.ShouldContain(Environment.NewLine);
            response.Warnings.ShouldBe(["The query executed without any rule-authored boost directives."]);
            response.UsedEditedPlan.ShouldBeFalse();
        }

        /// <summary>
        /// Verifies that edited-plan execution is routed through the supplied-plan application-service path and still projects a formatted plan payload for Monaco.
        /// </summary>
        [Fact]
        public async Task ExecutePlanAsync_projects_edited_plan_results_and_marks_the_response_source()
        {
            // Return a repository query result that reflects the caller-supplied plan so the adapter can be verified end to end.
            var suppliedPlan = CreatePlan(rawText: "edited wreck 42");
            var repositoryResult = new QuerySearchResult
            {
                Plan = suppliedPlan,
                ElasticsearchRequestJson = "{\"query\":{\"bool\":{\"should\":[{\"terms\":{\"keywords\":[\"edited\"]}}]}}}",
                Hits =
                [
                    new QuerySearchHit
                    {
                        Title = "Edited Wreck 42",
                        Type = "Wreck",
                        Region = "North Sea",
                        MatchedFields = ["keywords"]
                    }
                ],
                Total = 1,
                Duration = TimeSpan.FromMilliseconds(87),
                SearchEngineDuration = TimeSpan.FromMilliseconds(33)
            };

            var searchService = new DelegatingQuerySearchService(
                (_, _) => Task.FromException<QuerySearchResult>(new InvalidOperationException("Raw-query execution should not be used in this test.")),
                (plan, _) =>
                {
                    plan.ShouldBeSameAs(suppliedPlan);
                    return Task.FromResult(repositoryResult);
                });

            var client = new QueryUiSearchClient(searchService, NullLogger<QueryUiSearchClient>.Instance);

            // Execute the edited-plan adapter path that feeds the host-local response model.
            var response = await client.ExecutePlanAsync(suppliedPlan, CancellationToken.None);

            response.Total.ShouldBe(1);
            response.Duration.ShouldBe(repositoryResult.Duration);
            response.SearchEngineDuration.ShouldBe(repositoryResult.SearchEngineDuration);
            response.Hits.Count.ShouldBe(1);
            response.Hits[0].Title.ShouldBe("Edited Wreck 42");
            response.GeneratedPlanJson.ShouldContain("\"RawText\": \"edited wreck 42\"");
            response.ElasticsearchRequestJson.ShouldContain("\"edited\"");
            response.UsedEditedPlan.ShouldBeTrue();
        }

        /// <summary>
        /// Creates a minimal repository-owned query plan suitable for host projection tests.
        /// </summary>
        /// <param name="rawText">The raw query text that should appear in the generated JSON plan.</param>
        /// <returns>A deterministic query plan instance.</returns>
        private static QueryPlan CreatePlan(string rawText)
        {
            // Populate only the required query-plan sections so the projection test stays focused on host serialization behavior.
            return new QueryPlan
            {
                Input = new QueryInputSnapshot
                {
                    RawText = rawText,
                    NormalizedText = rawText.ToLowerInvariant(),
                    CleanedText = rawText,
                    Tokens = ["wreck", "42"],
                    ResidualTokens = ["wreck", "42"],
                    ResidualText = rawText
                },
                Extracted = new QueryExtractedSignals(),
                Model = new CanonicalQueryModel
                {
                    Keywords = ["wreck"]
                },
                Defaults = new QueryDefaultContributions(),
                Execution = new QueryExecutionDirectives(),
                Diagnostics = new QueryPlanDiagnostics
                {
                    MatchedRuleIds = ["query-ui-shell-test-rule"]
                }
            };
        }
    }
}
