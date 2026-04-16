using UKHO.Search.Infrastructure.Ingestion.Rules;
using Xunit;

namespace RulesWorkbench.Tests
{
    /// <summary>
    /// Verifies that the shared rule-key parser understands the namespace-aware App Configuration rule contract.
    /// </summary>
    public sealed class RuleKeyParserTests
    {
        /// <summary>
        /// Verifies that namespace-aware rule keys parse into the expected provider and provider-relative rule identifier.
        /// </summary>
        /// <param name="key">The App Configuration key to parse.</param>
        /// <param name="expectedOk">Indicates whether parsing should succeed.</param>
        /// <param name="expectedProvider">The expected logical provider when parsing succeeds.</param>
        /// <param name="expectedRuleId">The expected provider-relative rule identifier when parsing succeeds.</param>
        [Theory]
        [InlineData("rules:ingestion:file-share:abc", true, "file-share", "abc")]
        [InlineData("rules:ingestion:provider:rule-1", true, "provider", "rule-1")]
        [InlineData("rules:ingestion:FILE-SHARE:a:b", true, "file-share", "a:b")]
        [InlineData("rules:file-share:abc", false, "", "")]
        [InlineData("other:file-share:abc", false, "", "")]
        [InlineData("rules:ingestion::abc", false, "", "")]
        [InlineData("rules:ingestion:file-share:", false, "", "")]
        public void TryParse_ValidatesExpectedShape(string key, bool expectedOk, string expectedProvider, string expectedRuleId)
        {
            // Parse the key through the shared parser so the test pins the namespace-aware reader and writer contract together.
            var ok = RuleKeyParser.TryParse(key, out var provider, out var ruleId);

            // The parser should either reject the key entirely or return the normalized provider and rule identifier.
            Assert.Equal(expectedOk, ok);

            if (expectedOk)
            {
                Assert.Equal(expectedProvider, provider);
                Assert.Equal(expectedRuleId, ruleId);
            }
        }
    }
}
