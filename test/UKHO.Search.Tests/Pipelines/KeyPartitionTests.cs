using Shouldly;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class KeyPartitionTests
    {
        [Fact]
        public async Task Same_key_always_routes_to_the_same_lane()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var supervisor = new PipelineSupervisor(cts.Token);

            var input = BoundedChannelFactory.Create<Envelope<int>>(32, true, true);
            var lanes = new[]
            {
                BoundedChannelFactory.Create<Envelope<int>>(32, true, true),
                BoundedChannelFactory.Create<Envelope<int>>(32, true, true),
                BoundedChannelFactory.Create<Envelope<int>>(32, true, true)
            };

            var partition = new KeyPartitionNode<int>("partition", input.Reader, lanes.Select(l => l.Writer)
                                                                                      .ToArray(), fatalErrorReporter: supervisor);

            var sinks = lanes.Select((lane, idx) => new CollectingSinkNode<int>($"sink-{idx}", lane.Reader, fatalErrorReporter: supervisor))
                             .ToArray();

            supervisor.AddNode(partition);
            foreach (var sink in sinks)
            {
                supervisor.AddNode(sink);
            }

            await supervisor.StartAsync();

            var keys = new[] { "a", "b", "c", "a", "b", "a", "c", "c", "b" };
            for (var i = 0; i < keys.Length; i++)
            {
                await input.Writer.WriteAsync(new Envelope<int>(keys[i], i), cts.Token);
            }

            input.Writer.TryComplete();
            await supervisor.Completion.WaitAsync(cts.Token);

            var keyToLanes = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
            for (var laneIndex = 0; laneIndex < sinks.Length; laneIndex++)
            {
                foreach (var env in sinks[laneIndex].Items)
                {
                    if (!keyToLanes.TryGetValue(env.Key, out var set))
                    {
                        set = new HashSet<int>();
                        keyToLanes[env.Key] = set;
                    }

                    set.Add(laneIndex);
                }
            }

            keyToLanes["a"]
                .Count.ShouldBe(1);
            keyToLanes["b"]
                .Count.ShouldBe(1);
            keyToLanes["c"]
                .Count.ShouldBe(1);

            sinks.SelectMany(s => s.Items)
                 .Count()
                 .ShouldBe(keys.Length);
        }
    }
}