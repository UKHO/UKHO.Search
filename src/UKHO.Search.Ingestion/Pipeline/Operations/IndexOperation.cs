namespace UKHO.Search.Ingestion.Pipeline.Operations
{
    public abstract record IndexOperation
    {
        protected IndexOperation(string documentId)
        {
            DocumentId = documentId;
        }

        public string DocumentId { get; init; }
    }
}