using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Shouldly;
using UKHO.Search.ProviderModel;
using UKHO.Search.ProviderModel.Injection;
using StudioApiHost.Tests.TestDoubles;
using Xunit;

namespace StudioApiHost.Tests
{
    public sealed class StudioApiHostProviderEndpointTests
    {
        [Fact]
        public async Task GetProviders_returns_registered_provider_metadata()
        {
            var app = StudioApiHostApplication.BuildApp(
                Array.Empty<string>(),
                builder =>
                {
                    builder.WebHost.UseTestServer();
                    builder.Configuration.AddInMemoryCollection(CreateDefaultRulesConfiguration());
                });

            await app.StartAsync();

            try
            {
                var response = await app.GetTestClient().GetFromJsonAsync<List<ProviderDescriptor>>("/providers");

                response.ShouldNotBeNull();
                response.Count.ShouldBe(1);
                response[0].Name.ShouldBe("file-share");
                response[0].DisplayName.ShouldBe("File Share");
                response[0].Description.ShouldBe("Ingests content sourced from File Share.");
            }
            finally
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        [Fact]
        public async Task GetProviders_returns_full_provider_metadata_in_deterministic_order()
        {
            var app = StudioApiHostApplication.BuildApp(
                Array.Empty<string>(),
                builder =>
                {
                    builder.WebHost.UseTestServer();
                    builder.Configuration.AddInMemoryCollection(CreateDefaultRulesConfiguration());
                    builder.Services.AddProviderDescriptor<AdditionalProviderRegistrationMarker>(new ProviderDescriptor("a-provider", "Alpha", "Additional provider for ordering tests."));
                });

            await app.StartAsync();

            try
            {
                var response = await app.GetTestClient().GetFromJsonAsync<List<ProviderDescriptor>>("/providers");

                response.ShouldNotBeNull();
                response.Select(x => x.Name).ShouldBe(["a-provider", "file-share"]);
                response[0].DisplayName.ShouldBe("Alpha");
                response[0].Description.ShouldBe("Additional provider for ordering tests.");
                response[1].DisplayName.ShouldBe("File Share");
                response[1].Description.ShouldBe("Ingests content sourced from File Share.");
            }
            finally
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }

        private static Dictionary<string, string?> CreateDefaultRulesConfiguration()
        {
            return new Dictionary<string, string?>
            {
                ["rules:file-share:rule-1"] = """
                    {
                      "schemaVersion": "1.0",
                      "rule": {
                        "id": "rule-1",
                        "title": "Studio API host test rule",
                        "if": { "path": "id", "exists": true },
                        "then": { "keywords": { "add": [ "k" ] } }
                      }
                    }
                    """
            };
        }
    }
}
