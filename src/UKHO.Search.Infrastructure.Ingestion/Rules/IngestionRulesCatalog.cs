using Microsoft.Extensions.Logging;
using UKHO.Search.Infrastructure.Ingestion.Rules.Validation;

namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    internal sealed class IngestionRulesCatalog : IIngestionRulesCatalog, IProviderRulesReader
    {
        private readonly IngestionRulesLoader _loader;
        private readonly ILogger<IngestionRulesCatalog> _logger;
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

        internal void Reload()
        {
            var next = LoadAndValidate();
            Interlocked.Exchange(ref _ruleset, next);
        }

        public IReadOnlyDictionary<string, IReadOnlyList<string>> GetRuleIdsByProvider()
        {
            var ruleset = GetOrLoadRuleset();
            return ruleset.RulesByProvider.ToDictionary(k => k.Key, v => (IReadOnlyList<string>)v.Value.Select(r => r.Id)
                                                                                                 .ToArray(), StringComparer.OrdinalIgnoreCase);
        }

        public ProviderRulesSnapshot GetSnapshot()
        {
            var ruleset = GetOrLoadRuleset();

            return new ProviderRulesSnapshot
            {
                SchemaVersion = ruleset.SchemaVersion,
                RulesByProvider = ruleset.RulesByProvider.ToDictionary(
                    x => x.Key,
                    x => (IReadOnlyList<ProviderRuleDefinition>)x.Value.Select(MapRuleDefinition)
                                                                       .ToArray(),
                    StringComparer.OrdinalIgnoreCase)
            };
        }

        public bool TryGetProviderRules(string providerName, out IReadOnlyList<ProviderRuleDefinition> rules)
        {
            if (string.IsNullOrWhiteSpace(providerName))
            {
                rules = Array.Empty<ProviderRuleDefinition>();
                return false;
            }

            var ruleset = GetOrLoadRuleset();
            if (!ruleset.RulesByProvider.TryGetValue(providerName, out var validatedRules))
            {
                rules = Array.Empty<ProviderRuleDefinition>();
                return false;
            }

            rules = validatedRules.Select(MapRuleDefinition)
                                  .ToArray();
            return true;
        }

        internal bool TryGetValidatedProviderRules(string providerName, out IReadOnlyList<ValidatedRule> rules)
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
            return Volatile.Read(ref _ruleset) ?? LoadFirstTime();
        }

        private ValidatedRuleset LoadFirstTime()
        {
            var loaded = LoadAndValidate();
            var existing = Interlocked.CompareExchange(ref _ruleset, loaded, null);
            return existing ?? loaded;
        }

        private ValidatedRuleset LoadAndValidate()
        {
            var dto = _loader.Load();
            var ruleset = _validator.Validate(dto);

            _logger.LogInformation("Loaded ingestion rules. ProviderCount={ProviderCount}", ruleset.RulesByProvider.Count);
            return ruleset;
        }

        private static ProviderRuleDefinition MapRuleDefinition(ValidatedRule rule)
        {
            return new ProviderRuleDefinition
            {
                Id = rule.Id,
                Context = rule.Context,
                Title = rule.Title,
                Description = rule.Description,
                Enabled = rule.Enabled
            };
        }
    }
}