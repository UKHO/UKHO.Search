using UKHO.Workbench.WorkbenchShell;

namespace UKHO.Workbench.Tools
{
    /// <summary>
    /// Defines the bounded bridge that routes runtime tool-context requests back through the Workbench shell.
    /// </summary>
    public interface IToolContextBridge
    {
        /// <summary>
        /// Opens or focuses a tool through the approved Workbench activation path.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance issuing the request.</param>
        /// <param name="activationTarget">The shell activation target that should be opened or focused.</param>
        /// <param name="cancellationToken">The cancellation token that can stop the activation request before it completes.</param>
        /// <returns>A task that completes when the activation request has been processed.</returns>
        Task OpenToolAsync(string toolInstanceId, ActivationTarget activationTarget, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invokes a Workbench command through the approved command-routing path.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance issuing the request.</param>
        /// <param name="commandId">The command identifier that should be invoked.</param>
        /// <param name="cancellationToken">The cancellation token that can stop the command before it completes.</param>
        /// <returns>A task that completes when the command has been processed.</returns>
        Task InvokeCommandAsync(string toolInstanceId, string commandId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the runtime title shown for the supplied tool instance.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
        /// <param name="title">The new title that should be shown by the shell.</param>
        void UpdateTitle(string toolInstanceId, string title);

        /// <summary>
        /// Updates the runtime icon shown for the supplied tool instance.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
        /// <param name="icon">The new icon that should be shown by the shell.</param>
        void UpdateIcon(string toolInstanceId, string icon);

        /// <summary>
        /// Updates the runtime badge shown for the supplied tool instance.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
        /// <param name="badge">The new badge text that should be shown by the shell, or <see langword="null"/> to clear the badge.</param>
        void UpdateBadge(string toolInstanceId, string? badge);

        /// <summary>
        /// Replaces the runtime menu contributions exposed by the supplied tool instance.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
        /// <param name="menuContributions">The runtime menu contributions that should be visible while the tool is active.</param>
        void UpdateRuntimeMenuContributions(string toolInstanceId, IReadOnlyList<MenuContribution> menuContributions);

        /// <summary>
        /// Replaces the runtime toolbar contributions exposed by the supplied tool instance.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
        /// <param name="toolbarContributions">The runtime toolbar contributions that should be visible while the tool is active.</param>
        void UpdateRuntimeToolbarContributions(string toolInstanceId, IReadOnlyList<ToolbarContribution> toolbarContributions);

        /// <summary>
        /// Replaces the runtime status-bar contributions exposed by the supplied tool instance.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
        /// <param name="statusBarContributions">The runtime status-bar contributions that should be visible while the tool is active.</param>
        void UpdateRuntimeStatusBarContributions(string toolInstanceId, IReadOnlyList<StatusBarContribution> statusBarContributions);

        /// <summary>
        /// Updates the runtime selection summary published by the supplied tool instance.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
        /// <param name="selectionType">The logical selection type published by the tool, or <see langword="null"/> when no selection exists.</param>
        /// <param name="selectionCount">The number of currently selected items.</param>
        void UpdateSelection(string toolInstanceId, string? selectionType, int selectionCount);

        /// <summary>
        /// Returns the current fixed Workbench context values visible to the supplied tool instance.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance requesting the current context snapshot.</param>
        /// <returns>The fixed Workbench context values available to the tool.</returns>
        IReadOnlyDictionary<string, string> GetContextValues(string toolInstanceId);

        /// <summary>
        /// Raises a user-safe shell notification on behalf of the supplied tool instance.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance issuing the notification.</param>
        /// <param name="severity">The shell notification severity expressed as a simple string value such as <c>info</c> or <c>warning</c>.</param>
        /// <param name="summary">The short summary shown to the user.</param>
        /// <param name="detail">The longer explanatory detail shown to the user.</param>
        /// <param name="cancellationToken">The cancellation token that can stop the request before it completes.</param>
        /// <returns>A task that completes when the notification has been queued for the shell.</returns>
        Task NotifyAsync(string toolInstanceId, string severity, string summary, string detail, CancellationToken cancellationToken = default);
    }
}
