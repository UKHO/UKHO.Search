using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Tests.TestEnrichers
{
    internal sealed class RecordingEnricherB : IIngestionEnricher
    {
        private readonly List<string> _calls;
        private readonly int _ordinal;

        public RecordingEnricherB(List<string> calls, int ordinal)
        {
            _calls = calls;
            _ordinal = ordinal;
        }

        public int Ordinal => _ordinal;

        public Task TryBuildEnrichmentAsync(IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(document);

            _calls.Add(nameof(RecordingEnricherB));
            return Task.CompletedTask;
        }
    }
}