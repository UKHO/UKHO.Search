using Microsoft.AspNetCore.Components;
using Radzen;
using UKHO.Workbench.Explorers;
using UKHO.Workbench.Services.Shell;
using UKHO.Workbench.Tools;
using UKHO.Workbench.WorkbenchShell;
using WorkbenchHost.Services;

namespace WorkbenchHost.Components.Layout
{
    /// <summary>
    /// Renders and coordinates the bootstrap desktop-like Workbench shell.
    /// </summary>
    public partial class MainLayout : IDisposable
    {
        [Inject]
        private WorkbenchShellManager ShellManager { get; set; } = null!;

        [Inject]
        private NotificationService NotificationService { get; set; } = null!;

        [Inject]
        private WorkbenchStartupNotificationStore StartupNotificationStore { get; set; } = null!;

        /// <summary>
        /// Gets the active tool instance currently hosted by the shell.
        /// </summary>
        private ToolInstance? ActiveTool => ShellManager.State.ActiveTool;

        /// <summary>
        /// Gets the currently registered explorer contributions.
        /// </summary>
        private IReadOnlyList<ExplorerContribution> Explorers => ShellManager.Explorers;

        /// <summary>
        /// Gets the current menu contributions visible for the active tool.
        /// </summary>
        private IReadOnlyList<MenuContribution> MenuContributions => ShellManager.MenuContributions;

        /// <summary>
        /// Gets the current toolbar contributions visible for the active tool.
        /// </summary>
        private IReadOnlyList<ToolbarContribution> ToolbarContributions => ShellManager.ToolbarContributions;

        /// <summary>
        /// Gets the current status-bar contributions visible for the active tool.
        /// </summary>
        private IReadOnlyList<StatusBarContribution> StatusBarContributions => ShellManager.StatusBarContributions;

        /// <summary>
        /// Gets the current fixed Workbench context values.
        /// </summary>
        private IReadOnlyDictionary<string, string> ContextValues => ShellManager.ContextValues;

        /// <summary>
        /// Gets the currently active explorer contribution when one is selected.
        /// </summary>
        private ExplorerContribution? ActiveExplorer => string.IsNullOrWhiteSpace(ShellManager.State.ActiveExplorerId)
            ? null
            : ShellManager.GetExplorer(ShellManager.State.ActiveExplorerId);

        /// <summary>
        /// Gets the explorer sections visible in the currently active explorer.
        /// </summary>
        private IReadOnlyList<ExplorerSectionContribution> ActiveExplorerSections => ActiveExplorer is null
            ? Array.Empty<ExplorerSectionContribution>()
            : ShellManager.GetExplorerSections(ActiveExplorer.Id);

        /// <summary>
        /// Gets the display name of the active explorer.
        /// </summary>
        private string ActiveExplorerDisplayName => ActiveExplorer?.DisplayName ?? "No explorer selected";

        /// <summary>
        /// Subscribes to shell state changes and ensures the bootstrap explorer is selected.
        /// </summary>
        protected override void OnInitialized()
        {
            // The layout listens for shell state changes so explorer clicks and programmatic activation both refresh the visible chrome.
            ShellManager.StateChanged += HandleShellStateChanged;
            ShellManager.NotificationRaised += HandleWorkbenchNotificationRaised;

            // The shell activates the first registered explorer by default when startup bootstrap has not already selected one.
            if (string.IsNullOrWhiteSpace(ShellManager.State.ActiveExplorerId) && Explorers.Count > 0)
            {
                ShellManager.SetActiveExplorer(Explorers[0].Id);
            }

            base.OnInitialized();
        }

