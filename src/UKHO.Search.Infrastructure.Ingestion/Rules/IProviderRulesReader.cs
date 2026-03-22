namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    public interface IProviderRulesReader
    {
        void EnsureLoaded();

        ProviderRulesSnapshot GetSnapshot();

        bool TryGetProviderRules(string providerName, out IReadOnlyList<ProviderRuleDefinition> rules);
    }
}
