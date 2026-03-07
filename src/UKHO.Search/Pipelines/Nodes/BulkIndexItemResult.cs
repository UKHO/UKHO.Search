namespace UKHO.Search.Pipelines.Nodes
{
    public sealed class BulkIndexItemResult
    {
        public required Guid MessageId { get; init; }

        public required int StatusCode { get; init; }

        public string? ErrorType { get; init; }

        public string? ErrorReason { get; init; }
    }
}