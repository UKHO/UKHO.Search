using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UKHO.Search.Infrastructure.Query.Rules
{
    /// <summary>
    /// Reads flat query-rule documents from configuration using the shared rules:query namespace.
    /// </summary>
    internal sealed class AppConfigQueryRulesSource : IQueryRulesSource
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AppConfigQueryRulesSource> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppConfigQueryRulesSource"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration used to enumerate flat query-rule entries.</param>
        /// <param name="logger">The logger that records rule enumeration diagnostics.</param>
        public AppConfigQueryRulesSource(IConfiguration configuration, ILogger<AppConfigQueryRulesSource> logger)
        {
            // Retain the live configuration root so the source always enumerates the current rule hierarchy.
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Retain the logger so namespace and rule-count diagnostics are visible during startup and refresh investigations.
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Lists every flat query-rule entry stored beneath the query namespace in configuration.
        /// </summary>
        /// <returns>A list of namespace-aware query-rule entries keyed by flat rule identifier.</returns>
        public IReadOnlyList<QueryRuleEntry> ListRuleEntries()
        {
            // Materialize the entries into a list so downstream loaders can process the current snapshot deterministically.
            var result = new List<QueryRuleEntry>();
            var rulesRoot = _configuration.GetSection(QueryRuleConfigurationPath.QueryRulesRoot);

            foreach (var ruleSection in rulesRoot.GetChildren().OrderBy(section => section.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(ruleSection.Key))
                {
                    continue;
                }

                // Query rules are deliberately flat, so nested sections are ignored rather than recursively interpreted.
                if (ruleSection.GetChildren().Any())
                {
                    _logger.LogWarning("Ignored nested query rule section because query rules must remain flat. Section={Section}", ruleSection.Path);
                    continue;
                }

                var key = QueryRuleConfigurationPath.BuildRuleKey(ruleSection.Key);
                result.Add(new QueryRuleEntry
                {
                    Key = key,
                    RuleId = ruleSection.Key.Trim().ToLowerInvariant(),
                    Json = ruleSection.Value ?? string.Empty
                });
            }

            // Log the effective namespace explicitly so diagnostics can confirm the flat query-rule root in use.
            _logger.LogInformation(
                "Listed App Config query rules. Namespace={Namespace} RuleCount={RuleCount}",
                QueryRuleConfigurationPath.QueryRulesRoot,
                result.Count);

            return result;
        }
    }
}
