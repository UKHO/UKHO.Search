using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Shouldly;
using StudioServiceHost;
using Xunit;

namespace StudioServiceHost.Tests
{
    /// <summary>
    /// Verifies the OpenAPI and Scalar endpoints exposed by the Studio service host.
    /// </summary>
    public sealed class OpenApiEndpointTests
    {
        /// <summary>
        /// Verifies that the OpenAPI document is available at the Scalar-linked route and still advertises the expected paths.
        /// </summary>
        [Fact]
        public async Task OpenApi_document_is_available_for_scalar_at_v1_route()
        {
            // Build a test server instance of the host with the minimal rules configuration required for startup.
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
                // Request the OpenAPI document so the published endpoint paths can be verified.
                var response = await app.GetTestClient().GetAsync("/openapi/v1.json");

                response.IsSuccessStatusCode.ShouldBeTrue();

                // Read the document text and assert that the unchanged host routes are still advertised.
                var content = await response.Content.ReadAsStringAsync();
                content.ShouldContain("\"openapi\"");
                content.ShouldContain("\"/providers\"");
                content.ShouldContain("\"/rules\"");
                content.ShouldContain("\"/ingestion/{provider}/{id}\"");
                content.ShouldContain("\"/ingestion/{provider}/payload\"");
                content.ShouldContain("\"/ingestion/{provider}/all\"");
                content.ShouldContain("\"/ingestion/{provider}/contexts\"");
                content.ShouldContain("\"/ingestion/{provider}/context/{context}\"");
                content.ShouldContain("\"/ingestion/{provider}/context/{context}/operations/reset-indexing-status\"");
                content.ShouldContain("\"/ingestion/{provider}/operations/reset-indexing-status\"");
                content.ShouldContain("\"/operations/active\"");
                content.ShouldContain("\"/operations/{operationId}\"");
                content.ShouldContain("\"/operations/{operationId}/events\"");
            }
            finally
            {
                // Shut the test host down cleanly once the assertions are complete.
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        /// <summary>
        /// Verifies that the Scalar UI remains available at its existing route.
        /// </summary>
        [Fact]
        public async Task Scalar_ui_is_available_at_v1_route()
        {
            // Build a test server instance of the host with the minimal rules configuration required for startup.
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
                // Request the Scalar UI route to confirm it remains reachable after the host rename.
                var response = await app.GetTestClient().GetAsync("/scalar/v1/");

                response.IsSuccessStatusCode.ShouldBeTrue();
            }
            finally
            {
                // Shut the test host down cleanly once the assertions are complete.
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        /// <summary>
        /// Creates the minimal rules configuration needed for OpenAPI and Scalar endpoint startup during tests.
        /// </summary>
        /// <returns>The in-memory rules configuration consumed by the test host builder.</returns>
        private static Dictionary<string, string?> CreateDefaultRulesConfiguration()
        {
            // Return a single valid rule so the host can complete startup and publish its OpenAPI surface.
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
