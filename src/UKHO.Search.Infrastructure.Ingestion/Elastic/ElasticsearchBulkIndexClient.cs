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
        private readonly string _indexName;
        private readonly ILogger<ElasticsearchBulkIndexClient> _logger;

        public ElasticsearchBulkIndexClient(ElasticsearchClient client, IConfiguration configuration, ILogger<ElasticsearchBulkIndexClient> logger)
        {
            _client = client;
            _logger = logger;

            _indexName = configuration["ingestion:indexname"] ?? throw new InvalidOperationException("Missing required configuration value 'ingestion:indexname'.");
        }

        public async ValueTask<BulkIndexResponse> BulkIndexAsync(BulkIndexRequest<IndexOperation> request, CancellationToken cancellationToken)
        {
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
    }
}