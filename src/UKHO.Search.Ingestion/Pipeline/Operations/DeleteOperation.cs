namespace UKHO.Search.Ingestion.Pipeline.Operations
{
    public sealed record DeleteOperation : IndexOperation
    {
        public DeleteOperation(string documentId) : base(documentId)
        {
        }
    }
}