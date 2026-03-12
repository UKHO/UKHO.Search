using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Providers.FileShare.Enrichment.Handlers
{
    public sealed class S100BatchContentHandler : IBatchContentHandler
    {
        private readonly ILogger<S100BatchContentHandler> _logger;

        public S100BatchContentHandler(ILogger<S100BatchContentHandler> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            _logger = logger;
        }

        public async Task HandleFiles(IEnumerable<string> paths, IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(paths);
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(document);

            var catalogPath = paths.Select(p => new { Path = p, FileName = System.IO.Path.GetFileName(p) })
                                   .Where(x => string.Equals(x.FileName, "catalog.xml", StringComparison.OrdinalIgnoreCase))
                                   .Select(x => x.Path)
                                   .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                                   .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(catalogPath))
            {
                await HandleS100Files(catalogPath, request, document, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task HandleS100Files(string catalogPath, IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken)
        {
            var batchId = request.AddItem?.Id ?? request.UpdateItem?.Id;

            // catalog.xml is present, so this is S-100 data

            try
            {
                await using var stream = File.OpenRead(catalogPath);
                var catalogXml = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken)
                                                .ConfigureAwait(false);

                var root = catalogXml.Root;
                if (root is null)
                {
                    _logger.LogWarning("catalog.xml has no root element. BatchId={BatchId} FilePath={FilePath}", batchId, catalogPath);
                    return;
                }

                var xc = XNamespace.Get("http://www.iho.int/s100/xc/5.2");

                // We only consider S-101 at the moment.
                var productName = root.Element(xc + "productSpecification")
                                      ?.Element(xc + "name")
                                      ?.Value;

                if (!string.IsNullOrWhiteSpace(productName))
                {
                    document.DocumentType = productName;
                }

                if (!IsS101ProductName(productName))
                {
                    return;
                }

                var parser = new S101Parser(new LoggerAdapter<S101Parser>(_logger));
                parser.TryEnrichFromCatalogue(catalogXml, document);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to enrich from catalog.xml. BatchId={BatchId} FilePath={FilePath}", batchId, catalogPath);
            }
        }

        private static bool IsS101ProductName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return value.Trim().Equals("S-101", StringComparison.OrdinalIgnoreCase)
                   || value.Trim().Equals("S101", StringComparison.OrdinalIgnoreCase);
        }
    }
}
