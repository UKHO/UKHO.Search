using System.Text.Json;
using Shouldly;
using UKHO.Search.Infrastructure.Query.Search;
using UKHO.Search.Query.Models;
using Xunit;

namespace UKHO.Search.Infrastructure.Query.Tests
{
    /// <summary>
    /// Verifies the Elasticsearch request-body mapping introduced for the query-side default planning slice.
    /// </summary>
    public sealed class ElasticsearchQueryMapperTests
    {
        /// <summary>
        /// Verifies that the mapper emits keyword and analyzed-text clauses and preserves the required search-text boost.
        /// </summary>
        [Fact]
        public void CreateRequestBody_when_plan_contains_default_contributions_emits_expected_bool_query()
        {
            // Build a representative query plan because the mapper should only depend on repository-owned plan contracts.
            var plan = CreatePlan([
                new QueryDefaultFieldContribution
                {
                    FieldName = "keywords",
                    MatchingMode = QueryDefaultMatchingMode.ExactTerms,
                    Terms = ["latest", "solas"],
                    Boost = 1.0d
                },
                new QueryDefaultFieldContribution
                {
                    FieldName = "searchText",
                    MatchingMode = QueryDefaultMatchingMode.AnalyzedText,
                    Text = "latest solas",
                    Boost = 2.0d
                },
                new QueryDefaultFieldContribution
                {
                    FieldName = "content",
                    MatchingMode = QueryDefaultMatchingMode.AnalyzedText,
                    Text = "latest solas",
                    Boost = 1.0d
                }
            ]);

            // Generate the Elasticsearch request body so the emitted JSON can be validated directly.
            var requestBody = ElasticsearchQueryMapper.CreateRequestBody(plan);
            using var document = JsonDocument.Parse(requestBody);
            var root = document.RootElement;

            root.GetProperty("size").GetInt32().ShouldBe(25);
            var shouldClauses = root.GetProperty("query").GetProperty("bool").GetProperty("should");
            shouldClauses.GetArrayLength().ShouldBe(3);
            root.GetProperty("query").GetProperty("bool").GetProperty("minimum_should_match").GetInt32().ShouldBe(1);

            shouldClauses[0].GetProperty("terms").GetProperty("keywords").EnumerateArray().Select(static item => item.GetString()).ShouldBe(["latest", "solas"]);
            shouldClauses[1].GetProperty("match").GetProperty("searchText").GetProperty("boost").GetDouble().ShouldBe(2.0d);
            shouldClauses[2].GetProperty("match").GetProperty("content").GetProperty("boost").GetDouble().ShouldBe(1.0d);
        }

        /// <summary>
        /// Verifies that the mapper emits a match-none query when the plan contains no executable default contributions.
        /// </summary>
        [Fact]
        public void CreateRequestBody_when_plan_contains_no_default_contributions_emits_match_none()
        {
            // Build an empty plan because the executor must handle empty plans safely.
            var plan = CreatePlan([]);

            // Generate the request body so the empty-plan fallback can be validated directly.
            var requestBody = ElasticsearchQueryMapper.CreateRequestBody(plan);
            using var document = JsonDocument.Parse(requestBody);

            document.RootElement.GetProperty("query").TryGetProperty("match_none", out var matchNone).ShouldBeTrue();
            matchNone.ValueKind.ShouldBe(JsonValueKind.Object);
        }

        /// <summary>
        /// Verifies that the mapper emits canonical-model keyword clauses and execution sorts when rules shape the plan directly.
        /// </summary>
        [Fact]
        public void CreateRequestBody_when_plan_contains_model_keywords_and_sorts_emits_terms_clause_and_sort_array()
        {
            // Build a representative rule-driven plan because the mapper must execute canonical model values even when residual defaults are empty.
            var plan = CreatePlan(
                [],
                new CanonicalQueryModel
                {
                    Keywords = ["solas", "maritime", "safety", "msi"]
                },
                new QueryExecutionDirectives
                {
                    Sorts = [
                        new QueryExecutionSortDirective
                        {
                            FieldName = "majorVersion",
                            Direction = QueryExecutionSortDirection.Descending
                        },
                        new QueryExecutionSortDirective
                        {
                            FieldName = "minorVersion",
                            Direction = QueryExecutionSortDirection.Descending
                        }]
                });

            // Generate the Elasticsearch request body so the canonical-model clause and execution sorts can be validated directly.
            var requestBody = ElasticsearchQueryMapper.CreateRequestBody(plan);
            using var document = JsonDocument.Parse(requestBody);
            var root = document.RootElement;

            var shouldClauses = root.GetProperty("query").GetProperty("bool").GetProperty("should");
            shouldClauses.GetArrayLength().ShouldBe(1);
            shouldClauses[0].GetProperty("terms").GetProperty("keywords").EnumerateArray().Select(static item => item.GetString()).ShouldBe(["solas", "maritime", "safety", "msi"]);
            root.GetProperty("sort")[0].GetProperty("majorVersion").GetProperty("order").GetString().ShouldBe("desc");
            root.GetProperty("sort")[1].GetProperty("minorVersion").GetProperty("order").GetString().ShouldBe("desc");
        }

