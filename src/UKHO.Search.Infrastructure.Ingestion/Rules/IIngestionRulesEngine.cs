using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    public interface IIngestionRulesEngine
    {
        void Apply(string providerName, IngestionRequest request, CanonicalDocument document);
    }
}