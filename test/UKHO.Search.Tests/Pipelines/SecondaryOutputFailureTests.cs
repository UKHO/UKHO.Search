using System.Threading.Channels;
using Shouldly;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Retry;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class SecondaryOutputFailureTests
    {
        [Fact]
        public async Task ValidateNode_faults_pipeline_if_error_output_is_closed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var supervisor = new PipelineSupervisor(cts.Token);
            var input = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var output = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var errors = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            errors.Writer.TryComplete();

            var validate = new ValidateNode<int>("validate", input.Reader, output.Writer, errors.Writer, false, fatalErrorReporter: supervisor);

            supervisor.AddNode(validate);
            await supervisor.StartAsync();

            await input.Writer.WriteAsync(new Envelope<int>(string.Empty, 1), cts.Token);
            input.Writer.TryComplete();

            await Should.ThrowAsync<ChannelClosedException>(async () => await supervisor.Completion.WaitAsync(cts.Token));

            supervisor.FatalNodeName.ShouldBe("validate");
        }

        [Fact]
        public async Task RetryingTransformNode_faults_pipeline_if_error_output_is_closed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var supervisor = new PipelineSupervisor(cts.Token);
            var input = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var output = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var errors = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            errors.Writer.TryComplete();

            var retryPolicy = new ExponentialBackoffRetryPolicy(2, TimeSpan.Zero, TimeSpan.Zero, 0);

            static ValueTask<int> Transform(int payload, CancellationToken cancellationToken)
            {
                throw new TimeoutException("transient");
            }

            var node = new RetryingTransformNode<int, int>("retrying-transform", input.Reader, output.Writer, Transform, retryPolicy, ex => ex is TimeoutException, errors.Writer, false, fatalErrorReporter: supervisor);

            supervisor.AddNode(node);
            await supervisor.StartAsync();

            await input.Writer.WriteAsync(new Envelope<int>("key-0", 1), cts.Token);
            input.Writer.TryComplete();

            await Should.ThrowAsync<ChannelClosedException>(async () => await supervisor.Completion.WaitAsync(cts.Token));

            supervisor.FatalNodeName.ShouldBe("retrying-transform");
        }
    }
}