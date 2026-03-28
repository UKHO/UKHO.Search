using Microsoft.AspNetCore.Components;

namespace UKHO.Workbench.Layout
{
    /// <summary>
    /// Hosts authored content within the parent grid at a specific row and column location.
    /// </summary>
    public partial class GridElement : ComponentBase
    {
        /// <summary>
        /// Gets or sets the parent grid that owns this content element.
        /// </summary>
        [CascadingParameter]
        private Grid? Parent { get; set; }

        /// <summary>
        /// Gets or sets the 1-based starting column for the content element.
        /// </summary>
        [Parameter]
        public int Column { get; set; } = 1;

        /// <summary>
        /// Gets or sets the 1-based starting row for the content element.
        /// </summary>
        [Parameter]
        public int Row { get; set; } = 1;

        /// <summary>
        /// Gets or sets the ending grid row line used when rendering the element.
        /// </summary>
        [Parameter]
        public int RowSpan { get; set; }

        /// <summary>
        /// Gets or sets the ending grid column line used when rendering the element.
        /// </summary>
        [Parameter]
        public int ColumnSpan { get; set; }

        /// <summary>
        /// Gets or sets the horizontal alignment applied to the hosted content.
        /// </summary>
        [Parameter]
        public CssAlignment HorizontalAlignment { get; set; } = Alignment.Stretch;

        /// <summary>
        /// Gets or sets the vertical alignment applied to the hosted content.
        /// </summary>
        [Parameter]
        public CssAlignment VerticalAlignment { get; set; } = Alignment.Stretch;

        /// <summary>
        /// Gets or sets the child content rendered inside the element host.
        /// </summary>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Gets the effective ending row line used by the renderer.
        /// </summary>
        private int ResolvedRowSpan => RowSpan > 0 ? RowSpan : Row + 1;

        /// <summary>
        /// Gets the effective ending column line used by the renderer.
        /// </summary>
        private int ResolvedColumnSpan => ColumnSpan > 0 ? ColumnSpan : Column + 1;

        /// <summary>
        /// Validates the requested placement against the parent grid configuration.
        /// </summary>
        protected override void OnInitialized()
        {
            // Content placement only makes sense inside a Grid because the parent owns the track definitions and stacking order.
            if (Parent is null)
            {
                throw new ArgumentException("Parent not defined. Make sure your component is in a grid element");
            }

            // Splitter tracks are reserved gutters, so content must fail fast if it is authored into one of those rows or columns.
            Parent.ValidateContentPlacement(Column, ResolvedColumnSpan, Row, ResolvedRowSpan);
            base.OnInitialized();
        }
    }
}
