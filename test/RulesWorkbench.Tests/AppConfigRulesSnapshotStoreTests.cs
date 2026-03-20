using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RulesWorkbench.Services;
using Shouldly;
using Xunit;

namespace RulesWorkbench.Tests
{
    public sealed class AppConfigRulesSnapshotStoreTests
    {
        [Fact]
        public void GetRules_WhenContextPresent_ProjectsNormalizedContextIntoRuleEntry()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["rules:file-share:rule-1"] = "{\"schemaVersion\":\"1.0\",\"rule\":{\"id\":\"rule-1\",\"context\":\"AdDs-S100\",\"title\":\"Rule 1 title\",\"if\":{\"id\":\"batch-1\"},\"then\":{\"category\":{\"add\":[\"charts\"]}}}}"
                })
                .Build();

            var store = new AppConfigRulesSnapshotStore(configuration, new SystemTextJsonRuleJsonValidator(), NullLogger<AppConfigRulesSnapshotStore>.Instance);

            var rules = store.GetRules(null);

            rules.ShouldHaveSingleItem();
            rules[0].Context.ShouldBe("adds-s100");
        }

        [Fact]
        public void GetRules_WhenTitleMissing_ProjectsInvalidEntry()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["rules:file-share:rule-1"] = "{\"schemaVersion\":\"1.0\",\"rule\":{\"id\":\"rule-1\",\"if\":{\"id\":\"batch-1\"},\"then\":{\"category\":{\"add\":[\"charts\"]}}}}"
                })
                .Build();

            var store = new AppConfigRulesSnapshotStore(configuration, new SystemTextJsonRuleJsonValidator(), NullLogger<AppConfigRulesSnapshotStore>.Instance);

            var rules = store.GetRules(null);

            rules.ShouldHaveSingleItem();
            rules[0].IsValid.ShouldBeFalse();
            rules[0].ErrorMessage.ShouldContain("title");
        }

        [Fact]
        public void UpdateRuleJson_WhenTitleMissing_ReturnsValidationError()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["rules:file-share:rule-1"] = "{\"schemaVersion\":\"1.0\",\"rule\":{\"id\":\"rule-1\",\"title\":\"Rule 1 title\",\"if\":{\"id\":\"batch-1\"},\"then\":{\"category\":{\"add\":[\"charts\"]}}}}"
                })
                .Build();

            var store = new AppConfigRulesSnapshotStore(configuration, new SystemTextJsonRuleJsonValidator(), NullLogger<AppConfigRulesSnapshotStore>.Instance);

            var result = store.UpdateRuleJson("file-share", "rule-1", "{\"id\":\"rule-1\"}");

            result.IsValid.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("title");
        }
    }
}
