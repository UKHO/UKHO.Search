using Microsoft.AspNetCore.Components;
using UKHO.Workbench.Tools;
using UKHO.Workbench.WorkbenchShell;

namespace UKHO.Workbench.Modules.Search.Tools
{
    /// <summary>
    /// Renders the dummy Search query tool used to verify runtime Workbench menu, toolbar, status, and command behavior.
    /// </summary>
    public partial class SearchQueryTool
    {
        private const string SearchQueryToolId = "tool.module.search.query";
        private const string RunSampleSearchQueryCommandId = "command.module.search.query.run-sample";
        private const string ResetSearchQueryCommandId = "command.module.search.query.reset";
        private bool _runtimeShellStateInitialized;
        private IReadOnlyDictionary<string, string> _contextValues = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets the bounded Workbench tool context for the active Search query tool instance.
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
                ToolContext.SetTitle("Search query");
                ToolContext.SetIcon("manage_search");
                ToolContext.SetBadge(null);
                ToolContext.SetRuntimeStatusBarContributions(
                [
                    new StatusBarContribution("status.runtime.search.query.ready", "Search query ready", ownerToolId: SearchQueryToolId, order: 200)
                ]);
            }

            _runtimeShellStateInitialized = true;
        }

        /// <summary>
        /// Invokes the tool-scoped command that simulates running a sample Search query action.
        /// </summary>
        /// <returns>A task that completes when the command has been processed.</returns>
        protected async Task RunSampleCommandAsync()
        {
            try
            {
                // Hosted tool actions still route through the Workbench command system so the shell remains command-centric end to end.
                await ToolContext.InvokeCommandAsync(RunSampleSearchQueryCommandId);
            }
            catch (Exception)
            {
                // Recoverable command failures are already logged and surfaced by the shell manager, so the component prevents the UI from faulting again.
            }

            _contextValues = ToolContext.GetContextValues();
        }

        /// <summary>
        /// Invokes the tool-scoped command that restores the dummy Search query tool to its default state.
        /// </summary>
        /// <returns>A task that completes when the command has been processed.</returns>
        protected async Task ResetQueryAsync()
        {
            try
            {
                // Resetting uses the same bounded command path so the demo covers both state changes and recomposition in one place.
                await ToolContext.InvokeCommandAsync(ResetSearchQueryCommandId);
            }
            catch (Exception)
            {
                // Recoverable command failures are already logged and surfaced by the shell manager, so the component prevents the UI from faulting again.
            }

            _contextValues = ToolContext.GetContextValues();
        }

        /// <summary>
        /// Publishes the Search query tool runtime menu and toolbar contributions.
        /// </summary>
        private void PublishRuntimeMenuAndToolbar()
        {
            // Runtime menu and toolbar contributions remain constant for this dummy tool, so they are published once when the context becomes available.
            ToolContext.SetRuntimeMenuContributions(
            [
                new MenuContribution("menu.runtime.search.query.apply", "Run sample query", RunSampleSearchQueryCommandId, icon: "play_circle", ownerToolId: SearchQueryToolId, order: 100),
                new MenuContribution("menu.runtime.search.query.reset", "Reset Search query", ResetSearchQueryCommandId, icon: "restart_alt", ownerToolId: SearchQueryToolId, order: 101)
            ]);

            ToolContext.SetRuntimeToolbarContributions(
            [
                new ToolbarContribution("toolbar.runtime.search.query.apply", "Run sample query", RunSampleSearchQueryCommandId, icon: "play_circle", ownerToolId: SearchQueryToolId, order: 100),
                new ToolbarContribution("toolbar.runtime.search.query.reset", "Reset Search query", ResetSearchQueryCommandId, icon: "restart_alt", ownerToolId: SearchQueryToolId, order: 101)
            ]);
        }
    }
}
