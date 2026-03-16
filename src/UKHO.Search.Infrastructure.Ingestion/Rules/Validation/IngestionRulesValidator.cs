using System.Text.Json;
using UKHO.Search.Infrastructure.Ingestion.Rules.Model;

namespace UKHO.Search.Infrastructure.Ingestion.Rules.Validation
{
    internal sealed class IngestionRulesValidator
    {
        internal const string SupportedSchemaVersion = "1.0";

        private readonly IngestionRulesPathValidator _pathValidator;

        public IngestionRulesValidator(IngestionRulesPathValidator pathValidator)
        {
            _pathValidator = pathValidator;
        }

        public ValidatedRuleset Validate(RulesetDto ruleset)
        {
            ArgumentNullException.ThrowIfNull(ruleset);

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(ruleset.SchemaVersion))
            {
                errors.Add("Missing required 'schemaVersion'.");
            }
            else if (!string.Equals(ruleset.SchemaVersion, SupportedSchemaVersion, StringComparison.Ordinal))
            {
                errors.Add($"Unsupported schemaVersion '{ruleset.SchemaVersion}'. Expected '{SupportedSchemaVersion}'.");
            }

            if (ruleset.Rules is null)
            {
                errors.Add("Missing required 'rules' object.");
            }
            else
            {
                ValidateProviders(ruleset.Rules, errors, out var _);
            }

            if (errors.Count > 0)
            {
                var message = "Rules validation failed.";
                if (errors.Count > 0)
                {
                    message += Environment.NewLine + string.Join(Environment.NewLine, errors.Select(e => "- " + e));
                }

                throw new IngestionRulesValidationException(message, errors);
            }

            var rulesByProvider = new Dictionary<string, IReadOnlyList<ValidatedRule>>(StringComparer.OrdinalIgnoreCase);
            if (ruleset.Rules is not null)
            {
                foreach (var provider in ruleset.Rules)
                {
                    if (provider.Value is null || provider.Value.Length == 0)
                    {
                        continue;
                    }

                    var validatedRules = new List<ValidatedRule>(provider.Value.Length);
                    foreach (var rule in provider.Value)
                    {
                        if (rule is null)
                        {
                            continue;
                        }

                        var enabled = rule.Enabled ?? true;
                        var predicate = ResolvePredicate(rule, null);
                        if (predicate is null || rule.Then is null || string.IsNullOrWhiteSpace(rule.Id))
                        {
                            continue;
                        }

                        validatedRules.Add(new ValidatedRule
                        {
                            Id = rule.Id,
                            Description = rule.Description,
                            Enabled = enabled,
                            Predicate = predicate.Value,
                            Then = rule.Then
                        });
                    }

                    if (validatedRules.Count > 0)
                    {
                        rulesByProvider[provider.Key] = validatedRules;
                    }
                }
            }

            return new ValidatedRuleset
            {
                SchemaVersion = ruleset.SchemaVersion ?? SupportedSchemaVersion,
                RulesByProvider = rulesByProvider
            };
        }

