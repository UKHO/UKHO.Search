using System.Text.Json.Nodes;
using UKHO.Search.Ingestion;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Tests.TestEnrichers
{
    internal sealed class RequestEchoEnricher : IIngestionEnricher
    {
        public int Ordinal => 10;

        public Task TryBuildEnrichmentAsync(IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(document);

            document.Source["enrichment_requestType"] = request.RequestType.ToString();
            document.Source["enrichment_documentId"] = document.DocumentId;

            return Task.CompletedTask;
        }
    }
}
