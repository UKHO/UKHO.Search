using System.Diagnostics;
using Shouldly;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class BackpressureTests
    {
        [Fact]
        public async Task Bounded_channels_apply_backpressure_upstream_when_sink_is_slow()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var messageCount = 20;
            var sinkDelay = TimeSpan.FromMilliseconds(100);

            var graph = HelloPipelineGraph.Create(messageCount, 2, 1, 1, sinkDelay, (p, _) => ValueTask.FromResult(p), cts.Token);

            var stopwatch = Stopwatch.StartNew();

            await graph.Supervisor.StartAsync();
            await graph.Source.Completion.WaitAsync(cts.Token);

            var sourceCompletedAt = stopwatch.Elapsed;

            await graph.Supervisor.Completion.WaitAsync(cts.Token);

            var expectedMinimum = TimeSpan.FromMilliseconds(messageCount * sinkDelay.TotalMilliseconds * 0.5);
            sourceCompletedAt.ShouldBeGreaterThan(expectedMinimum);
        }
    }
}