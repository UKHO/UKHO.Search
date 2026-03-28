using Microsoft.AspNetCore.Components;
using UKHO.Workbench.Layout;

namespace WorkbenchHost.Components.Pages
{
    /// <summary>
    /// Demonstrates the Workbench row and column splitter slices inside the runnable host.
    /// </summary>
    public partial class LayoutSplitterShowcase : ComponentBase
    {
        private GridResizeNotification? _latestNotification;

        /// <summary>
        /// Gets the latest resize direction shown in the showcase diagnostics panel.
        /// </summary>
        private string LatestDirection => _latestNotification?.Direction.ToString() ?? "Awaiting interaction";

        /// <summary>
        /// Gets the latest splitter track index shown in the showcase diagnostics panel.
        /// </summary>
        private string LatestSplitterTrack => _latestNotification?.SplitterTrackIndex.ToString() ?? "-";

        /// <summary>
        /// Gets the latest previous track index shown in the showcase diagnostics panel.
        /// </summary>
        private string LatestPreviousTrack => _latestNotification?.PreviousTrackIndex.ToString() ?? "-";

        /// <summary>
        /// Gets the latest next track index shown in the showcase diagnostics panel.
        /// </summary>
        private string LatestNextTrack => _latestNotification?.NextTrackIndex.ToString() ?? "-";

        /// <summary>
        /// Gets the latest previous track width shown in the showcase diagnostics panel.
        /// </summary>
        private string LatestPreviousSize => _latestNotification is null ? "-" : $"{_latestNotification.PreviousTrackSizeInPixels:N0}px";

        /// <summary>
        /// Gets the latest next track width shown in the showcase diagnostics panel.
        /// </summary>
        private string LatestNextSize => _latestNotification is null ? "-" : $"{_latestNotification.NextTrackSizeInPixels:N0}px";

        /// <summary>
        /// Gets the latest resolved grid-template string shown in the showcase diagnostics panel.
        /// </summary>
        private string LatestTemplate => _latestNotification?.GridTemplate ?? "Interact with the splitter to populate this value.";

        /// <summary>
        /// Captures the latest grid resize notification emitted by the showcase grid.
        /// </summary>
        /// <param name="notification">The resize notification raised by the Workbench grid.</param>
        /// <returns>A completed task once the latest showcase state has been updated.</returns>
        private Task HandleGridResizeAsync(GridResizeNotification notification)
        {
            // The showcase simply echoes the latest notification so developers can verify the unified payload shape while dragging.
            _latestNotification = notification;
            return Task.CompletedTask;
        }
    }
}
