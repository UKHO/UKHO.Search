namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    internal sealed class RulesDuplicateRuleIdException : RulesLoadException
    {
        public RulesDuplicateRuleIdException(string ruleId, IReadOnlyList<string> filePaths)
            : base($"Duplicate rule id '{ruleId}' found in rule files: {string.Join(", ", filePaths)}")
        {
            RuleId = ruleId;
            FilePaths = filePaths;
        }

        public string RuleId { get; }

        public IReadOnlyList<string> FilePaths { get; }
    }
}
