using Shouldly;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class PipelineSupervisorFailureModeTests
    {
        [Fact]
        public async Task First_fatal_error_is_recorded_when_multiple_nodes_fault()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var supervisor = new PipelineSupervisor(cts.Token);

            var first = new DelayedThrowNode("first", TimeSpan.Zero, new InvalidOperationException("first"), supervisor);

            var second = new DelayedThrowNode("second", TimeSpan.FromMilliseconds(50), new InvalidOperationException("second"), supervisor);

            supervisor.AddNode(first);
            supervisor.AddNode(second);

            await supervisor.StartAsync();

            await Should.ThrowAsync<InvalidOperationException>(async () => await supervisor.Completion.WaitAsync(cts.Token));

            supervisor.CancellationToken.IsCancellationRequested.ShouldBeTrue();
            supervisor.FatalNodeName.ShouldBe("first");
            supervisor.FatalException.ShouldNotBeNull();
            supervisor.FatalException!.Message.ShouldBe("first");
        }

        [Fact]
        public async Task Normal_completion_of_other_nodes_does_not_prevent_fail_fast_on_fault()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var supervisor = new PipelineSupervisor(cts.Token);

            // Node that finishes successfully.
            var ok = new CompletedNode("ok");

            var faults = new DelayedThrowNode("faults", TimeSpan.FromMilliseconds(20), new InvalidOperationException("boom"), supervisor);

            supervisor.AddNode(ok);
            supervisor.AddNode(faults);

            await supervisor.StartAsync();

            await Should.ThrowAsync<InvalidOperationException>(async () => await supervisor.Completion.WaitAsync(cts.Token));

            supervisor.FatalNodeName.ShouldBe("faults");
        }
    }
}