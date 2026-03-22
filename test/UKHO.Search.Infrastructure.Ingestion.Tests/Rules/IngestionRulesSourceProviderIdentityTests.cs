using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Rules;
using UKHO.Search.Ingestion.Tests.TestSupport;
using UKHO.Search.ProviderModel;
using UKHO.Search.ProviderModel.Injection;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Rules
{
    public sealed class IngestionRulesSourceProviderIdentityTests
    {
        [Fact]
        public void EnsureLoaded_throws_when_rule_provider_is_unknown()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRuleFile("unknown-provider", "rule-1", CreateRuleJson("rule-1"));

            using var provider = IngestionRulesTestServiceProviderFactory.Create(temp.RootPath);
            var rulesCatalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            var exception = Should.Throw<IngestionRulesValidationException>(() => rulesCatalog.EnsureLoaded());
            exception.Message.ShouldContain("unknown-provider");
            exception.Message.ShouldContain("provider metadata");
        }

        [Fact]
        public void GetRuleIdsByProvider_canonicalizes_provider_name_from_rules_source()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRuleFile("FILE-SHARE", "rule-1", CreateRuleJson("rule-1"));

            using var provider = IngestionRulesTestServiceProviderFactory.Create(temp.RootPath);
            var rulesCatalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            var ruleIdsByProvider = rulesCatalog.GetRuleIdsByProvider();

            ruleIdsByProvider.Keys.ShouldBe(["file-share"]);
            ruleIdsByProvider["file-share"].ShouldBe(["rule-1"]);
        }

        [Fact]
        public void GetRuleIdsByProvider_allows_rules_for_known_but_disabled_provider()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRuleFile("other-provider", "rule-1", CreateRuleJson("rule-1"));

            using var provider = IngestionRulesTestServiceProviderFactory.Create(
                temp.RootPath,
                new Dictionary<string, string?>
                {
                    ["ingestion:providers:0"] = "file-share"
                },
                services => services.AddProviderDescriptor<OtherProviderRegistrationMarker>(new ProviderDescriptor("other-provider", "Other Provider")));
            var rulesCatalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            var ruleIdsByProvider = rulesCatalog.GetRuleIdsByProvider();

            ruleIdsByProvider.Keys.ShouldBe(["other-provider"]);
            ruleIdsByProvider["other-provider"].ShouldBe(["rule-1"]);
        }

        private static string CreateRuleJson(string ruleId)
        {
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
