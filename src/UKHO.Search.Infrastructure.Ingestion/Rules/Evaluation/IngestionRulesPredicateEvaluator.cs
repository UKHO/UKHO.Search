using System.Text.Json;

namespace UKHO.Search.Infrastructure.Ingestion.Rules.Evaluation
{
    internal sealed class IngestionRulesPredicateEvaluator
    {
        private readonly IPathResolver _pathResolver;

        public IngestionRulesPredicateEvaluator(IPathResolver pathResolver)
        {
            _pathResolver = pathResolver;
        }

        public MatchResult Evaluate(string ruleId, JsonElement predicate, object payload)
        {
            var (isMatch, matchedValues) = EvaluateNode(predicate, payload);
            return new MatchResult(ruleId, isMatch, matchedValues);
        }

        private (bool IsMatch, IReadOnlyList<string> MatchedValues) EvaluateNode(JsonElement node, object payload)
        {
            if (node.ValueKind != JsonValueKind.Object)
            {
                return (false, Array.Empty<string>());
            }

            if (node.TryGetProperty("all", out var all))
            {
                return EvaluateAll(all, payload);
            }

            if (node.TryGetProperty("any", out var any))
            {
                return EvaluateAny(any, payload);
            }

            if (node.TryGetProperty("not", out var not))
            {
                return EvaluateNot(not, payload);
            }

            if (node.TryGetProperty("path", out var pathElement))
            {
                return EvaluateLeaf(node, pathElement, payload);
            }

            return EvaluateShorthand(node, payload);
        }

        private (bool IsMatch, IReadOnlyList<string> MatchedValues) EvaluateAll(JsonElement array, object payload)
        {
            if (array.ValueKind != JsonValueKind.Array)
            {
                return (false, Array.Empty<string>());
            }

            var matchedValues = new List<string>();

            foreach (var child in array.EnumerateArray())
            {
                var (isMatch, childMatched) = EvaluateNode(child, payload);
                if (!isMatch)
                {
                    return (false, Array.Empty<string>());
                }

                matchedValues.AddRange(childMatched);
            }

            return (true, matchedValues);
        }

        private (bool IsMatch, IReadOnlyList<string> MatchedValues) EvaluateAny(JsonElement array, object payload)
        {
            if (array.ValueKind != JsonValueKind.Array)
            {
                return (false, Array.Empty<string>());
            }

            foreach (var child in array.EnumerateArray())
            {
                var (isMatch, childMatched) = EvaluateNode(child, payload);
                if (isMatch)
                {
                    return (true, childMatched);
                }
            }

            return (false, Array.Empty<string>());
        }

        private (bool IsMatch, IReadOnlyList<string> MatchedValues) EvaluateNot(JsonElement child, object payload)
        {
            var (childMatch, _) = EvaluateNode(child, payload);
            return childMatch ? (false, Array.Empty<string>()) : (true, Array.Empty<string>());
        }

        private (bool IsMatch, IReadOnlyList<string> MatchedValues) EvaluateShorthand(JsonElement node, object payload)
        {
            var matchedValues = new List<string>();

            foreach (var prop in node.EnumerateObject())
            {
                if (prop.Value.ValueKind != JsonValueKind.String)
                {
                    return (false, Array.Empty<string>());
                }

                var resolved = _pathResolver.Resolve(payload, prop.Name);

                if (!IngestionRulesOperatorEvaluator.Evaluate("eq", resolved, prop.Value, out var matched))
                {
                    return (false, Array.Empty<string>());
                }

                matchedValues.AddRange(matched);
            }

            return (true, matchedValues);
        }

        private (bool IsMatch, IReadOnlyList<string> MatchedValues) EvaluateLeaf(JsonElement node, JsonElement pathElement, object payload)
        {
            if (pathElement.ValueKind != JsonValueKind.String)
            {
                return (false, Array.Empty<string>());
            }

            var path = pathElement.GetString();
            if (string.IsNullOrWhiteSpace(path))
            {
                return (false, Array.Empty<string>());
            }

            string? op = null;
            JsonElement opValue = default;

            foreach (var prop in node.EnumerateObject())
            {
                if (string.Equals(prop.Name, "path", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                op = prop.Name;
                opValue = prop.Value;
                break;
            }

            if (op is null)
            {
                return (false, Array.Empty<string>());
            }

            var resolved = _pathResolver.Resolve(payload, path);
            var isMatch = IngestionRulesOperatorEvaluator.Evaluate(op, resolved, opValue, out var matchedValues);
            return (isMatch, matchedValues);
        }
    }
}