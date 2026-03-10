using Shouldly;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class PipelineSupervisorStopAsyncTests
    {
        [Fact]
        public async Task StopAsync_cancels_and_forwards_to_all_nodes()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var supervisor = new PipelineSupervisor(cts.Token);

            var node1 = new StopAsyncProbeNode("n1");
            var node2 = new StopAsyncProbeNode("n2");

            supervisor.AddNode(node1);
            supervisor.AddNode(node2);

            await supervisor.StopAsync(cts.Token);

            supervisor.CancellationToken.IsCancellationRequested.ShouldBeTrue();
            node1.StopCalls.ShouldBe(1);
            node2.StopCalls.ShouldBe(1);
        }
    }
}