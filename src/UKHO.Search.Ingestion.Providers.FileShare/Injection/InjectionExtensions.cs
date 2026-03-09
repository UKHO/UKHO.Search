using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UKHO.Search.Ingestion;
using UKHO.Search.Ingestion.Providers;
using UKHO.Search.Ingestion.Providers.FileShare.Enrichment;

namespace UKHO.Search.Ingestion.Providers.FileShare.Injection
{
    public static class InjectionExtensions
    {
        public static IServiceCollection AddFileShareProvider(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSingleton<IIngestionDataProviderFactory>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var queueName = configuration["ingestion:filesharequeuename"] ?? "file-share-queue";

                return new FileShareIngestionDataProviderFactory(queueName);
            });

            services.AddScoped<IIngestionEnricher, BasicEnricher>();
            services.AddScoped<IIngestionEnricher, FileContentEnricher>();
            services.AddScoped<IIngestionEnricher, ExchangeSetEnricher>();
            services.AddScoped<IIngestionEnricher, GeoLocationEnricher>();

            return services;
        }
    }
}
