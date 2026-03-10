using System.Text.Json;
using Shouldly;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class DeadLetterTests
    {
        [Fact]
        public async Task Poison_message_is_routed_to_dead_letter_and_pipeline_continues()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var messageCount = 20;
            var keyCardinality = 2;
            var poisonIndex = 5;
            var filePath = Path.Combine(Path.GetTempPath(), "ukho-search", Guid.NewGuid()
                                                                               .ToString("N"), "deadletter.jsonl");

            var graph = HelloPipelineGraph.Create(messageCount, keyCardinality, 2, 4, null, (p, _) => ValueTask.FromResult(p), cts.Token, i => i == poisonIndex ? string.Empty : $"key-{i % keyCardinality}", filePath);

            await graph.Supervisor.StartAsync();
            await graph.Supervisor.Completion.WaitAsync(cts.Token);

            graph.DeadLetterSink.ShouldNotBeNull();
            graph.DeadLetterSink.PersistedCount.ShouldBe(1);

            var sinkPayloads = graph.Sinks.SelectMany(s => s.Items)
                                    .Select(e => e.Payload)
                                    .ToArray();
            sinkPayloads.Length.ShouldBe(messageCount - 1);
            sinkPayloads.Max()
                        .ShouldBe(messageCount - 1);

            File.Exists(filePath)
                .ShouldBeTrue();
            var lines = await File.ReadAllLinesAsync(filePath, cts.Token);
            lines.Length.ShouldBe(1);

            using var json = JsonDocument.Parse(lines[0]);
            var root = json.RootElement;
            root.GetProperty("Envelope")
                .GetProperty("Payload")
                .GetInt32()
                .ShouldBe(poisonIndex);
            root.GetProperty("Error")
                .GetProperty("Code")
                .GetString()
                .ShouldBe("KEY_EMPTY");
        }
    }
}