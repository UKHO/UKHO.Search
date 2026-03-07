using System.Text.Json;
using Shouldly;
using UKHO.Search.Pipelines.Retry;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class RetryBlockingTests
    {
        [Fact]
        public async Task Retry_blocks_lane_in_order_and_dead_letters_after_exhaustion()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            var baseDelay = TimeSpan.FromMilliseconds(250);
            var retryPolicy = new ExponentialBackoffRetryPolicy(3, baseDelay, baseDelay, 0);

            static ValueTask<int> Transform(int payload, CancellationToken cancellationToken)
            {
                if (payload == 2)
                {
                    throw new TimeoutException("transient");
                }

                return ValueTask.FromResult(payload);
            }

            var deadLetterFilePath = Path.Combine(Path.GetTempPath(), "ukho-search", Guid.NewGuid()
                                                                                         .ToString("N"), "retry-deadletter.jsonl");

            var graph = RetryPipelineGraph.Create(5, 10, retryPolicy, null, Transform, ex => ex is TimeoutException, deadLetterFilePath, cts.Token);

            await graph.Supervisor.StartAsync();
            await graph.Supervisor.Completion.WaitAsync(cts.Token);

            var payloads = graph.Sink.Items.Select(e => e.Payload)
                                .ToArray();
            payloads.ShouldBe(new[] { 0, 1, 3, 4 });

            graph.DeadLetterSink.PersistedCount.ShouldBe(1);

            var lines = await File.ReadAllLinesAsync(deadLetterFilePath, cts.Token);
            lines.Length.ShouldBe(1);

            using var json = JsonDocument.Parse(lines[0]);
            var envelope = json.RootElement.GetProperty("Envelope");
            envelope.GetProperty("Payload")
                    .GetInt32()
                    .ShouldBe(2);
            envelope.GetProperty("Attempt")
                    .GetInt32()
                    .ShouldBe(3);

            var receivedKey = $"received:{graph.Sink.Name}";
            var env1 = graph.Sink.Items.Single(e => e.Payload == 1);
            var env3 = graph.Sink.Items.Single(e => e.Payload == 3);
            var t1 = env1.Context.TimingsUtc[receivedKey];
            var t3 = env3.Context.TimingsUtc[receivedKey];

            (t3 - t1).ShouldBeGreaterThanOrEqualTo(baseDelay + baseDelay);
        }
    }
}