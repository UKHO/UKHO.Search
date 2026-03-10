using UKHO.Search.Ingestion.Pipeline.Documents;

namespace UKHO.Search.Ingestion.Pipeline.Operations
{
    public sealed record UpsertOperation : IndexOperation
    {
        public UpsertOperation(string documentId, CanonicalDocument document) : base(documentId)
        {
            Document = document;
        }

        public CanonicalDocument Document { get; init; }
    }
}