using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Tests.TestEnrichers
{
    internal sealed class TitleSettingEnricher : IIngestionEnricher
    {
        private readonly int _ordinal;
        private readonly string _title;

        public TitleSettingEnricher(string title, int ordinal = 0)
        {
            _title = title;
            _ordinal = ordinal;
        }

        public int Ordinal => _ordinal;

        public Task TryBuildEnrichmentAsync(IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(document);

            document.AddTitle(_title);
            return Task.CompletedTask;
        }
    }
}
