using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Bootstrap;
using UKHO.Search.Infrastructure.Ingestion.Injection;
using UKHO.Search.Infrastructure.Ingestion.Rules;
using UKHO.Search.Ingestion.Tests.TestSupport;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Rules
{
    public sealed class BootstrapStartupFailTests
    {
        [Fact]
        public async Task Bootstrap_fails_when_rules_file_missing()
        {
            using var temp = new TempRulesRoot();

            using var provider = CreateProvider(temp.RootPath);
            var rulesCatalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            var bootstrap = new BootstrapService(
                new ConfigurationBuilder().Build(),
                providerService: null!,
                elasticClient: null!,
                queueClient: null!,
                indexDefinition: null!,
                rulesCatalog,
                NullLogger<BootstrapService>.Instance);

            var ex = await Should.ThrowAsync<IngestionRulesValidationException>(() => bootstrap.BootstrapAsync());
            ex.Message.ShouldContain("Missing required rules file", Case.Insensitive);
        }

        [Fact]
        public async Task Bootstrap_fails_when_rules_are_empty()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRulesFile("""
            {
              "schemaVersion": "1.0",
              "rules": { }
            }
            """);

            using var provider = CreateProvider(temp.RootPath);
            var rulesCatalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            var bootstrap = new BootstrapService(
                new ConfigurationBuilder().Build(),
                providerService: null!,
                elasticClient: null!,
                queueClient: null!,
                indexDefinition: null!,
                rulesCatalog,
                NullLogger<BootstrapService>.Instance);

            var ex = await Should.ThrowAsync<IngestionRulesValidationException>(() => bootstrap.BootstrapAsync());
            ex.Message.ShouldContain("Rules validation failed", Case.Insensitive);
            ex.Errors.ShouldContain(x => x.Contains("non-empty", StringComparison.OrdinalIgnoreCase));
        }

        private static ServiceProvider CreateProvider(string contentRootPath)
        {
            var services = new ServiceCollection();

            services.AddSingleton<IHostEnvironment>(new TestHostEnvironment { ContentRootPath = contentRootPath });
            services.AddLogging();
            services.AddIngestionServices();

            return services.BuildServiceProvider();
        }
    }
}
