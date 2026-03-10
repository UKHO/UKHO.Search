namespace UKHO.Search.Pipelines.Nodes
{
    public interface IBulkIndexClient<TDocument>
    {
        ValueTask<BulkIndexResponse> BulkIndexAsync(BulkIndexRequest<TDocument> request, CancellationToken cancellationToken);
    }
}