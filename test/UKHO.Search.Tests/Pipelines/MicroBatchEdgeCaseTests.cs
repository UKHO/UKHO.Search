using System.Threading.Channels;
using Shouldly;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class MicroBatchEdgeCaseTests
    {
        [Fact]
        public async Task Max_delay_flushes_even_when_new_items_arrive_before_deadline()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var input = BoundedChannelFactory.Create<Envelope<int>>(10, true, true);
            var output = BoundedChannelFactory.Create<BatchEnvelope<int>>(10, true, true);

            var maxDelay = TimeSpan.FromMilliseconds(1500);
            var node = new MicroBatchNode<int>("microbatch", 0, input.Reader, output.Writer, 100, maxDelay);

            await node.StartAsync(cts.Token);

            await input.Writer.WriteAsync(new Envelope<int>("key-0", 0), cts.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(20), cts.Token);
            await input.Writer.WriteAsync(new Envelope<int>("key-0", 1), cts.Token);

            await Task.Delay(maxDelay + TimeSpan.FromMilliseconds(400), cts.Token);
            input.Writer.TryComplete();

            await node.Completion.WaitAsync(cts.Token);

            var batches = await ReadAllAsync(output.Reader, cts.Token);
            batches.Count.ShouldBe(1);
            batches[0]
                .Items.Select(i => i.Payload)
                .ToArray()
                .ShouldBe(new[] { 0, 1 });
        }

        [Fact]
        public async Task Faulted_input_channel_is_propagated_as_node_failure()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var input = BoundedChannelFactory.Create<Envelope<int>>(10, true, true);
            var output = BoundedChannelFactory.Create<BatchEnvelope<int>>(10, true, true);

            var node = new MicroBatchNode<int>("microbatch", 0, input.Reader, output.Writer, 10, TimeSpan.FromSeconds(5));

            await node.StartAsync(cts.Token);

            input.Writer.TryComplete(new InvalidOperationException("upstream fault"));

            await Should.ThrowAsync<InvalidOperationException>(async () => await node.Completion.WaitAsync(cts.Token));
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