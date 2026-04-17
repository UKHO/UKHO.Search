namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Identifies how one explicit rule-driven boost should be mapped into Elasticsearch behavior.
    /// </summary>
    public enum QueryExecutionBoostMatchingMode
    {
        /// <summary>
        /// Uses an exact-terms clause against a keyword-like canonical field.
        /// </summary>
        ExactTerms = 0,

        /// <summary>
        /// Uses an analyzed match clause against an analyzed text field.
        /// </summary>
        AnalyzedText = 1
    }
}
