using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Ingestion.Providers.FileShare.Pipeline;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Ingestion.Tests.TestEnrichers;
using UKHO.Search.Ingestion.Tests.TestNodes;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Pipeline
{
    public sealed class FileShareIngestionGraphEnrichmentFailureRoutingTests
    {
        [Fact]
        public async Task Non_transient_enrichment_failure_routes_to_index_deadletter_and_bulk_index_receives_nothing()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                                                          {
                                                              ["ingestion:laneCount"] = "1",
                                                              ["ingestion:channelCapacityPrePartition"] = "16",
                                                              ["ingestion:channelCapacityPerLane"] = "16",
                                                              ["ingestion:channelCapacityMicrobatchOut"] = "16",
                                                              ["ingestion:microbatchMaxItems"] = "10",
                                                              ["ingestion:microbatchMaxDelayMilliseconds"] = "1",
                                                              ["ingestion:documentTypePlaceholder"] = "unknown",
                                                              ["ingestion:enrichmentRetryMaxAttempts"] = "0",
                                                              ["ingestion:enrichmentRetryBaseDelayMilliseconds"] = "0",
                                                              ["ingestion:enrichmentRetryMaxDelayMilliseconds"] = "0",
                                                              ["ingestion:enrichmentRetryJitterMilliseconds"] = "0"
                                                          })
                                                          .Build();

            using var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning));

            BlockingEnvelopeSinkNode<IndexOperation>? indexDeadLetterSink = null;
            RecordingBulkIndexNode? bulkIndexNode = null;

            var ingress = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(16, false, true);

            var factories = new FileShareIngestionProcessingGraphFactories
            {
                CreateRequestDeadLetterSinkNode = (name, input, supervisor) => new BlockingEnvelopeSinkNode<IngestionRequest>(name, input, 0),

                CreateIndexDeadLetterSinkNode = (name, input, supervisor) => indexDeadLetterSink = new BlockingEnvelopeSinkNode<IndexOperation>(name, input, 0),

                CreateDiagnosticsSinkNode = (name, input, supervisor) => new BlockingEnvelopeSinkNode<IndexOperation>(name, input, 0),

                CreateBulkIndexNode = (name, lane, input, successOutput, deadLetterOutput, supervisor) => bulkIndexNode = new RecordingBulkIndexNode(name, input, successOutput, deadLetterOutput, supervisor),

                CreateAckNode = (name, lane, input, supervisor) => new BlockingEnvelopeSinkNode<IndexOperation>(name, input, 0)
            };

            var services = new ServiceCollection();
            services.AddScoped<IIngestionEnricher>(_ => new AlwaysThrowingEnricher(10, () => new InvalidOperationException("boom")));
            await using var provider = services.BuildServiceProvider();

            var graph = FileShareIngestionProcessingGraph.Build(ingress.Reader, new FileShareIngestionProcessingGraphDependencies
            {
                Configuration = configuration,
                LoggerFactory = loggerFactory,
                Factories = factories,
                ScopeFactory = provider.GetRequiredService<IServiceScopeFactory>()
            }, cts.Token);

            await graph.Supervisor.StartAsync();

            var request = new IngestionRequest(IngestionRequestType.IndexItem, new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList()), null, null);
            await ingress.Writer.WriteAsync(new Envelope<IngestionRequest>("doc-1", request), cts.Token);
            ingress.Writer.TryComplete();

            await graph.Supervisor.Completion.WaitAsync(TimeSpan.FromSeconds(5));

            bulkIndexNode.ShouldNotBeNull();
            bulkIndexNode!.Received.ShouldBeEmpty();

            indexDeadLetterSink.ShouldNotBeNull();
            await indexDeadLetterSink!.WaitForCountAsync(1, TimeSpan.FromSeconds(2));

            indexDeadLetterSink.Items.Single()
                               .Error!.Code.ShouldBe("ENRICHMENT_ERROR");
        }

        [Fact]
        public async Task Transient_retry_exhaustion_routes_to_index_deadletter_and_bulk_index_receives_nothing()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                                                          {
                                                              ["ingestion:laneCount"] = "1",
                                                              ["ingestion:channelCapacityPrePartition"] = "16",
                                                              ["ingestion:channelCapacityPerLane"] = "16",
                                                              ["ingestion:channelCapacityMicrobatchOut"] = "16",
                                                              ["ingestion:microbatchMaxItems"] = "10",
                                                              ["ingestion:microbatchMaxDelayMilliseconds"] = "1",
                                                              ["ingestion:documentTypePlaceholder"] = "unknown",
                                                              ["ingestion:enrichmentRetryMaxAttempts"] = "1",
                                                              ["ingestion:enrichmentRetryBaseDelayMilliseconds"] = "0",
                                                              ["ingestion:enrichmentRetryMaxDelayMilliseconds"] = "0",
                                                              ["ingestion:enrichmentRetryJitterMilliseconds"] = "0"
                                                          })
                                                          .Build();

            using var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning));

            BlockingEnvelopeSinkNode<IndexOperation>? indexDeadLetterSink = null;
            RecordingBulkIndexNode? bulkIndexNode = null;

            var ingress = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(16, false, true);

            var factories = new FileShareIngestionProcessingGraphFactories
            {
                CreateRequestDeadLetterSinkNode = (name, input, supervisor) => new BlockingEnvelopeSinkNode<IngestionRequest>(name, input, 0),

                CreateIndexDeadLetterSinkNode = (name, input, supervisor) => indexDeadLetterSink = new BlockingEnvelopeSinkNode<IndexOperation>(name, input, 0),

                CreateDiagnosticsSinkNode = (name, input, supervisor) => new BlockingEnvelopeSinkNode<IndexOperation>(name, input, 0),

                CreateBulkIndexNode = (name, lane, input, successOutput, deadLetterOutput, supervisor) => bulkIndexNode = new RecordingBulkIndexNode(name, input, successOutput, deadLetterOutput, supervisor),

                CreateAckNode = (name, lane, input, supervisor) => new BlockingEnvelopeSinkNode<IndexOperation>(name, input, 0)
            };

            var services = new ServiceCollection();
            services.AddScoped<IIngestionEnricher>(_ => new AlwaysThrowingEnricher(10, () => new TimeoutException("timeout")));
            await using var provider = services.BuildServiceProvider();

            var graph = FileShareIngestionProcessingGraph.Build(ingress.Reader, new FileShareIngestionProcessingGraphDependencies
            {
                Configuration = configuration,
                LoggerFactory = loggerFactory,
                Factories = factories,
                ScopeFactory = provider.GetRequiredService<IServiceScopeFactory>()
            }, cts.Token);

            await graph.Supervisor.StartAsync();

            var request = new IngestionRequest(IngestionRequestType.IndexItem, new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList()), null, null);
            await ingress.Writer.WriteAsync(new Envelope<IngestionRequest>("doc-1", request), cts.Token);
            ingress.Writer.TryComplete();
            await graph.Supervisor.Completion.WaitAsync(TimeSpan.FromSeconds(5));

            bulkIndexNode.ShouldNotBeNull();
            bulkIndexNode!.Received.ShouldBeEmpty();

            indexDeadLetterSink.ShouldNotBeNull();
            await indexDeadLetterSink!.WaitForCountAsync(1, TimeSpan.FromSeconds(2));

            indexDeadLetterSink.Items.Single()
                               .Error!.Code.ShouldBe("ENRICHMENT_RETRIES_EXHAUSTED");
        }
    }
}