using Kreuzberg;
using Microsoft.Extensions.Logging;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Contracts;
using UKHO.Search.Query;

namespace UKHO.Search.Ingestion.Providers.FileShare.Enrichment.Handlers
{
    public sealed class TextExtractionBatchContentHandler : IBatchContentHandler
    {
        private readonly ILogger<TextExtractionBatchContentHandler> _logger;
        private readonly HashSet<string> _allowedExtensions;
        private readonly TokenNormalizer _tokenNormalizer = new();

        public TextExtractionBatchContentHandler(IEnumerable<string> allowedExtensions, ILogger<TextExtractionBatchContentHandler> logger)
        {
            ArgumentNullException.ThrowIfNull(allowedExtensions);
            ArgumentNullException.ThrowIfNull(logger);

            _allowedExtensions = new HashSet<string>(StringComparer.Ordinal);
            foreach (var ext in allowedExtensions)
            {
                if (string.IsNullOrWhiteSpace(ext))
                {
                    continue;
                }

                var normalized = ext.StartsWith(".", StringComparison.Ordinal) ? ext : "." + ext;
                _allowedExtensions.Add(normalized.ToLowerInvariant());
            }

            _logger = logger;
        }

        public async Task HandleFiles(IEnumerable<string> paths, IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(paths);
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(document);

            if (_allowedExtensions.Count == 0)
            {
                return;
            }

            var batchId = request.IndexItem?.Id;

            foreach (var filePath in paths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var extension = Path.GetExtension(filePath)
                                    .ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(extension) || !_allowedExtensions.Contains(extension))
                {
                    continue;
                }

                try
                {
                    var result = await KreuzbergClient.ExtractFileAsync(filePath, null, cancellationToken)
                                                      .ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(result.Content))
                    {
                        continue;
                    }

                    document.AddContent(result.Content);

                    var keyword = Path.GetFileNameWithoutExtension(filePath);
                    foreach (var normalizedKeyword in _tokenNormalizer.NormalizeToken(keyword))
                    {
                        document.AddKeyword(normalizedKeyword);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Failed to extract file content. BatchId={BatchId} FilePath={FilePath}", batchId, filePath);
                }
            }
        }
    }
}
