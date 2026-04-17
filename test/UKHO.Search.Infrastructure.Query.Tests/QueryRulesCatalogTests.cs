using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.Infrastructure.Query.Rules;
using UKHO.Search.Infrastructure.Query.Tests.TestSupport;
using Xunit;

namespace UKHO.Search.Infrastructure.Query.Tests
{
    /// <summary>
    /// Verifies query-rule catalog loading and reload behavior.
    /// </summary>
    public sealed class QueryRulesCatalogTests
    {
        /// <summary>
        /// Verifies that reloading the catalog replaces the cached validated snapshot with the latest source content.
        /// </summary>
        [Fact]
        public void Reload_when_rule_source_changes_replaces_cached_snapshot()
        {
            // Arrange a mutable source so the test can verify that the catalog actually reloads validated content rather than reusing stale state.
            var source = new MutableQueryRulesSource([
                CreateEntry("sort-latest", CreateRuleJson("sort-latest", "latest"))
            ]);
            var catalog = new QueryRulesCatalog(
                new QueryRulesLoader(source, new QueryRulesValidator(), NullLogger<QueryRulesLoader>.Instance),
                NullLogger<QueryRulesCatalog>.Instance);

            // Load the initial snapshot so the catalog caches the first validated rule set.
            catalog.GetSnapshot().Rules.Select(static rule => rule.Id).ShouldBe(["sort-latest"]);

            // Replace the underlying source entries and reload the catalog.
            source.SetEntries([
                CreateEntry("concept-solas", CreateConceptRuleJson())
            ]);
            catalog.Reload();

            // The next snapshot should reflect the replacement rule set rather than the original cached state.
            catalog.GetSnapshot().Rules.Select(static rule => rule.Id).ShouldBe(["concept-solas"]);
            catalog.GetDiagnostics().RuleCount.ShouldBe(1);
        }

        /// <summary>
        /// Creates one raw query-rule entry for catalog tests.
        /// </summary>
        /// <param name="ruleId">The flat rule identifier that should identify the entry.</param>
        /// <param name="json">The wrapped rule JSON stored for the entry.</param>
        /// <returns>The raw query-rule entry.</returns>
        private static QueryRuleEntry CreateEntry(string ruleId, string json)
        {
            // Build the namespace-aware entry explicitly so the catalog test stays focused on loader and reload behavior.
            return new QueryRuleEntry
            {
                Key = QueryRuleConfigurationPath.BuildRuleKey(ruleId),
                RuleId = ruleId,
                Json = json
            };
        }

        /// <summary>
        /// Creates a wrapped latest-sort rule document for catalog tests.
        /// </summary>
        /// <param name="ruleId">The flat rule identifier that should identify the rule.</param>
        /// <param name="phrase">The phrase that should trigger the latest sort rule.</param>
        /// <returns>The wrapped latest-sort rule JSON.</returns>
        private static string CreateRuleJson(string ruleId, string phrase)
        {
            // Mirror the repository sort rule shape so the catalog test exercises the same wrapped JSON used at runtime.
            return $$"""
                {
                  "schemaVersion": "1.0",
                  "rule": {
                    "id": "{{ruleId}}",
                    "title": "Rule {{ruleId}}",
                    "if": { "path": "input.cleanedText", "containsPhrase": "{{phrase}}" },
                    "then": {
                      "sortHints": [
                        {
                          "id": "latest",
                          "matchedText": "$val",
                          "fields": ["majorVersion", "minorVersion"],
                          "order": "desc"
                        }
                      ],
                      "consume": {
                        "phrases": ["{{phrase}}"]
                      }
                    }
                  }
                }
                """;
        }

        /// <summary>
        /// Creates a wrapped concept rule document for catalog tests.
        /// </summary>
        /// <returns>The wrapped concept rule JSON.</returns>
        private static string CreateConceptRuleJson()
        {
            // Mirror the repository concept rule shape so reload tests exercise a rule with canonical model mutations and token consumption.
            return """
                {
                  "schemaVersion": "1.0",
                  "rule": {
                    "id": "concept-solas",
                    "title": "Recognize SOLAS concept",
                    "if": { "path": "input.tokens[*]", "eq": "solas" },
                    "then": {
                      "concepts": [
                        {
                          "id": "solas",
                          "matchedText": "$val",
                          "keywordExpansions": ["solas", "maritime", "safety", "msi"]
                        }
                      ],
                      "model": {
                        "keywords": {
                          "add": ["solas", "maritime", "safety", "msi"]
                        }
                      },
                      "consume": {
                        "tokens": ["solas"]
                      }
                    }
                  }
                }
                """;
        }
    }
}
