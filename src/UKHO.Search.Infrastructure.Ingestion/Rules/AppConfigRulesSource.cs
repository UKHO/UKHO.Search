using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    internal sealed class AppConfigRulesSource : IRulesSource
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AppConfigRulesSource> _logger;

        public AppConfigRulesSource(IConfiguration configuration, ILogger<AppConfigRulesSource> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public IReadOnlyList<RuleEntryDto> ListRuleEntries()
        {
            var result = new List<RuleEntryDto>();

            var rulesRoot = _configuration.GetSection("rules");
            foreach (var providerSection in rulesRoot.GetChildren())
            {
                var provider = providerSection.Key;
                if (string.IsNullOrWhiteSpace(provider))
                {
                    continue;
                }

                foreach (var ruleSection in providerSection.GetChildren())
                {
                    var ruleId = ruleSection.Key;
                    if (string.IsNullOrWhiteSpace(ruleId))
                    {
                        continue;
                    }

                    var key = $"rules:{provider}:{ruleId}";
                    result.Add(new RuleEntryDto(
                        key,
                        provider,
                        ruleId,
                        ruleSection.Value,
                        IsValid: true,
                        ErrorMessage: null));
                }
            }

            _logger.LogInformation("Listed App Config rules. ProviderCount={ProviderCount} RuleCount={RuleCount}",
                result.Select(r => r.Provider).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                result.Count);

            return result;
        }
    }
}
