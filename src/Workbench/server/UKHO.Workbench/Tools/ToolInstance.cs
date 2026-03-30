using UKHO.Workbench.WorkbenchShell;

namespace UKHO.Workbench.Tools
{
    /// <summary>
    /// Represents a runtime hosted copy of a Workbench tool inside the shell.
    /// </summary>
    public class ToolInstance
    {
        private static readonly IReadOnlyList<MenuContribution> EmptyMenuContributions = Array.Empty<MenuContribution>();
        private static readonly IReadOnlyList<ToolbarContribution> EmptyToolbarContributions = Array.Empty<ToolbarContribution>();
        private static readonly IReadOnlyList<StatusBarContribution> EmptyStatusBarContributions = Array.Empty<StatusBarContribution>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolInstance"/> class.
        /// </summary>
        /// <param name="instanceId">The unique identifier of the runtime instance.</param>
        /// <param name="definition">The static tool definition from which the runtime instance was created.</param>
        /// <param name="title">The runtime title currently shown for the tool.</param>
        /// <param name="icon">The runtime icon currently shown for the tool.</param>
        /// <param name="badge">The runtime badge currently shown for the tool, or <see langword="null"/> when no badge is published.</param>
        /// <param name="hostedRegion">The shell region currently hosting the tool.</param>
        /// <param name="activatedAtUtc">The UTC timestamp recorded when the tool became active.</param>
        /// <param name="context">The bounded tool context used by the runtime component hosted for this instance.</param>
        public ToolInstance(
            string instanceId,
            ToolDefinition definition,
            string title,
            string icon,
            string? badge,
            WorkbenchShellRegion hostedRegion,
            DateTimeOffset activatedAtUtc,
            ToolContext context)
        {
            // Runtime tool instances preserve the static definition alongside lightweight per-instance shell metadata.
            ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
            ArgumentNullException.ThrowIfNull(definition);
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentException.ThrowIfNullOrWhiteSpace(icon);
            ArgumentNullException.ThrowIfNull(context);

            InstanceId = instanceId;
            Definition = definition;
            Title = title;
            Icon = icon;
            Badge = badge;
            HostedRegion = hostedRegion;
            ActivatedAtUtc = activatedAtUtc;
            Context = context;
            RuntimeMenuContributions = EmptyMenuContributions;
            RuntimeToolbarContributions = EmptyToolbarContributions;
            RuntimeStatusBarContributions = EmptyStatusBarContributions;
        }

        /// <summary>
        /// Gets the unique identifier of the runtime instance.
        /// </summary>
        public string InstanceId { get; }

        /// <summary>
        /// Gets the static tool definition from which the runtime instance was created.
        /// </summary>
        public ToolDefinition Definition { get; }

        /// <summary>
        /// Gets the runtime title currently shown for the tool.
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// Gets the runtime icon currently shown for the tool.
        /// </summary>
        public string Icon { get; private set; }

        /// <summary>
        /// Gets the runtime badge currently shown for the tool, or <see langword="null"/> when no badge is published.
        /// </summary>
        public string? Badge { get; private set; }

        /// <summary>
        /// Gets the shell region currently hosting the tool.
        /// </summary>
        public WorkbenchShellRegion HostedRegion { get; }

        /// <summary>
        /// Gets the UTC timestamp recorded when the tool became active.
        /// </summary>
        public DateTimeOffset ActivatedAtUtc { get; }

        /// <summary>
        /// Gets the bounded tool context used by the runtime component hosted for this instance.
        /// </summary>
        public ToolContext Context { get; }

        /// <summary>
        /// Gets the logical selection type currently published by the tool, or <see langword="null"/> when no selection exists.
        /// </summary>
        public string? SelectionType { get; private set; }

        /// <summary>
        /// Gets the current selection count published by the tool.
        /// </summary>
        public int SelectionCount { get; private set; }

        /// <summary>
        /// Gets the runtime menu contributions published by the tool while it is active.
        /// </summary>
        public IReadOnlyList<MenuContribution> RuntimeMenuContributions { get; private set; }

        /// <summary>
        /// Gets the runtime toolbar contributions published by the tool while it is active.
        /// </summary>
        public IReadOnlyList<ToolbarContribution> RuntimeToolbarContributions { get; private set; }

        /// <summary>
        /// Gets the runtime status-bar contributions published by the tool while it is active.
        /// </summary>
        public IReadOnlyList<StatusBarContribution> RuntimeStatusBarContributions { get; private set; }

        /// <summary>
        /// Updates the runtime title currently shown for the tool.
        /// </summary>
        /// <param name="title">The new title that should be shown by the shell.</param>
        public void UpdateTitle(string title)
        {
            // Runtime titles are mutable because tools can surface state changes without recreating the hosted instance.
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            Title = title;
        }

        /// <summary>
        /// Updates the runtime icon currently shown for the tool.
        /// </summary>
        /// <param name="icon">The new icon that should be shown by the shell.</param>
        public void UpdateIcon(string icon)
        {
            // Runtime icon changes are bounded to shell metadata rather than direct UI manipulation.
            ArgumentException.ThrowIfNullOrWhiteSpace(icon);
            Icon = icon;
        }

        /// <summary>
        /// Updates the runtime badge currently shown for the tool.
        /// </summary>
        /// <param name="badge">The new badge that should be shown, or <see langword="null"/> to clear it.</param>
        public void UpdateBadge(string? badge)
        {
            // Badges are optional, so a null value simply clears the current shell badge state.
            Badge = badge;
        }

        /// <summary>
        /// Updates the runtime selection summary published by the tool.
        /// </summary>
        /// <param name="selectionType">The logical selection type published by the tool, or <see langword="null"/> when no selection exists.</param>
        /// <param name="selectionCount">The number of currently selected items.</param>
        public void UpdateSelection(string? selectionType, int selectionCount)
        {
            // The first context model keeps selection publication intentionally simple and shell-friendly.
            SelectionType = selectionType;
            SelectionCount = Math.Max(0, selectionCount);
        }

        /// <summary>
        /// Replaces the runtime menu contributions published by the tool.
        /// </summary>
        /// <param name="menuContributions">The runtime menu contributions that should be visible while the tool is active.</param>
        public void UpdateRuntimeMenuContributions(IReadOnlyList<MenuContribution> menuContributions)
        {
            // Runtime menu composition is owned by the tool instance, but the shell controls where and when those items are shown.
            RuntimeMenuContributions = menuContributions ?? throw new ArgumentNullException(nameof(menuContributions));
        }

        /// <summary>
        /// Replaces the runtime toolbar contributions published by the tool.
        /// </summary>
        /// <param name="toolbarContributions">The runtime toolbar contributions that should be visible while the tool is active.</param>
        public void UpdateRuntimeToolbarContributions(IReadOnlyList<ToolbarContribution> toolbarContributions)
        {
            // The first implementation only exposes runtime toolbar items through the active-view toolbar surface.
            RuntimeToolbarContributions = toolbarContributions ?? throw new ArgumentNullException(nameof(toolbarContributions));
        }

        /// <summary>
        /// Replaces the runtime status-bar contributions published by the tool.
        /// </summary>
        /// <param name="statusBarContributions">The runtime status-bar contributions that should be visible while the tool is active.</param>
        public void UpdateRuntimeStatusBarContributions(IReadOnlyList<StatusBarContribution> statusBarContributions)
        {
            // Runtime status items are lightweight summaries, so the current tool instance stores the latest published list directly.
            RuntimeStatusBarContributions = statusBarContributions ?? throw new ArgumentNullException(nameof(statusBarContributions));
        }
    }
}
