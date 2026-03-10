namespace UKHO.Search.Infrastructure.Ingestion.Queue
{
    public sealed record QueueUpdateReceipt
    {
        public required string PopReceipt { get; init; }

        public DateTimeOffset? NextVisibleOnUtc { get; init; }
    }
}