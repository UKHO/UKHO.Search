using Microsoft.Extensions.Logging.Abstractions;
using UKHO.Search.Infrastructure.Ingestion.Rules;
using UKHO.Search.Infrastructure.Ingestion.Rules.Model;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Rules
{
    public sealed class RuleFileLoaderTests
    {
        private readonly RuleFileLoader _loader = new(new NullLogger<RuleFileLoader>());

        [Fact]
        public void LoadProviderRules_WhenProviderDirectoryMissing_Throws()
        {
            var contentRoot = CreateTempDir();

            var ex = Assert.Throws<IngestionRulesValidationException>(() => _loader.LoadProviderRules(contentRoot, "file-share"));
            Assert.Contains("Missing required rules directory", ex.Message);
        }

        [Fact]
        public void LoadProviderRules_LoadsRecursivelyAndDeterministicallyOrdersByPathThenRuleId()
        {
            var contentRoot = CreateTempDir();
            var providerRoot = Directory.CreateDirectory(Path.Combine(contentRoot, "Rules", "file-share"));
            Directory.CreateDirectory(Path.Combine(providerRoot.FullName, "Z"));
            Directory.CreateDirectory(Path.Combine(providerRoot.FullName, "A"));

            WriteRule(Path.Combine(providerRoot.FullName, "Z", "b.json"), "b");
            WriteRule(Path.Combine(providerRoot.FullName, "A", "a.json"), "a");

            var rules = _loader.LoadProviderRules(contentRoot, "file-share");

            Assert.Equal(2, rules.Count);
            Assert.Equal("a", rules[0].Id);
            Assert.Equal("b", rules[1].Id);
        }

        [Fact]
        public void LoadProviderRules_WhenInvalidJson_Throws()
        {
            var contentRoot = CreateTempDir();
            var providerRoot = Directory.CreateDirectory(Path.Combine(contentRoot, "Rules", "file-share"));
            File.WriteAllText(Path.Combine(providerRoot.FullName, "bad.json"), "{");

            var ex = Assert.Throws<IngestionRulesValidationException>(() => _loader.LoadProviderRules(contentRoot, "file-share"));
            Assert.Contains("invalid JSON", ex.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("0.9")]
        public void LoadProviderRules_WhenSchemaVersionMissingOrUnsupported_Throws(string? schemaVersion)
        {
            var contentRoot = CreateTempDir();
            var providerRoot = Directory.CreateDirectory(Path.Combine(contentRoot, "Rules", "file-share"));

            var schemaJson = schemaVersion is null ? "null" : $"\"{schemaVersion}\"";
            var json = $"{{\"SchemaVersion\":{schemaJson},\"Rule\":{{\"Id\":\"a\"}}}}";
            File.WriteAllText(Path.Combine(providerRoot.FullName, "a.json"), json);

            var ex = Assert.Throws<IngestionRulesValidationException>(() => _loader.LoadProviderRules(contentRoot, "file-share"));
            Assert.Contains("SchemaVersion", ex.Message);
        }

        [Fact]
        public void LoadProviderRules_WhenDuplicateRuleIds_ThrowsDuplicateException()
        {
            var contentRoot = CreateTempDir();
            var providerRoot = Directory.CreateDirectory(Path.Combine(contentRoot, "Rules", "file-share"));
            Directory.CreateDirectory(Path.Combine(providerRoot.FullName, "sub"));

            WriteRule(Path.Combine(providerRoot.FullName, "a.json"), "dup");
            WriteRule(Path.Combine(providerRoot.FullName, "sub", "b.json"), "dup");

            var ex = Assert.Throws<RulesDuplicateRuleIdException>(() => _loader.LoadProviderRules(contentRoot, "file-share"));
            Assert.Equal("dup", ex.RuleId);
            Assert.Equal(2, ex.FilePaths.Count);
        }

        private static void WriteRule(string path, string id)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, $"{{\"SchemaVersion\":\"{RuleFileLoader.SupportedSchemaVersion}\",\"Rule\":{{\"Id\":\"{id}\"}}}}");
        }

        private static string CreateTempDir()
        {
            var path = Path.Combine(Path.GetTempPath(), "ukho-search-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
