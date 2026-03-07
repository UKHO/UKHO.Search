namespace UKHO.Search.Pipelines.Nodes
{
    public sealed class BulkIndexResponse
    {
        public required IReadOnlyList<BulkIndexItemResult> Items { get; init; }
    }
}