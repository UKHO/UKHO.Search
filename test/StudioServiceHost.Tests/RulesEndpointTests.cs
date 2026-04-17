using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StudioServiceHost;
using StudioServiceHost.Tests.TestDoubles;
using UKHO.Search.Infrastructure.Ingestion.Rules;
using UKHO.Search.Ingestion.Providers;
using UKHO.Search.ProviderModel;
using UKHO.Search.ProviderModel.Injection;
using UKHO.Search.Studio.Rules;
using Xunit;

namespace StudioServiceHost.Tests
{
    /// <summary>
    /// Verifies the Studio rule discovery endpoints exposed by the Studio service host.
    /// </summary>
    public sealed class RulesEndpointTests
    {
        /// <summary>
        /// Verifies that the rules endpoint returns canonical rule information for each known provider.
        /// </summary>
        [Fact]
        public async Task GetRules_returns_canonical_rules_for_known_providers()
        {
            // Build a test server with rules for two providers so the endpoint projection can be validated.
            var app = StudioServiceHostApplication.BuildApp(
                Array.Empty<string>(),
                builder =>
                {
                    builder.WebHost.UseTestServer();
                    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["SkipAddsConfiguration"] = "true",
                        ["rules:ingestion:FILE-SHARE:rule-1"] = CreateRuleJson("rule-1", title: "File Share Rule", context: "Adds-S100"),
                        ["rules:ingestion:other-provider:rule-2"] = CreateRuleJson("rule-2", title: "Other Provider Rule", enabled: false)
                    });
                    builder.Services.AddProviderDescriptor<AdditionalProviderRegistrationMarker>(
                        new ProviderDescriptor("other-provider", "Other Provider", "Provider used by Studio rules endpoint tests."));
                });

            await app.StartAsync();

            try
            {
                // Query the rules endpoint and deserialize the Studio discovery response.
                var response = await app.GetTestClient().GetFromJsonAsync<StudioRuleDiscoveryResponse>("/rules");

                // Verify the endpoint still returns canonical rules grouped by provider after the host rename.
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
                // Shut the test host down cleanly once the assertions are complete.
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        /// <summary>
        /// Verifies that known providers without rules are still returned and that runtime ingestion services remain absent.
        /// </summary>
        [Fact]
        public async Task GetRules_includes_known_providers_without_rules_and_avoids_runtime_ingestion_services()
        {
            // Build a test server where one provider has rules and the second provider intentionally does not.
            var app = StudioServiceHostApplication.BuildApp(
                Array.Empty<string>(),
                builder =>
                {
                    builder.WebHost.UseTestServer();
                    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["SkipAddsConfiguration"] = "true",
                        ["rules:ingestion:file-share:rule-1"] = CreateRuleJson("rule-1", title: "File Share Rule")
                    });
                    builder.Services.AddProviderDescriptor<AdditionalProviderRegistrationMarker>(
                        new ProviderDescriptor("other-provider", "Other Provider", "Provider with no Studio rules."));
                });

            await app.StartAsync();

            try
            {
                // Query the rules endpoint to verify that known providers are preserved even without rules.
                var response = await app.GetTestClient().GetFromJsonAsync<StudioRuleDiscoveryResponse>("/rules");

                // Assert the provider list and the absence of runtime ingestion data-provider factories.
                response.ShouldNotBeNull();
                response.Providers.Select(x => x.ProviderName).ShouldBe(["file-share", "other-provider"]);
                response.Providers[0].Rules.Count.ShouldBe(1);
                response.Providers[1].Rules.ShouldBeEmpty();
                app.Services.GetServices<IIngestionDataProviderFactory>().ShouldBeEmpty();
            }
            finally
            {
                // Shut the test host down cleanly once the assertions are complete.
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        /// <summary>
        /// Verifies that host startup fails fast when configured rules reference an unknown provider.
        /// </summary>
        [Fact]
        public void BuildApp_throws_when_rules_reference_unknown_provider()
        {
            // Attempt to build the host with an invalid rules configuration so the validation failure can be asserted.
            var exception = Should.Throw<IngestionRulesValidationException>(() =>
                StudioServiceHostApplication.BuildApp(
                    Array.Empty<string>(),
                    builder =>
                    {
                        builder.WebHost.UseTestServer();
                        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["SkipAddsConfiguration"] = "true",
                            ["rules:ingestion:unknown-provider:rule-1"] = CreateRuleJson("rule-1", title: "Unknown Provider Rule")
                        });
                    }));

            // Confirm the validation error names the missing provider and the related metadata problem.
            exception.Message.ShouldContain("unknown-provider");
            exception.Message.ShouldContain("provider metadata");
        }

        /// <summary>
        /// Creates rule JSON for test host configuration.
        /// </summary>
        /// <param name="ruleId">The rule identifier to embed in the JSON payload.</param>
        /// <param name="title">The rule title to embed in the JSON payload.</param>
        /// <param name="context">The optional rule context to emit when the test requires one.</param>
        /// <param name="enabled">Indicates whether the generated rule should be enabled.</param>
        /// <returns>The JSON document supplied to the in-memory rules configuration.</returns>
        private static string CreateRuleJson(string ruleId, string title, string? context = null, bool enabled = true)
        {
            // Build the optional JSON fragments once so the generated rule document stays compact and readable.
            var contextJson = context is null ? string.Empty : $",\n        \"context\": \"{context}\"";
            var enabledJson = enabled ? string.Empty : ",\n        \"enabled\": false";

            // Return the rule JSON expected by the App Configuration-backed rules loader.
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
