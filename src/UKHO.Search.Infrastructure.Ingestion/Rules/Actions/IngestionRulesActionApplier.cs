using UKHO.Search.Infrastructure.Ingestion.Rules.Evaluation;
using UKHO.Search.Infrastructure.Ingestion.Rules.Model;
using UKHO.Search.Infrastructure.Ingestion.Rules.Templating;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Query;

namespace UKHO.Search.Infrastructure.Ingestion.Rules.Actions
{
    internal sealed class IngestionRulesActionApplier
    {
        private readonly IPathResolver _pathResolver;
        private readonly IngestionRulesTemplateExpander _templateExpander;
        private readonly TokenNormalizer _tokenNormalizer = new();

        public IngestionRulesActionApplier(IPathResolver pathResolver, IngestionRulesTemplateExpander templateExpander)
        {
            _pathResolver = pathResolver;
            _templateExpander = templateExpander;
        }

        public ActionApplySummary Apply(ThenDto? then, object payload, CanonicalDocument document, IReadOnlyList<string> matchedValues)
        {
            var summary = new ActionApplySummary();
            if (then is null)
            {
                return summary;
            }

            var context = new TemplateContext(payload, _pathResolver, matchedValues);

            ApplyKeywords(then, document, context, summary);
            ApplyAdditionalFields(then, document, context, summary);
            ApplySearchText(then, document, context, summary);
            ApplyContent(then, document, context, summary);

            return summary;
        }

        public int ApplyTitle(string? titleTemplate, object payload, CanonicalDocument document, IReadOnlyList<string> matchedValues)
        {
            if (string.IsNullOrWhiteSpace(titleTemplate))
            {
                return 0;
            }

            var context = new TemplateContext(payload, _pathResolver, matchedValues);
            var titlesAdded = 0;

            foreach (var title in _templateExpander.Expand(titleTemplate, context))
            {
                var normalized = NormalizeDisplayValue(title);
                if (normalized is null)
                {
                    continue;
                }

                if (document.Title.Contains(normalized))
                {
                    continue;
                }

                document.AddTitle(normalized);
                titlesAdded++;
            }

            return titlesAdded;
        }

        private void ApplyAdditionalFields(ThenDto then, CanonicalDocument document, TemplateContext context, ActionApplySummary summary)
        {
            ApplyStringAdds(then.Authority?.Add, document.Authority, document.AddAuthority, context, summary);
            ApplyStringAdds(then.Region?.Add, document.Region, document.AddRegion, context, summary);
            ApplyStringAdds(then.Format?.Add, document.Format, document.AddFormat, context, summary);
            ApplyStringAdds(then.Category?.Add, document.Category, document.AddCategory, context, summary);
            ApplyStringAdds(then.Series?.Add, document.Series, document.AddSeries, context, summary);
            ApplyStringAdds(then.Instance?.Add, document.Instance, document.AddInstance, context, summary);

            ApplyIntAdds(then.MajorVersion, document.MajorVersion, document.AddMajorVersion, context, summary);
            ApplyIntAdds(then.MinorVersion, document.MinorVersion, document.AddMinorVersion, context, summary);
        }

        private void ApplyStringAdds(
            IEnumerable<string>? add,
            SortedSet<string> existing,
            Action<string?> addToDocument,
            TemplateContext context,
            ActionApplySummary summary)
        {
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

                    if (existing.Contains(normalized))
                    {
                        continue;
                    }

                    addToDocument(normalized);
                    summary.AdditionalFieldValuesAdded++;
                }
            }
        }

        private void ApplyIntAdds(
            IntAddActionDto? action,
            SortedSet<int> existing,
            Action<int?> addToDocument,
            TemplateContext context,
            ActionApplySummary summary)
        {
            if (action is null)
            {
                return;
            }

            foreach (var value in action.GetAddValues())
            {
                if (existing.Contains(value))
                {
                    continue;
                }

                addToDocument(value);
                summary.AdditionalFieldValuesAdded++;
            }

            foreach (var template in action.GetAddTemplates())
            {
                foreach (var value in _templateExpander.ExpandToInt(template, context))
                {
                    if (existing.Contains(value))
                    {
                        continue;
                    }

                    addToDocument(value);
                    summary.AdditionalFieldValuesAdded++;
                }
            }
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
                    foreach (var normalizedToken in _tokenNormalizer.NormalizeToken(value))
                    {
                        if (document.Keywords.Contains(normalizedToken))
                        {
                            continue;
                        }

                        document.AddKeyword(normalizedToken);
                        summary.KeywordsAdded++;
                    }
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

                    document.AddSearchText(normalized);
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

                    document.AddContent(normalized);
                    summary.ContentAdded++;
                }
            }
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

            return value.Trim()
                        .ToLowerInvariant();
        }

        private static string? NormalizeDisplayValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }
    }
}