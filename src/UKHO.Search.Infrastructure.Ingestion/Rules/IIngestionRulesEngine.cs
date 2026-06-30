using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Contracts;

namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    public interface IIngestionRulesEngine
    {
        void Apply(string providerName, IngestionRequest request, CanonicalDocument document);

        IngestionRulesApplyReport ApplyWithReport(string providerName, IngestionRequest request, CanonicalDocument document);
    }
}