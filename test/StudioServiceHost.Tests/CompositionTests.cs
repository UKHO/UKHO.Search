using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using UKHO.Search.Ingestion.Providers;
using UKHO.Search.ProviderModel;
using UKHO.Search.Studio.Providers;
using Xunit;

namespace StudioServiceHost.Tests
{
    /// <summary>
    /// Verifies that the Studio service host registers its core provider services correctly.
    /// </summary>
    public sealed class CompositionTests
    {
        /// <summary>
        /// Verifies that building the host registers provider metadata services without reintroducing runtime factory registrations.
        /// </summary>
        [Fact]
        public async Task BuildApp_registers_provider_and_studio_metadata_without_runtime_factories()
        {
            // Build the host with a minimal in-memory rules configuration so composition can be validated in isolation.
            var app = StudioServiceHostApplication.BuildApp(
                Array.Empty<string>(),
                builder =>
                {
                    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["SkipAddsConfiguration"] = "true",
                        ["rules:ingestion:file-share:rule-1"] = """
                            {
                              "schemaVersion": "1.0",
                              "rule": {
                                "id": "rule-1",
                                "title": "Studio service host composition test rule",
                                "if": { "path": "id", "exists": true },
                                "then": { "keywords": { "add": [ "k" ] } }
                              }
                            }
                            """
                    });
                });

            try
            {
                // Resolve the provider catalogs from the built host so their registrations can be asserted directly.
                var catalog = app.Services.GetRequiredService<IProviderCatalog>();
                var studioProviderCatalog = app.Services.GetRequiredService<IStudioProviderCatalog>();

                // Confirm the expected provider metadata is present and that runtime ingestion factories are still absent.
                catalog.GetProvider("file-share").DisplayName.ShouldBe("File Share");
                studioProviderCatalog.GetProvider("file-share").ProviderName.ShouldBe("file-share");
                app.Services.GetServices<IIngestionDataProviderFactory>().ShouldBeEmpty();
            }
            finally
            {
                // Dispose the host so any background services or resources are released promptly after the assertion completes.
                await app.DisposeAsync();
            }
        }
    }
}
