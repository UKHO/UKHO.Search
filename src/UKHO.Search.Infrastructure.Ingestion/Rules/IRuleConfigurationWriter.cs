namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    public interface IRuleConfigurationWriter
    {
        Task SetRuleAsync(string provider, string ruleId, string json, CancellationToken cancellationToken);

        Task TouchSentinelAsync(CancellationToken cancellationToken);
    }
}
