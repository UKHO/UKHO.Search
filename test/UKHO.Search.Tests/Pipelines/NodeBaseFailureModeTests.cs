using System.Threading.Channels;
using Shouldly;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class NodeBaseFailureModeTests
    {
        [Fact]
        public async Task Writing_to_a_closed_output_channel_faults_the_node_and_triggers_supervisor_cancellation()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var supervisor = new PipelineSupervisor(cts.Token);
            var input = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var output = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            output.Writer.TryComplete();

            var node = new DropNode<int>("drop", input.Reader, output.Writer, fatalErrorReporter: supervisor);

            supervisor.AddNode(node);
            await supervisor.StartAsync();

            await input.Writer.WriteAsync(new Envelope<int>("key-0", 1), cts.Token);
            input.Writer.TryComplete();

            await Should.ThrowAsync<ChannelClosedException>(async () => await supervisor.Completion.WaitAsync(cts.Token));

            supervisor.CancellationToken.IsCancellationRequested.ShouldBeTrue();
            supervisor.FatalNodeName.ShouldBe("drop");
        }
    }
}