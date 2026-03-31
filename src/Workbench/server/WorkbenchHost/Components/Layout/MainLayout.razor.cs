using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Radzen;
using UKHO.Workbench.Explorers;
using UKHO.Workbench.Layout;
using UKHO.Workbench.Output;
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
    public partial class MainLayout : IDisposable, IAsyncDisposable
    {
        private const string OutputPanelModulePath = "./Components/Layout/MainLayout.razor.js";
        private readonly Dictionary<string, string> _projectedStatusContributionTexts = new(StringComparer.Ordinal);
        private IReadOnlyDictionary<string, string> _lastProjectedContextValues = new Dictionary<string, string>(StringComparer.Ordinal);
        private DotNetObjectReference<MainLayout>? _dotNetObjectReference;
        private ElementReference _outputStreamElement = default;
        private IJSObjectReference? _outputPanelModule;
        private bool _pendingScrollToEnd;

        [Inject]
        private WorkbenchShellManager ShellManager { get; set; } = null!;

        [Inject]
        private IWorkbenchOutputService WorkbenchOutputService { get; set; } = null!;

        [Inject]
        private IJSRuntime JsRuntime { get; set; } = null!;

        [Inject]
        private ILogger<MainLayout> Logger { get; set; } = null!;

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
        /// Gets the current shell-wide output entries in chronological order.
        /// </summary>
        private IReadOnlyList<OutputEntry> OutputEntries => WorkbenchOutputService.Entries;

        /// <summary>
        /// Gets the current output-panel session state used by the shell layout.
        /// </summary>
        private OutputPanelState OutputPanelState => WorkbenchOutputService.PanelState;

        /// <summary>
        /// Gets the row index used by the centre working surface.
        /// </summary>
        private int WorkingAreaRow => 3;

        /// <summary>
        /// Gets the row index used by the output panel when it is visible.
        /// </summary>
        private int OutputPanelRow => 5;

        /// <summary>
        /// Gets the row index used by the status bar for the current shell layout shape.
        /// </summary>
        private int StatusBarRow => OutputPanelState.IsVisible ? 6 : 4;

        /// <summary>
        /// Gets the grid-track height used by the centre working area for the current panel state.
        /// </summary>
        private string WorkingAreaHeight => OutputPanelState.IsVisible ? OutputPanelState.CenterPaneHeight : "*";

        /// <summary>
        /// Gets the grid-track height used by the output panel when it is visible.
        /// </summary>
        private string OutputPaneHeight => OutputPanelState.OutputPaneHeight;

        /// <summary>
        /// Gets the icon shown in the status-bar toggle according to the current panel visibility.
        /// </summary>
        private string OutputToggleIcon => OutputPanelState.IsVisible ? "keyboard_arrow_down" : "keyboard_arrow_up";

        /// <summary>
        /// Gets the hidden-panel unseen level rendered on the collapsed toggle, if any.
        /// </summary>
        private OutputLevel? HiddenUnseenLevel => OutputPanelState.IsVisible ? null : OutputPanelState.HiddenUnseenLevel;

        /// <summary>
        /// Gets the CSS class applied to the output stream according to the current editor-like wrap mode.
        /// </summary>
        private string OutputStreamCss => OutputPanelState.IsWordWrapEnabled
            ? "workbench-shell__output-stream workbench-shell__output-stream--editor-like workbench-shell__output-stream--wrapped"
            : "workbench-shell__output-stream workbench-shell__output-stream--editor-like";

        /// <summary>
        /// Gets the scroll-mode token rendered for the current output viewport state.
        /// </summary>
        private string OutputScrollMode => OutputPanelState.IsWordWrapEnabled ? "wrapped" : "horizontal";

        /// <summary>
        /// Gets the wrap-mode token rendered for the current output surface contract.
        /// </summary>
        private string OutputWrapMode => OutputPanelState.IsWordWrapEnabled ? "wrapped" : "nowrap";

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
            WorkbenchOutputService.EntriesChanged += HandleOutputEntriesChanged;
            WorkbenchOutputService.PanelStateChanged += HandleOutputPanelStateChanged;

            // The shell activates the first registered explorer by default when startup bootstrap has not already selected one.
            if (string.IsNullOrWhiteSpace(ShellManager.State.ActiveExplorerId) && Explorers.Count > 0)
            {
                ShellManager.SetActiveExplorer(Explorers[0].Id);
            }

            // The output-first shell now projects any current shell context and historical status messages into the shared output stream instead of leaving them in the status bar.
            ProjectShellTraceStateToOutput();

            base.OnInitialized();
        }

        /// <summary>
        /// Presents any buffered startup notifications after the interactive shell becomes available.
        /// </summary>
        /// <param name="firstRender"><see langword="true"/> when this is the first completed render for the layout instance.</param>
        /// <returns>A task that completes when startup-notification replay and any output-panel interop work has finished.</returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            // Startup notifications are deferred until first render because the notification service depends on an interactive shell.
            if (firstRender)
            {
                foreach (var notification in StartupNotificationStore.DequeueAll())
                {
                    // Each buffered notification is replayed once so users receive safe startup feedback without repeated noise.
                    WorkbenchOutputService.Write(
                        MapOutputLevel(notification.Severity),
                        "Notifications",
                        notification.Summary,
                        notification.Detail);

                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = notification.Severity,
                        Summary = notification.Summary,
                        Detail = notification.Detail,
                        Duration = 7000
                    });
                }
            }

            // Output interop is only required while the panel is visible because scroll tracking is a panel-local behavior.
            if (!OutputPanelState.IsVisible)
            {
                return;
            }

            try
            {
                await EnsureOutputPanelInteropAsync();

                if (_pendingScrollToEnd && OutputPanelState.IsAutoScrollEnabled)
                {
                    // Deferred scrolling happens after render so the newest output row exists in the DOM before the browser is asked to move the viewport.
                    await _outputPanelModule!.InvokeVoidAsync("scrollToEnd", _outputStreamElement);
                }

                _pendingScrollToEnd = false;
            }
            catch (JSDisconnectedException)
            {
                // Blazor Server can disconnect mid-render; that shutdown path should not surface as a user-facing error.
            }
            catch (TaskCanceledException)
            {
                // Render-driven interop can be cancelled during navigation or shutdown, which is expected.
            }
            catch (ObjectDisposedException)
            {
                // The JS runtime can be disposed during teardown before the final render completes.
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "The Workbench output panel could not complete its post-render interop work.");
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
        /// Toggles the shell-owned output panel between its collapsed and visible states.
        /// </summary>
        /// <returns>A completed task because the state change is handled locally by the layout.</returns>
        private Task ToggleOutputPanelAsync()
        {
            // Visibility toggling routes through the shared panel-state service so unseen-severity reset and session height memory stay centralized.
            return ToggleOutputPanelCoreAsync();
        }

        /// <summary>
        /// Toggles the shell-owned output panel while coordinating any required interop cleanup or deferred scrolling.
        /// </summary>
        /// <returns>A task that completes when the visibility transition work has finished.</returns>
        private async Task ToggleOutputPanelCoreAsync()
        {
            // Closing the panel disposes browser-side helpers, while opening it preserves the shared session height and clears hidden unseen severity through the service.
            try
            {
                if (OutputPanelState.IsVisible)
                {
                    await DisposeOutputPanelInteropAsync();
                    WorkbenchOutputService.SetPanelVisibility(false);
                    return;
                }

                WorkbenchOutputService.SetPanelVisibility(true);
                _pendingScrollToEnd = OutputPanelState.IsAutoScrollEnabled;
            }
            catch (Exception exception) when (exception is not JSDisconnectedException and not TaskCanceledException and not ObjectDisposedException)
            {
                Logger.LogError(exception, "The Workbench output panel visibility toggle failed.");
            }
        }

        /// <summary>
        /// Clears every retained output entry from the current Workbench session.
        /// </summary>
        /// <returns>A completed task because clearing the in-memory stream is handled synchronously by the shared output service.</returns>
        private Task ClearOutputAsync()
        {
            // Clear is intentionally destructive and silent because the specification requires an empty panel with no synthetic replacement entry.
            WorkbenchOutputService.Clear();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Toggles whether the output viewport should automatically remain pinned to the newest entry.
        /// </summary>
        /// <returns>A completed task because the state change is handled synchronously through the shared output service.</returns>
        private Task ToggleAutoScrollAsync()
        {
            // Toolbar toggles mutate the shared state directly so the UI and later row components read one authoritative flag.
            WorkbenchOutputService.SetAutoScrollEnabled(!OutputPanelState.IsAutoScrollEnabled);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Scrolls the output viewport to the newest retained entry and re-enables automatic scrolling.
        /// </summary>
        /// <returns>A task that completes when the browser-side scroll request has been scheduled.</returns>
        private Task ScrollOutputToEndAsync()
        {
            // Scroll-to-end is the explicit recovery path after a user has manually moved away from the newest entries.
            WorkbenchOutputService.SetAutoScrollEnabled(true);
            _pendingScrollToEnd = true;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Toggles whether long output content should wrap within the panel viewport.
        /// </summary>
        /// <returns>A completed task because the wrap toggle only updates shared session state.</returns>
        private Task ToggleWordWrapAsync()
        {
            // Wrap remains a global panel toggle so both compact rows and later expanded details follow the same presentation mode.
            WorkbenchOutputService.SetWordWrapEnabled(!OutputPanelState.IsWordWrapEnabled);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Determines whether the supplied output entry is currently expanded in the shell output surface.
        /// </summary>
        /// <param name="outputEntry">The output entry to evaluate.</param>
        /// <returns><see langword="true"/> when the supplied entry is expanded; otherwise, <see langword="false"/>.</returns>
        private bool IsOutputEntryExpanded(OutputEntry outputEntry)
        {
            // Expansion state is tracked by entry identifier so multiple rows can remain open without mutating the immutable output entries.
            ArgumentNullException.ThrowIfNull(outputEntry);

            return OutputPanelState.ExpandedEntryIds.Contains(outputEntry.Id, StringComparer.Ordinal);
        }

        /// <summary>
        /// Toggles the expanded detail state for one structured output row.
        /// </summary>
        /// <param name="outputEntry">The output entry whose details should be expanded or collapsed.</param>
        /// <returns>A completed task because the shared panel-state update is handled synchronously.</returns>
        private Task ToggleOutputEntryExpansionAsync(OutputEntry outputEntry)
        {
            // The shared panel state keeps expansion centralized so closing and reopening the panel can reset the view consistently.
            ArgumentNullException.ThrowIfNull(outputEntry);

            var expandedEntryIds = OutputPanelState.ExpandedEntryIds.ToList();
            var expandedEntryIndex = expandedEntryIds.FindIndex(entryId => string.Equals(entryId, outputEntry.Id, StringComparison.Ordinal));

            if (expandedEntryIndex >= 0)
            {
                expandedEntryIds.RemoveAt(expandedEntryIndex);
            }
            else
            {
                expandedEntryIds.Add(outputEntry.Id);
            }

            WorkbenchOutputService.SetExpandedEntryIds(expandedEntryIds);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Updates the retained output-panel heights after the shell splitter has been dragged.
        /// </summary>
        /// <param name="resizeNotification">The splitter resize payload raised by the Workbench grid.</param>
        /// <returns>A completed task because the resize state update is handled synchronously.</returns>
        private Task HandleOutputPanelResizeAsync(GridResizeNotification resizeNotification)
        {
            // Only the row splitter between the centre pane and output pane should influence output height memory for this slice.
            ArgumentNullException.ThrowIfNull(resizeNotification);

            if (!OutputPanelState.IsVisible
                || resizeNotification.Direction != GridResizeDirection.Row
                || resizeNotification.PreviousTrackIndex != WorkingAreaRow
                || resizeNotification.NextTrackIndex != OutputPanelRow)
            {
                return Task.CompletedTask;
            }

            WorkbenchOutputService.SetPaneHeights(
                FormatPixelTrackToken(resizeNotification.PreviousTrackSizeInPixels),
                FormatPixelTrackToken(resizeNotification.NextTrackSizeInPixels));

            return Task.CompletedTask;
        }

        /// <summary>
        /// Receives browser-reported output viewport state and disables auto-scroll when the user scrolls away from the newest output.
        /// </summary>
        /// <param name="isAtEnd"><see langword="true"/> when the viewport is still at the newest entry; otherwise, <see langword="false"/>.</param>
        /// <returns>A completed task because the shared state update is synchronous.</returns>
        [JSInvokable]
        public Task NotifyOutputViewportStateAsync(bool isAtEnd)
        {
            // Manual upward scrolling is the only automatic state transition for this slice, so reaching the end again does not silently re-enable auto-scroll.
            if (!isAtEnd && OutputPanelState.IsAutoScrollEnabled)
            {
                WorkbenchOutputService.SetAutoScrollEnabled(false);
            }

            return Task.CompletedTask;
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
            ProjectShellTraceStateToOutput();
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
                WorkbenchOutputService.Write(
                    MapOutputLevel(e.Severity),
                    "Notifications",
                    e.Summary,
                    e.Detail);

                NotificationService.Notify(new NotificationMessage
                {
                    Severity = MapRadzenNotificationSeverity(e.Severity),
                    Summary = e.Summary,
                    Detail = e.Detail,
                    Duration = 5000
                });
            });
        }

        /// <summary>
        /// Projects the current shell context and any newly published status-bar messages into the shared output stream.
        /// </summary>
        private void ProjectShellTraceStateToOutput()
        {
            // The shell keeps the status bar intentionally concise, so historical shell state and status messages are emitted into the output stream instead.
            ProjectStatusBarContributionsToOutput();
            ProjectContextValuesToOutput();
        }

        /// <summary>
        /// Writes newly observed status-bar contribution messages into the output stream.
        /// </summary>
        private void ProjectStatusBarContributionsToOutput()
        {
            // Contribution identifiers remain stable, which lets the shell avoid replaying the same historical status text every time focus returns to a tool.
            foreach (var statusBarContribution in StatusBarContributions)
            {
                if (_projectedStatusContributionTexts.TryGetValue(statusBarContribution.Id, out var previouslyProjectedText)
                    && string.Equals(previouslyProjectedText, statusBarContribution.Text, StringComparison.Ordinal))
                {
                    continue;
                }

                WorkbenchOutputService.Write(
                    OutputLevel.Debug,
                    string.IsNullOrWhiteSpace(statusBarContribution.OwnerToolId) ? "Status" : statusBarContribution.OwnerToolId,
                    statusBarContribution.Text);

                _projectedStatusContributionTexts[statusBarContribution.Id] = statusBarContribution.Text;
            }
        }

        /// <summary>
        /// Writes shell context snapshots into the output stream whenever the projected context changes.
        /// </summary>
        private void ProjectContextValuesToOutput()
        {
            // Context values used to live permanently in the status bar, but the output-first shell now records them as historical context snapshots instead.
            var currentContextValues = new Dictionary<string, string>(ContextValues, StringComparer.Ordinal);
            if (HaveEquivalentContextValues(_lastProjectedContextValues, currentContextValues))
            {
                return;
            }

            _lastProjectedContextValues = currentContextValues;

            var contextDetails = BuildContextDetails(currentContextValues);
            if (string.IsNullOrWhiteSpace(contextDetails))
            {
                return;
            }

            WorkbenchOutputService.Write(
                OutputLevel.Debug,
                "Shell context",
                "Workbench context updated.",
                contextDetails);
        }

        /// <summary>
        /// Determines whether two shell-context snapshots contain the same keys and values.
        /// </summary>
        /// <param name="previousContextValues">The previously projected shell-context snapshot.</param>
        /// <param name="currentContextValues">The current shell-context snapshot.</param>
        /// <returns><see langword="true"/> when both snapshots are equivalent; otherwise, <see langword="false"/>.</returns>
        private static bool HaveEquivalentContextValues(
            IReadOnlyDictionary<string, string> previousContextValues,
            IReadOnlyDictionary<string, string> currentContextValues)
        {
            // Comparing the normalized key/value pairs keeps context projection stable without depending on dictionary iteration order.
            ArgumentNullException.ThrowIfNull(previousContextValues);
            ArgumentNullException.ThrowIfNull(currentContextValues);

            if (previousContextValues.Count != currentContextValues.Count)
            {
                return false;
            }

            foreach (var currentContextEntry in currentContextValues)
            {
                if (!previousContextValues.TryGetValue(currentContextEntry.Key, out var previousValue)
                    || !string.Equals(previousValue, currentContextEntry.Value, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Builds the output-panel detail text for a shell-context snapshot.
        /// </summary>
        /// <param name="contextValues">The context snapshot that should be converted into output detail text.</param>
        /// <returns>The formatted detail text, or an empty string when no meaningful context values are present.</returns>
        private static string BuildContextDetails(IReadOnlyDictionary<string, string> contextValues)
        {
            // The shell records only meaningful context values so the output stream stays useful instead of repeating empty placeholders.
            ArgumentNullException.ThrowIfNull(contextValues);

            var contextLines = new List<string>();

            AddContextLine(contextLines, "Active explorer", GetContextValue(contextValues, WorkbenchContextKeys.ActiveExplorer));
            AddContextLine(contextLines, "Active tool", GetContextValue(contextValues, WorkbenchContextKeys.ActiveTool));
            AddContextLine(contextLines, "Active region", GetContextValue(contextValues, WorkbenchContextKeys.ActiveRegion));
            AddContextLine(contextLines, "Selection type", GetContextValue(contextValues, WorkbenchContextKeys.SelectionType));

            var selectionCount = GetContextValue(contextValues, WorkbenchContextKeys.SelectionCount);
            if (!string.IsNullOrWhiteSpace(selectionCount)
                && !string.Equals(selectionCount, "0", StringComparison.Ordinal))
            {
                AddContextLine(contextLines, "Selection count", selectionCount);
            }

            var toolSurfaceReady = GetContextValue(contextValues, WorkbenchContextKeys.ToolSurfaceReady);
            if (!string.IsNullOrWhiteSpace(toolSurfaceReady)
                && bool.TryParse(toolSurfaceReady, out var isToolSurfaceReady)
                && isToolSurfaceReady)
            {
                AddContextLine(contextLines, "Tool surface ready", toolSurfaceReady);
            }

            return string.Join(Environment.NewLine, contextLines);
        }

        /// <summary>
        /// Returns one shell-context value from the supplied snapshot.
        /// </summary>
        /// <param name="contextValues">The context snapshot that owns the requested key.</param>
        /// <param name="key">The context key that should be read.</param>
        /// <returns>The matching context value, or an empty string when the key is absent.</returns>
        private static string GetContextValue(IReadOnlyDictionary<string, string> contextValues, string key)
        {
            // Missing context values are treated as empty so context-detail rendering can stay concise.
            ArgumentNullException.ThrowIfNull(contextValues);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            return contextValues.TryGetValue(key, out var value)
                ? value
                : string.Empty;
        }

        /// <summary>
        /// Appends one formatted shell-context detail line when the supplied value is meaningful.
        /// </summary>
        /// <param name="contextLines">The context-detail lines being accumulated.</param>
        /// <param name="label">The human-readable label that should prefix the context value.</param>
        /// <param name="value">The context value that should be added when present.</param>
        private static void AddContextLine(List<string> contextLines, string label, string value)
        {
            // The helper centralizes blank-value filtering so context-detail formatting stays consistent across all shell keys.
            ArgumentNullException.ThrowIfNull(contextLines);
            ArgumentException.ThrowIfNullOrWhiteSpace(label);

            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            contextLines.Add($"{label}: {value}");
        }

        /// <summary>
        /// Maps a simple Workbench notification severity string to the corresponding Radzen severity.
        /// </summary>
        /// <param name="severity">The shell notification severity expressed as a simple string value.</param>
        /// <returns>The Radzen notification severity used to render the message.</returns>
        private static NotificationSeverity MapRadzenNotificationSeverity(string severity)
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
        /// Maps a simple Workbench notification severity string to the corresponding output severity.
        /// </summary>
        /// <param name="severity">The shell notification severity expressed as a simple string value.</param>
        /// <returns>The output severity used by the shared Workbench output stream.</returns>
        private static OutputLevel MapOutputLevel(string severity)
        {
            // Notification mirroring uses the output panel's severity model so toasts and output rows stay aligned at the shell level.
            ArgumentException.ThrowIfNullOrWhiteSpace(severity);

            return severity.ToLowerInvariant() switch
            {
                "success" => OutputLevel.Info,
                "warning" => OutputLevel.Warning,
                "error" => OutputLevel.Error,
                _ => OutputLevel.Info
            };
        }

        /// <summary>
        /// Maps a Radzen notification severity to the corresponding output severity.
        /// </summary>
        /// <param name="severity">The Radzen notification severity value.</param>
        /// <returns>The output severity used by the shared Workbench output stream.</returns>
        private static OutputLevel MapOutputLevel(NotificationSeverity severity)
        {
            // Startup notification replay also mirrors into the output panel, so the host converts Radzen-specific severities back into shell output levels here.
            return severity switch
            {
                NotificationSeverity.Success => OutputLevel.Info,
                NotificationSeverity.Warning => OutputLevel.Warning,
                NotificationSeverity.Error => OutputLevel.Error,
                _ => OutputLevel.Info
            };
        }

        /// <summary>
        /// Formats a pixel size value for reuse as a CSS grid-track token.
        /// </summary>
        /// <param name="sizeInPixels">The pixel size reported by the Workbench splitter interop.</param>
        /// <returns>The pixel token used by the shell grid state.</returns>
        private static string FormatPixelTrackToken(double sizeInPixels)
        {
            // Persisting pixel tokens preserves the user's exact in-session splitter adjustment without introducing cross-session layout storage.
            return $"{Math.Round(sizeInPixels, 2):0.##}px";
        }

        /// <summary>
        /// Ensures the output-panel JavaScript module is loaded and attached to the current output viewport.
        /// </summary>
        /// <returns>A task that completes when the browser helper has been initialized.</returns>
        private async Task EnsureOutputPanelInteropAsync()
        {
            // The module is loaded lazily so collapsed panels do not pay any browser-side setup cost.
            if (_outputPanelModule is not null)
            {
                return;
            }

            _outputPanelModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", OutputPanelModulePath);
            _dotNetObjectReference = DotNetObjectReference.Create(this);
            await _outputPanelModule.InvokeVoidAsync("initializeOutputPanel", _outputStreamElement, _dotNetObjectReference);
        }

        /// <summary>
        /// Releases any browser-side resources owned by the output-panel helper module.
        /// </summary>
        /// <returns>A task that completes when the interop resources have been released.</returns>
        private async Task DisposeOutputPanelInteropAsync()
        {
            // Output-panel interop is short-lived and panel-scoped, so closing the panel tears it down immediately instead of waiting for component disposal.
            if (_outputPanelModule is not null)
            {
                try
                {
                    await _outputPanelModule.InvokeVoidAsync("disposeOutputPanel", _outputStreamElement);
                    await _outputPanelModule.DisposeAsync();
                }
                catch (JSDisconnectedException)
                {
                    // The circuit can disconnect before disposal completes; there is nothing else to clean up in that path.
                }
                catch (TaskCanceledException)
                {
                    // Browser-side teardown can be cancelled during shutdown.
                }
                catch (ObjectDisposedException)
                {
                    // The JS runtime can already be gone during final teardown.
                }

                _outputPanelModule = null;
            }

            _dotNetObjectReference?.Dispose();
            _dotNetObjectReference = null;
        }

        /// <summary>
        /// Responds to output-stream changes by requesting a shell re-render.
        /// </summary>
        /// <param name="sender">The object that raised the change notification.</param>
        /// <param name="e">The event arguments for the change notification.</param>
        private void HandleOutputEntriesChanged(object? sender, EventArgs e)
        {
            // Output-stream changes can arrive while the panel is either collapsed or visible, so the shell always schedules a safe UI refresh.
            if (OutputPanelState.IsVisible && OutputPanelState.IsAutoScrollEnabled)
            {
                _pendingScrollToEnd = true;
            }

            _ = InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Responds to shared output-panel state changes by requesting a shell re-render.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments for the notification.</param>
        private void HandleOutputPanelStateChanged(object? sender, EventArgs e)
        {
            // Panel-state changes can affect layout rows, toolbar pressed states, and hidden severity indicators, so the shell re-renders from one place.
            _ = InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Unsubscribes from shell state notifications when the layout is disposed.
        /// </summary>
        public void Dispose()
        {
            // Layout disposal must release the event subscription to avoid retaining old component instances across reconnects or test renders.
            ShellManager.StateChanged -= HandleShellStateChanged;
            ShellManager.NotificationRaised -= HandleWorkbenchNotificationRaised;
            WorkbenchOutputService.EntriesChanged -= HandleOutputEntriesChanged;
            WorkbenchOutputService.PanelStateChanged -= HandleOutputPanelStateChanged;
        }

        /// <summary>
        /// Releases the output-panel interop resources when the layout is disposed asynchronously.
        /// </summary>
        /// <returns>A task that completes when the owned interop resources have been released.</returns>
        public async ValueTask DisposeAsync()
        {
            // Async disposal complements the synchronous event unsubscription path by releasing any browser-side output helpers the layout created.
            Dispose();
            await DisposeOutputPanelInteropAsync();
        }
    }
}
