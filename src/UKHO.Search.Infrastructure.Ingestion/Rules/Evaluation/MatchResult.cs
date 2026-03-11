namespace UKHO.Search.Infrastructure.Ingestion.Rules.Evaluation
{
    internal sealed class MatchResult
    {
        public MatchResult(string ruleId, bool isMatch, IReadOnlyList<string> matchedValues)
        {
            RuleId = ruleId;
            IsMatch = isMatch;
            MatchedValues = matchedValues;
        }

        public string RuleId { get; }

        public bool IsMatch { get; }

        public IReadOnlyList<string> MatchedValues { get; }
    }
}