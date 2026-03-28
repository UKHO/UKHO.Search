using Microsoft.AspNetCore.Components;

namespace UKHO.Workbench.Layout
{
    /// <summary>
    /// Registers a standard content column with its parent <see cref="Grid"/>.
    /// </summary>
    public partial class ColumnDefinition : ComponentBase
    {
        /// <summary>
        /// Gets or sets the parent grid that owns this column definition.
        /// </summary>
        [CascadingParameter]
        private Grid? Parent { get; set; }

        /// <summary>
        /// Gets or sets the authored width token for the column.
        /// </summary>
        [Parameter]
        public string? Width { get; set; } = "*";

        /// <summary>
        /// Gets or sets the minimum width constraint for the column.
        /// </summary>
        [Parameter]
        public string? MinWidth { get; set; }

        /// <summary>
        /// Gets or sets the maximum width constraint for the column.
        /// </summary>
        [Parameter]
        public string? MaxWidth { get; set; }

        /// <summary>
        /// Registers the column with the parent grid during component initialization.
        /// </summary>
        protected override void OnInitialized()
        {
            // Column definitions only make sense inside a Grid because the parent owns the ordered track collection.
            if (Parent is null)
            {
                throw new ArgumentException("Parent not defined. Make sure your component is in a grid element");
            }

            // Registration is delegated to the parent so all column ordering and validation stays centralized.
            Parent.AddColumn(Width, MinWidth, MaxWidth);
            base.OnInitialized();
        }
    }
}
