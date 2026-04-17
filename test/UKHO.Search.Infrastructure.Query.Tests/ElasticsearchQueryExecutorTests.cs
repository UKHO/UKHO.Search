using Shouldly;
using UKHO.Search.Infrastructure.Query.Search;
using UKHO.Search.Query.Models;
using Xunit;

namespace UKHO.Search.Infrastructure.Query.Tests
{
    /// <summary>
    /// Verifies the Elasticsearch response parsing introduced for the query-side execution slice.
    /// </summary>
    public sealed class ElasticsearchQueryExecutorTests
    {
        /// <summary>
        /// Verifies that the executor treats rule-driven canonical model clauses as executable even when residual defaults are empty.
        /// </summary>
        [Fact]
        public void HasExecutableClauses_when_plan_contains_rule_driven_model_keywords_returns_true()
        {
            // Build a representative rule-driven plan because keyword expansion can produce executable clauses without any residual default content.
            var plan = new QueryPlan
            {
                Input = new QueryInputSnapshot(),
                Extracted = new QueryExtractedSignals(),
                Model = new CanonicalQueryModel
                {
                    Keywords = ["solas", "maritime", "safety", "msi"]
                },
                Defaults = new QueryDefaultContributions(),
                Execution = new QueryExecutionDirectives(),
                Diagnostics = new QueryPlanDiagnostics()
            };

            // The executor should treat the rule-driven canonical model as executable so model-only plans are not skipped.
            ElasticsearchQueryExecutor.HasExecutableClauses(plan).ShouldBeTrue();
        }

        /// <summary>
        /// Verifies that the executor maps the Elasticsearch response body into repository-owned hits and preserves matched-field metadata.
        /// </summary>
        [Fact]
        public void ParseResponseBody_when_response_contains_hits_maps_titles_regions_types_matched_fields_and_execution_metrics()
        {
            // Build a minimal query plan because the parsed result retains the originating plan for later diagnostics.
            var plan = new QueryPlan
            {
                Input = new QueryInputSnapshot(),
                Extracted = new QueryExtractedSignals(),
                Model = new CanonicalQueryModel(),
                Defaults = new QueryDefaultContributions(),
                Execution = new QueryExecutionDirectives(),
                Diagnostics = new QueryPlanDiagnostics()
            };

            const string requestBody = "{\"query\":{\"bool\":{\"should\":[{\"terms\":{\"keywords\":[\"latest\",\"solas\"]}}]}}}";

            const string responseBody = """
                {
                  "took": 18,
                  "hits": {
                    "total": { "value": 2 },
                    "hits": [
                      {
                        "_source": {
                          "title": ["Latest SOLAS notice"],
                          "region": ["north sea"],
                          "category": ["notice"]
                        },
                        "matched_queries": ["keywords", "searchText"]
                      },
                      {
                        "_source": {
                          "title": ["Fallback type example"],
                          "region": ["baltic"],
                          "format": ["publication"]
                        },
                        "matched_queries": ["content"]
                      }
                    ]
                  }
                }
                """;

            // Parse the response body so the repository-owned hit projection can be validated directly.
            var result = ElasticsearchQueryExecutor.ParseResponseBody(plan, requestBody, responseBody, TimeSpan.FromMilliseconds(42));

            result.Plan.ShouldBe(plan);
            result.ElasticsearchRequestJson.ShouldBe(requestBody);
            result.Total.ShouldBe(2);
            result.Duration.ShouldBe(TimeSpan.FromMilliseconds(42));
            result.SearchEngineDuration.ShouldBe(TimeSpan.FromMilliseconds(18));
            result.Hits.Count.ShouldBe(2);
            result.Hits.ElementAt(0).Title.ShouldBe("Latest SOLAS notice");
            result.Hits.ElementAt(0).Region.ShouldBe("north sea");
            result.Hits.ElementAt(0).Type.ShouldBe("notice");
            result.Hits.ElementAt(0).MatchedFields.ShouldBe(["keywords", "searchText"]);
            result.Hits.ElementAt(1).Type.ShouldBe("publication");
            result.Hits.ElementAt(1).MatchedFields.ShouldBe(["content"]);
            result.Hits.ElementAt(0).Raw.ShouldNotBeNull();
        }
    }
}
