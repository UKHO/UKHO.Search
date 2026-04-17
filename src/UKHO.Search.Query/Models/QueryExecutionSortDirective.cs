namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents one execution-time sort directive derived during query planning.
    /// </summary>
    public sealed class QueryExecutionSortDirective
    {
        /// <summary>
        /// Gets the canonical index field name that should be used for sorting.
        /// </summary>
        public required string FieldName { get; init; }

        /// <summary>
        /// Gets the direction that should be applied when sorting by the field.
        /// </summary>
        public QueryExecutionSortDirection Direction { get; init; } = QueryExecutionSortDirection.Descending;
    }
}
