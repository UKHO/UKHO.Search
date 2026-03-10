using Shouldly;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Pipeline
{
    public sealed class PipelineFatalCancellationTests
    {
        [Fact]
        public async Task Fatal_node_exception_cancels_pipeline()
        {
            var channelIn = BoundedChannelFactory.Create<Envelope<int>>(10, true, true);
            var channelOut = BoundedChannelFactory.Create<Envelope<int>>(10, true, true);

            var supervisor = new PipelineSupervisor(CancellationToken.None);

            var source = new SyntheticSourceNode<int>("source", channelIn.Writer, 3, 1, i => i + 1, _ => "doc", fatalErrorReporter: supervisor);

            var boom = new TransformNode<int, int>("boom", channelIn.Reader, channelOut.Writer, (value, _) => value == 2 ? throw new InvalidOperationException("boom") : new ValueTask<int>(value), true, fatalErrorReporter: supervisor);

            var sink = new CollectingSinkNode<int>("sink", channelOut.Reader, fatalErrorReporter: supervisor);

            supervisor.AddNode(source);
            supervisor.AddNode(boom);
            supervisor.AddNode(sink);

            await supervisor.StartAsync();

            try
            {
                await supervisor.Completion.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // Expected: supervisor faults when a node faults.
            }

            supervisor.FatalNodeName.ShouldBe("boom");
            supervisor.FatalException.ShouldNotBeNull();
            supervisor.CancellationToken.IsCancellationRequested.ShouldBeTrue();
        }
    }
}