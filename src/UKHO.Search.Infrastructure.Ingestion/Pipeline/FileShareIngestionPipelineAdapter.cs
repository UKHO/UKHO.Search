using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UKHO.Search.Infrastructure.Ingestion.DeadLetter;
using UKHO.Search.Infrastructure.Ingestion.Diagnostics;
using UKHO.Search.Infrastructure.Ingestion.Pipeline.Nodes;
using UKHO.Search.Infrastructure.Ingestion.Pipeline.Terminal;
using UKHO.Search.Infrastructure.Ingestion.Queue;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Ingestion.Providers.FileShare.Pipeline;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Services.Ingestion.Providers;

namespace UKHO.Search.Infrastructure.Ingestion.Pipeline
{
    public sealed class FileShareIngestionPipelineAdapter
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly IBulkIndexClient<IndexOperation> _bulkIndexClient;
        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IIngestionProviderService _providerService;
        private readonly IQueueClientFactory _queueClientFactory;
        private readonly IServiceScopeFactory _scopeFactory;

        public FileShareIngestionPipelineAdapter(IConfiguration configuration, ILoggerFactory loggerFactory, IIngestionProviderService providerService, IQueueClientFactory queueClientFactory, IBulkIndexClient<IndexOperation> bulkIndexClient, BlobServiceClient blobServiceClient, IServiceScopeFactory scopeFactory)
        {
            _configuration = configuration;
            _loggerFactory = loggerFactory;
            _providerService = providerService;
            _queueClientFactory = queueClientFactory;
            _bulkIndexClient = bulkIndexClient;
            _blobServiceClient = blobServiceClient;
            _scopeFactory = scopeFactory;
        }

        public FileShareIngestionGraphHandle BuildAzureQueueBacked(CancellationToken cancellationToken)
        {
            var indexRetryMaxAttempts = _configuration.GetValue<int>("ingestion:indexRetryMaxAttempts");
            var indexRetryBaseDelayMs = _configuration.GetValue<int>("ingestion:indexRetryBaseDelayMilliseconds");
            var indexRetryMaxDelayMs = _configuration.GetValue<int>("ingestion:indexRetryMaxDelayMilliseconds");
            var indexRetryJitterMs = _configuration.GetValue<int>("ingestion:indexRetryJitterMilliseconds");

            var factories = new FileShareIngestionGraphFactories
            {
                CreateSourceNode = (name, output, supervisor) => new IngestionSourceNode(name, output, _configuration, _providerService, _queueClientFactory, _loggerFactory.CreateLogger(name), supervisor),

                CreateRequestDeadLetterSinkNode = (name, input, supervisor) => new BlobDeadLetterSinkNode<IngestionRequest>(name, input, _blobServiceClient, _configuration, _configuration.GetValue("ingestion:deadletterFatalIfCannotPersist", true), logger: _loggerFactory.CreateLogger(name), fatalErrorReporter: supervisor),

                CreateIndexDeadLetterSinkNode = (name, input, supervisor) => new BlobDeadLetterSinkNode<IndexOperation>(name, input, _blobServiceClient, _configuration, _configuration.GetValue("ingestion:deadletterFatalIfCannotPersist", true), logger: _loggerFactory.CreateLogger(name), fatalErrorReporter: supervisor),

                CreateDiagnosticsSinkNode = (name, input, supervisor) => new DiagnosticsSinkNode<IndexOperation>(name, input, _loggerFactory.CreateLogger(name), supervisor),

                CreateBulkIndexNode = (name, lane, input, successOutput, deadLetterOutput, supervisor) => new InOrderBulkIndexNode(name, input, _bulkIndexClient, successOutput, deadLetterOutput, indexRetryMaxAttempts, TimeSpan.FromMilliseconds(indexRetryBaseDelayMs), TimeSpan.FromMilliseconds(indexRetryMaxDelayMs), TimeSpan.FromMilliseconds(indexRetryJitterMs),
                    logger: _loggerFactory.CreateLogger(name), fatalErrorReporter: supervisor),

                CreateAckNode = (name, lane, input, supervisor) => new AckSinkNode<IndexOperation>(name, input, _loggerFactory.CreateLogger(name), supervisor)
            };

            return FileShareIngestionGraph.BuildAzureQueueBacked(new FileShareIngestionGraphDependencies
            {
                Configuration = _configuration,
                LoggerFactory = _loggerFactory,
                Factories = factories,
                ScopeFactory = _scopeFactory
            }, cancellationToken);
        }
    }
}