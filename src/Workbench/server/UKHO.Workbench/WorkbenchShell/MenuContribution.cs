namespace UKHO.Workbench.WorkbenchShell
{
    /// <summary>
    /// Describes a menu-bar action contributed by the host or by the active tool.
    /// </summary>
    public class MenuContribution
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MenuContribution"/> class.
        /// </summary>
        /// <param name="id">The stable contribution identifier used for diagnostics and rendering keys.</param>
        /// <param name="displayName">The label shown for the menu action.</param>
        /// <param name="commandId">The command invoked when the menu action is selected.</param>
        /// <param name="icon">The optional icon key shown when the shell renders icon-capable menu content.</param>
        /// <param name="ownerToolId">The optional tool identifier that owns the contribution when it is runtime-scoped.</param>
        /// <param name="order">The relative display order used when composing menu actions.</param>
        public MenuContribution(
            string id,
            string displayName,
            string commandId,
            string? icon = null,
            string? ownerToolId = null,
            int order = 0)
        {
            // Menu items route exclusively through commands so the shell stays command-centric rather than component-centric.
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
        /// Gets the label shown for the menu action.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the command invoked when the menu action is selected.
        /// </summary>
        public string CommandId { get; }

        /// <summary>
        /// Gets the optional icon key shown when the shell renders icon-capable menu content.
        /// </summary>
        public string? Icon { get; }

        /// <summary>
        /// Gets the optional tool identifier that owns the contribution when it is runtime-scoped.
        /// </summary>
        public string? OwnerToolId { get; }

        /// <summary>
        /// Gets the relative display order used when composing menu actions.
        /// </summary>
        public int Order { get; }
    }
}
