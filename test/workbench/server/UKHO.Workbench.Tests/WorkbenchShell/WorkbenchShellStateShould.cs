using Shouldly;
using UKHO.Workbench.Layout;
using UKHO.Workbench.Tools;
using UKHO.Workbench.WorkbenchShell;
using Xunit;

namespace UKHO.Workbench.Tests.WorkbenchShell
{
    /// <summary>
    /// Verifies the lightweight Workbench shell state model introduced for the bootstrap shell slice.
    /// </summary>
    public class WorkbenchShellStateShould
    {
        /// <summary>
        /// Confirms the bootstrap shell exposes the fixed chrome regions required by the first desktop-like layout.
        /// </summary>
        [Fact]
        public void StartWithAllBootstrapShellRegionsVisible()
        {
            // The bootstrap slice keeps the shell layout fixed so the host can render every required region without optional docking rules.
            var state = new WorkbenchShellState();

            // The state should expose the complete region list expected by the shell layout.
            state.VisibleRegions.ShouldBe(
            [
                WorkbenchShellRegion.MenuBar,
                WorkbenchShellRegion.ActivityRail,
                WorkbenchShellRegion.Explorer,
                WorkbenchShellRegion.ToolSurface,
                WorkbenchShellRegion.ActiveToolToolbar,
                WorkbenchShellRegion.StatusBar
            ]);
        }

        /// <summary>
        /// Confirms reopening a singleton tool reuses the existing runtime instance rather than duplicating it.
        /// </summary>
        [Fact]
        public void ReuseTheExistingInstanceWhenASingletonToolIsActivatedAgain()
        {
            // The bootstrap slice intentionally uses singleton shell-level hosting so tool reopening only changes focus.
            var state = new WorkbenchShellState();
            var definition = new ToolDefinition(
                "tool.bootstrap.overview",
                "Workbench overview",
                typeof(Grid),
                "explorer.bootstrap",
                "dashboard",
                "Shows the first host-owned tool.");
            var activationTarget = ActivationTarget.CreateToolSurfaceTarget(definition.Id);

            // The first activation creates the runtime tool instance.
            var firstActivation = state.ActivateTool(definition, activationTarget, CreateToolContext);

            // Reopening the same singleton tool should focus the original runtime instance.
            var secondActivation = state.ActivateTool(definition, activationTarget, CreateToolContext);

            firstActivation.ShouldBeSameAs(secondActivation);
            state.ToolInstances.Count.ShouldBe(1);
            state.ActiveTool.ShouldBe(firstActivation);
        }

        /// <summary>
        /// Confirms the shell state rejects requests to host tool content in unsupported regions.
        /// </summary>
        [Fact]
        public void ThrowWhenAToolIsActivatedIntoANonToolRegion()
        {
            // The bootstrap slice only supports hosting tools in the central working region.
            var state = new WorkbenchShellState();
            var definition = new ToolDefinition(
                "tool.bootstrap.overview",
                "Workbench overview",
                typeof(Grid),
                "explorer.bootstrap",
                "dashboard",
                "Shows the first host-owned tool.");
            var activationTarget = new ActivationTarget(definition.Id, WorkbenchShellRegion.MenuBar);

            // Activating a tool outside the central surface should fail fast with a descriptive error.
            var exception = Should.Throw<InvalidOperationException>(() => state.ActivateTool(definition, activationTarget, CreateToolContext));

            exception.Message.ShouldContain("ToolSurface");
        }

        /// <summary>
        /// Creates a bounded tool context for shell-state tests.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance identifier that should be bound to the new context.</param>
        /// <returns>A bounded tool context backed by a no-op test bridge.</returns>
        private static ToolContext CreateToolContext(string toolInstanceId)
        {
            // The shell-state tests exercise only state transitions, so a no-op bridge is sufficient for runtime tool-context creation.
            return new ToolContext(toolInstanceId, new TestToolContextBridge());
        }

        /// <summary>
        /// Provides a no-op Workbench tool-context bridge for shell-state tests.
        /// </summary>
        private sealed class TestToolContextBridge : IToolContextBridge
        {
            /// <summary>
            /// Ignores tool-opening requests because shell-state tests do not exercise nested activation flows.
            /// </summary>
            /// <param name="toolInstanceId">The runtime tool instance issuing the request.</param>
            /// <param name="activationTarget">The shell activation target that would be opened or focused.</param>
            /// <param name="cancellationToken">The cancellation token that would flow with the request.</param>
            /// <returns>A completed task because the test bridge performs no work.</returns>
            public Task OpenToolAsync(string toolInstanceId, ActivationTarget activationTarget, CancellationToken cancellationToken = default)
            {
                // The no-op bridge keeps the tests focused on shell state rather than runtime tool behavior.
                return Task.CompletedTask;
            }

