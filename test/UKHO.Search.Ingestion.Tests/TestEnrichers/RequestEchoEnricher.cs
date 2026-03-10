using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Tests.TestEnrichers
{
    internal sealed class RequestEchoEnricher : IIngestionEnricher
    {
        public int Ordinal => 10;

        public Task TryBuildEnrichmentAsync(IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(document);

            document.DocumentType = request.RequestType.ToString();
            document.AddFacetValue("enrichment_documentId", document.DocumentId);

            return Task.CompletedTask;
        }
    }
}