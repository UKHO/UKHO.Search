namespace RulesWorkbench.Builder
{
	public sealed record BuilderPredicate(
		CompositionType Composition,
		IReadOnlyList<BuilderCondition> Conditions);
}
