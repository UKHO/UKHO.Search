namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    public sealed class ProviderRuleDefinition
    {
        public required string Id { get; init; }

        public string? Context { get; init; }

        public string? Title { get; init; }

        public string? Description { get; init; }

        public required bool Enabled { get; init; }
    }
}
