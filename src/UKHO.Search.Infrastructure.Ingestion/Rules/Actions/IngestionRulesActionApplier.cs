using UKHO.Search.Infrastructure.Ingestion.Rules.Evaluation;
using UKHO.Search.Infrastructure.Ingestion.Rules.Model;
using UKHO.Search.Infrastructure.Ingestion.Rules.Templating;
using UKHO.Search.Ingestion.Pipeline.Documents;

namespace UKHO.Search.Infrastructure.Ingestion.Rules.Actions
{
    internal sealed class IngestionRulesActionApplier
    {
        private readonly IPathResolver _pathResolver;
        private readonly IngestionRulesTemplateExpander _templateExpander;

        public IngestionRulesActionApplier(IPathResolver pathResolver, IngestionRulesTemplateExpander templateExpander)
        {
            _pathResolver = pathResolver;
            _templateExpander = templateExpander;
        }

        public ActionApplySummary Apply(ThenDto then, object payload, CanonicalDocument document, IReadOnlyList<string> matchedValues)
        {
            var summary = new ActionApplySummary();
            var context = new TemplateContext(payload, _pathResolver, matchedValues);

            ApplyKeywords(then, document, context, summary);
            ApplySearchText(then, document, context, summary);
            ApplyContent(then, document, context, summary);
            ApplyFacets(then, document, context, summary);
            ApplyDocumentType(then, document, context, summary);

            return summary;
        }

        private void ApplyKeywords(ThenDto then, CanonicalDocument document, TemplateContext context, ActionApplySummary summary)
        {
            var add = then.Keywords?.Add;
            if (add is null)
            {
                return;
            }

            foreach (var template in add)
            {
                foreach (var value in _templateExpander.Expand(template, context))
                {
                    var normalized = NormalizeToken(value);
                    if (normalized is null)
                    {
                        continue;
                    }

                    if (document.Keywords.Contains(normalized))
                    {
                        continue;
                    }

                    document.AddKeyword(normalized);
                    summary.KeywordsAdded++;
                }
            }
        }

        private void ApplySearchText(ThenDto then, CanonicalDocument document, TemplateContext context, ActionApplySummary summary)
        {
            var add = then.SearchText?.Add;
            if (add is null)
            {
                return;
            }

            foreach (var template in add)
            {
                foreach (var phrase in _templateExpander.Expand(template, context))
                {
                    var normalized = NormalizeToken(phrase);
                    if (normalized is null)
                    {
                        continue;
                    }

                    if (ContainsPhrase(document.SearchText, normalized))
                    {
                        continue;
                    }

                    document.SetSearchText(normalized);
                    summary.SearchTextAdded++;
                }
            }
        }

        private void ApplyContent(ThenDto then, CanonicalDocument document, TemplateContext context, ActionApplySummary summary)
        {
            var add = then.Content?.Add;
            if (add is null)
            {
                return;
            }

            foreach (var template in add)
            {
                foreach (var phrase in _templateExpander.Expand(template, context))
                {
                    var normalized = NormalizeToken(phrase);
                    if (normalized is null)
                    {
                        continue;
                    }

                    if (ContainsPhrase(document.Content, normalized))
                    {
                        continue;
                    }

                    document.SetContent(normalized);
                    summary.ContentAdded++;
                }
            }
        }

        private void ApplyFacets(ThenDto then, CanonicalDocument document, TemplateContext context, ActionApplySummary summary)
        {
            var add = then.Facets?.Add;
            if (add is null)
            {
                return;
            }

            foreach (var facet in add)
            {
                if (facet is null)
                {
                    continue;
                }

                var facetNames = _templateExpander.Expand(facet.Name, context);
                if (facetNames.Count == 0)
                {
                    continue;
                }

                var valuesToAdd = new List<string>();

                if (facet.Value is not null)
                {
                    valuesToAdd.AddRange(_templateExpander.Expand(facet.Value, context));
                }

                if (facet.Values is not null)
                {
                    foreach (var valueTemplate in facet.Values)
                    {
                        valuesToAdd.AddRange(_templateExpander.Expand(valueTemplate, context));
                    }
                }

                if (valuesToAdd.Count == 0)
                {
                    continue;
                }

                foreach (var facetName in facetNames)
                {
                    var normalizedName = NormalizeToken(facetName);
                    if (normalizedName is null)
                    {
                        continue;
                    }

                    foreach (var value in valuesToAdd)
                    {
                        var normalizedValue = NormalizeToken(value);
                        if (normalizedValue is null)
                        {
                            continue;
                        }

                        if (document.Facets.TryGetValue(normalizedName, out var existing) && existing.Contains(normalizedValue))
                        {
                            continue;
                        }

                        document.AddFacetValue(normalizedName, normalizedValue);
                        summary.FacetValuesAdded++;
                    }
                }
            }
        }

        private void ApplyDocumentType(ThenDto then, CanonicalDocument document, TemplateContext context, ActionApplySummary summary)
        {
            var template = then.DocumentType?.Set;
            if (template is null)
            {
                return;
            }

            var values = _templateExpander.Expand(template, context);
            if (values.Count == 0)
            {
                return;
            }

            if (values.Count > 1)
            {
                throw new InvalidOperationException("documentType.set must resolve to exactly one value.");
            }

            var normalized = NormalizeToken(values[0]);
            if (normalized is null)
            {
                return;
            }

            document.DocumentType = normalized;
            summary.DocumentTypeSet = 1;
        }

        private static bool ContainsPhrase(string existing, string phrase)
        {
            if (string.IsNullOrWhiteSpace(existing))
            {
                return false;
            }

            var idx = 0;
            while (true)
            {
                idx = existing.IndexOf(phrase, idx, StringComparison.Ordinal);
                if (idx < 0)
                {
                    return false;
                }

                var leftOk = idx == 0 || existing[idx - 1] == ' ';
                var rightIdx = idx + phrase.Length;
                var rightOk = rightIdx == existing.Length || existing[rightIdx] == ' ';

                if (leftOk && rightOk)
                {
                    return true;
                }

                idx = rightIdx;
            }
        }

        private static string? NormalizeToken(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim().ToLowerInvariant();
        }
    }
}
