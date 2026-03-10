using Shouldly;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class NodeNamingCollisionTests
    {
        [Fact]
        public void Supervisor_rejects_duplicate_node_names()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var supervisor = new PipelineSupervisor(cts.Token);

            supervisor.AddNode(new CompletedNode("dup"));

            Should.Throw<InvalidOperationException>(() => supervisor.AddNode(new CompletedNode("dup")));
        }
    }
}