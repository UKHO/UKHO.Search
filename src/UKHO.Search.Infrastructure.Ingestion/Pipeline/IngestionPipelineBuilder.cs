using System.Threading.Channels;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UKHO.Search.Infrastructure.Ingestion.Pipeline.Channels;
using UKHO.Search.Infrastructure.Ingestion.Pipeline.Nodes;
using UKHO.Search.Infrastructure.Ingestion.Pipeline.Terminal;
using UKHO.Search.Infrastructure.Ingestion.Queue;
using UKHO.Search.Ingestion.Pipeline;
using UKHO.Search.Ingestion.Pipeline.Nodes;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Ingestion.Providers.FileShare;
using UKHO.Search.Ingestion.Providers.FileShare.Pipeline.Documents;
using UKHO.Search.Ingestion.Providers.FileShare.Pipeline.Nodes;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;
using UKHO.Search.Services.Ingestion.Providers;

namespace UKHO.Search.Infrastructure.Ingestion.Pipeline
{
    public sealed class IngestionPipelineBuilder
    {
        private readonly BlobServiceClient? _blobServiceClient;
        private readonly IBulkIndexClient<IndexOperation>? _bulkIndexClient;
        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IIngestionProviderService? _providerService;
        private readonly IQueueClientFactory? _queueClientFactory;

        public IngestionPipelineBuilder(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _configuration = configuration;
            _loggerFactory = loggerFactory;
        }

        public IngestionPipelineBuilder(IConfiguration configuration, ILoggerFactory loggerFactory, IIngestionProviderService providerService, IQueueClientFactory queueClientFactory, IBulkIndexClient<IndexOperation> bulkIndexClient, BlobServiceClient blobServiceClient) : this(configuration, loggerFactory)
        {
            _providerService = providerService;
            _queueClientFactory = queueClientFactory;
            _bulkIndexClient = bulkIndexClient;
            _blobServiceClient = blobServiceClient;
        }

