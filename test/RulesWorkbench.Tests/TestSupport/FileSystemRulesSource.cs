using System.Text.Json;
using UKHO.Search.Infrastructure.Ingestion.Rules;

namespace RulesWorkbench.Tests.TestSupport
{
    internal sealed class FileSystemRulesSource : IRulesSource
    {
        private const string RulesRootDirectoryName = "Rules";
        private const string DefaultProviderName = "file-share";
        private const string SupportedSchemaVersion = "1.0";

        private readonly string _contentRootPath;

        public FileSystemRulesSource(string contentRootPath)
        {
            _contentRootPath = contentRootPath;
        }

        public IReadOnlyList<RuleEntryDto> ListRuleEntries()
        {
            var rulesRootPath = Path.Combine(_contentRootPath, RulesRootDirectoryName);
            var defaultProviderPath = Path.Combine(rulesRootPath, DefaultProviderName);

            if (!Directory.Exists(rulesRootPath))
            {
                throw new IngestionRulesValidationException($"Missing required rules directory '{RulesRootDirectoryName}' for provider '{DefaultProviderName}' at '{defaultProviderPath}'.");
            }

            var providerDirectories = Directory.EnumerateDirectories(rulesRootPath)
                                             .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                                             .ToArray();

            var entries = new List<RuleEntryDto>();

            foreach (var providerDirectory in providerDirectories)
            {
                var providerName = Path.GetFileName(providerDirectory);
                var jsonFiles = Directory.EnumerateFiles(providerDirectory, "*.json", SearchOption.AllDirectories)
                                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

                foreach (var filePath in jsonFiles)
                {
                    var json = File.ReadAllText(filePath);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        throw new IngestionRulesValidationException($"Rule file '{filePath}' is empty.");
                    }

                    JsonDocument document;
                    try
                    {
                        document = JsonDocument.Parse(json);
                    }
                    catch (JsonException ex)
                    {
                        throw new IngestionRulesValidationException($"Rule file '{filePath}' contains invalid JSON.", innerException: ex);
                    }

                    if (!document.RootElement.TryGetProperty("schemaVersion", out var schemaVersionElement))
                    {
                        throw new IngestionRulesValidationException($"Rule file '{filePath}' is missing SchemaVersion.");
                    }

                    var schemaVersion = schemaVersionElement.GetString();
                    if (!string.Equals(schemaVersion, SupportedSchemaVersion, StringComparison.Ordinal))
                    {
                        throw new IngestionRulesValidationException($"Rule file '{filePath}' has unsupported SchemaVersion '{schemaVersion}'. Expected '{SupportedSchemaVersion}'.");
                    }

                    if (!document.RootElement.TryGetProperty("rule", out var ruleElement) || ruleElement.ValueKind != JsonValueKind.Object)
                    {
                        throw new IngestionRulesValidationException($"Rule file '{filePath}' does not contain a Rule.");
                    }

                    if (!ruleElement.TryGetProperty("id", out var ruleIdElement) || string.IsNullOrWhiteSpace(ruleIdElement.GetString()))
                    {
                        throw new IngestionRulesValidationException($"Rule file '{filePath}' contains a rule with missing Id.");
                    }

                    var ruleId = ruleIdElement.GetString()!;

                    entries.Add(new RuleEntryDto(
                        filePath,
                        providerName,
                        ruleId,
                        json,
                        IsValid: true,
                        ErrorMessage: null));

                    document.Dispose();
                }
            }

            return entries;
        }
    }
}
