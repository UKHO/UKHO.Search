using Microsoft.AspNetCore.Components;
using UKHO.Workbench.Services.Shell;
using UKHO.Workbench.Tools;

namespace WorkbenchHost.Components.Pages
{
    /// <summary>
    /// Hosts the currently active Workbench tool inside the shell's central working region.
    /// </summary>
    public partial class Index : IDisposable
    {
        private static readonly IDictionary<string, object> EmptyParameters = new Dictionary<string, object>();

        [Inject]
        private WorkbenchShellManager ShellManager { get; set; } = null!;

        /// <summary>
        /// Gets the active runtime tool instance currently hosted by the shell.
        /// </summary>
        private ToolInstance? ActiveTool => ShellManager.State.ActiveTool;

        /// <summary>
        /// Gets the component type that should be rendered for the active tool.
        /// </summary>
        private Type? ActiveToolComponentType => ActiveTool?.Definition.ComponentType;

        /// <summary>
        /// Gets the dynamic component parameters that should be supplied to the active tool component.
        /// </summary>
        private IDictionary<string, object> ActiveToolParameters
        {
            get
            {
                // Only components that explicitly declare a ToolContext parameter receive it so host-owned tools without that contract continue to render safely.
                if (ActiveToolComponentType is null || ActiveTool is null)
                {
                    return EmptyParameters;
                }

                var toolContextParameter = ActiveToolComponentType.GetProperty("ToolContext");
                if (toolContextParameter is null || toolContextParameter.PropertyType != typeof(ToolContext))
                {
                    return EmptyParameters;
                }

                return new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["ToolContext"] = ActiveTool.Context
                };
            }
        }

        /// <summary>
        /// Subscribes to shell state changes so the hosted tool surface refreshes when focus moves.
        /// </summary>
        protected override void OnInitialized()
        {
            // The hosted tool surface reacts to shell-state changes so explorer activation immediately updates the center region.
            ShellManager.StateChanged += HandleShellStateChanged;
            base.OnInitialized();
        }

        /// <summary>
        /// Responds to shell state changes by scheduling a component re-render.
        /// </summary>
        /// <param name="sender">The object that raised the state change event.</param>
        /// <param name="e">The event arguments for the notification.</param>
        private void HandleShellStateChanged(object? sender, EventArgs e)
        {
            // State updates may be triggered outside the current render cycle, so the page marshals the refresh back onto the renderer.
            _ = InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Unsubscribes from shell state notifications when the page is disposed.
        /// </summary>
        public void Dispose()
        {
            // The page releases its subscription so stale component instances are not retained after navigation or reconnection.
            ShellManager.StateChanged -= HandleShellStateChanged;
        }
    }
}