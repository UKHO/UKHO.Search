namespace UKHO.Workbench.Explorers
{
    /// <summary>
    /// Describes a section within an explorer so tools can be grouped without the host hard-coding navigation markup.
    /// </summary>
    public class ExplorerSectionContribution
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExplorerSectionContribution"/> class.
        /// </summary>
        /// <param name="id">The stable section identifier used by explorer items.</param>
        /// <param name="explorerId">The explorer that owns the section.</param>
        /// <param name="displayName">The section label shown in the explorer pane.</param>
        /// <param name="order">The relative display order used when multiple sections exist in the same explorer.</param>
        public ExplorerSectionContribution(string id, string explorerId, string displayName, int order = 0)
        {
            // Section identifiers and explorer ownership must remain explicit so module-contributed items compose deterministically.
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(explorerId);
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

            Id = id;
            ExplorerId = explorerId;
            DisplayName = displayName;
            Order = order;
        }

        /// <summary>
        /// Gets the stable section identifier used by explorer items.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the explorer that owns the section.
        /// </summary>
        public string ExplorerId { get; }

        /// <summary>
        /// Gets the section label shown in the explorer pane.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the relative display order used when multiple sections exist in the same explorer.
        /// </summary>
        public int Order { get; }
    }
}
