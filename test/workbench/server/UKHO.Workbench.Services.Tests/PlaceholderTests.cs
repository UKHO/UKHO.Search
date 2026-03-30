using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Workbench.Commands;
using UKHO.Workbench.Services.Shell;
using UKHO.Workbench.Tools;
using UKHO.Workbench.WorkbenchShell;
using Xunit;

namespace UKHO.Workbench.Services.Tests
{
    /// <summary>
    /// Verifies command routing, fixed context publication, and runtime contribution recomposition for the Workbench service layer.
    /// </summary>
    public class PlaceholderTests
    {
        /// <summary>
        /// Confirms a declarative activation command opens a tool and then reuses the existing singleton instance when executed again.
        /// </summary>
        [Fact]
        public async Task ExecuteRegisteredActivationCommandAndRefocusTheExistingSingletonTool()
        {
            // The shell manager now routes explorer, menu, toolbar, and hosted-tool actions through the same command path.
            var shellManager = new WorkbenchShellManager(NullLogger<WorkbenchShellManager>.Instance);
            shellManager.RegisterTool(new ToolDefinition("tool.search", "Search", typeof(TestToolComponent), "explorer.bootstrap", "search"));
            shellManager.RegisterCommand(
                new CommandContribution(
                    "command.search.open",
                    "Open Search",
                    CommandScope.Host,
                    activationTarget: ActivationTarget.CreateToolSurfaceTarget("tool.search")));

            // Executing the same activation command twice should keep the singleton tool policy intact.
            await shellManager.ExecuteCommandAsync("command.search.open");
            var firstActivation = shellManager.State.ActiveTool;
            await shellManager.ExecuteCommandAsync("command.search.open");
            var secondActivation = shellManager.State.ActiveTool;

            firstActivation.ShouldNotBeNull();
            secondActivation.ShouldBeSameAs(firstActivation);
            shellManager.State.ToolInstances.Count.ShouldBe(1);
        }

        /// <summary>
        /// Confirms runtime menu and status-bar contributions participate only while their owning tool is active.
        /// </summary>
        [Fact]
        public void RecomposeRuntimeContributionsWhenToolFocusChanges()
        {
            // Static shell contributions remain visible, while tool runtime contributions should disappear automatically when focus moves away.
            var shellManager = new WorkbenchShellManager(NullLogger<WorkbenchShellManager>.Instance);
            shellManager.RegisterTool(new ToolDefinition("tool.overview", "Overview", typeof(TestToolComponent), "explorer.bootstrap", "dashboard"));
            shellManager.RegisterTool(new ToolDefinition("tool.search", "Search", typeof(TestToolComponent), "explorer.bootstrap", "search"));
            shellManager.RegisterMenu(new MenuContribution("menu.host.overview", "Overview", "command.host.overview", order: 100));
            shellManager.RegisterStatusBar(new StatusBarContribution("status.host.ready", "Workbench ready", order: 100));

            // The Search tool publishes runtime contributions through its bounded context once it is active.
            var searchTool = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.search"));
            searchTool.Context.SetRuntimeMenuContributions([new MenuContribution("menu.runtime.search", "Run sample query", "command.search.run", ownerToolId: "tool.search", order: 200)]);
            searchTool.Context.SetRuntimeStatusBarContributions([new StatusBarContribution("status.runtime.search", "Sample query executed", ownerToolId: "tool.search", order: 200)]);

            shellManager.MenuContributions.Select(menuContribution => menuContribution.DisplayName).ShouldContain("Run sample query");
            shellManager.StatusBarContributions.Select(statusBarContribution => statusBarContribution.Text).ShouldContain("Sample query executed");

            // Activating the overview tool should automatically hide the Search tool runtime contributions while keeping static host contributions visible.
            shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.overview"));

            shellManager.MenuContributions.Select(menuContribution => menuContribution.DisplayName).ShouldContain("Overview");
            shellManager.MenuContributions.Select(menuContribution => menuContribution.DisplayName).ShouldNotContain("Run sample query");
            shellManager.StatusBarContributions.Select(statusBarContribution => statusBarContribution.Text).ShouldContain("Workbench ready");
            shellManager.StatusBarContributions.Select(statusBarContribution => statusBarContribution.Text).ShouldNotContain("Sample query executed");
        }

