using Shouldly;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Retry;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class RetryCancellationTests
    {
        [Fact]
        public async Task Cancellation_during_backoff_does_not_deadlock_and_completes_outputs()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var supervisor = new PipelineSupervisor(cts.Token);
            var input = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var output = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);

            var retryPolicy = new ExponentialBackoffRetryPolicy(10, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), 0);

            static ValueTask<int> Transform(int payload, CancellationToken cancellationToken)
            {
                throw new TimeoutException("transient");
            }

            var node = new RetryingTransformNode<int, int>("retrying-transform", input.Reader, output.Writer, Transform, retryPolicy, ex => ex is TimeoutException, forwardFailedToMainOutput: false, fatalErrorReporter: supervisor);

            var sink = new CollectingSinkNode<int>("sink", output.Reader, fatalErrorReporter: supervisor);

            supervisor.AddNode(node);
            supervisor.AddNode(sink);

            await supervisor.StartAsync();

            await input.Writer.WriteAsync(new Envelope<int>("key-0", 1), cts.Token);

            // Give the node a moment to enter the delay.
            await Task.Delay(TimeSpan.FromMilliseconds(100), cts.Token);
            supervisor.Cancel();

            input.Writer.TryComplete();
            await supervisor.Completion.WaitAsync(cts.Token);

            sink.Items.Count.ShouldBe(0);
        }
    }
}