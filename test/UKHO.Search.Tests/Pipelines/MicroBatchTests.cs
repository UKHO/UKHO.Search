using System.Threading.Channels;
using Shouldly;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class MicroBatchTests
    {
        [Fact]
        public async Task Flushes_remaining_items_on_completion()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var supervisor = new PipelineSupervisor(cts.Token);

            var sourceToBatch = BoundedChannelFactory.Create<Envelope<int>>(10, true, true);
            var batchToSink = BoundedChannelFactory.Create<BatchEnvelope<int>>(10, true, true);

            var source = new SyntheticSourceNode<int>("source", sourceToBatch.Writer, 3, 1, i => i, fatalErrorReporter: supervisor);

            var batch = new MicroBatchNode<int>("microbatch", 0, sourceToBatch.Reader, batchToSink.Writer, 10, TimeSpan.FromSeconds(5), fatalErrorReporter: supervisor);

            supervisor.AddNode(source);
            supervisor.AddNode(batch);

            await supervisor.StartAsync();
            await supervisor.Completion.WaitAsync(cts.Token);

            var batches = await ReadAllAsync(batchToSink.Reader, cts.Token);
            batches.Count.ShouldBe(1);
            batches[0]
                .Items.Count.ShouldBe(3);
            batches[0]
                .Items.Select(i => i.Payload)
                .ToArray()
                .ShouldBe(new[] { 0, 1, 2 });
        }

        [Fact]
        public async Task Flushes_when_max_delay_is_reached()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var sourceToBatch = BoundedChannelFactory.Create<Envelope<int>>(10, true, true);
            var batchToSink = BoundedChannelFactory.Create<BatchEnvelope<int>>(10, true, true);

            var maxDelay = TimeSpan.FromMilliseconds(200);

            var batch = new MicroBatchNode<int>("microbatch", 7, sourceToBatch.Reader, batchToSink.Writer, 10, maxDelay);

            await batch.StartAsync(cts.Token);

            await sourceToBatch.Writer.WriteAsync(new Envelope<int>("key-0", 1), cts.Token);

            await Task.Delay(TimeSpan.FromMilliseconds(600), cts.Token);
            sourceToBatch.Writer.TryComplete();

            await batch.Completion.WaitAsync(cts.Token);

            var batches = await ReadAllAsync(batchToSink.Reader, cts.Token);
            batches.Count.ShouldBe(1);
            batches[0]
                .PartitionId.ShouldBe(7);
            batches[0]
                .Items.Count.ShouldBe(1);
            batches[0]
                .Items[0]
                .Payload.ShouldBe(1);
            (batches[0].FlushedUtc - batches[0].CreatedUtc).ShouldBeGreaterThanOrEqualTo(maxDelay - TimeSpan.FromMilliseconds(50));
        }

        private static async Task<List<BatchEnvelope<int>>> ReadAllAsync(ChannelReader<BatchEnvelope<int>> reader, CancellationToken cancellationToken)
        {
            var result = new List<BatchEnvelope<int>>();
            await foreach (var batch in reader.ReadAllAsync(cancellationToken))
            {
                result.Add(batch);
            }

            return result;
        }
    }
}