using System;

namespace WorkbenchHost.Components.Pages
{
    /// <summary>
    /// Represents one static row rendered in the temporary Workbench Radzen data-grid showcase.
    /// </summary>
    internal sealed class WorkbenchShowcaseGridRow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkbenchShowcaseGridRow"/> class.
        /// </summary>
        /// <param name="title">The short item title displayed in the sample grid.</param>
        /// <param name="category">The generic grouping used to show categorical values in the grid.</param>
        /// <param name="owner">The placeholder owner or reviewer shown for the row.</param>
        /// <param name="status">The visual-only status label shown in the grid badge.</param>
        /// <param name="confidence">The illustrative confidence percentage shown in the grid.</param>
        /// <param name="lastReviewedOn">The static review timestamp used to compare date formatting between themes.</param>
        public WorkbenchShowcaseGridRow(string title, string category, string owner, string status, int confidence, DateTime lastReviewedOn)
        {
            // Capture the fixed row values once so the grid can remain entirely in-memory and deterministic.
            Title = title;
            Category = category;
            Owner = owner;
            Status = status;
            Confidence = confidence;
            LastReviewedOn = lastReviewedOn;
        }

        /// <summary>
        /// Gets the short item title displayed in the sample grid.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets the generic grouping rendered to demonstrate categorical grid columns.
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// Gets the placeholder owner or reviewer name shown in the grid.
        /// </summary>
        public string Owner { get; }

        /// <summary>
        /// Gets the visual-only status label used to style the mock badge.
        /// </summary>
        public string Status { get; }

        /// <summary>
        /// Gets the illustrative confidence percentage shown in the sample grid.
        /// </summary>
        public int Confidence { get; }

        /// <summary>
        /// Gets the static review timestamp used to demonstrate date rendering.
        /// </summary>
        public DateTime LastReviewedOn { get; }
    }
}
