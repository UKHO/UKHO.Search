using Azure.Storage.Queues;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.Search.Infrastructure.Ingestion.Elastic;
using UKHO.Search.Infrastructure.Ingestion.Rules;
using UKHO.Search.Services.Ingestion.Providers;

namespace UKHO.Search.Infrastructure.Ingestion.Bootstrap
{
    public class BootstrapService : IBootstrapService
    {
        private readonly IConfiguration _configuration;
        private readonly CanonicalIndexDefinition _indexDefinition;
        private readonly ElasticsearchClient _elasticClient;
        private readonly ILogger<BootstrapService> _logger;
        private readonly IIngestionProviderService _providerService;
        private readonly QueueServiceClient _queueClient;
        private readonly IIngestionRulesCatalog _rulesCatalog;

        public BootstrapService(IConfiguration configuration, IIngestionProviderService providerService, ElasticsearchClient elasticClient, QueueServiceClient queueClient, CanonicalIndexDefinition indexDefinition, IIngestionRulesCatalog rulesCatalog, ILogger<BootstrapService> logger)
        {
            _configuration = configuration;
            _providerService = providerService;
            _elasticClient = elasticClient;
            _queueClient = queueClient;
            _indexDefinition = indexDefinition;
            _rulesCatalog = rulesCatalog;
            _logger = logger;
        }

        public async Task BootstrapAsync(CancellationToken cancellationToken = default)
        {
            _rulesCatalog.EnsureLoaded();

            var ruleIdsByProvider = _rulesCatalog.GetRuleIdsByProvider();
            _logger.LogInformation("Ingestion rules loaded. ProviderCount={ProviderCount}", ruleIdsByProvider.Count);
            foreach (var provider in ruleIdsByProvider)
            {
                _logger.LogInformation("Ingestion rules provider loaded. ProviderName={ProviderName} RuleCount={RuleCount} RuleIds={RuleIds}", provider.Key, provider.Value.Count, provider.Value);
            }

            var indexName = _configuration["ingestion:indexname"];
            if (string.IsNullOrWhiteSpace(indexName))
            {
                throw new InvalidOperationException("Missing required configuration value 'ingestion:indexname'.");
            }

            var indexExistsResponse = await _elasticClient.Indices.ExistsAsync(indexName, cancellationToken)
                                                          .ConfigureAwait(false);
            if (!indexExistsResponse.Exists)
            {
                await _elasticClient.Indices.CreateAsync(indexName, d => _indexDefinition.Configure(d), cancellationToken)
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

                var poisonQueueName = queueName + (_configuration["ingestion:poisonQueueSuffix"] ?? "-poison");
                var poisonQueueClient = _queueClient.GetQueueClient(poisonQueueName);
                await poisonQueueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken)
                                       .ConfigureAwait(false);
            }
        }
    }
}