using System;

namespace WorkbenchHost.Components.Pages
{
    /// <summary>
    /// Stores the temporary in-memory values bound to the upper-middle Radzen form showcase.
    /// </summary>
    internal sealed class WorkbenchShowcaseFormModel
    {
        /// <summary>
        /// Gets or sets the mock workspace title shown in the text input sample.
        /// </summary>
        public string WorkspaceTitle { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the longer descriptive text used by the multiline input sample.
        /// </summary>
        public string ReviewNotes { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the illustrative numeric limit used by the sample numeric input.
        /// </summary>
        public int ResultLimit { get; set; }

        /// <summary>
        /// Gets or sets the target review date shown by the date picker sample.
        /// </summary>
        public DateTime? ReviewDate { get; set; }

        /// <summary>
        /// Gets or sets the selected sample dataset value used by the drop-down showcase.
        /// </summary>
        public string Dataset { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the selected density option bound to the radio-button list showcase.
        /// </summary>
        public string LayoutDensity { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the sample should visually imply pinned navigation.
        /// </summary>
        public bool PinOverview { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the sample should visually imply automatic refresh.
        /// </summary>
        public bool AutoRefresh { get; set; }

        /// <summary>
        /// Gets or sets the base64-backed file payload produced by the sample file input when a local file is selected.
        /// </summary>
        public string? ReferenceFileContent { get; set; }
    }
}
