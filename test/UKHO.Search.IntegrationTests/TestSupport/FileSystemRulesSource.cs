using System.Text.Json;
using UKHO.Search.Infrastructure.Ingestion.Rules;

namespace UKHO.Search.Ingestion.Tests.TestSupport
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

                    RuleFileDocument? document;
                    try
                    {
                        document = JsonSerializer.Deserialize<RuleFileDocument>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    }
                    catch (JsonException ex)
                    {
                        throw new IngestionRulesValidationException($"Rule file '{filePath}' contains invalid JSON.", innerException: ex);
                    }

                    if (document is null)
                    {
                        throw new IngestionRulesValidationException($"Rule file '{filePath}' could not be parsed.");
                    }

                    if (!string.Equals(document.SchemaVersion, SupportedSchemaVersion, StringComparison.Ordinal))
                    {
                        throw new IngestionRulesValidationException($"Rule file '{filePath}' has unsupported SchemaVersion '{document.SchemaVersion}'. Expected '{SupportedSchemaVersion}'.");
                    }

                    if (document.Rule is null)
                    {
                        throw new IngestionRulesValidationException($"Rule file '{filePath}' does not contain a Rule.");
                    }

                    if (string.IsNullOrWhiteSpace(document.Rule.Id))
                    {
                        throw new IngestionRulesValidationException($"Rule file '{filePath}' contains a rule with missing Id.");
                    }

                    entries.Add(new RuleEntryDto(
                        filePath,
                        providerName,
                        document.Rule.Id,
                        json,
                        IsValid: true,
                        ErrorMessage: null));
                }
            }

            return entries;
        }

        private sealed class RuleFileDocument
        {
            public string? SchemaVersion { get; set; }

            public RuleDocument? Rule { get; set; }
        }

        private sealed class RuleDocument
        {
            public string? Id { get; set; }
        }
    }
}
