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
    public sealed class FileShareIngestionGraphEntrypointTests
    {
        [Fact]
        public async Task Entrypoint_starts_graph_that_produces_index_operations()
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
                                                              ["ingestion:documentTypePlaceholder"] = "unknown"
                                                          })
                                                          .Build();

            using var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning));

            BlockingEnvelopeSinkNode<IndexOperation>? ackSink = null;

            var ingress = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(16, false, true);

            var factories = new FileShareIngestionProcessingGraphFactories
            {
                CreateRequestDeadLetterSinkNode = (name, input, supervisor) => new BlockingEnvelopeSinkNode<IngestionRequest>(name, input, 0),

                CreateIndexDeadLetterSinkNode = (name, input, supervisor) => new BlockingEnvelopeSinkNode<IndexOperation>(name, input, 0),

                CreateDiagnosticsSinkNode = (name, input, supervisor) => new BlockingEnvelopeSinkNode<IndexOperation>(name, input, 0),

                CreateBulkIndexNode = (name, lane, input, successOutput, deadLetterOutput, supervisor) => new PassthroughBulkIndexNode(name, input, successOutput, deadLetterOutput, supervisor),

                CreateAckNode = (name, lane, input, supervisor) => ackSink = new BlockingEnvelopeSinkNode<IndexOperation>(name, input, 0)
            };

            using var provider = new ServiceCollection().AddScoped<IIngestionEnricher>(_ => new TitleSettingEnricher("Entrypoint Title"))
                                                     .BuildServiceProvider();

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
            await ackSink!.WaitForCountAsync(1, TimeSpan.FromSeconds(2));

            var op = ackSink.Items.Single()
                            .Payload;
            var upsert = op.ShouldBeOfType<UpsertOperation>();
            upsert.Document.Id.ShouldBe("doc-1");
            upsert.Document.Provider.ShouldBe("file-share");
        }
    }
}