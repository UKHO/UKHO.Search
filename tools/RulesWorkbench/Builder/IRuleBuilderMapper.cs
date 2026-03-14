using System.Text.Json.Nodes;

namespace RulesWorkbench.Builder
{
	public interface IRuleBuilderMapper
	{
		RuleBuilderMappingResult<BuilderRule> TryParse(JsonNode ruleJson);
		JsonObject ToJson(BuilderRule rule);
	}
}
