namespace UKHO.Workbench.Explorers
{
    /// <summary>
    /// Describes an explorer surface that can be selected from the Workbench activity rail.
    /// </summary>
    public class ExplorerContribution
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExplorerContribution"/> class.
        /// </summary>
        /// <param name="id">The stable explorer identifier used by shell state and explorer items.</param>
        /// <param name="displayName">The human-readable explorer label shown in the activity rail and explorer header.</param>
        /// <param name="icon">The icon key used by the activity rail button.</param>
        /// <param name="order">The relative display order used when multiple explorers are registered.</param>
        public ExplorerContribution(string id, string displayName, string icon, int order = 0)
        {
            // Explorer metadata must stay explicit because the host shell renders it directly in the activity rail.
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
            ArgumentException.ThrowIfNullOrWhiteSpace(icon);

            Id = id;
            DisplayName = displayName;
            Icon = icon;
            Order = order;
        }

        /// <summary>
        /// Gets the stable explorer identifier used by shell state and explorer items.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the human-readable explorer label shown in the activity rail and explorer header.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the icon key used by the activity rail button.
        /// </summary>
        public string Icon { get; }

        /// <summary>
        /// Gets the relative display order used when multiple explorers are registered.
        /// </summary>
        public int Order { get; }
    }
}
