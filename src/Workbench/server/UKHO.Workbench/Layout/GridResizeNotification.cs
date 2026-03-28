namespace UKHO.Workbench.Layout
{
    /// <summary>
    /// Describes a single splitter-driven resize update emitted by the grid.
    /// </summary>
    public sealed class GridResizeNotification
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridResizeNotification"/> class.
        /// </summary>
        /// <param name="direction">The axis affected by the resize interaction.</param>
        /// <param name="splitterTrackIndex">The 1-based index of the splitter track that is being dragged.</param>
        /// <param name="previousTrackIndex">The 1-based index of the resizable track immediately before the splitter.</param>
        /// <param name="nextTrackIndex">The 1-based index of the resizable track immediately after the splitter.</param>
        /// <param name="previousTrackSizeInPixels">The resolved pixel size of the track immediately before the splitter after the latest drag update.</param>
        /// <param name="nextTrackSizeInPixels">The resolved pixel size of the track immediately after the splitter after the latest drag update.</param>
        /// <param name="gridTemplate">The raw resolved CSS grid-template string for the affected direction.</param>
        public GridResizeNotification(
            GridResizeDirection direction,
            int splitterTrackIndex,
            int previousTrackIndex,
            int nextTrackIndex,
            double previousTrackSizeInPixels,
            double nextTrackSizeInPixels,
            string gridTemplate)
        {
            // Preserve the latest drag state exactly as it crossed the JS interop boundary so handlers can react without re-querying the DOM.
            Direction = direction;
            SplitterTrackIndex = splitterTrackIndex;
            PreviousTrackIndex = previousTrackIndex;
            NextTrackIndex = nextTrackIndex;
            PreviousTrackSizeInPixels = previousTrackSizeInPixels;
            NextTrackSizeInPixels = nextTrackSizeInPixels;
            GridTemplate = gridTemplate;
        }

        /// <summary>
        /// Gets the axis affected by the resize interaction.
        /// </summary>
        public GridResizeDirection Direction { get; }

        /// <summary>
        /// Gets the 1-based index of the splitter track that raised the notification.
        /// </summary>
        public int SplitterTrackIndex { get; }

        /// <summary>
        /// Gets the 1-based index of the track immediately before the splitter.
        /// </summary>
        public int PreviousTrackIndex { get; }

        /// <summary>
        /// Gets the 1-based index of the track immediately after the splitter.
        /// </summary>
        public int NextTrackIndex { get; }

        /// <summary>
        /// Gets the resolved pixel size of the track immediately before the splitter.
        /// </summary>
        public double PreviousTrackSizeInPixels { get; }

        /// <summary>
        /// Gets the resolved pixel size of the track immediately after the splitter.
        /// </summary>
        public double NextTrackSizeInPixels { get; }

        /// <summary>
        /// Gets the raw resolved CSS grid-template string for the affected direction.
        /// </summary>
        public string GridTemplate { get; }
    }
}
