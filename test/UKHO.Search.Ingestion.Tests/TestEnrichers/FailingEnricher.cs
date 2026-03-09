using UKHO.Search.Ingestion;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Tests.TestEnrichers
{
    internal sealed class FailingEnricher : IIngestionEnricher
    {
        private readonly Func<int, Exception> _exceptionFactory;
        private readonly int _failuresBeforeSuccess;
        private int _callCount;
        private readonly int _ordinal;

        public FailingEnricher(int ordinal, int failuresBeforeSuccess, Func<int, Exception> exceptionFactory)
        {
            if (failuresBeforeSuccess < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(failuresBeforeSuccess));
            }

            _ordinal = ordinal;
            _failuresBeforeSuccess = failuresBeforeSuccess;
            _exceptionFactory = exceptionFactory;
        }

        public int Ordinal => _ordinal;

        public int CallCount => _callCount;

        public Task TryBuildEnrichmentAsync(IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(document);

            _callCount++;

            if (_callCount <= _failuresBeforeSuccess)
            {
                throw _exceptionFactory(_callCount);
            }

            return Task.CompletedTask;
        }
    }
}
