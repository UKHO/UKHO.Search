namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Identifies the underlying value kind used by one validated execution-time filter field.
    /// </summary>
    public enum QueryRuleFilterFieldKind
    {
        /// <summary>
        /// Indicates that the filter targets string-backed exact values.
        /// </summary>
        String = 0,

        /// <summary>
        /// Indicates that the filter targets integer-backed exact values.
        /// </summary>
        Integer = 1
    }
}
