namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents one raw query rule before validation normalizes it into the runtime model.
    /// </summary>
    public sealed class QueryRuleDto
    {
        /// <summary>
        /// Gets or sets the authored rule identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the authored rule title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional authored rule description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the authored rule is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the authored predicate tree used to decide whether the rule matches.
        /// </summary>
        public QueryRulePredicateDto? If { get; set; }

        /// <summary>
        /// Gets or sets the authored action set that should run when the rule matches.
        /// </summary>
        public QueryRuleActionSetDto? Then { get; set; }
    }
}
