using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Rules;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Rules
{
    /// <summary>
    /// Verifies how <see cref="AppConfigRulesSource"/> enumerates App Configuration-backed ingestion rules.
    /// </summary>
    public sealed class AppConfigRulesSourceTests
    {
        /// <summary>
        /// Verifies that rules under the ingestion namespace are projected with full namespace-aware keys and nested rule identifiers.
        /// </summary>
        [Fact]
        public void ListRuleEntries_WhenNamespaceAwareRulesExist_ShouldReturnCanonicalKeysAndNestedRuleIdentifiers()
        {
            // Arrange configuration values that mirror the App Configuration keys produced from nested repository rule folders.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["rules:ingestion:FILE-SHARE:rule-1"] = CreateRuleJson("rule-1"),
                    ["rules:ingestion:file-share:subset:rule-2"] = CreateRuleJson("subset:rule-2"),
                })
                .Build();
            var source = new AppConfigRulesSource(configuration, NullLogger<AppConfigRulesSource>.Instance);

            // Enumerate the rule entries through the real App Configuration source implementation.
            var entries = source.ListRuleEntries().OrderBy(entry => entry.RuleId, StringComparer.Ordinal).ToArray();

            // The source must normalize provider identity and preserve the nested rule-id path in the emitted key.
            entries.Length.ShouldBe(2);
            entries[0].Provider.ShouldBe("file-share");
            entries[0].RuleId.ShouldBe("rule-1");
            entries[0].Key.ShouldBe("rules:ingestion:file-share:rule-1");
            entries[1].Provider.ShouldBe("file-share");
            entries[1].RuleId.ShouldBe("subset:rule-2");
            entries[1].Key.ShouldBe("rules:ingestion:file-share:subset:rule-2");
        }

        /// <summary>
        /// Verifies that legacy rules without the ingestion namespace are ignored by the runtime source.
        /// </summary>
        [Fact]
        public void ListRuleEntries_WhenLegacyRulesExistOutsideIngestionNamespace_ShouldIgnoreLegacyKeys()
        {
            // Arrange one legacy key and one namespace-aware key so the test proves only the supported namespace is read.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["rules:file-share:legacy-rule"] = CreateRuleJson("legacy-rule"),
                    ["rules:ingestion:file-share:active-rule"] = CreateRuleJson("active-rule"),
                })
                .Build();
            var source = new AppConfigRulesSource(configuration, NullLogger<AppConfigRulesSource>.Instance);

            // Enumerate the active rules through the App Configuration source.
            var entries = source.ListRuleEntries();

            // Only the namespace-aware rule should be returned because legacy keys are no longer part of the supported contract.
            entries.Count.ShouldBe(1);
            entries[0].RuleId.ShouldBe("active-rule");
            entries[0].Key.ShouldBe("rules:ingestion:file-share:active-rule");
        }

        /// <summary>
        /// Creates a minimal wrapped rule document for App Configuration-backed tests.
        /// </summary>
        /// <param name="ruleId">The rule identifier to place in the JSON payload.</param>
        /// <returns>A wrapped rule document that mirrors checked-in repository rule files.</returns>
        private static string CreateRuleJson(string ruleId)
        {
            // Mirror the repository rule wrapper so the source tests exercise the same payload shape used at runtime.
            return $$"""
                {
                  "schemaVersion": "1.0",
                  "rule": {
                    "id": "{{ruleId}}",
                    "title": "Rule {{ruleId}}",
                    "if": { "path": "id", "exists": true },
                    "then": { "keywords": { "add": [ "k" ] } }
                  }
                }
                """;
        }
    }
}