            /// <summary>
            /// Ignores command-invocation requests because shell-state tests do not exercise command routing.
            /// </summary>
            /// <param name="toolInstanceId">The runtime tool instance issuing the request.</param>
            /// <param name="commandId">The command identifier that would be invoked.</param>
            /// <param name="cancellationToken">The cancellation token that would flow with the request.</param>
            /// <returns>A completed task because the test bridge performs no work.</returns>
            public Task InvokeCommandAsync(string toolInstanceId, string commandId, CancellationToken cancellationToken = default)
            {
                // The no-op bridge keeps the tests focused on shell state rather than runtime tool behavior.
                return Task.CompletedTask;
            }

            /// <summary>
            /// Ignores title updates because shell-state tests do not assert runtime shell metadata changes.
            /// </summary>
            /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
            /// <param name="title">The new title that would be shown by the shell.</param>
            public void UpdateTitle(string toolInstanceId, string title)
            {
                // The no-op bridge keeps the tests focused on shell state rather than runtime tool behavior.
            }

            /// <summary>
            /// Ignores icon updates because shell-state tests do not assert runtime shell metadata changes.
            /// </summary>
            /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
            /// <param name="icon">The new icon that would be shown by the shell.</param>
            public void UpdateIcon(string toolInstanceId, string icon)
            {
                // The no-op bridge keeps the tests focused on shell state rather than runtime tool behavior.
            }

            /// <summary>
            /// Ignores badge updates because shell-state tests do not assert runtime shell metadata changes.
            /// </summary>
            /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
            /// <param name="badge">The new badge text that would be shown by the shell.</param>
            public void UpdateBadge(string toolInstanceId, string? badge)
            {
                // The no-op bridge keeps the tests focused on shell state rather than runtime tool behavior.
            }

            /// <summary>
            /// Ignores runtime menu updates because shell-state tests do not exercise contribution composition.
            /// </summary>
            /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
            /// <param name="menuContributions">The runtime menu contributions that would be visible while the tool is active.</param>
            public void UpdateRuntimeMenuContributions(string toolInstanceId, IReadOnlyList<MenuContribution> menuContributions)
            {
                // The no-op bridge keeps the tests focused on shell state rather than runtime tool behavior.
            }

            /// <summary>
            /// Ignores runtime toolbar updates because shell-state tests do not exercise contribution composition.
            /// </summary>
            /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
            /// <param name="toolbarContributions">The runtime toolbar contributions that would be visible while the tool is active.</param>
            public void UpdateRuntimeToolbarContributions(string toolInstanceId, IReadOnlyList<ToolbarContribution> toolbarContributions)
            {
                // The no-op bridge keeps the tests focused on shell state rather than runtime tool behavior.
            }

            /// <summary>
            /// Ignores runtime status-bar updates because shell-state tests do not exercise contribution composition.
            /// </summary>
            /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
            /// <param name="statusBarContributions">The runtime status-bar contributions that would be visible while the tool is active.</param>
            public void UpdateRuntimeStatusBarContributions(string toolInstanceId, IReadOnlyList<StatusBarContribution> statusBarContributions)
            {
                // The no-op bridge keeps the tests focused on shell state rather than runtime tool behavior.
            }

            /// <summary>
            /// Ignores selection updates because shell-state tests do not assert fixed context values.
            /// </summary>
            /// <param name="toolInstanceId">The runtime tool instance being updated.</param>
            /// <param name="selectionType">The logical selection type that would be published by the tool.</param>
            /// <param name="selectionCount">The number of selected items that would be published by the tool.</param>
            public void UpdateSelection(string toolInstanceId, string? selectionType, int selectionCount)
            {
                // The no-op bridge keeps the tests focused on shell state rather than runtime tool behavior.
            }

            /// <summary>
            /// Returns an empty fixed-context snapshot because shell-state tests do not require runtime context values.
            /// </summary>
            /// <param name="toolInstanceId">The runtime tool instance requesting the current context snapshot.</param>
            /// <returns>An empty context-value dictionary.</returns>
            public IReadOnlyDictionary<string, string> GetContextValues(string toolInstanceId)
            {
                // The no-op bridge keeps the tests focused on shell state rather than runtime tool behavior.
                return new Dictionary<string, string>(StringComparer.Ordinal);
            }

            /// <summary>
            /// Ignores notification requests because shell-state tests do not exercise user-facing notifications.
            /// </summary>
            /// <param name="toolInstanceId">The runtime tool instance issuing the notification.</param>
            /// <param name="severity">The shell notification severity value that would be raised.</param>
            /// <param name="summary">The short summary that would be shown to the user.</param>
            /// <param name="detail">The longer detail that would be shown to the user.</param>
            /// <param name="cancellationToken">The cancellation token that would flow with the notification request.</param>
            /// <returns>A completed task because the test bridge performs no work.</returns>
            public Task NotifyAsync(string toolInstanceId, string severity, string summary, string detail, CancellationToken cancellationToken = default)
            {
                // The no-op bridge keeps the tests focused on shell state rather than runtime tool behavior.
                return Task.CompletedTask;
            }
        }
    }
}
