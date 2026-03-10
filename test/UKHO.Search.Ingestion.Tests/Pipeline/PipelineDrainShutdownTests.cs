using Shouldly;
using UKHO.Search.Ingestion.Tests.TestNodes;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Pipeline
{
    public sealed class PipelineDrainShutdownTests
    {
        [Fact]
        public async Task Drain_cancellation_flushes_microbatch_even_without_input_completion()
        {
            using var cts = new CancellationTokenSource();

            var laneDispatch = BoundedChannelFactory.Create<Envelope<int>>(10, true, true);
            var laneBatches = BoundedChannelFactory.Create<BatchEnvelope<int>>(10, true, true);

            var microbatch = new MicroBatchNode<int>("microbatch", 0, laneDispatch.Reader, laneBatches.Writer, 100, TimeSpan.FromHours(1), cancellationMode: CancellationMode.Drain);

            var sink = new CollectingBatchEnvelopeSinkNode<int>("sink", laneBatches.Reader);

            await sink.StartAsync(CancellationToken.None);
            await microbatch.StartAsync(cts.Token);

            await laneDispatch.Writer.WriteAsync(new Envelope<int>("doc", 1));
            await laneDispatch.Writer.WriteAsync(new Envelope<int>("doc", 2));

            await WaitUntilAsync(() => ((IQueueDepthProvider)laneDispatch.Reader).QueueDepth == 0, TimeSpan.FromSeconds(2));

            cts.Cancel();

            await microbatch.Completion.WaitAsync(TimeSpan.FromSeconds(5));
            await sink.Completion.WaitAsync(TimeSpan.FromSeconds(5));

            await sink.WaitForCountAsync(2, TimeSpan.FromSeconds(2));
            sink.Items.Select(x => x.Payload)
                .ToArray()
                .ShouldBe(new[] { 1, 2 });
        }

        private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
        {
            var deadline = DateTimeOffset.UtcNow + timeout;
            while (DateTimeOffset.UtcNow < deadline)
            {
                if (condition())
                {
                    return;
                }

                await Task.Delay(10);
            }

            throw new TimeoutException("Condition not met in time.");
        }
    }
}