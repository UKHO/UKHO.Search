using Microsoft.Extensions.Logging;
using UKHO.Search.Query.Abstractions;
using UKHO.Search.Query.Models;

namespace UKHO.Search.Services.Query.Rules
{
    /// <summary>
    /// Evaluates validated flat query rules against normalized input and typed extracted signals.
    /// </summary>
    public sealed class ConfigurationQueryRuleEngine : IQueryRuleEngine
    {
        private readonly IQueryRulesCatalog _catalog;
        private readonly ILogger<ConfigurationQueryRuleEngine> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationQueryRuleEngine"/> class.
        /// </summary>
        /// <param name="catalog">The validated query-rule catalog used to obtain the current flat rule snapshot.</param>
        /// <param name="logger">The logger that records rule-match and rule-application diagnostics.</param>
        public ConfigurationQueryRuleEngine(IQueryRulesCatalog catalog, ILogger<ConfigurationQueryRuleEngine> logger)
        {
            // Retain the rule catalog and logger once so every evaluation request follows the same validated rule snapshot and diagnostics pattern.
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Evaluates the current query-rule snapshot against the supplied planning state.
        /// </summary>
        /// <param name="input">The normalized query input snapshot that the rule engine should inspect.</param>
        /// <param name="extracted">The typed extracted signals that were already derived from the query text.</param>
        /// <param name="model">The seeded canonical query model that the rule engine may augment.</param>
        /// <param name="cancellationToken">The cancellation token that stops evaluation when the caller no longer needs the result.</param>
        /// <returns>The rule-evaluation result that carries the shaped extracted signals, canonical model, execution directives, diagnostics, and residual content.</returns>
        public Task<QueryRuleEvaluationResult> EvaluateAsync(QueryInputSnapshot input, QueryExtractedSignals extracted, CanonicalQueryModel model, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(extracted);
            ArgumentNullException.ThrowIfNull(model);

            // Observe cancellation before any rule work begins so callers can stop planning cleanly.
            cancellationToken.ThrowIfCancellationRequested();

            // Snapshot the current rules once per request so one evaluation uses a stable validated rule set even if refresh happens concurrently.
            var snapshot = _catalog.GetSnapshot();
            var residualTokens = input.ResidualTokens.ToList();
            var matchedRuleIds = new List<string>();
            var appliedFilters = new List<string>();
            var appliedBoosts = new List<string>();
            var appliedSorts = new List<string>();
            var conceptSignals = extracted.Concepts.ToList();
            var sortHintSignals = extracted.SortHints.ToList();
            var executionFilters = new List<QueryExecutionFilterDirective>();
            var executionBoosts = new List<QueryExecutionBoostDirective>();
            var executionSorts = new List<QueryExecutionSortDirective>();
            var keywords = model.Keywords.ToList();
            var authority = model.Authority.ToList();
            var region = model.Region.ToList();
            var format = model.Format.ToList();
            var majorVersion = model.MajorVersion.ToList();
            var minorVersion = model.MinorVersion.ToList();
            var category = model.Category.ToList();
            var series = model.Series.ToList();
            var instance = model.Instance.ToList();
            var title = model.Title.ToList();
            var catalogDiagnostics = _catalog.GetDiagnostics();

            foreach (var rule in snapshot.Rules)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Match each rule against the stable normalized input and typed extraction snapshot before applying any mutations.
                var match = EvaluatePredicate(rule.Predicate, input, extracted);
                if (!match.IsMatch)
                {
                    continue;
                }

                matchedRuleIds.Add(rule.Id);
                ApplyModelMutations(rule.Id, rule.ModelMutations, keywords, authority, region, format, majorVersion, minorVersion, category, series, instance, title);
                ApplyConcepts(rule.Concepts, match.MatchedValue, conceptSignals);
                ApplyFilters(rule.Filters, executionFilters, appliedFilters);
                ApplyBoosts(rule.Boosts, executionBoosts, appliedBoosts);
                ApplySortHints(rule.SortHints, match.MatchedValue, sortHintSignals, executionSorts, appliedSorts);
                ApplyConsumeDirectives(rule.Consume, residualTokens);

                // Record each matched rule with the value that satisfied it so contributors can diagnose query-plan shaping decisions.
                _logger.LogInformation("Applied query rule. RuleId={RuleId} MatchedValue={MatchedValue}", rule.Id, match.MatchedValue);
            }

            var residualText = string.Join(' ', residualTokens);
            var result = new QueryRuleEvaluationResult
            {
                Extracted = new QueryExtractedSignals
                {
                    Temporal = extracted.Temporal,
                    Numbers = extracted.Numbers.ToArray(),
                    Concepts = conceptSignals.ToArray(),
                    SortHints = sortHintSignals.ToArray()
                },
                Model = new CanonicalQueryModel
                {
                    Keywords = keywords.ToArray(),
                    Authority = authority.ToArray(),
                    Region = region.ToArray(),
                    Format = format.ToArray(),
                    MajorVersion = majorVersion.ToArray(),
                    MinorVersion = minorVersion.ToArray(),
                    Category = category.ToArray(),
                    Series = series.ToArray(),
                    Instance = instance.ToArray(),
                    SearchText = model.SearchText,
                    Content = model.Content,
                    Title = title.ToArray()
                },
                ResidualText = residualText,
                ResidualTokens = residualTokens.ToArray(),
                Execution = new QueryExecutionDirectives
                {
                    Filters = executionFilters.ToArray(),
                    Boosts = executionBoosts.ToArray(),
                    Sorts = executionSorts.ToArray()
                },
                Diagnostics = new QueryPlanDiagnostics
                {
                    MatchedRuleIds = matchedRuleIds.ToArray(),
                    AppliedFilters = appliedFilters.ToArray(),
                    AppliedBoosts = appliedBoosts.ToArray(),
                    AppliedSorts = appliedSorts.ToArray(),
                    RuleCatalogLoadedAtUtc = catalogDiagnostics.LoadedAtUtc
                }
            };

            // Log the aggregate outcome so startup and runtime diagnostics can see how many rules shaped the plan and what residual text remained.
            _logger.LogInformation(
                "Evaluated query rules. RuleCount={RuleCount} MatchedRuleCount={MatchedRuleCount} ResidualTokenCount={ResidualTokenCount}",
                snapshot.Rules.Count,
                matchedRuleIds.Count,
                residualTokens.Count);

            return Task.FromResult(result);
        }

