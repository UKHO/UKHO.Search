using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    /// <summary>
    /// Reads ingestion rule documents from Azure App Configuration using the namespace-aware ingestion rule key contract.
    /// </summary>
    internal sealed class AppConfigRulesSource : IRulesSource
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AppConfigRulesSource> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppConfigRulesSource"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration used to enumerate App Configuration-backed rule entries.</param>
        /// <param name="logger">The logger that records rule enumeration diagnostics.</param>
        public AppConfigRulesSource(IConfiguration configuration, ILogger<AppConfigRulesSource> logger)
        {
            // Retain the raw configuration root so the source can enumerate the live App Configuration-backed hierarchy on demand.
            _configuration = configuration;

            // Retain the logger so enumeration decisions and rule counts are visible during diagnostics.
            _logger = logger;
        }

        /// <summary>
        /// Lists every ingestion rule entry stored beneath the ingestion namespace in App Configuration.
        /// </summary>
        /// <returns>A list of namespace-aware rule entries keyed by provider and rule identifier.</returns>
        public IReadOnlyList<RuleEntryDto> ListRuleEntries()
        {
            // Materialize the rule entries into a list so downstream callers can enumerate the snapshot deterministically.
            var result = new List<RuleEntryDto>();

            // Enumerate only the ingestion namespace so legacy rules:file-share:* keys are ignored rather than treated as active input.
            var rulesRoot = _configuration.GetSection(IngestionRuleConfigurationPath.IngestionRulesRoot);
            foreach (var providerSection in rulesRoot.GetChildren())
            {
                if (string.IsNullOrWhiteSpace(providerSection.Key))
                {
                    continue;
                }

                var provider = IngestionRuleConfigurationPath.NormalizeProvider(providerSection.Key);

                foreach (var ruleSection in EnumerateRuleSections(providerSection, []))
                {
                    var ruleId = BuildRuleId(ruleSection.RelativeSegments);
                    if (string.IsNullOrWhiteSpace(ruleId))
                    {
                        continue;
                    }

                    var key = IngestionRuleConfigurationPath.BuildRuleKey(provider, ruleId);
                    result.Add(new RuleEntryDto(
                        key,
                        provider,
                        ruleId,
                        ruleSection.Section.Value,
                        IsValid: true,
                        ErrorMessage: null));
                }
            }

            // Log the effective namespace root explicitly so diagnostics distinguish the new key space from legacy rule storage.
            _logger.LogInformation("Listed App Config ingestion rules. Namespace={Namespace} ProviderCount={ProviderCount} RuleCount={RuleCount}",
                IngestionRuleConfigurationPath.IngestionRulesRoot,
                result.Select(r => r.Provider).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                result.Count);

            return result;
        }

        /// <summary>
        /// Recursively walks a provider section until it reaches leaf configuration entries that hold individual rule JSON values.
        /// </summary>
        /// <param name="currentSection">The provider-relative configuration section currently being examined.</param>
        /// <param name="relativeSegments">The provider-relative rule identifier segments accumulated so far.</param>
        /// <returns>A sequence of leaf rule sections paired with their provider-relative rule identifier segments.</returns>
        private static IEnumerable<(IConfigurationSection Section, IReadOnlyList<string> RelativeSegments)> EnumerateRuleSections(IConfigurationSection currentSection, IReadOnlyList<string> relativeSegments)
        {
            // Snapshot the children once so the method can distinguish leaf rule values from intermediate grouping sections.
            var childSections = currentSection.GetChildren().ToArray();

            // A section with no children is the terminal rule value regardless of whether the value is populated.
            if (childSections.Length == 0)
            {
                yield return (currentSection, relativeSegments);
                yield break;
            }

            foreach (var childSection in childSections)
            {
                if (string.IsNullOrWhiteSpace(childSection.Key))
                {
                    continue;
                }

                // Extend the provider-relative rule identifier so nested repository folders become nested rule-id segments.
                var childSegments = relativeSegments.Concat([childSection.Key]).ToArray();
                foreach (var nestedRuleSection in EnumerateRuleSections(childSection, childSegments))
                {
                    yield return nestedRuleSection;
                }
            }
        }

        /// <summary>
        /// Builds the rule identifier from the provider-relative configuration path segments.
        /// </summary>
        /// <param name="relativeSegments">The provider-relative path segments that identify a rule beneath the provider namespace.</param>
        /// <returns>The rule identifier string used by the runtime, including nested segments separated by colons.</returns>
        private static string BuildRuleId(IEnumerable<string> relativeSegments)
        {
            // Preserve nested segments instead of flattening them away so repository subfolders map directly to rule identifiers.
            return string.Join(':', relativeSegments.Where(segment => !string.IsNullOrWhiteSpace(segment)).Select(segment => segment.Trim()));
        }
    }
}
