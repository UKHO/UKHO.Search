using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UKHO.Search.Infrastructure.Ingestion.Rules.Model;
using UKHO.Search.Infrastructure.Ingestion.Rules.Validation;

namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    internal sealed class IngestionRulesLoader
    {
        private readonly IHostEnvironment _hostEnvironment;
        private readonly IngestionRulesValidator _validator;
        private readonly RuleFileLoader _ruleFileLoader;
        private readonly ILogger<IngestionRulesLoader> _logger;

        public IngestionRulesLoader(IHostEnvironment hostEnvironment, IngestionRulesValidator validator, RuleFileLoader ruleFileLoader, ILogger<IngestionRulesLoader> logger)
        {
            _hostEnvironment = hostEnvironment;
            _validator = validator;
            _ruleFileLoader = ruleFileLoader;
            _logger = logger;
        }

        public RulesetDto Load()
        {
            if (!TryLoadFromRulesDirectory(out var dtoFromDirectory))
            {
                var rulesPath = Path.Combine(_hostEnvironment.ContentRootPath, RuleFileLoader.RulesRootDirectoryName);
                throw new IngestionRulesValidationException($"Missing required rules directory '{RuleFileLoader.RulesRootDirectoryName}' at '{rulesPath}'.");
            }

            return dtoFromDirectory;
        }

        private bool TryLoadFromRulesDirectory(out RulesetDto dto)
        {
            dto = new RulesetDto();

            var rulesPath = Path.Combine(_hostEnvironment.ContentRootPath, RuleFileLoader.RulesRootDirectoryName);
            if (!Directory.Exists(rulesPath))
            {
                return false;
            }

            var providerRules = new Dictionary<string, RuleDto[]>(StringComparer.OrdinalIgnoreCase);

            foreach (var providerDirectory in Directory.EnumerateDirectories(rulesPath))
            {
                var providerName = Path.GetFileName(providerDirectory);
                if (string.IsNullOrWhiteSpace(providerName))
                {
                    continue;
                }

                var rules = _ruleFileLoader.LoadProviderRules(_hostEnvironment.ContentRootPath, providerName);
                if (rules.Count == 0)
                {
                    continue;
                }

                // Validate provider rules using existing validator paths/operator checks.
                var candidate = new RulesetDto
                {
                    SchemaVersion = IngestionRulesValidator.SupportedSchemaVersion,
                    Rules = new Dictionary<string, RuleDto[]>(StringComparer.OrdinalIgnoreCase)
                    {
                        [providerName] = rules.ToArray()
                    }
                };

                // Validator throws on invalid predicate/operator/path etc.
                _ = _validator.Validate(candidate);

                providerRules[providerName] = rules.ToArray();
            }

            dto.SchemaVersion = IngestionRulesValidator.SupportedSchemaVersion;
            dto.Rules = providerRules;

            _logger.LogInformation("Loaded ingestion rules from directory. RulesRoot={RulesRoot} ProviderCount={ProviderCount}", rulesPath, providerRules.Count);

            return true;
        }

    }
}