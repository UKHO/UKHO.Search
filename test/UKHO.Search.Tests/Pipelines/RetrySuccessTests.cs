using Shouldly;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Retry;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class RetrySuccessTests
    {
        [Fact]
        public async Task Retries_eventually_succeed_and_attempt_is_preserved()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var supervisor = new PipelineSupervisor(cts.Token);

            var input = BoundedChannelFactory.Create<Envelope<int>>(32, true, true);
            var output = BoundedChannelFactory.Create<Envelope<int>>(32, true, true);

            var retryPolicy = new ExponentialBackoffRetryPolicy(5, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), 0);

            var failures = 0;

            ValueTask<int> Transform(int payload, CancellationToken cancellationToken)
            {
                if (payload == 2)
                {
                    failures++;
                    if (failures < 3)
                    {
                        throw new TimeoutException("transient");
                    }
                }

                return ValueTask.FromResult(payload);
            }

            var retryNode = new RetryingTransformNode<int, int>("retrying-transform", input.Reader, output.Writer, Transform, retryPolicy, ex => ex is TimeoutException, forwardFailedToMainOutput: true, fatalErrorReporter: supervisor);

            var sink = new CollectingSinkNode<int>("sink", output.Reader, fatalErrorReporter: supervisor);

            supervisor.AddNode(retryNode);
            supervisor.AddNode(sink);

            await supervisor.StartAsync();

            for (var i = 0; i < 4; i++)
            {
                await input.Writer.WriteAsync(new Envelope<int>("key-0", i), cts.Token);
            }

            input.Writer.TryComplete();
            await supervisor.Completion.WaitAsync(cts.Token);

            var payloads = sink.Items.Select(i => i.Payload)
                               .ToArray();
            payloads.ShouldBe(new[] { 0, 1, 2, 3 });

            var env2 = sink.Items.Single(i => i.Payload == 2);
            env2.Attempt.ShouldBe(3);
            env2.Status.ShouldBe(MessageStatus.Ok);
            env2.Error.ShouldBeNull();
        }
    }
}