using Microsoft.AspNetCore.Components;

namespace UKHO.Workbench.Layout
{
    /// <summary>
    /// Registers a standard content row with its parent <see cref="Grid"/>.
    /// </summary>
    public partial class RowDefinition : ComponentBase
    {
        /// <summary>
        /// Gets or sets the parent grid that owns this row definition.
        /// </summary>
        [CascadingParameter]
        private Grid? Parent { get; set; }

        /// <summary>
        /// Gets or sets the authored height token for the row.
        /// </summary>
        [Parameter]
        public string? Height { get; set; } = "*";

        /// <summary>
        /// Gets or sets the minimum height constraint for the row.
        /// </summary>
        [Parameter]
        public string? MinHeight { get; set; }

        /// <summary>
        /// Gets or sets the maximum height constraint for the row.
        /// </summary>
        [Parameter]
        public string? MaxHeight { get; set; }

        /// <summary>
        /// Registers the row with the parent grid during component initialization.
        /// </summary>
        protected override void OnInitialized()
        {
            // Row definitions only make sense inside a Grid because the parent owns the ordered track collection.
            if (Parent is null)
            {
                throw new ArgumentException("Parent not defined. Make sure your component is in a grid element");
            }

            // Registration is delegated to the parent so all row ordering and sizing behavior stays centralized.
            Parent.AddRow(Height, MinHeight, MaxHeight);
            base.OnInitialized();
        }
    }
}
