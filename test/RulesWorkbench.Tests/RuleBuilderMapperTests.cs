using RulesWorkbench.Builder;
using Shouldly;
using Xunit;

namespace RulesWorkbench.Tests
{
	public class RuleBuilderMapperTests
	{
		[Fact]
		public void ToJson_ForPropertiesExists_CreatesExpectedShape()
		{
			var mapper = new RuleBuilderMapper();
			var rule = new BuilderRule(
				"my-rule",
				"desc",
				new BuilderPredicate(CompositionType.All, new[]
				{
					new BuilderCondition(ConditionType.PropertiesExists, "edition number", null),
				}),
				new[]
				{
					new BuilderThenAction(ThenActionType.KeywordsAdd, new[] { "S57" }),
				});

			var json = mapper.ToJson(rule).ToJsonString();

			json.ShouldContain("\"id\":\"my-rule\"");
			json.ShouldContain("\"if\"");
			json.ShouldContain("\"all\"");
			json.ShouldContain("edition number");
			json.ShouldContain("\"exists\":true");
			json.ShouldContain("\"then\"");
			json.ShouldContain("\"keywords\"");
			json.ShouldContain("\"add\"");
		}
	}
}
