using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using StudioApiHost;
using Xunit;

namespace UKHO.Search.Tests.Studio
{
    public class StudioApiHostEchoEndpointTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public StudioApiHostEchoEndpointTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = true
            });
        }

        [Fact]
        public async Task GetEcho_WhenRequested_ShouldReturnStudioApiHostMessage()
        {
            var response = await _client.GetAsync("/echo");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();

            content.ShouldBe("Hello from StudioApiHost echo.");
        }
    }
}