        private void ValidateProviders(Dictionary<string, RuleDto[]> rules, List<string> errors, out int totalRuleCount)
        {
            totalRuleCount = 0;
            var anyNonEmptyProvider = false;

            foreach (var provider in rules)
            {
                var providerName = provider.Key;
                var providerRules = provider.Value;

                if (providerRules is null || providerRules.Length == 0)
                {
                    continue;
                }

                anyNonEmptyProvider = true;
                totalRuleCount += providerRules.Length;

                var seenIds = new HashSet<string>(StringComparer.Ordinal);

                foreach (var rule in providerRules)
                {
                    if (rule is null)
                    {
                        errors.Add($"Provider '{providerName}' contains a null rule entry.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(rule.Id))
                    {
                        errors.Add($"Provider '{providerName}' contains a rule with missing 'id'.");
                    }
                    else if (!seenIds.Add(rule.Id))
                    {
                        errors.Add($"Provider '{providerName}' contains duplicate rule id '{rule.Id}'.");
                    }

                    var predicate = ResolvePredicate(rule, errors);
                    if (predicate is not null)
                    {
                        ValidatePredicateNode(predicate.Value, errors);
                    }

                    if (rule.Then is null)
                    {
                        errors.Add($"Rule '{rule.Id ?? "<missing id>"}' in provider '{providerName}' is missing required 'then' block.");
                    }
                    else
                    {
                        ValidateThen(rule.Then, rule.Id, providerName, errors, predicate);
                    }
                }
            }

            if (!anyNonEmptyProvider)
            {
                errors.Add("Ruleset must contain at least one provider with a non-empty rule array.");
            }
        }

        private static JsonElement? ResolvePredicate(RuleDto rule, List<string>? errors)
        {
            var hasIf = rule.If.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Null);
            var hasMatch = rule.Match.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Null);

            if (hasIf == hasMatch)
            {
                errors?.Add($"Rule '{rule.Id ?? "<missing id>"}' must contain exactly one predicate block: 'if' or 'match'.");
                return null;
            }

            return hasIf ? rule.If : rule.Match;
        }

        private void ValidateThen(ThenDto then, string? ruleId, string providerName, List<string> errors, JsonElement? predicate)
        {
        }

        private static IEnumerable<string> ExtractPathVariables(string template)
        {
            var idx = 0;
            while (idx < template.Length)
            {
                idx = template.IndexOf("$path:", idx, StringComparison.Ordinal);
                if (idx < 0)
                {
                    yield break;
                }

                var start = idx + 6;
                var end = start;
                while (end < template.Length)
                {
                    var c = template[end];
                    if (char.IsWhiteSpace(c) || c == '$')
                    {
                        break;
                    }

                    end++;
                }

                if (end > start)
                {
                    yield return template.Substring(start, end - start);
                }

                idx = end;
            }
        }

        private static void CollectPredicateShape(JsonElement node, ref int leafCount, ref bool hasWildcard)
        {
            if (node.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            if (node.TryGetProperty("all", out var all) && all.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in all.EnumerateArray())
                {
                    CollectPredicateShape(child, ref leafCount, ref hasWildcard);
                }

                return;
            }

            if (node.TryGetProperty("any", out var any) && any.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in any.EnumerateArray())
                {
                    CollectPredicateShape(child, ref leafCount, ref hasWildcard);
                }

                return;
            }

            if (node.TryGetProperty("not", out var not))
            {
                CollectPredicateShape(not, ref leafCount, ref hasWildcard);
                return;
            }

            if (node.TryGetProperty("path", out var pathElement) && pathElement.ValueKind == JsonValueKind.String)
            {
                leafCount++;
                var path = pathElement.GetString() ?? string.Empty;
                if (path.Contains("[*]", StringComparison.Ordinal))
                {
                    hasWildcard = true;
                }

                return;
            }

