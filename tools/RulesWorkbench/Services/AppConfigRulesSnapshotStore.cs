using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using UKHO.Search.Infrastructure.Ingestion.Rules;

namespace RulesWorkbench.Services
{
    /// <summary>
    /// Provides an editable snapshot of ingestion rules loaded from App Configuration for the RulesWorkbench UI.
    /// </summary>
    public sealed class AppConfigRulesSnapshotStore
    {
        private readonly IConfiguration _configuration;
        private readonly IRuleJsonValidator _validator;
        private readonly ILogger<AppConfigRulesSnapshotStore> _logger;

        private readonly object _gate = new();
        private IReadOnlyList<RulesWorkbenchRuleEntry>? _entries;
        private readonly Dictionary<string, string> _overrides = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="AppConfigRulesSnapshotStore"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration used to read App Configuration-backed rules.</param>
        /// <param name="validator">The validator used to check edited rule JSON before it is projected into the snapshot.</param>
        /// <param name="logger">The logger that records snapshot loading diagnostics.</param>
        public AppConfigRulesSnapshotStore(IConfiguration configuration, IRuleJsonValidator validator, ILogger<AppConfigRulesSnapshotStore> logger)
        {
            // Retain configuration so the store can rebuild its snapshot from the latest App Configuration-backed values on demand.
            _configuration = configuration;

            // Retain the validator so edited rules are checked before they are surfaced back to the UI.
            _validator = validator;

            // Retain the logger so load operations can report the effective namespace and rule counts.
            _logger = logger;
        }

        /// <summary>
        /// Returns the current RulesWorkbench snapshot, optionally filtered by rule identifier or description text.
        /// </summary>
        /// <param name="query">The optional case-insensitive query string used to filter the snapshot.</param>
        /// <returns>The full or filtered list of rule entries currently visible to the tool.</returns>
        public IReadOnlyList<RulesWorkbenchRuleEntry> GetRules(string? query)
        {
            // Load or reuse the cached snapshot before applying any optional client-side filtering.
            var entries = LoadAll();

            if (string.IsNullOrWhiteSpace(query))
            {
                return entries;
            }

            // Apply the same case-insensitive rule-id and description filtering used by the RulesWorkbench search box.
            var q = query.Trim().ToLowerInvariant();
            return entries.Where(e => (!string.IsNullOrWhiteSpace(e.RuleId) && e.RuleId.ToLowerInvariant().Contains(q, StringComparison.Ordinal))
                                      || (TryGetDescription(e.RuleJson)?.ToLowerInvariant().Contains(q, StringComparison.Ordinal) ?? false))
                          .ToArray();
        }

        /// <summary>
        /// Attempts to extract the optional description text from a rule JSON node.
        /// </summary>
        /// <param name="node">The rule JSON node to inspect.</param>
        /// <returns>The description when present; otherwise, <see langword="null"/>.</returns>
        private static string? TryGetDescription(JsonNode? node)
        {
            if (node is not JsonObject obj)
            {
                return null;
            }

            return obj["description"]?.GetValue<string?>() ?? obj["Description"]?.GetValue<string?>();
        }

        /// <summary>
        /// Attempts to extract the optional title text from a rule JSON node.
        /// </summary>
        /// <param name="node">The rule JSON node to inspect.</param>
        /// <returns>The title when present; otherwise, <see langword="null"/>.</returns>
        private static string? TryGetTitle(JsonNode? node)
        {
            if (node is not JsonObject obj)
            {
                return null;
            }

            return obj["title"]?.GetValue<string?>() ?? obj["Title"]?.GetValue<string?>();
        }

