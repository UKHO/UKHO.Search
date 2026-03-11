using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Configuration;
using UKHO.Search.Infrastructure.Ingestion.Statistics.Models;

namespace UKHO.Search.Infrastructure.Ingestion.Statistics
{
    public sealed class StatisticsService : IStatisticsService
    {
        private readonly IConfiguration _configuration;
        private readonly ElasticsearchClient _elasticClient;

        public StatisticsService(IConfiguration configuration, ElasticsearchClient elasticClient)
        {
            _configuration = configuration;
            _elasticClient = elasticClient;
        }

        public async Task<IndexStatistics> GetIndexStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var indexName = _configuration["ingestion:indexname"];
            if (string.IsNullOrWhiteSpace(indexName))
            {
                throw new InvalidOperationException("Missing required configuration value 'ingestion:indexname'.");
            }

            var existsResponse = await _elasticClient.Indices.ExistsAsync(indexName, cancellationToken)
                                                     .ConfigureAwait(false);
            if (!existsResponse.Exists)
            {
                return new IndexStatistics { IndexName = indexName, Exists = false };
            }

            var mappingResponse = await _elasticClient.Indices.GetMappingAsync(g => g.Indices(indexName), cancellationToken)
                                                      .ConfigureAwait(false);

            var settingsResponse = await _elasticClient.Indices.GetSettingsAsync(g => g.Indices(indexName), cancellationToken)
                                                       .ConfigureAwait(false);

            var healthResponse = await _elasticClient.Cluster.HealthAsync(h => h.Indices(indexName), cancellationToken)
                                                     .ConfigureAwait(false);

            var countResponse = await _elasticClient.CountAsync<object>(c => c.Indices(indexName), cancellationToken)
                                                    .ConfigureAwait(false);

            var statsResponse = await _elasticClient.Indices.StatsAsync(s => s.Indices(indexName), cancellationToken)
                                                    .ConfigureAwait(false);

            var fieldCapsResponse = await _elasticClient.FieldCapsAsync(f => f.Indices(indexName)
                                                                             .Fields("*"), cancellationToken)
                                                        .ConfigureAwait(false);

            return new IndexStatistics
            {
                IndexName = indexName,
                Exists = true,
                Health = new IndexHealth
                {
                    Status = healthResponse.Status.ToString(),
                    NumberOfNodes = healthResponse.NumberOfNodes,
                    NumberOfDataNodes = healthResponse.NumberOfDataNodes,
                    ActivePrimaryShards = healthResponse.ActivePrimaryShards,
                    ActiveShards = healthResponse.ActiveShards,
                    RelocatingShards = healthResponse.RelocatingShards,
                    InitializingShards = healthResponse.InitializingShards,
                    UnassignedShards = healthResponse.UnassignedShards,
                    ActiveShardsPercentAsNumber = healthResponse.ActiveShardsPercentAsNumber
                },
                Mapping = new IndexMapping { Raw = mappingResponse },
                Settings = new IndexSettings { Raw = settingsResponse },
                Documents = new IndexDocumentStatistics
                {
                    Count = countResponse.Count
                },
                FieldCapabilities = ParseFieldCapabilities(fieldCapsResponse),
                Client = new IndexStatisticsClientDetails
                {
                    Exists = existsResponse,
                    Mapping = mappingResponse,
                    Settings = settingsResponse,
                    Health = healthResponse,
                    Count = countResponse,
                    Stats = statsResponse,
                    FieldCaps = fieldCapsResponse
                }
            };
        }

        private static IReadOnlyDictionary<string, IndexFieldCapabilities> ParseFieldCapabilities(FieldCapsResponse fieldCapsResponse)
        {
            if (fieldCapsResponse.Fields is null)
            {
                return new Dictionary<string, IndexFieldCapabilities>();
            }

            var result = new Dictionary<string, IndexFieldCapabilities>(StringComparer.Ordinal);

            foreach (var (fieldName, perType) in fieldCapsResponse.Fields)
            {
                if (perType is null)
                {
                    continue;
                }

                var isSearchable = perType.Values.Any(v => v.Searchable);
                var isAggregatable = perType.Values.Any(v => v.Aggregatable);
                var types = perType.Keys.ToArray();

                result[fieldName] = new IndexFieldCapabilities
                {
                    IsSearchable = isSearchable,
                    IsAggregatable = isAggregatable,
                    Types = types
                };
            }

            return result;
        }
    }
}