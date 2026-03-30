using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Workbench.Services.Shell;
using UKHO.Workbench.Tools;
using WorkbenchHost.Components.Tools;
using Xunit;

namespace WorkbenchHost.Tests
{
    /// <summary>
    /// Verifies the host-facing shell manager behavior used by the first Workbench slice.
    /// </summary>
    public class WorkbenchShellManagerTests
    {
        /// <summary>
        /// Confirms the shell manager reuses an already open singleton tool when the same activation is requested again.
        /// </summary>
        [Fact]
        public void ReuseTheExistingToolInstanceWhenTheSameToolIsActivatedTwice()
        {
            // The bootstrap shell keeps the activation path intentionally lightweight by routing duplicate activations back to the existing singleton instance.
            var shellManager = new WorkbenchShellManager(NullLogger<WorkbenchShellManager>.Instance);
            var definition = new ToolDefinition(
                "tool.bootstrap.overview",
                "Workbench overview",
                typeof(WorkbenchOverviewTool),
                "explorer.bootstrap",
                "dashboard",
                "Shows the first host-owned tool.");

            // Register the exemplar tool so the shell can resolve activation requests by identifier.
            shellManager.RegisterTool(definition);

            // The first request creates the instance and the second request should simply re-focus it.
            var firstActivation = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget(definition.Id));
            var secondActivation = shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget(definition.Id));

            firstActivation.ShouldBeSameAs(secondActivation);
            shellManager.State.ToolInstances.Count.ShouldBe(1);
            shellManager.State.ActiveTool.ShouldBe(firstActivation);
        }

        /// <summary>
        /// Confirms activation fails fast when the host requests a tool that was never registered.
        /// </summary>
        [Fact]
        public void ThrowWhenTheHostRequestsAnUnknownTool()
        {
            // The shell manager should surface a clear failure so the host can log and notify without silently losing the activation request.
            var shellManager = new WorkbenchShellManager(NullLogger<WorkbenchShellManager>.Instance);

            // Unknown tool identifiers should fail with a descriptive message.
            var exception = Should.Throw<InvalidOperationException>(() => shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget("tool.unknown")));

            exception.Message.ShouldContain("tool.unknown");
        }
    }
}
