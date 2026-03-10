using Shouldly;
using UKHO.Search.Ingestion.Tests.TestNodes;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Pipeline
{
    public sealed class PipelineBackpressureTests
    {
        [Fact]
        public async Task Bounded_channel_applies_backpressure_when_downstream_is_slow()
        {
            var channel = BoundedChannelFactory.Create<Envelope<int>>(1, true, true);

            var sink = new BlockingEnvelopeSinkNode<int>("sink", channel.Reader, 1);
            await sink.StartAsync(CancellationToken.None);

            var thirdWriteStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var producerTask = Task.Run(async () =>
            {
                await channel.Writer.WriteAsync(new Envelope<int>("doc", 1));
                await channel.Writer.WriteAsync(new Envelope<int>("doc", 2));

                thirdWriteStarted.TrySetResult();
                await channel.Writer.WriteAsync(new Envelope<int>("doc", 3));

                channel.Writer.TryComplete();
            });

            await sink.WaitForCountAsync(1, TimeSpan.FromSeconds(2));
            await thirdWriteStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

            producerTask.IsCompleted.ShouldBeFalse();

            sink.ReleaseBlocking();

            await producerTask.WaitAsync(TimeSpan.FromSeconds(2));
            await sink.Completion.WaitAsync(TimeSpan.FromSeconds(2));

            sink.Items.Select(x => x.Payload)
                .ToArray()
                .ShouldBe(new[] { 1, 2, 3 });
        }
    }
}