namespace UKHO.Search.Infrastructure.Ingestion.Queue
{
    public sealed record QueueReceivedMessage
    {
        public required string MessageId { get; init; }

        public required string PopReceipt { get; init; }

        public required int DequeueCount { get; init; }

        public required string MessageText { get; init; }

        public DateTimeOffset? InsertedOnUtc { get; init; }

        public DateTimeOffset? NextVisibleOnUtc { get; init; }
    }
}