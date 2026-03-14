namespace RulesWorkbench.Builder
{
	public sealed record BuilderCondition(
		ConditionType Type,
		string? PropertyName,
		string? Value);
}
