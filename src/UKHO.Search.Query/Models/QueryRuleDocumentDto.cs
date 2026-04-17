namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents one raw query-rule document as it is authored in repository JSON and loaded from configuration.
    /// </summary>
    public sealed class QueryRuleDocumentDto
    {
        /// <summary>
        /// Gets or sets the authored schema version for the rule document.
        /// </summary>
        public string SchemaVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the authored rule payload contained by the document wrapper.
        /// </summary>
        public QueryRuleDto? Rule { get; set; }
    }
}
