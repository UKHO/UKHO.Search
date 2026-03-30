using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Radzen;
using Shouldly;
using UKHO.Workbench.Commands;
using UKHO.Workbench.Explorers;
using UKHO.Workbench.Infrastructure.Modules;
using UKHO.Workbench.Modules;
using UKHO.Workbench.Modules.Admin;
using UKHO.Workbench.Modules.FileShare;
using UKHO.Workbench.Modules.PKS;
using UKHO.Workbench.Modules.Search;
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
    /// Verifies that dynamically discovered module tools appear in the Workbench shell only when the module is enabled.
    /// </summary>
    public class WorkbenchModuleRenderingTests
    {
        private const string BootstrapExplorerId = "explorer.bootstrap";
        private const string BootstrapExplorerDisplayName = "Workbench";
        private const string HostToolsSectionId = "explorer.section.host.tools";
        private const string OverviewToolId = "tool.bootstrap.overview";
        private const string OverviewCommandId = "command.host.open-overview";
        private const string OverviewMenuId = "menu.host.overview";
        private const string OverviewToolbarId = "toolbar.host.overview";
        private const string HostReadyStatusId = "status.host.ready";

        /// <summary>
        /// Confirms the full initial module map can be discovered, rendered, and activated through the shared singleton shell path.
        /// </summary>
        [Fact]
        public async Task RenderTheInitialModuleMapAndOpenEachModuleTool()
        {
            // The test runs the real reader, scanner, and loader pipeline against the compiled module outputs so the host bootstrap stays end to end.
            var moduleContributions = LoadModuleContributions(
                new Dictionary<string, bool>(StringComparer.Ordinal)
                {
                    ["UKHO.Workbench.Modules.Search"] = true,
                    ["UKHO.Workbench.Modules.PKS"] = true,
                    ["UKHO.Workbench.Modules.FileShare"] = true,
                    ["UKHO.Workbench.Modules.Admin"] = true
                });

            moduleContributions.ToolDefinitions.Count.ShouldBe(6);
            moduleContributions.ExplorerItems.Count.ShouldBe(6);
            moduleContributions.ToolDefinitions.Select(toolDefinition => toolDefinition.DisplayName).ShouldContain("Search ingestion");
            moduleContributions.ToolDefinitions.Select(toolDefinition => toolDefinition.DisplayName).ShouldContain("Search query");
            moduleContributions.ToolDefinitions.Select(toolDefinition => toolDefinition.DisplayName).ShouldContain("Ingestion rule editor");
            moduleContributions.ToolDefinitions.Select(toolDefinition => toolDefinition.DisplayName).ShouldContain("PKS operations");
            moduleContributions.ToolDefinitions.Select(toolDefinition => toolDefinition.DisplayName).ShouldContain("File Share workspace");
            moduleContributions.ToolDefinitions.Select(toolDefinition => toolDefinition.DisplayName).ShouldContain("Administration");

            var shellManager = CreateShellManager(moduleContributions);

            await AssertSingletonActivationAsync(shellManager, "command.module.search.open-ingestion", "Search ingestion");
            await AssertSingletonActivationAsync(shellManager, "command.module.search.open-query", "Search query");
            await AssertSingletonActivationAsync(shellManager, "command.module.search.open-rule-editor", "Ingestion rule editor");
            await AssertSingletonActivationAsync(shellManager, "command.module.pks.open-operations", "PKS operations");
            await AssertSingletonActivationAsync(shellManager, "command.module.fileshare.open-workspace", "File Share workspace");
            await AssertSingletonActivationAsync(shellManager, "command.module.admin.open-console", "Administration");

            // Rendering the shell after activation should expose every enabled module tool in the explorer chrome.
            var html = await RenderLayoutAsync(shellManager);

            html.ShouldContain("Search ingestion");
            html.ShouldContain("Search query");
            html.ShouldContain("Ingestion rule editor");
            html.ShouldContain("PKS operations");
            html.ShouldContain("File Share workspace");
            html.ShouldContain("Administration");
        }

        /// <summary>
        /// Confirms disabled modules remain absent from the shell even when their assemblies are present in approved probe roots.
        /// </summary>
        [Fact]
        public async Task KeepDisabledModuleToolsOutOfTheShell()
        {
            // The disabled case uses the same probe roots but flips host-owned enablement flags in modules.json.
            var moduleContributions = LoadModuleContributions(
                new Dictionary<string, bool>(StringComparer.Ordinal)
                {
                    ["UKHO.Workbench.Modules.Search"] = true,
                    ["UKHO.Workbench.Modules.PKS"] = false,
                    ["UKHO.Workbench.Modules.FileShare"] = false,
                    ["UKHO.Workbench.Modules.Admin"] = true
                });

            moduleContributions.ToolDefinitions.Count.ShouldBe(4);

            var shellManager = CreateShellManager(moduleContributions);

            // Rendering the shell should show only the enabled module tools alongside the host-owned overview tool.
            var html = await RenderLayoutAsync(shellManager);

            html.ShouldContain("Search ingestion");
            html.ShouldContain("Search query");
            html.ShouldContain("Ingestion rule editor");
            html.ShouldContain("Administration");
            html.ShouldNotContain("PKS operations");
            html.ShouldNotContain("File Share workspace");
        }

        /// <summary>
        /// Loads module tool definitions through the real configuration, scanning, and loader pipeline.
        /// </summary>
        /// <param name="moduleEnablement">The host-owned module enablement flags that should be written into the temporary configuration.</param>
        /// <returns>The tool definitions contributed by the enabled modules for the supplied configuration.</returns>
        private static WorkbenchContributionRegistry LoadModuleContributions(IReadOnlyDictionary<string, bool> moduleEnablement)
        {
            // A temporary configuration directory is used so the host-level discovery pipeline can be executed without mutating repository files.
            var temporaryDirectoryPath = Path.Combine(Path.GetTempPath(), $"workbench-host-module-render-{Guid.NewGuid():N}");
            Directory.CreateDirectory(temporaryDirectoryPath);

            try
            {
                var configurationPath = Path.Combine(temporaryDirectoryPath, "modules.json");
                var probeRoots = GetModuleProbeRoots();
                var configuration = new
                {
                    probeRoots,
                    modules = moduleEnablement.Select(module => new { id = module.Key, enabled = module.Value }).ToArray()
                };

                File.WriteAllText(
                    configurationPath,
                    System.Text.Json.JsonSerializer.Serialize(
                        configuration,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            WriteIndented = true
                        }));

                var reader = new ModulesConfigurationReader(NullLogger<ModulesConfigurationReader>.Instance);
                var scanner = new ModuleAssemblyScanner(NullLogger<ModuleAssemblyScanner>.Instance);
                var loader = new ModuleLoader(NullLogger<ModuleLoader>.Instance);
                var registry = new WorkbenchContributionRegistry();

                // The same startup pipeline used by the host should produce module tool definitions when the module is enabled.
                var options = reader.Read(configurationPath);
                var discoveredAssemblies = scanner.Scan(options, temporaryDirectoryPath);
                var loadResult = loader.LoadModules(discoveredAssemblies, new ServiceCollection(), registry);

                loadResult.Failures.ShouldBeEmpty();
                return registry;
            }
            finally
            {
                // Temporary configuration artifacts are removed after the discovery assertions complete.
                Directory.Delete(temporaryDirectoryPath, true);
            }
        }

        /// <summary>
        /// Returns the compiled probe-root directories for the initial Workbench module map.
        /// </summary>
        /// <returns>The absolute probe-root directories that should be scanned during the host-style discovery run.</returns>
        private static IReadOnlyList<string> GetModuleProbeRoots()
        {
            // The helper uses the compiled assembly locations so the test exercises the same file-system probe-root logic as the production host.
            return
            [
                Path.GetDirectoryName(typeof(SearchWorkbenchModule).Assembly.Location)
                    ?? throw new InvalidOperationException("The compiled Search module output directory could not be resolved."),
                Path.GetDirectoryName(typeof(PksWorkbenchModule).Assembly.Location)
                    ?? throw new InvalidOperationException("The compiled PKS module output directory could not be resolved."),
                Path.GetDirectoryName(typeof(FileShareWorkbenchModule).Assembly.Location)
                    ?? throw new InvalidOperationException("The compiled File Share module output directory could not be resolved."),
                Path.GetDirectoryName(typeof(AdminWorkbenchModule).Assembly.Location)
                    ?? throw new InvalidOperationException("The compiled Admin module output directory could not be resolved.")
            ];
        }

        /// <summary>
        /// Executes the supplied open command twice and verifies the Workbench reuses the same singleton instance.
        /// </summary>
        /// <param name="shellManager">The seeded shell manager that should execute the command.</param>
        /// <param name="commandId">The command identifier that should open or focus the tool.</param>
        /// <param name="expectedDisplayName">The display name that should belong to the active tool after the command completes.</param>
        /// <returns>A task that completes when the singleton activation assertions have been evaluated.</returns>
        private static async Task AssertSingletonActivationAsync(WorkbenchShellManager shellManager, string commandId, string expectedDisplayName)
        {
            // Every module tool should reuse the same singleton shell instance when reopened through its declarative command contribution.
            await shellManager.ExecuteCommandAsync(commandId);
            var firstActivation = shellManager.State.ActiveTool;
            await shellManager.ExecuteCommandAsync(commandId);
            var secondActivation = shellManager.State.ActiveTool;

            firstActivation.ShouldNotBeNull();
            firstActivation.Definition.DisplayName.ShouldBe(expectedDisplayName);
            secondActivation.ShouldBeSameAs(firstActivation);
        }

        /// <summary>
        /// Creates a seeded shell manager that mirrors the host bootstrap registration path.
        /// </summary>
        /// <param name="moduleContributions">The module-contributed shell definitions that should be registered with the shell.</param>
        /// <returns>A shell manager seeded with the host tool and any supplied module tools.</returns>
        private static WorkbenchShellManager CreateShellManager(WorkbenchContributionRegistry moduleContributions)
        {
            // The helper mirrors Program.cs by registering the host shell surfaces first and then applying any discovered module contributions.
            var shellManager = new WorkbenchShellManager(NullLogger<WorkbenchShellManager>.Instance);
            shellManager.RegisterTool(
                new ToolDefinition(
                    OverviewToolId,
                    "Workbench overview",
                    typeof(WorkbenchOverviewTool),
                    BootstrapExplorerId,
                    "dashboard",
                    "Shows the first host-owned tool."));
            shellManager.RegisterExplorer(new ExplorerContribution(BootstrapExplorerId, BootstrapExplorerDisplayName, "dashboard_customize", 0));
            shellManager.RegisterExplorerSection(new ExplorerSectionContribution(HostToolsSectionId, BootstrapExplorerId, "Host tools", 100));
            shellManager.RegisterCommand(new CommandContribution(OverviewCommandId, "Open Workbench overview", CommandScope.Host, activationTarget: ActivationTarget.CreateToolSurfaceTarget(OverviewToolId)));
            shellManager.RegisterExplorerItem(new ExplorerItem("explorer.item.host.overview", BootstrapExplorerId, HostToolsSectionId, "Workbench overview", OverviewCommandId, ActivationTarget.CreateToolSurfaceTarget(OverviewToolId), "dashboard", "Shows the first host-owned tool.", 100));
            shellManager.RegisterMenu(new MenuContribution(OverviewMenuId, "Overview", OverviewCommandId, icon: "dashboard", order: 100));
            shellManager.RegisterToolbar(new ToolbarContribution(OverviewToolbarId, "Overview", OverviewCommandId, icon: "dashboard", order: 100));
            shellManager.RegisterStatusBar(new StatusBarContribution(HostReadyStatusId, "Workbench shell ready", icon: "check_circle", order: 100));

            foreach (var moduleToolDefinition in moduleContributions.ToolDefinitions)
            {
                // Module-contributed tools are registered through the same shell manager entry point used by host-owned tools.
                shellManager.RegisterTool(moduleToolDefinition);
            }

            foreach (var commandContribution in moduleContributions.CommandContributions)
            {
                shellManager.RegisterCommand(commandContribution);
            }

            foreach (var explorerContribution in moduleContributions.ExplorerContributions)
            {
                shellManager.RegisterExplorer(explorerContribution);
            }

            foreach (var explorerSectionContribution in moduleContributions.ExplorerSectionContributions)
            {
                shellManager.RegisterExplorerSection(explorerSectionContribution);
            }

            foreach (var explorerItem in moduleContributions.ExplorerItems)
            {
                shellManager.RegisterExplorerItem(explorerItem);
            }

            foreach (var menuContribution in moduleContributions.MenuContributions)
            {
                shellManager.RegisterMenu(menuContribution);
            }

            foreach (var toolbarContribution in moduleContributions.ToolbarContributions)
            {
                shellManager.RegisterToolbar(toolbarContribution);
            }

            foreach (var statusBarContribution in moduleContributions.StatusBarContributions)
            {
                shellManager.RegisterStatusBar(statusBarContribution);
            }

            shellManager.SetActiveExplorer(BootstrapExplorerId);
            shellManager.ActivateTool(
                ActivationTarget.CreateToolSurfaceTarget(
                    moduleContributions.ToolDefinitions.FirstOrDefault()?.Id ?? OverviewToolId));

            return shellManager;
        }

        /// <summary>
        /// Renders the shell layout using the supplied shell manager instance.
        /// </summary>
        /// <param name="shellManager">The seeded shell manager that should back the rendered layout.</param>
        /// <returns>The rendered HTML for the layout.</returns>
        private static async Task<string> RenderLayoutAsync(WorkbenchShellManager shellManager)
        {
            // The layout uses the same Radzen services as the host, with a seeded shell manager standing in for the runtime singleton.
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddRadzenComponents();
            services.AddSingleton(shellManager);
            services.AddSingleton<WorkbenchStartupNotificationStore>();
            services.AddSingleton<IJSRuntime, TestJsRuntime>();
            services.AddSingleton<NavigationManager, TestNavigationManager>();

            await using var serviceProvider = services.BuildServiceProvider();
            var renderer = new HtmlRenderer(serviceProvider, serviceProvider.GetRequiredService<ILoggerFactory>());

            // Rendering the layout with a sentinel body confirms that the shell chrome and center surface are both present.
            return await renderer.Dispatcher.InvokeAsync(async () =>
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
            /// Returns a default value because the shell rendering tests do not execute JavaScript.
            /// </summary>
            /// <typeparam name="TValue">The expected return type.</typeparam>
            /// <param name="identifier">The JavaScript identifier requested by the component.</param>
            /// <param name="args">The arguments that would be passed to JavaScript.</param>
            /// <returns>A completed task containing the default value for <typeparamref name="TValue"/>.</returns>
            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            {
                // Static HTML rendering never executes the Workbench splitter interop, so returning the default value is sufficient for these tests.
                return ValueTask.FromResult(default(TValue)!);
            }

            /// <summary>
            /// Returns a default value because the shell rendering tests do not execute JavaScript.
            /// </summary>
            /// <typeparam name="TValue">The expected return type.</typeparam>
            /// <param name="identifier">The JavaScript identifier requested by the component.</param>
            /// <param name="cancellationToken">The cancellation token that would flow to the JavaScript invocation.</param>
            /// <param name="args">The arguments that would be passed to JavaScript.</param>
            /// <returns>A completed task containing the default value for <typeparamref name="TValue"/>.</returns>
            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            {
                // Static HTML rendering never executes the Workbench splitter interop, so returning the default value is sufficient for these tests.
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
            /// Ignores navigation requests because the rendering tests do not exercise navigation behavior.
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
