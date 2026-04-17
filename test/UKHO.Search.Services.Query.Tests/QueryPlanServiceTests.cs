using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.Query.Models;
using UKHO.Search.Services.Query.Planning;
using UKHO.Search.Services.Query.Normalization;
using UKHO.Search.Services.Query.Rules;
using UKHO.Search.Services.Query.Tests.TestSupport;
using Xunit;

namespace UKHO.Search.Services.Query.Tests
{
    /// <summary>
    /// Verifies the query planning behavior for residual defaults, typed extraction projection, and recognizer fallback handling.
    /// </summary>
    public sealed class QueryPlanServiceTests
    {
        /// <summary>
        /// Verifies that the planner emits residual keyword and analyzed-text default contributions in the expected order and with the expected boosts.
        /// </summary>
        [Fact]
        public async Task PlanAsync_when_query_contains_residual_content_emits_keyword_and_analyzed_defaults()
        {
            // Compose the real planner with deterministic no-op collaborators because slice one still keeps extraction and rules behind interfaces.
            var service = new QueryPlanService(
                new QueryTextNormalizer(),
                new EmptyTypedQuerySignalExtractor(),
                new PassThroughQueryRuleEngine(),
                NullLogger<QueryPlanService>.Instance);

            // Plan a representative query so the resulting plan shape can be verified end to end.
            var plan = await service.PlanAsync("Latest SOLAS notice", CancellationToken.None);

            plan.Input.CleanedText.ShouldBe("latest solas notice");
            plan.Model.SearchText.ShouldBeEmpty();
            plan.Defaults.Items.Count.ShouldBe(3);
            plan.Defaults.Items.ElementAt(0).FieldName.ShouldBe("keywords");
            plan.Defaults.Items.ElementAt(0).Terms.ShouldBe(["latest", "solas", "notice"]);
            plan.Defaults.Items.ElementAt(1).FieldName.ShouldBe("searchText");
            plan.Defaults.Items.ElementAt(1).Text.ShouldBe("latest solas notice");
            plan.Defaults.Items.ElementAt(1).Boost.ShouldBe(2.0d);
            plan.Defaults.Items.ElementAt(2).FieldName.ShouldBe("content");
            plan.Defaults.Items.ElementAt(2).Text.ShouldBe("latest solas notice");
            plan.Defaults.Items.ElementAt(2).Boost.ShouldBe(1.0d);
            plan.Diagnostics.MatchedRuleIds.ShouldBeEmpty();
        }

        /// <summary>
        /// Verifies that recognized years are preserved in the extracted temporal contract and projected into the canonical majorVersion field.
        /// </summary>
        [Fact]
        public async Task PlanAsync_when_typed_extraction_returns_years_projects_them_into_major_version_without_changing_defaults()
        {
            // Compose the planner with a deterministic typed extractor so the service can be verified independently from recognizer implementation details.
            var service = new QueryPlanService(
                new QueryTextNormalizer(),
                new FixedTypedQuerySignalExtractor(new QueryExtractedSignals
                {
                    Temporal = new QueryTemporalSignals
                    {
                        Years = [2024]
                    }
                }),
                new PassThroughQueryRuleEngine(),
                NullLogger<QueryPlanService>.Instance);

            // Plan a year-bearing query so the typed extraction projection and the residual default behavior can be checked together.
            var plan = await service.PlanAsync("latest notice from 2024", CancellationToken.None);

            plan.Extracted.Temporal.Years.ShouldBe([2024]);
            plan.Model.MajorVersion.ShouldBe([2024]);
            plan.Defaults.Items.Count.ShouldBe(3);
            plan.Defaults.Items.ElementAt(0).Terms.ShouldBe(["latest", "notice", "from", "2024"]);
            plan.Defaults.Items.ElementAt(1).Text.ShouldBe("latest notice from 2024");
            plan.Defaults.Items.ElementAt(2).Text.ShouldBe("latest notice from 2024");
        }

        /// <summary>
        /// Verifies that the planner degrades to an empty extracted-signal contract when typed extraction fails.
        /// </summary>
        [Fact]
        public async Task PlanAsync_when_typed_extraction_throws_returns_default_only_plan()
        {
            // Compose the planner with a throwing extractor so the failure-handling path can be asserted directly.
            var service = new QueryPlanService(
                new QueryTextNormalizer(),
                new ThrowingTypedQuerySignalExtractor(),
                new PassThroughQueryRuleEngine(),
                NullLogger<QueryPlanService>.Instance);

            // Plan a representative query so the fallback result can be verified after the extraction failure is swallowed.
            var plan = await service.PlanAsync("2024", CancellationToken.None);

            plan.Extracted.Temporal.Years.ShouldBeEmpty();
            plan.Extracted.Temporal.Dates.ShouldBeEmpty();
            plan.Extracted.Numbers.ShouldBeEmpty();
            plan.Model.MajorVersion.ShouldBeEmpty();
            plan.Defaults.Items.ElementAt(0).Terms.ShouldBe(["2024"]);
            plan.Defaults.Items.ElementAt(1).Text.ShouldBe("2024");
            plan.Defaults.Items.ElementAt(2).Text.ShouldBe("2024");
        }

