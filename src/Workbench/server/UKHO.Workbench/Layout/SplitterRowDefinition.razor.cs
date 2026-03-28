using Microsoft.AspNetCore.Components;

namespace UKHO.Workbench.Layout
{
    /// <summary>
    /// Registers a dedicated row splitter gutter with its parent <see cref="Grid"/>.
    /// </summary>
    public partial class SplitterRowDefinition : ComponentBase
    {
        /// <summary>
        /// Gets or sets the parent grid that owns this splitter definition.
        /// </summary>
        [CascadingParameter]
        private Grid? Parent { get; set; }

        /// <summary>
        /// Gets or sets the authored gutter height token.
        /// </summary>
        /// <remarks>
        /// When omitted, the grid applies the built-in default splitter thickness of <c>4px</c>.
        /// </remarks>
        [Parameter]
        public string? Height { get; set; }

        /// <summary>
        /// Registers the splitter gutter with the parent grid during component initialization.
        /// </summary>
        protected override void OnInitialized()
        {
            // Splitter definitions only make sense inside a Grid because the parent owns adjacent-track validation and rendering.
            if (Parent is null)
            {
                throw new ArgumentException("Parent not defined. Make sure your component is in a grid element");
            }

            // Registration is delegated to the parent so splitter ordering and validation rules stay consistent with normal rows.
            Parent.AddSplitterRow(Height);
            base.OnInitialized();
        }
    }
}
