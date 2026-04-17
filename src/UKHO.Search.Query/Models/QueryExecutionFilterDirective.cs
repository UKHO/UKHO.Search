namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents one execution-time filter directive carried by the query plan.
    /// </summary>
    public sealed class QueryExecutionFilterDirective
    {
        /// <summary>
        /// Gets the canonical field name targeted by the filter.
        /// </summary>
        public required string FieldName { get; init; }

        /// <summary>
        /// Gets the string values that should be matched when the field is string-backed.
        /// </summary>
        public IReadOnlyCollection<string> StringValues { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the integer values that should be matched when the field is integer-backed.
        /// </summary>
        public IReadOnlyCollection<int> IntegerValues { get; init; } = Array.Empty<int>();
    }
}
