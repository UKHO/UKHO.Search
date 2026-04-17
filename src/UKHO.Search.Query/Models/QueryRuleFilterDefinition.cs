namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents one validated execution-time filter emitted by a query rule.
    /// </summary>
    public sealed class QueryRuleFilterDefinition
    {
        /// <summary>
        /// Gets the canonical field name targeted by the filter.
        /// </summary>
        public required string FieldName { get; init; }

        /// <summary>
        /// Gets the underlying value kind used by the targeted field.
        /// </summary>
        public QueryRuleFilterFieldKind FieldKind { get; init; } = QueryRuleFilterFieldKind.String;

        /// <summary>
        /// Gets the validated string values that should be matched when the field is string-backed.
        /// </summary>
        public IReadOnlyCollection<string> StringValues { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the validated integer values that should be matched when the field is integer-backed.
        /// </summary>
        public IReadOnlyCollection<int> IntegerValues { get; init; } = Array.Empty<int>();
    }
}