        /// <summary>
        /// Evaluates one validated predicate tree against the supplied input and extracted signals.
        /// </summary>
        /// <param name="predicate">The validated predicate that should be evaluated.</param>
        /// <param name="input">The normalized query input snapshot currently being planned.</param>
        /// <param name="extracted">The typed extracted signals currently available to the planner.</param>
        /// <returns>The match result describing whether the predicate matched and which value satisfied it.</returns>
        private static QueryRuleMatchResult EvaluatePredicate(QueryRulePredicate predicate, QueryInputSnapshot input, QueryExtractedSignals extracted)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(extracted);

            // Route predicate evaluation by validated predicate kind so runtime logic stays small and deterministic.
            return predicate.Kind switch
            {
                QueryRulePredicateKind.Any => EvaluateAnyPredicate(predicate, input, extracted),
                QueryRulePredicateKind.Equals => EvaluateEqualsPredicate(predicate, input, extracted),
                QueryRulePredicateKind.ContainsPhrase => EvaluateContainsPhrasePredicate(predicate, input),
                _ => QueryRuleMatchResult.NoMatch()
            };
        }

        /// <summary>
        /// Evaluates a validated any-group predicate.
        /// </summary>
        /// <param name="predicate">The any-group predicate whose children should be evaluated.</param>
        /// <param name="input">The normalized query input snapshot currently being planned.</param>
        /// <param name="extracted">The typed extracted signals currently available to the planner.</param>
        /// <returns>The first successful child match, or a no-match result when all children fail.</returns>
        private static QueryRuleMatchResult EvaluateAnyPredicate(QueryRulePredicate predicate, QueryInputSnapshot input, QueryExtractedSignals extracted)
        {
            // Return the first matching child so templates can reuse the specific value that satisfied the any-group.
            foreach (var childPredicate in predicate.Any)
            {
                var childResult = EvaluatePredicate(childPredicate, input, extracted);
                if (childResult.IsMatch)
                {
                    return childResult;
                }
            }

            return QueryRuleMatchResult.NoMatch();
        }

