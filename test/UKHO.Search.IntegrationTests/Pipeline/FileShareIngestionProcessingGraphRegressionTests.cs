using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Ingestion.Providers.FileShare;
using UKHO.Search.Ingestion.Providers.FileShare.Pipeline;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Ingestion.Tests.TestEnrichers;
using UKHO.Search.Ingestion.Tests.TestNodes;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Pipeline
{
    public sealed class FileShareIngestionProcessingGraphRegressionTests
    {
        [Fact]
        public async Task Transient_enrichment_failure_then_success_retries_and_reaches_ack()
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

            BlockingEnvelopeSinkNode<IndexOperation>? ackSink = null;
            BlockingEnvelopeSinkNode<IndexOperation>? indexDeadLetterSink = null;
            RecordingBulkIndexNode? bulkIndexNode = null;

            var ingress = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(16, false, true);

            var factories = new FileShareIngestionProcessingGraphFactories
            {
                CreateRequestDeadLetterSinkNode = (name, input, supervisor) => new BlockingEnvelopeSinkNode<IngestionRequest>(name, input, 0),
                CreateIndexDeadLetterSinkNode = (name, input, supervisor) => indexDeadLetterSink = new BlockingEnvelopeSinkNode<IndexOperation>(name, input, 0),
                CreateDiagnosticsSinkNode = (name, input, supervisor) => new BlockingEnvelopeSinkNode<IndexOperation>(name, input, 0),
                CreateBulkIndexNode = (name, lane, input, successOutput, deadLetterOutput, supervisor) => bulkIndexNode = new RecordingBulkIndexNode(name, input, successOutput, deadLetterOutput, supervisor),
                CreateAckNode = (name, lane, input, supervisor) => ackSink = new BlockingEnvelopeSinkNode<IndexOperation>(name, input, 0)
            };

            var services = new ServiceCollection();
            services.AddScoped<IIngestionEnricher>(_ => new TitleSettingEnricher("Graph Title", 0));
            services.AddScoped<IIngestionEnricher>(_ => new FailingEnricher(10, 1, _ => new TimeoutException("timeout")));
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
            var envelope = new Envelope<IngestionRequest>("doc-1", request);
            envelope.Context.SetItem("test:context", 123);

            await ingress.Writer.WriteAsync(envelope, cts.Token);
            ingress.Writer.TryComplete();

            await graph.Supervisor.Completion.WaitAsync(TimeSpan.FromSeconds(5));

            bulkIndexNode.ShouldNotBeNull();
            bulkIndexNode!.Received.Count.ShouldBe(1);

            indexDeadLetterSink.ShouldNotBeNull();
            indexDeadLetterSink!.Items.ShouldBeEmpty();

            ackSink.ShouldNotBeNull();
            await ackSink!.WaitForCountAsync(1, TimeSpan.FromSeconds(2));

            ackSink.Items.Single()
                   .Context.TryGetItem<int>("test:context", out var value)
                   .ShouldBeTrue();
            value.ShouldBe(123);
        }

        [Fact]
        public async Task Validation_failure_routes_to_request_deadletter_and_bulk_index_receives_nothing()
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

            BlockingEnvelopeSinkNode<IngestionRequest>? requestDeadLetterSink = null;
            BlockingEnvelopeSinkNode<IndexOperation>? ackSink = null;
            RecordingBulkIndexNode? bulkIndexNode = null;

            var ingress = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(16, false, true);

            var factories = new FileShareIngestionProcessingGraphFactories
            {
                CreateRequestDeadLetterSinkNode = (name, input, supervisor) => requestDeadLetterSink = new BlockingEnvelopeSinkNode<IngestionRequest>(name, input, 0),
                CreateIndexDeadLetterSinkNode = (name, input, supervisor) => new BlockingEnvelopeSinkNode<IndexOperation>(name, input, 0),
                CreateDiagnosticsSinkNode = (name, input, supervisor) => new BlockingEnvelopeSinkNode<IndexOperation>(name, input, 0),
                CreateBulkIndexNode = (name, lane, input, successOutput, deadLetterOutput, supervisor) => bulkIndexNode = new RecordingBulkIndexNode(name, input, successOutput, deadLetterOutput, supervisor),
                CreateAckNode = (name, lane, input, supervisor) => ackSink = new BlockingEnvelopeSinkNode<IndexOperation>(name, input, 0)
            };

            var services = new ServiceCollection();
            services.AddScoped<IIngestionEnricher>(_ => new TitleSettingEnricher("Graph Title"));
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
            await ingress.Writer.WriteAsync(new Envelope<IngestionRequest>("doc-2", request), cts.Token);
            ingress.Writer.TryComplete();

            await graph.Supervisor.Completion.WaitAsync(TimeSpan.FromSeconds(5));

            bulkIndexNode.ShouldNotBeNull();
            bulkIndexNode!.Received.ShouldBeEmpty();

            ackSink.ShouldNotBeNull();
            ackSink!.Items.ShouldBeEmpty();

            requestDeadLetterSink.ShouldNotBeNull();
            await requestDeadLetterSink!.WaitForCountAsync(1, TimeSpan.FromSeconds(2));
            requestDeadLetterSink.Items.Single()
                                 .Error!.Code.ShouldBe("KEY_ID_MISMATCH");
        }

        [Fact]
        public async Task Bulk_index_failure_routes_to_index_deadletter_and_ack_receives_nothing()
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
            BlockingEnvelopeSinkNode<IndexOperation>? ackSink = null;

            var ingress = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(16, false, true);

            var factories = new FileShareIngestionProcessingGraphFactories
            {
                CreateRequestDeadLetterSinkNode = (name, input, supervisor) => new BlockingEnvelopeSinkNode<IngestionRequest>(name, input, 0),
                CreateIndexDeadLetterSinkNode = (name, input, supervisor) => indexDeadLetterSink = new BlockingEnvelopeSinkNode<IndexOperation>(name, input, 0),
                CreateDiagnosticsSinkNode = (name, input, supervisor) => new BlockingEnvelopeSinkNode<IndexOperation>(name, input, 0),
                CreateBulkIndexNode = (name, lane, input, successOutput, deadLetterOutput, supervisor) => new DeadLetteringBulkIndexNode(name, input, successOutput, deadLetterOutput, supervisor),
                CreateAckNode = (name, lane, input, supervisor) => ackSink = new BlockingEnvelopeSinkNode<IndexOperation>(name, input, 0)
            };

            var services = new ServiceCollection();
            services.AddScoped<IIngestionEnricher>(_ => new TitleSettingEnricher("Graph Title"));
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

            ackSink.ShouldNotBeNull();
            ackSink!.Items.ShouldBeEmpty();

            indexDeadLetterSink.ShouldNotBeNull();
            await indexDeadLetterSink!.WaitForCountAsync(1, TimeSpan.FromSeconds(2));
            indexDeadLetterSink.Items.Single()
                               .Error!.Code.ShouldBe("BULK_INDEX_ERROR");
        }

        [Fact]
        public async Task Provider_dispose_drains_inflight_work_before_returning()
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

            BlockingBulkIndexNode? bulkIndexNode = null;

            var factories = new FileShareIngestionProcessingGraphFactories
            {
                CreateRequestDeadLetterSinkNode = (name, input, supervisor) => new BlockingEnvelopeSinkNode<IngestionRequest>(name, input, 0),
                CreateIndexDeadLetterSinkNode = (name, input, supervisor) => new BlockingEnvelopeSinkNode<IndexOperation>(name, input, 0),
                CreateDiagnosticsSinkNode = (name, input, supervisor) => new BlockingEnvelopeSinkNode<IndexOperation>(name, input, 0),
                CreateBulkIndexNode = (name, lane, input, successOutput, deadLetterOutput, supervisor) => bulkIndexNode = new BlockingBulkIndexNode(name, input, successOutput, deadLetterOutput, supervisor),
                CreateAckNode = (name, lane, input, supervisor) => new BlockingEnvelopeSinkNode<IndexOperation>(name, input, 0)
            };

            var services = new ServiceCollection();
            services.AddScoped<IIngestionEnricher>(_ => new TitleSettingEnricher("Graph Title"));
            await using var root = services.BuildServiceProvider();

            var dependencies = new FileShareIngestionProcessingGraphDependencies
            {
                Configuration = configuration,
                LoggerFactory = loggerFactory,
                Factories = factories,
                ScopeFactory = root.GetRequiredService<IServiceScopeFactory>()
            };

            await using var provider = new FileShareIngestionDataProvider(dependencies, 16, loggerFactory.CreateLogger<FileShareIngestionDataProvider>());

            var request = new IngestionRequest(IngestionRequestType.IndexItem, new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList()), null, null);
            await provider.ProcessIngestionRequestAsync(new Envelope<IngestionRequest>("doc-1", request), cts.Token);

            bulkIndexNode.ShouldNotBeNull();
            await bulkIndexNode!.FirstBatchReceived.Task.WaitAsync(TimeSpan.FromSeconds(2));

            var disposeTask = provider.DisposeAsync()
                                      .AsTask();

            await Task.Delay(TimeSpan.FromMilliseconds(100), cts.Token);
            disposeTask.IsCompleted.ShouldBeFalse();

            bulkIndexNode.Release();

            await disposeTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
    }
}