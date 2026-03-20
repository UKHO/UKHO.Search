using RulesWorkbench.Services;
using Shouldly;
using Xunit;

namespace RulesWorkbench.Tests
{
	public class SystemTextJsonRuleJsonValidatorTests
	{
		[Fact]
		public void Validate_WhenJsonInvalid_ReturnsInvalid()
		{
			var validator = new SystemTextJsonRuleJsonValidator();

			var result = validator.Validate("{");

			result.IsValid.ShouldBeFalse();
			result.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
		}

		[Fact]
		public void Validate_WhenJsonValid_ReturnsValid()
		{
			var validator = new SystemTextJsonRuleJsonValidator();

          var result = validator.Validate("{\"id\":\"x\",\"title\":\"Display title\"}");

			result.IsValid.ShouldBeTrue();
			result.ErrorMessage.ShouldBeNull();
		}

		[Fact]
		public void Validate_WhenWrappedRuleJsonHasTitle_ReturnsValid()
		{
			var validator = new SystemTextJsonRuleJsonValidator();

			var result = validator.Validate("{\"schemaVersion\":\"1.0\",\"rule\":{\"id\":\"x\",\"title\":\"Display title\"}}");

			result.IsValid.ShouldBeTrue();
			result.ErrorMessage.ShouldBeNull();
		}

		[Fact]
		public void Validate_WhenTitleMissing_ReturnsInvalid()
		{
			var validator = new SystemTextJsonRuleJsonValidator();

			var result = validator.Validate("{\"id\":\"x\"}");

			result.IsValid.ShouldBeFalse();
			result.ErrorMessage.ShouldContain("title");
		}

		[Fact]
		public void Validate_WhenTitleBlank_ReturnsInvalid()
		{
			var validator = new SystemTextJsonRuleJsonValidator();

			var result = validator.Validate("{\"id\":\"x\",\"title\":\"   \"}");

			result.IsValid.ShouldBeFalse();
			result.ErrorMessage.ShouldContain("title");
		}
	}
}
