using System.Xml.Linq;
using UKHO.Search.Ingestion.Pipeline.Documents;

namespace UKHO.Search.Ingestion.Providers.FileShare.Enrichment.Handlers
{
    internal interface IS100Parser
    {
        bool TryEnrichFromCatalogue(XDocument catalogueXml, CanonicalDocument document);
    }
}
