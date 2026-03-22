namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    public sealed class ProviderRulesSnapshot
    {
        public required string SchemaVersion { get; init; }

        public required IReadOnlyDictionary<string, IReadOnlyList<ProviderRuleDefinition>> RulesByProvider { get; init; }
    }
}
