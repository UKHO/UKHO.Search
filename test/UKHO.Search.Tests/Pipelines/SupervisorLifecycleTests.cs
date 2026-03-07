using Shouldly;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class SupervisorLifecycleTests
    {
        [Fact]
        public async Task StartAsync_is_idempotent()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var supervisor = new PipelineSupervisor(cts.Token);
            supervisor.AddNode(new CompletedNode("ok"));

            await supervisor.StartAsync();
            var t1 = supervisor.Completion;

            await supervisor.StartAsync();
            var t2 = supervisor.Completion;

            t2.ShouldBeSameAs(t1);
            await supervisor.Completion.WaitAsync(cts.Token);
        }

        [Fact]
        public async Task Adding_nodes_after_start_is_rejected()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var supervisor = new PipelineSupervisor(cts.Token);
            supervisor.AddNode(new CompletedNode("ok"));

            await supervisor.StartAsync();

            Should.Throw<InvalidOperationException>(() => supervisor.AddNode(new CompletedNode("late")));
        }
    }
}