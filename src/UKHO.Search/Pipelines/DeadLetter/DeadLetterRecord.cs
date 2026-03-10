using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;

namespace UKHO.Search.Pipelines.DeadLetter
{
    public sealed class DeadLetterRecord<TPayload>
    {
        public required DateTimeOffset DeadLetteredAtUtc { get; init; }

        public required string NodeName { get; init; }

        public required Envelope<TPayload> Envelope { get; init; }

        public PipelineError? Error { get; init; }

        public string? RawSnapshot { get; init; }

        public DeadLetterMetadata? Metadata { get; init; }
    }
}