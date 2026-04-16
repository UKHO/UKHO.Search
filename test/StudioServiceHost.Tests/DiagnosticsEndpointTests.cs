using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Shouldly;
using StudioServiceHost;
using Xunit;

namespace StudioServiceHost.Tests
{
    /// <summary>
    /// Verifies the lightweight diagnostics and shell-origin assumptions that the fresh Studio shell depends on.
    /// </summary>
    public sealed class DiagnosticsEndpointTests
    {
        /// <summary>
        /// Verifies that the lightweight <c>/echo</c> endpoint remains available for smoke testing.
        /// </summary>
        [Fact]
        public async Task Echo_endpoint_returns_the_expected_payload()
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
                // Request the smoke-test endpoint so the shell's manual verification path stays protected.
                var response = await app.GetTestClient().GetAsync("/echo");
                var content = await response.Content.ReadAsStringAsync();

                response.IsSuccessStatusCode.ShouldBeTrue();
                content.ShouldBe("Hello from StudioServiceHost echo.");
            }
            finally
            {
                // Shut the test host down cleanly once the assertions are complete.
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        /// <summary>
        /// Verifies that the Studio service host still trusts the fixed HTTP Studio shell origin.
        /// </summary>
        [Fact]
        public async Task Cors_policy_allows_the_fixed_http_studio_shell_origin()
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
                // Send a preflight request from the fixed shell origin so the HTTP shell hosting contract stays protected.
                var request = new HttpRequestMessage(HttpMethod.Options, "/echo");
                request.Headers.Add("Origin", "http://localhost:3000");
                request.Headers.Add("Access-Control-Request-Method", "GET");

                var response = await app.GetTestClient().SendAsync(request);

                response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins).ShouldBeTrue();
                origins.ShouldContain("http://localhost:3000");
            }
            finally
            {
                // Shut the test host down cleanly once the assertions are complete.
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        /// <summary>
        /// Creates the minimal rules configuration needed for diagnostics endpoint startup during tests.
        /// </summary>
        /// <returns>The in-memory rules configuration consumed by the test host builder.</returns>
        private static Dictionary<string, string?> CreateDefaultRulesConfiguration()
        {
            // Return a single valid rule so the host can complete startup and publish its diagnostics endpoints.
            return new Dictionary<string, string?>
            {
                ["SkipAddsConfiguration"] = "true",
                ["rules:ingestion:file-share:rule-1"] = """
                    {
                      "schemaVersion": "1.0",
                      "rule": {
                        "id": "rule-1",
                        "title": "Studio service host diagnostics test rule",
                        "if": { "path": "id", "exists": true },
                        "then": { "keywords": { "add": [ "k" ] } }
                      }
                    }
                    """
            };
        }
    }
}
