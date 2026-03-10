using System.Threading.Channels;
using Shouldly;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class MicroBatchMaxItemsTests
    {
        [Fact]
        public async Task Flushes_when_max_items_is_reached_and_does_not_exceed_batch_size()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var input = BoundedChannelFactory.Create<Envelope<int>>(10, true, true);
            var output = BoundedChannelFactory.Create<BatchEnvelope<int>>(10, true, true);

            var node = new MicroBatchNode<int>("microbatch", 0, input.Reader, output.Writer, 2, TimeSpan.FromSeconds(5));

            await node.StartAsync(cts.Token);

            await input.Writer.WriteAsync(new Envelope<int>("key-0", 0), cts.Token);
            await input.Writer.WriteAsync(new Envelope<int>("key-0", 1), cts.Token);
            await input.Writer.WriteAsync(new Envelope<int>("key-0", 2), cts.Token);

            input.Writer.TryComplete();
            await node.Completion.WaitAsync(cts.Token);

            var batches = await ReadAllAsync(output.Reader, cts.Token);
            batches.Count.ShouldBe(2);
            batches[0]
                .Items.Select(i => i.Payload)
                .ToArray()
                .ShouldBe(new[] { 0, 1 });
            batches[1]
                .Items.Select(i => i.Payload)
                .ToArray()
                .ShouldBe(new[] { 2 });
            batches[0]
                .Items.Count.ShouldBe(2);
            batches[1]
                .Items.Count.ShouldBe(1);
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