        /// <summary>
        /// Confirms the fixed Workbench context values reflect the current active tool and published selection summary.
        /// </summary>
        [Fact]
        public void ExposeTheFixedContextKeysForTheActiveTool()
        {
            // The first context model is intentionally small and fixed, so the shell should surface the current active tool and selection summary clearly.
            var shellManager = new WorkbenchShellManager(NullLogger<WorkbenchShellManager>.Instance);
            shellManager.RegisterTool(new ToolDefinition("tool.search", "Search", typeof(TestToolComponent), "explorer.bootstrap", "search"));

            var activeTool = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.search"));
            activeTool.Context.SetSelection("search.query", 2);

            shellManager.ContextValues[WorkbenchContextKeys.ActiveTool].ShouldBe("tool.search");
            shellManager.ContextValues[WorkbenchContextKeys.ActiveRegion].ShouldBe(WorkbenchShellRegion.ToolSurface.ToString());
            shellManager.ContextValues[WorkbenchContextKeys.SelectionType].ShouldBe("search.query");
            shellManager.ContextValues[WorkbenchContextKeys.SelectionCount].ShouldBe("2");
            shellManager.ContextValues[WorkbenchContextKeys.ToolSurfaceReady].ShouldBe(bool.TrueString);
        }

        /// <summary>
        /// Confirms the shell raises a user-safe notification when a command cannot activate its requested tool.
        /// </summary>
        [Fact]
        public async Task RaiseASafeNotificationWhenCommandExecutionFails()
        {
            // The shell should surface recoverable command failures as safe notifications so UI callers do not need to translate infrastructure details.
            var shellManager = new WorkbenchShellManager(NullLogger<WorkbenchShellManager>.Instance);
            shellManager.RegisterCommand(
                new CommandContribution(
                    "command.missing.open",
                    "Open missing tool",
                    CommandScope.Host,
                    activationTarget: ActivationTarget.CreateToolSurfaceTarget("tool.missing")));

            WorkbenchNotificationEventArgs? notification = null;
            shellManager.NotificationRaised += (_, args) => notification = args;

            await shellManager.ExecuteCommandAsync("command.missing.open");

            notification.ShouldNotBeNull();
            notification.Summary.ShouldBe("Workbench action failed");
            notification.Detail.ShouldBe("The selected Workbench action could not be completed. Check the application logs for more detail.");
        }

        /// <summary>
        /// Provides a minimal renderable component type for shell-service tests.
        /// </summary>
        private sealed class TestToolComponent : IComponent
        {
            /// <summary>
            /// Gets or sets the renderer handle supplied by Blazor.
            /// </summary>
            private RenderHandle RenderHandle { get; set; }

            /// <summary>
            /// Attaches the component to the supplied renderer.
            /// </summary>
            /// <param name="renderHandle">The renderer handle supplied by Blazor.</param>
            public void Attach(RenderHandle renderHandle)
            {
                // The test component stores the renderer handle only so it satisfies the IComponent contract.
                RenderHandle = renderHandle;
            }

            /// <summary>
            /// Accepts incoming parameters without rendering any UI because these tests exercise only service-layer behavior.
            /// </summary>
            /// <param name="parameters">The incoming parameters supplied by the renderer.</param>
            /// <returns>A completed task because the test component performs no rendering work.</returns>
            public Task SetParametersAsync(ParameterView parameters)
            {
                // The service-layer tests do not need component UI, so the stub simply completes immediately.
                return Task.CompletedTask;
            }
        }
    }
}
