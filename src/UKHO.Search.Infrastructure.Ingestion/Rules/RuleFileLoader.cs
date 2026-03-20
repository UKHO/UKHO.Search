using System.Text.Json;
using Microsoft.Extensions.Logging;
using UKHO.Search.Infrastructure.Ingestion.Rules.Model;

namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    internal sealed class RuleFileLoader
    {
        internal const string RulesRootDirectoryName = "Rules";
        internal const string SupportedSchemaVersion = "1.0";

        private readonly ILogger<RuleFileLoader> _logger;

        public RuleFileLoader(ILogger<RuleFileLoader> logger)
        {
            _logger = logger;
        }

        public IReadOnlyList<RuleDto> LoadProviderRules(string contentRootPath, string providerName)
        {
            if (string.IsNullOrWhiteSpace(contentRootPath))
            {
                throw new ArgumentException("Content root path is required.", nameof(contentRootPath));
            }

            if (string.IsNullOrWhiteSpace(providerName))
            {
                throw new ArgumentException("Provider name is required.", nameof(providerName));
            }

            var rulesRootPath = Path.Combine(contentRootPath, RulesRootDirectoryName);
            var providerRootPath = Path.Combine(rulesRootPath, providerName);

            if (!Directory.Exists(providerRootPath))
            {
                throw new IngestionRulesValidationException($"Missing required rules directory '{RulesRootDirectoryName}' for provider '{providerName}' at '{providerRootPath}'.");
            }

            var jsonFiles = Directory.EnumerateFiles(providerRootPath, "*.json", SearchOption.AllDirectories)
                                     .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                                     .ToArray();

            _logger.LogInformation("Scanning rule files. Provider={Provider} ProviderRoot={ProviderRoot} FileCount={FileCount}", providerName, providerRootPath, jsonFiles.Length);

            var loaded = new List<(string FilePath, RuleDto Rule)>(jsonFiles.Length);

            foreach (var filePath in jsonFiles)
            {
                var json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    throw new IngestionRulesValidationException($"Rule file '{filePath}' is empty.");
                }

                RuleFileDocumentDto? doc;
                try
                {
                    doc = JsonSerializer.Deserialize<RuleFileDocumentDto>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (JsonException ex)
                {
                    throw new IngestionRulesValidationException($"Rule file '{filePath}' contains invalid JSON.", innerException: ex);
                }

                if (doc is null)
                {
                    throw new IngestionRulesValidationException($"Rule file '{filePath}' could not be parsed.");
                }

                if (!string.Equals(doc.SchemaVersion, SupportedSchemaVersion, StringComparison.Ordinal))
                {
                    throw new IngestionRulesValidationException($"Rule file '{filePath}' has unsupported SchemaVersion '{doc.SchemaVersion}'. Expected '{SupportedSchemaVersion}'.");
                }

                if (doc.Rule is null)
                {
                    throw new IngestionRulesValidationException($"Rule file '{filePath}' does not contain a Rule.");
                }

                if (string.IsNullOrWhiteSpace(doc.Rule.Id))
                {
                    throw new IngestionRulesValidationException($"Rule file '{filePath}' contains a rule with missing Id.");
                }

                if (string.IsNullOrWhiteSpace(doc.Rule.Title))
                {
                    throw new IngestionRulesValidationException($"Rule file '{filePath}' contains a rule with missing required Title.");
                }

                loaded.Add((filePath, doc.Rule));
            }

            var duplicateIds = loaded.GroupBy(r => r.Rule.Id!, StringComparer.OrdinalIgnoreCase)
                                     .Where(g => g.Count() > 1)
                                     .Select(g => new { RuleId = g.Key, Paths = g.Select(x => x.FilePath).ToArray() })
                                     .ToArray();

            if (duplicateIds.Length > 0)
            {
                var first = duplicateIds[0];
                throw new RulesDuplicateRuleIdException(first.RuleId, first.Paths);
            }

            var ordered = loaded.OrderBy(r => r.FilePath, StringComparer.OrdinalIgnoreCase)
                                .ThenBy(r => r.Rule.Id, StringComparer.OrdinalIgnoreCase)
                                .Select(r => r.Rule)
                                .ToArray();

            _logger.LogInformation("Loaded provider rules. Provider={Provider} RuleCount={RuleCount}", providerName, ordered.Length);

            return ordered;
        }
    }
}