        /// <summary>
        /// Presents any buffered startup notifications after the interactive shell becomes available.
        /// </summary>
        /// <param name="firstRender"><see langword="true"/> when this is the first completed render for the layout instance.</param>
        protected override void OnAfterRender(bool firstRender)
        {
            // Startup notifications are deferred until first render because the notification service depends on an interactive shell.
            if (!firstRender)
            {
                return;
            }

            foreach (var notification in StartupNotificationStore.DequeueAll())
            {
                // Each buffered notification is replayed once so users receive safe startup feedback without repeated noise.
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = notification.Severity,
                    Summary = notification.Summary,
                    Detail = notification.Detail,
                    Duration = 7000
                });
            }
        }

        /// <summary>
        /// Determines whether a fixed shell region is visible in the bootstrap layout.
        /// </summary>
        /// <param name="region">The region to evaluate.</param>
        /// <returns><see langword="true"/> when the region should be rendered; otherwise, <see langword="false"/>.</returns>
        private bool IsRegionVisible(WorkbenchShellRegion region)
        {
            // The layout delegates visibility checks to the shell state so later slices can evolve region behavior without changing markup conditions.
            return ShellManager.State.IsRegionVisible(region);
        }

        /// <summary>
        /// Selects the active explorer shown in the left-hand shell pane.
        /// </summary>
        /// <param name="explorerId">The identifier of the explorer that should become active.</param>
        /// <returns>A task that completes when the shell has processed the selection.</returns>
        private Task SelectExplorerAsync(string explorerId)
        {
            // Explorer selection is lightweight in the first slice and simply updates the active explorer id tracked by the shell manager.
            ShellManager.SetActiveExplorer(explorerId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Opens or focuses a tool from the explorer list.
        /// </summary>
        /// <param name="toolId">The identifier of the tool that should become active.</param>
        /// <returns>A task that completes when the activation request has been processed.</returns>
        private async Task ExecuteCommandAsync(string commandId)
        {
            // All shell surfaces route through the shared command path so explorer, menu, toolbar, and hosted-tool actions behave consistently.
            await ShellManager.ExecuteCommandAsync(commandId);
        }

        /// <summary>
        /// Determines whether the supplied explorer is the currently selected explorer.
        /// </summary>
        /// <param name="explorerId">The explorer identifier to compare.</param>
        /// <returns><see langword="true"/> when the supplied explorer is active; otherwise, <see langword="false"/>.</returns>
        private bool IsExplorerActive(string explorerId)
        {
            // The activity rail uses this helper to keep its selected styling aligned with the shell state.
            return string.Equals(ShellManager.State.ActiveExplorerId, explorerId, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether the supplied tool is the currently focused tool instance.
        /// </summary>
        /// <param name="toolDefinition">The tool definition to compare.</param>
        /// <returns><see langword="true"/> when the supplied tool is active; otherwise, <see langword="false"/>.</returns>
        private bool IsExplorerItemActive(ExplorerItem explorerItem)
        {
            // Explorer entries highlight the active tool so the command-routed activation path remains visible in the explorer chrome.
            ArgumentNullException.ThrowIfNull(explorerItem);

            return string.Equals(ActiveTool?.Definition.Id, explorerItem.ActivationTarget.ToolId, StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns the CSS class used for an activity-rail button.
        /// </summary>
        /// <param name="explorerId">The explorer identifier represented by the button.</param>
        /// <returns>The CSS class string for the button.</returns>
        private string GetActivityButtonCss(string explorerId)
        {
            // The selected explorer button keeps a distinct style so the activity rail behaves like a desktop workbench selector strip.
            return IsExplorerActive(explorerId)
                ? "workbench-shell__activity-button workbench-shell__activity-button--active"
                : "workbench-shell__activity-button";
        }

        /// <summary>
        /// Returns the CSS class used for an explorer tool button.
        /// </summary>
        /// <param name="toolDefinition">The tool represented by the button.</param>
        /// <returns>The CSS class string for the button.</returns>
        private string GetExplorerItemButtonCss(ExplorerItem explorerItem)
        {
            // Active item highlighting makes it clear which command-routed explorer item currently owns focus in the central surface.
            return IsExplorerItemActive(explorerItem)
                ? "workbench-shell__tool-button workbench-shell__tool-button--active"
                : "workbench-shell__tool-button";
        }

        /// <summary>
        /// Returns the explorer items that belong to the supplied active explorer section.
        /// </summary>
        /// <param name="sectionId">The section identifier whose items should be returned.</param>
        /// <returns>The explorer items belonging to the supplied section.</returns>
        private IReadOnlyList<ExplorerItem> GetExplorerItems(string sectionId)
        {
            // Section lookups are delegated to the shell manager so the layout only renders already-composed explorer data.
            ArgumentException.ThrowIfNullOrWhiteSpace(sectionId);

            return ActiveExplorer is null
                ? Array.Empty<ExplorerItem>()
                : ShellManager.GetExplorerItems(ActiveExplorer.Id, sectionId);
        }

        /// <summary>
        /// Responds to shell state changes by requesting a layout re-render.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments for the notification.</param>
        private void HandleShellStateChanged(object? sender, EventArgs e)
        {
            // State changes may arrive from either startup bootstrap or user interaction, so the layout schedules a safe UI refresh on the renderer.
            _ = InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Presents a user-safe shell notification raised by the active tool or by command handling.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The notification payload that should be shown by the shell.</param>
        private void HandleWorkbenchNotificationRaised(object? sender, WorkbenchNotificationEventArgs e)
        {
            // Runtime tool notifications are marshalled back onto the renderer so they can safely use the Radzen notification service.
            _ = InvokeAsync(() =>
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = MapNotificationSeverity(e.Severity),
                    Summary = e.Summary,
                    Detail = e.Detail,
                    Duration = 5000
                });
            });
        }

        /// <summary>
        /// Maps a simple Workbench notification severity string to the corresponding Radzen severity.
        /// </summary>
        /// <param name="severity">The shell notification severity expressed as a simple string value.</param>
        /// <returns>The Radzen notification severity used to render the message.</returns>
        private static NotificationSeverity MapNotificationSeverity(string severity)
        {
            // The Workbench contract keeps severity values simple strings so the host owns the presentation-specific mapping.
            return severity.ToLowerInvariant() switch
            {
                "success" => NotificationSeverity.Success,
                "warning" => NotificationSeverity.Warning,
                "error" => NotificationSeverity.Error,
                _ => NotificationSeverity.Info
            };
        }

        /// <summary>
        /// Unsubscribes from shell state notifications when the layout is disposed.
        /// </summary>
        public void Dispose()
        {
            // Layout disposal must release the event subscription to avoid retaining old component instances across reconnects or test renders.
            ShellManager.StateChanged -= HandleShellStateChanged;
            ShellManager.NotificationRaised -= HandleWorkbenchNotificationRaised;
        }
    }
}
