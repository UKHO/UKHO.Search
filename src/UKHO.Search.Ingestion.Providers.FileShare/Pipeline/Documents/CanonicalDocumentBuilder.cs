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

            var source = request.IndexItem ?? throw new InvalidOperationException("Upsert requires IndexItem payload.");
            var properties = source.Properties;
            var sourceCopy = source with
            {
                Properties = properties.Count == 0 ? new IngestionPropertyList() : new IngestionPropertyList(properties)
            };

            return new CanonicalDocument
            {
                Id = documentId,
                Source = sourceCopy,
                Timestamp = source.Timestamp
            };
        }
    }
}