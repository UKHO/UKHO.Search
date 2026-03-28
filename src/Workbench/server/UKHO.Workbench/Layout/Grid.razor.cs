using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace UKHO.Workbench.Layout
{
    /// <summary>
    /// Renders the Workbench WPF-style CSS grid surface and, when configured, wires in automatic row and column splitter behavior.
    /// </summary>
    public partial class Grid : ComponentBase, IAsyncDisposable
    {
        private readonly GridWrapper _wrapper = new GridWrapper();
        private DotNetObjectReference<Grid>? _dotNetObjectReference;
        private ElementReference _gridElement = default;
        private IJSObjectReference? _splitterModule;
        private bool _layoutReady;
        private bool _splitterInteropInitialized;
        private int _zIndex = 1;

        /// <summary>
        /// Gets or sets the JS runtime used to load the Workbench-owned splitter module.
        /// </summary>
        [Inject]
        private IJSRuntime JsRuntime { get; set; } = null!;

        /// <summary>
        /// Gets or sets the logger used to record validation and initialization failures.
        /// </summary>
        [Inject]
        private ILogger<Grid> Logger { get; set; } = null!;

        /// <summary>
        /// Gets or sets the column definition fragment that registers the grid's columns.
        /// </summary>
        [Parameter]
        public RenderFragment? ColumnDefinitions { get; set; }

        /// <summary>
        /// Gets or sets the row definition fragment that registers the grid's rows.
        /// </summary>
        [Parameter]
        public RenderFragment? RowDefinitions { get; set; }

        /// <summary>
        /// Gets or sets the content fragment rendered inside the configured grid tracks.
        /// </summary>
        [Parameter]
        public RenderFragment? Content { get; set; }

        /// <summary>
        /// Gets or sets the gap, in pixels, placed between column tracks in addition to any splitter gutter size.
        /// </summary>
        [Parameter]
        public double ColumnGap { get; set; }

        /// <summary>
        /// Gets or sets the gap, in pixels, placed between row tracks.
        /// </summary>
        [Parameter]
        public double RowGap { get; set; }

        /// <summary>
        /// Gets or sets the fixed width, in pixels, applied to the rendered grid container.
        /// </summary>
        [Parameter]
        public double? Width { get; set; }

        /// <summary>
        /// Gets or sets the fixed height, in pixels, applied to the rendered grid container.
        /// </summary>
        [Parameter]
        public double? Height { get; set; }

        /// <summary>
        /// Gets or sets the callback that receives unified, direction-aware resize notifications during splitter dragging.
        /// </summary>
        [Parameter]
        public EventCallback<GridResizeNotification> OnResize { get; set; }

        /// <summary>
        /// Gets the inline style string that combines the grid template, width, height, and configured gaps.
        /// </summary>
        private string GridStyle => $"{_wrapper.Css} {_wrapper.RowGap(RowGap)} {_wrapper.ColumnGap(ColumnGap)}".Trim();

        /// <summary>
        /// Registers a normal content column using the existing WPF-style authoring model.
        /// </summary>
        /// <param name="width">The authored width token for the column.</param>
        /// <param name="minimumWidth">The optional minimum width constraint for the column.</param>
        /// <param name="maximumWidth">The optional maximum width constraint for the column.</param>
        internal void AddColumn(string? width, string? minimumWidth, string? maximumWidth)
        {
            // Definitions are collected during child-component initialization and applied on the follow-up render once the full layout is known.
            _wrapper.AddColumn(width, minimumWidth, maximumWidth);
        }

        /// <summary>
        /// Registers a dedicated splitter column that occupies a reserved gutter track.
        /// </summary>
        /// <param name="width">The optional authored width token for the splitter gutter.</param>
        internal void AddSplitterColumn(string? width)
        {
            // Splitter tracks are treated as first-class columns so developer-facing indexing remains aligned with the authored markup order.
            _wrapper.AddSplitterColumn(width);
        }

        /// <summary>
        /// Registers a normal content row using the existing WPF-style authoring model.
        /// </summary>
        /// <param name="height">The authored height token for the row.</param>
        /// <param name="minimumHeight">The optional minimum height constraint for the row.</param>
        /// <param name="maximumHeight">The optional maximum height constraint for the row.</param>
        internal void AddRow(string? height, string? minimumHeight, string? maximumHeight)
        {
            // Rows remain part of the existing grid registration path and now participate in splitter-aware validation and rendering.
            _wrapper.AddRow(height, minimumHeight, maximumHeight);
        }

        /// <summary>
        /// Registers a dedicated splitter row that occupies a reserved gutter track.
        /// </summary>
        /// <param name="height">The optional authored height token for the splitter gutter.</param>
        internal void AddSplitterRow(string? height)
        {
            // Splitter tracks are treated as first-class rows so developer-facing indexing remains aligned with the authored markup order.
            _wrapper.AddSplitterRow(height);
        }

        /// <summary>
        /// Returns the next z-index value used to preserve authored stacking order for grid content.
        /// </summary>
        /// <returns>The next z-index value for a content element.</returns>
        internal int NextZIndex()
        {
            // Content order should stay deterministic even after splitter gutters are injected automatically by the grid.
            return _zIndex++;
        }

        /// <summary>
        /// Validates that a content element does not occupy any dedicated splitter gutter rows or columns.
        /// </summary>
        /// <param name="column">The 1-based starting column for the content element.</param>
        /// <param name="columnSpan">The authored ending grid line for the content element.</param>
        /// <param name="row">The 1-based starting row for the content element.</param>
        /// <param name="rowSpan">The authored ending grid line for the content element.</param>
        internal void ValidateContentPlacement(int column, int columnSpan, int row, int rowSpan)
        {
            try
            {
                // Guard the reserved splitter tracks so author content stays in real content rows and columns only.
                _wrapper.ValidateContentPlacement(column, columnSpan, row, rowSpan);
            }
            catch (Exception exception)
            {
                Logger.LogError(
                    exception,
                    "Grid content registration failed because the requested placement intersects a splitter gutter. Column: {Column}, ColumnSpan: {ColumnSpan}, Row: {Row}, RowSpan: {RowSpan}.",
                    column,
                    columnSpan,
                    row,
                    rowSpan);

                throw;
            }
        }

        /// <summary>
        /// Applies the latest width and height parameters to the underlying template generator.
        /// </summary>
        protected override void OnParametersSet()
        {
            // Parameter updates only need to refresh container sizing because track definitions are supplied by child components.
            _wrapper.SetWidth(Width);
            _wrapper.SetHeight(Height);
            base.OnParametersSet();
        }

        /// <summary>
        /// Performs the follow-up render that uses the collected definitions and initializes splitter interop once the final DOM exists.
        /// </summary>
        /// <param name="firstRender">Indicates whether this is the component's first completed render.</param>
        /// <returns>A task that completes when the render lifecycle work has finished.</returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            // The initial render collects child definitions; the second render applies the completed layout and can safely validate splitters.
            if (!_layoutReady)
            {
                _layoutReady = true;
                await InvokeAsync(StateHasChanged);
                return;
            }

            // Splitter assets only need to load once, and only for grids that actually declare splitter tracks.
            if (_splitterInteropInitialized)
            {
                return;
            }

            var columnSplitters = GetValidatedColumnSplitters();
            var rowSplitters = GetValidatedRowSplitters();
            if (columnSplitters.Count == 0 && rowSplitters.Count == 0)
            {
                return;
            }

            try
            {
                _splitterModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/UKHO.Workbench/workbench-grid-splitter.js");
                _dotNetObjectReference = DotNetObjectReference.Create(this);
                await _splitterModule.InvokeVoidAsync("initializeSplitters", _gridElement, _dotNetObjectReference);
                _splitterInteropInitialized = true;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Grid splitter initialization failed for a splitter-enabled Workbench grid.");
                throw;
            }
        }

        /// <summary>
        /// Receives continuous column resize updates from the Workbench-owned splitter JavaScript module.
        /// </summary>
        /// <param name="splitterTrackIndex">The 1-based splitter track index being dragged.</param>
        /// <param name="previousTrackIndex">The 1-based track index immediately before the splitter.</param>
        /// <param name="nextTrackIndex">The 1-based track index immediately after the splitter.</param>
        /// <param name="previousTrackSizeInPixels">The resolved pixel width of the track immediately before the splitter.</param>
        /// <param name="nextTrackSizeInPixels">The resolved pixel width of the track immediately after the splitter.</param>
        /// <param name="gridTemplate">The raw resolved <c>grid-template-columns</c> string after the latest drag update.</param>
        /// <returns>A task that completes when the notification has been forwarded to the consumer callback.</returns>
        [JSInvokable]
        public Task NotifyColumnResize(
            int splitterTrackIndex,
            int previousTrackIndex,
            int nextTrackIndex,
            double previousTrackSizeInPixels,
            double nextTrackSizeInPixels,
            string gridTemplate)
        {
            // The interop layer sends a unified payload shape so the public callback surface stays direction-aware from the first slice onward.
            var notification = new GridResizeNotification(
                GridResizeDirection.Column,
                splitterTrackIndex,
                previousTrackIndex,
                nextTrackIndex,
                previousTrackSizeInPixels,
                nextTrackSizeInPixels,
                gridTemplate);

            return InvokeAsync(() => OnResize.InvokeAsync(notification));
        }

        /// <summary>
        /// Receives continuous row resize updates from the Workbench-owned splitter JavaScript module.
        /// </summary>
        /// <param name="splitterTrackIndex">The 1-based splitter track index being dragged.</param>
        /// <param name="previousTrackIndex">The 1-based track index immediately before the splitter.</param>
        /// <param name="nextTrackIndex">The 1-based track index immediately after the splitter.</param>
        /// <param name="previousTrackSizeInPixels">The resolved pixel height of the track immediately before the splitter.</param>
        /// <param name="nextTrackSizeInPixels">The resolved pixel height of the track immediately after the splitter.</param>
        /// <param name="gridTemplate">The raw resolved <c>grid-template-rows</c> string after the latest drag update.</param>
        /// <returns>A task that completes when the notification has been forwarded to the consumer callback.</returns>
        [JSInvokable]
        public Task NotifyRowResize(
            int splitterTrackIndex,
            int previousTrackIndex,
            int nextTrackIndex,
            double previousTrackSizeInPixels,
            double nextTrackSizeInPixels,
            string gridTemplate)
        {
            // The interop layer sends a unified payload shape so the public callback surface stays direction-aware across both supported axes.
            var notification = new GridResizeNotification(
                GridResizeDirection.Row,
                splitterTrackIndex,
                previousTrackIndex,
                nextTrackIndex,
                previousTrackSizeInPixels,
                nextTrackSizeInPixels,
                gridTemplate);

            return InvokeAsync(() => OnResize.InvokeAsync(notification));
        }

        /// <summary>
        /// Disposes the JS resources used to power automatic splitter behavior.
        /// </summary>
        /// <returns>A task that completes when all owned async resources have been released.</returns>
        public async ValueTask DisposeAsync()
        {
            // Dispose in reverse order so event handlers are detached before module references are released.
            if (_splitterModule is not null)
            {
                try
                {
                    await _splitterModule.InvokeVoidAsync("disposeSplitters", _gridElement);
                    await _splitterModule.DisposeAsync();
                }
                catch (JSDisconnectedException)
                {
                    // Blazor Server can disconnect before disposal runs; that shutdown path is expected and does not need extra handling.
                }
            }

            _dotNetObjectReference?.Dispose();
        }

        /// <summary>
        /// Returns the validated column splitter tracks that should render gutter elements for the current grid.
        /// </summary>
        /// <returns>The validated column splitter tracks in authored order.</returns>
        private IReadOnlyList<GridTrackDefinition> GetValidatedColumnSplitters()
        {
            // Validation is centralized here so rendering and JS initialization share the same fail-fast rules and diagnostics.
            try
            {
                return _wrapper.GetValidatedColumnSplitters();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Grid splitter validation failed during layout generation.");
                throw;
            }
        }

        /// <summary>
        /// Returns the validated row splitter tracks that should render gutter elements for the current grid.
        /// </summary>
        /// <returns>The validated row splitter tracks in authored order.</returns>
        private IReadOnlyList<GridTrackDefinition> GetValidatedRowSplitters()
        {
            // Validation is centralized here so rendering and JS initialization share the same fail-fast rules and diagnostics.
            try
            {
                return _wrapper.GetValidatedRowSplitters();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Grid row splitter validation failed during layout generation.");
                throw;
            }
        }
    }
}
