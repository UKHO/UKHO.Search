using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Ingestion.Rules;

namespace UKHO.Search.Ingestion.Tests.TestEnrichers
{
    internal sealed class ProviderContextRecordingEnricher : IIngestionEnricher
    {
        private readonly IIngestionProviderContext _providerContext;
        private readonly List<string?> _providerNames;

        public ProviderContextRecordingEnricher(IIngestionProviderContext providerContext, List<string?> providerNames)
        {
            _providerContext = providerContext;
            _providerNames = providerNames;
        }

        public int Ordinal => 0;

        public Task TryBuildEnrichmentAsync(IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(document);

            _providerNames.Add(_providerContext.ProviderName);
            return Task.CompletedTask;
        }
    }
}