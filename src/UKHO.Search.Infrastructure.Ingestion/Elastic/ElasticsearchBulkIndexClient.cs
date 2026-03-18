using System.Threading;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Pipelines.Nodes;

namespace UKHO.Search.Infrastructure.Ingestion.Elastic
{
    public sealed class ElasticsearchBulkIndexClient : IBulkIndexClient<IndexOperation>
    {
        private readonly ElasticsearchClient _client;
        private readonly CanonicalIndexDefinition _indexDefinition;
        private readonly string _indexName;
        private readonly ILogger<ElasticsearchBulkIndexClient> _logger;

        private readonly SemaphoreSlim _indexEnsureLock = new(1, 1);
        private volatile bool _indexEnsured;

        public ElasticsearchBulkIndexClient(ElasticsearchClient client, IConfiguration configuration, CanonicalIndexDefinition indexDefinition, ILogger<ElasticsearchBulkIndexClient> logger)
        {
            _client = client;
            _indexDefinition = indexDefinition;
            _logger = logger;

            _indexName = configuration["ingestion:indexname"] ?? throw new InvalidOperationException("Missing required configuration value 'ingestion:indexname'.");
        }

        public async ValueTask<BulkIndexResponse> BulkIndexAsync(BulkIndexRequest<IndexOperation> request, CancellationToken cancellationToken)
        {
            await EnsureIndexReadyAsync(cancellationToken)
                .ConfigureAwait(false);

            var bulkRequest = new BulkRequest(_indexName)
            {
                Operations = new BulkOperationsCollection()
            };

            foreach (var envelope in request.Items)
            {
                switch (envelope.Payload)
                {
                    case UpsertOperation upsert:
                        bulkRequest.Operations.Add(new BulkIndexOperation<CanonicalDocument>(upsert.Document) { Id = envelope.Key });
                        break;

                    case DeleteOperation:
                        bulkRequest.Operations.Add(new BulkDeleteOperation<CanonicalDocument>(envelope.Key));
                        break;

                    case AclUpdateOperation aclUpdate:
                        bulkRequest.Operations.Add(new BulkUpdateOperation<CanonicalDocument, object>(envelope.Key)
                        {
                            Doc = new { source = new { securityTokens = aclUpdate.SecurityTokens } }
                        });
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported IndexOperation type '{envelope.Payload.GetType().Name}'.");
                }
            }

            var response = await _client.BulkAsync(bulkRequest, cancellationToken)
                                        .ConfigureAwait(false);

            if (!response.IsValidResponse)
            {
                _logger.LogWarning("Elasticsearch bulk request returned an invalid response. DebugInformation={DebugInformation}", response.DebugInformation);
            }

            var results = new List<BulkIndexItemResult>(request.Items.Count);

            var items = response.Items.ToArray();

            for (var i = 0; i < request.Items.Count; i++)
            {
                var envelope = request.Items[i];
                var item = items[i];

                results.Add(new BulkIndexItemResult
                {
                    MessageId = envelope.MessageId,
                    StatusCode = item.Status,
                    ErrorType = item.Error?.Type,
                    ErrorReason = item.Error?.Reason
                });
            }

            return new BulkIndexResponse
            {
                Items = results
            };
        }

        private async ValueTask EnsureIndexReadyAsync(CancellationToken cancellationToken)
        {
            if (_indexEnsured)
            {
                // The index may be deleted/recreated while the service is running; re-check existence to avoid
                // Elasticsearch auto-creating the index with default dynamic mappings on the next bulk request.
                var existsResponse = await _client.Indices.ExistsAsync(_indexName, cancellationToken)
                                                        .ConfigureAwait(false);
                if (existsResponse.Exists)
                {
                    return;
                }

                _indexEnsured = false;
            }

            await _indexEnsureLock.WaitAsync(cancellationToken)
                                 .ConfigureAwait(false);
            try
            {
                if (_indexEnsured)
                {
                    return;
                }

                var existsResponse = await _client.Indices.ExistsAsync(_indexName, cancellationToken)
                                                        .ConfigureAwait(false);

                if (!existsResponse.Exists)
                {
                    var createResponse = await _client.Indices.CreateAsync(_indexName, d => _indexDefinition.Configure(d), cancellationToken)
                                                             .ConfigureAwait(false);

                    if (!createResponse.IsValidResponse)
                    {
                        _logger.LogError("Failed to create Elasticsearch index '{IndexName}'. DebugInformation={DebugInformation}", _indexName, createResponse.DebugInformation);
                        throw new InvalidOperationException($"Failed to create Elasticsearch index '{_indexName}'.");
                    }
                }

                await ValidateIndexMappingAsync(cancellationToken)
                    .ConfigureAwait(false);

                _indexEnsured = true;
            }
            finally
            {
                _indexEnsureLock.Release();
            }
        }

        private async ValueTask ValidateIndexMappingAsync(CancellationToken cancellationToken)
        {
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                var fieldCapsResponse = await _client.FieldCapsAsync(f => f.Indices(_indexName)
                                                                           .Fields("*"), cancellationToken)
                                                    .ConfigureAwait(false);

                if (fieldCapsResponse.IsValidResponse && fieldCapsResponse.Fields is not null)
                {
                    ValidateExpectedFieldMappings(fieldCapsResponse.Fields);

                    return;
                }

                if (attempt < 3)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken)
                              .ConfigureAwait(false);
                }
            }

            throw new InvalidOperationException($"Elasticsearch index '{_indexName}' did not return any field capabilities after retries.");
        }

        internal static void ValidateExpectedFieldMappings<T>(IReadOnlyDictionary<string, IReadOnlyDictionary<string, T>> fields)
        {
            // These fields should be mapped explicitly (not default dynamic 'text' with '.keyword' multi-fields).
            EnsureHasType(fields, "keywords", "keyword");
            EnsureHasType(fields, "authority", "keyword");
            EnsureHasType(fields, "region", "keyword");
            EnsureHasType(fields, "format", "keyword");
            EnsureHasType(fields, "majorVersion", "keyword");
            EnsureHasType(fields, "minorVersion", "keyword");
            EnsureHasType(fields, "category", "keyword");
            EnsureHasType(fields, "series", "keyword");
            EnsureHasType(fields, "instance", "keyword");
            EnsureHasType(fields, "searchText", "text");
            EnsureHasType(fields, "content", "text");

            // If the index was created via default dynamic mapping, these multi-fields will typically exist.
            EnsureAbsent(fields, "keywords.keyword");
            EnsureAbsent(fields, "searchText.keyword");
            EnsureAbsent(fields, "content.keyword");
        }

        private static void EnsureHasType<T>(IReadOnlyDictionary<string, IReadOnlyDictionary<string, T>> fields, string fieldName, string expectedType)
        {
            if (!fields.TryGetValue(fieldName, out var perType) || perType is null || !perType.ContainsKey(expectedType))
            {
                var types = perType is null ? "<missing>" : string.Join(",", perType.Keys);
                throw new InvalidOperationException($"Index mapping mismatch: expected field '{fieldName}' to include type '{expectedType}', but found '{types}'.");
            }
        }

        private static void EnsureAbsent<T>(IReadOnlyDictionary<string, IReadOnlyDictionary<string, T>> fields, string fieldName)
        {
            if (fields.ContainsKey(fieldName))
            {
                throw new InvalidOperationException($"Index mapping mismatch: unexpected multi-field '{fieldName}' exists; index appears to have been created with dynamic mappings.");
            }
        }
    }
}