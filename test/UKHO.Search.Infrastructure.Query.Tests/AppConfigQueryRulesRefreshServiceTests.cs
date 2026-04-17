using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.Infrastructure.Query.Rules;
using UKHO.Search.Infrastructure.Query.Tests.TestSupport;
using Xunit;

namespace UKHO.Search.Infrastructure.Query.Tests
{
    /// <summary>
    /// Verifies activation behavior for the query-rule refresh background service.
    /// </summary>
    public sealed class AppConfigQueryRulesRefreshServiceTests
    {
        /// <summary>
        /// Verifies that the refresh service can be activated even when Azure App Configuration refresher services are not registered.
        /// </summary>
        [Fact]
        public void Constructor_when_azure_app_configuration_refresher_provider_is_not_registered_still_activates()
        {
            // Arrange a minimal validated query-rule catalog and configuration root because the host may run without Azure App Configuration refresh services.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();
            var catalog = new QueryRulesCatalog(
                new QueryRulesLoader(
                    new MutableQueryRulesSource([]),
                    new QueryRulesValidator(),
                    NullLogger<QueryRulesLoader>.Instance),
                NullLogger<QueryRulesCatalog>.Instance);
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton(catalog);
            services.AddSingleton<ILogger<AppConfigQueryRulesRefreshService>>(static _ => NullLogger<AppConfigQueryRulesRefreshService>.Instance);
            using var provider = services.BuildServiceProvider();

            // Act by creating the refresh service through DI activation without registering IConfigurationRefresherProvider.
            var service = ActivatorUtilities.CreateInstance<AppConfigQueryRulesRefreshService>(provider);

            // The service should still be created so the runtime can fall back to configuration reload-token monitoring.
            service.ShouldNotBeNull();
        }
    }
}
