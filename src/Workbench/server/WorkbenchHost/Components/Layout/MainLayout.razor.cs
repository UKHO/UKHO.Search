using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using UKHO.Workbench.Explorers;
using UKHO.Workbench.Services.Shell;
using UKHO.Workbench.Tools;
using UKHO.Workbench.WorkbenchShell;
using WorkbenchHost.Components.WorkbenchShell;
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

        [Inject]
        private ContextMenuService ContextMenuService { get; set; } = null!;

        [Inject]
        private TooltipService TooltipService { get; set; } = null!;

        /// <summary>
        /// Gets the active tool instance currently hosted by the shell.
        /// </summary>
        private ToolInstance? ActiveTool => ShellManager.State.ActiveTool;

        /// <summary>
        /// Gets the ordered tabs currently open in the shell.
        /// </summary>
        private IReadOnlyList<WorkbenchTab> OpenTabs => ShellManager.OpenTabs;

        /// <summary>
        /// Gets the subset of tabs that should remain visible in the main strip after overflow windowing is applied.
        /// </summary>
        private IReadOnlyList<WorkbenchTab> VisibleTabs => ShellManager.VisibleTabs;

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
        /// Gets the host-owned toolbar contribution that should surface as the persistent Home action.
        /// </summary>
        private ToolbarContribution? HomeToolbarContribution => ToolbarContributions.FirstOrDefault(toolbarContribution =>
            string.Equals(toolbarContribution.Id, WorkbenchHostShellDefaults.OverviewToolbarId, StringComparison.Ordinal));

        /// <summary>
        /// Gets the remaining toolbar contributions after the persistent Home action has been removed from the trailing toolbar collection.
        /// </summary>
        private IReadOnlyList<ToolbarContribution> VisibleToolbarContributions => ToolbarContributions
            .Where(toolbarContribution => !string.Equals(toolbarContribution.Id, WorkbenchHostShellDefaults.OverviewToolbarId, StringComparison.Ordinal))
            .ToArray();

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
        /// Records explorer-item selection without opening a tab.
        /// </summary>
        /// <param name="explorerItem">The explorer item that should become selected.</param>
        /// <param name="mouseEventArgs">The mouse event that triggered the selection request.</param>
        /// <returns>A task that completes when the shell has processed the selection.</returns>
        private Task SelectExplorerItemAsync(ExplorerItem explorerItem, MouseEventArgs mouseEventArgs)
        {
            // Explorer single-click selection updates shell state only so users can inspect items without changing the open-tab collection.
            ArgumentNullException.ThrowIfNull(explorerItem);
            ArgumentNullException.ThrowIfNull(mouseEventArgs);

            if (mouseEventArgs.Button == 1)
            {
                return Task.CompletedTask;
            }

            ShellManager.SelectExplorerItem(explorerItem.Id);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Opens or focuses the supplied explorer item in the tab strip.
        /// </summary>
        /// <param name="explorerItem">The explorer item whose command should be executed.</param>
        /// <param name="mouseEventArgs">The mouse event that triggered the activation request.</param>
        /// <returns>A task that completes when the activation request has been processed.</returns>
        private async Task OpenExplorerItemAsync(ExplorerItem explorerItem, MouseEventArgs mouseEventArgs)
        {
            // Explorer double-click first preserves explorer selection and then routes opening through the shared command path.
            ArgumentNullException.ThrowIfNull(explorerItem);
            ArgumentNullException.ThrowIfNull(mouseEventArgs);

            if (mouseEventArgs.Button == 1)
            {
                return;
            }

            ShellManager.SelectExplorerItem(explorerItem.Id);
            await ExecuteCommandAsync(explorerItem.CommandId);
        }

        /// <summary>
        /// Executes a Workbench command through the shared shell command path.
        /// </summary>
        /// <param name="commandId">The command identifier that should be executed.</param>
        /// <returns>A task that completes when the command has been processed.</returns>
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
        /// Determines whether the supplied explorer item is the currently selected explorer item.
        /// </summary>
        /// <param name="explorerItem">The explorer item to compare.</param>
        /// <returns><see langword="true"/> when the supplied explorer item is selected; otherwise, <see langword="false"/>.</returns>
        private bool IsExplorerItemSelected(ExplorerItem explorerItem)
        {
            // Explorer entries highlight selection independently from open tabs so single-click selection can remain visible without opening content.
            ArgumentNullException.ThrowIfNull(explorerItem);

            return string.Equals(ShellManager.State.SelectedExplorerItemId, explorerItem.Id, StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns the CSS class used for an activity-rail button.
        /// </summary>
        /// <param name="explorerId">The explorer identifier represented by the button.</param>
        /// <returns>The CSS class string for the button.</returns>
        private string GetActivityButtonCss(string explorerId)
        {
            // The selected explorer button keeps a distinct style so the compact icon rail still behaves like a desktop workbench selector strip.
            return IsExplorerActive(explorerId)
                ? "workbench-shell__activity-button workbench-shell__activity-button--active"
                : "workbench-shell__activity-button";
        }

        /// <summary>
        /// Opens a Radzen tooltip for the supplied activity-rail label.
        /// </summary>
        /// <param name="element">The rendered activity-rail button that should anchor the tooltip.</param>
        /// <param name="displayName">The explorer display name that should be shown in the tooltip.</param>
        private void ShowActivityRailTooltip(ElementReference element, string displayName)
        {
            // The compact icon-only rail still needs discoverable labels, so hover and focus use the shared Radzen tooltip service.
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

            TooltipService.Open(element, displayName);
        }

        /// <summary>
        /// Returns the CSS class used for an explorer tool button.
        /// </summary>
        /// <param name="explorerItem">The explorer item represented by the button.</param>
        /// <returns>The CSS class string for the button.</returns>
        private string GetExplorerItemButtonCss(ExplorerItem explorerItem)
        {
            // Selected-item highlighting makes it clear which explorer item currently owns explorer focus even when no tab is open.
            return IsExplorerItemSelected(explorerItem)
                ? "workbench-shell__tool-button workbench-shell__tool-button--active"
                : "workbench-shell__tool-button";
        }

        /// <summary>
        /// Determines whether the supplied tab is the currently active tab.
        /// </summary>
        /// <param name="openTab">The open tab to compare.</param>
        /// <returns><see langword="true"/> when the supplied tab is active; otherwise, <see langword="false"/>.</returns>
        private bool IsTabActive(WorkbenchTab openTab)
        {
            // Tab-strip active styling follows the shell-state active tab so switching tabs updates the strip and the tool surface together.
            ArgumentNullException.ThrowIfNull(openTab);

            return string.Equals(ShellManager.State.ActiveTab?.Id, openTab.Id, StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns the CSS class used for a tab-strip button.
        /// </summary>
        /// <param name="openTab">The open tab represented by the button.</param>
        /// <returns>The CSS class string for the tab button.</returns>
        private string GetTabButtonCss(WorkbenchTab openTab)
        {
            // The active tab uses a stronger visual treatment so the desktop-like shell keeps focus and tab-strip state aligned.
            return IsTabActive(openTab)
                ? "workbench-shell__tab-button workbench-shell__tab-button--active"
                : "workbench-shell__tab-button";
        }

        /// <summary>
        /// Returns the CSS class used for an overflow entry.
        /// </summary>
        /// <param name="openTab">The open tab represented by the overflow entry.</param>
        /// <returns>The CSS class string for the overflow entry.</returns>
        private string GetOverflowEntryCss(WorkbenchTab openTab)
        {
            // Overflow entries mirror active-tab state so the dropdown immediately shows which logical tab currently owns focus.
            return IsTabActive(openTab)
                ? "workbench-shell__overflow-entry workbench-shell__overflow-entry--active"
                : "workbench-shell__overflow-entry";
        }

        /// <summary>
        /// Focuses an already open tab from the tab strip.
        /// </summary>
        /// <param name="tabId">The stable tab identifier to focus.</param>
        /// <returns>A task that completes when the shell has processed the focus change.</returns>
        private Task ActivateTabAsync(string tabId)
        {
            // Tab-strip selection routes through the shell manager so activity history and contribution composition update consistently.
            ShellManager.ActivateTab(tabId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Activates a tab selected from the overflow dropdown.
        /// </summary>
        /// <param name="selectedValue">The selected overflow value, expected to be a stable tab identifier.</param>
        /// <returns>A task that completes when the shell has processed the overflow activation request.</returns>
        private Task SelectOverflowTabAsync(object? selectedValue)
        {
            // The overflow dropdown shares the shell focus path with the visible strip so activation and minimal window shifting stay consistent.
            if (selectedValue is string tabId && !string.IsNullOrWhiteSpace(tabId))
            {
                ShellManager.ActivateTabFromOverflow(tabId);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Adds the rendered attributes used by overflow entries.
        /// </summary>
        /// <param name="itemRenderEventArgs">The Radzen overflow item-render callback payload.</param>
        private void ConfigureOverflowItemAttributes(DropDownItemRenderEventArgs<string> itemRenderEventArgs)
        {
            // Item-level attributes let the host expose stable hooks for styling and tests without changing the dropdown's built-in list semantics.
            ArgumentNullException.ThrowIfNull(itemRenderEventArgs);

            if (itemRenderEventArgs.Item is not WorkbenchTab openTab)
            {
                return;
            }

            itemRenderEventArgs.Attributes["data-overflow-tab-id"] = openTab.Id;
            itemRenderEventArgs.Attributes["data-overflow-active"] = IsTabActive(openTab).ToString().ToLowerInvariant();
        }

        /// <summary>
        /// Closes an open tab from the tab strip.
        /// </summary>
        /// <param name="tabId">The stable tab identifier to close.</param>
        /// <returns>A task that completes when the shell has processed the close request.</returns>
        private Task CloseTabAsync(string tabId)
        {
            // Tab close requests also flow through the shell manager so the close rule can promote the most recently active remaining tab.
            ShellManager.CloseTab(tabId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Opens the first-implementation tab context menu for the supplied tab.
        /// </summary>
        /// <param name="openTab">The open tab whose context menu should be shown.</param>
        /// <param name="mouseEventArgs">The mouse event that triggered the context-menu request.</param>
        /// <returns>A completed task because the context menu opens synchronously through the injected Radzen service.</returns>
        private Task OpenTabContextMenuAsync(WorkbenchTab openTab, MouseEventArgs mouseEventArgs)
        {
            // The first implementation exposes a minimal context menu so non-active tabs can be closed through the same shell close flow as the active-tab strip button.
            ArgumentNullException.ThrowIfNull(openTab);
            ArgumentNullException.ThrowIfNull(mouseEventArgs);

            if (mouseEventArgs.Button != 2)
            {
                return Task.CompletedTask;
            }

            ContextMenuService.Open(
                mouseEventArgs,
                CreateTabContextMenuItems(openTab),
                menuItemEventArgs => HandleTabContextMenuSelection(menuItemEventArgs.Value));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates the tab context-menu items for the supplied tab.
        /// </summary>
        /// <param name="openTab">The open tab whose context-menu items should be created.</param>
        /// <returns>The context-menu items available for the supplied tab.</returns>
        private static IReadOnlyList<ContextMenuItem> CreateTabContextMenuItems(WorkbenchTab openTab)
        {
            // The initial context menu deliberately exposes only Close so future actions can be added without changing the shared close implementation.
            ArgumentNullException.ThrowIfNull(openTab);

            return
            [
                new ContextMenuItem
                {
                    Text = "Close",
                    Value = openTab.Id,
                    Icon = "close"
                }
            ];
        }

        /// <summary>
        /// Handles selection from the tab context menu.
        /// </summary>
        /// <param name="selectedValue">The selected menu-item value.</param>
        private void HandleTabContextMenuSelection(object? selectedValue)
        {
            // Context-menu close is routed through the same close helper used by the strip button so disposal and next-tab selection remain identical.
            if (selectedValue is string tabId && !string.IsNullOrWhiteSpace(tabId))
            {
                ShellManager.CloseTab(tabId);
            }
        }

        /// <summary>
        /// Ignores middle-click interaction for the first tabbed shell slice.
        /// </summary>
        /// <param name="mouseEventArgs">The mouse event that should be ignored.</param>
        /// <returns>A completed task because middle-click is intentionally a no-op in this slice.</returns>
        private static Task IgnoreAuxiliaryClickAsync(MouseEventArgs mouseEventArgs)
        {
            // Middle-click tab and explorer behavior is explicitly out of scope for the first implementation, so the handler intentionally does nothing.
            ArgumentNullException.ThrowIfNull(mouseEventArgs);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Opens a Radzen tooltip for the supplied title text.
        /// </summary>
        /// <param name="element">The rendered element that should anchor the tooltip.</param>
        /// <param name="title">The full title text that should be shown in the tooltip.</param>
        private void ShowTitleTooltip(ElementReference element, string title)
        {
            // Title tooltips open on every hover so truncated strip and overflow titles remain discoverable without custom timing overrides.
            ArgumentException.ThrowIfNullOrWhiteSpace(title);

            TooltipService.Open(element, title);
        }

        /// <summary>
        /// Closes any currently visible title tooltip.
        /// </summary>
        /// <param name="element">The rendered element whose hover interaction ended.</param>
        private void HideTitleTooltip(ElementReference element)
        {
            // Tooltip closure stays centralized so both strip and overflow hover interactions use the same Radzen tooltip lifecycle.
            TooltipService.Close();
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
