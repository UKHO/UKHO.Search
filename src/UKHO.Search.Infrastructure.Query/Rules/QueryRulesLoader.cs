using System.Text.Json;
using Microsoft.Extensions.Logging;
using UKHO.Search.Query.Models;

namespace UKHO.Search.Infrastructure.Query.Rules
{
    /// <summary>
    /// Loads flat query rules from the configured source and validates them into a runtime snapshot.
    /// </summary>
    internal sealed class QueryRulesLoader
    {
        private readonly IQueryRulesSource _source;
        private readonly QueryRulesValidator _validator;
        private readonly ILogger<QueryRulesLoader> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryRulesLoader"/> class.
        /// </summary>
        /// <param name="source">The source that enumerates raw query-rule entries.</param>
        /// <param name="validator">The validator that normalizes the raw rule documents into a runtime snapshot.</param>
        /// <param name="logger">The logger that records loading diagnostics.</param>
        public QueryRulesLoader(IQueryRulesSource source, QueryRulesValidator validator, ILogger<QueryRulesLoader> logger)
        {
            // Retain the collaborating services once so each load operation follows the same deterministic pipeline.
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Loads the current query-rule snapshot from the configured source.
        /// </summary>
        /// <returns>The validated query-rule snapshot that should be cached by the runtime catalog.</returns>
        public QueryRulesSnapshot Load()
        {
            // Enumerate the current raw entries first so the load pipeline works from one deterministic source snapshot.
            var entries = _source.ListRuleEntries();
            var documents = new List<QueryRuleDocumentDto>();
            var rejectedCount = 0;

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Json))
                {
                    rejectedCount++;
                    _logger.LogWarning("Rejected query rule with empty value. Key={Key}", entry.Key);
                    continue;
                }

                QueryRuleDocumentDto? document;
                try
                {
                    document = JsonSerializer.Deserialize<QueryRuleDocumentDto>(entry.Json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (JsonException ex)
                {
                    rejectedCount++;
                    _logger.LogWarning(ex, "Rejected query rule with invalid JSON. Key={Key}", entry.Key);
                    continue;
                }

                if (document?.Rule is null)
                {
                    rejectedCount++;
                    _logger.LogWarning("Rejected query rule with missing rule payload. Key={Key}", entry.Key);
                    continue;
                }

                // Bind the runtime rule identifier to the flat configuration key so file name and loaded rule id stay aligned.
                document.Rule.Id = entry.RuleId;
                documents.Add(document);
            }

            var snapshot = _validator.Validate(documents);

            // Log the final validated count so startup diagnostics can distinguish rejected raw entries from accepted runtime rules.
            _logger.LogInformation(
                "Loaded query rules from App Config. RuleCount={RuleCount} RejectedCount={RejectedCount}",
                snapshot.Rules.Count,
                rejectedCount);

            return snapshot;
        }
    }
}
