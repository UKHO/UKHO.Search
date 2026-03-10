using System.Threading.Channels;
using Shouldly;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class CancellationModeTests
    {
        [Fact]
        public async Task NodeBase_drain_processes_buffered_items_on_cancellation()
        {
            using var cts = new CancellationTokenSource();

            var input = BoundedChannelFactory.Create<Envelope<int>>(10, true, true);
            var output = BoundedChannelFactory.Create<Envelope<int>>(10, true, true);

            await input.Writer.WriteAsync(new Envelope<int>("k", 1));
            await input.Writer.WriteAsync(new Envelope<int>("k", 2));

            cts.Cancel();

            var node = new PassThroughNode("pass", input.Reader, output.Writer, CancellationMode.Drain);
            await node.StartAsync(cts.Token);
            await node.Completion;

            var items = await ReadAllEnvelopesAsync(output.Reader, CancellationToken.None);
            items.Select(x => x.Payload)
                 .ToArray()
                 .ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public async Task NodeBase_immediate_does_not_process_buffered_items_on_cancellation()
        {
            using var cts = new CancellationTokenSource();

            var input = BoundedChannelFactory.Create<Envelope<int>>(10, true, true);
            var output = BoundedChannelFactory.Create<Envelope<int>>(10, true, true);

            await input.Writer.WriteAsync(new Envelope<int>("k", 1));
            await input.Writer.WriteAsync(new Envelope<int>("k", 2));

            cts.Cancel();

            var node = new PassThroughNode("pass", input.Reader, output.Writer, CancellationMode.Immediate);
            await node.StartAsync(cts.Token);
            await node.Completion;

            (await ReadAllEnvelopesAsync(output.Reader, CancellationToken.None)).Count.ShouldBe(0);
        }

        [Fact]
        public async Task MicroBatch_drain_flushes_buffered_items_on_cancellation()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var input = BoundedChannelFactory.Create<Envelope<int>>(10, true, true);
            var output = BoundedChannelFactory.Create<BatchEnvelope<int>>(10, true, true);

            var node = new MicroBatchNode<int>("microbatch", 0, input.Reader, output.Writer, 100, TimeSpan.FromSeconds(30), cancellationMode: CancellationMode.Drain);

            await node.StartAsync(cts.Token);

            var e1 = new Envelope<int>("k", 1);
            var e2 = new Envelope<int>("k", 2);

            await input.Writer.WriteAsync(e1, cts.Token);
            await input.Writer.WriteAsync(e2, cts.Token);

            await WaitUntilAsync(() => e1.Context.Breadcrumbs.Contains("microbatch") && e2.Context.Breadcrumbs.Contains("microbatch"), TimeSpan.FromSeconds(2), cts.Token);

            cts.Cancel();

            await node.Completion;

            var batch = await output.Reader.ReadAsync(CancellationToken.None);
            batch.Items.Select(x => x.Payload)
                 .ToArray()
                 .ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public async Task MicroBatch_immediate_does_not_flush_buffered_items_on_cancellation()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var input = BoundedChannelFactory.Create<Envelope<int>>(10, true, true);
            var output = BoundedChannelFactory.Create<BatchEnvelope<int>>(10, true, true);

            var node = new MicroBatchNode<int>("microbatch", 0, input.Reader, output.Writer, 100, TimeSpan.FromSeconds(30), cancellationMode: CancellationMode.Immediate);

            await node.StartAsync(cts.Token);

            var e1 = new Envelope<int>("k", 1);
            var e2 = new Envelope<int>("k", 2);

            await input.Writer.WriteAsync(e1, cts.Token);
            await input.Writer.WriteAsync(e2, cts.Token);

            await WaitUntilAsync(() => e1.Context.Breadcrumbs.Contains("microbatch") && e2.Context.Breadcrumbs.Contains("microbatch"), TimeSpan.FromSeconds(2), cts.Token);

            cts.Cancel();

            await node.Completion;

            (await ReadAllBatchesAsync(output.Reader, CancellationToken.None)).Count.ShouldBe(0);
        }

        private static async Task WaitUntilAsync(Func<bool> predicate, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var deadline = DateTimeOffset.UtcNow + timeout;
            while (!predicate())
            {
                if (DateTimeOffset.UtcNow >= deadline)
                {
                    throw new TimeoutException("Condition was not met before timeout.");
                }

                await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken);
            }
        }

        private static async Task<List<Envelope<int>>> ReadAllEnvelopesAsync(ChannelReader<Envelope<int>> reader, CancellationToken cancellationToken)
        {
            var result = new List<Envelope<int>>();
            await foreach (var item in reader.ReadAllAsync(cancellationToken))
            {
                result.Add(item);
            }

            return result;
        }

        private static async Task<List<BatchEnvelope<int>>> ReadAllBatchesAsync(ChannelReader<BatchEnvelope<int>> reader, CancellationToken cancellationToken)
        {
            var result = new List<BatchEnvelope<int>>();
            await foreach (var item in reader.ReadAllAsync(cancellationToken))
            {
                result.Add(item);
            }

            return result;
        }
    }
}