using Microsoft.AspNetCore.Components;
using UKHO.Workbench.Tools;
using UKHO.Workbench.WorkbenchShell;

namespace UKHO.Workbench.Modules.Search.Tools
{
    /// <summary>
    /// Renders the dummy Search tool used to verify runtime Workbench menu, toolbar, status, and command behavior.
    /// </summary>
    public partial class SearchWorkbenchWelcomeTool
    {
        private const string SearchToolId = "tool.module.search.welcome";
        private const string ApplySampleSearchCommandId = "command.module.search.apply-sample";
        private const string ResetSearchWorkspaceCommandId = "command.module.search.reset-workspace";
        private bool _runtimeShellStateInitialized;
        private IReadOnlyDictionary<string, string> _contextValues = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets the bounded Workbench tool context for the active Search tool instance.
        /// </summary>
        [Parameter]
        [EditorRequired]
        public ToolContext ToolContext { get; set; } = null!;

        /// <summary>
        /// Gets the current fixed Workbench context values visible to the tool.
        /// </summary>
        protected IReadOnlyDictionary<string, string> ContextValues => _contextValues;

        /// <summary>
        /// Publishes the initial runtime shell state once the active tool context becomes available.
        /// </summary>
        protected override void OnParametersSet()
        {
            // The component publishes its runtime shell metadata lazily because the bounded ToolContext arrives through DynamicComponent parameters.
            ArgumentNullException.ThrowIfNull(ToolContext);

            _contextValues = ToolContext.GetContextValues();

            if (_runtimeShellStateInitialized)
            {
                return;
            }

            PublishRuntimeMenuAndToolbar();

            // A zero selection count indicates the tool is in its default dummy state, so the component seeds the initial title and status only then.
            if (!_contextValues.TryGetValue(WorkbenchContextKeys.SelectionCount, out var selectionCount) || selectionCount == "0")
            {
                ToolContext.SetTitle("Search workspace");
                ToolContext.SetIcon("travel_explore");
                ToolContext.SetBadge(null);
                ToolContext.SetRuntimeStatusBarContributions(
                [
                    new StatusBarContribution("status.runtime.search.ready", "Search workspace ready", ownerToolId: SearchToolId, order: 200)
                ]);
            }

            _runtimeShellStateInitialized = true;
        }

        /// <summary>
        /// Invokes the tool-scoped command that simulates running a sample Search action.
        /// </summary>
        /// <returns>A task that completes when the command has been processed.</returns>
        protected async Task RunSampleCommandAsync()
        {
            // Hosted tool actions still route through the Workbench command system so the shell remains command-centric end to end.
            await ToolContext.InvokeCommandAsync(ApplySampleSearchCommandId);
            _contextValues = ToolContext.GetContextValues();
        }

        /// <summary>
        /// Invokes the tool-scoped command that restores the dummy Search workspace to its default state.
        /// </summary>
        /// <returns>A task that completes when the command has been processed.</returns>
        protected async Task ResetWorkspaceAsync()
        {
            // Resetting uses the same bounded command path so the demo covers both state changes and recomposition in one place.
            await ToolContext.InvokeCommandAsync(ResetSearchWorkspaceCommandId);
            _contextValues = ToolContext.GetContextValues();
        }

        /// <summary>
        /// Publishes the Search tool runtime menu and toolbar contributions.
        /// </summary>
        private void PublishRuntimeMenuAndToolbar()
        {
            // Runtime menu and toolbar contributions remain constant for this dummy tool, so they are published once when the context becomes available.
            ToolContext.SetRuntimeMenuContributions(
            [
                new MenuContribution("menu.runtime.search.apply", "Run sample query", ApplySampleSearchCommandId, icon: "play_circle", ownerToolId: SearchToolId, order: 100),
                new MenuContribution("menu.runtime.search.reset", "Reset Search", ResetSearchWorkspaceCommandId, icon: "restart_alt", ownerToolId: SearchToolId, order: 101)
            ]);

            ToolContext.SetRuntimeToolbarContributions(
            [
                new ToolbarContribution("toolbar.runtime.search.apply", "Run sample query", ApplySampleSearchCommandId, icon: "play_circle", ownerToolId: SearchToolId, order: 100),
                new ToolbarContribution("toolbar.runtime.search.reset", "Reset Search", ResetSearchWorkspaceCommandId, icon: "restart_alt", ownerToolId: SearchToolId, order: 101)
            ]);
        }
    }
}
