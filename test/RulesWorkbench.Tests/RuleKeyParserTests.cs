using UKHO.Search.Infrastructure.Ingestion.Rules;
using Xunit;

namespace RulesWorkbench.Tests
{
    public class RuleKeyParserTests
    {
        [Theory]
        [InlineData("rules:file-share:abc", true, "file-share", "abc")]
        [InlineData("rules:provider:rule-1", true, "provider", "rule-1")]
        [InlineData("rules:file-share:a:b", false, "", "")]
        [InlineData("other:file-share:abc", false, "", "")]
        [InlineData("rules::abc", false, "", "")]
        [InlineData("rules:file-share:", false, "", "")]
        public void TryParse_ValidatesExpectedShape(string key, bool expectedOk, string expectedProvider, string expectedRuleId)
        {
            var ok = RuleKeyParser.TryParse(key, out var provider, out var ruleId);

            Assert.Equal(expectedOk, ok);

            if (expectedOk)
            {
                Assert.Equal(expectedProvider, provider);
                Assert.Equal(expectedRuleId, ruleId);
            }
        }
    }
}
