using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Tests.TestEnrichers
{
    internal sealed class FailingEnricher : IIngestionEnricher
    {
        private readonly Func<int, Exception> _exceptionFactory;
        private readonly int _failuresBeforeSuccess;
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

        public int CallCount { get; private set; }

        public int Ordinal => _ordinal;

        public Task TryBuildEnrichmentAsync(IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(document);

            CallCount++;

            if (CallCount <= _failuresBeforeSuccess)
            {
                throw _exceptionFactory(CallCount);
            }

            document.AddTitle(nameof(FailingEnricher));
            return Task.CompletedTask;
        }
    }
}