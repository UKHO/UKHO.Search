using Microsoft.Extensions.Logging;
using UKHO.Workbench.Commands;
using UKHO.Workbench.Explorers;
using UKHO.Workbench.Services.Commands;
using UKHO.Workbench.Services.Context;
using UKHO.Workbench.Services.Contributions;
using UKHO.Workbench.Services.Explorers;
using UKHO.Workbench.Services.Tools;
using UKHO.Workbench.Tools;
using UKHO.Workbench.WorkbenchShell;

namespace UKHO.Workbench.Services.Shell
{
    /// <summary>
    /// Orchestrates the bootstrap Workbench shell state and singleton tool activation path.
    /// </summary>
    public class WorkbenchShellManager : IToolContextBridge
    {
        private const string SafeActionFailureSummary = "Workbench action failed";
        private const string SafeActionFailureDetail = "The selected Workbench action could not be completed. Check the application logs for more detail.";
        private readonly ILogger<WorkbenchShellManager> _logger;
        private readonly CommandManager _commandManager;
        private readonly ExplorerManager _explorerManager;
        private readonly RuntimeContributionManager _runtimeContributionManager;
        private readonly ToolActivationManager _toolActivationManager;
        private readonly WorkbenchContextManager _workbenchContextManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkbenchShellManager"/> class.
        /// </summary>
        /// <param name="logger">The logger used to record registration and activation failures.</param>
        public WorkbenchShellManager(ILogger<WorkbenchShellManager> logger)
        {
            // The initial shell keeps orchestration lightweight by composing a few focused managers behind one host-facing façade.
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commandManager = new CommandManager();
            _explorerManager = new ExplorerManager();
            _runtimeContributionManager = new RuntimeContributionManager();
            _workbenchContextManager = new WorkbenchContextManager();
            _toolActivationManager = new ToolActivationManager(this);
        }

        /// <summary>
        /// Raised whenever the shell state changes and interactive components should refresh.
        /// </summary>
        public event EventHandler? StateChanged;

        /// <summary>
        /// Raised whenever the shell should surface a user-safe notification.
        /// </summary>
        public event EventHandler<WorkbenchNotificationEventArgs>? NotificationRaised;

        /// <summary>
        /// Gets the current bootstrap shell state.
        /// </summary>
        public WorkbenchShellState State => _toolActivationManager.State;

        /// <summary>
        /// Gets the registered tool definitions in display order.
        /// </summary>
        public IReadOnlyList<ToolDefinition> ToolDefinitions => _toolActivationManager.ToolDefinitions;

        /// <summary>
        /// Gets the registered explorer contributions in display order.
        /// </summary>
        public IReadOnlyList<ExplorerContribution> Explorers => _explorerManager.Explorers;

        /// <summary>
        /// Gets the menu contributions visible for the current active tool.
        /// </summary>
        public IReadOnlyList<MenuContribution> MenuContributions => _runtimeContributionManager.GetMenuContributions(State.ActiveTool);

        /// <summary>
        /// Gets the toolbar contributions visible for the current active tool.
        /// </summary>
        public IReadOnlyList<ToolbarContribution> ToolbarContributions => _runtimeContributionManager.GetToolbarContributions(State.ActiveTool);

        /// <summary>
        /// Gets the status-bar contributions visible for the current active tool.
        /// </summary>
        public IReadOnlyList<StatusBarContribution> StatusBarContributions => _runtimeContributionManager.GetStatusBarContributions(State.ActiveTool);

        /// <summary>
        /// Gets the current fixed Workbench context values.
        /// </summary>
        public IReadOnlyDictionary<string, string> ContextValues => _workbenchContextManager.GetContextValues(State);

        /// <summary>
        /// Registers a statically known tool definition with the bootstrap shell.
        /// </summary>
        /// <param name="definition">The tool definition to register.</param>
        public void RegisterTool(ToolDefinition definition)
        {
            // Tool registration is delegated to the activation manager so definition lookup and singleton activation share one source of truth.
            _toolActivationManager.RegisterTool(definition);
            NotifyStateChanged();
        }

