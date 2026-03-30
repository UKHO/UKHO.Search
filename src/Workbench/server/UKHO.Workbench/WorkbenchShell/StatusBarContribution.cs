namespace UKHO.Workbench.WorkbenchShell
{
    /// <summary>
    /// Describes a status-bar item contributed by the host or by the active tool.
    /// </summary>
    public class StatusBarContribution
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StatusBarContribution"/> class.
        /// </summary>
        /// <param name="id">The stable contribution identifier used for diagnostics and rendering keys.</param>
        /// <param name="text">The text shown for the status-bar item.</param>
        /// <param name="commandId">The optional command invoked when the status item is clicked.</param>
        /// <param name="icon">The optional icon key shown before the status text.</param>
        /// <param name="ownerToolId">The optional tool identifier that owns the contribution when it is runtime-scoped.</param>
        /// <param name="order">The relative display order used when composing status-bar items.</param>
        public StatusBarContribution(
            string id,
            string text,
            string? commandId = null,
            string? icon = null,
            string? ownerToolId = null,
            int order = 0)
        {
            // Status items are lightweight shell summaries, but they still need stable identifiers for deterministic composition.
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(text);

            Id = id;
            Text = text;
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
        /// Gets the text shown for the status-bar item.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the optional command invoked when the status item is clicked.
        /// </summary>
        public string? CommandId { get; }

        /// <summary>
        /// Gets the optional icon key shown before the status text.
        /// </summary>
        public string? Icon { get; }

        /// <summary>
        /// Gets the optional tool identifier that owns the contribution when it is runtime-scoped.
        /// </summary>
        public string? OwnerToolId { get; }

        /// <summary>
        /// Gets the relative display order used when composing status-bar items.
        /// </summary>
        public int Order { get; }
    }
}
