using System.Text.Json;

using Microsoft.Extensions.Logging;
using UKHO.Search.Infrastructure.Ingestion.Rules.Model;
using UKHO.Search.Infrastructure.Ingestion.Rules.Validation;

namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    internal sealed class IngestionRulesSource
    {
        private readonly IRulesSource _rulesSource;
        private readonly IngestionRulesValidator _validator;
        private readonly ILogger<IngestionRulesSource> _logger;

        public IngestionRulesSource(IRulesSource rulesSource, IngestionRulesValidator validator, ILogger<IngestionRulesSource> logger)
        {
            _rulesSource = rulesSource;
            _validator = validator;
            _logger = logger;
        }

        public RulesetDto LoadStrict()
        {
            var entries = _rulesSource.ListRuleEntries();

            var providerRules = new Dictionary<string, List<RuleDto>>(StringComparer.OrdinalIgnoreCase);
            var rejectedCount = 0;

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Json))
                {
                    rejectedCount++;
                    _logger.LogWarning("Rejected rule with empty value. Key={Key}", entry.Key);
                    continue;
                }

                RuleDto? rule;
                try
                {
                    var json = UnwrapRuleJson(entry.Json);
                    rule = JsonSerializer.Deserialize<RuleDto>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (JsonException ex)
                {
                    rejectedCount++;
                    _logger.LogWarning(ex, "Rejected rule with invalid JSON. Key={Key}", entry.Key);
                    continue;
                }

                if (rule is null)
                {
                    rejectedCount++;
                    _logger.LogWarning("Rejected rule with null document after deserialization. Key={Key}", entry.Key);
                    continue;
                }

                rule.Id = entry.RuleId;

                providerRules.TryAdd(entry.Provider, new List<RuleDto>());
                providerRules[entry.Provider].Add(rule);
            }

            if (providerRules.Count == 0)
            {
                throw new IngestionRulesValidationException("No ingestion rules found in configuration under 'rules:'.");
            }

            var dto = new RulesetDto
            {
                SchemaVersion = IngestionRulesValidator.SupportedSchemaVersion,
                Rules = providerRules.ToDictionary(k => k.Key, v => v.Value.ToArray(), StringComparer.OrdinalIgnoreCase)
            };

            _ = _validator.Validate(dto);

            var providerCounts = dto.Rules?.ToDictionary(k => k.Key, v => v.Value.Length, StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            _logger.LogInformation(
                "Loaded ingestion rules from App Config. ProviderCount={ProviderCount} RejectedCount={RejectedCount} ProviderRuleCounts={ProviderRuleCounts}",
                providerCounts.Count,
                rejectedCount,
                providerCounts);

            return dto;
        }

        private static string UnwrapRuleJson(string json)
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return json;
            }

            // Support rule file document shape: { "schemaVersion": "1.0", "rule": { ... } }
            if (document.RootElement.TryGetProperty("rule", out var ruleElement)
                || document.RootElement.TryGetProperty("Rule", out ruleElement))
            {
                if (ruleElement.ValueKind == JsonValueKind.Object)
                {
                    return ruleElement.GetRawText();
                }
            }

            return json;
        }
    }
}
