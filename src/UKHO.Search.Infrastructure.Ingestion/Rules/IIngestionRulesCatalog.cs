namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    public interface IIngestionRulesCatalog
    {
        void EnsureLoaded();

        IReadOnlyDictionary<string, IReadOnlyList<string>> GetRuleIdsByProvider();
    }
}