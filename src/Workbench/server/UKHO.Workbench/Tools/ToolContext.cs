using UKHO.Workbench.WorkbenchShell;

namespace UKHO.Workbench.Tools
{
    /// <summary>
    /// Provides the bounded runtime contract through which a hosted tool instance interacts with the Workbench shell.
    /// </summary>
    public class ToolContext
    {
        private readonly IToolContextBridge _bridge;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolContext"/> class.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance identifier associated with this context.</param>
        /// <param name="bridge">The bounded shell bridge that performs approved Workbench interactions.</param>
        public ToolContext(string toolInstanceId, IToolContextBridge bridge)
        {
            // Every tool context is bound to one runtime tool instance so updates remain attributable and isolated.
            ArgumentException.ThrowIfNullOrWhiteSpace(toolInstanceId);
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));

            ToolInstanceId = toolInstanceId;
        }

        /// <summary>
        /// Gets the runtime tool instance identifier associated with this context.
        /// </summary>
        public string ToolInstanceId { get; }

        /// <summary>
        /// Opens or focuses another tool through the approved Workbench activation path.
        /// </summary>
        /// <param name="activationTarget">The activation target that should be opened or focused.</param>
        /// <param name="cancellationToken">The cancellation token that can stop the activation request before it completes.</param>
        /// <returns>A task that completes when the activation request has been processed.</returns>
        public Task OpenToolAsync(ActivationTarget activationTarget, CancellationToken cancellationToken = default)
        {
            // Tool-to-tool navigation remains bounded because requests flow back through the host-owned shell bridge.
            return _bridge.OpenToolAsync(ToolInstanceId, activationTarget, cancellationToken);
        }

        /// <summary>
        /// Invokes a Workbench command through the approved command-routing path.
        /// </summary>
        /// <param name="commandId">The command identifier that should be invoked.</param>
        /// <param name="cancellationToken">The cancellation token that can stop the command before it completes.</param>
        /// <returns>A task that completes when the command has been processed.</returns>
        public Task InvokeCommandAsync(string commandId, CancellationToken cancellationToken = default)
        {
            // Tools invoke commands instead of reaching into shell components directly.
            return _bridge.InvokeCommandAsync(ToolInstanceId, commandId, cancellationToken);
        }

        /// <summary>
        /// Updates the runtime title shown for the current tool instance.
        /// </summary>
        /// <param name="title">The new title that should be shown by the shell.</param>
        public void SetTitle(string title)
        {
            // Title updates let tools expose meaningful runtime state without taking ownership of shell chrome.
            _bridge.UpdateTitle(ToolInstanceId, title);
        }

        /// <summary>
        /// Updates the runtime icon shown for the current tool instance.
        /// </summary>
        /// <param name="icon">The new icon that should be shown by the shell.</param>
        public void SetIcon(string icon)
        {
            // Icon updates stay bounded for the same reason as title updates.
            _bridge.UpdateIcon(ToolInstanceId, icon);
        }

        /// <summary>
        /// Updates the runtime badge shown for the current tool instance.
        /// </summary>
        /// <param name="badge">The new badge text that should be shown by the shell, or <see langword="null"/> to clear the badge.</param>
        public void SetBadge(string? badge)
        {
            // Badges provide small runtime summaries without forcing tools to know about shell rendering details.
            _bridge.UpdateBadge(ToolInstanceId, badge);
        }

        /// <summary>
        /// Replaces the runtime menu contributions exposed by the current tool instance.
        /// </summary>
        /// <param name="menuContributions">The runtime menu contributions that should be visible while the tool is active.</param>
        public void SetRuntimeMenuContributions(IReadOnlyList<MenuContribution> menuContributions)
        {
            // Runtime menu composition is active-tool scoped, so the tool pushes its current menu state through the bridge.
            _bridge.UpdateRuntimeMenuContributions(ToolInstanceId, menuContributions ?? throw new ArgumentNullException(nameof(menuContributions)));
        }

        /// <summary>
        /// Replaces the runtime toolbar contributions exposed by the current tool instance.
        /// </summary>
        /// <param name="toolbarContributions">The runtime toolbar contributions that should be visible while the tool is active.</param>
        public void SetRuntimeToolbarContributions(IReadOnlyList<ToolbarContribution> toolbarContributions)
        {
            // The first implementation only exposes runtime toolbar participation through the active-view toolbar surface.
            _bridge.UpdateRuntimeToolbarContributions(ToolInstanceId, toolbarContributions ?? throw new ArgumentNullException(nameof(toolbarContributions)));
        }

        /// <summary>
        /// Replaces the runtime status-bar contributions exposed by the current tool instance.
        /// </summary>
        /// <param name="statusBarContributions">The runtime status-bar contributions that should be visible while the tool is active.</param>
        public void SetRuntimeStatusBarContributions(IReadOnlyList<StatusBarContribution> statusBarContributions)
        {
            // Status contributions let tools surface lightweight runtime summaries through the bounded shell contract.
            _bridge.UpdateRuntimeStatusBarContributions(ToolInstanceId, statusBarContributions ?? throw new ArgumentNullException(nameof(statusBarContributions)));
        }

        /// <summary>
        /// Updates the selection summary published by the current tool instance.
        /// </summary>
        /// <param name="selectionType">The logical selection type published by the tool, or <see langword="null"/> when no selection exists.</param>
        /// <param name="selectionCount">The number of currently selected items.</param>
        public void SetSelection(string? selectionType, int selectionCount)
        {
            // The first context model keeps selection publication intentionally small and fixed.
            _bridge.UpdateSelection(ToolInstanceId, selectionType, selectionCount);
        }

        /// <summary>
        /// Returns the current fixed Workbench context values visible to the current tool instance.
        /// </summary>
        /// <returns>The fixed Workbench context values available to the tool.</returns>
        public IReadOnlyDictionary<string, string> GetContextValues()
        {
            // Tools can inspect the fixed context model without needing direct access to shell state internals.
            return _bridge.GetContextValues(ToolInstanceId);
        }

        /// <summary>
        /// Raises a user-safe shell notification on behalf of the current tool instance.
        /// </summary>
        /// <param name="severity">The shell notification severity expressed as a simple string value such as <c>info</c> or <c>warning</c>.</param>
        /// <param name="summary">The short summary shown to the user.</param>
        /// <param name="detail">The longer explanatory detail shown to the user.</param>
        /// <param name="cancellationToken">The cancellation token that can stop the request before it completes.</param>
        /// <returns>A task that completes when the notification has been queued for the shell.</returns>
        public Task NotifyAsync(string severity, string summary, string detail, CancellationToken cancellationToken = default)
        {
            // Notifications still flow through the Workbench so tools cannot bypass user-safety and host presentation rules.
            return _bridge.NotifyAsync(ToolInstanceId, severity, summary, detail, cancellationToken);
        }
    }
}
