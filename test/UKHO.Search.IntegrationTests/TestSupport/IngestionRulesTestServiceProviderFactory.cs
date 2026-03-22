using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UKHO.Search.Configuration;
using UKHO.Search.Infrastructure.Ingestion.Injection;
using UKHO.Search.Infrastructure.Ingestion.Rules;

namespace UKHO.Search.Ingestion.Tests.TestSupport
{
    internal static class IngestionRulesTestServiceProviderFactory
    {
        public static ServiceProvider Create(
            string contentRootPath,
            IDictionary<string, string?>? configurationValues = null,
            Action<IServiceCollection>? configureServices = null)
        {
            var services = new ServiceCollection();
            var configurationBuilder = new ConfigurationBuilder();

            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ingestion:fileContentExtractionAllowedExtensions"] = string.Empty
            });

            if (configurationValues is not null)
            {
                configurationBuilder.AddInMemoryCollection(configurationValues);
            }

            services.AddSingleton<IHostEnvironment>(new TestHostEnvironment { ContentRootPath = contentRootPath });
            services.AddSingleton<IConfiguration>(configurationBuilder.Build());
            services.AddSingleton(new IngestionModeOptions(IngestionMode.Strict));
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
            services.AddIngestionServices();
            services.AddSingleton<IRulesSource>(_ => new FileSystemRulesSource(contentRootPath));

            configureServices?.Invoke(services);

            return services.BuildServiceProvider();
        }
    }
}
