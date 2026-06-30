using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Contracts;

namespace UKHO.Search.Ingestion
{
    public interface IBatchContentHandler
    {
        Task HandleFiles(IEnumerable<string> paths, IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default);
    }
}
