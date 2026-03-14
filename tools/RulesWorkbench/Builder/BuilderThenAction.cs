namespace RulesWorkbench.Builder
{
	public sealed record BuilderThenAction(
		ThenActionType Type,
		IReadOnlyList<string> Values);
}
