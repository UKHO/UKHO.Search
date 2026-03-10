using Shouldly;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class MergeNodeTests
    {
        [Fact]
        public async Task Merges_two_inputs_without_starvation()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var input1 = BoundedChannelFactory.Create<Envelope<int>>(128, true, true);
            var input2 = BoundedChannelFactory.Create<Envelope<int>>(128, true, true);
            var output = BoundedChannelFactory.Create<Envelope<int>>(256, true, true);

            for (var i = 0; i < 50; i++)
            {
                await input1.Writer.WriteAsync(new Envelope<int>("k1", i), cts.Token);
            }

            await input2.Writer.WriteAsync(new Envelope<int>("k2", 999), cts.Token);

            input1.Writer.TryComplete();
            input2.Writer.TryComplete();

            var node = new MergeNode<int>("merge", input1.Reader, input2.Reader, output.Writer);
            await node.StartAsync(cts.Token);

            var firstTen = new int[10];
            for (var i = 0; i < firstTen.Length; i++)
            {
                firstTen[i] = (await output.Reader.ReadAsync(cts.Token)).Payload;
            }

            firstTen.Contains(999)
                    .ShouldBeTrue();

            await node.Completion.WaitAsync(cts.Token);
        }

        [Fact]
        public async Task Completes_when_one_upstream_completes_early()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var input1 = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var input2 = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var output = BoundedChannelFactory.Create<Envelope<int>>(8, true, true);

            var node = new MergeNode<int>("merge", input1.Reader, input2.Reader, output.Writer);
            await node.StartAsync(cts.Token);

            await input1.Writer.WriteAsync(new Envelope<int>("k1", 1), cts.Token);
            input1.Writer.TryComplete();

            await input2.Writer.WriteAsync(new Envelope<int>("k2", 2), cts.Token);
            input2.Writer.TryComplete();

            var items = new[]
            {
                (await output.Reader.ReadAsync(cts.Token)).Payload,
                (await output.Reader.ReadAsync(cts.Token)).Payload
            };

            items.OrderBy(x => x)
                 .ToArray()
                 .ShouldBe(new[] { 1, 2 });

            await node.Completion.WaitAsync(cts.Token);
            await output.Reader.Completion.WaitAsync(cts.Token);
        }

        [Fact]
        public async Task Fault_propagates_from_either_upstream()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var input1 = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var input2 = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var output = BoundedChannelFactory.Create<Envelope<int>>(8, true, true);

            var node = new MergeNode<int>("merge", input1.Reader, input2.Reader, output.Writer);
            await node.StartAsync(cts.Token);

            input1.Writer.TryComplete(new InvalidOperationException("boom"));
            input2.Writer.TryComplete();

            await Should.ThrowAsync<InvalidOperationException>(async () => await node.Completion.WaitAsync(cts.Token));

            await Should.ThrowAsync<InvalidOperationException>(async () => await output.Reader.Completion.WaitAsync(cts.Token));
        }
    }
}