namespace UKHO.Search.Ingestion.Tests.TestSupport
{
    internal sealed class TempRulesRoot : IDisposable
    {
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
            File.WriteAllText(Path.Combine(RootPath, "ingestion-rules.json"), json);
        }
    }
}