using UKHO.Workbench.Tools;

namespace UKHO.Workbench.Explorers
{
    /// <summary>
    /// Describes an actionable item rendered inside an explorer section.
    /// </summary>
    public class ExplorerItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExplorerItem"/> class.
        /// </summary>
        /// <param name="id">The stable explorer item identifier used for diagnostics and rendering keys.</param>
        /// <param name="explorerId">The explorer that owns the item.</param>
        /// <param name="sectionId">The section that groups the item.</param>
        /// <param name="displayName">The label shown for the item in the explorer pane.</param>
        /// <param name="commandId">The command routed when the user activates the item.</param>
        /// <param name="activationTarget">The declarative activation target associated with the item.</param>
        /// <param name="icon">The icon key shown next to the item label.</param>
        /// <param name="description">The optional explanatory text shown under the item label.</param>
        /// <param name="order">The relative display order used inside the section.</param>
        public ExplorerItem(
            string id,
            string explorerId,
            string sectionId,
            string displayName,
            string commandId,
            ActivationTarget activationTarget,
            string icon,
            string? description = null,
            int order = 0)
        {
            // Explorer items are declarative routing entries, so they must always point at both a command and an activation target.
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(explorerId);
            ArgumentException.ThrowIfNullOrWhiteSpace(sectionId);
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
            ArgumentException.ThrowIfNullOrWhiteSpace(commandId);
            ArgumentNullException.ThrowIfNull(activationTarget);
            ArgumentException.ThrowIfNullOrWhiteSpace(icon);

            Id = id;
            ExplorerId = explorerId;
            SectionId = sectionId;
            DisplayName = displayName;
            CommandId = commandId;
            ActivationTarget = activationTarget;
            Icon = icon;
            Description = description;
            Order = order;
        }

        /// <summary>
        /// Gets the stable explorer item identifier used for diagnostics and rendering keys.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the explorer that owns the item.
        /// </summary>
        public string ExplorerId { get; }

        /// <summary>
        /// Gets the section that groups the item.
        /// </summary>
        public string SectionId { get; }

        /// <summary>
        /// Gets the label shown for the item in the explorer pane.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the command routed when the user activates the item.
        /// </summary>
        public string CommandId { get; }

        /// <summary>
        /// Gets the declarative activation target associated with the item.
        /// </summary>
        public ActivationTarget ActivationTarget { get; }

        /// <summary>
        /// Gets the icon key shown next to the item label.
        /// </summary>
        public string Icon { get; }

        /// <summary>
        /// Gets the optional explanatory text shown under the item label.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Gets the relative display order used inside the section.
        /// </summary>
        public int Order { get; }
    }
}
