using Shouldly;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class OrderingTests
    {
        [Fact]
        public async Task Messages_with_the_same_key_exit_in_the_same_order_they_entered()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var messageCount = 200;
            var keyCardinality = 10;
            var partitions = 4;
            var capacity = 8;

            var graph = HelloPipelineGraph.Create(messageCount, keyCardinality, partitions, capacity, null, (p, _) => ValueTask.FromResult(p), cts.Token);

            await graph.Supervisor.StartAsync();
            await graph.Supervisor.Completion.WaitAsync(cts.Token);

            var all = graph.Sinks.SelectMany(s => s.Items)
                           .ToList();
            all.Count.ShouldBe(messageCount);

            foreach (var group in all.GroupBy(e => e.Key))
            {
                var sequence = group.Select(e => e.Payload)
                                    .ToArray();
                sequence.ShouldBe(sequence.OrderBy(x => x)
                                          .ToArray());
            }
        }
    }
}