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
        private readonly ElasticsearchClient _elasticClient;
        private readonly CanonicalIndexDefinition _indexDefinition;
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
                var createResponse = await _elasticClient.Indices.CreateAsync(indexName, d => _indexDefinition.Configure(d), cancellationToken)
                                                     .ConfigureAwait(false);

                if (!createResponse.IsValidResponse)
                {
                    _logger.LogError("Failed to create Elasticsearch index '{IndexName}'. DebugInformation={DebugInformation}", indexName, createResponse.DebugInformation);
                    throw new InvalidOperationException($"Failed to create Elasticsearch index '{indexName}'.");
                }
            }

            await ValidateIndexMappingAsync(indexName, cancellationToken)
                .ConfigureAwait(false);

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

        private async ValueTask ValidateIndexMappingAsync(string indexName, CancellationToken cancellationToken)
        {
            var existsResponse = await _elasticClient.Indices.ExistsAsync(indexName, cancellationToken)
                                                    .ConfigureAwait(false);
            if (!existsResponse.Exists)
            {
                _logger.LogInformation("Skipping index mapping validation because index '{IndexName}' does not exist yet.", indexName);
                return;
            }

            var fieldCapsResponse = await _elasticClient.FieldCapsAsync(f => f.Indices(indexName)
                                                                             .Fields("*"), cancellationToken)
                                                        .ConfigureAwait(false);

            if (!fieldCapsResponse.IsValidResponse)
            {
                _logger.LogWarning("Failed to read field capabilities for index '{IndexName}'. DebugInformation={DebugInformation}", indexName, fieldCapsResponse.DebugInformation);
                return;
            }

            var fields = fieldCapsResponse.Fields;
            if (fields is null)
            {
                _logger.LogWarning("Field capabilities response for index '{IndexName}' did not include any fields; skipping mapping validation.", indexName);
                return;
            }

            ElasticsearchBulkIndexClient.ValidateExpectedFieldMappings(fields);
        }
    }
}