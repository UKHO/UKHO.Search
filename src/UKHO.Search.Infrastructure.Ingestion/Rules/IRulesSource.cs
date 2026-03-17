namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    public interface IRulesSource
    {
        IReadOnlyList<RuleEntryDto> ListRuleEntries();
    }
}