        /// <summary>
        /// Registers a declarative Workbench command.
        /// </summary>
        /// <param name="commandContribution">The command contribution that should become available to Workbench shell surfaces.</param>
        public void RegisterCommand(CommandContribution commandContribution)
        {
            // The shell manager keeps command registration host-facing while the command manager owns uniqueness and execution behavior.
            _commandManager.RegisterCommand(commandContribution);
            NotifyStateChanged();
        }

        /// <summary>
        /// Registers an explorer contribution.
        /// </summary>
        /// <param name="explorerContribution">The explorer contribution that should become available to the activity rail.</param>
        public void RegisterExplorer(ExplorerContribution explorerContribution)
        {
            // Explorer registration is routed through the explorer manager so the layout can render a fully declarative explorer surface.
            _explorerManager.RegisterExplorer(explorerContribution);
            NotifyStateChanged();
        }

        /// <summary>
        /// Registers an explorer-section contribution.
        /// </summary>
        /// <param name="explorerSectionContribution">The explorer-section contribution that should become available to the explorer pane.</param>
        public void RegisterExplorerSection(ExplorerSectionContribution explorerSectionContribution)
        {
            // Section registration keeps grouping data out of the host markup.
            _explorerManager.RegisterExplorerSection(explorerSectionContribution);
            NotifyStateChanged();
        }

        /// <summary>
        /// Registers an explorer-item contribution.
        /// </summary>
        /// <param name="explorerItem">The explorer item that should become available to the explorer pane.</param>
        public void RegisterExplorerItem(ExplorerItem explorerItem)
        {
            // Explorer items remain declarative routing entries owned by the explorer manager.
            _explorerManager.RegisterExplorerItem(explorerItem);
            NotifyStateChanged();
        }

        /// <summary>
        /// Registers a static menu contribution.
        /// </summary>
        /// <param name="menuContribution">The menu contribution that should become available to the shell menu bar.</param>
        public void RegisterMenu(MenuContribution menuContribution)
        {
            // Static shell contributions are composed centrally so later runtime contributions can be merged consistently.
            _runtimeContributionManager.RegisterMenu(menuContribution);
            NotifyStateChanged();
        }

        /// <summary>
        /// Registers a static toolbar contribution.
        /// </summary>
        /// <param name="toolbarContribution">The toolbar contribution that should become available to the active-view toolbar.</param>
        public void RegisterToolbar(ToolbarContribution toolbarContribution)
        {
            // Runtime and static toolbar items share the same composition path through the runtime contribution manager.
            _runtimeContributionManager.RegisterToolbar(toolbarContribution);
            NotifyStateChanged();
        }

        /// <summary>
        /// Registers a static status-bar contribution.
        /// </summary>
        /// <param name="statusBarContribution">The status-bar contribution that should become available to the shell status bar.</param>
        public void RegisterStatusBar(StatusBarContribution statusBarContribution)
        {
            // Status-bar contributions also compose through the runtime contribution manager so focus changes automatically hide tool-owned items.
            _runtimeContributionManager.RegisterStatusBar(statusBarContribution);
            NotifyStateChanged();
        }

        /// <summary>
        /// Returns the tool definitions assigned to a specific explorer.
        /// </summary>
        /// <param name="explorerId">The explorer identifier to filter by.</param>
        /// <returns>The registered tool definitions that belong to the supplied explorer.</returns>
        public IReadOnlyList<ToolDefinition> GetToolsForExplorer(string explorerId)
        {
            // This helper remains available for existing callers even though explorer rendering is now driven primarily by declarative explorer items.
            ArgumentException.ThrowIfNullOrWhiteSpace(explorerId);

            return ToolDefinitions.Where(definition => string.Equals(definition.ExplorerId, explorerId, StringComparison.Ordinal)).ToArray();
        }

        /// <summary>
        /// Returns the registered explorer contribution for the supplied identifier.
        /// </summary>
        /// <param name="explorerId">The explorer identifier to resolve.</param>
        /// <returns>The explorer contribution when one exists; otherwise, <see langword="null"/>.</returns>
        public ExplorerContribution? GetExplorer(string explorerId)
        {
            // Lookup helpers keep layout code concise while preserving the explorer manager as the source of truth.
            return _explorerManager.GetExplorer(explorerId);
        }

