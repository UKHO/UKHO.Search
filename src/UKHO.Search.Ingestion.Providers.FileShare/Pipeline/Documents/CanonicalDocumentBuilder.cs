using UKHO.Search.Ingestion.Pipeline;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Providers.FileShare.Pipeline.Documents
{
    public sealed class CanonicalDocumentBuilder
    {
        public CanonicalDocument BuildForUpsert(string documentId, IngestionRequest request, ProviderParameters providerParameters)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(providerParameters);

            var source = request.IndexItem ?? throw new InvalidOperationException("Upsert requires IndexItem payload.");
            var properties = source.Properties;
            var sourceCopy = source with
            {
                Properties = properties.Count == 0 ? new IngestionPropertyList() : new IngestionPropertyList(properties)
            };

            return CanonicalDocument.CreateMinimal(documentId, providerParameters.Provider, sourceCopy, source.Timestamp);
        }
    }
}