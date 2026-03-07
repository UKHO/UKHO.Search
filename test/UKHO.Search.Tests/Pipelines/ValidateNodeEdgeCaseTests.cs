using Shouldly;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class ValidateNodeEdgeCaseTests
    {
        [Fact]
        public async Task Null_key_is_treated_as_invalid()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var supervisor = new PipelineSupervisor(cts.Token);
            var input = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var output = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);

            var validate = new ValidateNode<int>("validate", input.Reader, output.Writer, fatalErrorReporter: supervisor);

            var sink = new CollectingSinkNode<int>("sink", output.Reader, fatalErrorReporter: supervisor);

            supervisor.AddNode(validate);
            supervisor.AddNode(sink);

            await supervisor.StartAsync();

            await input.Writer.WriteAsync(new Envelope<int>(null!, 1), cts.Token);
            input.Writer.TryComplete();

            await supervisor.Completion.WaitAsync(cts.Token);

            sink.Items.Count.ShouldBe(1);
            sink.Items[0]
                .Status.ShouldBe(MessageStatus.Failed);
            sink.Items[0]
                .Error.ShouldNotBeNull();
            sink.Items[0].Error!.Code.ShouldBe("KEY_EMPTY");
        }
    }
}