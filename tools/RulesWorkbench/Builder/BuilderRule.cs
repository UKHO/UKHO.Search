namespace RulesWorkbench.Builder
{
	public sealed record BuilderRule(
		string Id,
        string Title,
		string? Description,
		BuilderPredicate If,
		IReadOnlyList<BuilderThenAction> Then);
}
