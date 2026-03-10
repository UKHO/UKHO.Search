using System.Linq;
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

            var (properties, timestamp) = request.AddItem is not null
                ? (request.AddItem.Properties, request.AddItem.Timestamp)
                : request.UpdateItem is not null
                    ? (request.UpdateItem.Properties, request.UpdateItem.Timestamp)
                    : throw new InvalidOperationException("Upsert requires AddItem or UpdateItem payload.");

            var sourceCopy = properties.Count == 0
                ? Array.Empty<IngestionProperty>()
                : properties.ToArray();

            return new CanonicalDocument
            {
                DocumentId = documentId,
                Source = sourceCopy,
                Timestamp = timestamp
            };
        }
    }
}