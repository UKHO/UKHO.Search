using Shouldly;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class CancellationBackpressureTests
    {
        [Fact]
        public async Task Cancelling_supervisor_unblocks_node_waiting_on_bounded_output_write()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var supervisor = new PipelineSupervisor(cts.Token);
            var input = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var output = BoundedChannelFactory.Create<Envelope<int>>(1, true, true);

            // Fill output to force the next write to block.
            await output.Writer.WriteAsync(new Envelope<int>("key-0", 0), cts.Token);

            var drop = new DropNode<int>("drop", input.Reader, output.Writer, fatalErrorReporter: supervisor);

            supervisor.AddNode(drop);
            await supervisor.StartAsync();

            await input.Writer.WriteAsync(new Envelope<int>("key-0", 1), cts.Token);

            await Task.Delay(TimeSpan.FromMilliseconds(100), cts.Token);
            supervisor.Cancel();

            input.Writer.TryComplete();

            await supervisor.Completion.WaitAsync(cts.Token);
            supervisor.CancellationToken.IsCancellationRequested.ShouldBeTrue();
        }
    }
}