using Shouldly;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class MicroBatchMaxBytesTests
    {
        [Fact]
        public async Task Flushes_when_max_bytes_is_reached_and_does_not_exceed_batch_size()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var input = BoundedChannelFactory.Create<Envelope<int>>(10, true, true);
            var output = BoundedChannelFactory.Create<BatchEnvelope<int>>(10, true, true);

            static int EstimateBytes(int payload)
            {
                return 10;
            }

            var node = new MicroBatchNode<int>("microbatch", 0, input.Reader, output.Writer, 100, TimeSpan.FromSeconds(5), 20, EstimateBytes);

            await node.StartAsync(cts.Token);

            await input.Writer.WriteAsync(new Envelope<int>("key-0", 0), cts.Token);
            await input.Writer.WriteAsync(new Envelope<int>("key-0", 1), cts.Token);
            await input.Writer.WriteAsync(new Envelope<int>("key-0", 2), cts.Token);

            input.Writer.TryComplete();
            await node.Completion.WaitAsync(cts.Token);

            var batch1 = await output.Reader.ReadAsync(cts.Token);
            var batch2 = await output.Reader.ReadAsync(cts.Token);

            batch1.Items.Count.ShouldBe(2);
            batch2.Items.Count.ShouldBe(1);
        }

        [Fact]
        public async Task Batch_envelope_contains_aggregate_context()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var input = BoundedChannelFactory.Create<Envelope<int>>(10, true, true);
            var output = BoundedChannelFactory.Create<BatchEnvelope<int>>(10, true, true);

            static int EstimateBytes(int payload)
            {
                return payload;
            }

            var node = new MicroBatchNode<int>("microbatch", 0, input.Reader, output.Writer, 100, TimeSpan.FromSeconds(5), null, EstimateBytes);

            await node.StartAsync(cts.Token);

            var t1 = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var t2 = new DateTimeOffset(2025, 1, 1, 0, 0, 1, TimeSpan.Zero);

            await input.Writer.WriteAsync(new Envelope<int>("key-0", 5) { TimestampUtc = t1 }, cts.Token);
            await input.Writer.WriteAsync(new Envelope<int>("key-0", 7) { TimestampUtc = t2 }, cts.Token);
            input.Writer.TryComplete();

            await node.Completion.WaitAsync(cts.Token);

            var batch = await output.Reader.ReadAsync(cts.Token);
            batch.ItemCount.ShouldBe(2);
            batch.TotalEstimatedBytes.ShouldBe(12);
            batch.MinItemTimestampUtc.ShouldBe(t1);
            batch.MaxItemTimestampUtc.ShouldBe(t2);
        }
    }
}