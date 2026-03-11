namespace UKHO.Search.Infrastructure.Ingestion.Rules.Model
{
    internal sealed class RulesetDto
    {
        public string? SchemaVersion { get; set; }

        public Dictionary<string, RuleDto[]>? Rules { get; set; }
    }
}