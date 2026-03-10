using System.Text.Json;
using Shouldly;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class DeadLetterSchemaTests
    {
        [Fact]
        public async Task Dead_letter_jsonl_includes_envelope_and_error_fields()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var filePath = Path.Combine(Path.GetTempPath(), "ukho-search", Guid.NewGuid()
                                                                               .ToString("N"), "deadletter.jsonl");

            var supervisor = new PipelineSupervisor(cts.Token);
            var input = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);

            var node = new DeadLetterSinkNode<int>("dead-letter", input.Reader, filePath, true, e => $"payload={e.Payload}", new TestDeadLetterMetadataProvider("1.2.3", "commit-123", "host-01"), fatalErrorReporter: supervisor);

            supervisor.AddNode(node);
            await supervisor.StartAsync();

            var env = new Envelope<int>("key-0", 123);
            env.MarkDropped("dropped", "test");
            await input.Writer.WriteAsync(env, cts.Token);
            input.Writer.TryComplete();

            await supervisor.Completion.WaitAsync(cts.Token);

            var line = (await File.ReadAllLinesAsync(filePath, cts.Token)).Single();
            using var json = JsonDocument.Parse(line);

            json.RootElement.TryGetProperty("Envelope", out var envelope)
                .ShouldBeTrue();
            envelope.TryGetProperty("Key", out var _)
                    .ShouldBeTrue();
            envelope.TryGetProperty("MessageId", out var _)
                    .ShouldBeTrue();
            envelope.TryGetProperty("Attempt", out var _)
                    .ShouldBeTrue();
            envelope.TryGetProperty("Status", out var _)
                    .ShouldBeTrue();
            envelope.TryGetProperty("Error", out var error)
                    .ShouldBeTrue();
            error.TryGetProperty("Code", out var _)
                 .ShouldBeTrue();

            json.RootElement.TryGetProperty("DeadLetteredAtUtc", out var _)
                .ShouldBeTrue();
            json.RootElement.TryGetProperty("NodeName", out var nodeName)
                .ShouldBeTrue();
            nodeName.GetString()
                    .ShouldBe("dead-letter");

            json.RootElement.TryGetProperty("RawSnapshot", out var snapshot)
                .ShouldBeTrue();
            snapshot.GetString()
                    .ShouldBe("payload=123");

            json.RootElement.TryGetProperty("Metadata", out var metadata)
                .ShouldBeTrue();
            metadata.TryGetProperty("AppVersion", out var appVersion)
                    .ShouldBeTrue();
            appVersion.GetString()
                      .ShouldBe("1.2.3");
            metadata.TryGetProperty("CommitId", out var commitId)
                    .ShouldBeTrue();
            commitId.GetString()
                    .ShouldBe("commit-123");
            metadata.TryGetProperty("HostName", out var hostName)
                    .ShouldBeTrue();
            hostName.GetString()
                    .ShouldBe("host-01");
        }
    }
}