        /// <summary>
        /// Returns the explorer sections that belong to the supplied explorer.
        /// </summary>
        /// <param name="explorerId">The explorer identifier whose sections should be returned.</param>
        /// <returns>The explorer sections belonging to the supplied explorer.</returns>
        public IReadOnlyList<ExplorerSectionContribution> GetExplorerSections(string explorerId)
        {
            // The shell layout renders explorer sections declaratively from the explorer manager.
            return _explorerManager.GetSections(explorerId);
        }

        /// <summary>
        /// Returns the explorer items that belong to the supplied explorer section.
        /// </summary>
        /// <param name="explorerId">The explorer identifier that owns the section.</param>
        /// <param name="sectionId">The section identifier whose items should be returned.</param>
        /// <returns>The explorer items belonging to the supplied section.</returns>
        public IReadOnlyList<ExplorerItem> GetExplorerItems(string explorerId, string sectionId)
        {
            // Explorer item lookup is centralized so the host UI only renders already-composed explorer data.
            return _explorerManager.GetItems(explorerId, sectionId);
        }

        /// <summary>
        /// Sets the active explorer tracked by the bootstrap shell.
        /// </summary>
        /// <param name="explorerId">The identifier of the explorer that should become active.</param>
        public void SetActiveExplorer(string explorerId)
        {
            // The first slice only needs a single active explorer identifier to drive the left-hand selector and tool list.
            State.SetActiveExplorer(explorerId);
            NotifyStateChanged();
        }

        /// <summary>
        /// Opens or focuses a tool using the singleton activation rules of the bootstrap shell.
        /// </summary>
        /// <param name="activationTarget">The shell target that identifies which tool should be opened or focused.</param>
        /// <returns>The active runtime tool instance after the activation request completes.</returns>
        public ToolInstance ActivateTool(ActivationTarget activationTarget)
        {
            _logger.LogInformation(
                "Activating Workbench tool {ToolId} in region {Region}. Current active tool: {ActiveToolId}.",
                activationTarget.ToolId,
                activationTarget.Region,
                State.ActiveTool?.Definition.Id);

            try
            {
                // The activation manager owns definition lookup and singleton reuse while the shell manager adds diagnostics and UI notifications.
                var activeTool = _toolActivationManager.ActivateTool(activationTarget);
                _logger.LogInformation(
                    "Activated Workbench tool {ToolId} as instance {ToolInstanceId} in region {Region}.",
                    activeTool.Definition.Id,
                    activeTool.InstanceId,
                    activeTool.HostedRegion);
                NotifyStateChanged();
                return activeTool;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Workbench tool activation failed for tool {ToolId} in region {Region}. Current active tool: {ActiveToolId}.",
                    activationTarget.ToolId,
                    activationTarget.Region,
                    State.ActiveTool?.Definition.Id);
                RaiseSafeFailureNotification();
                throw;
            }
        }

