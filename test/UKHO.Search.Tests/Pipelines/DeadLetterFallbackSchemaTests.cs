using System.Text.Json;
using Shouldly;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class DeadLetterFallbackSchemaTests
    {
        [Fact]
        public async Task Dead_letter_jsonl_persists_fallback_record_when_payload_cannot_be_serialized()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var filePath = Path.Combine(Path.GetTempPath(), "ukho-search", Guid.NewGuid()
                                                                               .ToString("N"), "deadletter.jsonl");

            var supervisor = new PipelineSupervisor(cts.Token);
            var input = BoundedChannelFactory.Create<Envelope<UnsupportedPayload>>(4, true, true);

            var node = new DeadLetterSinkNode<UnsupportedPayload>("dead-letter", input.Reader, filePath, true, metadataProvider: new TestDeadLetterMetadataProvider("1.2.3", "commit-123", "host-01"), fatalErrorReporter: supervisor);

            supervisor.AddNode(node);
            await supervisor.StartAsync();

            var env = new Envelope<UnsupportedPayload>("key-0", new UnsupportedPayload());
            env.MarkFailed(new PipelineError
            {
                Category = PipelineErrorCategory.Validation,
                Code = "TEST",
                Message = "failed",
                ExceptionType = null,
                ExceptionMessage = null,
                StackTrace = null,
                IsTransient = false,
                OccurredAtUtc = DateTimeOffset.UtcNow,
                NodeName = "test",
                Details = new Dictionary<string, string>()
            });

            await input.Writer.WriteAsync(env, cts.Token);
            input.Writer.TryComplete();

            await supervisor.Completion.WaitAsync(cts.Token);

            var line = (await File.ReadAllLinesAsync(filePath, cts.Token)).Single();
            using var json = JsonDocument.Parse(line);

            json.RootElement.TryGetProperty("serializationError", out var serializationError)
                .ShouldBeTrue();
            serializationError.GetString().ShouldContain("System.Action");

            json.RootElement.TryGetProperty("payloadDiagnostics", out var payloadDiagnostics)
                .ShouldBeTrue();
            payloadDiagnostics.TryGetProperty("runtimePayloadType", out var runtimePayloadType)
                              .ShouldBeTrue();
            runtimePayloadType.GetString().ShouldBe(typeof(UnsupportedPayload).FullName);
            payloadDiagnostics.TryGetProperty("payloadSnapshot", out var payloadSnapshot)
                              .ShouldBeTrue();
            payloadSnapshot.ValueKind.ShouldBe(JsonValueKind.Null);
            payloadDiagnostics.TryGetProperty("snapshotError", out var snapshotError)
                              .ShouldBeTrue();
            snapshotError.TryGetProperty("exceptionType", out var exceptionType)
                         .ShouldBeTrue();
            exceptionType.GetString().ShouldBe(typeof(NotSupportedException).FullName);

            json.RootElement.TryGetProperty("envelope", out var envelope)
                .ShouldBeTrue();
            envelope.TryGetProperty("payload", out var hasPayload)
                    .ShouldBeFalse();
        }

        private sealed class UnsupportedPayload
        {
            public Action Callback { get; } = static () => { };
        }
    }
}
