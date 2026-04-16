using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RulesWorkbench.Services;
using Shouldly;
using Xunit;

namespace RulesWorkbench.Tests
{
    /// <summary>
    /// Verifies that <see cref="AppConfigRulesSnapshotStore"/> reads and updates namespace-aware App Configuration rule entries correctly.
    /// </summary>
    public sealed class AppConfigRulesSnapshotStoreTests
    {
        /// <summary>
        /// Verifies that namespace-aware App Configuration keys are projected into canonical provider, rule-id, key, and context metadata.
        /// </summary>
        [Fact]
        public void GetRules_WhenNamespaceAwareRulesExist_ProjectsCanonicalMetadataIntoRuleEntries()
        {
            // Arrange rules beneath the ingestion namespace, including one nested rule identifier, to mirror the live App Configuration shape.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["rules:ingestion:FILE-SHARE:rule-1"] = CreateRuleJson("rule-1", context: "AdDs-S100", description: "Rule 1 description"),
                    ["rules:ingestion:file-share:subset:rule-2"] = CreateRuleJson("subset:rule-2", title: "Rule 2 title", description: "Nested rule description"),
                })
                .Build();
            var store = new AppConfigRulesSnapshotStore(configuration, new SystemTextJsonRuleJsonValidator(), NullLogger<AppConfigRulesSnapshotStore>.Instance);

            // Load the snapshot through the real RulesWorkbench App Configuration store.
            var rules = store.GetRules(null).OrderBy(rule => rule.RuleId, StringComparer.Ordinal).ToArray();