        /// <summary>
        /// Evaluates a validated equality predicate.
        /// </summary>
        /// <param name="predicate">The equality predicate that should be evaluated.</param>
        /// <param name="input">The normalized query input snapshot currently being planned.</param>
        /// <param name="extracted">The typed extracted signals currently available to the planner.</param>
        /// <returns>The match result describing whether one resolved value matched the expected equality value.</returns>
        private static QueryRuleMatchResult EvaluateEqualsPredicate(QueryRulePredicate predicate, QueryInputSnapshot input, QueryExtractedSignals extracted)
        {
            // Resolve the repository-owned values at the configured path and compare them case-insensitively against the expected value.
            foreach (var resolvedValue in ResolveValues(predicate.Path, input, extracted))
            {
                if (string.Equals(resolvedValue, predicate.Value, StringComparison.OrdinalIgnoreCase))
                {
                    return QueryRuleMatchResult.Match(resolvedValue);
                }
            }

            return QueryRuleMatchResult.NoMatch();
        }

        /// <summary>
        /// Evaluates a validated contains-phrase predicate.
        /// </summary>
        /// <param name="predicate">The contains-phrase predicate that should be evaluated.</param>
        /// <param name="input">The normalized query input snapshot currently being planned.</param>
        /// <returns>The match result describing whether the configured phrase was found.</returns>
        private static QueryRuleMatchResult EvaluateContainsPhrasePredicate(QueryRulePredicate predicate, QueryInputSnapshot input)
        {
            var textSurface = ResolveSingleText(predicate.Path, input);
            if (string.IsNullOrWhiteSpace(textSurface) || string.IsNullOrWhiteSpace(predicate.Value))
            {
                return QueryRuleMatchResult.NoMatch();
            }

            // Surround both surfaces with spaces so phrase matching respects token boundaries inside cleaned text.
            var paddedText = $" {textSurface.Trim().ToLowerInvariant()} ";
            var paddedPhrase = $" {predicate.Value.Trim().ToLowerInvariant()} ";
            return paddedText.Contains(paddedPhrase, StringComparison.Ordinal)
                ? QueryRuleMatchResult.Match(predicate.Value)
                : QueryRuleMatchResult.NoMatch();
        }

        /// <summary>
        /// Resolves the repository-owned values exposed by one supported predicate path.
        /// </summary>
        /// <param name="path">The validated predicate path to resolve.</param>
        /// <param name="input">The normalized query input snapshot currently being planned.</param>
        /// <param name="extracted">The typed extracted signals currently available to the planner.</param>
        /// <returns>The resolved scalar values exposed by the path.</returns>
        private static IEnumerable<string> ResolveValues(string path, QueryInputSnapshot input, QueryExtractedSignals extracted)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(extracted);

