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
        public async Task Dead_letter_jsonl_includes_runtime_payload_diagnostics_and_error_fields()
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

            json.RootElement.TryGetProperty("envelope", out var envelope)
                .ShouldBeTrue();
            envelope.TryGetProperty("key", out var _)
                    .ShouldBeTrue();
            envelope.TryGetProperty("messageId", out var _)
                    .ShouldBeTrue();
            envelope.TryGetProperty("attempt", out var _)
                    .ShouldBeTrue();
            envelope.TryGetProperty("status", out var _)
                    .ShouldBeTrue();
            envelope.TryGetProperty("error", out var error)
                    .ShouldBeTrue();
            error.TryGetProperty("code", out var _)
                 .ShouldBeTrue();

            json.RootElement.TryGetProperty("deadLetteredAtUtc", out var _)
                .ShouldBeTrue();
            json.RootElement.TryGetProperty("nodeName", out var nodeName)
                .ShouldBeTrue();
            nodeName.GetString()
                    .ShouldBe("dead-letter");

            json.RootElement.TryGetProperty("rawSnapshot", out var snapshot)
                .ShouldBeTrue();
            snapshot.GetString()
                    .ShouldBe("payload=123");

            json.RootElement.TryGetProperty("metadata", out var metadata)
                .ShouldBeTrue();
            metadata.TryGetProperty("appVersion", out var appVersion)
                    .ShouldBeTrue();
            appVersion.GetString()
                      .ShouldBe("1.2.3");
            metadata.TryGetProperty("commitId", out var commitId)
                    .ShouldBeTrue();
            commitId.GetString()
                    .ShouldBe("commit-123");
            metadata.TryGetProperty("hostName", out var hostName)
                    .ShouldBeTrue();
            hostName.GetString()
                    .ShouldBe("host-01");

            json.RootElement.TryGetProperty("payloadDiagnostics", out var payloadDiagnostics)
                .ShouldBeTrue();
            payloadDiagnostics.TryGetProperty("runtimePayloadType", out var runtimePayloadType)
                              .ShouldBeTrue();
            runtimePayloadType.GetString()
                              .ShouldBe(typeof(int).FullName);
            payloadDiagnostics.TryGetProperty("payloadSnapshot", out var payloadSnapshot)
                              .ShouldBeTrue();
            payloadSnapshot.GetInt32()
                           .ShouldBe(123);
            payloadDiagnostics.TryGetProperty("snapshotError", out var snapshotError)
                              .ShouldBeTrue();
            snapshotError.ValueKind.ShouldBe(JsonValueKind.Null);
        }
    }
}