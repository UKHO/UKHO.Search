using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion
{
    public interface IIngestionEnricher
    {
        int Ordinal { get; }

        Task TryBuildEnrichmentAsync(IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default);
    }
}