            // The snapshot must preserve canonical provider identity, full keys, nested rule ids, and normalized context.
            rules.Length.ShouldBe(2);
            rules[0].Provider.ShouldBe("file-share");
            rules[0].RuleId.ShouldBe("rule-1");
            rules[0].Key.ShouldBe("rules:ingestion:file-share:rule-1");
            rules[0].Context.ShouldBe("adds-s100");
            rules[1].Provider.ShouldBe("file-share");
            rules[1].RuleId.ShouldBe("subset:rule-2");
            rules[1].Key.ShouldBe("rules:ingestion:file-share:subset:rule-2");
        }

        /// <summary>
        /// Verifies that filtering still searches both rule identifiers and description text after the namespace change.
        /// </summary>
        [Fact]
        public void GetRules_WhenQueryMatchesDescriptionOrRuleId_FiltersNamespaceAwareEntries()
        {
            // Arrange two rules so the test can verify both rule-id and description-based filtering.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["rules:ingestion:file-share:rule-1"] = CreateRuleJson("rule-1", description: "Charts discovery rule"),
                    ["rules:ingestion:file-share:rule-2"] = CreateRuleJson("rule-2", description: "Fisheries rule"),
                })
                .Build();
            var store = new AppConfigRulesSnapshotStore(configuration, new SystemTextJsonRuleJsonValidator(), NullLogger<AppConfigRulesSnapshotStore>.Instance);

            // Filter first by description text and then by the rule identifier itself.
            var descriptionMatches = store.GetRules("discovery");
            var idMatches = store.GetRules("rule-2");

            // Both filtering paths must continue to work after the move to namespace-aware keys.
            descriptionMatches.Select(rule => rule.RuleId).ShouldBe(["rule-1"]);
            idMatches.Select(rule => rule.RuleId).ShouldBe(["rule-2"]);
        }

        /// <summary>
        /// Verifies that invalid rules remain visible in the snapshot with a helpful validation error.
        /// </summary>
        [Fact]
        public void GetRules_WhenTitleMissing_ProjectsInvalidEntry()
        {
            // Arrange a namespace-aware rule missing the required title metadata.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["rules:ingestion:file-share:rule-1"] = "{\"schemaVersion\":\"1.0\",\"rule\":{\"id\":\"rule-1\",\"if\":{\"id\":\"batch-1\"},\"then\":{\"category\":{\"add\":[\"charts\"]}}}}"
                })
                .Build();
            var store = new AppConfigRulesSnapshotStore(configuration, new SystemTextJsonRuleJsonValidator(), NullLogger<AppConfigRulesSnapshotStore>.Instance);

            // Load the snapshot and inspect the projected entry state.
            var rules = store.GetRules(null);

            // The rule should stay visible but be marked invalid with a title-related error.
            rules.ShouldHaveSingleItem();
            rules[0].IsValid.ShouldBeFalse();
            rules[0].ErrorMessage.ShouldContain("title");
        }

        /// <summary>
        /// Verifies that valid in-memory edits are cached beneath the namespace-aware full rule key.
        /// </summary>
        [Fact]
        public void UpdateRuleJson_WhenValidJsonProvided_UsesNamespaceAwareOverrideKey()
        {
            // Arrange an initial rule so the store has a snapshot entry to override.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["rules:ingestion:file-share:subset:rule-1"] = CreateRuleJson("subset:rule-1", title: "Original title")
                })
                .Build();
            var store = new AppConfigRulesSnapshotStore(configuration, new SystemTextJsonRuleJsonValidator(), NullLogger<AppConfigRulesSnapshotStore>.Instance);

            // Update the rule JSON through the same in-memory override path used by the RulesWorkbench editor.
            var result = store.UpdateRuleJson("FILE-SHARE", "subset:rule-1", CreateRuleJson("subset:rule-1", title: "Updated title", description: "Updated description"));
            var updatedRule = store.GetRules(null).ShouldHaveSingleItem();

            // The override should succeed and remain addressable through the namespace-aware full App Configuration key.
            result.IsValid.ShouldBeTrue();
            updatedRule.Key.ShouldBe("rules:ingestion:file-share:subset:rule-1");
            updatedRule.RuleJson?.ToJsonString().ShouldContain("Updated title");
            updatedRule.RuleJson?.ToJsonString().ShouldContain("Updated description");
        }

        /// <summary>
        /// Verifies that invalid edited JSON is rejected before the in-memory override cache is updated.
        /// </summary>
        [Fact]
        public void UpdateRuleJson_WhenTitleMissing_ReturnsValidationError()
        {
            // Arrange a valid initial rule so the update path starts from a healthy snapshot state.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["rules:ingestion:file-share:rule-1"] = CreateRuleJson("rule-1", title: "Rule 1 title")
                })
                .Build();
            var store = new AppConfigRulesSnapshotStore(configuration, new SystemTextJsonRuleJsonValidator(), NullLogger<AppConfigRulesSnapshotStore>.Instance);

            // Attempt to replace the rule with invalid JSON that omits the required title.
            var result = store.UpdateRuleJson("file-share", "rule-1", "{\"id\":\"rule-1\"}");

            // The update should be rejected with the validator's title-related error.
            result.IsValid.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("title");
        }

        /// <summary>
        /// Creates a wrapped rule document that mirrors the checked-in repository rule files.
        /// </summary>
        /// <param name="ruleId">The rule identifier to place in the JSON payload.</param>
        /// <param name="title">The title to emit for the rule.</param>
        /// <param name="context">The optional context to emit when the test needs one.</param>
        /// <param name="description">The optional description to emit when the test needs one.</param>
        /// <returns>A wrapped rule document suitable for App Configuration-backed snapshot tests.</returns>
        private static string CreateRuleJson(string ruleId, string title = "Rule 1 title", string? context = null, string? description = null)
        {
            // Build the optional JSON fragments once so the generated rule documents stay concise and readable.
            var contextJson = context is null ? string.Empty : $",\n        \"context\": \"{context}\"";
            var descriptionJson = description is null ? string.Empty : $",\n        \"description\": \"{description}\"";

            // Return the wrapped rule JSON used by the repository authoring flow and App Configuration seeding path.
            return $$"""
                {
                  "schemaVersion": "1.0",
                  "rule": {
                    "id": "{{ruleId}}",
                    "title": "{{title}}"{{contextJson}}{{descriptionJson}},
                    "if": { "id": "batch-1" },
                    "then": { "category": { "add": [ "charts" ] } }
                  }
                }
                """;
        }
    }
}
