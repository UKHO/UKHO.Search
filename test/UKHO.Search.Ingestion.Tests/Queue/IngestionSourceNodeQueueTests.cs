using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Pipeline.Terminal;
using UKHO.Search.Infrastructure.Ingestion.Queue;
using UKHO.Search.Ingestion.Providers.FileShare;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Ingestion.Requests.Serialization;
using UKHO.Search.Ingestion.Tests.TestProviders;
using UKHO.Search.Ingestion.Tests.TestQueues;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Queue
{
    public sealed class IngestionSourceNodeQueueTests
    {
        [Fact]
        public async Task Source_creates_provider_and_poison_queues_at_startup()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                                                          {
                                                              ["ingestion:queueReceiveBatchSize"] = "16",
                                                              ["ingestion:queueVisibilityTimeoutSeconds"] = "30",
                                                              ["ingestion:queueVisibilityRenewalSeconds"] = "30",
                                                              ["ingestion:queuePollingIntervalMilliseconds"] = "1000",
                                                              ["ingestion:queueMaxDequeueCount"] = "5",
                                                              ["ingestion:poisonQueueSuffix"] = "-poison"
                                                          })
                                                          .Build();

            var queueFactory = new FakeQueueClientFactory();
            var queue = queueFactory.GetOrAdd("q");
            var poison = queueFactory.GetOrAdd("q-poison");

            var providerFactory = new FileShareIngestionDataProviderFactory("q");
            var providerService = new SingleProviderService(providerFactory);

            var output = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(10, true, true);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            var node = new IngestionSourceNode("source", output.Writer, configuration, providerService, queueFactory, NullLogger.Instance);

            await node.StartAsync(cts.Token);

            await queue.CreateCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await poison.CreateCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));

            queue.CreateCallCount.ShouldBe(1);
            poison.CreateCallCount.ShouldBe(1);

            cts.Cancel();

            try
            {
                await node.Completion.WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch
            {
            }
        }

        [Fact]
        public async Task Poison_queue_name_uses_configured_suffix()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                                                          {
                                                              ["ingestion:queueReceiveBatchSize"] = "16",
                                                              ["ingestion:queueVisibilityTimeoutSeconds"] = "30",
                                                              ["ingestion:queueVisibilityRenewalSeconds"] = "30",
                                                              ["ingestion:queuePollingIntervalMilliseconds"] = "1000",
                                                              ["ingestion:queueMaxDequeueCount"] = "5",
                                                              ["ingestion:poisonQueueSuffix"] = "-dead"
                                                          })
                                                          .Build();

            var queueFactory = new FakeQueueClientFactory();
            var queue = queueFactory.GetOrAdd("q");
            var poison = queueFactory.GetOrAdd("q-dead");

            var providerFactory = new FileShareIngestionDataProviderFactory("q");
            var providerService = new SingleProviderService(providerFactory);

            var output = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(10, true, true);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            var node = new IngestionSourceNode("source", output.Writer, configuration, providerService, queueFactory, NullLogger.Instance);

            await node.StartAsync(cts.Token);

            await queue.CreateCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await poison.CreateCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));

            queueFactory.RequestedQueueNames.ShouldContain("q");
            queueFactory.RequestedQueueNames.ShouldContain("q-dead");

            cts.Cancel();

            try
            {
                await node.Completion.WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch
            {
            }
        }

        [Fact]
        public async Task DequeueCount_exceeding_max_moves_message_to_poison_and_deletes_original()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                                                          {
                                                              ["ingestion:queueReceiveBatchSize"] = "16",
                                                              ["ingestion:queueVisibilityTimeoutSeconds"] = "30",
                                                              ["ingestion:queueVisibilityRenewalSeconds"] = "30",
                                                              ["ingestion:queuePollingIntervalMilliseconds"] = "1",
                                                              ["ingestion:queueMaxDequeueCount"] = "5",
                                                              ["ingestion:poisonQueueSuffix"] = "-poison"
                                                          })
                                                          .Build();

            var queueFactory = new FakeQueueClientFactory();
            var queue = queueFactory.GetOrAdd("q");
            var poison = queueFactory.GetOrAdd("q-poison");

            queue.Enqueue(new QueueReceivedMessage
            {
                MessageId = "m1",
                PopReceipt = "pr1",
                DequeueCount = 6,
                MessageText = "{\"bad\":true}"
            });

            var providerFactory = new FileShareIngestionDataProviderFactory("q");
            var providerService = new SingleProviderService(providerFactory);

            var output = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(10, true, true);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            var node = new IngestionSourceNode("source", output.Writer, configuration, providerService, queueFactory, NullLogger.Instance);

            await node.StartAsync(cts.Token);

            await poison.SendCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await queue.DeleteCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));

            output.Reader.TryRead(out var _)
                  .ShouldBeFalse();

            poison.SentMessages.Count.ShouldBe(1);
            queue.DeletedMessages.Count.ShouldBe(1);

            cts.Cancel();

            try
            {
                await node.Completion.WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch
            {
            }
        }

        [Fact]
        public async Task Terminal_ack_deletes_queue_message_only_after_pipeline_success()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                                                          {
                                                              ["ingestion:queueReceiveBatchSize"] = "16",
                                                              ["ingestion:queueVisibilityTimeoutSeconds"] = "30",
                                                              ["ingestion:queueVisibilityRenewalSeconds"] = "30",
                                                              ["ingestion:queuePollingIntervalMilliseconds"] = "1",
                                                              ["ingestion:queueMaxDequeueCount"] = "5",
                                                              ["ingestion:poisonQueueSuffix"] = "-poison"
                                                          })
                                                          .Build();

            var request = new IngestionRequest(IngestionRequestType.UpdateItem, null, new UpdateItemRequest("doc-1", Array.Empty<IngestionProperty>(), new[] { "t1" }), null, null);

            var messageBody = JsonSerializer.Serialize(request, IngestionJsonSerializerOptions.Create());

            var queueFactory = new FakeQueueClientFactory();
            var queue = queueFactory.GetOrAdd("q");

            queue.Enqueue(new QueueReceivedMessage
            {
                MessageId = "m1",
                PopReceipt = "pr1",
                DequeueCount = 1,
                MessageText = messageBody
            });

            var providerFactory = new FileShareIngestionDataProviderFactory("q");
            var providerService = new SingleProviderService(providerFactory);

            var output = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(10, true, true);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            var node = new IngestionSourceNode("source", output.Writer, configuration, providerService, queueFactory, NullLogger.Instance);

            await node.StartAsync(cts.Token);

            var hasItem = await output.Reader.WaitToReadAsync(cts.Token);
            hasItem.ShouldBeTrue();

            output.Reader.TryRead(out var envelope)
                  .ShouldBeTrue();
            envelope.Key.ShouldBe("doc-1");

            envelope.Headers["queueName"]
                    .ShouldBe("q");
            envelope.Headers["queueMessageId"]
                    .ShouldBe("m1");

            envelope.Context.TryGetItem<IQueueMessageAcker>(QueueEnvelopeContextKeys.MessageAcker, out var acker)
                    .ShouldBeTrue();
            acker.ShouldNotBeNull();

            queue.DeletedMessages.Count.ShouldBe(0);

            var ackChannel = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(1, true, true);
            var ack = new AckSinkNode<IngestionRequest>("ack", ackChannel.Reader, NullLogger.Instance);
            await ack.StartAsync(CancellationToken.None);

            await ackChannel.Writer.WriteAsync(envelope);
            ackChannel.Writer.TryComplete();

            await ack.Completion.WaitAsync(TimeSpan.FromSeconds(2));

            queue.DeletedMessages.Count.ShouldBe(1);

            cts.Cancel();

            try
            {
                await node.Completion.WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch
            {
            }
        }
    }
}