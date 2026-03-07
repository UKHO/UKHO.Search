using Azure.Storage.Queues;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Configuration;
using UKHO.Search.Services.Ingestion.Providers;

namespace UKHO.Search.Infrastructure.Ingestion.Bootstrap
{
    public class BootstrapService : IBootstrapService
    {
        private readonly IConfiguration _configuration;
        private readonly ElasticsearchClient _elasticClient;
        private readonly IIngestionProviderService _providerService;
        private readonly QueueServiceClient _queueClient;

        public BootstrapService(IConfiguration configuration, IIngestionProviderService providerService, ElasticsearchClient elasticClient, QueueServiceClient queueClient)
        {
            _configuration = configuration;
            _providerService = providerService;
            _elasticClient = elasticClient;
            _queueClient = queueClient;
        }

        public async Task BootstrapAsync(CancellationToken cancellationToken = default)
        {
            var indexName = _configuration["ingestion:indexname"];
            if (string.IsNullOrWhiteSpace(indexName))
            {
                throw new InvalidOperationException("Missing required configuration value 'ingestion:indexname'.");
            }

            var indexExistsResponse = await _elasticClient.Indices.ExistsAsync(indexName, cancellationToken)
                                                          .ConfigureAwait(false);
            if (!indexExistsResponse.Exists)
            {
                await _elasticClient.Indices.CreateAsync(indexName, cancellationToken)
                                    .ConfigureAwait(false);
            }

            foreach (var provider in _providerService.GetAllProviders())
            {
                var queueName = provider.QueueName;
                if (string.IsNullOrWhiteSpace(queueName))
                {
                    continue;
                }

                var queueClient = _queueClient.GetQueueClient(queueName);
                await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken)
                                 .ConfigureAwait(false);
            }
        }
    }
}