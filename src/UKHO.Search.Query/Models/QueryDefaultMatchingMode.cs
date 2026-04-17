namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Identifies how a default query contribution should be translated into Elasticsearch query behavior.
    /// </summary>
    public enum QueryDefaultMatchingMode
    {
        /// <summary>
        /// Indicates that the contribution should target exact-match keyword values.
        /// </summary>
        ExactTerms,

        /// <summary>
        /// Indicates that the contribution should target analyzed full-text behavior.
        /// </summary>
        AnalyzedText
    }
}
