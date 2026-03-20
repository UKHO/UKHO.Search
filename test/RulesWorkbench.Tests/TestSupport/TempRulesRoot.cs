namespace RulesWorkbench.Tests.TestSupport
{
    internal sealed class TempRulesRoot : IDisposable
    {
        private const string RulesRootDirectoryName = "Rules";

        public TempRulesRoot()
        {
            Directory.CreateDirectory(RootPath);
        }

        public string RootPath { get; } = Path.Combine(Path.GetTempPath(), $"rules-workbench-tests-{Guid.NewGuid():N}");

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, true);
            }
        }

        public void WriteRuleFile(string providerName, string ruleId, string ruleJson)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
            ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);
            ArgumentException.ThrowIfNullOrWhiteSpace(ruleJson);

            var providerRoot = Path.Combine(RootPath, RulesRootDirectoryName, providerName);
            Directory.CreateDirectory(providerRoot);
            File.WriteAllText(Path.Combine(providerRoot, $"{ruleId}.json"), ruleJson);
        }
    }
}