        public IngestionPipelineGraph BuildSynthetic(CancellationToken cancellationToken)
        {
            const string providerName = FileShareIngestionDataProviderFactory.ProviderName;

            var laneCount = _configuration.GetValue<int>("ingestion:laneCount");
            var channelCapacityPrePartition = _configuration.GetValue<int>("ingestion:channelCapacityPrePartition");
            var channelCapacityPerLane = _configuration.GetValue<int>("ingestion:channelCapacityPerLane");
            var channelCapacityMicrobatchOut = _configuration.GetValue<int>("ingestion:channelCapacityMicrobatchOut");
            var microbatchMaxItems = _configuration.GetValue<int>("ingestion:microbatchMaxItems");
            var microbatchMaxDelayMs = _configuration.GetValue<int>("ingestion:microbatchMaxDelayMilliseconds");
            var documentTypePlaceholder = _configuration.GetValue<string>("ingestion:documentTypePlaceholder");

            var enrichmentRetryMaxAttempts = _configuration.GetValue("ingestion:enrichmentRetryMaxAttempts", 5);
            var enrichmentRetryBaseDelayMs = _configuration.GetValue("ingestion:enrichmentRetryBaseDelayMilliseconds", 200);
            var enrichmentRetryMaxDelayMs = _configuration.GetValue("ingestion:enrichmentRetryMaxDelayMilliseconds", 5000);
            var enrichmentRetryJitterMs = _configuration.GetValue("ingestion:enrichmentRetryJitterMilliseconds", 250);

            if (laneCount <= 0)
            {
                throw new InvalidOperationException("ingestion:laneCount must be > 0.");
            }

            var emptyEnricherProvider = new ServiceCollection().BuildServiceProvider();
            var scopeFactory = emptyEnricherProvider.GetRequiredService<IServiceScopeFactory>();

            var supervisor = new PipelineSupervisor(cancellationToken);

            var prePartition = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(channelCapacityPrePartition, true, true);
            var validated = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(channelCapacityPrePartition, true, true);
            var deadLetter = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(channelCapacityPrePartition, true);
            var indexDeadLetter = BoundedChannelFactory.Create<Envelope<IndexOperation>>(channelCapacityPrePartition, true);

            var deadLetterWriterCompletion = new RefCountedCompletion(1 + laneCount);
            var validateDeadLetterWriter = new RefCountedChannelWriter<Envelope<IngestionRequest>>(deadLetter.Writer, deadLetterWriterCompletion);

            var source = new SyntheticSourceNode<IngestionRequest>("ingestion-source-synthetic", prePartition.Writer, 64, 8, i => CreateSyntheticRequest(i), i => $"doc-{i % 8}", _loggerFactory.CreateLogger("ingestion-source-synthetic"), supervisor);

            var validate = new IngestionRequestValidateNode("ingestion-validate", prePartition.Reader, validated.Writer, validateDeadLetterWriter, _loggerFactory.CreateLogger("ingestion-validate"), supervisor, providerName);

            var deadLetterSink = new DeadLetterPersistAndAckSinkNode<IngestionRequest>("ingestion-deadletter-request", deadLetter.Reader, Path.Combine(AppContext.BaseDirectory, "deadletter", "ingestion-request.jsonl"), logger: _loggerFactory.CreateLogger("ingestion-deadletter-request"), fatalErrorReporter: supervisor);

            var indexDeadLetterSink = new DeadLetterPersistAndAckSinkNode<IndexOperation>("ingestion-deadletter-index", indexDeadLetter.Reader, Path.Combine(AppContext.BaseDirectory, "deadletter", "ingestion-index.jsonl"), logger: _loggerFactory.CreateLogger("ingestion-deadletter-index"), fatalErrorReporter: supervisor);

            var laneDispatchChannels = new List<CountingChannel<Envelope<IngestionRequest>>>(laneCount);
            var laneDispatchWriters = new List<ChannelWriter<Envelope<IngestionRequest>>>(laneCount);
            for (var lane = 0; lane < laneCount; lane++)
            {
                var laneDispatch = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(channelCapacityPerLane, true, true);
                laneDispatchChannels.Add(laneDispatch);
                laneDispatchWriters.Add(laneDispatch.Writer);
            }

            var partition = new KeyPartitionNode<IngestionRequest>("ingestion-partition", validated.Reader, laneDispatchWriters, _loggerFactory.CreateLogger("ingestion-partition"), supervisor, providerName);

            var canonicalBuilder = new CanonicalDocumentBuilder();

            var laneSinks = new List<CollectingBatchSinkNode<IndexOperation>>(laneCount);
            for (var lane = 0; lane < laneCount; lane++)
            {
                var laneDeadLetterWriter = new RefCountedChannelWriter<Envelope<IngestionRequest>>(deadLetter.Writer, deadLetterWriterCompletion);
                var laneDispatchOut = BoundedChannelFactory.Create<Envelope<IngestionPipelineContext>>(channelCapacityPerLane, true, true);
                var laneOps = BoundedChannelFactory.Create<Envelope<IndexOperation>>(channelCapacityPerLane, true, true);
                var laneBatches = BoundedChannelFactory.Create<BatchEnvelope<IndexOperation>>(channelCapacityMicrobatchOut, true, true);

                var dispatch = new IngestionRequestDispatchNode($"ingestion-dispatch-{lane}", laneDispatchChannels[lane].Reader, laneDispatchOut.Writer, laneDeadLetterWriter, canonicalBuilder, _loggerFactory.CreateLogger($"ingestion-dispatch-{lane}"), supervisor, providerName);

                var enrich = new ApplyEnrichmentNode($"ingestion-enrich-{lane}", laneDispatchOut.Reader, laneOps.Writer, indexDeadLetter.Writer, scopeFactory, _loggerFactory.CreateLogger($"ingestion-enrich-{lane}"), supervisor, enrichmentRetryMaxAttempts, TimeSpan.FromMilliseconds(enrichmentRetryBaseDelayMs), TimeSpan.FromMilliseconds(enrichmentRetryMaxDelayMs),
                    TimeSpan.FromMilliseconds(enrichmentRetryJitterMs), providerName: providerName);

                var microBatch = new MicroBatchNode<IndexOperation>($"ingestion-microbatch-{lane}", lane, laneOps.Reader, laneBatches.Writer, microbatchMaxItems, TimeSpan.FromMilliseconds(microbatchMaxDelayMs), logger: _loggerFactory.CreateLogger($"ingestion-microbatch-{lane}"), fatalErrorReporter: supervisor, cancellationMode: CancellationMode.Drain, providerName: providerName);

                var stubSink = new CollectingBatchSinkNode<IndexOperation>($"ingestion-stub-index-{lane}", laneBatches.Reader, _loggerFactory.CreateLogger($"ingestion-stub-index-{lane}"), supervisor);

                supervisor.AddNode(dispatch);
                supervisor.AddNode(enrich);
                supervisor.AddNode(microBatch);
                supervisor.AddNode(stubSink);
                laneSinks.Add(stubSink);
            }

            supervisor.AddNode(source);
            supervisor.AddNode(validate);
            supervisor.AddNode(partition);
            supervisor.AddNode(deadLetterSink);
            supervisor.AddNode(indexDeadLetterSink);

            return new IngestionPipelineGraph
            {
                Supervisor = supervisor,
                LaneSinks = laneSinks
            };
        }

        private static IngestionRequest CreateSyntheticRequest(int sequence)
        {
            var tokens = new[] { "t1" };
            var properties = new IngestionPropertyList
            {
                new IngestionProperty
                {
                    Name = "sequence",
                    Type = IngestionPropertyType.Integer,
                    Value = sequence
                }
            };

            var updateItem = new IndexRequest($"doc-{sequence % 8}", properties, tokens, DateTimeOffset.UnixEpoch, new IngestionFileList());

            return new IngestionRequest(IngestionRequestType.IndexItem, updateItem, null, null);
        }
    }
}