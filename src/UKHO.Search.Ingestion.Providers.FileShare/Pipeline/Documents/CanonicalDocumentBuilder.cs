using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Providers.FileShare.Pipeline.Documents
{
    public sealed class CanonicalDocumentBuilder
    {
        public CanonicalDocument BuildForUpsert(string documentId, IngestionRequest request)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
            ArgumentNullException.ThrowIfNull(request);

            return new CanonicalDocument
            {
                DocumentId = documentId,
                Source = request
            };
        }
    }
}