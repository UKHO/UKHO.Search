using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.Query.Models;
using UKHO.Search.Services.Query.Normalization;
using UKHO.Search.Services.Query.Rules;
using UKHO.Search.Services.Query.Tests.TestSupport;
using Xunit;

namespace UKHO.Search.Services.Query.Tests
{
    /// <summary>
    /// Verifies the flat query-rule engine behavior for predicate matching, concept expansion, sort hints, and consumption semantics.
    /// </summary>
    public sealed class ConfigurationQueryRuleEngineTests
    {
        /// <summary>
        /// Verifies that the latest-SOLAS rule set emits concept expansions, sort hints, matched rule ids, and fully consumed residual content.
        /// </summary>
        [Fact]
        public async Task EvaluateAsync_when_latest_solas_matches_emits_keyword_expansions_sorts_and_empty_residual_content()
        {
            // Normalize a representative query so the rule engine sees the same input shape used by the real planner.
            var input = new QueryTextNormalizer().Normalize("latest SOLAS");
            var engine = new ConfigurationQueryRuleEngine(
                new FixedQueryRulesCatalog(CreateLatestSolasSnapshot()),
                NullLogger<ConfigurationQueryRuleEngine>.Instance);

            // Evaluate the real rule engine against the representative query.
            var result = await engine.EvaluateAsync(input, new QueryExtractedSignals(), new CanonicalQueryModel(), CancellationToken.None);

            result.Model.Keywords.ShouldBe(["solas", "maritime", "safety", "msi"]);
            result.Extracted.Concepts.Count.ShouldBe(1);
            result.Extracted.Concepts.Single().MatchedText.ShouldBe("solas");
            result.Extracted.SortHints.Count.ShouldBe(1);
            result.Extracted.SortHints.Single().Fields.ShouldBe(["majorVersion", "minorVersion"]);
            result.Execution.Sorts.Select(static sort => sort.FieldName).ShouldBe(["majorVersion", "minorVersion"]);
            result.ResidualTokens.ShouldBeEmpty();
            result.ResidualText.ShouldBeEmpty();
            result.Diagnostics.MatchedRuleIds.ShouldBe(["concept-solas", "sort-latest"]);
        }

        /// <summary>
        /// Verifies that the any-group latest rule matches the multi-token phrase and removes only the recognized phrase from the residual token stream.
        /// </summary>
        [Fact]
        public async Task EvaluateAsync_when_any_group_matches_most_recent_consumes_phrase_and_preserves_remaining_tokens()
        {
            // Normalize a representative multi-token phrase so the rule engine can prove phrase-window matching and residual token preservation.
            var input = new QueryTextNormalizer().Normalize("most recent notices");
            var engine = new ConfigurationQueryRuleEngine(
                new FixedQueryRulesCatalog(CreateLatestOnlySnapshot()),
                NullLogger<ConfigurationQueryRuleEngine>.Instance);

            // Evaluate the real rule engine against the multi-token phrase query.
            var result = await engine.EvaluateAsync(input, new QueryExtractedSignals(), new CanonicalQueryModel(), CancellationToken.None);

            result.Extracted.SortHints.Count.ShouldBe(1);
            result.Extracted.SortHints.Single().MatchedText.ShouldBe("most recent");
            result.Execution.Sorts.Select(static sort => sort.FieldName).ShouldBe(["majorVersion", "minorVersion"]);
            result.ResidualTokens.ShouldBe(["notices"]);
            result.ResidualText.ShouldBe("notices");
            result.Diagnostics.MatchedRuleIds.ShouldBe(["sort-latest"]);
        }

