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
            ex.Message.ShouldContain("Missing required rules file");
        }

        [Fact]
        public void Invalid_json_fails_startup_validation()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRulesFile("{");

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
            temp.WriteRulesFile("""
                                {
                                  "rules": {
                                    "file-share": [
                                      {
                                        "id": "r1",
                                        "if": { "any": [ { "path": "id", "exists": true } ] },
                                        "then": { "keywords": { "add": [ "k" ] } }
                                      }
                                    ]
                                  }
                                }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var catalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            var ex = Should.Throw<IngestionRulesValidationException>(() => catalog.EnsureLoaded());
            ex.Errors.ShouldContain(x => x.Contains("schemaVersion", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Unsupported_schema_version_fails_validation()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRulesFile("""
                                {
                                  "schemaVersion": "2.0",
                                  "rules": {
                                    "file-share": [
                                      {
                                        "id": "r1",
                                        "if": { "any": [ { "path": "id", "exists": true } ] },
                                        "then": { "keywords": { "add": [ "k" ] } }
                                      }
                                    ]
                                  }
                                }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var catalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            var ex = Should.Throw<IngestionRulesValidationException>(() => catalog.EnsureLoaded());
            ex.Errors.ShouldContain(x => x.Contains("Unsupported schemaVersion", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Empty_ruleset_fails_validation()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRulesFile("""
                                {
                                  "schemaVersion": "1.0",
                                  "rules": { }
                                }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var catalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            var ex = Should.Throw<IngestionRulesValidationException>(() => catalog.EnsureLoaded());
            ex.Errors.ShouldContain(x => x.Contains("non-empty rule array", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Invalid_predicate_shapes_fail_validation()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRulesFile("""
                                {
                                  "schemaVersion": "1.0",
                                  "rules": {
                                    "file-share": [
                                      { "id": "all-empty", "if": { "all": [ ] }, "then": { "keywords": { "add": [ "k" ] } } },
                                      { "id": "not-array", "if": { "not": [ ] }, "then": { "keywords": { "add": [ "k" ] } } },
                                      { "id": "multi", "if": { "all": [ { "path": "id", "exists": true } ], "any": [ { "path": "id", "exists": true } ] }, "then": { "keywords": { "add": [ "k" ] } } }
                                    ]
                                  }
                                }
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
            temp.WriteRulesFile("""
                                {
                                  "schemaVersion": "1.0",
                                  "rules": {
                                    "file-share": [
                                      { "id": "missing-wildcard", "if": { "any": [ { "path": "files.mimeType", "exists": true } ] }, "then": { "keywords": { "add": [ "k" ] } } },
                                      { "id": "numeric-index", "if": { "any": [ { "path": "files[0].mimeType", "exists": true } ] }, "then": { "keywords": { "add": [ "k" ] } } },
                                      { "id": "selector", "if": { "any": [ { "path": "files[name=\"a\"].mimeType", "exists": true } ] }, "then": { "keywords": { "add": [ "k" ] } } }
                                    ]
                                  }
                                }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var catalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            var ex = Should.Throw<IngestionRulesValidationException>(() => catalog.EnsureLoaded());
            ex.Errors.ShouldContain(x => x.Contains("[*]", StringComparison.OrdinalIgnoreCase) || x.Contains("explicit", StringComparison.OrdinalIgnoreCase));
            ex.Errors.ShouldContain(x => x.Contains("Unsupported selector", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Facets_add_value_and_values_together_fails_validation()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRulesFile("""
                                {
                                  "schemaVersion": "1.0",
                                  "rules": {
                                    "file-share": [
                                      {
                                        "id": "facet-bad",
                                        "if": { "any": [ { "path": "id", "exists": true } ] },
                                        "then": {
                                          "facets": {
                                            "add": [
                                              { "name": "facet1", "value": "a", "values": [ "b" ] }
                                            ]
                                          }
                                        }
                                      }
                                    ]
                                  }
                                }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var catalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            var ex = Should.Throw<IngestionRulesValidationException>(() => catalog.EnsureLoaded());
            ex.Errors.ShouldContain(x => x.Contains("value", StringComparison.OrdinalIgnoreCase) && x.Contains("values", StringComparison.OrdinalIgnoreCase));
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