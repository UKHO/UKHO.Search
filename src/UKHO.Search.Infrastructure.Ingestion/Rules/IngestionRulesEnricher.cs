using UKHO.Search.Ingestion;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Ingestion.Rules;

namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    internal sealed class IngestionRulesEnricher : IIngestionEnricher
    {
        private readonly IIngestionProviderContext _providerContext;
        private readonly IIngestionRulesEngine _rulesEngine;

        public IngestionRulesEnricher(IIngestionRulesEngine rulesEngine, IIngestionProviderContext providerContext)
        {
            _rulesEngine = rulesEngine;
            _providerContext = providerContext;
        }

        public int Ordinal => 50;

        public Task TryBuildEnrichmentAsync(IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var providerName = _providerContext.ProviderName;
            if (string.IsNullOrWhiteSpace(providerName))
            {
                return Task.CompletedTask;
            }

            _rulesEngine.Apply(providerName, request, document);
            return Task.CompletedTask;
        }
    }
}