        /// <summary>
        /// Verifies that explicit filters and boosts are carried into execution directives and surfaced in diagnostics when the matching rule fires.
        /// </summary>
        [Fact]
        public async Task EvaluateAsync_when_rule_contains_filters_and_boosts_emits_execution_directives_and_diagnostics()
        {
            // Normalize a representative notice query so the rule engine can prove explicit filter and boost handling.
            var input = new QueryTextNormalizer().Normalize("latest notice");
            var engine = new ConfigurationQueryRuleEngine(
                new FixedCatalogDiagnosticsQueryRulesCatalog(
                    new QueryRulesSnapshot
                    {
                        SchemaVersion = "1.0",
                        Rules = [new QueryRuleDefinition
                        {
                            Id = "filter-notice-latest",
                            Title = "Filter and boost latest notices",
                            Predicate = new QueryRulePredicate
                            {
                                Kind = QueryRulePredicateKind.ContainsPhrase,
                                Path = "input.cleanedText",
                                Value = "notice"
                            },
                            Filters = [
                                new QueryRuleFilterDefinition
                                {
                                    FieldName = "category",
                                    FieldKind = QueryRuleFilterFieldKind.String,
                                    StringValues = ["notice"]
                                },
                                new QueryRuleFilterDefinition
                                {
                                    FieldName = "majorVersion",
                                    FieldKind = QueryRuleFilterFieldKind.Integer,
                                    IntegerValues = [2024]
                                }],
                            Boosts = [
                                new QueryRuleBoostDefinition
                                {
                                    FieldName = "searchText",
                                    MatchingMode = QueryExecutionBoostMatchingMode.AnalyzedText,
                                    Text = "notice",
                                    Weight = 4.0d
                                },
                                new QueryRuleBoostDefinition
                                {
                                    FieldName = "keywords",
                                    MatchingMode = QueryExecutionBoostMatchingMode.ExactTerms,
                                    StringValues = ["notice"],
                                    Weight = 2.0d
                                }]
                        }]
                    },
                    new QueryRulesCatalogDiagnostics
                    {
                        LoadedAtUtc = new DateTimeOffset(2025, 1, 15, 8, 30, 0, TimeSpan.Zero),
                        RuleCount = 1
                    }),
                NullLogger<ConfigurationQueryRuleEngine>.Instance);

            // Evaluate the real rule engine against the representative query.
            var result = await engine.EvaluateAsync(input, new QueryExtractedSignals(), new CanonicalQueryModel(), CancellationToken.None);

            result.Execution.Filters.Count.ShouldBe(2);
            result.Execution.Filters.Single(filter => filter.FieldName == "category").StringValues.ShouldBe(["notice"]);
            result.Execution.Filters.Single(filter => filter.FieldName == "majorVersion").IntegerValues.ShouldBe([2024]);
            result.Execution.Boosts.Count.ShouldBe(2);
            result.Execution.Boosts.Single(boost => boost.FieldName == "searchText").Text.ShouldBe("notice");
            result.Execution.Boosts.Single(boost => boost.FieldName == "keywords").StringValues.ShouldBe(["notice"]);
            result.Diagnostics.AppliedFilters.ShouldBe(["category=[notice]", "majorVersion=[2024]"]);
            result.Diagnostics.AppliedBoosts.ShouldBe(["searchText:analyzed:notice:4", "keywords:exact:[notice]:2"]);
            result.Diagnostics.RuleCatalogLoadedAtUtc.ShouldBe(new DateTimeOffset(2025, 1, 15, 8, 30, 0, TimeSpan.Zero));
        }

        /// <summary>
        /// Creates the validated query-rule snapshot used for the latest-SOLAS unit scenario.
        /// </summary>
        /// <returns>The validated query-rule snapshot used by the tests.</returns>
        private static QueryRulesSnapshot CreateLatestSolasSnapshot()
        {
            // Reuse the same representative rules that the work item writes under rules/query so tests and runtime stay aligned.
            return new QueryRulesSnapshot
            {
                SchemaVersion = "1.0",
                Rules = [CreateConceptSolasRule(), CreateSortLatestRule()]
            };
        }

        /// <summary>
        /// Creates the validated query-rule snapshot used for the latest-only unit scenario.
        /// </summary>
        /// <returns>The validated query-rule snapshot used by the tests.</returns>
        private static QueryRulesSnapshot CreateLatestOnlySnapshot()
        {
            // Keep the snapshot focused on the phrase rule so the test isolates any-group phrase matching behavior.
            return new QueryRulesSnapshot
            {
                SchemaVersion = "1.0",
                Rules = [CreateSortLatestRule()]
            };
        }

        /// <summary>
        /// Creates the validated concept rule used by the latest-SOLAS scenarios.
        /// </summary>
        /// <returns>The validated concept rule definition.</returns>
        private static QueryRuleDefinition CreateConceptSolasRule()
        {
            // Mirror the concept-solas repository rule so the test proves concept expansion and token consumption semantics.
            return new QueryRuleDefinition
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
            };
        }

        /// <summary>
        /// Creates the validated latest-sort rule used by the rule-engine scenarios.
        /// </summary>
        /// <returns>The validated latest-sort rule definition.</returns>
        private static QueryRuleDefinition CreateSortLatestRule()
        {
            // Mirror the sort-latest repository rule so the test proves phrase matching, sort materialization, and phrase consumption semantics.
            return new QueryRuleDefinition
            {
                Id = "sort-latest",
                Title = "Recognize latest intent",
                Predicate = new QueryRulePredicate
                {
                    Kind = QueryRulePredicateKind.Any,
                    Any = [
                        new QueryRulePredicate
                        {
                            Kind = QueryRulePredicateKind.ContainsPhrase,
                            Path = "input.cleanedText",
                            Value = "latest"
                        },
                        new QueryRulePredicate
                        {
                            Kind = QueryRulePredicateKind.ContainsPhrase,
                            Path = "input.cleanedText",
                            Value = "most recent"
                        }]
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
                    Phrases = ["latest", "most recent"]
                }
            };
        }
    }
}
