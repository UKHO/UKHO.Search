using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using UKHO.Search.Geo;
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

            var batchId = request.AddItem?.Id ?? request.UpdateItem?.Id;

            var catalogPath = paths.Select(p => new { Path = p, FileName = System.IO.Path.GetFileName(p) })
                                   .Where(x => string.Equals(x.FileName, "catalog.xml", StringComparison.OrdinalIgnoreCase))
                                   .Select(x => x.Path)
                                   .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                                   .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(catalogPath))
            {
                return;
            }

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
                var gco = XNamespace.Get("http://standards.iso.org/iso/19115/-3/gco/1.0");
                var gml = XNamespace.Get("http://www.opengis.net/gml/3.2");

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

                document.SetKeyword("S-101");
                document.SetKeyword("S101");

                var organization = root.Element(xc + "contact")
                                       ?.Element(xc + "organization")
                                       ?.Element(gco + "CharacterString")
                                       ?.Value;

                if (string.IsNullOrWhiteSpace(organization))
                {
                    _logger.LogWarning("S-101 catalog.xml is missing XC:contact/XC:organization/gco:CharacterString. BatchId={BatchId} FilePath={FilePath}", batchId, catalogPath);
                }
                else
                {
                    document.AddSearchText(organization);
                }

                var comment = root.Element(xc + "exchangeCatalogueComment")
                                  ?.Element(gco + "CharacterString")
                                  ?.Value;

                if (string.IsNullOrWhiteSpace(comment))
                {
                    _logger.LogWarning("S-101 catalog.xml is missing XC:exchangeCatalogueComment/gco:CharacterString. BatchId={BatchId} FilePath={FilePath}", batchId, catalogPath);
                }
                else
                {
                    document.AddSearchText(comment);
                }

                var posLists = root.Descendants(xc + "dataCoverage")
                                   .Descendants(gml + "posList")
                                   .Select(x => x.Value)
                                   .Where(v => !string.IsNullOrWhiteSpace(v));

                foreach (var posList in posLists)
                {
                    if (!TryParseLatLonPosList(posList, out var polygon))
                    {
                        _logger.LogWarning("Failed to parse gml:posList into geo polygon. BatchId={BatchId} FilePath={FilePath}", batchId, catalogPath);
                        continue;
                    }

                    document.AddGeoPolygon(polygon);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to load catalog.xml. BatchId={BatchId} FilePath={FilePath}", batchId, catalogPath);
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

        private static bool TryParseLatLonPosList(string posList, out GeoPolygon polygon)
        {
            polygon = null!;

            var tokens = posList.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length < 8 || tokens.Length % 2 != 0)
            {
                return false;
            }

            var ring = new List<GeoCoordinate>(capacity: (tokens.Length / 2) + 1);
            for (var i = 0; i < tokens.Length; i += 2)
            {
                if (!double.TryParse(tokens[i], System.Globalization.CultureInfo.InvariantCulture, out var lat))
                {
                    return false;
                }

                if (!double.TryParse(tokens[i + 1], System.Globalization.CultureInfo.InvariantCulture, out var lon))
                {
                    return false;
                }

                // gml:posList is encoded as lat lon (EPSG:4326 WGS-84), GeoCoordinate expects lon/lat.
                ring.Add(GeoCoordinate.Create(lon, lat));
            }

            if (!ring[0].Equals(ring[^1]))
            {
                ring.Add(ring[0]);
            }

            polygon = GeoPolygon.Create(ring);
            return true;
        }
    }
}
