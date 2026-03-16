using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
                                { "schemaVersion": "1.0", "rule": { "id": "all-empty", "if": { "all": [ ] }, "then": { "keywords": { "add": [ "k" ] } } } }
                                """);
            temp.WriteRuleFile("file-share", "not-array", """
                                { "schemaVersion": "1.0", "rule": { "id": "not-array", "if": { "not": [ ] }, "then": { "keywords": { "add": [ "k" ] } } } }
                                """);
            temp.WriteRuleFile("file-share", "multi", """
                                { "schemaVersion": "1.0", "rule": { "id": "multi", "if": { "all": [ { "path": "id", "exists": true } ], "any": [ { "path": "id", "exists": true } ] }, "then": { "keywords": { "add": [ "k" ] } } } }
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
                                { "schemaVersion": "1.0", "rule": { "id": "missing-wildcard", "if": { "any": [ { "path": "files.mimeType", "exists": true } ] }, "then": { "keywords": { "add": [ "k" ] } } } }
                                """);

            temp.WriteRuleFile("file-share", "numeric-index", """
                                { "schemaVersion": "1.0", "rule": { "id": "numeric-index", "if": { "any": [ { "path": "files[0].mimeType", "exists": true } ] }, "then": { "keywords": { "add": [ "k" ] } } } }
                                """);

            temp.WriteRuleFile("file-share", "selector", """
                                { "schemaVersion": "1.0", "rule": { "id": "selector", "if": { "any": [ { "path": "files[name=\"a\"].mimeType", "exists": true } ] }, "then": { "keywords": { "add": [ "k" ] } } } }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var catalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            var ex = Should.Throw<IngestionRulesValidationException>(() => catalog.EnsureLoaded());
            ex.Errors.ShouldContain(x => x.Contains("[*]", StringComparison.OrdinalIgnoreCase) || x.Contains("explicit", StringComparison.OrdinalIgnoreCase));
            ex.Errors.ShouldContain(x => x.Contains("Unsupported selector", StringComparison.OrdinalIgnoreCase));
        }

        private static ServiceProvider CreateProvider(string contentRootPath)
        {
            var services = new ServiceCollection();

            services.AddSingleton<IHostEnvironment>(new TestHostEnvironment { ContentRootPath = contentRootPath });
            services.AddLogging(b => b.SetMinimumLevel(LogLevel.Debug));
            services.AddIngestionServices();

            return services.BuildServiceProvider();
        }
    }
}