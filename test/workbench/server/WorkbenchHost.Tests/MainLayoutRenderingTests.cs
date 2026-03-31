using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Radzen;
using System.Reflection;
using Shouldly;
using UKHO.Workbench.Layout;
using UKHO.Workbench.Commands;
using UKHO.Workbench.Explorers;
using UKHO.Workbench.Output;
using UKHO.Workbench.Services;
using UKHO.Workbench.Services.Shell;
using UKHO.Workbench.Tools;
using UKHO.Workbench.WorkbenchShell;
using WorkbenchHost.Components.Layout;
using WorkbenchHost.Components.Tools;
using WorkbenchHost.Services;
using Xunit;

namespace WorkbenchHost.Tests
{
    /// <summary>
    /// Verifies the Workbench host layout introduced for the first tabbed Workbench slice.
    /// </summary>
    public class MainLayoutRenderingTests
    {
        private const string FallbackExplorerId = "explorer.host.overview";
        private const string FallbackExplorerDisplayName = "Workbench";
        private const string HostToolsSectionId = "explorer.section.host.tools";
        private const string OverviewToolId = "tool.bootstrap.overview";
        private const string OverviewCommandId = "command.host.open-overview";
        private const string OverviewMenuId = "menu.host.overview";
        private const string OverviewToolbarId = "toolbar.host.overview";
        /// <summary>
        /// Confirms the layout renders the full shell structure, including the visible tab strip above the center surface.
        /// </summary>
        [Fact]
        public async Task RenderTheBootstrapWorkbenchShell()
        {
            // The renderer uses the same shell manager and Radzen registrations as the host so the markup reflects the real shell composition.
            await using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            var outputService = serviceProvider.GetRequiredService<IWorkbenchOutputService>();
            SeedHostShell(shellManager);
            shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget(OverviewToolId));
            outputService.Write(OutputLevel.Info, "Shell", "Workbench shell bootstrap completed.");

            var renderer = new HtmlRenderer(serviceProvider, serviceProvider.GetRequiredService<ILoggerFactory>());
            var html = await RenderLayoutAsync(renderer);

            html.ShouldContain("data-region=\"menu-bar\"");
            html.ShouldContain("data-region=\"activity-rail\"");
            html.ShouldContain("data-region=\"explorer\"");
            html.ShouldContain("data-region=\"tool-surface\"");
            html.ShouldContain("data-region=\"tab-strip\"");
            html.ShouldContain("data-region=\"status-bar\"");
            html.ShouldContain("data-role=\"output-toggle\"");
            html.ShouldContain("Workbench overview");
            html.ShouldContain("Home");
            html.ShouldContain("aria-label=\"Workbench\"");
            html.ShouldNotContain("Active tab");
            html.ShouldContain("BodyMarker");
        }

        /// <summary>
        /// Confirms the output toggle renders at the far-left side of the status bar while the panel itself stays hidden by default.
        /// </summary>
        [Fact]
        public async Task RenderTheOutputToggleOnTheFarLeftAndKeepThePanelHiddenByDefault()
        {
            // Startup entries may already exist, but the first slice still requires the panel to stay collapsed until the user opens it.
            await using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            var outputService = serviceProvider.GetRequiredService<IWorkbenchOutputService>();
            SeedHostShell(shellManager);
            shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget(OverviewToolId));
            outputService.Write(OutputLevel.Info, "Shell", "Workbench shell bootstrap completed.");

            var renderer = new HtmlRenderer(serviceProvider, serviceProvider.GetRequiredService<ILoggerFactory>());
            var html = await RenderLayoutAsync(renderer);
            var outputToggleIndex = html.IndexOf("data-role=\"output-toggle\"", StringComparison.Ordinal);

