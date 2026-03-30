namespace UKHO.Workbench.WorkbenchShell
{
    /// <summary>
    /// Describes an action rendered in the active-view toolbar surface.
    /// </summary>
    public class ToolbarContribution
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToolbarContribution"/> class.
        /// </summary>
        /// <param name="id">The stable contribution identifier used for diagnostics and rendering keys.</param>
        /// <param name="displayName">The label shown for the toolbar action.</param>
        /// <param name="commandId">The command invoked when the toolbar action is selected.</param>
        /// <param name="icon">The optional icon key shown by the toolbar button.</param>
        /// <param name="ownerToolId">The optional tool identifier that owns the contribution when it is runtime-scoped.</param>
        /// <param name="order">The relative display order used when composing toolbar actions.</param>
        public ToolbarContribution(
            string id,
            string displayName,
            string commandId,
            string? icon = null,
            string? ownerToolId = null,
            int order = 0)
        {
            // Toolbar buttons route through commands for the same reason as menu items: the shell must stay action-driven.
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
            ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

            Id = id;
            DisplayName = displayName;
            CommandId = commandId;
            Icon = icon;
            OwnerToolId = ownerToolId;
            Order = order;
        }

        /// <summary>
        /// Gets the stable contribution identifier used for diagnostics and rendering keys.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the label shown for the toolbar action.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the command invoked when the toolbar action is selected.
        /// </summary>
        public string CommandId { get; }

        /// <summary>
        /// Gets the optional icon key shown by the toolbar button.
        /// </summary>
        public string? Icon { get; }

        /// <summary>
        /// Gets the optional tool identifier that owns the contribution when it is runtime-scoped.
        /// </summary>
        public string? OwnerToolId { get; }

        /// <summary>
        /// Gets the relative display order used when composing toolbar actions.
        /// </summary>
        public int Order { get; }
    }
}
