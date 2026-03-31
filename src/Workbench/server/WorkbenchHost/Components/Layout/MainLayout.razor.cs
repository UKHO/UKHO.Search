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
using XtermBlazor;
using XtermBlazorTheme = XtermBlazor.Theme;

namespace WorkbenchHost.Components.Layout
{
    /// <summary>
    /// Renders and coordinates the bootstrap desktop-like Workbench shell.
    /// </summary>
    public partial class MainLayout : IDisposable, IAsyncDisposable
    {
        private const string OutputPanelModulePath = "./Components/Layout/MainLayout.razor.js";
        private const string OutputTerminalFitAddonId = "addon-fit";
        private const string OutputTerminalSearchAddonId = "addon-search";
        private const string OutputTerminalSeverityReset = "\u001b[0m";
        private static readonly string[] OutputDetailLineSeparators = ["\r\n", "\n", "\r"];
        private readonly Dictionary<string, string> _projectedStatusContributionTexts = new(StringComparer.Ordinal);
        private readonly HashSet<string> _outputTerminalAddons = [OutputTerminalFitAddonId, OutputTerminalSearchAddonId];
        private readonly List<OutputEntry> _pendingOutputTerminalEntries = [];
        private readonly TerminalOptions _outputTerminalOptions = CreateOutputTerminalOptions();
        private IReadOnlyDictionary<string, string> _lastProjectedContextValues = new Dictionary<string, string>(StringComparer.Ordinal);
        private IReadOnlyList<string> _projectedOutputEntryIds = [];
        private DotNetObjectReference<MainLayout>? _dotNetObjectReference;
        private Xterm? _outputTerminal;
        private ElementReference _outputFindInput = default;
        private ElementReference _outputStreamElement = default;
        private IJSObjectReference? _outputPanelModule;
        private bool _isOutputFindSurfaceVisible;
        private bool _isOutputSelectionAvailable;
        private bool _isOutputTerminalReady;
        private bool _outputFindInputShouldReceiveFocus;
        private string _outputFindText = string.Empty;
        private bool _outputTerminalNeedsPresentationRefresh = true;
        private bool _outputTerminalNeedsRebuild = true;
        private int _outputTerminalRenderKey;
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
        /// Gets the terminal options used for the read-only Workbench output surface.
        /// </summary>
        private TerminalOptions OutputTerminalOptions => _outputTerminalOptions;

        /// <summary>
        /// Gets a value indicating whether the hosted terminal currently exposes an active text selection.
        /// </summary>
        private bool IsOutputSelectionAvailable => _isOutputSelectionAvailable;

        /// <summary>
        /// Gets a value indicating whether the panel-local find workflow is currently visible.
        /// </summary>
        private bool IsOutputFindVisible => _isOutputFindSurfaceVisible;

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

                // Terminal synchronization is deferred until both the Blazor component reference and the browser-side terminal instance are ready.
                if (_isOutputTerminalReady)
                {
                    await SynchronizeOutputTerminalAsync();
                }

                // Find input focus happens after the panel re-renders so both toolbar and Ctrl+F can reveal a usable search field immediately.
                if (_outputFindInputShouldReceiveFocus && IsOutputFindVisible)
                {
                    await _outputFindInput.FocusAsync();
                    _outputFindInputShouldReceiveFocus = false;
                }
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
                    ResetOutputTerminalProjectionState();
                    await DisposeOutputPanelInteropAsync();
                    WorkbenchOutputService.SetPanelVisibility(false);
                    return;
                }

