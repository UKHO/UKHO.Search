using Microsoft.AspNetCore.Components;
using UKHO.Workbench.Output;

namespace WorkbenchHost.Components.Layout
{
    /// <summary>
    /// Renders one compact structured output row for the Workbench shell output surface.
    /// </summary>
    public partial class WorkbenchOutputRow : ComponentBase
    {
        private static readonly string[] DetailLineSeparators = ["\r\n", "\n", "\r"];
        private IReadOnlyList<string> _detailLines = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the immutable output entry that should be rendered by the row.
        /// </summary>
        [Parameter]
        [EditorRequired]
        public OutputEntry Entry { get; set; } = null!;

        /// <summary>
        /// Gets or sets a value indicating whether the current row is expanded.
        /// </summary>
        [Parameter]
        public bool IsExpanded { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the shell-wide output panel wrap mode is enabled.
        /// </summary>
        [Parameter]
        public bool IsWordWrapEnabled { get; set; }

        /// <summary>
        /// Gets or sets the callback raised when the disclosure button is activated for this row.
        /// </summary>
        [Parameter]
        public EventCallback<OutputEntry> ExpansionToggled { get; set; }

        /// <summary>
        /// Gets a value indicating whether the current entry exposes an expandable inline detail region.
        /// </summary>
        private bool HasExpandableContent => _detailLines.Count > 0 || !string.IsNullOrWhiteSpace(Entry.EventCode);

        /// <summary>
        /// Gets the compact disclosure glyph that matches the current expansion state.
        /// </summary>
        private string DisclosureGlyph => IsExpanded ? "▾" : "▸";

        /// <summary>
        /// Gets the accessible label announced for the disclosure control.
        /// </summary>
        private string DisclosureAriaLabel => IsExpanded
            ? $"Collapse output entry from {Entry.Source}"
            : $"Expand output entry from {Entry.Source}";

        /// <summary>
        /// Gets the tooltip text used to keep the chrome-less disclosure discoverable for pointer users.
        /// </summary>
        private string DisclosureTooltip => IsExpanded
            ? $"Collapse inline details for {Entry.Source}"
            : $"Expand inline details for {Entry.Source}";

        /// <summary>
        /// Gets the compact local-time timestamp rendered in the collapsed row summary.
        /// </summary>
        private string FormattedTimestamp => Entry.TimestampUtc.ToLocalTime().ToString("HH:mm:ss");

        /// <summary>
        /// Gets the detail lines projected from the optional expanded-detail payload.
        /// </summary>
        private IReadOnlyList<string> DetailLines => _detailLines;

        /// <summary>
        /// Gets the stable DOM identifier used to associate the disclosure button with the details region.
        /// </summary>
        private string DetailsRegionId => $"output-entry-details-{Entry.Id}";

        /// <summary>
        /// Gets the tooltip text used for the subtle visual severity marker.
        /// </summary>
        private string LevelMarkerTooltip => $"{Entry.Level} output";

        /// <summary>
        /// Validates the required row parameters before the component renders.
        /// </summary>
        protected override void OnParametersSet()
        {
            // The row cannot render useful shell output without an immutable entry payload.
            ArgumentNullException.ThrowIfNull(Entry);

            // The detail payload is normalized once per parameter set so the markup can render inline lines without repeating newline parsing during the same render.
            _detailLines = BuildDetailLines(Entry.Details);

            base.OnParametersSet();
        }

        /// <summary>
        /// Normalizes the optional detail payload into inline-renderable text lines.
        /// </summary>
        /// <param name="details">The optional multi-line detail payload supplied by the output entry.</param>
        /// <returns>The ordered detail lines that should render beneath the main output line.</returns>
        private static IReadOnlyList<string> BuildDetailLines(string? details)
        {
            // Empty or whitespace-only detail payloads should not create empty inline rows or a redundant fold affordance.
            if (string.IsNullOrWhiteSpace(details))
            {
                return Array.Empty<string>();
            }

            // All supported newline representations collapse onto the same rendering path so the UI behaves consistently regardless of the source platform.
            return details.Split(DetailLineSeparators, StringSplitOptions.None);
        }

        /// <summary>
        /// Raises the row-specific expansion callback for the disclosure control.
        /// </summary>
        /// <returns>A task that completes when the parent layout has processed the expansion request.</returns>
        private Task ToggleExpansionAsync()
        {
            // Flat rows do not surface a disclosure control, but the guard keeps the callback path safe if the method is invoked unexpectedly.
            if (!HasExpandableContent)
            {
                return Task.CompletedTask;
            }

            // Expansion remains parent-owned so the shared output panel state continues to track all expanded row identifiers centrally.
            return ExpansionToggled.InvokeAsync(Entry);
        }

    }
}
