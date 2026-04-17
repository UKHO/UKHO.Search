namespace UKHO.Search.Infrastructure.Query.Rules
{
    /// <summary>
    /// Represents a fail-fast validation error encountered while loading query rules.
    /// </summary>
    public sealed class QueryRulesValidationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryRulesValidationException"/> class.
        /// </summary>
        /// <param name="message">The validation error message describing the invalid rule contract.</param>
        /// <param name="innerException">The optional underlying exception that triggered the validation failure.</param>
        public QueryRulesValidationException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
            // No additional state is required because the message fully describes the invalid query-rule contract.
        }
    }
}
