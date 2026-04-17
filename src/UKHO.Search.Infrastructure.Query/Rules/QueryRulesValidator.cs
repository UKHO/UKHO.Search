using System.Text.Json;
using UKHO.Search.Query.Models;

namespace UKHO.Search.Infrastructure.Query.Rules
{
    /// <summary>
    /// Validates raw query-rule documents and normalizes them into the runtime rule snapshot.
    /// </summary>
    internal sealed class QueryRulesValidator
    {
        internal const string SupportedSchemaVersion = "1.0";

        private static readonly HashSet<string> SupportedPredicatePaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "input.rawText",
            "input.normalizedText",
            "input.cleanedText",
            "input.tokens[*]",
            "input.residualText",
            "input.residualTokens[*]",
            "extracted.temporal.years[*]"
        };

        private static readonly HashSet<string> SupportedModelFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "keywords",
            "authority",
            "region",
            "format",
            "majorVersion",
            "minorVersion",
            "category",
            "series",
            "instance",
            "title"
        };

        private static readonly HashSet<string> SupportedSortFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "majorVersion",
            "minorVersion"
        };

        private static readonly HashSet<string> SupportedFilterFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "keywords",
            "authority",
            "region",
            "format",
            "majorVersion",
            "minorVersion",
            "category",
            "series",
            "instance",
            "title"
        };

        private static readonly HashSet<string> SupportedBoostFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "keywords",
            "authority",
            "region",
            "format",
            "majorVersion",
            "minorVersion",
            "category",
            "series",
            "instance",
            "title",
            "searchText",
            "content"
        };

        /// <summary>
        /// Validates the supplied raw rule documents and converts them into a deterministic runtime snapshot.
        /// </summary>
        /// <param name="documents">The raw query-rule documents that should be validated.</param>
        /// <returns>The validated flat query-rule snapshot.</returns>
        public QueryRulesSnapshot Validate(IEnumerable<QueryRuleDocumentDto> documents)
        {
            ArgumentNullException.ThrowIfNull(documents);

            // Validate each document independently so the resulting runtime snapshot contains only known-good rule definitions.
            var rules = documents.Select(ValidateDocument)
                                 .Where(static rule => rule.Enabled)
                                 .OrderBy(static rule => rule.Id, StringComparer.Ordinal)
                                 .ToArray();

            return new QueryRulesSnapshot
            {
                SchemaVersion = SupportedSchemaVersion,
                Rules = rules
            };
        }

        /// <summary>
        /// Validates one raw query-rule document.
        /// </summary>
        /// <param name="document">The raw document to validate.</param>
        /// <returns>The validated runtime rule definition.</returns>
        private static QueryRuleDefinition ValidateDocument(QueryRuleDocumentDto document)
        {
            ArgumentNullException.ThrowIfNull(document);

            // Fail fast when the wrapper schema version is unsupported because runtime evaluation should never guess at document semantics.
            if (!string.Equals(document.SchemaVersion, SupportedSchemaVersion, StringComparison.Ordinal))
            {
                throw new QueryRulesValidationException($"Unsupported query rule schema version '{document.SchemaVersion}'. Expected '{SupportedSchemaVersion}'.");
            }

            if (document.Rule is null)
            {
                throw new QueryRulesValidationException("A query rule document must contain a rule payload.");
            }

            var rule = document.Rule;
            if (string.IsNullOrWhiteSpace(rule.Id))
            {
                throw new QueryRulesValidationException("A query rule must define a non-empty id.");
            }

            if (string.IsNullOrWhiteSpace(rule.Title))
            {
                throw new QueryRulesValidationException($"Query rule '{rule.Id}' must define a non-empty title.");
            }

            if (rule.If is null)
            {
                throw new QueryRulesValidationException($"Query rule '{rule.Id}' must define an if predicate.");
            }

            if (rule.Then is null)
            {
                throw new QueryRulesValidationException($"Query rule '{rule.Id}' must define a then action block.");
            }

            var predicate = ValidatePredicate(rule.Id, rule.If);
            var modelMutations = ValidateModel(rule.Id, rule.Then.Model);
            var concepts = ValidateConcepts(rule.Id, rule.Then.Concepts);
            var sortHints = ValidateSortHints(rule.Id, rule.Then.SortHints);
            var consume = ValidateConsume(rule.Then.Consume);
            var filters = ValidateFilters(rule.Id, rule.Then.Filters);
            var boosts = ValidateBoosts(rule.Id, rule.Then.Boosts);

            if (modelMutations.Count == 0 && concepts.Count == 0 && sortHints.Count == 0
                && consume.Tokens.Count == 0 && consume.Phrases.Count == 0
                && filters.Count == 0 && boosts.Count == 0)
            {
                throw new QueryRulesValidationException($"Query rule '{rule.Id}' must emit at least one supported action.");
            }

            return new QueryRuleDefinition
            {
                Id = rule.Id.Trim(),
                Title = rule.Title.Trim(),
                Description = rule.Description?.Trim() ?? string.Empty,
                Enabled = rule.Enabled,
                Predicate = predicate,
                ModelMutations = modelMutations,
                Concepts = concepts,
                SortHints = sortHints,
                Consume = consume,
                Filters = filters,
                Boosts = boosts
            };
        }

        /// <summary>
        /// Validates one raw predicate node.
        /// </summary>
        /// <param name="ruleId">The identifier of the rule currently being validated.</param>
        /// <param name="predicate">The raw predicate node to validate.</param>
        /// <returns>The validated predicate node.</returns>
        private static QueryRulePredicate ValidatePredicate(string ruleId, QueryRulePredicateDto predicate)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);
            ArgumentNullException.ThrowIfNull(predicate);

            // Require exactly one supported predicate form so runtime evaluation never has to guess which operator wins.
            var hasAny = predicate.Any.Count > 0;
            var hasEq = !string.IsNullOrWhiteSpace(predicate.Eq);
            var hasContainsPhrase = !string.IsNullOrWhiteSpace(predicate.ContainsPhrase);
            var operatorCount = (hasAny ? 1 : 0) + (hasEq ? 1 : 0) + (hasContainsPhrase ? 1 : 0);

            if (operatorCount != 1)
            {
                throw new QueryRulesValidationException($"Query rule '{ruleId}' must define exactly one supported predicate operator per predicate node.");
            }

            if (hasAny)
            {
                if (!string.IsNullOrWhiteSpace(predicate.Path) || hasEq || hasContainsPhrase)
                {
                    throw new QueryRulesValidationException($"Query rule '{ruleId}' uses an any predicate that also defines path or scalar operators, which is not supported.");
                }

                var children = predicate.Any.Select(child => ValidatePredicate(ruleId, child)).ToArray();
                if (children.Length == 0)
                {
                    throw new QueryRulesValidationException($"Query rule '{ruleId}' defines an empty any predicate.");
                }

                return new QueryRulePredicate
                {
                    Kind = QueryRulePredicateKind.Any,
                    Any = children
                };
            }

            if (string.IsNullOrWhiteSpace(predicate.Path))
            {
                throw new QueryRulesValidationException($"Query rule '{ruleId}' must define a supported predicate path.");
            }

            var normalizedPath = predicate.Path.Trim();
            if (!SupportedPredicatePaths.Contains(normalizedPath))
            {
                throw new QueryRulesValidationException($"Query rule '{ruleId}' uses unsupported predicate path '{normalizedPath}'.");
            }

            if (hasEq)
            {
                return new QueryRulePredicate
                {
                    Kind = QueryRulePredicateKind.Equals,
                    Path = normalizedPath,
                    Value = predicate.Eq.Trim()
                };
            }

            if (!IsTextPath(normalizedPath))
            {
                throw new QueryRulesValidationException($"Query rule '{ruleId}' uses containsPhrase on non-text path '{normalizedPath}'.");
            }

            return new QueryRulePredicate
            {
                Kind = QueryRulePredicateKind.ContainsPhrase,
                Path = normalizedPath,
                Value = NormalizeTextValue(predicate.ContainsPhrase)
            };
        }

        /// <summary>
        /// Validates the raw execution-time filter block.
        /// </summary>
        /// <param name="ruleId">The identifier of the rule currently being validated.</param>
        /// <param name="filters">The raw execution-time filter block to validate.</param>
        /// <returns>The validated filter definitions emitted by the rule.</returns>
        private static IReadOnlyCollection<QueryRuleFilterDefinition> ValidateFilters(string ruleId, QueryRuleFilterDto? filters)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);

            if (filters?.FieldActions is null || filters.FieldActions.Count == 0)
            {
                return Array.Empty<QueryRuleFilterDefinition>();
            }

            // Validate each filter field action so runtime execution receives deterministic non-scoring constraints only on supported fields.
            var definitions = new List<QueryRuleFilterDefinition>();

            foreach (var fieldAction in filters.FieldActions.OrderBy(static item => item.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (!SupportedFilterFields.Contains(fieldAction.Key))
                {
                    throw new QueryRulesValidationException($"Query rule '{ruleId}' uses unsupported filter field '{fieldAction.Key}'.");
                }

                if (fieldAction.Value.ValueKind != JsonValueKind.Object || !fieldAction.Value.TryGetProperty("add", out var addValuesElement) || addValuesElement.ValueKind != JsonValueKind.Array)
                {
                    throw new QueryRulesValidationException($"Query rule '{ruleId}' must express filter field '{fieldAction.Key}' as an object containing an add array.");
                }

                if (IsIntegerField(fieldAction.Key))
                {
                    var integerValues = addValuesElement.EnumerateArray().Select(element => NormalizeIntegerValue(ruleId, fieldAction.Key, element)).ToArray();
                    if (integerValues.Length == 0)
                    {
                        throw new QueryRulesValidationException($"Query rule '{ruleId}' must add at least one value to filter field '{fieldAction.Key}'.");
                    }

                    definitions.Add(new QueryRuleFilterDefinition
                    {
                        FieldName = fieldAction.Key.Trim(),
                        FieldKind = QueryRuleFilterFieldKind.Integer,
                        IntegerValues = integerValues
                    });
                    continue;
                }

                var stringValues = addValuesElement.EnumerateArray().Select(element => NormalizeStringValue(ruleId, fieldAction.Key, element)).ToArray();
                if (stringValues.Length == 0)
                {
                    throw new QueryRulesValidationException($"Query rule '{ruleId}' must add at least one value to filter field '{fieldAction.Key}'.");
                }

                definitions.Add(new QueryRuleFilterDefinition
                {
                    FieldName = fieldAction.Key.Trim(),
                    FieldKind = QueryRuleFilterFieldKind.String,
                    StringValues = stringValues
                });
            }

            return definitions;
        }

        /// <summary>
        /// Validates the raw execution-time boosts.
        /// </summary>
        /// <param name="ruleId">The identifier of the rule currently being validated.</param>
        /// <param name="boosts">The raw boost actions to validate.</param>
        /// <returns>The validated boost definitions emitted by the rule.</returns>
        private static IReadOnlyCollection<QueryRuleBoostDefinition> ValidateBoosts(string ruleId, IReadOnlyCollection<QueryRuleBoostDto> boosts)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);

            if (boosts.Count == 0)
            {
                return Array.Empty<QueryRuleBoostDefinition>();
            }

            // Validate each explicit boost so runtime mapping can translate it without guessing field type, values, or scoring weight.
            return boosts.Select(boost =>
            {
                if (string.IsNullOrWhiteSpace(boost.Field))
                {
                    throw new QueryRulesValidationException($"Query rule '{ruleId}' contains a boost action with missing field.");
                }

                var normalizedField = boost.Field.Trim();
                if (!SupportedBoostFields.Contains(normalizedField))
                {
                    throw new QueryRulesValidationException($"Query rule '{ruleId}' uses unsupported boost field '{normalizedField}'.");
                }

                if (boost.Weight <= 0)
                {
                    throw new QueryRulesValidationException($"Query rule '{ruleId}' boost for field '{normalizedField}' must use a positive weight.");
                }

                if (IsAnalyzedField(normalizedField))
                {
                    if (string.IsNullOrWhiteSpace(boost.Text))
                    {
                        throw new QueryRulesValidationException($"Query rule '{ruleId}' boost for analyzed field '{normalizedField}' must define text.");
                    }

                    var matchingMode = NormalizeBoostMatchingMode(ruleId, normalizedField, boost.MatchingMode, allowAnalyzedText: true);
                    if (matchingMode != QueryExecutionBoostMatchingMode.AnalyzedText)
                    {
                        throw new QueryRulesValidationException($"Query rule '{ruleId}' boost for analyzed field '{normalizedField}' must use analyzedText matching mode.");
                    }

                    return new QueryRuleBoostDefinition
                    {
                        FieldName = normalizedField,
                        MatchingMode = QueryExecutionBoostMatchingMode.AnalyzedText,
                        Text = NormalizeTextValue(boost.Text),
                        Weight = boost.Weight
                    };
                }

                var resolvedMatchingMode = NormalizeBoostMatchingMode(ruleId, normalizedField, boost.MatchingMode, allowAnalyzedText: false);
                if (resolvedMatchingMode != QueryExecutionBoostMatchingMode.ExactTerms)
                {
                    throw new QueryRulesValidationException($"Query rule '{ruleId}' boost for exact field '{normalizedField}' must use exactTerms matching mode.");
                }

                if (IsIntegerField(normalizedField))
                {
                    var integerValues = boost.Values.Where(static value => !string.IsNullOrWhiteSpace(value))
                                                    .Select(value =>
                                                    {
                                                        if (!int.TryParse(value, out var parsedValue))
                                                        {
                                                            throw new QueryRulesValidationException($"Query rule '{ruleId}' boost for field '{normalizedField}' contains non-integer value '{value}'.");
                                                        }

                                                        return parsedValue;
                                                    })
                                                    .ToArray();
                    if (integerValues.Length == 0)
                    {
                        throw new QueryRulesValidationException($"Query rule '{ruleId}' boost for field '{normalizedField}' must define at least one integer value.");
                    }

                    return new QueryRuleBoostDefinition
                    {
                        FieldName = normalizedField,
                        MatchingMode = QueryExecutionBoostMatchingMode.ExactTerms,
                        IntegerValues = integerValues,
                        Weight = boost.Weight
                    };
                }

                var stringValues = boost.Values.Where(static value => !string.IsNullOrWhiteSpace(value))
                                              .Select(static value => value.Trim().ToLowerInvariant())
                                              .ToArray();
                if (stringValues.Length == 0)
                {
                    throw new QueryRulesValidationException($"Query rule '{ruleId}' boost for field '{normalizedField}' must define at least one string value.");
                }

                return new QueryRuleBoostDefinition
                {
                    FieldName = normalizedField,
                    MatchingMode = QueryExecutionBoostMatchingMode.ExactTerms,
                    StringValues = stringValues,
                    Weight = boost.Weight
                };
            }).ToArray();
        }

        /// <summary>
        /// Parses one raw string value element used by filters and boosts.
        /// </summary>
        /// <param name="ruleId">The identifier of the rule currently being validated.</param>
        /// <param name="fieldName">The canonical field currently being validated.</param>
        /// <param name="element">The raw JSON element that should become one string value.</param>
        /// <returns>The normalized string representation retained by the validated definition.</returns>
        private static string NormalizeStringValue(string ruleId, string fieldName, JsonElement element)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

            // Restrict string-backed filters and boosts to string JSON values so runtime execution can emit exact clauses deterministically.
            if (element.ValueKind != JsonValueKind.String)
            {
                throw new QueryRulesValidationException($"Query rule '{ruleId}' field '{fieldName}' contains unsupported non-string value kind '{element.ValueKind}'.");
            }

            return NormalizeTextValue(element.GetString());
        }

        /// <summary>
        /// Parses one raw integer value element used by filters and boosts.
        /// </summary>
        /// <param name="ruleId">The identifier of the rule currently being validated.</param>
        /// <param name="fieldName">The canonical field currently being validated.</param>
        /// <param name="element">The raw JSON element that should become one integer value.</param>
        /// <returns>The normalized integer value retained by the validated definition.</returns>
        private static int NormalizeIntegerValue(string ruleId, string fieldName, JsonElement element)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

            // Restrict integer-backed filters and boosts to numeric JSON values so runtime execution can emit numeric terms clauses deterministically.
            if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out var value))
            {
                throw new QueryRulesValidationException($"Query rule '{ruleId}' field '{fieldName}' contains unsupported non-integer value kind '{element.ValueKind}'.");
            }

            return value;
        }

        /// <summary>
        /// Normalizes the authored boost matching mode.
        /// </summary>
        /// <param name="ruleId">The identifier of the rule currently being validated.</param>
        /// <param name="fieldName">The target field currently being validated.</param>
        /// <param name="matchingMode">The raw authored matching mode.</param>
        /// <param name="allowAnalyzedText">A value indicating whether analyzed-text matching is valid for the target field.</param>
        /// <returns>The validated execution boost matching mode.</returns>
        private static QueryExecutionBoostMatchingMode NormalizeBoostMatchingMode(string ruleId, string fieldName, string? matchingMode, bool allowAnalyzedText)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

            var normalizedMatchingMode = NormalizeTextValue(matchingMode);
            if (string.IsNullOrWhiteSpace(normalizedMatchingMode))
            {
                return allowAnalyzedText ? QueryExecutionBoostMatchingMode.AnalyzedText : QueryExecutionBoostMatchingMode.ExactTerms;
            }

            return normalizedMatchingMode switch
            {
                "exactterms" => QueryExecutionBoostMatchingMode.ExactTerms,
                "analyzedtext" when allowAnalyzedText => QueryExecutionBoostMatchingMode.AnalyzedText,
                _ => throw new QueryRulesValidationException($"Query rule '{ruleId}' boost for field '{fieldName}' uses unsupported matching mode '{matchingMode}'.")
            };
        }

        /// <summary>
        /// Validates the raw model mutation block.
        /// </summary>
        /// <param name="ruleId">The identifier of the rule currently being validated.</param>
        /// <param name="model">The raw model mutation block to validate.</param>
        /// <returns>The validated model mutations emitted by the rule.</returns>
        private static IReadOnlyCollection<QueryRuleModelMutation> ValidateModel(string ruleId, QueryRuleModelDto? model)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);

            if (model?.FieldActions is null || model.FieldActions.Count == 0)
            {
                return Array.Empty<QueryRuleModelMutation>();
            }

            // Validate each field action so the runtime only receives canonical model mutations with supported field names and values.
            var mutations = new List<QueryRuleModelMutation>();

            foreach (var fieldAction in model.FieldActions.OrderBy(static item => item.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (!SupportedModelFields.Contains(fieldAction.Key))
                {
                    throw new QueryRulesValidationException($"Query rule '{ruleId}' uses unsupported model field '{fieldAction.Key}'.");
                }

                if (fieldAction.Value.ValueKind != JsonValueKind.Object || !fieldAction.Value.TryGetProperty("add", out var addValuesElement))
                {
                    throw new QueryRulesValidationException($"Query rule '{ruleId}' must express model field '{fieldAction.Key}' as an object containing an add array.");
                }

                if (addValuesElement.ValueKind != JsonValueKind.Array)
                {
                    throw new QueryRulesValidationException($"Query rule '{ruleId}' must express model field '{fieldAction.Key}' add values as an array.");
                }

                var addValues = addValuesElement.EnumerateArray().Select(element => NormalizeModelValue(ruleId, fieldAction.Key, element)).ToArray();
                if (addValues.Length == 0)
                {
                    throw new QueryRulesValidationException($"Query rule '{ruleId}' must add at least one value to model field '{fieldAction.Key}'.");
                }

                mutations.Add(new QueryRuleModelMutation
                {
                    FieldName = fieldAction.Key.Trim(),
                    AddValues = addValues
                });
            }

            return mutations;
        }

        /// <summary>
        /// Validates the raw concept actions.
        /// </summary>
        /// <param name="ruleId">The identifier of the rule currently being validated.</param>
        /// <param name="concepts">The raw concept actions to validate.</param>
        /// <returns>The validated concept outputs emitted by the rule.</returns>
        private static IReadOnlyCollection<QueryRuleConceptDefinition> ValidateConcepts(string ruleId, IReadOnlyCollection<QueryRuleConceptDto> concepts)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);

            if (concepts.Count == 0)
            {
                return Array.Empty<QueryRuleConceptDefinition>();
            }

            // Validate concept identifiers and keyword expansions so rule evaluation can emit deterministic extracted concept signals.
            return concepts.Select(concept =>
            {
                if (string.IsNullOrWhiteSpace(concept.Id))
                {
                    throw new QueryRulesValidationException($"Query rule '{ruleId}' contains a concept action with missing id.");
                }

                var keywordExpansions = concept.KeywordExpansions.Where(static value => !string.IsNullOrWhiteSpace(value))
                                                                .Select(static value => value.Trim().ToLowerInvariant())
                                                                .ToArray();
                if (keywordExpansions.Length == 0)
                {
                    throw new QueryRulesValidationException($"Query rule '{ruleId}' concept '{concept.Id}' must define at least one keyword expansion.");
                }

                return new QueryRuleConceptDefinition
                {
                    Id = concept.Id.Trim(),
                    MatchedTextTemplate = concept.MatchedText?.Trim() ?? string.Empty,
                    KeywordExpansions = keywordExpansions
                };
            }).ToArray();
        }

        /// <summary>
        /// Validates the raw sort-hint actions.
        /// </summary>
        /// <param name="ruleId">The identifier of the rule currently being validated.</param>
        /// <param name="sortHints">The raw sort-hint actions to validate.</param>
        /// <returns>The validated sort-hint outputs emitted by the rule.</returns>
        private static IReadOnlyCollection<QueryRuleSortHintDefinition> ValidateSortHints(string ruleId, IReadOnlyCollection<QueryRuleSortHintDto> sortHints)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);

            if (sortHints.Count == 0)
            {
                return Array.Empty<QueryRuleSortHintDefinition>();
            }

            // Validate sort fields and order so execution directives can be generated deterministically from the validated rule model.
            return sortHints.Select(sortHint =>
            {
                if (string.IsNullOrWhiteSpace(sortHint.Id))
                {
                    throw new QueryRulesValidationException($"Query rule '{ruleId}' contains a sort hint with missing id.");
                }

                var fields = sortHint.Fields.Where(static value => !string.IsNullOrWhiteSpace(value))
                                            .Select(static value => value.Trim())
                                            .ToArray();
                if (fields.Length == 0)
                {
                    throw new QueryRulesValidationException($"Query rule '{ruleId}' sort hint '{sortHint.Id}' must define at least one field.");
                }

                foreach (var field in fields)
                {
                    if (!SupportedSortFields.Contains(field))
                    {
                        throw new QueryRulesValidationException($"Query rule '{ruleId}' sort hint '{sortHint.Id}' uses unsupported field '{field}'.");
                    }
                }

                return new QueryRuleSortHintDefinition
                {
                    Id = sortHint.Id.Trim(),
                    MatchedTextTemplate = sortHint.MatchedText?.Trim() ?? string.Empty,
                    Fields = fields,
                    Direction = ParseSortDirection(ruleId, sortHint.Id, sortHint.Order)
                };
            }).ToArray();
        }

        /// <summary>
        /// Validates the raw consume block.
        /// </summary>
        /// <param name="consume">The raw consume block to validate.</param>
        /// <returns>The validated consume directives emitted by the rule.</returns>
        private static QueryRuleConsumeDefinition ValidateConsume(QueryRuleConsumeDto? consume)
        {
            if (consume is null)
            {
                return new QueryRuleConsumeDefinition();
            }

            // Normalize consume directives now so residual processing can compare lowercased cleaned tokens without extra rule-specific logic.
            return new QueryRuleConsumeDefinition
            {
                Tokens = consume.Tokens.Where(static value => !string.IsNullOrWhiteSpace(value))
                                       .Select(static value => NormalizeTextValue(value))
                                       .ToArray(),
                Phrases = consume.Phrases.Where(static value => !string.IsNullOrWhiteSpace(value))
                                         .Select(static value => NormalizeTextValue(value))
                                         .ToArray()
            };
        }

        /// <summary>
        /// Parses one raw model value element.
        /// </summary>
        /// <param name="ruleId">The identifier of the rule currently being validated.</param>
        /// <param name="fieldName">The canonical field currently being validated.</param>
        /// <param name="element">The raw JSON element that should become one model mutation value.</param>
        /// <returns>The normalized string representation retained by the validated mutation.</returns>
        private static string NormalizeModelValue(string ruleId, string fieldName, JsonElement element)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

            // Convert supported primitive JSON values into deterministic string form so runtime mutation logic can parse them consistently.
            return element.ValueKind switch
            {
                JsonValueKind.String => NormalizeTextValue(element.GetString()),
                JsonValueKind.Number => element.ToString(),
                _ => throw new QueryRulesValidationException($"Query rule '{ruleId}' model field '{fieldName}' contains unsupported value kind '{element.ValueKind}'.")
            };
        }

        /// <summary>
        /// Parses one raw sort order.
        /// </summary>
        /// <param name="ruleId">The identifier of the rule currently being validated.</param>
        /// <param name="sortHintId">The identifier of the sort hint currently being validated.</param>
        /// <param name="order">The raw textual sort order.</param>
        /// <returns>The validated execution sort direction.</returns>
        private static QueryExecutionSortDirection ParseSortDirection(string ruleId, string sortHintId, string? order)
        {
            var normalizedOrder = NormalizeTextValue(order);
            return normalizedOrder switch
            {
                "asc" => QueryExecutionSortDirection.Ascending,
                "desc" => QueryExecutionSortDirection.Descending,
                _ => throw new QueryRulesValidationException($"Query rule '{ruleId}' sort hint '{sortHintId}' uses unsupported order '{order}'.")
            };
        }

        /// <summary>
        /// Normalizes a text value used by the rule contract.
        /// </summary>
        /// <param name="value">The raw value to normalize.</param>
        /// <returns>The trimmed lowercase value.</returns>
        private static string NormalizeTextValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            // Lowercase authored text values so flat query rules behave consistently with the cleaned lowercase query input.
            return value.Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Determines whether one supported predicate path resolves to a text surface.
        /// </summary>
        /// <param name="path">The validated predicate path.</param>
        /// <returns><see langword="true" /> when the path resolves to text; otherwise, <see langword="false" />.</returns>
        private static bool IsTextPath(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            // Restrict containsPhrase to the known text surfaces so phrase matching never attempts to search scalar collections.
            return string.Equals(path, "input.rawText", StringComparison.OrdinalIgnoreCase)
                || string.Equals(path, "input.normalizedText", StringComparison.OrdinalIgnoreCase)
                || string.Equals(path, "input.cleanedText", StringComparison.OrdinalIgnoreCase)
                || string.Equals(path, "input.residualText", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether one canonical field is integer-backed.
        /// </summary>
        /// <param name="fieldName">The canonical field to inspect.</param>
        /// <returns><see langword="true" /> when the field is integer-backed; otherwise, <see langword="false" />.</returns>
        private static bool IsIntegerField(string fieldName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

            // Keep the integer-backed field list explicit so validation and runtime mapping stay aligned with the canonical index contract.
            return string.Equals(fieldName, "majorVersion", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "minorVersion", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether one canonical field is analyzed text.
        /// </summary>
        /// <param name="fieldName">The canonical field to inspect.</param>
        /// <returns><see langword="true" /> when the field is analyzed text; otherwise, <see langword="false" />.</returns>
        private static bool IsAnalyzedField(string fieldName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

            // Restrict analyzed boosts to the known analyzed fields exposed by the canonical index mapping.
            return string.Equals(fieldName, "searchText", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "content", StringComparison.OrdinalIgnoreCase);
        }
    }
}