        /// <summary>
        /// Verifies that the real rule engine produces the latest-SOLAS plan shape with rule-driven keyword expansion, sort directives, and empty residual defaults.
        /// </summary>
        [Fact]
        public async Task PlanAsync_when_latest_solas_matches_rule_catalog_emits_rule_driven_plan_shape()
        {
            // Compose the planner with the real rule engine and a deterministic validated rule snapshot so the full service integration can be asserted.
            var service = new QueryPlanService(
                new QueryTextNormalizer(),
                new EmptyTypedQuerySignalExtractor(),
                new ConfigurationQueryRuleEngine(
                    new FixedQueryRulesCatalog(new QueryRulesSnapshot
                    {
                        SchemaVersion = "1.0",
                        Rules = [
                            new QueryRuleDefinition
                            {
                                Id = "concept-solas",
                                Title = "Recognize SOLAS concept",
                                Predicate = new QueryRulePredicate
                                {
                                    Kind = QueryRulePredicateKind.Equals,
                                    Path = "input.tokens[*]",
                                    Value = "solas"
                                },
                                ModelMutations = [new QueryRuleModelMutation
                                {
                                    FieldName = "keywords",
                                    AddValues = ["solas", "maritime", "safety", "msi"]
                                }],
                                Concepts = [new QueryRuleConceptDefinition
                                {
                                    Id = "solas",
                                    MatchedTextTemplate = "$val",
                                    KeywordExpansions = ["solas", "maritime", "safety", "msi"]
                                }],
                                Consume = new QueryRuleConsumeDefinition
                                {
                                    Tokens = ["solas"]
                                }
                            },
                            new QueryRuleDefinition
                            {
                                Id = "sort-latest",
                                Title = "Recognize latest intent",
                                Predicate = new QueryRulePredicate
                                {
                                    Kind = QueryRulePredicateKind.ContainsPhrase,
                                    Path = "input.cleanedText",
                                    Value = "latest"
                                },
                                SortHints = [new QueryRuleSortHintDefinition
                                {
                                    Id = "latest",
                                    MatchedTextTemplate = "$val",
                                    Fields = ["majorVersion", "minorVersion"],
                                    Direction = QueryExecutionSortDirection.Descending
                                }],
                                Consume = new QueryRuleConsumeDefinition
                                {
                                    Phrases = ["latest"]
                                }
                            }]
                    }),
                    NullLogger<ConfigurationQueryRuleEngine>.Instance),
                NullLogger<QueryPlanService>.Instance);

            // Plan the representative rule-driven query end to end through normalization, typed extraction, rule evaluation, and default mapping.
            var plan = await service.PlanAsync("latest SOLAS", CancellationToken.None);

            plan.Model.Keywords.ShouldBe(["solas", "maritime", "safety", "msi"]);
            plan.Extracted.Concepts.Count.ShouldBe(1);
            plan.Extracted.SortHints.Count.ShouldBe(1);
            plan.Execution.Sorts.Select(static sort => sort.FieldName).ShouldBe(["majorVersion", "minorVersion"]);
            plan.Defaults.Items.ShouldBeEmpty();
            plan.Diagnostics.MatchedRuleIds.ShouldBe(["concept-solas", "sort-latest"]);
        }

        /// <summary>
        /// Verifies that the planner retains explicit filters, boosts, and richer diagnostics emitted by the rule engine.
        /// </summary>
        [Fact]
        public async Task PlanAsync_when_rule_engine_emits_filters_and_boosts_retains_execution_directives_and_diagnostics()
        {
            // Compose the planner with a deterministic rule engine snapshot so the richer execution directives can be asserted end to end.
            var service = new QueryPlanService(
                new QueryTextNormalizer(),
                new EmptyTypedQuerySignalExtractor(),
                new ConfigurationQueryRuleEngine(
                    new FixedCatalogDiagnosticsQueryRulesCatalog(
                        new QueryRulesSnapshot
                        {
                            SchemaVersion = "1.0",
                            Rules = [new QueryRuleDefinition
                            {
                                Id = "filter-latest-notice",
                                Title = "Filter and boost notice queries",
                                Predicate = new QueryRulePredicate
                                {
                                    Kind = QueryRulePredicateKind.ContainsPhrase,
                                    Path = "input.cleanedText",
                                    Value = "notice"
                                },
                                Filters = [new QueryRuleFilterDefinition
                                {
                                    FieldName = "category",
                                    FieldKind = QueryRuleFilterFieldKind.String,
                                    StringValues = ["notice"]
                                }],
                                Boosts = [new QueryRuleBoostDefinition
                                {
                                    FieldName = "searchText",
                                    MatchingMode = QueryExecutionBoostMatchingMode.AnalyzedText,
                                    Text = "notice",
                                    Weight = 4.0d
                                }]
                            }]
                        },
                        new QueryRulesCatalogDiagnostics
                        {
                            LoadedAtUtc = new DateTimeOffset(2025, 1, 15, 8, 30, 0, TimeSpan.Zero),
                            RuleCount = 1
                        }),
                    NullLogger<ConfigurationQueryRuleEngine>.Instance),
                NullLogger<QueryPlanService>.Instance);

            // Plan a representative notice query so the richer execution directives flow through the planner into the final query plan.
            var plan = await service.PlanAsync("latest notice", CancellationToken.None);

            plan.Execution.Filters.Count.ShouldBe(1);
            plan.Execution.Filters.Single().FieldName.ShouldBe("category");
            plan.Execution.Boosts.Count.ShouldBe(1);
            plan.Execution.Boosts.Single().FieldName.ShouldBe("searchText");
            plan.Diagnostics.AppliedFilters.ShouldBe(["category=[notice]"]);
            plan.Diagnostics.AppliedBoosts.ShouldBe(["searchText:analyzed:notice:4"]);
            plan.Diagnostics.RuleCatalogLoadedAtUtc.ShouldBe(new DateTimeOffset(2025, 1, 15, 8, 30, 0, TimeSpan.Zero));
        }
    }
}