        /// <summary>
        /// Executes a registered Workbench command.
        /// </summary>
        /// <param name="commandId">The command identifier that should be executed.</param>
        /// <param name="cancellationToken">The cancellation token that can stop command execution before it completes.</param>
        /// <returns>A task that completes when the command has finished executing.</returns>
        public async Task ExecuteCommandAsync(string commandId, CancellationToken cancellationToken = default)
        {
            // Every shell surface routes through the same command path so explorer, menu, toolbar, and hosted-tool actions behave consistently.
            ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

            _logger.LogInformation(
                "Executing Workbench command {CommandId} for active tool {ActiveToolId}.",
                commandId,
                State.ActiveTool?.Definition.Id);

            try
            {
                await _commandManager.ExecuteAsync(
                    commandId,
                    State.ActiveTool?.Context,
                    activationTarget =>
                    {
                        ActivateTool(activationTarget);
                        return Task.CompletedTask;
                    },
                    cancellationToken).ConfigureAwait(false);

                _logger.LogInformation(
                    "Executed Workbench command {CommandId}. Active tool is now {ActiveToolId}.",
                    commandId,
                    State.ActiveTool?.Definition.Id);
                NotifyStateChanged();
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Workbench command execution failed for command {CommandId}. Active tool at failure time: {ActiveToolId}.",
                    commandId,
                    State.ActiveTool?.Definition.Id);
                RaiseSafeFailureNotification();
            }
        }

        /// <summary>
        /// Opens or focuses a tool through the approved Workbench activation path.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance issuing the request.</param>
        /// <param name="activationTarget">The shell activation target that should be opened or focused.</param>
        /// <param name="cancellationToken">The cancellation token that can stop the activation request before it completes.</param>
        /// <returns>A task that completes when the activation request has been processed.</returns>
        public Task OpenToolAsync(string toolInstanceId, ActivationTarget activationTarget, CancellationToken cancellationToken = default)
        {
            // Tool-context activation requests still flow through the same singleton activation path used by explorer and menu commands.
            cancellationToken.ThrowIfCancellationRequested();
            _ = ResolveRequiredToolInstance(toolInstanceId);

            try
            {
                ActivateTool(activationTarget);
            }
            catch (Exception)
            {
                // Recoverable activation failures are already logged and surfaced as safe notifications by ActivateTool.
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Invokes a Workbench command through the approved command-routing path.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance issuing the request.</param>
        /// <param name="commandId">The command identifier that should be invoked.</param>
        /// <param name="cancellationToken">The cancellation token that can stop the command before it completes.</param>
        /// <returns>A task that completes when the command has been processed.</returns>
        public Task InvokeCommandAsync(string toolInstanceId, string commandId, CancellationToken cancellationToken = default)
        {
            // Tool-context command requests validate the caller instance before routing into the shared command manager.
            _ = ResolveRequiredToolInstance(toolInstanceId);
            return ExecuteCommandAsync(commandId, cancellationToken);
        }

        /// <summary>
        /// Updates the runtime title shown for the supplied tool instance.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
        /// <param name="title">The new title that should be shown by the shell.</param>
        public void UpdateTitle(string toolInstanceId, string title)
        {
            // Runtime shell metadata changes are bounded to tracked tool instances so the host retains shell ownership.
            var toolInstance = ResolveRequiredToolInstance(toolInstanceId);
            toolInstance.UpdateTitle(title);
            NotifyStateChanged();
        }

        /// <summary>
        /// Updates the runtime icon shown for the supplied tool instance.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
        /// <param name="icon">The new icon that should be shown by the shell.</param>
        public void UpdateIcon(string toolInstanceId, string icon)
        {
            // Icon updates follow the same bounded metadata path as title updates.
            var toolInstance = ResolveRequiredToolInstance(toolInstanceId);
            toolInstance.UpdateIcon(icon);
            NotifyStateChanged();
        }

        /// <summary>
        /// Updates the runtime badge shown for the supplied tool instance.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
        /// <param name="badge">The new badge text that should be shown by the shell, or <see langword="null"/> to clear the badge.</param>
        public void UpdateBadge(string toolInstanceId, string? badge)
        {
            // Badges are optional metadata, so the bridge simply applies the latest value to the tracked instance.
            var toolInstance = ResolveRequiredToolInstance(toolInstanceId);
            toolInstance.UpdateBadge(badge);
            NotifyStateChanged();
        }

        /// <summary>
        /// Replaces the runtime menu contributions exposed by the supplied tool instance.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
        /// <param name="menuContributions">The runtime menu contributions that should be visible while the tool is active.</param>
        public void UpdateRuntimeMenuContributions(string toolInstanceId, IReadOnlyList<MenuContribution> menuContributions)
        {
            // Runtime menu contributions are stored on the tool instance and later composed by the runtime contribution manager.
            var toolInstance = ResolveRequiredToolInstance(toolInstanceId);
            toolInstance.UpdateRuntimeMenuContributions(menuContributions ?? throw new ArgumentNullException(nameof(menuContributions)));
            NotifyStateChanged();
        }

        /// <summary>
        /// Replaces the runtime toolbar contributions exposed by the supplied tool instance.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
        /// <param name="toolbarContributions">The runtime toolbar contributions that should be visible while the tool is active.</param>
        public void UpdateRuntimeToolbarContributions(string toolInstanceId, IReadOnlyList<ToolbarContribution> toolbarContributions)
        {
            // Runtime toolbar contributions are also stored on the tool instance so they disappear automatically when focus changes.
            var toolInstance = ResolveRequiredToolInstance(toolInstanceId);
            toolInstance.UpdateRuntimeToolbarContributions(toolbarContributions ?? throw new ArgumentNullException(nameof(toolbarContributions)));
            NotifyStateChanged();
        }

        /// <summary>
        /// Replaces the runtime status-bar contributions exposed by the supplied tool instance.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
        /// <param name="statusBarContributions">The runtime status-bar contributions that should be visible while the tool is active.</param>
        public void UpdateRuntimeStatusBarContributions(string toolInstanceId, IReadOnlyList<StatusBarContribution> statusBarContributions)
        {
            // Runtime status-bar contributions follow the same active-tool-scoped storage pattern as the other runtime contribution surfaces.
            var toolInstance = ResolveRequiredToolInstance(toolInstanceId);
            toolInstance.UpdateRuntimeStatusBarContributions(statusBarContributions ?? throw new ArgumentNullException(nameof(statusBarContributions)));
            NotifyStateChanged();
        }

        /// <summary>
        /// Updates the runtime selection summary published by the supplied tool instance.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
        /// <param name="selectionType">The logical selection type published by the tool, or <see langword="null"/> when no selection exists.</param>
        /// <param name="selectionCount">The number of currently selected items.</param>
        public void UpdateSelection(string toolInstanceId, string? selectionType, int selectionCount)
        {
            // Selection publication feeds the fixed context-key snapshot exposed by the shell.
            var toolInstance = ResolveRequiredToolInstance(toolInstanceId);
            toolInstance.UpdateSelection(selectionType, selectionCount);
            NotifyStateChanged();
        }

        /// <summary>
        /// Returns the current fixed Workbench context values visible to the supplied tool instance.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance requesting the current context snapshot.</param>
        /// <returns>The fixed Workbench context values available to the tool.</returns>
        public IReadOnlyDictionary<string, string> GetContextValues(string toolInstanceId)
        {
            // The caller is validated first so context requests cannot be issued for unknown tool instances.
            _ = ResolveRequiredToolInstance(toolInstanceId);
            return ContextValues;
        }

        /// <summary>
        /// Raises a user-safe shell notification on behalf of the supplied tool instance.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance issuing the notification.</param>
        /// <param name="severity">The shell notification severity expressed as a simple string value such as <c>info</c> or <c>warning</c>.</param>
        /// <param name="summary">The short summary shown to the user.</param>
        /// <param name="detail">The longer explanatory detail shown to the user.</param>
        /// <param name="cancellationToken">The cancellation token that can stop the request before it completes.</param>
        /// <returns>A task that completes when the notification has been queued for the shell.</returns>
        public Task NotifyAsync(string toolInstanceId, string severity, string summary, string detail, CancellationToken cancellationToken = default)
        {
            // Notifications remain bounded to tracked tool instances and use a minimal payload so the host controls presentation details.
            cancellationToken.ThrowIfCancellationRequested();
            _ = ResolveRequiredToolInstance(toolInstanceId);

            NotificationRaised?.Invoke(this, new WorkbenchNotificationEventArgs(severity, summary, detail));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Raises the shell state changed event for any subscribed UI components.
        /// </summary>
        private void NotifyStateChanged()
        {
            // The Blazor host uses this event to re-render the shell whenever explorer or active-tool state changes.
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Resolves a tracked tool instance or throws when the supplied identifier is unknown.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance identifier to resolve.</param>
        /// <returns>The resolved tracked tool instance.</returns>
        private ToolInstance ResolveRequiredToolInstance(string toolInstanceId)
        {
            // Tool-context callbacks must target a real tracked instance so runtime updates cannot drift away from shell state.
            ArgumentException.ThrowIfNullOrWhiteSpace(toolInstanceId);

            if (_toolActivationManager.TryGetToolInstance(toolInstanceId, out var toolInstance) && toolInstance is not null)
            {
                return toolInstance;
            }

            _logger.LogError("Workbench runtime update failed because tool instance {ToolInstanceId} was not tracked by the shell.", toolInstanceId);
            throw new InvalidOperationException($"The Workbench tool instance '{toolInstanceId}' is not tracked by the shell.");
        }

        /// <summary>
        /// Raises a user-safe notification for a recoverable Workbench action failure.
        /// </summary>
        private void RaiseSafeFailureNotification()
        {
            // Recoverable shell failures share one safe notification payload so callers do not need to translate technical exceptions for the user.
            NotificationRaised?.Invoke(this, new WorkbenchNotificationEventArgs("error", SafeActionFailureSummary, SafeActionFailureDetail));
        }
    }
}
