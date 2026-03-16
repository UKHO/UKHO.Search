using System.Text.Json.Nodes;

namespace RulesWorkbench.Services
{
	public sealed record RuleSummary(
		int Index,
     string? FilePath,
		string? Id,
		string? Description,
		JsonNode RuleJson)
	{
	}
}
