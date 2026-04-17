using Shouldly;
using UKHO.Search.Infrastructure.Query.Rules;
using UKHO.Search.Query.Models;
using Xunit;

namespace UKHO.Search.Infrastructure.Query.Tests
{
    /// <summary>
    /// Verifies validation of explicit rule-driven filters and boosts in the query rule DSL.
    /// </summary>
    public sealed class QueryRulesValidatorTests
    {
        /// <summary>
        /// Verifies that the validator accepts explicit filters and boosts and normalizes them into the runtime model.
        /// </summary>
        [Fact]
        public void Validate_when_rule_contains_filters_and_boosts_normalizes_them_into_runtime_definitions()
        {
            // Arrange a representative rule document because work item four adds explicit filter and boost sections to the rule DSL.
            var validator = new QueryRulesValidator();
            var documents = new[]
            {
                new QueryRuleDocumentDto
                {
                    SchemaVersion = "1.0",
                    Rule = new QueryRuleDto
                    {
                        Id = "filter-latest-notices",
                        Title = "Filter latest notices",
                        If = new QueryRulePredicateDto
                        {
                            Path = "input.cleanedText",
                            ContainsPhrase = "notice"
                        },
                        Then = new QueryRuleActionSetDto
                        {
                            Filters = new QueryRuleFilterDto
                            {
                                FieldActions = new Dictionary<string, System.Text.Json.JsonElement>(StringComparer.OrdinalIgnoreCase)
                                {
                                    ["category"] = System.Text.Json.JsonDocument.Parse("{\"add\":[\"notice\"]}").RootElement.Clone(),
                                    ["majorVersion"] = System.Text.Json.JsonDocument.Parse("{\"add\":[2024]}").RootElement.Clone()
                                }
                            },
                            Boosts = new[]
                            {
                                new QueryRuleBoostDto
                                {
                                    Field = "searchText",
                                    Text = "notice",
                                    MatchingMode = "analyzedText",
                                    Weight = 4.0d
                                },
                                new QueryRuleBoostDto
                                {
                                    Field = "keywords",
                                    Values = new[] { "notice" },
                                    Weight = 2.0d
                                }
                            }
                        }
                    }
                }
            };

            // Act by validating the representative rule document.
            var snapshot = validator.Validate(documents);
            var rule = snapshot.Rules.Single();

            // Assert that the explicit filters and boosts were retained in validated runtime form.
            rule.Filters.Count.ShouldBe(2);
            rule.Filters.Single(filter => filter.FieldName == "category").StringValues.ShouldBe(["notice"]);
            rule.Filters.Single(filter => filter.FieldName == "majorVersion").IntegerValues.ShouldBe([2024]);
            rule.Boosts.Count.ShouldBe(2);
            rule.Boosts.Single(boost => boost.FieldName == "searchText").MatchingMode.ShouldBe(QueryExecutionBoostMatchingMode.AnalyzedText);
            rule.Boosts.Single(boost => boost.FieldName == "searchText").Text.ShouldBe("notice");
            rule.Boosts.Single(boost => boost.FieldName == "keywords").StringValues.ShouldBe(["notice"]);
        }
    }
}
