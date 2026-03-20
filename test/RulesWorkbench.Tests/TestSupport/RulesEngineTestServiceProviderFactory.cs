using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UKHO.Search.Infrastructure.Ingestion.Injection;
using UKHO.Search.Infrastructure.Ingestion.Rules;

namespace RulesWorkbench.Tests.TestSupport
{
    internal static class RulesEngineTestServiceProviderFactory
    {
        public static ServiceProvider Create(string contentRootPath)
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ingestion:fileContentExtractionAllowedExtensions"] = string.Empty
                })
                .Build();

            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
            services.AddIngestionRulesEngine();
            services.AddSingleton<IRulesSource>(_ => new FileSystemRulesSource(contentRootPath));

            return services.BuildServiceProvider();
        }
    }
}
