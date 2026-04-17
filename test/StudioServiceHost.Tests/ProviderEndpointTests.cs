using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Shouldly;
using StudioServiceHost;
using StudioServiceHost.Tests.TestDoubles;
using UKHO.Search.ProviderModel;
using UKHO.Search.ProviderModel.Injection;
using Xunit;

namespace StudioServiceHost.Tests
{
    /// <summary>
    /// Verifies the provider discovery endpoints exposed by the Studio service host.
    /// </summary>
    public sealed class ProviderEndpointTests
    {
        /// <summary>
        /// Verifies that the providers endpoint returns the registered provider metadata for the default test host configuration.
        /// </summary>
        [Fact]
        public async Task GetProviders_returns_registered_provider_metadata()
        {
            // Build a test server instance of the host with the default in-memory rules configuration.
            var app = StudioServiceHostApplication.BuildApp(
                Array.Empty<string>(),
                builder =>
                {
                    builder.WebHost.UseTestServer();
                    builder.Configuration.AddInMemoryCollection(CreateDefaultRulesConfiguration());
                });

            await app.StartAsync();

            try
            {
                // Query the providers endpoint and deserialize the provider descriptors returned by the host.
                var response = await app.GetTestClient().GetFromJsonAsync<List<ProviderDescriptor>>("/providers");

                // Confirm the default file-share provider metadata is exposed unchanged.
                response.ShouldNotBeNull();
                response.Count.ShouldBe(1);
                response[0].Name.ShouldBe("file-share");
                response[0].DisplayName.ShouldBe("File Share");
                response[0].Description.ShouldBe("Ingests content sourced from File Share.");
            }
            finally
            {
                // Shut the test host down cleanly once the assertions are complete.
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        /// <summary>
        /// Verifies that the providers endpoint returns every provider descriptor in deterministic order.
        /// </summary>
        [Fact]
        public async Task GetProviders_returns_full_provider_metadata_in_deterministic_order()
        {
            // Build a test server and add a second provider so ordering and full metadata projection can be asserted.
            var app = StudioServiceHostApplication.BuildApp(
                Array.Empty<string>(),
                builder =>
                {
                    builder.WebHost.UseTestServer();
                    builder.Configuration.AddInMemoryCollection(CreateDefaultRulesConfiguration());
                    builder.Services.AddProviderDescriptor<AdditionalProviderRegistrationMarker>(
                        new ProviderDescriptor("a-provider", "Alpha", "Additional provider for ordering tests."));
                });

            await app.StartAsync();

            try
            {
                // Query the providers endpoint to verify that both providers are returned in stable order.
                var response = await app.GetTestClient().GetFromJsonAsync<List<ProviderDescriptor>>("/providers");

                // Assert both metadata payloads so the endpoint move did not alter projection behavior.
                response.ShouldNotBeNull();
                response.Select(x => x.Name).ShouldBe(["a-provider", "file-share"]);
                response[0].DisplayName.ShouldBe("Alpha");
                response[0].Description.ShouldBe("Additional provider for ordering tests.");
                response[1].DisplayName.ShouldBe("File Share");
                response[1].Description.ShouldBe("Ingests content sourced from File Share.");
            }
            finally
            {
                // Shut the test host down cleanly once the assertions are complete.
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        /// <summary>
        /// Creates the minimal rules configuration needed for provider endpoint host startup during tests.
        /// </summary>
        /// <returns>The in-memory rules configuration consumed by the test host builder.</returns>
        private static Dictionary<string, string?> CreateDefaultRulesConfiguration()
        {
            // Return a single valid rule so the host can complete startup and expose the provider endpoints.
            return new Dictionary<string, string?>
            {
                ["SkipAddsConfiguration"] = "true",
                ["rules:ingestion:file-share:rule-1"] = """
                    {
                      "schemaVersion": "1.0",
                      "rule": {
                        "id": "rule-1",
                        "title": "Studio service host test rule",
                        "if": { "path": "id", "exists": true },
                        "then": { "keywords": { "add": [ "k" ] } }
                      }
                    }
                    """
            };
        }
    }
}
