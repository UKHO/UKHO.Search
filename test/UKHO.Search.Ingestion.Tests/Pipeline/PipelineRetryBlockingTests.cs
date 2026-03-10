using Shouldly;
using UKHO.Search.Ingestion.Tests.TestNodes;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Retry;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Pipeline
{
    public sealed class PipelineRetryBlockingTests
    {
        [Fact]
        public async Task Retrying_transform_blocks_lane_until_retry_succeeds()
        {
            var channelIn = BoundedChannelFactory.Create<Envelope<int>>(10, true, true);
            var channelOut = BoundedChannelFactory.Create<Envelope<int>>(10, true, true);

            var supervisor = new PipelineSupervisor(CancellationToken.None);

            var source = new SyntheticSourceNode<int>("source", channelIn.Writer, 3, 1, i => i + 1, _ => "doc", fatalErrorReporter: supervisor);

            var attempt2FirstAttemptEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseFirstAttempt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var failedOnce = false;

            async ValueTask<int> Transform(int value, CancellationToken ct)
            {
                if (value == 2 && !failedOnce)
                {
                    attempt2FirstAttemptEntered.TrySetResult();
                    await releaseFirstAttempt.Task.WaitAsync(ct);
                    failedOnce = true;
                    throw new TransientTestException();
                }

                return value;
            }

            var retryPolicy = new ExponentialBackoffRetryPolicy(3, TimeSpan.Zero, TimeSpan.Zero, 0);

            var retry = new RetryingTransformNode<int, int>("retry", channelIn.Reader, channelOut.Writer, Transform, retryPolicy, ex => ex is TransientTestException, errorCode: "TRANSIENT");

            var sink = new BlockingEnvelopeSinkNode<int>("sink", channelOut.Reader, 0);

            supervisor.AddNode(source);
            supervisor.AddNode(retry);
            supervisor.AddNode(sink);

            await supervisor.StartAsync();

            await attempt2FirstAttemptEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await sink.WaitForCountAsync(1, TimeSpan.FromSeconds(2));

            sink.Items.Select(x => x.Payload)
                .ToArray()
                .ShouldBe(new[] { 1 });

            releaseFirstAttempt.TrySetResult();

            await supervisor.Completion.WaitAsync(TimeSpan.FromSeconds(5));

            sink.Items.Select(x => x.Payload)
                .ToArray()
                .ShouldBe(new[] { 1, 2, 3 });
            sink.Items.Single(x => x.Payload == 2)
                .Attempt.ShouldBe(2);
            sink.Items.Single(x => x.Payload == 3)
                .Attempt.ShouldBe(1);
        }
    }
}