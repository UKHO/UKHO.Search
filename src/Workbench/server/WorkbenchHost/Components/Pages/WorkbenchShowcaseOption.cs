using System;

namespace WorkbenchHost.Components.Pages
{
    /// <summary>
    /// Represents a small labeled option used by the temporary Workbench Radzen showcase controls.
    /// </summary>
    internal sealed class WorkbenchShowcaseOption
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkbenchShowcaseOption"/> class.
        /// </summary>
        /// <param name="label">The human-readable text shown to reviewers in the mock-up UI.</param>
        /// <param name="value">The stable in-memory value bound by the sample control.</param>
        /// <param name="description">The short supporting description used by the visual-only showcase.</param>
        public WorkbenchShowcaseOption(string label, string value, string description)
        {
            // Capture the fixed option values once so the showcase controls can bind to predictable sample data.
            Label = label;
            Value = value;
            Description = description;
        }

        /// <summary>
        /// Gets the human-readable text shown in the associated control.
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// Gets the stable in-memory value submitted by the associated control.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets a short descriptive note that helps differentiate similar sample options.
        /// </summary>
        public string Description { get; }
    }
}
