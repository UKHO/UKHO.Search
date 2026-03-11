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
    public sealed class DocumentTypeSetValidationTests
    {
        [Fact]
        public void DocumentType_set_rejects_path_variable_with_wildcard()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRulesFile("""
                                {
                                  "schemaVersion": "1.0",
                                  "rules": {
                                    "file-share": [
                                      {
                                        "id": "docType-wildcard-path",
                                        "if": { "id": "doc-1" },
                                        "then": { "documentType": { "set": "$path:files[*].mimeType" } }
                                      }
                                    ]
                                  }
                                }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var catalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            var ex = Should.Throw<IngestionRulesValidationException>(() => catalog.EnsureLoaded());
            ex.Errors.ShouldContain(x => x.Contains("documentType", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void DocumentType_set_rejects_val_when_predicate_may_produce_multiple_values_due_to_wildcard()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRulesFile("""
                                {
                                  "schemaVersion": "1.0",
                                  "rules": {
                                    "file-share": [
                                      {
                                        "id": "docType-val-wildcard",
                                        "if": { "files[*].mimeType": "app/s63" },
                                        "then": { "documentType": { "set": "$val" } }
                                      }
                                    ]
                                  }
                                }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var catalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            var ex = Should.Throw<IngestionRulesValidationException>(() => catalog.EnsureLoaded());
            ex.Errors.ShouldContain(x => x.Contains("$val", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void DocumentType_set_rejects_val_when_predicate_has_multiple_leaves()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRulesFile("""
                                {
                                  "schemaVersion": "1.0",
                                  "rules": {
                                    "file-share": [
                                      {
                                        "id": "docType-val-multi-leaf",
                                        "if": { "id": "doc-1", "properties[\"abcdef\"]": "a value" },
                                        "then": { "documentType": { "set": "$val" } }
                                      }
                                    ]
                                  }
                                }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var catalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            var ex = Should.Throw<IngestionRulesValidationException>(() => catalog.EnsureLoaded());
            ex.Errors.ShouldContain(x => x.Contains("$val", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void DocumentType_set_allows_val_when_predicate_is_single_scalar_leaf()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRulesFile("""
                                {
                                  "schemaVersion": "1.0",
                                  "rules": {
                                    "file-share": [
                                      {
                                        "id": "docType-val-single-leaf",
                                        "if": { "properties[\"abcdef\"]": "a value" },
                                        "then": { "documentType": { "set": "$val" } }
                                      }
                                    ]
                                  }
                                }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var catalog = provider.GetRequiredService<IIngestionRulesCatalog>();

            catalog.EnsureLoaded();
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