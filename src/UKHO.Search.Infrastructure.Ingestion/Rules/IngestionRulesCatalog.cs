using System.Linq;
using Microsoft.Extensions.Logging;
using UKHO.Search.Infrastructure.Ingestion.Rules.Validation;

namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    internal sealed class IngestionRulesCatalog : IIngestionRulesCatalog
    {
        private readonly ILogger<IngestionRulesCatalog> _logger;
        private readonly IngestionRulesLoader _loader;
        private readonly IngestionRulesValidator _validator;

        private ValidatedRuleset? _ruleset;

        public IngestionRulesCatalog(IngestionRulesLoader loader, IngestionRulesValidator validator, ILogger<IngestionRulesCatalog> logger)
        {
            _loader = loader;
            _validator = validator;
            _logger = logger;
        }

        public void EnsureLoaded()
        {
            _ = GetOrLoadRuleset();
        }

        public IReadOnlyDictionary<string, IReadOnlyList<string>> GetRuleIdsByProvider()
        {
            var ruleset = GetOrLoadRuleset();
            return ruleset.RulesByProvider.ToDictionary(
                k => k.Key,
                v => (IReadOnlyList<string>)v.Value.Select(r => r.Id).ToArray(),
                StringComparer.OrdinalIgnoreCase);
        }

        internal bool TryGetProviderRules(string providerName, out IReadOnlyList<ValidatedRule> rules)
        {
            if (string.IsNullOrWhiteSpace(providerName))
            {
                rules = Array.Empty<ValidatedRule>();
                return false;
            }

            var ruleset = GetOrLoadRuleset();
            return ruleset.RulesByProvider.TryGetValue(providerName, out rules!);
        }

        private ValidatedRuleset GetOrLoadRuleset()
        {
            _ruleset ??= LoadAndValidate();
            return _ruleset;
        }

        private ValidatedRuleset LoadAndValidate()
        {
            var dto = _loader.Load();
            var ruleset = _validator.Validate(dto);

            _logger.LogInformation("Loaded ingestion rules. ProviderCount={ProviderCount}", ruleset.RulesByProvider.Count);
            return ruleset;
        }
    }
}
