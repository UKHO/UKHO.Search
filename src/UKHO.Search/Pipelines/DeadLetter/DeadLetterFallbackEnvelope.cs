using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;

namespace UKHO.Search.Pipelines.DeadLetter
{
    public sealed class DeadLetterFallbackEnvelope
    {
        public required DateTimeOffset TimestampUtc { get; init; }

        public string? CorrelationId { get; init; }

        public required IReadOnlyDictionary<string, string> Headers { get; init; }

        public required Guid MessageId { get; init; }

        public required string Key { get; init; }

        public required int Attempt { get; init; }

        public required MessageStatus Status { get; init; }

        public PipelineError? Error { get; init; }

        public required MessageContext Context { get; init; }
    }
}
