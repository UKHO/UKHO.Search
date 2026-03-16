namespace UKHO.Search.Ingestion.Tests.TestSupport
{
    internal sealed class TempRulesRoot : IDisposable
    {
        private const string RulesRootDirectoryName = "Rules";

        public TempRulesRoot()
        {
            Directory.CreateDirectory(RootPath);
        }

        public string RootPath { get; } = Path.Combine(Path.GetTempPath(), $"ukho-search-rules-{Guid.NewGuid():N}");

        public void Dispose()
        {
            Directory.Delete(RootPath, true);
        }

        public void WriteRulesFile(string json)
        {
            // Back-compat helper name: now writes to the new per-rule directory layout.
            WriteRuleFile(providerName: "file-share", ruleId: "rule-1", ruleJson: json);
        }

        public void WriteRuleFile(string providerName, string ruleId, string ruleJson)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
            ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);
            ArgumentException.ThrowIfNullOrWhiteSpace(ruleJson);

            var providerRoot = Path.Combine(RootPath, RulesRootDirectoryName, providerName);
            Directory.CreateDirectory(providerRoot);

            var filePath = Path.Combine(providerRoot, $"{ruleId}.json");
            File.WriteAllText(filePath, ruleJson);
        }
    }
}