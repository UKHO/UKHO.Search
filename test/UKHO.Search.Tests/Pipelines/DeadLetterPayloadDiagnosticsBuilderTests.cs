using System.Text.Json;
using Shouldly;
using UKHO.Search.Pipelines.DeadLetter;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class DeadLetterPayloadDiagnosticsBuilderTests
    {
        [Fact]
        public void Create_WhenPayloadHasRuntimeType_ShouldCaptureRuntimePayloadType()
        {
            var diagnostics = DeadLetterPayloadDiagnosticsBuilder.Create(123);

            diagnostics.RuntimePayloadType.ShouldBe(typeof(int).FullName);
            diagnostics.PayloadSnapshot.ShouldNotBeNull();
            diagnostics.PayloadSnapshot!.Value.GetInt32().ShouldBe(123);
            diagnostics.SnapshotError.ShouldBeNull();
        }

        [Fact]
        public void Create_WhenDeclaredTypeIsObject_ShouldCaptureDerivedPayloadMembers()
        {
            object payload = new { DocumentId = "doc-1", RetryCount = 2 };

            var diagnostics = DeadLetterPayloadDiagnosticsBuilder.Create(payload);

            diagnostics.RuntimePayloadType.ShouldNotBeNull();
            diagnostics.PayloadSnapshot.ShouldNotBeNull();
            diagnostics.PayloadSnapshot!.Value.GetProperty("documentId").GetString().ShouldBe("doc-1");
            diagnostics.PayloadSnapshot!.Value.GetProperty("retryCount").GetInt32().ShouldBe(2);
            diagnostics.SnapshotError.ShouldBeNull();
        }

        [Fact]
        public void Create_WhenPayloadSnapshotSerializationFails_ShouldReturnFallbackError()
        {
            object payload = new { Unsupported = (Action)(() => { }) };

            var diagnostics = DeadLetterPayloadDiagnosticsBuilder.Create(payload);

            diagnostics.RuntimePayloadType.ShouldNotBeNull();
            diagnostics.PayloadSnapshot.ShouldBeNull();
            diagnostics.SnapshotError.ShouldNotBeNull();
            diagnostics.SnapshotError!.ExceptionType.ShouldBe(typeof(NotSupportedException).FullName);
            diagnostics.SnapshotError.ExceptionMessage.ShouldContain("System.Action");
        }
    }
}
