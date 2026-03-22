using Microsoft.Extensions.Logging;
using UKHO.Search.Infrastructure.Ingestion.Rules.Actions;
using UKHO.Search.Infrastructure.Ingestion.Rules.Evaluation;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    internal sealed class IngestionRulesEngine : IIngestionRulesEngine
    {
        private readonly IngestionRulesActionApplier _actionApplier;
        private readonly IngestionRulesCatalog _catalog;
        private readonly ILogger<IngestionRulesEngine> _logger;
        private readonly IngestionRulesPredicateEvaluator _predicateEvaluator;

        public IngestionRulesEngine(IngestionRulesCatalog catalog, IngestionRulesPredicateEvaluator predicateEvaluator, IngestionRulesActionApplier actionApplier, ILogger<IngestionRulesEngine> logger)
        {
            _catalog = catalog;
            _predicateEvaluator = predicateEvaluator;
            _actionApplier = actionApplier;
            _logger = logger;
        }

        public void Apply(string providerName, IngestionRequest request, CanonicalDocument document)
        {
            _ = ApplyWithReport(providerName, request, document);
        }

        public IngestionRulesApplyReport ApplyWithReport(string providerName, IngestionRequest request, CanonicalDocument document)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(document);

            if (!_catalog.TryGetValidatedProviderRules(providerName, out var rules))
            {
                return IngestionRulesApplyReport.Empty;
            }

            object? payload = request.IndexItem;

            if (payload is null)
            {
                _logger.LogDebug("Rules engine ignored request type '{RequestType}' for provider '{ProviderName}' (no IndexItem payload).", request.RequestType, providerName);
                return IngestionRulesApplyReport.Empty;
            }

            var matchedRuleIds = new List<string>();
            var matchedRules = new List<IngestionRulesMatchedRule>();
            var summary = new ActionApplySummary();

            foreach (var rule in rules)
            {
                if (!rule.Enabled)
                {
                    continue;
                }

                var match = _predicateEvaluator.Evaluate(rule.Id, rule.Predicate, payload);
                if (!match.IsMatch)
                {
                    continue;
                }

                matchedRuleIds.Add(rule.Id);
                var titleValuesAdded = _actionApplier.ApplyTitle(rule.Title, payload, document, match.MatchedValues);
                var ruleSummary = _actionApplier.Apply(rule.Then, payload, document, match.MatchedValues);
                ruleSummary.AdditionalFieldValuesAdded += titleValuesAdded;
                summary.Add(ruleSummary);
                matchedRules.Add(new IngestionRulesMatchedRule
                {
                    RuleId = rule.Id,
                    Description = rule.Description,
                    Summary = FormatRuleSummary(ruleSummary),
                });
            }

            _logger.LogDebug("Ingestion rules applied. ProviderName={ProviderName} MatchedRuleIds={MatchedRuleIds} KeywordsAdded={KeywordsAdded} SearchTextAdded={SearchTextAdded} ContentAdded={ContentAdded} FacetValuesAdded={FacetValuesAdded} DocumentTypeSet={DocumentTypeSet}", providerName, matchedRuleIds, summary.KeywordsAdded, summary.SearchTextAdded, summary.ContentAdded,
                summary.FacetValuesAdded, summary.DocumentTypeSet);

            return new IngestionRulesApplyReport
            {
                MatchedRules = matchedRules,
            };
        }

        private static string FormatRuleSummary(ActionApplySummary summary)
        {
            var parts = new List<string>();

            if (summary.KeywordsAdded > 0)
            {
                parts.Add($"Keywords +{summary.KeywordsAdded}");
            }

            if (summary.AdditionalFieldValuesAdded > 0)
            {
                parts.Add($"Fields +{summary.AdditionalFieldValuesAdded}");
            }

            if (summary.SearchTextAdded > 0)
            {
                parts.Add($"SearchText +{summary.SearchTextAdded}");
            }

            if (summary.ContentAdded > 0)
            {
                parts.Add($"Content +{summary.ContentAdded}");
            }

            if (summary.FacetValuesAdded > 0)
            {
                parts.Add($"Facets +{summary.FacetValuesAdded}");
            }

            if (summary.DocumentTypeSet > 0)
            {
                parts.Add($"DocumentType set {summary.DocumentTypeSet} time(s)");
            }

            return parts.Count == 0
                ? "No document changes."
                : string.Join(", ", parts);
        }
    }
}