            // Resolve only the small supported path surface so rule evaluation stays explicit and deterministic.
            return path switch
            {
                "input.rawText" => [input.RawText],
                "input.normalizedText" => [input.NormalizedText],
                "input.cleanedText" => [input.CleanedText],
                "input.tokens[*]" => input.Tokens.Select(static token => token),
                "input.residualText" => [input.ResidualText],
                "input.residualTokens[*]" => input.ResidualTokens.Select(static token => token),
                "extracted.temporal.years[*]" => extracted.Temporal.Years.Select(static year => year.ToString()),
                _ => Array.Empty<string>()
            };
        }

        /// <summary>
        /// Resolves one supported text path into a scalar text surface.
        /// </summary>
        /// <param name="path">The validated text path to resolve.</param>
        /// <param name="input">The normalized query input snapshot currently being planned.</param>
        /// <returns>The resolved scalar text surface.</returns>
        private static string ResolveSingleText(string path, QueryInputSnapshot input)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            ArgumentNullException.ThrowIfNull(input);

            // Return only the supported scalar text surfaces used by contains-phrase predicates.
            return path switch
            {
                "input.rawText" => input.RawText,
                "input.normalizedText" => input.NormalizedText,
                "input.cleanedText" => input.CleanedText,
                "input.residualText" => input.ResidualText,
                _ => string.Empty
            };
        }

        /// <summary>
        /// Applies validated canonical-model mutations to the current mutable field lists.
        /// </summary>
        /// <param name="ruleId">The identifier of the rule currently being applied.</param>
        /// <param name="mutations">The validated model mutations emitted by the rule.</param>
        /// <param name="keywords">The mutable keyword field values being built for the final canonical model.</param>
        /// <param name="authority">The mutable authority field values being built for the final canonical model.</param>
        /// <param name="region">The mutable region field values being built for the final canonical model.</param>
        /// <param name="format">The mutable format field values being built for the final canonical model.</param>
        /// <param name="majorVersion">The mutable major-version field values being built for the final canonical model.</param>
        /// <param name="minorVersion">The mutable minor-version field values being built for the final canonical model.</param>
        /// <param name="category">The mutable category field values being built for the final canonical model.</param>
        /// <param name="series">The mutable series field values being built for the final canonical model.</param>
        /// <param name="instance">The mutable instance field values being built for the final canonical model.</param>
        /// <param name="title">The mutable title field values being built for the final canonical model.</param>
        private void ApplyModelMutations(
            string ruleId,
            IReadOnlyCollection<QueryRuleModelMutation> mutations,
            List<string> keywords,
            List<string> authority,
            List<string> region,
            List<string> format,
            List<int> majorVersion,
            List<int> minorVersion,
            List<string> category,
            List<string> series,
            List<string> instance,
            List<string> title)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);
            ArgumentNullException.ThrowIfNull(mutations);

            // Apply each validated mutation by canonical field name while preserving insertion order and de-duplicating repeated values.
            foreach (var mutation in mutations)
            {
                switch (mutation.FieldName)
                {
                    case "keywords":
                        AddUniqueStrings(keywords, mutation.AddValues.Select(static value => value.ToLowerInvariant()));
                        break;

                    case "authority":
                        AddUniqueStrings(authority, mutation.AddValues.Select(static value => value.ToLowerInvariant()));
                        break;

                    case "region":
                        AddUniqueStrings(region, mutation.AddValues.Select(static value => value.ToLowerInvariant()));
                        break;

                    case "format":
                        AddUniqueStrings(format, mutation.AddValues.Select(static value => value.ToLowerInvariant()));
                        break;

                    case "category":
                        AddUniqueStrings(category, mutation.AddValues.Select(static value => value.ToLowerInvariant()));
                        break;

                    case "series":
                        AddUniqueStrings(series, mutation.AddValues.Select(static value => value.ToLowerInvariant()));
                        break;

                    case "instance":
                        AddUniqueStrings(instance, mutation.AddValues.Select(static value => value.ToLowerInvariant()));
                        break;

                    case "title":
                        AddUniqueStrings(title, mutation.AddValues.Select(static value => value.ToLowerInvariant()));
                        break;

                    case "majorVersion":
                        AddUniqueIntegers(ruleId, mutation.FieldName, mutation.AddValues, majorVersion);
                        break;

                    case "minorVersion":
                        AddUniqueIntegers(ruleId, mutation.FieldName, mutation.AddValues, minorVersion);
                        break;
                }
            }
        }

        /// <summary>
        /// Applies validated concept outputs to the current concept signal list.
        /// </summary>
        /// <param name="conceptDefinitions">The validated concept outputs emitted by the matched rule.</param>
        /// <param name="matchedValue">The specific value that satisfied the rule predicate.</param>
        /// <param name="conceptSignals">The mutable concept signal list being built for the final extracted-signal contract.</param>
        private static void ApplyConcepts(IReadOnlyCollection<QueryRuleConceptDefinition> conceptDefinitions, string matchedValue, List<QueryConceptSignal> conceptSignals)
        {
            ArgumentNullException.ThrowIfNull(conceptDefinitions);
            ArgumentNullException.ThrowIfNull(conceptSignals);

            // Append each concept signal in rule order while avoiding duplicate concept ids in the final extracted signal list.
            foreach (var conceptDefinition in conceptDefinitions)
            {
                if (conceptSignals.Any(existing => string.Equals(existing.Id, conceptDefinition.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                conceptSignals.Add(new QueryConceptSignal
                {
                    Id = conceptDefinition.Id,
                    MatchedText = ExpandMatchedText(conceptDefinition.MatchedTextTemplate, matchedValue),
                    KeywordExpansions = conceptDefinition.KeywordExpansions.ToArray()
                });
            }
        }

        /// <summary>
        /// Applies validated sort-hint outputs to the current extracted signal list and execution directive list.
        /// </summary>
        /// <param name="sortHintDefinitions">The validated sort-hint outputs emitted by the matched rule.</param>
        /// <param name="matchedValue">The specific value that satisfied the rule predicate.</param>
        /// <param name="sortHintSignals">The mutable sort-hint signal list being built for the final extracted-signal contract.</param>
        /// <param name="executionSorts">The mutable execution-sort list being built for the final execution directives.</param>
        private static void ApplySortHints(IReadOnlyCollection<QueryRuleSortHintDefinition> sortHintDefinitions, string matchedValue, List<QuerySortHintSignal> sortHintSignals, List<QueryExecutionSortDirective> executionSorts, List<string> appliedSorts)
        {
            ArgumentNullException.ThrowIfNull(sortHintDefinitions);
            ArgumentNullException.ThrowIfNull(sortHintSignals);
            ArgumentNullException.ThrowIfNull(executionSorts);
            ArgumentNullException.ThrowIfNull(appliedSorts);

            // Append each sort hint in rule order and materialize each referenced field into the execution directive list.
            foreach (var sortHintDefinition in sortHintDefinitions)
            {
                if (!sortHintSignals.Any(existing => string.Equals(existing.Id, sortHintDefinition.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    sortHintSignals.Add(new QuerySortHintSignal
                    {
                        Id = sortHintDefinition.Id,
                        MatchedText = ExpandMatchedText(sortHintDefinition.MatchedTextTemplate, matchedValue),
                        Fields = sortHintDefinition.Fields.ToArray(),
                        Order = sortHintDefinition.Direction == QueryExecutionSortDirection.Ascending ? "asc" : "desc"
                    });
                }

                foreach (var field in sortHintDefinition.Fields)
                {
                    if (executionSorts.Any(existing => string.Equals(existing.FieldName, field, StringComparison.OrdinalIgnoreCase)
                        && existing.Direction == sortHintDefinition.Direction))
                    {
                        continue;
                    }

                    executionSorts.Add(new QueryExecutionSortDirective
                    {
                        FieldName = field,
                        Direction = sortHintDefinition.Direction
                    });
                    appliedSorts.Add($"{field}:{(sortHintDefinition.Direction == QueryExecutionSortDirection.Descending ? "desc" : "asc")}");
                }
            }
        }

        /// <summary>
        /// Applies validated execution-time filters to the current execution directive list.
        /// </summary>
        /// <param name="filterDefinitions">The validated filter definitions emitted by the matched rule.</param>
        /// <param name="executionFilters">The mutable execution filter list being built for the final execution directives.</param>
        /// <param name="appliedFilters">The mutable diagnostics list describing the applied filters.</param>
        private static void ApplyFilters(IReadOnlyCollection<QueryRuleFilterDefinition> filterDefinitions, List<QueryExecutionFilterDirective> executionFilters, List<string> appliedFilters)
        {
            ArgumentNullException.ThrowIfNull(filterDefinitions);
            ArgumentNullException.ThrowIfNull(executionFilters);
            ArgumentNullException.ThrowIfNull(appliedFilters);

            // Materialize each validated filter into the execution directives while preserving rule order and suppressing duplicates.
            foreach (var filterDefinition in filterDefinitions)
            {
                if (executionFilters.Any(existing => string.Equals(existing.FieldName, filterDefinition.FieldName, StringComparison.OrdinalIgnoreCase)
                    && existing.StringValues.SequenceEqual(filterDefinition.StringValues)
                    && existing.IntegerValues.SequenceEqual(filterDefinition.IntegerValues)))
                {
                    continue;
                }

                executionFilters.Add(new QueryExecutionFilterDirective
                {
                    FieldName = filterDefinition.FieldName,
                    StringValues = filterDefinition.StringValues.ToArray(),
                    IntegerValues = filterDefinition.IntegerValues.ToArray()
                });

                appliedFilters.Add(filterDefinition.IntegerValues.Count > 0
                    ? $"{filterDefinition.FieldName}=[{string.Join(',', filterDefinition.IntegerValues)}]"
                    : $"{filterDefinition.FieldName}=[{string.Join(',', filterDefinition.StringValues)}]");
            }
        }

        /// <summary>
        /// Applies validated execution-time boosts to the current execution directive list.
        /// </summary>
        /// <param name="boostDefinitions">The validated boost definitions emitted by the matched rule.</param>
        /// <param name="executionBoosts">The mutable execution boost list being built for the final execution directives.</param>
        /// <param name="appliedBoosts">The mutable diagnostics list describing the applied boosts.</param>
        private static void ApplyBoosts(IReadOnlyCollection<QueryRuleBoostDefinition> boostDefinitions, List<QueryExecutionBoostDirective> executionBoosts, List<string> appliedBoosts)
        {
            ArgumentNullException.ThrowIfNull(boostDefinitions);
            ArgumentNullException.ThrowIfNull(executionBoosts);
            ArgumentNullException.ThrowIfNull(appliedBoosts);

            // Materialize each validated boost into the execution directives while preserving rule order and suppressing duplicates.
            foreach (var boostDefinition in boostDefinitions)
            {
                if (executionBoosts.Any(existing => string.Equals(existing.FieldName, boostDefinition.FieldName, StringComparison.OrdinalIgnoreCase)
                    && existing.MatchingMode == boostDefinition.MatchingMode
                    && string.Equals(existing.Text, boostDefinition.Text, StringComparison.OrdinalIgnoreCase)
                    && existing.Weight.Equals(boostDefinition.Weight)
                    && existing.StringValues.SequenceEqual(boostDefinition.StringValues)
                    && existing.IntegerValues.SequenceEqual(boostDefinition.IntegerValues)))
                {
                    continue;
                }

                executionBoosts.Add(new QueryExecutionBoostDirective
                {
                    FieldName = boostDefinition.FieldName,
                    MatchingMode = boostDefinition.MatchingMode,
                    StringValues = boostDefinition.StringValues.ToArray(),
                    IntegerValues = boostDefinition.IntegerValues.ToArray(),
                    Text = boostDefinition.Text,
                    Weight = boostDefinition.Weight
                });

                appliedBoosts.Add(boostDefinition.MatchingMode == QueryExecutionBoostMatchingMode.AnalyzedText
                    ? $"{boostDefinition.FieldName}:analyzed:{boostDefinition.Text}:{boostDefinition.Weight}"
                    : boostDefinition.IntegerValues.Count > 0
                        ? $"{boostDefinition.FieldName}:exact:[{string.Join(',', boostDefinition.IntegerValues)}]:{boostDefinition.Weight}"
                        : $"{boostDefinition.FieldName}:exact:[{string.Join(',', boostDefinition.StringValues)}]:{boostDefinition.Weight}");
            }
        }

        /// <summary>
        /// Applies validated consume directives to the residual token list.
        /// </summary>
        /// <param name="consume">The validated consume directives emitted by the matched rule.</param>
        /// <param name="residualTokens">The mutable residual token list that should no longer include consumed phrases or tokens.</param>
        private static void ApplyConsumeDirectives(QueryRuleConsumeDefinition consume, List<string> residualTokens)
        {
            ArgumentNullException.ThrowIfNull(consume);
            ArgumentNullException.ThrowIfNull(residualTokens);

            // Remove phrases first so multi-token intent markers disappear before any single-token cleanup runs.
            foreach (var phrase in consume.Phrases)
            {
                RemovePhrase(residualTokens, phrase);
            }

            foreach (var token in consume.Tokens)
            {
                RemoveToken(residualTokens, token);
            }
        }

        /// <summary>
        /// Removes every occurrence of one consumed token from the residual token list.
        /// </summary>
        /// <param name="residualTokens">The mutable residual token list that should be updated.</param>
        /// <param name="token">The lowercased consumed token to remove.</param>
        private static void RemoveToken(List<string> residualTokens, string token)
        {
            ArgumentNullException.ThrowIfNull(residualTokens);
            ArgumentException.ThrowIfNullOrWhiteSpace(token);

            // Remove matching tokens in place so later phrase or default processing sees only the true residual content.
            residualTokens.RemoveAll(existingToken => string.Equals(existingToken, token, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Removes every non-overlapping occurrence of one consumed phrase from the residual token list.
        /// </summary>
        /// <param name="residualTokens">The mutable residual token list that should be updated.</param>
        /// <param name="phrase">The lowercased consumed phrase to remove.</param>
        private static void RemovePhrase(List<string> residualTokens, string phrase)
        {
            ArgumentNullException.ThrowIfNull(residualTokens);
            ArgumentException.ThrowIfNullOrWhiteSpace(phrase);

            var phraseTokens = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (phraseTokens.Length == 0)
            {
                return;
            }

            // Walk the residual token list and remove matching token windows until no further occurrences remain.
            for (var index = 0; index <= residualTokens.Count - phraseTokens.Length;)
            {
                var isMatch = true;
                for (var phraseIndex = 0; phraseIndex < phraseTokens.Length; phraseIndex++)
                {
                    if (!string.Equals(residualTokens[index + phraseIndex], phraseTokens[phraseIndex], StringComparison.OrdinalIgnoreCase))
                    {
                        isMatch = false;
                        break;
                    }
                }

                if (!isMatch)
                {
                    index++;
                    continue;
                }

                residualTokens.RemoveRange(index, phraseTokens.Length);
            }
        }

        /// <summary>
        /// Expands one matched-text template using the predicate value that satisfied the rule.
        /// </summary>
        /// <param name="template">The authored matched-text template.</param>
        /// <param name="matchedValue">The specific predicate value that satisfied the rule.</param>
        /// <returns>The resolved matched text retained on the emitted signal.</returns>
        private static string ExpandMatchedText(string template, string matchedValue)
        {
            // Support the simple $val placeholder used by the specification while leaving literal authored text untouched.
            return string.IsNullOrWhiteSpace(template)
                ? matchedValue
                : template.Replace("$val", matchedValue, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Adds string values to the target list while preserving insertion order and uniqueness.
        /// </summary>
        /// <param name="target">The mutable string list being updated.</param>
        /// <param name="values">The values that should be appended when they are not already present.</param>
        private static void AddUniqueStrings(List<string> target, IEnumerable<string> values)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(values);

            // Preserve the first-seen order because rule-authored keyword expansion order should remain deterministic in tests and diagnostics.
            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value) || target.Any(existing => string.Equals(existing, value, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                target.Add(value);
            }
        }

        /// <summary>
        /// Adds integer values to the target list while preserving insertion order and uniqueness.
        /// </summary>
        /// <param name="ruleId">The identifier of the rule currently being applied.</param>
        /// <param name="fieldName">The canonical field currently being updated.</param>
        /// <param name="values">The raw string values that should be parsed as integers.</param>
        /// <param name="target">The mutable integer list being updated.</param>
        private void AddUniqueIntegers(string ruleId, string fieldName, IEnumerable<string> values, List<int> target)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
            ArgumentNullException.ThrowIfNull(values);
            ArgumentNullException.ThrowIfNull(target);

            // Parse integers defensively even though validation already guarantees numeric values so unexpected runtime corruption is visible in logs.
            foreach (var value in values)
            {
                if (!int.TryParse(value, out var parsedValue))
                {
                    _logger.LogWarning("Skipped invalid integer query-rule model value. RuleId={RuleId} FieldName={FieldName} Value={Value}", ruleId, fieldName, value);
                    continue;
                }

                if (target.Contains(parsedValue))
                {
                    continue;
                }

                target.Add(parsedValue);
            }
        }
    }
}