            html.ShouldContain("data-output-toggle-state=\"collapsed\"");
            html.ShouldContain("aria-expanded=\"false\"");
            html.ShouldNotContain("data-region=\"output-panel\"");
            html.ShouldNotContain("Workbench shell ready");
            html.ShouldNotContain("workbench.activeTool:");
            outputToggleIndex.ShouldBeGreaterThanOrEqualTo(0);
        }

        /// <summary>
        /// Confirms opening the output panel applies the expected layout ordering above the status bar and below the centre working area.
        /// </summary>
        [Fact]
        public async Task PlaceTheOpenedOutputPanelAboveTheStatusBarAndBelowTheWorkingArea()
        {
            // The first-open layout must keep the output pane between the centre surface and the status bar while using the planned 1:4 split.
            using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            SeedHostShell(shellManager);
            var layout = CreateLayoutInstance(serviceProvider, shellManager);

            await InvokePrivateMethodAsync(layout, "ToggleOutputPanelAsync");

            var outputPanelState = GetPrivatePropertyValue<OutputPanelState>(layout, "OutputPanelState");
            var workingAreaRow = GetPrivatePropertyValue<int>(layout, "WorkingAreaRow");
            var outputPanelRow = GetPrivatePropertyValue<int>(layout, "OutputPanelRow");
            var statusBarRow = GetPrivatePropertyValue<int>(layout, "StatusBarRow");
            var workingAreaHeight = GetPrivatePropertyValue<string>(layout, "WorkingAreaHeight");
            var outputPaneHeight = GetPrivatePropertyValue<string>(layout, "OutputPaneHeight");

            outputPanelState.IsVisible.ShouldBeTrue();
            workingAreaHeight.ShouldBe("4*");
            outputPaneHeight.ShouldBe("1*");
            outputPanelRow.ShouldBeGreaterThan(workingAreaRow);
            statusBarRow.ShouldBeGreaterThan(outputPanelRow);
        }

        /// <summary>
        /// Confirms the opened panel renders the compact toolbar actions required for the session-state slice.
        /// </summary>
        [Fact]
        public async Task RenderTheOutputToolbarWhenThePanelIsVisible()
        {
            // The second slice turns the panel into a usable output surface, so the toolbar actions should render only when the panel is open.
            await using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            var outputService = serviceProvider.GetRequiredService<IWorkbenchOutputService>();
            SeedHostShell(shellManager);
            shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget(OverviewToolId));
            outputService.Write(OutputLevel.Info, "Shell", "Workbench shell bootstrap completed.");
            outputService.SetPanelVisibility(true);

            var renderer = new HtmlRenderer(serviceProvider, serviceProvider.GetRequiredService<ILoggerFactory>());
            var html = await RenderLayoutAsync(renderer);

            html.ShouldContain("data-region=\"output-panel\"");
            html.ShouldContain("data-output-surface=\"editor-like\"");
            html.ShouldContain("data-output-wrap-mode=\"nowrap\"");
            html.ShouldContain("data-output-selection-mode=\"text-first\"");
            html.ShouldContain("data-output-density=\"dense\"");
            html.ShouldContain("data-output-theme-scope=\"shell-tokens\"");
            html.ShouldContain("workbench-shell__output-stream--editor-like");
            html.ShouldContain("data-role=\"output-clear\"");
            html.ShouldContain("data-role=\"output-auto-scroll\"");
            html.ShouldContain("data-role=\"output-scroll-to-end\"");
            html.ShouldContain("data-role=\"output-wrap\"");
        }

        /// <summary>
        /// Confirms hidden unseen severity appears on the collapsed toggle without auto-opening the panel.
        /// </summary>
        [Fact]
        public async Task ShowTheMostSevereHiddenIndicatorWithoutAutoOpeningThePanel()
        {
            // Unseen warnings and errors should surface on the collapsed toggle so the panel stays hidden until the user chooses to inspect it.
            await using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            var outputService = serviceProvider.GetRequiredService<IWorkbenchOutputService>();
            SeedHostShell(shellManager);
            shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget(OverviewToolId));
            outputService.Write(OutputLevel.Warning, "Shell", "A warning was written while the panel was collapsed.");

            var renderer = new HtmlRenderer(serviceProvider, serviceProvider.GetRequiredService<ILoggerFactory>());
            var html = await RenderLayoutAsync(renderer);

            html.ShouldContain("data-output-toggle-state=\"collapsed\"");
            html.ShouldContain("data-output-unseen-level=\"warning\"");
            html.ShouldNotContain("data-region=\"output-panel\"");
        }

        /// <summary>
        /// Confirms collapsed structured rows keep the compact line format while exposing the subtle gutter treatment for foldable entries.
        /// </summary>
        [Fact]
        public async Task RenderCompactCollapsedOutputRowsWithTheFoldableGutterButWithoutExpandedDetailsOrEventCodes()
        {
            // Foldable rows should keep the same visible text contract while moving expansion affordance and severity cues into the editor-like gutter.
            await using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            var outputService = serviceProvider.GetRequiredService<IWorkbenchOutputService>();
            SeedHostShell(shellManager);
            outputService.Clear();
            shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget(OverviewToolId));
            outputService.SetPanelVisibility(true);

            var outputEntry = CreateOutputEntry(
                "entry-collapsed",
                OutputLevel.Warning,
                "Module loader",
                "Search module loaded with warnings.",
                "The module emitted additional diagnostic detail.",
                "WB-103");
            outputService.Write(outputEntry);

            var renderer = new HtmlRenderer(serviceProvider, serviceProvider.GetRequiredService<ILoggerFactory>());
            var html = await RenderLayoutAsync(renderer);

            html.ShouldContain("data-role=\"output-entry-toggle\"");
            html.ShouldContain($"data-output-entry-id=\"{outputEntry.Id}\"");
            html.ShouldContain("data-output-foldable=\"true\"");
            html.ShouldContain($"data-output-level=\"{outputEntry.Level.ToString().ToLowerInvariant()}\"");
            html.ShouldContain("data-role=\"output-gutter-marker\"");
            html.ShouldContain(outputEntry.TimestampUtc.ToLocalTime().ToString("HH:mm:ss"));
            html.ShouldContain(outputEntry.Source);
            html.ShouldContain(outputEntry.Summary);
            html.ShouldNotContain(outputEntry.Details!);
            html.ShouldNotContain($"Event code: {outputEntry.EventCode}");
            html.ShouldNotContain("data-role=\"output-copy-entry\"");
        }

        /// <summary>
        /// Confirms multiple rows can remain expanded simultaneously and render inline details beneath their main lines.
        /// </summary>
        [Fact]
        public async Task KeepMultipleOutputRowsExpandedAtTheSameTimeAndRenderTheirInlineDetails()
        {
            // The disclosure control owns expansion, and the shell should allow several diagnostics to stay open together without reintroducing row-action chrome.
            await using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            var outputService = serviceProvider.GetRequiredService<IWorkbenchOutputService>();
            SeedHostShell(shellManager);
            outputService.Clear();
            outputService.SetPanelVisibility(true);

            var firstEntry = CreateOutputEntry(
                "entry-expanded-1",
                OutputLevel.Info,
                "Shell",
                "Workbench overview activated.",
                "Overview tab restored its previous state.",
                "WB-201");
            var secondEntry = CreateOutputEntry(
                "entry-expanded-2",
                OutputLevel.Error,
                "Module loader",
                "Administration module failed to initialise.",
                "The module assembly could not load a dependent service.",
                "WB-202");
            outputService.Write(firstEntry);
            outputService.Write(secondEntry);

            var layout = CreateLayoutInstance(serviceProvider, shellManager);
            await InvokePrivateMethodAsync(layout, "ToggleOutputEntryExpansionAsync", firstEntry);
            await InvokePrivateMethodAsync(layout, "ToggleOutputEntryExpansionAsync", secondEntry);

            var renderer = new HtmlRenderer(serviceProvider, serviceProvider.GetRequiredService<ILoggerFactory>());
            var html = await RenderLayoutAsync(renderer);

            outputService.PanelState.ExpandedEntryIds.ShouldBe([firstEntry.Id, secondEntry.Id]);
            html.ShouldContain($"data-output-entry-id=\"{firstEntry.Id}\"");
            html.ShouldContain($"data-output-entry-id=\"{secondEntry.Id}\"");
            html.ShouldContain("data-output-expanded=\"true\"");
            html.ShouldContain("data-role=\"output-entry-details\"");
            html.ShouldContain("data-role=\"output-detail-line\"");
            html.ShouldContain(firstEntry.Details!);
            html.ShouldContain(secondEntry.Details!);
            html.ShouldContain($"Event code: {firstEntry.EventCode}");
            html.ShouldContain($"Event code: {secondEntry.EventCode}");
            html.ShouldNotContain("data-role=\"output-copy-entry\"");
        }

        /// <summary>
        /// Confirms expanded details follow the shared wrap toggle and expose the output scroll mode required for long diagnostic content.
        /// </summary>
        [Fact]
        public async Task ApplyTheSharedWrapModeToExpandedOutputDetailsAndScrollState()
        {
            // Wrap is a global output-surface setting, so both the summary row and inline details should switch modes together.
            await using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            var outputService = serviceProvider.GetRequiredService<IWorkbenchOutputService>();
            SeedHostShell(shellManager);
            outputService.Clear();
            outputService.SetPanelVisibility(true);

            var outputEntry = CreateOutputEntry(
                "entry-wrap",
                OutputLevel.Debug,
                "Shell",
                "Long diagnostic summary for wrap verification.",
                "First detail line.\nSecond detail line with additional diagnostic content.",
                "WB-301");
            outputService.Write(outputEntry);
            outputService.SetExpandedEntryIds([outputEntry.Id]);

            var renderer = new HtmlRenderer(serviceProvider, serviceProvider.GetRequiredService<ILoggerFactory>());
            var nowrapHtml = await RenderLayoutAsync(renderer);

            nowrapHtml.ShouldContain("data-output-scroll-mode=\"horizontal\"");
            nowrapHtml.ShouldContain("data-output-wrap-mode=\"nowrap\"");
            nowrapHtml.ShouldContain("data-output-selection-mode=\"text-first\"");
            nowrapHtml.ShouldContain("data-output-density=\"dense\"");
            nowrapHtml.ShouldContain("data-output-theme-scope=\"shell-tokens\"");
            nowrapHtml.ShouldNotContain("workbench-shell__output-stream--wrapped");

            var layout = CreateLayoutInstance(serviceProvider, shellManager);
            await InvokePrivateMethodAsync(layout, "ToggleWordWrapAsync");

            var wrappedHtml = await RenderLayoutAsync(renderer);

            wrappedHtml.ShouldContain("data-output-scroll-mode=\"wrapped\"");
            wrappedHtml.ShouldContain("data-output-wrap-mode=\"wrapped\"");
            wrappedHtml.ShouldContain("data-output-selection-mode=\"text-first\"");
            wrappedHtml.ShouldContain("data-output-density=\"dense\"");
            wrappedHtml.ShouldContain("data-output-theme-scope=\"shell-tokens\"");
            wrappedHtml.ShouldContain("workbench-shell__output-stream--wrapped");
            wrappedHtml.ShouldContain("First detail line.");
            wrappedHtml.ShouldContain("Second detail line with additional diagnostic content.");
        }

        /// <summary>
        /// Confirms only entries with expandable content render the disclosure affordance while every row keeps the subtle severity gutter marker.
        /// </summary>
        [Fact]
        public async Task RenderTheFoldAffordanceOnlyForEntriesThatExposeInlineDetails()
        {
            // Non-foldable rows should keep gutter alignment without surfacing a redundant toggle, while foldable rows still expose the disclosure control.
            await using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            var outputService = serviceProvider.GetRequiredService<IWorkbenchOutputService>();
            SeedHostShell(shellManager);
            outputService.SetPanelVisibility(true);

            var foldableEntry = CreateOutputEntry(
                "entry-foldable",
                OutputLevel.Info,
                "Shell",
                "Foldable output row.",
                "Inline detail text.",
                "WB-320");
            var flatEntry = CreateOutputEntry(
                "entry-flat",
                OutputLevel.Debug,
                "Shell",
                "Flat output row without extra detail.");
            outputService.Write(foldableEntry);
            outputService.Write(flatEntry);

            var renderer = new HtmlRenderer(serviceProvider, serviceProvider.GetRequiredService<ILoggerFactory>());
            var html = await RenderLayoutAsync(renderer);

            html.ShouldContain($"data-output-entry-id=\"{foldableEntry.Id}\"");
            html.ShouldContain($"data-output-entry-id=\"{flatEntry.Id}\"");
            html.ShouldContain("data-output-foldable=\"true\"");
            html.ShouldContain("data-output-foldable=\"false\"");
            html.ShouldContain("data-role=\"output-gutter-marker\"");
        }

        /// <summary>
        /// Confirms session resize memory and auto-scroll state transitions are coordinated through the layout handlers.
        /// </summary>
        [Fact]
        public async Task RestoreTheResizedPanelHeightAndToggleAutoScrollThroughTheLayoutHandlers()
        {
            // The shell should preserve the user's in-session splitter size and should disable or re-enable auto-scroll through the expected interaction paths.
            using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            var outputService = serviceProvider.GetRequiredService<IWorkbenchOutputService>();
            SeedHostShell(shellManager);
            var layout = CreateLayoutInstance(serviceProvider, shellManager);

            await InvokePrivateMethodAsync(layout, "ToggleOutputPanelAsync");
            await InvokePrivateMethodAsync(
                layout,
                "HandleOutputPanelResizeAsync",
                new GridResizeNotification(
                    GridResizeDirection.Row,
                    4,
                    3,
                    5,
                    640,
                    160,
                    "48px 44px 640px 4px 160px 30px"));
            await InvokePrivateMethodAsync(layout, "NotifyOutputViewportStateAsync", false);

            outputService.PanelState.CenterPaneHeight.ShouldBe("640px");
            outputService.PanelState.OutputPaneHeight.ShouldBe("160px");
            outputService.PanelState.IsAutoScrollEnabled.ShouldBeFalse();

            await InvokePrivateMethodAsync(layout, "ToggleOutputPanelAsync");
            await InvokePrivateMethodAsync(layout, "ToggleOutputPanelAsync");

            outputService.PanelState.OutputPaneHeight.ShouldBe("160px");

            await InvokePrivateMethodAsync(layout, "ScrollOutputToEndAsync");

            outputService.PanelState.IsAutoScrollEnabled.ShouldBeTrue();
        }

        /// <summary>
        /// Confirms the activity rail uses the Radzen tooltip service so icon-only rail items still expose their labels.
        /// </summary>
        [Fact]
        public void UseTheRadzenTooltipServiceForActivityRailHoverInteractions()
        {
            // The compact icon rail still needs discoverable names, so hover should open the same Radzen tooltip service used elsewhere in the shell.
            using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            var tooltipService = serviceProvider.GetRequiredService<TooltipService>();
            var layout = CreateLayoutInstance(serviceProvider, shellManager);
            var openedTitles = new List<string>();
            var closeCount = 0;

            tooltipService.OnOpen += (_, _, options) =>
            {
                if (!string.IsNullOrWhiteSpace(options.Text))
                {
                    openedTitles.Add(options.Text);
                }
            };
            tooltipService.OnClose += () => closeCount++;

            InvokePrivateMethod(layout, "ShowActivityRailTooltip", [default(ElementReference), "Workbench"]);
            InvokePrivateMethod(layout, "HideTitleTooltip", [default(ElementReference)]);

            openedTitles.ShouldBe(["Workbench"]);
            closeCount.ShouldBe(1);
        }

        /// <summary>
        /// Confirms runtime status-bar messages are projected into the output stream instead of rendering as persistent status-bar text.
        /// </summary>
        [Fact]
        public async Task ProjectHistoricalStatusMessagesIntoTheOutputStreamAndKeepTheStatusBarSimplified()
        {
            // Historical status messages should remain visible in the output stream across navigation, while the status bar itself stays focused on shell affordances.
            await using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            var outputService = serviceProvider.GetRequiredService<IWorkbenchOutputService>();
            SeedHostShell(shellManager);
            SeedSearchShell(shellManager);
            outputService.SetPanelVisibility(true);

            var searchTool = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.module.search.query"));
            searchTool.Context.SetRuntimeMenuContributions([new MenuContribution("menu.runtime.search", "Run sample query", "command.search.run", ownerToolId: "tool.module.search.query", order: 200)]);
            searchTool.Context.SetRuntimeStatusBarContributions([new StatusBarContribution("status.runtime.search", "Sample query executed", ownerToolId: "tool.module.search.query", order: 200)]);

            var renderer = new HtmlRenderer(serviceProvider, serviceProvider.GetRequiredService<ILoggerFactory>());
            var activeSearchHtml = await RenderLayoutAsync(renderer);

            activeSearchHtml.ShouldContain("Run sample query");
            activeSearchHtml.ShouldContain("Sample query executed");
            activeSearchHtml.ShouldNotContain("workbench-shell__status-bar-items");

            shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget(OverviewToolId));
            var activeOverviewHtml = await RenderLayoutAsync(renderer);

            activeOverviewHtml.ShouldNotContain("Run sample query");
            activeOverviewHtml.ShouldContain("Sample query executed");
            activeOverviewHtml.ShouldNotContain("Workbench shell ready");
            activeOverviewHtml.ShouldNotContain("workbench.activeTool:");
        }

        /// <summary>
        /// Confirms startup notifications are mirrored into the output stream when the interactive shell replays them.
        /// </summary>
        [Fact]
        public async Task MirrorStartupNotificationsIntoTheOutputStreamWhenTheShellBecomesInteractive()
        {
            // Startup notifications are still shown as toasts, but the output-first shell should also capture them in the shared output stream.
            await using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            var outputService = serviceProvider.GetRequiredService<IWorkbenchOutputService>();
            var startupNotificationStore = serviceProvider.GetRequiredService<WorkbenchStartupNotificationStore>();
            SeedHostShell(shellManager);
            startupNotificationStore.Add(NotificationSeverity.Warning, "Startup warning", "The Workbench replayed a startup warning.");
            var layout = CreateLayoutInstance(serviceProvider, shellManager);

            await InvokePrivateMethodAsync(layout, "OnAfterRenderAsync", true);

            outputService.Entries.ShouldContain(entry => entry.Source == "Notifications"
                && entry.Summary == "Startup warning"
                && entry.Level == OutputLevel.Warning);
        }

        /// <summary>
        /// Confirms buffered startup diagnostics from module discovery are replayed into the shared output stream during shell bootstrap.
        /// </summary>
        [Fact]
        public void ReplayBufferedStartupDiagnosticsIntoTheOutputStreamDuringBootstrap()
        {
            // Module discovery runs before DI finalization, so the host must buffer output and replay it once the singleton output service exists.
            var outputService = new UKHO.Workbench.Services.Output.WorkbenchOutputService();
            var startupNotificationStore = new WorkbenchStartupNotificationStore();
            startupNotificationStore.AddOutput(
                OutputLevel.Debug,
                "Module loader",
                "Loaded Workbench module 'UKHO.Workbench.Modules.Search'.",
                "Assembly path: c:/modules/Search.dll");

            InvokeStaticPrivateMethod(
                typeof(WorkbenchHost.Program),
                "ReplayBufferedStartupOutput",
                [outputService, startupNotificationStore]);

            outputService.Entries.ShouldContain(entry => entry.Source == "Module loader"
                && entry.Summary == "Loaded Workbench module 'UKHO.Workbench.Modules.Search'."
                && entry.Level == OutputLevel.Debug);
            startupNotificationStore.DequeueOutputEntries().ShouldBeEmpty();
        }

        /// <summary>
        /// Confirms single-click explorer interaction records selection without opening a tab.
        /// </summary>
        [Fact]
        public async Task SelectTheExplorerItemOnSingleClickWithoutOpeningATab()
        {
            // Explorer single-click should update explorer selection only so the center surface remains unchanged until a double-click activation occurs.
            await using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            SeedHostShell(shellManager);
            var explorerItem = shellManager.GetExplorerItems(FallbackExplorerId, HostToolsSectionId).Single();

            shellManager.SelectExplorerItem(explorerItem.Id);

            shellManager.State.SelectedExplorerItemId.ShouldBe(explorerItem.Id);
            shellManager.OpenTabs.Count.ShouldBe(0);
            shellManager.State.ActiveTab.ShouldBeNull();
        }

        /// <summary>
        /// Confirms double-click explorer interaction opens the requested tab and renders it in the tab strip.
        /// </summary>
        [Fact]
        public async Task OpenTheExplorerItemOnDoubleClickAndRenderItInTheTabStrip()
        {
            // Explorer double-click should route through the shared command path and immediately activate the resulting tab.
            await using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            SeedHostShell(shellManager);
            var explorerItem = shellManager.GetExplorerItems(FallbackExplorerId, HostToolsSectionId).Single();

            shellManager.SelectExplorerItem(explorerItem.Id);
            await shellManager.ExecuteCommandAsync(explorerItem.CommandId);

            shellManager.OpenTabs.Count.ShouldBe(1);
            shellManager.State.ActiveTool.ShouldNotBeNull();

            var renderer = new HtmlRenderer(serviceProvider, serviceProvider.GetRequiredService<ILoggerFactory>());
            var html = await RenderLayoutAsync(renderer);
            html.ShouldContain("data-region=\"tab-strip\"");
            html.ShouldContain($"data-tab-id=\"{shellManager.State.ActiveTab!.Id}\"");
            html.ShouldContain("Workbench overview");
        }

        /// <summary>
        /// Confirms reopening the same explorer item focuses the existing tab instead of opening a duplicate tab.
        /// </summary>
        [Fact]
        public async Task FocusTheExistingTabWhenTheSameExplorerItemIsOpenedAgain()
        {
            // Duplicate explorer activation should reuse the logical target so the same tab instance remains active.
            await using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            SeedHostShell(shellManager);
            var explorerItem = shellManager.GetExplorerItems(FallbackExplorerId, HostToolsSectionId).Single();

            shellManager.SelectExplorerItem(explorerItem.Id);
            await shellManager.ExecuteCommandAsync(explorerItem.CommandId);
            var firstTabId = shellManager.State.ActiveTab!.Id;
            shellManager.SelectExplorerItem(explorerItem.Id);
            await shellManager.ExecuteCommandAsync(explorerItem.CommandId);

            shellManager.OpenTabs.Count.ShouldBe(1);
            shellManager.State.ActiveTab.ShouldNotBeNull();
            shellManager.State.ActiveTab.Id.ShouldBe(firstTabId);
        }

        /// <summary>
        /// Confirms selecting another open tab switches the active tab without reopening any content.
        /// </summary>
        [Fact]
        public async Task SwitchTheActiveTabWhenAnotherOpenTabIsSelected()
        {
            // Tab-strip selection should change the active tab while preserving the existing open-tab collection.
            await using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            SeedHostShell(shellManager);
            SeedSearchShell(shellManager);
            var overviewTool = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget(OverviewToolId));
            _ = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.module.search.query"));

            shellManager.ActivateTab(overviewTool.InstanceId);

            shellManager.State.ActiveTab.ShouldNotBeNull();
            shellManager.State.ActiveTab.Id.ShouldBe(overviewTool.InstanceId);
            shellManager.OpenTabs.Count.ShouldBe(2);
        }

        /// <summary>
        /// Confirms inactive tabs render updated title and icon metadata immediately after the hosted view publishes a runtime update.
        /// </summary>
        [Fact]
        public async Task RenderInactiveTabMetadataUpdatesImmediately()
        {
            // Inactive-tab metadata updates should still be visible in the strip so background state changes do not require the user to re-activate the tab first.
            await using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            SeedHostShell(shellManager);
            SeedSearchShell(shellManager);

            var overviewTool = shellManager.ActivateTool(
                ActivationTarget.CreateToolSurfaceTarget(
                    OverviewToolId,
                    parameterIdentity: "item=overview",
                    initialTitle: "Explorer overview",
                    initialIcon: "explore"));
            _ = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.module.search.query"));

            overviewTool.Context.SetTitle("Updated inactive overview");
            overviewTool.Context.SetIcon("travel_explore");

            var renderer = new HtmlRenderer(serviceProvider, serviceProvider.GetRequiredService<ILoggerFactory>());
            var html = await RenderLayoutAsync(renderer);

            html.ShouldContain("Updated inactive overview");
            html.ShouldContain("travel_explore");
            html.ShouldNotContain("Explorer overview");
        }

        /// <summary>
        /// Confirms the tab strip always renders the overflow dropdown and keeps the overflow list text-only with no filter UI.
        /// </summary>
        [Fact]
        public async Task RenderAnAlwaysVisibleOverflowDropdownWithoutFilterOrCloseActions()
        {
            // The first overflow slice should keep the control permanently available while deliberately omitting filtering, searching, and overflow close affordances.
            await using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            SeedHostShell(shellManager);
            SeedSearchShell(shellManager);
            shellManager.RegisterTool(new ToolDefinition("tool.module.admin.long", "Administration workbench tool with a deliberately long title", typeof(WorkbenchOverviewTool), "explorer.bootstrap", "admin_panel_settings"));

            _ = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget(OverviewToolId));
            _ = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.module.search.query"));
            var longTitleTool = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.module.admin.long"));
            longTitleTool.Context.SetTitle("Administration workbench tool with a deliberately long runtime title for tooltip coverage");

            var renderer = new HtmlRenderer(serviceProvider, serviceProvider.GetRequiredService<ILoggerFactory>());
            var html = await RenderLayoutAsync(renderer);

            html.ShouldContain("data-region=\"tab-strip-overflow\"");
            html.ShouldContain("data-overflow-tab-id");
            html.ShouldContain("data-overflow-active=\"true\"");
            html.ShouldContain("workbench-shell__tab-title");
            html.ShouldContain("workbench-shell__overflow-entry-title");
            html.ShouldNotContain("rz-dropdown-filter-container");
            html.ShouldNotContain("data-overflow-close");
        }

        /// <summary>
        /// Confirms the center tab host renders flush on the top, bottom, and left edges without the removed intermediate wrapper.
        /// </summary>
        [Fact]
        public async Task RenderAFlushCenterTabHostWithoutTheRemovedPaddingWrapper()
        {
            // The spacing refinement should be owned by the shell itself, so the tab host now renders with explicit flush metadata and without the extra tool-area wrapper.
            await using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            SeedHostShell(shellManager);

            _ = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget(OverviewToolId));

            var renderer = new HtmlRenderer(serviceProvider, serviceProvider.GetRequiredService<ILoggerFactory>());
            var html = await RenderLayoutAsync(renderer);

            html.ShouldContain("data-tab-strip-spacing=\"flush-top-bottom-left\"");
            html.ShouldNotContain("workbench-shell__tool-area");
        }

        /// <summary>
        /// Confirms the overflow affordance remains rendered after the flush spacing change and stays anchored at the right side of the tab strip.
        /// </summary>
        [Fact]
        public async Task KeepTheOverflowAffordanceAnchoredToTheRightSideOfTheTabStrip()
        {
            // The flush spacing change must not disturb overflow placement, so the rendered strip keeps the overflow host after the main tab-list content.
            await using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            SeedHostShell(shellManager);
            SeedSearchShell(shellManager);
            shellManager.RegisterTool(new ToolDefinition("tool.module.admin.long", "Administration workbench tool with a deliberately long title", typeof(WorkbenchOverviewTool), "explorer.bootstrap", "admin_panel_settings"));

            _ = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget(OverviewToolId));
            _ = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.module.search.query"));
            _ = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.module.admin.long"));

            var renderer = new HtmlRenderer(serviceProvider, serviceProvider.GetRequiredService<ILoggerFactory>());
            var html = await RenderLayoutAsync(renderer);
            var contentIndex = html.IndexOf("workbench-shell__tab-strip-content", StringComparison.Ordinal);
            var overflowIndex = html.IndexOf("data-tab-strip-overflow-anchor=\"right\"", StringComparison.Ordinal);

            html.ShouldContain("data-region=\"tab-strip-overflow\"");
            html.ShouldContain("data-tab-strip-overflow-anchor=\"right\"");
            contentIndex.ShouldBeGreaterThanOrEqualTo(0);
            overflowIndex.ShouldBeGreaterThan(contentIndex);
        }

        /// <summary>
        /// Confirms selecting an overflow entry through the layout activates the chosen tab and moves the visible strip just enough to reveal it.
        /// </summary>
        [Fact]
        public async Task ActivateAnOverflowEntryThroughTheLayoutAndRevealItWithMinimalMovement()
        {
            // Layout-driven overflow activation should call the dedicated shell path so the visible strip state remains aligned with the selected hidden tab.
            using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();

            foreach (var index in Enumerable.Range(1, 6))
            {
                shellManager.RegisterTool(new ToolDefinition($"tool.host.{index}", $"Host tool {index}", typeof(WorkbenchOverviewTool), FallbackExplorerId, $"looks_{index}"));
                shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget($"tool.host.{index}"));
            }

            var layout = CreateLayoutInstance(serviceProvider, shellManager);
            var selectedOverflowTabId = shellManager.OpenTabs[1].Id;

            await InvokePrivateMethodAsync(layout, "SelectOverflowTabAsync", selectedOverflowTabId);

            shellManager.State.ActiveTab.ShouldNotBeNull();
            shellManager.State.ActiveTab.Id.ShouldBe(selectedOverflowTabId);
            shellManager.VisibleTabs.Select(openTab => openTab.ToolInstance.Definition.Id).ShouldBe([
                "tool.host.2",
                "tool.host.3",
                "tool.host.4",
                "tool.host.5"
            ]);
        }

        /// <summary>
        /// Confirms the layout uses the Radzen tooltip service for repeated hover interactions on tab and overflow titles.
        /// </summary>
        [Fact]
        public void UseTheRadzenTooltipServiceForRepeatedTitleHoverInteractions()
        {
            // The tooltip slice should open a fresh tooltip on every hover and close it again when hover ends so truncated titles remain discoverable.
            using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            var tooltipService = serviceProvider.GetRequiredService<TooltipService>();
            var layout = CreateLayoutInstance(serviceProvider, shellManager);
            var openedTitles = new List<string>();
            var closeCount = 0;

            tooltipService.OnOpen += (_, _, options) =>
            {
                if (!string.IsNullOrWhiteSpace(options.Text))
                {
                    openedTitles.Add(options.Text);
                }
            };
            tooltipService.OnClose += () => closeCount++;

            InvokePrivateMethod(layout, "ShowTitleTooltip", [default(ElementReference), "Long tab title for tooltip coverage"]);
            InvokePrivateMethod(layout, "HideTitleTooltip", [default(ElementReference)]);
            InvokePrivateMethod(layout, "ShowTitleTooltip", [default(ElementReference), "Long overflow title for tooltip coverage"]);

            openedTitles.ShouldBe([
                "Long tab title for tooltip coverage",
                "Long overflow title for tooltip coverage"
            ]);
            closeCount.ShouldBe(1);
        }

        /// <summary>
        /// Confirms the tab context menu offers only the first-implementation close action.
        /// </summary>
        [Fact]
        public void BuildATabContextMenuWithCloseAsTheOnlyAction()
        {
            // The first implementation intentionally keeps the tab context menu minimal so all close behavior continues to flow through the shared shell close path.
            using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            SeedHostShell(shellManager);

            _ = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget(OverviewToolId));
            var openTab = shellManager.OpenTabs.Single();

            var contextMenuItems = InvokeStaticPrivateMethod(
                typeof(MainLayout),
                "CreateTabContextMenuItems",
                [openTab]) as IReadOnlyList<ContextMenuItem>;

            contextMenuItems.ShouldNotBeNull();

            contextMenuItems.Count.ShouldBe(1);
            contextMenuItems[0].Text.ShouldBe("Close");
            contextMenuItems[0].Icon.ShouldBe("close");
            contextMenuItems[0].Value.ShouldBe(openTab.Id);
        }

        /// <summary>
        /// Confirms the tab context-menu close handler routes through the same shell close behavior used by the visible strip close button.
        /// </summary>
        [Fact]
        public void CloseTabsThroughTheContextMenuHandlerWithTheSameSharedShellBehavior()
        {
            // Context-menu close should not introduce a second close implementation because the specification requires the same lifecycle and active-tab outcomes.
            using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            SeedHostShell(shellManager);
            SeedSearchShell(shellManager);
            shellManager.RegisterTool(new ToolDefinition("tool.admin", "Admin", typeof(WorkbenchOverviewTool), "explorer.bootstrap", "build"));

            var overviewTool = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget(OverviewToolId));
            var searchTool = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.module.search.query"));
            var adminTool = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.admin"));
            shellManager.ActivateTab(overviewTool.InstanceId);
            var layout = CreateLayoutInstance(serviceProvider, shellManager);

            InvokePrivateMethod(layout, "HandleTabContextMenuSelection", [overviewTool.InstanceId]);

            shellManager.State.ActiveTool.ShouldBe(adminTool);
            shellManager.OpenTabs.Select(openTab => openTab.Id).ShouldBe([
                searchTool.InstanceId,
                adminTool.InstanceId
            ]);
        }

        /// <summary>
        /// Confirms explorer middle-click remains a no-op for both selection and opening interactions.
        /// </summary>
        [Fact]
        public async Task IgnoreMiddleClickForExplorerInteractions()
        {
            // The first implementation explicitly defers middle-click behavior, so explorer interaction handlers should return without mutating shell state.
            using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            SeedHostShell(shellManager);
            var explorerItem = shellManager.GetExplorerItems(FallbackExplorerId, HostToolsSectionId).Single();
            var layout = CreateLayoutInstance(serviceProvider, shellManager);
            var middleClick = new MouseEventArgs { Button = 1 };

            await InvokePrivateMethodAsync(layout, "SelectExplorerItemAsync", explorerItem, middleClick);
            await InvokePrivateMethodAsync(layout, "OpenExplorerItemAsync", explorerItem, middleClick);

            shellManager.State.SelectedExplorerItemId.ShouldBeNull();
            shellManager.OpenTabs.Count.ShouldBe(0);
        }

        /// <summary>
        /// Confirms tab middle-click remains a no-op in the first implementation.
        /// </summary>
        [Fact]
        public async Task IgnoreMiddleClickForOpenTabs()
        {
            // Tab middle-click is intentionally out of scope, so the no-op auxiliary-click handler should leave the shell state unchanged.
            using var serviceProvider = CreateServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            SeedHostShell(shellManager);

            var overviewTool = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget(OverviewToolId));
            var originalActiveTabId = shellManager.State.ActiveTab?.Id;

            await InvokeStaticPrivateMethodAsync(typeof(MainLayout), "IgnoreAuxiliaryClickAsync", new MouseEventArgs { Button = 1 });

            shellManager.OpenTabs.Count.ShouldBe(1);
            shellManager.State.ActiveTab.ShouldNotBeNull();
            shellManager.State.ActiveTab.Id.ShouldBe(originalActiveTabId);
            shellManager.State.ActiveTool.ShouldBe(overviewTool);
        }

        /// <summary>
        /// Creates the service provider used by the Workbench host rendering and interaction tests.
        /// </summary>
        /// <returns>A fully configured service provider for the Workbench host shell.</returns>
        private static ServiceProvider CreateServiceProvider()
        {
            // The test provider mirrors the host registrations so rendering and interaction behavior matches the real Workbench shell composition.
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddRadzenComponents();
            services.AddWorkbenchServices();
            services.AddSingleton<WorkbenchStartupNotificationStore>();
            services.AddSingleton<IJSRuntime, TestJsRuntime>();
            services.AddSingleton<NavigationManager, TestNavigationManager>();
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Registers the host-owned shell contributions used by the runtime shell bootstrap.
        /// </summary>
        /// <param name="shellManager">The shell manager that should receive the host-owned contributions.</param>
        private static void SeedHostShell(WorkbenchShellManager shellManager)
        {
            // The helper mirrors the host bootstrap path so rendering tests stay aligned with production shell composition.
            shellManager.RegisterTool(
                new ToolDefinition(
                    OverviewToolId,
                    "Workbench overview",
                    typeof(WorkbenchOverviewTool),
                    FallbackExplorerId,
                    "dashboard",
                    "Shows the first host-owned tool."));
            shellManager.RegisterExplorer(new ExplorerContribution(FallbackExplorerId, FallbackExplorerDisplayName, "dashboard_customize", 0));
            shellManager.RegisterExplorerSection(new ExplorerSectionContribution(HostToolsSectionId, FallbackExplorerId, "Host tools", 100));
            var overviewActivationTarget = ActivationTarget.CreateToolSurfaceTarget(
                OverviewToolId,
                initialTitle: "Workbench overview",
                initialIcon: "dashboard");
            shellManager.RegisterCommand(
                new CommandContribution(
                    OverviewCommandId,
                    "Open Workbench overview",
                    CommandScope.Host,
                    activationTarget: overviewActivationTarget));
            shellManager.RegisterExplorerItem(
                new ExplorerItem(
                    "explorer.item.host.overview",
                    FallbackExplorerId,
                    HostToolsSectionId,
                    "Workbench overview",
                    OverviewCommandId,
                    overviewActivationTarget,
                    "dashboard",
                    "Shows the first host-owned tool.",
                    100));
            shellManager.RegisterMenu(new MenuContribution(OverviewMenuId, "Home", OverviewCommandId, icon: "dashboard", order: 100));
            shellManager.RegisterToolbar(new ToolbarContribution(OverviewToolbarId, "Home", OverviewCommandId, icon: "dashboard", order: 100));
            shellManager.SetActiveExplorer(FallbackExplorerId);
        }

        /// <summary>
        /// Registers the minimal Search module shell contributions required by the layout tests.
        /// </summary>
        /// <param name="shellManager">The shell manager that should receive the Search tool contributions.</param>
        private static void SeedSearchShell(WorkbenchShellManager shellManager)
        {
            // The helper provides the same Search explorer and command shape that the module contributes at runtime.
            shellManager.RegisterTool(new ToolDefinition("tool.module.search.query", "Search query", typeof(WorkbenchOverviewTool), "explorer.module.search.query", "manage_search", "Search module dummy tool."));
            shellManager.RegisterExplorer(new ExplorerContribution("explorer.module.search.query", "Query", "manage_search", 100));
            shellManager.RegisterExplorerSection(new ExplorerSectionContribution("explorer.section.search.query", "explorer.module.search.query", "Search module", 100));
            var searchActivationTarget = ActivationTarget.CreateToolSurfaceTarget(
                "tool.module.search.query",
                initialTitle: "Search query",
                initialIcon: "manage_search");
            shellManager.RegisterCommand(new CommandContribution("command.module.search.open-query", "Open Search query", CommandScope.Host, activationTarget: searchActivationTarget));
            shellManager.RegisterExplorerItem(new ExplorerItem("explorer.item.search.query", "explorer.module.search.query", "explorer.section.search.query", "Search query", "command.module.search.open-query", searchActivationTarget, "manage_search", "Search module dummy tool.", 100));
        }

        /// <summary>
        /// Creates a layout instance with the injected services required by the interaction tests.
        /// </summary>
        /// <param name="serviceProvider">The service provider that owns the Workbench shell test services.</param>
        /// <param name="shellManager">The seeded shell manager that should back the layout instance.</param>
        /// <returns>A layout instance with its injected dependencies populated.</returns>
        private static MainLayout CreateLayoutInstance(IServiceProvider serviceProvider, WorkbenchShellManager shellManager)
        {
            // The interaction tests call private handlers directly, so the layout instance needs the same injected services the runtime component would receive.
            var layout = new MainLayout();
            SetInjectedProperty(layout, "ShellManager", shellManager);
            SetInjectedProperty(layout, "WorkbenchOutputService", serviceProvider.GetRequiredService<IWorkbenchOutputService>());
            SetInjectedProperty(layout, "JsRuntime", serviceProvider.GetRequiredService<IJSRuntime>());
            SetInjectedProperty(layout, "Logger", serviceProvider.GetRequiredService<ILogger<MainLayout>>());
            SetInjectedProperty(layout, "NotificationService", serviceProvider.GetRequiredService<NotificationService>());
            SetInjectedProperty(layout, "StartupNotificationStore", serviceProvider.GetRequiredService<WorkbenchStartupNotificationStore>());
            SetInjectedProperty(layout, "ContextMenuService", serviceProvider.GetRequiredService<ContextMenuService>());
            SetInjectedProperty(layout, "TooltipService", serviceProvider.GetRequiredService<TooltipService>());
            return layout;
        }

        /// <summary>
        /// Sets one of the layout's injected properties through reflection for test setup.
        /// </summary>
        /// <param name="layout">The layout instance whose property should be populated.</param>
        /// <param name="propertyName">The injected property name to populate.</param>
        /// <param name="value">The value that should be assigned to the property.</param>
        private static void SetInjectedProperty(MainLayout layout, string propertyName, object value)
        {
            // The interaction tests bypass the Blazor renderer, so they populate private injected properties directly.
            var propertyInfo = typeof(MainLayout).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);

            if (propertyInfo is null)
            {
                throw new InvalidOperationException($"The property '{propertyName}' was not found on {nameof(MainLayout)}.");
            }

            propertyInfo.SetValue(layout, value);
        }

        /// <summary>
        /// Creates a deterministic output entry for rendering tests that need stable timestamps, identifiers, and optional diagnostics.
        /// </summary>
        /// <param name="id">The stable output-entry identifier.</param>
        /// <param name="level">The severity level to assign to the test entry.</param>
        /// <param name="source">The source value that should be rendered for the test entry.</param>
        /// <param name="summary">The compact summary that should be rendered for the test entry.</param>
        /// <param name="details">Optional expanded diagnostic details for the test entry.</param>
        /// <param name="eventCode">Optional event code rendered only in expanded details.</param>
        /// <returns>A deterministic immutable output entry suitable for rendering verification.</returns>
        private static OutputEntry CreateOutputEntry(
            string id,
            OutputLevel level,
            string source,
            string summary,
            string? details = null,
            string? eventCode = null)
        {
            // Fixed timestamps keep rendering assertions stable regardless of the machine or time zone that runs the tests.
            return new OutputEntry(
                id,
                new DateTimeOffset(2026, 2, 3, 14, 5, 6, TimeSpan.Zero),
                level,
                source,
                summary,
                details,
                eventCode);
        }

        /// <summary>
        /// Counts the number of times a rendered HTML fragment contains a specific token.
        /// </summary>
        /// <param name="content">The rendered HTML content to inspect.</param>
        /// <param name="token">The token whose occurrences should be counted.</param>
        /// <returns>The number of occurrences of the supplied token in the rendered content.</returns>
        private static int CountOccurrences(string content, string token)
        {
            // Stable token counting keeps the assertions lightweight without introducing an HTML parser into the focused rendering tests.
            ArgumentNullException.ThrowIfNull(content);
            ArgumentException.ThrowIfNullOrWhiteSpace(token);

            var occurrenceCount = 0;
            var searchIndex = 0;

            while ((searchIndex = content.IndexOf(token, searchIndex, StringComparison.Ordinal)) >= 0)
            {
                occurrenceCount++;
                searchIndex += token.Length;
            }

            return occurrenceCount;
        }

        /// <summary>
        /// Reads a private property value from the supplied instance.
        /// </summary>
        /// <typeparam name="TValue">The expected property value type.</typeparam>
        /// <param name="instance">The instance that owns the private property.</param>
        /// <param name="propertyName">The private property name to read.</param>
        /// <returns>The private property value cast to <typeparamref name="TValue"/>.</returns>
        private static TValue GetPrivatePropertyValue<TValue>(object instance, string propertyName)
        {
            // Reflection keeps layout-only state private while still letting the tests verify shell layout decisions directly.
            var propertyInfo = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);

            if (propertyInfo is null)
            {
                throw new InvalidOperationException($"The property '{propertyName}' was not found on {instance.GetType().Name}.");
            }

            return (TValue)(propertyInfo.GetValue(instance)
                ?? throw new InvalidOperationException($"The property '{propertyName}' did not return a value."));
        }

        /// <summary>
        /// Invokes a private instance method and returns its result.
        /// </summary>
        /// <param name="instance">The instance that owns the private method.</param>
        /// <param name="methodName">The private method name to invoke.</param>
        /// <param name="arguments">The arguments that should be supplied to the private method.</param>
        /// <returns>The result returned by the invoked private method.</returns>
        private static object? InvokePrivateMethod(object instance, string methodName, object?[] arguments)
        {
            // Reflection keeps the tests focused on Workbench behavior without promoting layout-only helpers into the public surface.
            var methodInfo = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (methodInfo is null)
            {
                throw new InvalidOperationException($"The method '{methodName}' was not found on {instance.GetType().Name}.");
            }

            return methodInfo.Invoke(instance, arguments);
        }

        /// <summary>
        /// Invokes a private static method and returns its result.
        /// </summary>
        /// <param name="declaringType">The type that owns the private static method.</param>
        /// <param name="methodName">The private static method name to invoke.</param>
        /// <param name="arguments">The arguments that should be supplied to the private static method.</param>
        /// <returns>The result returned by the invoked private static method.</returns>
        private static object? InvokeStaticPrivateMethod(Type declaringType, string methodName, object?[] arguments)
        {
            // Reflection keeps layout-only helpers private while still letting the tests verify the first-implementation context-menu contract.
            var methodInfo = declaringType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);

            if (methodInfo is null)
            {
                throw new InvalidOperationException($"The static method '{methodName}' was not found on {declaringType.Name}.");
            }

            return methodInfo.Invoke(null, arguments);
        }

        /// <summary>
        /// Invokes a private instance method that returns a task and awaits its completion.
        /// </summary>
        /// <param name="instance">The instance that owns the private method.</param>
        /// <param name="methodName">The private method name to invoke.</param>
        /// <param name="arguments">The arguments that should be supplied to the private method.</param>
        /// <returns>A task that completes when the private method task has completed.</returns>
        private static async Task InvokePrivateMethodAsync(object instance, string methodName, params object?[] arguments)
        {
            // Awaiting the reflected task keeps the interaction tests aligned with the asynchronous event-handler contract used by the component.
            var result = InvokePrivateMethod(instance, methodName, arguments);
            var task = result as Task;

            if (task is null)
            {
                throw new InvalidOperationException($"The method '{methodName}' did not return a {nameof(Task)} instance.");
            }

            await task.ConfigureAwait(false);
        }

        /// <summary>
        /// Invokes a private static method that returns a task and awaits its completion.
        /// </summary>
        /// <param name="declaringType">The type that owns the private static method.</param>
        /// <param name="methodName">The private static method name to invoke.</param>
        /// <param name="argument">The argument that should be supplied to the private static method.</param>
        /// <returns>A task that completes when the private static method task has completed.</returns>
        private static async Task InvokeStaticPrivateMethodAsync(Type declaringType, string methodName, object? argument)
        {
            // The tab middle-click test exercises the dedicated no-op helper directly because the tab markup routes auxiliary-click interactions there.
            var methodInfo = declaringType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);

            if (methodInfo is null)
            {
                throw new InvalidOperationException($"The static method '{methodName}' was not found on {declaringType.Name}.");
            }

            var result = methodInfo.Invoke(null, [argument]);
            var task = result as Task;

            if (task is null)
            {
                throw new InvalidOperationException($"The static method '{methodName}' did not return a {nameof(Task)} instance.");
            }

            await task.ConfigureAwait(false);
        }

        /// <summary>
        /// <summary>
        /// Renders the layout with a sentinel body fragment so shell chrome assertions stay focused on layout composition.
        /// </summary>
        /// <param name="renderer">The renderer that should be used to produce static HTML.</param>
        /// <returns>The rendered HTML for the layout.</returns>
        private static Task<string> RenderLayoutAsync(HtmlRenderer renderer)
        {
            // Reusing one helper keeps the rendering tests aligned and avoids duplicating the sentinel body setup.
            return renderer.Dispatcher.InvokeAsync(async () =>
            {
                var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(LayoutComponentBase.Body)] = (RenderFragment)(builder => builder.AddContent(0, "BodyMarker"))
                });

                var output = await renderer.RenderComponentAsync<MainLayout>(parameters);
                return output.ToHtmlString();
            });
        }

        /// <summary>
        /// <summary>
        /// Supplies a minimal JS runtime stub for static component rendering tests.
        /// </summary>
        private sealed class TestJsRuntime : IJSRuntime
        {
            private readonly TestJsModule _module = new();

            /// <summary>
            /// Returns a default value because the shell rendering test does not execute JavaScript.
            /// </summary>
            /// <typeparam name="TValue">The expected return type.</typeparam>
            /// <param name="identifier">The JavaScript identifier requested by the component.</param>
            /// <param name="args">The arguments that would be passed to JavaScript.</param>
            /// <returns>A completed task containing the default value for <typeparamref name="TValue"/>.</returns>
            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            {
                // Static HTML rendering can request JS modules, so the test runtime returns a stable fake module for imports and a default value for everything else.
                if (typeof(TValue) == typeof(IJSObjectReference) && string.Equals(identifier, "import", StringComparison.Ordinal))
                {
                    return ValueTask.FromResult((TValue)(object)_module);
                }

                return ValueTask.FromResult(default(TValue)!);
            }

            /// <summary>
            /// Returns a default value because the shell rendering test does not execute JavaScript.
            /// </summary>
            /// <typeparam name="TValue">The expected return type.</typeparam>
            /// <param name="identifier">The JavaScript identifier requested by the component.</param>
            /// <param name="cancellationToken">The cancellation token that would flow to the JavaScript invocation.</param>
            /// <param name="args">The arguments that would be passed to JavaScript.</param>
            /// <returns>A completed task containing the default value for <typeparamref name="TValue"/>.</returns>
            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            {
                // Static HTML rendering can request JS modules, so the test runtime returns a stable fake module for imports and a default value for everything else.
                if (typeof(TValue) == typeof(IJSObjectReference) && string.Equals(identifier, "import", StringComparison.Ordinal))
                {
                    return ValueTask.FromResult((TValue)(object)_module);
                }

                return ValueTask.FromResult(default(TValue)!);
            }
        }

        /// <summary>
        /// Supplies a minimal JS module stub for layout tests that now import an output-panel helper.
        /// </summary>
        private sealed class TestJsModule : IJSObjectReference
        {
            /// <summary>
            /// Returns a default value because the layout tests only need imported module calls to succeed.
            /// </summary>
            /// <typeparam name="TValue">The expected return type.</typeparam>
            /// <param name="identifier">The exported JavaScript function identifier.</param>
            /// <param name="args">The arguments that would be passed to the JavaScript function.</param>
            /// <returns>A completed task containing the default value for <typeparamref name="TValue"/>.</returns>
            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            {
                // The layout tests do not execute browser behavior; they only need module calls to remain non-throwing.
                return ValueTask.FromResult(default(TValue)!);
            }

            /// <summary>
            /// Returns a default value because the layout tests only need imported module calls to succeed.
            /// </summary>
            /// <typeparam name="TValue">The expected return type.</typeparam>
            /// <param name="identifier">The exported JavaScript function identifier.</param>
            /// <param name="cancellationToken">The cancellation token associated with the invocation.</param>
            /// <param name="args">The arguments that would be passed to the JavaScript function.</param>
            /// <returns>A completed task containing the default value for <typeparamref name="TValue"/>.</returns>
            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            {
                // The layout tests do not execute browser behavior; they only need module calls to remain non-throwing.
                return ValueTask.FromResult(default(TValue)!);
            }

            /// <summary>
            /// Releases the fake module without additional work.
            /// </summary>
            /// <returns>A completed value task.</returns>
            public ValueTask DisposeAsync()
            {
                // The fake module owns no unmanaged resources.
                return ValueTask.CompletedTask;
            }
        }

        /// <summary>
        /// Supplies a minimal navigation manager for Radzen services during static rendering tests.
        /// </summary>
        private sealed class TestNavigationManager : NavigationManager
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TestNavigationManager"/> class.
            /// </summary>
            public TestNavigationManager()
            {
                // Static rendering only needs a stable base URI so component services can be constructed successfully.
                Initialize("http://localhost/", "http://localhost/");
            }

            /// <summary>
            /// Ignores navigation requests because the shell rendering tests do not exercise navigation behavior.
            /// </summary>
            /// <param name="uri">The destination URI.</param>
            /// <param name="options">The navigation options associated with the request.</param>
            protected override void NavigateToCore(string uri, NavigationOptions options)
            {
                // The static renderer never navigates, so the test stub simply tracks the last URI value.
                Uri = ToAbsoluteUri(uri).ToString();
            }
        }
    }
}
