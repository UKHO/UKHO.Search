namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents the collection of default query contributions derived from the residual query content.
    /// </summary>
    public sealed class QueryDefaultContributions
    {
        /// <summary>
        /// Gets the ordered contributions that the infrastructure layer should translate into Elasticsearch query clauses.
        /// </summary>
        public IReadOnlyCollection<QueryDefaultFieldContribution> Items { get; init; } = Array.Empty<QueryDefaultFieldContribution>();
    }
}
