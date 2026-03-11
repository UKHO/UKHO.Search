namespace UKHO.Search.Infrastructure.Ingestion.Rules.Validation
{
    internal sealed class ValidatedRuleset
    {
        public required string SchemaVersion { get; init; }

        public required IReadOnlyDictionary<string, IReadOnlyList<ValidatedRule>> RulesByProvider { get; init; }
    }
}