        /// <summary>
        /// Verifies that explicit execution-time filters and boosts are mapped into deterministic Elasticsearch filter and scoring clauses.
        /// </summary>
        [Fact]
        public void CreateRequestBody_when_plan_contains_filters_and_boosts_emits_filter_array_and_boost_clauses()
        {
            // Build a representative rule-driven plan because work item four adds explicit filters and boosts to the execution contract.
            var plan = CreatePlan(
                [],
                execution: new QueryExecutionDirectives
                {
                    Filters = [
                        new QueryExecutionFilterDirective
                        {
                            FieldName = "category",
                            StringValues = ["notice"]
                        },
                        new QueryExecutionFilterDirective
                        {
                            FieldName = "majorVersion",
                            IntegerValues = [2024]
                        }],
                    Boosts = [
                        new QueryExecutionBoostDirective
                        {
                            FieldName = "keywords",
                            MatchingMode = QueryExecutionBoostMatchingMode.ExactTerms,
                            StringValues = ["notice"],
                            Weight = 3.0d
                        },
                        new QueryExecutionBoostDirective
                        {
                            FieldName = "searchText",
                            MatchingMode = QueryExecutionBoostMatchingMode.AnalyzedText,
                            Text = "notice",
                            Weight = 5.0d
                        }]
                });

            // Generate the Elasticsearch request body so the explicit filter and boost clauses can be validated directly.
            var requestBody = ElasticsearchQueryMapper.CreateRequestBody(plan);
            using var document = JsonDocument.Parse(requestBody);
            var query = document.RootElement.GetProperty("query");
            query.ValueKind.ShouldBe(JsonValueKind.Object);
            var propertyNames = query.EnumerateObject().Select(static property => property.Name).ToArray();
            propertyNames.ShouldContain("bool");
            var boolQuery = query.GetProperty("bool");

            boolQuery.GetProperty("filter").GetArrayLength().ShouldBe(2);
            boolQuery.GetProperty("filter")[0].GetProperty("terms").GetProperty("category").EnumerateArray().Select(static item => item.GetString()).ShouldBe(["notice"]);
            boolQuery.GetProperty("filter")[1].GetProperty("terms").GetProperty("majorVersion").EnumerateArray().Select(static item => item.GetInt32()).ShouldBe([2024]);
            boolQuery.GetProperty("should").GetArrayLength().ShouldBe(2);
            boolQuery.GetProperty("should")[0].GetProperty("terms").GetProperty("boost").GetDouble().ShouldBe(3.0d);
            boolQuery.GetProperty("should")[1].GetProperty("match").GetProperty("searchText").GetProperty("boost").GetDouble().ShouldBe(5.0d);
        }

        /// <summary>
        /// Creates a minimal repository-owned query plan for mapper tests.
        /// </summary>
        /// <param name="defaults">The default contributions that should be retained on the test plan.</param>
        /// <param name="model">The canonical query model that should be retained on the test plan.</param>
        /// <param name="execution">The execution directives that should be retained on the test plan.</param>
        /// <returns>The test query plan.</returns>
        private static QueryPlan CreatePlan(
            IReadOnlyCollection<QueryDefaultFieldContribution> defaults,
            CanonicalQueryModel? model = null,
            QueryExecutionDirectives? execution = null)
        {
            // Build only the plan data that the mapper actually consumes so the test stays focused on request-body translation.
            return new QueryPlan
            {
                Input = new QueryInputSnapshot(),
                Extracted = new QueryExtractedSignals(),
                Model = model ?? new CanonicalQueryModel(),
                Defaults = new QueryDefaultContributions { Items = defaults },
                Execution = execution ?? new QueryExecutionDirectives(),
                Diagnostics = new QueryPlanDiagnostics()
            };
        }
    }
}
