namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    public static class RuleKeyParser
    {
        private const string RulesPrefix = "rules";

        public static bool TryParse(string key, out string provider, out string ruleId)
        {
            provider = string.Empty;
            ruleId = string.Empty;

            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            var parts = key.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 3)
            {
                return false;
            }

            if (!string.Equals(parts[0], RulesPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(parts[1]) || string.IsNullOrWhiteSpace(parts[2]))
            {
                return false;
            }

            provider = parts[1];
            ruleId = parts[2];
            return true;
        }
    }
}
