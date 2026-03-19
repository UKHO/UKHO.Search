using System.Text.Json;

namespace UKHO.Search.Pipelines.DeadLetter
{
    public sealed class DeadLetterPayloadDiagnostics
    {
        public required string RuntimePayloadType { get; init; }

        public JsonElement? PayloadSnapshot { get; init; }

        public DeadLetterPayloadSnapshotError? SnapshotError { get; init; }
    }
}
