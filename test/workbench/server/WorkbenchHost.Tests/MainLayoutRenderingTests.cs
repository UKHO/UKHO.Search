using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Radzen;
using Shouldly;
using UKHO.Workbench.Commands;
using UKHO.Workbench.Explorers;
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
    /// Verifies the bootstrap Workbench shell renders the expected desktop-like chrome.
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
        private const string HostReadyStatusId = "status.host.ready";

        /// <summary>
        /// Confirms the layout renders the full shell structure and exposes the registered exemplar tool in the explorer region.
        /// </summary>
        [Fact]
        public async Task RenderTheBootstrapWorkbenchShell()
        {
            // The renderer uses the same shell manager and Radzen registrations as the host so the markup reflects the real shell composition.
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddRadzenComponents();
            services.AddWorkbenchServices();
            services.AddSingleton<WorkbenchStartupNotificationStore>();
            services.AddSingleton<IJSRuntime, TestJsRuntime>();
            services.AddSingleton<NavigationManager, TestNavigationManager>();

            await using var serviceProvider = services.BuildServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();

            // Seed the shell through the same contribution model used by the runtime host so the rendered markup matches the new shell composition path.
            SeedHostShell(shellManager);
            shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget(OverviewToolId));

            var renderer = new HtmlRenderer(serviceProvider, serviceProvider.GetRequiredService<ILoggerFactory>());

            // Render the layout with a sentinel body fragment so the test can confirm the center surface hosts routed content.
            var html = await renderer.Dispatcher.InvokeAsync(async () =>
            {
                var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(LayoutComponentBase.Body)] = (RenderFragment)(builder => builder.AddContent(0, "BodyMarker"))
                });

                var output = await renderer.RenderComponentAsync<MainLayout>(parameters);
                return output.ToHtmlString();
            });

            html.ShouldContain("data-region=\"menu-bar\"");
            html.ShouldContain("data-region=\"activity-rail\"");
            html.ShouldContain("data-region=\"explorer\"");
            html.ShouldContain("data-region=\"tool-surface\"");
            html.ShouldContain("data-region=\"status-bar\"");
            html.ShouldContain("Workbench overview");
            html.ShouldContain("Overview");
            html.ShouldContain("BodyMarker");
        }

        /// <summary>
        /// Confirms runtime menu and status-bar contributions render only while their owning tool is active.
        /// </summary>
        [Fact]
        public async Task RenderRuntimeMenuAndStatusItemsOnlyForTheActiveTool()
        {
            // The host shell should surface runtime contributions for the focused tool only, even when the tool instance remains tracked as a singleton.
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddRadzenComponents();
            services.AddWorkbenchServices();
            services.AddSingleton<WorkbenchStartupNotificationStore>();
            services.AddSingleton<IJSRuntime, TestJsRuntime>();
            services.AddSingleton<NavigationManager, TestNavigationManager>();

            await using var serviceProvider = services.BuildServiceProvider();
            var shellManager = serviceProvider.GetRequiredService<WorkbenchShellManager>();
            SeedHostShell(shellManager);
            SeedSearchShell(shellManager);

            // Activate the Search tool and publish runtime menu/status contributions through its bounded tool context.
            var searchTool = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.module.search.query"));
            searchTool.Context.SetRuntimeMenuContributions([new MenuContribution("menu.runtime.search", "Run sample query", "command.search.run", ownerToolId: "tool.module.search.query", order: 200)]);
            searchTool.Context.SetRuntimeStatusBarContributions([new StatusBarContribution("status.runtime.search", "Sample query executed", ownerToolId: "tool.module.search.query", order: 200)]);

            var renderer = new HtmlRenderer(serviceProvider, serviceProvider.GetRequiredService<ILoggerFactory>());
            var activeSearchHtml = await RenderLayoutAsync(renderer);

            activeSearchHtml.ShouldContain("Run sample query");
            activeSearchHtml.ShouldContain("Sample query executed");

            // When focus moves back to the host overview tool, the Search runtime contributions should disappear from shell chrome.
            shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget(OverviewToolId));
            var activeOverviewHtml = await RenderLayoutAsync(renderer);

            activeOverviewHtml.ShouldNotContain("Run sample query");
            activeOverviewHtml.ShouldNotContain("Sample query executed");
            activeOverviewHtml.ShouldContain("Workbench shell ready");
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
            shellManager.RegisterCommand(
                new CommandContribution(
                    OverviewCommandId,
                    "Open Workbench overview",
                    CommandScope.Host,
                    activationTarget: ActivationTarget.CreateToolSurfaceTarget(OverviewToolId)));
            shellManager.RegisterExplorerItem(
                new ExplorerItem(
                    "explorer.item.host.overview",
                    FallbackExplorerId,
                    HostToolsSectionId,
                    "Workbench overview",
                    OverviewCommandId,
                    ActivationTarget.CreateToolSurfaceTarget(OverviewToolId),
                    "dashboard",
                    "Shows the first host-owned tool.",
                    100));
            shellManager.RegisterMenu(new MenuContribution(OverviewMenuId, "Overview", OverviewCommandId, icon: "dashboard", order: 100));
            shellManager.RegisterToolbar(new ToolbarContribution(OverviewToolbarId, "Overview", OverviewCommandId, icon: "dashboard", order: 100));
            shellManager.RegisterStatusBar(new StatusBarContribution(HostReadyStatusId, "Workbench shell ready", icon: "check_circle", order: 100));
            shellManager.SetActiveExplorer(FallbackExplorerId);
        }

        /// <summary>
        /// Registers the minimal Search module shell contributions required by the rendering tests.
        /// </summary>
        /// <param name="shellManager">The shell manager that should receive the Search tool contributions.</param>
        private static void SeedSearchShell(WorkbenchShellManager shellManager)
        {
            // The helper provides the same Search explorer and command shape that the module contributes at runtime.
            shellManager.RegisterTool(new ToolDefinition("tool.module.search.query", "Search query", typeof(WorkbenchOverviewTool), "explorer.module.search.query", "manage_search", "Search module dummy tool."));
            shellManager.RegisterExplorer(new ExplorerContribution("explorer.module.search.query", "Query", "manage_search", 100));
            shellManager.RegisterExplorerSection(new ExplorerSectionContribution("explorer.section.search.query", "explorer.module.search.query", "Search module", 100));
            shellManager.RegisterCommand(new CommandContribution("command.module.search.open-query", "Open Search query", CommandScope.Host, activationTarget: ActivationTarget.CreateToolSurfaceTarget("tool.module.search.query")));
            shellManager.RegisterExplorerItem(new ExplorerItem("explorer.item.search.query", "explorer.module.search.query", "explorer.section.search.query", "Search query", "command.module.search.open-query", ActivationTarget.CreateToolSurfaceTarget("tool.module.search.query"), "manage_search", "Search module dummy tool.", 100));
        }

        /// <summary>
        /// Renders the layout with a sentinel body fragment so shell chrome assertions stay focused on layout composition.
        /// </summary>
        /// <param name="renderer">The renderer that should be used to produce static HTML.</param>
        /// <returns>The rendered HTML for the layout.</returns>
        private static Task<string> RenderLayoutAsync(HtmlRenderer renderer)
        {
            // Reusing one helper keeps the two rendering tests aligned and avoids duplicating the sentinel body setup.
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
        /// Supplies a minimal JS runtime stub for static component rendering tests.
        /// </summary>
        private sealed class TestJsRuntime : IJSRuntime
        {
            /// <summary>
            /// Returns a default value because the shell rendering test does not execute JavaScript.
            /// </summary>
            /// <typeparam name="TValue">The expected return type.</typeparam>
            /// <param name="identifier">The JavaScript identifier requested by the component.</param>
            /// <param name="args">The arguments that would be passed to JavaScript.</param>
            /// <returns>A completed task containing the default value for <typeparamref name="TValue"/>.</returns>
            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            {
                // Static HTML rendering never executes the Workbench splitter interop, so returning the default value is sufficient for this test.
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
                // Static HTML rendering never executes the Workbench splitter interop, so returning the default value is sufficient for this test.
                return ValueTask.FromResult(default(TValue)!);
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
            /// Ignores navigation requests because the shell rendering test does not exercise navigation behavior.
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
