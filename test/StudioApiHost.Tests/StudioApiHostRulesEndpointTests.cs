using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StudioApiHost.Tests.TestDoubles;
using UKHO.Search.Infrastructure.Ingestion.Rules;
using UKHO.Search.Ingestion.Providers;
using UKHO.Search.ProviderModel;
using UKHO.Search.ProviderModel.Injection;
using Xunit;

namespace StudioApiHost.Tests
{
    public sealed class StudioApiHostRulesEndpointTests
    {
        [Fact]
        public async Task GetRules_returns_canonical_rules_for_known_providers()
        {
            var app = StudioApiHostApplication.BuildApp(
                Array.Empty<string>(),
                builder =>
                {
                    builder.WebHost.UseTestServer();
                    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["rules:FILE-SHARE:rule-1"] = CreateRuleJson("rule-1", title: "File Share Rule", context: "Adds-S100"),
                        ["rules:other-provider:rule-2"] = CreateRuleJson("rule-2", title: "Other Provider Rule", enabled: false)
                    });
                    builder.Services.AddProviderDescriptor<AdditionalProviderRegistrationMarker>(
                        new ProviderDescriptor("other-provider", "Other Provider", "Provider used by Studio rules endpoint tests."));
                });

            await app.StartAsync();

            try
            {
                var response = await app.GetTestClient().GetFromJsonAsync<StudioRuleDiscoveryResponse>("/rules");

                response.ShouldNotBeNull();
                response.SchemaVersion.ShouldBe("1.0");
                response.Providers.Select(x => x.ProviderName).ShouldBe(["file-share", "other-provider"]);
                response.Providers[0].DisplayName.ShouldBe("File Share");
                response.Providers[0].Rules.Select(x => x.Id).ShouldBe(["rule-1"]);
                response.Providers[0].Rules[0].Context.ShouldBe("adds-s100");
                response.Providers[1].DisplayName.ShouldBe("Other Provider");
                response.Providers[1].Rules.Select(x => x.Id).ShouldBe(["rule-2"]);
                response.Providers[1].Rules[0].Enabled.ShouldBeFalse();
            }
            finally
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        [Fact]
        public async Task GetRules_includes_known_providers_without_rules_and_avoids_runtime_ingestion_services()
        {
            var app = StudioApiHostApplication.BuildApp(
                Array.Empty<string>(),
                builder =>
                {
                    builder.WebHost.UseTestServer();
                    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["rules:file-share:rule-1"] = CreateRuleJson("rule-1", title: "File Share Rule")
                    });
                    builder.Services.AddProviderDescriptor<AdditionalProviderRegistrationMarker>(
                        new ProviderDescriptor("other-provider", "Other Provider", "Provider with no Studio rules."));
                });

            await app.StartAsync();

            try
            {
                var response = await app.GetTestClient().GetFromJsonAsync<StudioRuleDiscoveryResponse>("/rules");

                response.ShouldNotBeNull();
                response.Providers.Select(x => x.ProviderName).ShouldBe(["file-share", "other-provider"]);
                response.Providers[0].Rules.Count.ShouldBe(1);
                response.Providers[1].Rules.ShouldBeEmpty();
                app.Services.GetServices<IIngestionDataProviderFactory>().ShouldBeEmpty();
            }
            finally
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        [Fact]
        public void BuildApp_throws_when_rules_reference_unknown_provider()
        {
            var exception = Should.Throw<IngestionRulesValidationException>(() =>
                StudioApiHostApplication.BuildApp(
                    Array.Empty<string>(),
                    builder =>
                    {
                        builder.WebHost.UseTestServer();
                        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["rules:unknown-provider:rule-1"] = CreateRuleJson("rule-1", title: "Unknown Provider Rule")
                        });
                    }));

            exception.Message.ShouldContain("unknown-provider");
            exception.Message.ShouldContain("provider metadata");
        }

        private static string CreateRuleJson(string ruleId, string title, string? context = null, bool enabled = true)
        {
            var contextJson = context is null ? string.Empty : $",\n        \"context\": \"{context}\"";
            var enabledJson = enabled ? string.Empty : ",\n        \"enabled\": false";

            return $$"""
                {
                  "schemaVersion": "1.0",
                  "rule": {
                    "id": "{{ruleId}}",
                    "title": "{{title}}"{{contextJson}}{{enabledJson}},
                    "if": { "path": "id", "exists": true },
                    "then": { "keywords": { "add": [ "k" ] } }
                  }
                }
                """;
        }
    }
}
