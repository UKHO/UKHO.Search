using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Providers.FileShare.Enrichment
{
    public sealed class BasicEnricher : IIngestionEnricher
    {
        public int Ordinal => 10;

        public Task TryBuildEnrichmentAsync(IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(document);

            return Task.CompletedTask;
        }
    }
}
