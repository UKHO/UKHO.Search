using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Rules;
using UKHO.Search.Ingestion.Tests.TestSupport;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Rules
{
    /// <summary>
    /// Verifies the runtime ingestion-rules path when App Configuration-backed rules are loaded through the normal service graph.
    /// </summary>
    public sealed class AppConfigIngestionRulesRuntimeTests
    {
        /// <summary>
        /// Verifies that the service graph loads rules from the ingestion namespace, ignores legacy keys, and preserves the logical provider identity.
        /// </summary>
        [Fact]
        public void ProviderRulesReader_WhenUsingAppConfigRulesSource_ShouldLoadNamespaceAwareRulesOnly()
        {
            // Arrange App Configuration-style keys for both the supported ingestion namespace and the obsolete legacy root.
            using var provider = IngestionRulesTestServiceProviderFactory.Create(
                contentRootPath: AppContext.BaseDirectory,
                configurationValues: new Dictionary<string, string?>
                {
                    ["rules:file-share:legacy-rule"] = CreateRuleJson("legacy-rule"),
                    ["rules:ingestion:FILE-SHARE:subset:active-rule"] = CreateRuleJson("subset:active-rule"),
                },
                configureServices: services => services.AddSingleton<IRulesSource, AppConfigRulesSource>());
            var reader = provider.GetRequiredService<IProviderRulesReader>();

            // Load the runtime snapshot through the normal ingestion rules catalog service path.
            var snapshot = reader.GetSnapshot();

            // The runtime must expose the canonical provider name while loading only the namespace-aware rule.
            snapshot.RulesByProvider.Keys.ShouldBe(["file-share"]);
            snapshot.RulesByProvider["file-share"].Count.ShouldBe(1);
            snapshot.RulesByProvider["file-share"][0].Id.ShouldBe("subset:active-rule");
        }

        /// <summary>
        /// Creates a minimal wrapped rule document for App Configuration-backed runtime tests.
        /// </summary>
        /// <param name="ruleId">The rule identifier to place in the JSON payload.</param>
        /// <returns>A wrapped rule document that mirrors checked-in repository rule files.</returns>
        private static string CreateRuleJson(string ruleId)
        {
            // Mirror the repository rule wrapper so the runtime test exercises the same JSON shape used by AppHost seeding.
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
