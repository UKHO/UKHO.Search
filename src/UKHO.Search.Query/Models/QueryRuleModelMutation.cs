namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents one validated canonical-model mutation emitted by a query rule.
    /// </summary>
    public sealed class QueryRuleModelMutation
    {
        /// <summary>
        /// Gets the canonical query-model field name targeted by the mutation.
        /// </summary>
        public required string FieldName { get; init; }

        /// <summary>
        /// Gets the validated values that should be added to the targeted field.
        /// </summary>
        public IReadOnlyCollection<string> AddValues { get; init; } = Array.Empty<string>();
    }
}
