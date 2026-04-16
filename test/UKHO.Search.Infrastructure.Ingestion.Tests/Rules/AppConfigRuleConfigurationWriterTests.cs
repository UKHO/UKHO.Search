using System.Net.Mime;
using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Rules;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Rules
{
    /// <summary>
    /// Verifies the non-network portions of <see cref="AppConfigRuleConfigurationWriter"/> that shape App Configuration save-back operations.
    /// </summary>
    public sealed class AppConfigRuleConfigurationWriterTests
    {
        /// <summary>
        /// Verifies that configured labels are normalized to lowercase before save-back occurs.
        /// </summary>
        [Fact]
        public void NormalizeLabel_WhenConfigurationValueIsProvided_ShouldReturnLowercaseLabel()
        {
            // Normalize a representative mixed-case label to prove the writer avoids duplicate labels that differ only by casing.
            var label = AppConfigRuleConfigurationWriter.NormalizeLabel("AdDs-Prod");

            // The resulting label must be lowercase because save-back always targets normalized App Configuration labels.
            label.ShouldBe("adds-prod");
        }

        /// <summary>
        /// Verifies that save-back settings use the namespace-aware rule key and JSON content type.
        /// </summary>
        [Fact]
        public void CreateRuleSetting_WhenCalled_ShouldUseNamespaceAwareKeyAndJsonContentType()
        {
            // Build the setting that would be written for a representative file-share rule update.
            var setting = AppConfigRuleConfigurationWriter.CreateRuleSetting("FILE-SHARE", "rule-1", "{\"id\":\"rule-1\"}", AppConfigRuleConfigurationWriter.NormalizeLabel("AdDs"));

            // The generated setting must target the new namespace-aware key contract and preserve JSON metadata.
            setting.Key.ShouldBe("rules:ingestion:file-share:rule-1");
            setting.Label.ShouldBe("adds");
            setting.ContentType.ShouldBe(MediaTypeNames.Application.Json);
            setting.Value.ShouldBe("{\"id\":\"rule-1\"}");
        }
    }
}
