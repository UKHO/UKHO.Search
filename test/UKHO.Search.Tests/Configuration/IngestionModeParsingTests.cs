using Shouldly;
using UKHO.Search.Configuration;
using Xunit;

namespace UKHO.Search.Tests.Configuration
{
    public sealed class IngestionModeParsingTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Parse_throws_when_env_var_missing_or_blank(string? value)
        {
            Should.Throw<InvalidOperationException>(() => IngestionModeParser.Parse(value));
        }

        [Theory]
        [InlineData("nope")]
        [InlineData("best-effort")]
        [InlineData("STRICTEST")]
        public void Parse_throws_when_value_unparsable(string value)
        {
            Should.Throw<InvalidOperationException>(() => IngestionModeParser.Parse(value));
        }

        [Theory]
        [InlineData("strict", IngestionMode.Strict)]
        [InlineData("Strict", IngestionMode.Strict)]
        [InlineData("BESTEFFORT", IngestionMode.BestEffort)]
        [InlineData("bestEffort", IngestionMode.BestEffort)]
        public void Parse_accepts_valid_values_case_insensitive(string value, IngestionMode expected)
        {
            IngestionModeParser.Parse(value)
                               .ShouldBe(expected);
        }
    }
}
