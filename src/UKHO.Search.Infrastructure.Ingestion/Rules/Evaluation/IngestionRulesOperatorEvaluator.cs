using System.Text.Json;

namespace UKHO.Search.Infrastructure.Ingestion.Rules.Evaluation
{
    internal static class IngestionRulesOperatorEvaluator
    {
        public static bool Evaluate(string @operator, IReadOnlyList<string> resolvedValues, JsonElement operatorValue, out IReadOnlyList<string> matchedValues)
        {
            matchedValues = Array.Empty<string>();

            if (resolvedValues is null || resolvedValues.Count == 0)
            {
                return false;
            }

            if (@operator.Equals("exists", StringComparison.OrdinalIgnoreCase))
            {
                var nonEmpty = resolvedValues.Where(v => !string.IsNullOrWhiteSpace(v))
                                             .ToArray();
                matchedValues = nonEmpty;
                return nonEmpty.Length > 0;
            }

            if (@operator.Equals("in", StringComparison.OrdinalIgnoreCase))
            {
                if (operatorValue.ValueKind != JsonValueKind.Array)
                {
                    return false;
                }

                var set = new HashSet<string>(StringComparer.Ordinal);
                foreach (var element in operatorValue.EnumerateArray())
                {
                    if (element.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var normalized = Normalize(element.GetString());
                    if (!string.IsNullOrEmpty(normalized))
                    {
                        set.Add(normalized);
                    }
                }

                if (set.Count == 0)
                {
                    return false;
                }

                var matched = resolvedValues.Where(v => set.Contains(Normalize(v)))
                                            .Where(v => !string.IsNullOrEmpty(v))
                                            .ToArray();
                matchedValues = matched;
                return matched.Length > 0;
            }

            if (operatorValue.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var comparator = Normalize(operatorValue.GetString());
            if (string.IsNullOrEmpty(comparator))
            {
                return false;
            }

            if (@operator.Equals("eq", StringComparison.OrdinalIgnoreCase))
            {
                var matched = resolvedValues.Where(v => Normalize(v) == comparator)
                                            .Where(v => !string.IsNullOrEmpty(v))
                                            .ToArray();
                matchedValues = matched;
                return matched.Length > 0;
            }

            if (@operator.Equals("contains", StringComparison.OrdinalIgnoreCase))
            {
                var matched = resolvedValues.Where(v => Normalize(v)
                                                .Contains(comparator, StringComparison.Ordinal))
                                            .Where(v => !string.IsNullOrEmpty(v))
                                            .ToArray();
                matchedValues = matched;
                return matched.Length > 0;
            }

            if (@operator.Equals("startsWith", StringComparison.OrdinalIgnoreCase))
            {
                var matched = resolvedValues.Where(v => Normalize(v)
                                                .StartsWith(comparator, StringComparison.Ordinal))
                                            .Where(v => !string.IsNullOrEmpty(v))
                                            .ToArray();
                matchedValues = matched;
                return matched.Length > 0;
            }

            if (@operator.Equals("endsWith", StringComparison.OrdinalIgnoreCase))
            {
                var matched = resolvedValues.Where(v => Normalize(v)
                                                .EndsWith(comparator, StringComparison.Ordinal))
                                            .Where(v => !string.IsNullOrEmpty(v))
                                            .ToArray();
                matchedValues = matched;
                return matched.Length > 0;
            }

            return false;
        }

        private static string Normalize(string? input)
        {
            return (input ?? string.Empty).Trim()
                                          .ToLowerInvariant();
        }
    }
}