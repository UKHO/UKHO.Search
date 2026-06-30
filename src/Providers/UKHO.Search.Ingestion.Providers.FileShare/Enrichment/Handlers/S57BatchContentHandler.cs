using Microsoft.Extensions.Logging;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Contracts;
using UKHO.Search.Ingestion.Providers.FileShare.Enrichment.Handlers.Enrichers;

namespace UKHO.Search.Ingestion.Providers.FileShare.Enrichment.Handlers
{
    internal sealed class S57BatchContentHandler : IBatchContentHandler
    {
        private readonly ILogger<S57BatchContentHandler> _logger;

        public S57BatchContentHandler(ILogger<S57BatchContentHandler> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            _logger = logger;
        }

        public Task HandleFiles(IEnumerable<string> paths, IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(paths);
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(document);

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            var batchId = request.IndexItem?.Id;
            var fileCount = paths is ICollection<string> c ? c.Count : paths.Count();

            var datasets = S57DatasetGrouper.GroupDatasets(paths);

            _logger.LogDebug(
                "S57 batch content handler invoked. BatchId={BatchId} FileCount={FileCount} DatasetCount={DatasetCount}",
                batchId,
                fileCount,
                datasets.Count);

            foreach (var dataset in datasets)
            {
                _logger.LogDebug(
                    "S57 dataset detected. BatchId={BatchId} BaseName={BaseName} EntryPoint={EntryPoint} Members={MemberCount}",
                    batchId,
                    dataset.BaseName,
                    dataset.EntryPointPath,
                    dataset.MemberPaths.Count);
            }

            var first = datasets.FirstOrDefault();
            if (first is null)
            {
                return Task.CompletedTask;
            }

            var enricher = new BasicS57Enricher(new LoggerAdapter<BasicS57Enricher>(_logger));
            if (!enricher.TryParse(first.EntryPointPath, document))
            {
                _logger.LogWarning("S57 parsing failed. BatchId={BatchId} EntryPoint={EntryPoint}", batchId, first.EntryPointPath);
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}
