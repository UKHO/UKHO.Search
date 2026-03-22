using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Injection;
using UKHO.Search.Infrastructure.Ingestion.Rules;
using UKHO.Search.Ingestion.Tests.TestSupport;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Rules
{
    public sealed class RulesetValidationTests
    {
        [Fact]
        public void Missing_rules_file_fails_startup_validation()
        {
            using var temp = new TempRulesRoot();

            using var provider = CreateProvider(temp.RootPath);
            var catalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            var ex = Should.Throw<IngestionRulesValidationException>(() => catalog.EnsureLoaded());
            ex.Message.ShouldContain("Missing required rules directory");
        }

        [Fact]
        public void Invalid_json_fails_startup_validation()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRuleFile("file-share", "bad", "{");

            using var provider = CreateProvider(temp.RootPath);
            var catalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            var ex = Should.Throw<IngestionRulesValidationException>(() => catalog.EnsureLoaded());
            ex.Message.ShouldContain("invalid JSON");
            ex.InnerException.ShouldNotBeNull();
        }

        [Fact]
        public void Missing_schema_version_fails_validation()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRuleFile("file-share", "r1", """
                                {
                                  "rule": {
                                    "id": "r1",
                                    "title": "Rule 1",
                                    "if": { "any": [ { "path": "id", "exists": true } ] },
                                    "then": { "keywords": { "add": [ "k" ] } }
                                  }
                                }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var catalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            var ex = Should.Throw<IngestionRulesValidationException>(() => catalog.EnsureLoaded());
            ex.Message.ToLowerInvariant().ShouldContain("schemaversion");
        }

        [Fact]
        public void Unsupported_schema_version_fails_validation()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRuleFile("file-share", "r1", """
                                {
                                  "schemaVersion": "2.0",
                                  "rule": {
                                    "id": "r1",
                                    "title": "Rule 1",
                                    "if": { "any": [ { "path": "id", "exists": true } ] },
                                    "then": { "keywords": { "add": [ "k" ] } }
                                  }
                                }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var catalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            var ex = Should.Throw<IngestionRulesValidationException>(() => catalog.EnsureLoaded());
            ex.Message.ToLowerInvariant().ShouldContain("unsupported");
            ex.Message.ToLowerInvariant().ShouldContain("schemaversion");
        }

        [Fact]
        public void Empty_ruleset_fails_validation()
        {
            using var temp = new TempRulesRoot();
            // Ensure the Rules directory exists but contains no provider directories/rules.
            Directory.CreateDirectory(Path.Combine(temp.RootPath, "Rules"));

            using var provider = CreateProvider(temp.RootPath);
            var catalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            var ex = Should.Throw<IngestionRulesValidationException>(() => catalog.EnsureLoaded());
            ex.Errors.ShouldContain(x => x.Contains("non-empty rule array", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Invalid_predicate_shapes_fail_validation()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRuleFile("file-share", "all-empty", """
                                { "schemaVersion": "1.0", "rule": { "id": "all-empty", "title": "All empty", "if": { "all": [ ] }, "then": { "keywords": { "add": [ "k" ] } } } }
                                """);
            temp.WriteRuleFile("file-share", "not-array", """
                                { "schemaVersion": "1.0", "rule": { "id": "not-array", "title": "Not array", "if": { "not": [ ] }, "then": { "keywords": { "add": [ "k" ] } } } }
                                """);
            temp.WriteRuleFile("file-share", "multi", """
                                { "schemaVersion": "1.0", "rule": { "id": "multi", "title": "Multi", "if": { "all": [ { "path": "id", "exists": true } ], "any": [ { "path": "id", "exists": true } ] }, "then": { "keywords": { "add": [ "k" ] } } } }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var catalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            var ex = Should.Throw<IngestionRulesValidationException>(() => catalog.EnsureLoaded());
            ex.Errors.ShouldContain(x => x.Contains("non-empty", StringComparison.OrdinalIgnoreCase));
            ex.Errors.ShouldContain(x => x.Contains("not", StringComparison.OrdinalIgnoreCase));
            ex.Errors.ShouldContain(x => x.Contains("exactly one", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Invalid_path_syntax_fails_validation()
        {
            using var temp = new TempRulesRoot();

            temp.WriteRuleFile("file-share", "missing-wildcard", """
                                { "schemaVersion": "1.0", "rule": { "id": "missing-wildcard", "title": "Missing wildcard", "if": { "any": [ { "path": "files.mimeType", "exists": true } ] }, "then": { "keywords": { "add": [ "k" ] } } } }
                                """);

            temp.WriteRuleFile("file-share", "numeric-index", """
                                { "schemaVersion": "1.0", "rule": { "id": "numeric-index", "title": "Numeric index", "if": { "any": [ { "path": "files[0].mimeType", "exists": true } ] }, "then": { "keywords": { "add": [ "k" ] } } } }
                                """);

            temp.WriteRuleFile("file-share", "selector", """
                                { "schemaVersion": "1.0", "rule": { "id": "selector", "title": "Selector", "if": { "any": [ { "path": "files[name=\"a\"].mimeType", "exists": true } ] }, "then": { "keywords": { "add": [ "k" ] } } } }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var catalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            var ex = Should.Throw<IngestionRulesValidationException>(() => catalog.EnsureLoaded());
            ex.Errors.ShouldContain(x => x.Contains("[*]", StringComparison.OrdinalIgnoreCase) || x.Contains("explicit", StringComparison.OrdinalIgnoreCase));
            ex.Errors.ShouldContain(x => x.Contains("Unsupported selector", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void File_share_rules_without_context_remain_valid_during_transitional_uplift()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRuleFile("file-share", "r1", """
                                {
                                  "schemaVersion": "1.0",
                                  "rule": {
                                    "id": "r1",
                                    "title": "Rule 1",
                                    "if": { "path": "id", "exists": true },
                                    "then": { "keywords": { "add": [ "k" ] } }
                                  }
                                }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var catalog = provider.GetRequiredService<IngestionRulesCatalog>();

            catalog.EnsureLoaded();

            catalog.TryGetProviderRules("file-share", out var rules).ShouldBeTrue();
            rules.ShouldHaveSingleItem();
            rules[0].Context.ShouldBeNull();
        }

        [Fact]
        public void File_share_rules_with_context_are_projected_into_validated_rules()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRuleFile("file-share", "r1", """
                                {
                                  "schemaVersion": "1.0",
                                  "rule": {
                                    "id": "r1",
                                    "context": "adds-s100",
                                    "title": "Rule 1",
                                    "if": { "path": "id", "exists": true },
                                    "then": { "keywords": { "add": [ "k" ] } }
                                  }
                                }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var catalog = provider.GetRequiredService<IngestionRulesCatalog>();

            catalog.EnsureLoaded();

            catalog.TryGetProviderRules("file-share", out var rules).ShouldBeTrue();
            rules.ShouldHaveSingleItem();
            rules[0].Context.ShouldBe("adds-s100");
        }

        [Fact]
        public void File_share_rules_fail_validation_when_context_is_missing_from_a_partially_uplifted_ruleset()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRuleFile("file-share", "r1", """
                                {
                                  "schemaVersion": "1.0",
                                  "rule": {
                                    "id": "r1",
                                    "context": "adds-s100",
                                    "title": "Rule 1",
                                    "if": { "path": "id", "exists": true },
                                    "then": { "keywords": { "add": [ "k" ] } }
                                  }
                                }
                                """);
            temp.WriteRuleFile("file-share", "r2", """
                                {
                                  "schemaVersion": "1.0",
                                  "rule": {
                                    "id": "r2",
                                    "title": "Rule 2",
                                    "if": { "path": "id", "exists": true },
                                    "then": { "keywords": { "add": [ "k2" ] } }
                                  }
                                }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var catalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            var ex = Should.Throw<IngestionRulesValidationException>(() => catalog.EnsureLoaded());
            ex.Errors.ShouldContain(x => x.Contains("context", StringComparison.OrdinalIgnoreCase));
            ex.Errors.ShouldContain(x => x.Contains("r2", StringComparison.OrdinalIgnoreCase));
        }

        private static ServiceProvider CreateProvider(string contentRootPath)
        {
            return IngestionRulesTestServiceProviderFactory.Create(contentRootPath);
        }
    }
}