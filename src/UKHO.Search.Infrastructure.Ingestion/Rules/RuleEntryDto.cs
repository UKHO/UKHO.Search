namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    public sealed record RuleEntryDto(
        string Key,
        string Provider,
        string RuleId,
        string? Json,
        bool IsValid,
        string? ErrorMessage);
}
