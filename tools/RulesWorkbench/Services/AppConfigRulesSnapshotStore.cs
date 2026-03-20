using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RulesWorkbench.Services
{
    public sealed class AppConfigRulesSnapshotStore
    {
        private readonly IConfiguration _configuration;
        private readonly IRuleJsonValidator _validator;
        private readonly ILogger<AppConfigRulesSnapshotStore> _logger;

        private readonly object _gate = new();
        private IReadOnlyList<RulesWorkbenchRuleEntry>? _entries;
        private readonly Dictionary<string, string> _overrides = new(StringComparer.OrdinalIgnoreCase);

        public AppConfigRulesSnapshotStore(IConfiguration configuration, IRuleJsonValidator validator, ILogger<AppConfigRulesSnapshotStore> logger)
        {
            _configuration = configuration;
            _validator = validator;
            _logger = logger;
        }

        public IReadOnlyList<RulesWorkbenchRuleEntry> GetRules(string? query)
        {
            var entries = LoadAll();

            if (string.IsNullOrWhiteSpace(query))
            {
                return entries;
            }

            var q = query.Trim().ToLowerInvariant();
            return entries.Where(e => (!string.IsNullOrWhiteSpace(e.RuleId) && e.RuleId.ToLowerInvariant().Contains(q, StringComparison.Ordinal))
                                      || (TryGetDescription(e.RuleJson)?.ToLowerInvariant().Contains(q, StringComparison.Ordinal) ?? false))
                          .ToArray();
        }

        private static string? TryGetDescription(JsonNode? node)
        {
            if (node is not JsonObject obj)
            {
                return null;
            }

            return obj["description"]?.GetValue<string?>() ?? obj["Description"]?.GetValue<string?>();
        }

        private static string? TryGetTitle(JsonNode? node)
        {
            if (node is not JsonObject obj)
            {
                return null;
            }

            return obj["title"]?.GetValue<string?>() ?? obj["Title"]?.GetValue<string?>();
        }

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

        public (bool IsValid, string? ErrorMessage) UpdateRuleJson(string provider, string ruleId, string json)
        {
            var validation = _validator.Validate(UnwrapRuleJson(json));
            if (!validation.IsValid)
            {
                return (false, validation.ErrorMessage);
            }

            // Work Item 2 only updates in-memory representation; save-back is Work Item 3.
            lock (_gate)
            {
                var key = $"rules:{provider}:{ruleId}";
                _overrides[key] = json;
                _entries = null;
            }
            return (true, null);
        }

        private static string UnwrapRuleJson(string json)
        {
            try
            {
                var node = JsonNode.Parse(json);
                var unwrapped = UnwrapRuleNode(node);
                return unwrapped?.ToJsonString() ?? json;
            }
            catch (JsonException)
            {
                return json;
            }
        }

        private IReadOnlyList<RulesWorkbenchRuleEntry> LoadAll()
        {
            lock (_gate)
            {
                if (_entries is not null)
                {
                    return _entries;
                }

                var list = new List<RulesWorkbenchRuleEntry>();
                var rulesRoot = _configuration.GetSection("rules");

                foreach (var providerSection in rulesRoot.GetChildren())
                {
                    var provider = providerSection.Key;
                    if (string.IsNullOrWhiteSpace(provider))
                    {
                        continue;
                    }

                    foreach (var ruleSection in providerSection.GetChildren())
                    {
                        var ruleId = ruleSection.Key;
                        if (string.IsNullOrWhiteSpace(ruleId))
                        {
                            continue;
                        }

                        var key = $"rules:{provider}:{ruleId}";
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
                                node = UnwrapRuleNode(JsonNode.Parse(json));
                            }
                            catch (JsonException ex)
                            {
                                isValid = false;
                                error = ex.Message;
                            }

                            if (isValid)
                            {
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
                            obj["id"] = ruleId;
                        }

                        if (!isValid && string.IsNullOrWhiteSpace(error) && string.IsNullOrWhiteSpace(TryGetTitle(node)))
                        {
                            error = "Rule JSON must include a non-empty 'title' property.";
                        }

                        list.Add(new RulesWorkbenchRuleEntry(list.Count, provider, ruleId, key, TryGetContext(node), node, isValid, error));
                    }
                }

                _logger.LogInformation("Loaded rules from App Configuration for RulesWorkbench. RuleCount={RuleCount}", list.Count);

                _entries = list;
                return _entries;
            }
        }

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
