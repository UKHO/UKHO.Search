using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Rules;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Rules
{
    public sealed class RuleContextMigrationHelperTests
    {
        [Theory]
        [InlineData("bu-adds-4-something.json", "adds")]
        [InlineData("bu-adds-s100-4-base-exchange-set-product-type.json", "adds-s100")]
        [InlineData("bu-avcs-bespokeexchangesets-12-foo.json", "avcs-bespokeexchangesets")]
        [InlineData("bu-test-penrose-s57-99-sample.json", "test-penrose-s57")]
        public void TryDeriveContextFromLegacyFileName_WhenPatternMatches_ReturnsLowercaseContext(string fileName, string expectedContext)
        {
            var ok = RuleContextMigrationHelper.TryDeriveContextFromLegacyFileName(fileName, out var context);

            ok.ShouldBeTrue();
            context.ShouldBe(expectedContext);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("navpac.json")]
        [InlineData("bu-no-integer-segment.json")]
        [InlineData("bu--4-invalid.json")]
        public void TryDeriveContextFromLegacyFileName_WhenPatternDoesNotMatch_ReturnsFalse(string? fileName)
        {
            var ok = RuleContextMigrationHelper.TryDeriveContextFromLegacyFileName(fileName, out var context);

            ok.ShouldBeFalse();
            context.ShouldBeNull();
        }
    }
}
