using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.Infrastructure.Query.Rules;
using Xunit;

namespace UKHO.Search.Infrastructure.Query.Tests
{
    /// <summary>
    /// Verifies how <see cref="AppConfigQueryRulesSource"/> enumerates flat App Configuration-backed query rules.
    /// </summary>
    public sealed class AppConfigQueryRulesSourceTests
    {
        /// <summary>
        /// Verifies that flat rules beneath the query namespace are projected as canonical namespace-aware keys.
        /// </summary>
        [Fact]
        public void ListRuleEntries_when_flat_query_rules_exist_returns_canonical_query_rule_keys()
        {
            // Arrange configuration values that mirror the App Configuration keys produced from flat rules/query files.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["rules:query:sort-latest"] = CreateRuleJson("sort-latest"),
                    ["rules:query:concept-solas"] = CreateRuleJson("concept-solas")
                })
                .Build();
            var source = new AppConfigQueryRulesSource(configuration, NullLogger<AppConfigQueryRulesSource>.Instance);

            // Enumerate the rule entries through the real App Configuration source implementation.
            var entries = source.ListRuleEntries().OrderBy(entry => entry.RuleId, StringComparer.Ordinal).ToArray();

            entries.Length.ShouldBe(2);
            entries[0].RuleId.ShouldBe("concept-solas");
            entries[0].Key.ShouldBe("rules:query:concept-solas");
            entries[1].RuleId.ShouldBe("sort-latest");
            entries[1].Key.ShouldBe("rules:query:sort-latest");
        }

        /// <summary>
        /// Verifies that legacy and nested query-rule keys are ignored because query rules must remain flat beneath the query namespace.
        /// </summary>
        [Fact]
        public void ListRuleEntries_when_legacy_or_nested_rules_exist_ignores_them()
        {
            // Arrange one supported flat rule, one legacy rule, and one nested rule path so the source contract is pinned precisely.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["rules:query:sort-latest"] = CreateRuleJson("sort-latest"),
                    ["rules:query:nested:concept-solas"] = CreateRuleJson("concept-solas"),
                    ["rules:file-share:legacy-rule"] = CreateRuleJson("legacy-rule")
                })
                .Build();
            var source = new AppConfigQueryRulesSource(configuration, NullLogger<AppConfigQueryRulesSource>.Instance);

            // Enumerate the active rules through the App Configuration source.
            var entries = source.ListRuleEntries();

            entries.Count.ShouldBe(1);
            entries[0].RuleId.ShouldBe("sort-latest");
            entries[0].Key.ShouldBe("rules:query:sort-latest");
        }

        /// <summary>
        /// Creates a minimal wrapped query-rule document for App Configuration-backed tests.
        /// </summary>
        /// <param name="ruleId">The rule identifier to place in the JSON payload.</param>
        /// <returns>A wrapped rule document that mirrors checked-in repository query-rule files.</returns>
        private static string CreateRuleJson(string ruleId)
        {
            // Mirror the repository rule wrapper so the source tests exercise the same payload shape used at runtime.
            return $$"""
                {
                  "schemaVersion": "1.0",
                  "rule": {
                    "id": "{{ruleId}}",
                    "title": "Rule {{ruleId}}",
                    "if": { "path": "input.cleanedText", "containsPhrase": "latest" },
                    "then": { "consume": { "phrases": [ "latest" ] } }
                  }
                }
                """;
        }
    }
}
