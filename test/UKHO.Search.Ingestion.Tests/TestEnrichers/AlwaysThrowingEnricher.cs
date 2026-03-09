using UKHO.Search.Ingestion;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Tests.TestEnrichers
{
    internal sealed class AlwaysThrowingEnricher : IIngestionEnricher
    {
        private readonly Func<Exception> _exceptionFactory;
        private readonly int _ordinal;

        public AlwaysThrowingEnricher(int ordinal, Func<Exception> exceptionFactory)
        {
            _ordinal = ordinal;
            _exceptionFactory = exceptionFactory;
        }

        public int Ordinal => _ordinal;

        public Task TryBuildEnrichmentAsync(IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)
        {
            throw _exceptionFactory();
        }
    }
}
