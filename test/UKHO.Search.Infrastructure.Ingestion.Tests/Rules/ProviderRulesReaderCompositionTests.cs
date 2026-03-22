using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shouldly;
using UKHO.Search.Configuration;
using UKHO.Search.Infrastructure.Ingestion.Injection;
using UKHO.Search.Infrastructure.Ingestion.Rules;
using UKHO.Search.Ingestion.Tests.TestSupport;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Rules
{
    public sealed class ProviderRulesReaderCompositionTests
    {
        [Fact]
        public void AddIngestionRulesEngine_registers_provider_rules_reader_without_runtime_provider_factories()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRuleFile("FILE-SHARE", "rule-1", CreateRuleJson("rule-1"));

            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();

            services.AddSingleton<IHostEnvironment>(new TestHostEnvironment { ContentRootPath = temp.RootPath });
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton(new IngestionModeOptions(IngestionMode.Strict));
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
            services.AddIngestionRulesEngine();
            services.AddSingleton<IRulesSource>(_ => new FileSystemRulesSource(temp.RootPath));

            using var provider = services.BuildServiceProvider();
            var reader = provider.GetRequiredService<IProviderRulesReader>();
            var ruleIdsCatalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            var snapshot = reader.GetSnapshot();

            snapshot.SchemaVersion.ShouldBe("1.0");
            snapshot.RulesByProvider.Keys.ShouldBe(["file-share"]);
            snapshot.RulesByProvider["file-share"].Count.ShouldBe(1);
            snapshot.RulesByProvider["file-share"][0].Id.ShouldBe("rule-1");
            provider.GetServices<UKHO.Search.Ingestion.Providers.IIngestionDataProviderFactory>().ShouldBeEmpty();
            ruleIdsCatalog.GetRuleIdsByProvider().Keys.ShouldBe(["file-share"]);
        }

        [Fact]
        public void GetSnapshot_preserves_canonical_provider_names_and_rule_metadata()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRuleFile("FILE-SHARE", "rule-1", CreateRuleJson("rule-1", title: "Alpha Rule", description: "Rule description", context: "Adds-S100", enabled: false));

            using var provider = IngestionRulesTestServiceProviderFactory.Create(temp.RootPath);
            var reader = provider.GetRequiredService<IProviderRulesReader>();

            var snapshot = reader.GetSnapshot();

            snapshot.RulesByProvider.Keys.ShouldBe(["file-share"]);
            snapshot.RulesByProvider["file-share"][0].Id.ShouldBe("rule-1");
            snapshot.RulesByProvider["file-share"][0].Title.ShouldBe("Alpha Rule");
            snapshot.RulesByProvider["file-share"][0].Description.ShouldBe("Rule description");
            snapshot.RulesByProvider["file-share"][0].Context.ShouldBe("adds-s100");
            snapshot.RulesByProvider["file-share"][0].Enabled.ShouldBeFalse();
        }

        [Fact]
        public void TryGetProviderRules_is_case_insensitive_and_returns_false_for_unknown_provider()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRuleFile("file-share", "rule-1", CreateRuleJson("rule-1"));

            using var provider = IngestionRulesTestServiceProviderFactory.Create(temp.RootPath);
            var reader = provider.GetRequiredService<IProviderRulesReader>();

            reader.TryGetProviderRules("FILE-SHARE", out var rules).ShouldBeTrue();
            rules.ShouldNotBeNull();
            rules.Count.ShouldBe(1);
            rules[0].Id.ShouldBe("rule-1");

            reader.TryGetProviderRules("unknown-provider", out var missingRules).ShouldBeFalse();
            missingRules.ShouldBeEmpty();
        }

        private static string CreateRuleJson(string ruleId, string title = "Rule title", string? description = null, string? context = null, bool enabled = true)
        {
            var descriptionJson = description is null ? string.Empty : $",\n        \"description\": \"{description}\"";
            var contextJson = context is null ? string.Empty : $",\n        \"context\": \"{context}\"";
            var enabledJson = enabled ? string.Empty : ",\n        \"enabled\": false";

            return $$"""
                {
                  "schemaVersion": "1.0",
                  "rule": {
                    "id": "{{ruleId}}",
                    "title": "{{title}}"{{descriptionJson}}{{contextJson}}{{enabledJson}},
                    "if": { "path": "id", "exists": true },
                    "then": { "keywords": { "add": [ "k" ] } }
                  }
                }
                """;
        }
    }
}
