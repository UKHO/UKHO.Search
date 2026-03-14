namespace RulesWorkbench.Builder
{
	public sealed record BuilderRule(
		string Id,
		string? Description,
		BuilderPredicate If,
		IReadOnlyList<BuilderThenAction> Then);
}
