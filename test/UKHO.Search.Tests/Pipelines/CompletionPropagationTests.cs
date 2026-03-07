using Shouldly;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class CompletionPropagationTests
    {
        [Fact]
        public async Task Completing_input_drains_pipeline_and_completes_downstream_cleanly()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var graph = HelloPipelineGraph.Create(50, 5, 2, 4, null, (p, _) => ValueTask.FromResult(p), cts.Token);

            await graph.Supervisor.StartAsync();
            await graph.Supervisor.Completion.WaitAsync(cts.Token);

            graph.Source.Completion.IsCompletedSuccessfully.ShouldBeTrue();
            graph.Validate.Completion.IsCompletedSuccessfully.ShouldBeTrue();
            graph.Partition.Completion.IsCompletedSuccessfully.ShouldBeTrue();
            graph.Transforms.All(t => t.Completion.IsCompletedSuccessfully)
                 .ShouldBeTrue();
            graph.Sinks.All(s => s.Completion.IsCompletedSuccessfully)
                 .ShouldBeTrue();

            graph.Sinks.SelectMany(s => s.Items)
                 .Count()
                 .ShouldBe(50);
        }
    }
}