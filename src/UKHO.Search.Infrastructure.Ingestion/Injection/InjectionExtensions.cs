using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UKHO.Search.Infrastructure.Ingestion.Bootstrap;
using UKHO.Search.Infrastructure.Ingestion.Elastic;
using UKHO.Search.Infrastructure.Ingestion.Pipeline;
using UKHO.Search.Infrastructure.Ingestion.Queue;
using UKHO.Search.Infrastructure.Ingestion.Statistics;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Ingestion.Providers;
using UKHO.Search.Ingestion.Providers.FileShare.Injection;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Services.Ingestion.Providers;

namespace UKHO.Search.Infrastructure.Ingestion.Injection
{
    public static class InjectionExtensions
    {
        public static IServiceCollection AddIngestionServices(this IServiceCollection collection)
        {
            collection.AddFileShareProvider();

            collection.AddSingleton<IIngestionProviderService, IngestionProviderService>();
            collection.AddSingleton<IBootstrapService, BootstrapService>();
            collection.AddSingleton<IStatisticsService, StatisticsService>();

            collection.AddSingleton<IQueueClientFactory, AzureQueueClientFactory>();

            collection.AddSingleton<IBulkIndexClient<IndexOperation>, ElasticsearchBulkIndexClient>();

            collection.AddSingleton<FileShareIngestionPipelineAdapter>();
            collection.AddHostedService<IngestionPipelineHostedService>();

            return collection;
        }
    }
}