        /// <summary>
        /// Attempts to extract and normalize the optional rule context from a rule JSON node.
        /// </summary>
        /// <param name="node">The rule JSON node to inspect.</param>
        /// <returns>The normalized lowercase context when present; otherwise, <see langword="null"/>.</returns>
        private static string? TryGetContext(JsonNode? node)
        {
            if (node is not JsonObject obj)
            {
                return null;
            }

            var context = obj["context"]?.GetValue<string?>() ?? obj["Context"]?.GetValue<string?>();
            if (string.IsNullOrWhiteSpace(context))
            {
                return null;
            }

            return context.Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Updates the in-memory JSON for a selected rule after validating the edited payload.
        /// </summary>
        /// <param name="provider">The logical provider that owns the edited rule.</param>
        /// <param name="ruleId">The provider-relative rule identifier to update.</param>
        /// <param name="json">The edited rule JSON to validate and cache.</param>
        /// <returns>A tuple describing whether the updated JSON is valid and, when invalid, why validation failed.</returns>
        public (bool IsValid, string? ErrorMessage) UpdateRuleJson(string provider, string ruleId, string json)
        {
            // Validate the unwrapped rule payload so the in-memory editor view stays aligned with runtime validation rules.
            var validation = _validator.Validate(UnwrapRuleJson(json));
            if (!validation.IsValid)
            {
                return (false, validation.ErrorMessage);
            }

            // Cache the override beneath the namespace-aware App Configuration key so subsequent snapshot reads project the edited rule.
            lock (_gate)
            {
                var key = IngestionRuleConfigurationPath.BuildRuleKey(provider, ruleId);
                _overrides[key] = json;
                _entries = null;
            }

            return (true, null);
        }

        /// <summary>
        /// Unwraps repository-style rule documents into the inner runtime rule payload when possible.
        /// </summary>
        /// <param name="json">The rule JSON to inspect.</param>
        /// <returns>The unwrapped rule JSON when the wrapper shape is present; otherwise, the original JSON.</returns>
        private static string UnwrapRuleJson(string json)
        {
            try
            {
                // Parse and unwrap the rule document so validation works against the same inner shape the runtime evaluates.
                var node = JsonNode.Parse(json);
                var unwrapped = UnwrapRuleNode(node);
                return unwrapped?.ToJsonString() ?? json;
            }
            catch (JsonException)
            {
                // Leave malformed JSON unchanged so the caller receives the original parse failure from the validator.
                return json;
            }
        }

        /// <summary>
        /// Loads and caches the full set of App Configuration-backed ingestion rules for RulesWorkbench.
        /// </summary>
        /// <returns>The cached or newly built list of rule entries.</returns>
        private IReadOnlyList<RulesWorkbenchRuleEntry> LoadAll()
        {
            lock (_gate)
            {
                if (_entries is not null)
                {
                    return _entries;
                }

                // Build a fresh snapshot so the UI reflects the current namespace-aware App Configuration rules and local overrides.
                var list = new List<RulesWorkbenchRuleEntry>();
                var rulesRoot = _configuration.GetSection(IngestionRuleConfigurationPath.IngestionRulesRoot);

                foreach (var ruleSection in EnumerateRuleSections(rulesRoot))
                {
                    if (!RuleKeyParser.TryParse(ruleSection.Path, out var provider, out var ruleId))
                    {
                        continue;
                    }

                    // Rebuild the canonical key from the normalized provider and rule identifier so the UI always shows the active contract.
                    var key = IngestionRuleConfigurationPath.BuildRuleKey(provider, ruleId);
                    var json = _overrides.TryGetValue(key, out var overrideJson)
                        ? overrideJson
                        : ruleSection.Value;

                    JsonNode? node = null;
                    var isValid = true;
                    string? error = null;

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        isValid = false;
                        error = "Rule JSON is empty.";
                    }
                    else
                    {
                        try
                        {
                            // Parse the rule JSON once so the store can project context, title, and validation details for the UI.
                            node = UnwrapRuleNode(JsonNode.Parse(json));
                        }
                        catch (JsonException ex)
                        {
                            isValid = false;
                            error = ex.Message;
                        }

                        if (isValid)
                        {
                            // Validate the unwrapped payload so the Rules page surfaces the same rule-shape problems as the runtime.
                            var validation = _validator.Validate(UnwrapRuleJson(json));
                            if (!validation.IsValid)
                            {
                                isValid = false;
                                error = validation.ErrorMessage;
                            }
                        }
                    }

                    if (node is JsonObject obj && obj.ContainsKey("id") == false)
                    {
                        // Inject the rule identifier when the stored JSON omits it so the editor always sees the effective id.
                        obj["id"] = ruleId;
                    }

                    if (!isValid && string.IsNullOrWhiteSpace(error) && string.IsNullOrWhiteSpace(TryGetTitle(node)))
                    {
                        error = "Rule JSON must include a non-empty 'title' property.";
                    }

                    list.Add(new RulesWorkbenchRuleEntry(list.Count, provider, ruleId, key, TryGetContext(node), node, isValid, error));
                }

                // Record the namespace explicitly so diagnostics distinguish the active rules:ingestion path from stale legacy keys.
                _logger.LogInformation("Loaded rules from App Configuration for RulesWorkbench. Namespace={Namespace} RuleCount={RuleCount}", IngestionRuleConfigurationPath.IngestionRulesRoot, list.Count);

                _entries = list;
                return _entries;
            }
        }

        /// <summary>
        /// Recursively enumerates leaf configuration sections that hold individual rule JSON values.
        /// </summary>
        /// <param name="currentSection">The configuration section to walk.</param>
        /// <returns>The leaf sections that represent individual App Configuration rule entries.</returns>
        private static IEnumerable<IConfigurationSection> EnumerateRuleSections(IConfigurationSection currentSection)
        {
            // Snapshot the children so the method can distinguish leaf rule values from intermediate grouping sections.
            var childSections = currentSection.GetChildren().ToArray();
            if (childSections.Length == 0)
            {
                yield return currentSection;
                yield break;
            }

            foreach (var childSection in childSections)
            {
                foreach (var nestedRuleSection in EnumerateRuleSections(childSection))
                {
                    yield return nestedRuleSection;
                }
            }
        }

        /// <summary>
        /// Unwraps repository-style rule documents into the inner runtime rule payload when possible.
        /// </summary>
        /// <param name="node">The parsed rule JSON node to inspect.</param>
        /// <returns>The unwrapped rule node when the wrapper shape is present; otherwise, the original node.</returns>
        private static JsonNode? UnwrapRuleNode(JsonNode? node)
        {
            if (node is not JsonObject obj)
            {
                return node;
            }

            // Support rule file document shape: { "schemaVersion": "1.0", "rule": { ... } }
            var unwrapped = obj["rule"] ?? obj["Rule"];
            return unwrapped is JsonObject ? unwrapped : node;
        }
    }
}
