using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Shouldly;
using Xunit;

namespace StudioApiHost.Tests
{
    public sealed class StudioApiHostOpenApiEndpointTests
    {
        [Fact]
        public async Task OpenApi_document_is_available_for_scalar_at_v1_route()
        {
            var app = StudioApiHostApplication.BuildApp(
                Array.Empty<string>(),
                builder => builder.WebHost.UseTestServer());

            await app.StartAsync();

            try
            {
                var response = await app.GetTestClient().GetAsync("/openapi/v1.json");

                response.IsSuccessStatusCode.ShouldBeTrue();

                var content = await response.Content.ReadAsStringAsync();
                content.ShouldContain("\"openapi\"");
                content.ShouldContain("\"/providers\"");
            }
            finally
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        [Fact]
        public async Task Scalar_ui_is_available_at_v1_route()
        {
            var app = StudioApiHostApplication.BuildApp(
                Array.Empty<string>(),
                builder => builder.WebHost.UseTestServer());

            await app.StartAsync();

            try
            {
                var response = await app.GetTestClient().GetAsync("/scalar/v1/");

                response.IsSuccessStatusCode.ShouldBeTrue();
            }
            finally
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }
    }
}