                InvalidateOutputTerminalProjection();
                _outputTerminalNeedsPresentationRefresh = true;
                WorkbenchOutputService.SetPanelVisibility(true);
                _pendingScrollToEnd = OutputPanelState.IsAutoScrollEnabled;
            }
            catch (Exception exception) when (exception is not JSDisconnectedException and not TaskCanceledException and not ObjectDisposedException)
            {
                Logger.LogError(exception, "The Workbench output panel visibility toggle failed.");
            }
        }

        /// <summary>
        /// Copies the currently selected terminal text to the clipboard when the hosted output surface reports an active selection.
        /// </summary>
        /// <returns>A task that completes when the copy request has been issued.</returns>
        private async Task CopySelectedOutputAsync()
        {
            // Copy remains terminal-driven so the toolbar never guesses selection state and instead mirrors the hosted surface exactly.
            if (_outputTerminal is null || !_isOutputTerminalReady || !IsOutputSelectionAvailable)
            {
                return;
            }

            var selectedText = await _outputTerminal.GetSelection();
            if (string.IsNullOrWhiteSpace(selectedText))
            {
                _isOutputSelectionAvailable = false;
                await RequestLayoutRefreshAsync();
                return;
            }

            await EnsureOutputPanelInteropAsync();

            if (_outputPanelModule is null)
            {
                return;
            }

            await _outputPanelModule.InvokeVoidAsync("copyTextToClipboard", selectedText);
        }

        /// <summary>
        /// Clears every retained output entry from the current Workbench session.
        /// </summary>
        /// <returns>A completed task because clearing the in-memory stream is handled synchronously by the shared output service.</returns>
        private Task ClearOutputAsync()
        {
            // Clear is intentionally destructive and silent because the specification requires an empty panel with no synthetic replacement entry.
            _isOutputSelectionAvailable = false;
            WorkbenchOutputService.Clear();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Opens the terminal-native find workflow from the panel toolbar.
        /// </summary>
        /// <returns>A task that completes when the find surface has been made visible.</returns>
        private Task OpenOutputFindAsync()
        {
            // Toolbar and keyboard search both route through one helper so the panel always reveals the same terminal-focused find experience.
            _isOutputFindSurfaceVisible = true;
            _outputFindInputShouldReceiveFocus = true;
            return RequestLayoutRefreshAsync();
        }

        /// <summary>
        /// Closes the terminal-native find workflow and clears any active terminal search decorations.
        /// </summary>
        /// <returns>A task that completes when the find workflow has been dismissed.</returns>
        private async Task CloseOutputFindAsync()
        {
            // Closing find hides the panel-local chrome and removes stale highlights so later searches start from a clean terminal state.
            _isOutputFindSurfaceVisible = false;
            _outputFindInputShouldReceiveFocus = false;
            await ClearOutputTerminalFindDecorationsAsync();
            await RequestLayoutRefreshAsync();
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
            return RequestLayoutRefreshAsync();
        }

        /// <summary>
        /// Updates the find text from the panel-local search input and triggers incremental terminal searching when meaningful text is present.
        /// </summary>
        /// <param name="changeEventArgs">The change payload raised by the search input.</param>
        /// <returns>A task that completes when any incremental search request has been issued.</returns>
        private async Task HandleOutputFindInputAsync(ChangeEventArgs changeEventArgs)
        {
            // Incremental search keeps the first-delivery find workflow lightweight by moving directly to visible matches as the user types.
            ArgumentNullException.ThrowIfNull(changeEventArgs);

            _outputFindText = changeEventArgs.Value?.ToString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(_outputFindText))
            {
                await ClearOutputTerminalFindDecorationsAsync();
                return;
            }

            await FindNextOutputMatchAsync();
        }

        /// <summary>
        /// Handles keyboard shortcuts inside the panel-local search input.
        /// </summary>
        /// <param name="keyboardEventArgs">The keyboard event raised by the search input.</param>
        /// <returns>A task that completes when the requested find action has finished.</returns>
        private Task HandleOutputFindKeyDownAsync(KeyboardEventArgs keyboardEventArgs)
        {
            // Enter repeats the active search while Escape dismisses the find surface so keyboard-only users do not need to leave the terminal workflow.
            ArgumentNullException.ThrowIfNull(keyboardEventArgs);

            if (string.Equals(keyboardEventArgs.Key, "Enter", StringComparison.OrdinalIgnoreCase))
            {
                return keyboardEventArgs.ShiftKey
                    ? FindPreviousOutputMatchAsync()
                    : FindNextOutputMatchAsync();
            }

            if (string.Equals(keyboardEventArgs.Key, "Escape", StringComparison.OrdinalIgnoreCase))
            {
                return CloseOutputFindAsync();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Finds the next terminal match for the current search text.
        /// </summary>
        /// <returns>A task that completes when the search addon has processed the request.</returns>
        private Task FindNextOutputMatchAsync()
        {
            // Forward search remains the default toolbar and Enter behavior so repeated search requests keep moving deeper into the retained stream.
            return SearchOutputAsync(searchBackward: false);
        }

        /// <summary>
        /// Finds the previous terminal match for the current search text.
        /// </summary>
        /// <returns>A task that completes when the search addon has processed the request.</returns>
        private Task FindPreviousOutputMatchAsync()
        {
            // Reverse search gives the toolbar and Shift+Enter path a clean way to walk back through historical matches.
            return SearchOutputAsync(searchBackward: true);
        }

        /// <summary>
        /// Searches the hosted terminal for the current search text by using the official xterm.js search addon.
        /// </summary>
        /// <param name="searchBackward"><see langword="true"/> to search toward older output; otherwise, <see langword="false"/> to search toward newer output.</param>
        /// <returns>A task that completes when the search addon has processed the request.</returns>
        private async Task SearchOutputAsync(bool searchBackward)
        {
            // Search requests stay terminal-native so match navigation uses the addon that already understands wrapped terminal content and viewport focus.
            if (_outputTerminal is null || !_isOutputTerminalReady || string.IsNullOrWhiteSpace(_outputFindText))
            {
                return;
            }

            var searchOptions = CreateOutputTerminalSearchOptions();
            if (searchBackward)
            {
                await _outputTerminal.Addon(OutputTerminalSearchAddonId).InvokeAsync<bool>("findPrevious", _outputFindText, searchOptions);
                return;
            }

            await _outputTerminal.Addon(OutputTerminalSearchAddonId).InvokeAsync<bool>("findNext", _outputFindText, searchOptions);
        }

        /// <summary>
        /// Clears any active terminal search decorations when the panel-local find workflow is dismissed or reset.
        /// </summary>
        /// <returns>A task that completes when the search addon has cleared its active highlights.</returns>
        private async Task ClearOutputTerminalFindDecorationsAsync()
        {
            // Clearing addon decorations keeps the next search honest and avoids leaving stale highlight state behind after the find strip closes.
            if (_outputTerminal is null || !_isOutputTerminalReady)
            {
                return;
            }

            await _outputTerminal.Addon(OutputTerminalSearchAddonId).InvokeVoidAsync("clearDecorations");
        }

        /// <summary>
        /// Writes the current retained Workbench output history into the newly rendered terminal instance.
        /// </summary>
        /// <returns>A task that completes when the retained terminal projection has been written.</returns>
        private async Task HandleOutputTerminalFirstRenderAsync()
        {
            // The terminal becomes ready only after the child component finishes its own first render, so the layout records readiness before rebuilding from shared state.
            if (_outputTerminal is null)
            {
                return;
            }

            _isOutputTerminalReady = true;
            _outputTerminalNeedsRebuild = true;

            // The initial terminal mount needs an immediate sync because the parent layout does not automatically re-render when the child first-render callback completes.
            try
            {
                await EnsureOutputPanelInteropAsync();
                await SynchronizeOutputTerminalAsync();
            }
            catch (JSDisconnectedException)
            {
                // Blazor Server can disconnect during the initial mount path, which should not surface as a user-facing error.
            }
            catch (TaskCanceledException)
            {
                // Initial interop work can be cancelled during navigation or shutdown.
            }
            catch (ObjectDisposedException)
            {
                // The JS runtime can already be disposed during teardown before the first terminal sync completes.
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "The Workbench output terminal could not complete its initial fit and synchronization pass.");
            }
        }

        /// <summary>
        /// Receives browser-reported terminal selection-state changes so the copy toolbar action can enable only when a real selection exists.
        /// </summary>
        /// <param name="hasSelection"><see langword="true"/> when the terminal currently exposes selected text; otherwise, <see langword="false"/>.</param>
        /// <returns>A task that completes when the layout has applied the selection-state change.</returns>
        [JSInvokable]
        public Task NotifyOutputSelectionStateAsync(bool hasSelection)
        {
            // Selection changes are presentation-only state, so the layout simply mirrors the terminal state and re-renders the toolbar when needed.
            if (_isOutputSelectionAvailable == hasSelection)
            {
                return Task.CompletedTask;
            }

            _isOutputSelectionAvailable = hasSelection;
            return RequestLayoutRefreshAsync();
        }

        /// <summary>
        /// Copies the current terminal selection when the browser forwards the standard keyboard copy shortcut from the focused output surface.
        /// </summary>
        /// <returns>A task that completes when the copy request has finished.</returns>
        [JSInvokable]
        public Task NotifyOutputCopyShortcutAsync()
        {
            // Keyboard copy should reuse the same terminal-backed selection path as the toolbar so both entry points stay behaviorally identical.
            return CopySelectedOutputAsync();
        }

        /// <summary>
        /// Probes the hosted terminal for its current selection state after browser gestures such as mouse drag selection complete.
        /// </summary>
        /// <returns>A task that completes when the terminal-backed selection state has been refreshed.</returns>
        [JSInvokable]
        public Task ProbeOutputSelectionStateAsync()
        {
            // Browser gesture notifications can arrive even when DOM selection text is unavailable, so the shell re-queries xterm directly here.
            return HandleOutputTerminalSelectionChangedAsync();
        }

        /// <summary>
        /// Mirrors the hosted terminal's selection state into the output toolbar by querying the terminal directly.
        /// </summary>
        /// <returns>A task that completes when the selection state has been refreshed.</returns>
        private async Task HandleOutputTerminalSelectionChangedAsync()
        {
            // Native xterm selection notifications are more reliable than DOM-selection heuristics, so the layout queries the terminal itself before updating the toolbar.
            if (_outputTerminal is null || !_isOutputTerminalReady)
            {
                return;
            }

            var hasSelection = await _outputTerminal.HasSelection();
            if (_isOutputSelectionAvailable == hasSelection)
            {
                return;
            }

            _isOutputSelectionAvailable = hasSelection;
            await RequestLayoutRefreshAsync();
        }

        /// <summary>
        /// Receives the Ctrl+F keyboard shortcut from the hosted terminal surface and opens the panel-local find workflow.
        /// </summary>
        /// <returns>A task that completes when the find workflow has been revealed.</returns>
        [JSInvokable]
        public Task NotifyOutputFindShortcutAsync()
        {
            // The browser forwards Ctrl+F here so the shell, not ad-hoc JavaScript, remains responsible for showing and managing the find UI.
            return OpenOutputFindAsync();
        }

        /// <summary>
        /// Receives browser-reported shell-theme changes so the hosted terminal can refresh its palette without becoming the source of truth for theme state.
        /// </summary>
        /// <returns>A task that completes when the layout has queued the presentation refresh.</returns>
        [JSInvokable]
        public Task NotifyOutputThemeStateChangedAsync()
        {
            // Theme changes are handled as presentation-only refreshes so the retained output stream and shared panel state remain untouched.
            _outputTerminalNeedsPresentationRefresh = true;
            return InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Receives browser-reported host resize notifications so the terminal can refit against the latest output-panel dimensions.
        /// </summary>
        /// <returns>A task that completes when the layout has queued the fit refresh.</returns>
        [JSInvokable]
        public Task NotifyOutputHostResizedAsync()
        {
            // Browser-driven resize notifications should only refit the hosted terminal, because forcing a full layout re-render on every resize creates unnecessary shell churn.
            return InvokeAsync(async () =>
            {
                if (!OutputPanelState.IsVisible || !_isOutputTerminalReady)
                {
                    return;
                }

                try
                {
                    await FitOutputTerminalAsync();
                }
                catch (JSDisconnectedException)
                {
                    // Blazor Server can disconnect while resize-driven interop is in flight.
                }
                catch (TaskCanceledException)
                {
                    // Resize-driven fit work can be cancelled during navigation or shutdown.
                }
                catch (ObjectDisposedException)
                {
                    // The JS runtime can be disposed before a queued resize notification finishes.
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "The Workbench output terminal could not refit after a host resize notification.");
                }
            });
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
                FormatProportionalTrackToken(
                    resizeNotification.PreviousTrackSizeInPixels,
                    resizeNotification.PreviousTrackSizeInPixels,
                    resizeNotification.NextTrackSizeInPixels),
                FormatProportionalTrackToken(
                    resizeNotification.NextTrackSizeInPixels,
                    resizeNotification.PreviousTrackSizeInPixels,
                    resizeNotification.NextTrackSizeInPixels));

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
        private static string FormatProportionalTrackToken(double trackSizeInPixels, double firstTrackSizeInPixels, double secondTrackSizeInPixels)
        {
            // Persisting proportional star tokens lets the shell remember the splitter ratio while still allowing the grid to absorb later browser resizes without clipping the status bar.
            var smallestTrackSize = Math.Min(firstTrackSizeInPixels, secondTrackSizeInPixels);
            if (trackSizeInPixels <= 0 || smallestTrackSize <= 0)
            {
                return "1*";
            }

            var normalizedWeight = trackSizeInPixels / smallestTrackSize;
            return $"{Math.Round(normalizedWeight, 3):0.###}*";
        }

        /// <summary>
        /// Builds the full retained terminal projection used by the Workbench output surface.
        /// </summary>
        /// <param name="outputEntries">The retained output entries that should be projected into terminal text.</param>
        /// <returns>The newline-delimited terminal projection text for the supplied retained output entries.</returns>
        private static string BuildOutputTerminalProjection(IReadOnlyList<OutputEntry> outputEntries)
        {
            // The baseline projection keeps retained-history formatting deterministic for tests and future append/rebuild work without changing shared output ownership.
            ArgumentNullException.ThrowIfNull(outputEntries);

            return string.Join(
                Environment.NewLine,
                outputEntries.SelectMany(BuildOutputTerminalLines));
        }

        /// <summary>
        /// Builds the renderable terminal lines for one retained output entry, including ANSI severity styling for the summary line.
        /// </summary>
        /// <param name="outputEntry">The retained output entry that should be projected into renderable terminal lines.</param>
        /// <returns>The projected terminal lines ready to be written to the terminal surface.</returns>
        private static IReadOnlyList<string> BuildOutputTerminalRenderableLines(OutputEntry outputEntry)
        {
            // The renderable projection keeps the plain-text contract intact for tests while layering ANSI styling onto the terminal-only summary line.
            ArgumentNullException.ThrowIfNull(outputEntry);

            var projectedLines = BuildOutputTerminalLines(outputEntry).ToArray();
            projectedLines[0] = ApplyOutputTerminalSeverityStyling(outputEntry.Level, projectedLines[0]);
            return projectedLines;
        }

        /// <summary>
        /// Builds the terminal lines for one retained output entry.
        /// </summary>
        /// <param name="outputEntry">The retained output entry that should be projected into terminal lines.</param>
        /// <returns>The projected terminal lines for the supplied output entry.</returns>
        private static IReadOnlyList<string> BuildOutputTerminalLines(OutputEntry outputEntry)
        {
            // Each entry becomes one summary line followed by any inline details and optional event code so the terminal stays chronological and scan-friendly.
            ArgumentNullException.ThrowIfNull(outputEntry);

            var projectedLines = new List<string>
            {
                BuildOutputTerminalSummaryLine(outputEntry)
            };

            projectedLines.AddRange(BuildOutputTerminalDetailLines(outputEntry.Details));

            if (!string.IsNullOrWhiteSpace(outputEntry.EventCode))
            {
                // Event codes remain visually grouped with their related summary line by using the same inline indentation contract as detail text.
                projectedLines.Add($"  Event code: {outputEntry.EventCode}");
            }

            return projectedLines;
        }

        /// <summary>
        /// Builds the summary line for one retained output entry.
        /// </summary>
        /// <param name="outputEntry">The retained output entry that should be converted into a summary line.</param>
        /// <returns>The projected summary line for the supplied output entry.</returns>
        private static string BuildOutputTerminalSummaryLine(OutputEntry outputEntry)
        {
            // The summary line preserves the existing timestamp, source, and summary ordering contract while moving the presentation into a terminal surface.
            ArgumentNullException.ThrowIfNull(outputEntry);

            return $"{outputEntry.TimestampUtc.ToLocalTime():HH:mm:ss} {outputEntry.Source} {outputEntry.Summary}";
        }

        /// <summary>
        /// Builds the inline terminal detail lines for one retained output detail payload.
        /// </summary>
        /// <param name="details">The optional detail payload that should be projected beneath the related summary line.</param>
        /// <returns>The ordered inline terminal detail lines for the supplied payload.</returns>
        private static IReadOnlyList<string> BuildOutputTerminalDetailLines(string? details)
        {
            // Supported newline variants are normalized once so retained diagnostic detail renders consistently regardless of the originating platform.
            if (string.IsNullOrWhiteSpace(details))
            {
                return Array.Empty<string>();
            }

            return details
                .Split(OutputDetailLineSeparators, StringSplitOptions.None)
                .Select(detailLine => $"  {detailLine}")
                .ToArray();
        }

        /// <summary>
        /// Applies ANSI severity styling to a terminal summary line while preserving the existing summary text contract.
        /// </summary>
        /// <param name="level">The severity level that should control the rendered styling.</param>
        /// <param name="summaryLine">The plain summary line text that should be wrapped with ANSI styling.</param>
        /// <returns>The ANSI-styled summary line for terminal rendering.</returns>
        private static string ApplyOutputTerminalSeverityStyling(OutputLevel level, string summaryLine)
        {
            // ANSI styling keeps the textual summary contract unchanged while making severity visually distinct inside the terminal surface.
            ArgumentException.ThrowIfNullOrWhiteSpace(summaryLine);

            var severityEscapeSequence = level switch
            {
                OutputLevel.Debug => "\u001b[90m",
                OutputLevel.Info => "\u001b[94m",
                OutputLevel.Warning => "\u001b[93;1m",
                OutputLevel.Error => "\u001b[91;1m",
                _ => OutputTerminalSeverityReset
            };

            return string.Concat(severityEscapeSequence, summaryLine, OutputTerminalSeverityReset);
        }

        /// <summary>
        /// Creates the search options used by the official xterm.js search addon for the Workbench output panel.
        /// </summary>
        /// <returns>The terminal search options used for panel-local find operations.</returns>
        private static IReadOnlyDictionary<string, object> CreateOutputTerminalSearchOptions()
        {
            // The first-delivery search experience stays intentionally lightweight by using incremental, case-insensitive literal matching.
            return new Dictionary<string, object>
            {
                ["caseSensitive"] = false,
                ["incremental"] = true,
                ["regex"] = false,
                ["wholeWord"] = false
            };
        }

        /// <summary>
        /// Returns the append-only output entries that can be written directly to the terminal, or <see langword="null"/> when the terminal must rebuild from retained shared state.
        /// </summary>
        /// <param name="currentEntries">The current retained output entries owned by the shared output service.</param>
        /// <param name="projectedEntryIds">The entry identifiers already projected into the terminal, including any queued appends.</param>
        /// <returns>The append-only entries that can be written directly, or <see langword="null"/> when a rebuild is required.</returns>
        private static IReadOnlyList<OutputEntry>? BuildOutputTerminalAppendEntries(
            IReadOnlyList<OutputEntry> currentEntries,
            IReadOnlyList<string> projectedEntryIds)
        {
            // Direct appends are safe only when the retained history still starts with the entries already written to the terminal.
            ArgumentNullException.ThrowIfNull(currentEntries);
            ArgumentNullException.ThrowIfNull(projectedEntryIds);

            if (currentEntries.Count < projectedEntryIds.Count)
            {
                return null;
            }

            for (var entryIndex = 0; entryIndex < projectedEntryIds.Count; entryIndex++)
            {
                if (!string.Equals(currentEntries[entryIndex].Id, projectedEntryIds[entryIndex], StringComparison.Ordinal))
                {
                    return null;
                }
            }

            return currentEntries
                .Skip(projectedEntryIds.Count)
                .ToArray();
        }

        /// <summary>
        /// Creates the terminal options used by the read-only Workbench output surface.
        /// </summary>
        /// <returns>The terminal options used for the Workbench output projection.</returns>
        private static TerminalOptions CreateOutputTerminalOptions(IReadOnlyDictionary<string, string>? themeValues = null)
        {
            // The output terminal remains read-only and shell-aligned, while optional theme values let the host follow the active Radzen light or dark appearance.
            var terminalOptions = new TerminalOptions
            {
                CursorBlink = false,
                DisableStdin = true,
                FontFamily = "Consolas, \"Courier New\", monospace",
                FontSize = 12,
                Theme = BuildOutputTerminalTheme(themeValues)
            };

            return terminalOptions;
        }

        /// <summary>
        /// Builds the terminal theme that keeps the output surface aligned with the current shell appearance tokens.
        /// </summary>
        /// <param name="themeValues">The browser-derived terminal theme values, or <see langword="null"/> to use package defaults until browser values are available.</param>
        /// <returns>The terminal theme that should be applied to the hosted output surface.</returns>
        private static XtermBlazorTheme BuildOutputTerminalTheme(IReadOnlyDictionary<string, string>? themeValues)
        {
            // The theme helper maps browser-derived shell tokens into xterm.js palette values so ANSI severity colors remain readable in both light and dark modes.
            return new XtermBlazorTheme
            {
                Background = GetOutputTerminalThemeValue(themeValues, "background"),
                Foreground = GetOutputTerminalThemeValue(themeValues, "foreground"),
                Cursor = GetOutputTerminalThemeValue(themeValues, "cursor"),
                CursorAccent = GetOutputTerminalThemeValue(themeValues, "cursorAccent"),
                SelectionBackground = GetOutputTerminalThemeValue(themeValues, "selectionBackground"),
                SelectionInactiveBackground = GetOutputTerminalThemeValue(themeValues, "selectionInactiveBackground"),
                ScrollbarSliderBackground = GetOutputTerminalThemeValue(themeValues, "scrollbarSliderBackground"),
                ScrollbarSliderHoverBackground = GetOutputTerminalThemeValue(themeValues, "scrollbarSliderHoverBackground"),
                ScrollbarSliderActiveBackground = GetOutputTerminalThemeValue(themeValues, "scrollbarSliderActiveBackground"),
                Black = GetOutputTerminalThemeValue(themeValues, "black"),
                Red = GetOutputTerminalThemeValue(themeValues, "red"),
                Green = GetOutputTerminalThemeValue(themeValues, "green"),
                Yellow = GetOutputTerminalThemeValue(themeValues, "yellow"),
                Blue = GetOutputTerminalThemeValue(themeValues, "blue"),
                Magenta = GetOutputTerminalThemeValue(themeValues, "magenta"),
                Cyan = GetOutputTerminalThemeValue(themeValues, "cyan"),
                White = GetOutputTerminalThemeValue(themeValues, "white"),
                BrightBlack = GetOutputTerminalThemeValue(themeValues, "brightBlack"),
                BrightRed = GetOutputTerminalThemeValue(themeValues, "brightRed"),
                BrightGreen = GetOutputTerminalThemeValue(themeValues, "brightGreen"),
                BrightYellow = GetOutputTerminalThemeValue(themeValues, "brightYellow"),
                BrightBlue = GetOutputTerminalThemeValue(themeValues, "brightBlue"),
                BrightMagenta = GetOutputTerminalThemeValue(themeValues, "brightMagenta"),
                BrightCyan = GetOutputTerminalThemeValue(themeValues, "brightCyan"),
                BrightWhite = GetOutputTerminalThemeValue(themeValues, "brightWhite")
            };
        }

        /// <summary>
        /// Returns one browser-derived terminal theme value or an empty string when the browser has not yet supplied that value.
        /// </summary>
        /// <param name="themeValues">The browser-derived theme values, if available.</param>
        /// <param name="key">The terminal theme key that should be read.</param>
        /// <returns>The requested terminal theme value, or an empty string when no value is available yet.</returns>
        private static string GetOutputTerminalThemeValue(IReadOnlyDictionary<string, string>? themeValues, string key)
        {
            // Empty-string fallbacks let the initial terminal mount succeed before the browser reports the active shell palette.
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            if (themeValues is null)
            {
                return string.Empty;
            }

            return themeValues.TryGetValue(key, out var value)
                ? value
                : string.Empty;
        }

        /// <summary>
        /// Invalidates the current terminal instance so the next render rebuilds retained history into a fresh terminal surface.
        /// </summary>
        private void InvalidateOutputTerminalProjection()
        {
            // Recreating the terminal component remains the clean rebuild path when the DOM host is remounted by panel open or close transitions.
            ResetOutputTerminalProjectionState();
            _outputTerminalRenderKey++;
        }

        /// <summary>
        /// Resets the layout-owned terminal projection state without touching the shared retained output stream.
        /// </summary>
        private void ResetOutputTerminalProjectionState()
        {
            // Resetting the projection state keeps rebuild responsibility in the layout while preserving the shared output service as the only source of truth.
            _outputTerminal = null;
            _isOutputSelectionAvailable = false;
            _isOutputTerminalReady = false;
            _outputFindInputShouldReceiveFocus = false;
            _pendingOutputTerminalEntries.Clear();
            _projectedOutputEntryIds = [];
            _outputTerminalNeedsRebuild = true;
            _outputTerminalNeedsPresentationRefresh = true;
        }

        /// <summary>
        /// Synchronizes the hosted terminal with the retained output stream, current shell theme, and panel fit requirements.
        /// </summary>
        /// <returns>A task that completes when any pending rebuild, append, theme, fit, and deferred scroll work has finished.</returns>
        private async Task SynchronizeOutputTerminalAsync()
        {
            // Synchronization runs after render so the layout can choose the lightest safe path: append when history only grows at the tail, otherwise rebuild from retained state.
            if (_outputTerminal is null || !_isOutputTerminalReady)
            {
                return;
            }

            if (_outputTerminalNeedsRebuild)
            {
                await RebuildOutputTerminalFromSharedStateAsync();
            }
            else if (_pendingOutputTerminalEntries.Count > 0)
            {
                await AppendPendingOutputEntriesToTerminalAsync();
            }

            if (_outputTerminalNeedsPresentationRefresh)
            {
                await RefreshOutputTerminalPresentationAsync();
            }

            await FitOutputTerminalAsync();

            if (_pendingScrollToEnd && OutputPanelState.IsAutoScrollEnabled && _outputPanelModule is not null)
            {
                // Deferred scrolling happens after the terminal update and fit work so the viewport moves against the final rendered buffer height.
                await _outputTerminal.ScrollToBottom();
            }

            _pendingScrollToEnd = false;
        }

        /// <summary>
        /// Rebuilds the hosted terminal buffer from the retained shared output state.
        /// </summary>
        /// <returns>A task that completes when the full retained history has been written to the terminal.</returns>
        private async Task RebuildOutputTerminalFromSharedStateAsync()
        {
            // Rebuilds are used when the terminal is first created or when retained history changed outside the simple append-only path.
            if (_outputTerminal is null)
            {
                return;
            }

            var currentEntries = OutputEntries;
            await _outputTerminal.Reset();

            foreach (var outputEntry in currentEntries)
            {
                // Each retained entry is projected into styled terminal lines so the shell preserves chronological text while surfacing severity visually.
                foreach (var terminalLine in BuildOutputTerminalRenderableLines(outputEntry))
                {
                    await _outputTerminal.WriteLine(terminalLine);
                }
            }

            _projectedOutputEntryIds = currentEntries
                .Select(outputEntry => outputEntry.Id)
                .ToArray();
            _pendingOutputTerminalEntries.Clear();
            _outputTerminalNeedsRebuild = false;
        }

        /// <summary>
        /// Appends the queued tail entries to the hosted terminal without replaying the full retained stream.
        /// </summary>
        /// <returns>A task that completes when every queued append entry has been written.</returns>
        private async Task AppendPendingOutputEntriesToTerminalAsync()
        {
            // Append mode avoids terminal resets during normal live output flow while still deferring to rebuilds when ordering assumptions no longer hold.
            if (_outputTerminal is null || _pendingOutputTerminalEntries.Count == 0)
            {
                return;
            }

            foreach (var outputEntry in _pendingOutputTerminalEntries)
            {
                // Appended entries use the same styled line projection as retained-history rebuilds so both code paths stay visually identical.
                foreach (var terminalLine in BuildOutputTerminalRenderableLines(outputEntry))
                {
                    await _outputTerminal.WriteLine(terminalLine);
                }
            }

            _projectedOutputEntryIds = _projectedOutputEntryIds
                .Concat(_pendingOutputTerminalEntries.Select(outputEntry => outputEntry.Id))
                .ToArray();
            _pendingOutputTerminalEntries.Clear();
        }

        /// <summary>
        /// Refreshes the terminal options that depend on browser-derived shell appearance values.
        /// </summary>
        /// <returns>A task that completes when the current terminal options have been refreshed.</returns>
        private async Task RefreshOutputTerminalPresentationAsync()
        {
            // Theme refresh stays browser-driven because the Radzen appearance toggle ultimately changes CSS tokens that only the browser can read accurately.
            if (_outputTerminal is null || _outputPanelModule is null)
            {
                return;
            }

            var themeValues = await _outputPanelModule.InvokeAsync<Dictionary<string, string>>("readOutputTerminalTheme", _outputStreamElement);
            await _outputTerminal.SetOptions(CreateOutputTerminalOptions(themeValues));
            _outputTerminalNeedsPresentationRefresh = false;
        }

        /// <summary>
        /// Fits the hosted terminal to the current output-panel dimensions by using the official xterm.js fit addon.
        /// </summary>
        /// <returns>A task that completes when the fit request has been issued.</returns>
        private async Task FitOutputTerminalAsync()
        {
            // The fit addon keeps the terminal sizing logic minimal in .NET while letting xterm.js calculate rows and columns from the current host dimensions.
            if (_outputTerminal is null)
            {
                return;
            }

            await _outputTerminal
                .Addon(OutputTerminalFitAddonId)
                .InvokeVoidAsync("fit");
        }

        /// <summary>
        /// Returns the entry identifiers already projected into the terminal, including any queued append entries that have not yet been flushed to the browser.
        /// </summary>
        /// <returns>The effective projected output-entry identifiers.</returns>
        private IReadOnlyList<string> GetEffectiveProjectedOutputEntryIds()
        {
            // Queued append identifiers are included so repeated output changes before the next render do not cause duplicate appends.
            if (_pendingOutputTerminalEntries.Count == 0)
            {
                return _projectedOutputEntryIds;
            }

            return _projectedOutputEntryIds
                .Concat(_pendingOutputTerminalEntries.Select(outputEntry => outputEntry.Id))
                .ToArray();
        }

        /// <summary>
        /// Queues either append-only terminal writes or a retained-history rebuild according to how the shared output stream changed.
        /// </summary>
        /// <param name="currentEntries">The current retained output entries owned by the shared output service.</param>
        private void QueueOutputTerminalProjectionUpdate(IReadOnlyList<OutputEntry> currentEntries)
        {
            // Hidden or not-yet-ready terminal paths always fall back to rebuild so reopening the panel projects the latest retained history without trusting stale terminal state.
            ArgumentNullException.ThrowIfNull(currentEntries);

            if (!OutputPanelState.IsVisible || !_isOutputTerminalReady || _outputTerminal is null)
            {
                _pendingOutputTerminalEntries.Clear();
                _outputTerminalNeedsRebuild = true;
                return;
            }

            if (_outputTerminalNeedsRebuild)
            {
                return;
            }

            var appendedEntries = BuildOutputTerminalAppendEntries(currentEntries, GetEffectiveProjectedOutputEntryIds());
            if (appendedEntries is null)
            {
                _pendingOutputTerminalEntries.Clear();
                _outputTerminalNeedsRebuild = true;
                return;
            }

            if (appendedEntries.Count == 0)
            {
                return;
            }

            _pendingOutputTerminalEntries.AddRange(appendedEntries);
        }

        /// <summary>
        /// Ensures the output-panel JavaScript module is loaded and attached to the current output viewport.
        /// </summary>
        /// <returns>A task that completes when the browser helper has been initialized.</returns>
        private async Task EnsureOutputPanelInteropAsync()
        {
            // The module is loaded lazily so collapsed panels do not pay any browser-side setup cost, then reinitialized so terminal remounts keep scroll tracking attached.
            if (_outputPanelModule is null)
            {
                _outputPanelModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", OutputPanelModulePath);
            }

            _dotNetObjectReference ??= DotNetObjectReference.Create(this);
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
        /// Requests a layout refresh when the component is interactive and silently skips the refresh when tests exercise the layout without an assigned render handle.
        /// </summary>
        /// <returns>A task that completes when the refresh request has been processed or intentionally skipped.</returns>
        private Task RequestLayoutRefreshAsync()
        {
            // Direct interaction tests instantiate the layout without a renderer, so refresh requests must tolerate the missing render handle while real interactive renders still update normally.
            try
            {
                return InvokeAsync(StateHasChanged);
            }
            catch (InvalidOperationException)
            {
                return Task.CompletedTask;
            }
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

            QueueOutputTerminalProjectionUpdate(OutputEntries);

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
            if (!OutputPanelState.IsVisible)
            {
                // Closing the panel removes the terminal from the render tree, so the cached component reference is cleared immediately.
                _isOutputFindSurfaceVisible = false;
                ResetOutputTerminalProjectionState();
            }
            else
            {
                // Visible panel-state changes can affect fit and theme application, so the next render refreshes terminal presentation from browser-derived values.
                _outputTerminalNeedsPresentationRefresh = true;
            }

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
