using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using UKHO.Search.Geo;
using UKHO.Search.Ingestion.Pipeline.Documents;

namespace UKHO.Search.Ingestion.Providers.FileShare.Enrichment.Handlers
{
    internal sealed class S101Parser : IS100Parser
    {
        private readonly ILogger<S101Parser> _logger;

        public S101Parser(ILogger<S101Parser> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            _logger = logger;
        }

        public bool TryEnrichFromCatalogue(XDocument catalogueXml, CanonicalDocument document)
        {
            ArgumentNullException.ThrowIfNull(catalogueXml);
            ArgumentNullException.ThrowIfNull(document);

            var root = catalogueXml.Root;
            if (root is null)
            {
                _logger.LogWarning("catalog.xml has no root element.");
                return false;
            }

            var xc = XNamespace.Get("http://www.iho.int/s100/xc/5.2");
            var gco = XNamespace.Get("http://standards.iso.org/iso/19115/-3/gco/1.0");
            var gml = XNamespace.Get("http://www.opengis.net/gml/3.2");

            document.SetKeyword("S-101");
            document.SetKeyword("S101");

            var organization = root.Element(xc + "contact")
                                   ?.Element(xc + "organization")
                                   ?.Element(gco + "CharacterString")
                                   ?.Value;

            if (string.IsNullOrWhiteSpace(organization))
            {
                _logger.LogWarning("S-101 catalog.xml is missing XC:contact/XC:organization/gco:CharacterString.");
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
                _logger.LogWarning("S-101 catalog.xml is missing XC:exchangeCatalogueComment/gco:CharacterString.");
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
                    _logger.LogWarning("Failed to parse gml:posList into geo polygon.");
                    continue;
                }

                document.AddGeoPolygon(polygon);
            }

            return true;
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
