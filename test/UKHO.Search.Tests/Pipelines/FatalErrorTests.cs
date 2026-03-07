using Shouldly;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class FatalErrorTests
    {
        [Fact]
        public async Task Unexpected_exception_faults_downstream_and_triggers_fail_fast_cancellation()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            static ValueTask<int> ThrowingTransform(int payload, CancellationToken cancellationToken)
            {
                if (payload == 3)
                {
                    throw new InvalidOperationException("boom");
                }

                return ValueTask.FromResult(payload);
            }

            var graph = HelloPipelineGraph.Create(10, 1, 1, 4, null, ThrowingTransform, cts.Token, faultPipelineOnTransformException: true);

            await graph.Supervisor.StartAsync();

            await Should.ThrowAsync<InvalidOperationException>(async () => await graph.Supervisor.Completion.WaitAsync(cts.Token));

            graph.Supervisor.CancellationToken.IsCancellationRequested.ShouldBeTrue();
            graph.Supervisor.FatalException.ShouldNotBeNull();
            graph.Supervisor.FatalNodeName.ShouldBe("transform-0");

            await Should.ThrowAsync<InvalidOperationException>(async () => await graph.Sinks[0]
                                                                                      .Completion.WaitAsync(cts.Token));
        }
    }
}