            // Shorthand AND-only form
            foreach (var prop in node.EnumerateObject())
            {
                leafCount++;
                if (prop.Name.Contains("[*]", StringComparison.Ordinal))
                {
                    hasWildcard = true;
                }
            }
        }

        private void ValidatePredicateNode(JsonElement node, List<string> errors)
        {
            if (node.ValueKind != JsonValueKind.Object)
            {
                errors.Add("Predicate node must be a JSON object.");
                return;
            }

            var props = node.EnumerateObject()
                            .ToArray();
            if (props.Length == 0)
            {
                errors.Add("Predicate object must not be empty.");
                return;
            }

            var hasAll = node.TryGetProperty("all", out var all);
            var hasAny = node.TryGetProperty("any", out var any);
            var hasNot = node.TryGetProperty("not", out var not);
            var hasPath = node.TryGetProperty("path", out var path);

            var booleanKeys = (hasAll ? 1 : 0) + (hasAny ? 1 : 0) + (hasNot ? 1 : 0);

            if (booleanKeys > 0)
            {
                if (booleanKeys != 1 || props.Length != 1)
                {
                    errors.Add("Boolean predicate nodes must contain exactly one of: all, any, not.");
                    return;
                }

                if (hasAll)
                {
                    ValidatePredicateArray(all, "all", errors);
                    return;
                }

                if (hasAny)
                {
                    ValidatePredicateArray(any, "any", errors);
                    return;
                }

                // not
                if (not.ValueKind == JsonValueKind.Array)
                {
                    errors.Add("'not' must be a single predicate object, not an array.");
                    return;
                }

                ValidatePredicateNode(not, errors);
                return;
            }

            if (hasPath)
            {
                ValidateLeafPredicate(path, node, errors);
                return;
            }

            // Shorthand AND-only form: { "<path>": "<value>" }
            foreach (var prop in props)
            {
                if (prop.Value.ValueKind != JsonValueKind.String)
                {
                    errors.Add($"Shorthand predicate values must be strings (path '{prop.Name}').");
                    continue;
                }

                ValidatePath(prop.Name, errors);
            }
        }

        private void ValidatePredicateArray(JsonElement element, string name, List<string> errors)
        {
            if (element.ValueKind != JsonValueKind.Array)
            {
                errors.Add($"'{name}' must be an array.");
                return;
            }

            if (element.GetArrayLength() == 0)
            {
                errors.Add($"'{name}' must be a non-empty array.");
                return;
            }

            foreach (var child in element.EnumerateArray())
            {
                ValidatePredicateNode(child, errors);
            }
        }

        private void ValidateLeafPredicate(JsonElement pathElement, JsonElement node, List<string> errors)
        {
            if (pathElement.ValueKind != JsonValueKind.String)
            {
                errors.Add("Leaf predicate 'path' must be a string.");
                return;
            }

            var path = pathElement.GetString();
            if (string.IsNullOrWhiteSpace(path))
            {
                errors.Add("Leaf predicate 'path' must not be empty.");
                return;
            }

            ValidatePath(path, errors);

            var operatorCount = 0;
            foreach (var prop in node.EnumerateObject())
            {
                if (string.Equals(prop.Name, "path", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (IsSupportedOperator(prop.Name))
                {
                    operatorCount++;
                    ValidateOperatorValue(prop.Name, prop.Value, errors);
                }
                else
                {
                    errors.Add($"Unsupported operator '{prop.Name}'.");
                }
            }

            if (operatorCount == 0)
            {
                errors.Add("Leaf predicate must specify an operator (exists/eq/contains/startsWith/endsWith/in).");
            }
            else if (operatorCount > 1)
            {
                errors.Add("Leaf predicate must specify exactly one operator.");
            }
        }

        private void ValidatePath(string path, List<string> errors)
        {
            if (!_pathValidator.TryValidate(path, out var error))
            {
                errors.Add($"Invalid path '{path}': {error}");
            }
        }

        private static bool IsSupportedOperator(string op)
        {
            return op.Equals("exists", StringComparison.OrdinalIgnoreCase) || op.Equals("eq", StringComparison.OrdinalIgnoreCase) || op.Equals("contains", StringComparison.OrdinalIgnoreCase) || op.Equals("startsWith", StringComparison.OrdinalIgnoreCase) || op.Equals("endsWith", StringComparison.OrdinalIgnoreCase) || op.Equals("in", StringComparison.OrdinalIgnoreCase);
        }

        private static void ValidateOperatorValue(string op, JsonElement value, List<string> errors)
        {
            if (op.Equals("exists", StringComparison.OrdinalIgnoreCase))
            {
                if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    errors.Add("'exists' operator value must be a boolean.");
                }

                return;
            }

            if (op.Equals("in", StringComparison.OrdinalIgnoreCase))
            {
                if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() == 0)
                {
                    errors.Add("'in' operator value must be a non-empty array.");
                    return;
                }

                foreach (var v in value.EnumerateArray())
                {
                    if (v.ValueKind != JsonValueKind.String)
                    {
                        errors.Add("'in' operator array values must be strings.");
                        return;
                    }
                }

                return;
            }

            if (value.ValueKind != JsonValueKind.String)
            {
                errors.Add($"'{op}' operator value must be a string.");
            }
        }
    }
}