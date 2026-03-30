using Microsoft.AspNetCore.Components;
using UKHO.Workbench.Tools;
using UKHO.Workbench.WorkbenchShell;

namespace UKHO.Workbench.Modules.Search.Tools
{
    /// <summary>
    /// Renders the dummy Search ingestion tool used to prove the Search module can contribute multiple singleton tools.
    /// </summary>
    public partial class SearchIngestionTool
    {
        private const string SearchIngestionToolId = "tool.module.search.ingestion";
        private bool _runtimeShellStateInitialized;
        private IReadOnlyDictionary<string, string> _contextValues = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets the bounded Workbench tool context for the active Search ingestion tool instance.
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
            // The component publishes lightweight title and status metadata when its bounded tool context becomes available.
            ArgumentNullException.ThrowIfNull(ToolContext);

            _contextValues = ToolContext.GetContextValues();

            if (_runtimeShellStateInitialized)
            {
                return;
            }

            ToolContext.SetTitle("Search ingestion");
            ToolContext.SetIcon("publish");
            ToolContext.SetBadge(null);
            ToolContext.SetSelection(null, 0);
            ToolContext.SetRuntimeStatusBarContributions(
            [
                new StatusBarContribution("status.runtime.search.ingestion.ready", "Search ingestion ready", ownerToolId: SearchIngestionToolId, order: 200)
            ]);

            _runtimeShellStateInitialized = true;
        }
    }
}
