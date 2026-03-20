using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Ingestion;

namespace UKHO.Search.Infrastructure.Ingestion.Pipeline
{
    internal sealed class SyntheticTitleEnricher : IIngestionEnricher
    {
        public int Ordinal => 0;

        public Task TryBuildEnrichmentAsync(IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(document);

            document.AddTitle($"Synthetic {document.Id}");
            return Task.CompletedTask;
        }
    }
}
