using System.Text.Json.Nodes;

namespace RulesWorkbench.Services
{
    public sealed record RulesWorkbenchRuleEntry(
        int Index,
        string Provider,
        string RuleId,
        string Key,
        JsonNode? RuleJson,
        bool IsValid,
        string? ErrorMessage);
}
