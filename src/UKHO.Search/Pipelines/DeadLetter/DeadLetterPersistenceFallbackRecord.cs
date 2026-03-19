using UKHO.Search.Pipelines.Errors;

namespace UKHO.Search.Pipelines.DeadLetter
{
    public sealed class DeadLetterPersistenceFallbackRecord
    {
        public required DateTimeOffset DeadLetteredAtUtc { get; init; }

        public required string NodeName { get; init; }

        public required DeadLetterFallbackEnvelope Envelope { get; init; }

        public PipelineError? Error { get; init; }

        public DeadLetterPayloadDiagnostics? PayloadDiagnostics { get; init; }

        public string? RawSnapshot { get; init; }

        public DeadLetterMetadata? Metadata { get; init; }

        public required string SerializationError { get; init; }
    }
}
