using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Rules;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Rules
{
    /// <summary>
    /// Verifies the shared App Configuration key contract for ingestion-authored rules.
    /// </summary>
    public sealed class IngestionRuleConfigurationPathTests
    {
        /// <summary>
        /// Verifies that rule keys are written beneath the ingestion namespace while preserving the logical provider segment.
        /// </summary>
        [Fact]
        public void BuildRuleKey_WhenProviderAndRuleIdAreProvided_ShouldReturnNamespaceAwareKey()
        {
            // Build the key for a representative file-share rule using mixed provider casing to verify normalization.
            var key = IngestionRuleConfigurationPath.BuildRuleKey("FILE-SHARE", "rule-1");

            // The resulting key must use the rules:ingestion root and the canonical lowercase provider name.
            key.ShouldBe("rules:ingestion:file-share:rule-1");
        }

        /// <summary>
        /// Verifies that nested rule identifiers remain nested rather than being flattened away.
        /// </summary>
        [Fact]
        public void BuildRuleKey_WhenRuleIdentifierContainsNestedSegments_ShouldPreserveNestedSegments()
        {
            // Build the key for a rule that originated from a nested repository folder beneath the provider.
            var key = IngestionRuleConfigurationPath.BuildRuleKey("file-share", "subset:example");

            // The resulting key must preserve the nested rule-id segments after the provider name.
            key.ShouldBe("rules:ingestion:file-share:subset:example");
        }
    }
}
