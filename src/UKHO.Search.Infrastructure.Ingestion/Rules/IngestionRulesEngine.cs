using Microsoft.Extensions.Logging;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Infrastructure.Ingestion.Rules.Actions;
using UKHO.Search.Infrastructure.Ingestion.Rules.Evaluation;

namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    internal sealed class IngestionRulesEngine : IIngestionRulesEngine
    {
        private readonly IngestionRulesCatalog _catalog;
        private readonly IngestionRulesPredicateEvaluator _predicateEvaluator;
        private readonly IngestionRulesActionApplier _actionApplier;
        private readonly ILogger<IngestionRulesEngine> _logger;

        public IngestionRulesEngine(IngestionRulesCatalog catalog, IngestionRulesPredicateEvaluator predicateEvaluator, IngestionRulesActionApplier actionApplier, ILogger<IngestionRulesEngine> logger)
        {
            _catalog = catalog;
            _predicateEvaluator = predicateEvaluator;
            _actionApplier = actionApplier;
            _logger = logger;
        }

        public void Apply(string providerName, IngestionRequest request, CanonicalDocument document)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(document);

            if (!_catalog.TryGetProviderRules(providerName, out var rules))
            {
                return;
            }

            object? payload = request.AddItem;
            if (payload is null)
            {
                payload = request.UpdateItem;
            }

            if (payload is null)
            {
                _logger.LogDebug("Rules engine ignored request type '{RequestType}' for provider '{ProviderName}' (no AddItem/UpdateItem payload).", request.RequestType, providerName);
                return;
            }

            var matchedRuleIds = new List<string>();
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
                summary.Add(_actionApplier.Apply(rule.Then, payload, document, match.MatchedValues));
            }

            _logger.LogDebug(
                "Ingestion rules applied. ProviderName={ProviderName} MatchedRuleIds={MatchedRuleIds} KeywordsAdded={KeywordsAdded} SearchTextAdded={SearchTextAdded} ContentAdded={ContentAdded} FacetValuesAdded={FacetValuesAdded} DocumentTypeSet={DocumentTypeSet}",
                providerName,
                matchedRuleIds,
                summary.KeywordsAdded,
                summary.SearchTextAdded,
                summary.ContentAdded,
                summary.FacetValuesAdded,
                summary.DocumentTypeSet);
        }
    }
}
