using Shouldly;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class BroadcastNodeTests
    {
        [Fact]
        public async Task AllMustReceive_blocks_when_any_output_is_backpressured()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var input = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var output1 = BoundedChannelFactory.Create<Envelope<int>>(1, true, true);
            var output2 = BoundedChannelFactory.Create<Envelope<int>>(1, true, true);

            // Occupy output2 so the broadcast must wait before writing to any outputs.
            output2.Writer.TryWrite(new Envelope<int>("occupy", -1));

            var node = new BroadcastNode<int>("broadcast", input.Reader, new[] { output1.Writer, output2.Writer }, mode: BroadcastMode.AllMustReceive);

            await node.StartAsync(cts.Token);

            await input.Writer.WriteAsync(new Envelope<int>("key", 1), cts.Token);

            await Task.Delay(TimeSpan.FromMilliseconds(100), cts.Token);
            output1.Reader.TryRead(out var _)
                   .ShouldBeFalse();

            // Free output2 and observe the message is delivered to both outputs.
            (await output2.Reader.ReadAsync(cts.Token)).Payload.ShouldBe(-1);

            (await output1.Reader.ReadAsync(cts.Token)).Payload.ShouldBe(1);
            (await output2.Reader.ReadAsync(cts.Token)).Payload.ShouldBe(1);

            input.Writer.TryComplete();
            await node.Completion.WaitAsync(cts.Token);
        }

        [Fact]
        public async Task BestEffort_does_not_block_required_outputs_when_optional_output_is_backpressured()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var input = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var required = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var optional = BoundedChannelFactory.Create<Envelope<int>>(1, true, true);

            // Occupy optional so it is backpressured.
            optional.Writer.TryWrite(new Envelope<int>("occupy", -1));

            var node = new BroadcastNode<int>("broadcast", input.Reader, new[] { required.Writer }, new[] { optional.Writer }, BroadcastMode.BestEffort);

            await node.StartAsync(cts.Token);

            await input.Writer.WriteAsync(new Envelope<int>("key", 1), cts.Token);
            await input.Writer.WriteAsync(new Envelope<int>("key", 2), cts.Token);
            input.Writer.TryComplete();

            (await required.Reader.ReadAsync(cts.Token)).Payload.ShouldBe(1);
            (await required.Reader.ReadAsync(cts.Token)).Payload.ShouldBe(2);

            // Optional output should not receive messages while backpressured.
            (await optional.Reader.ReadAsync(cts.Token)).Payload.ShouldBe(-1);
            optional.Reader.TryRead(out var _)
                    .ShouldBeFalse();

            await node.Completion.WaitAsync(cts.Token);
        }

        [Fact]
        public async Task Completion_is_propagated_to_all_outputs()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var input = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var output1 = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var output2 = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);

            var node = new BroadcastNode<int>("broadcast", input.Reader, new[] { output1.Writer, output2.Writer }, mode: BroadcastMode.AllMustReceive);

            await node.StartAsync(cts.Token);

            await input.Writer.WriteAsync(new Envelope<int>("key", 1), cts.Token);
            input.Writer.TryComplete();

            (await output1.Reader.ReadAsync(cts.Token)).Payload.ShouldBe(1);
            (await output2.Reader.ReadAsync(cts.Token)).Payload.ShouldBe(1);

            await node.Completion.WaitAsync(cts.Token);
            await output1.Reader.Completion.WaitAsync(cts.Token);
            await output2.Reader.Completion.WaitAsync(cts.Token);
        }
    }
}