namespace UKHO.Workbench.Layout
{
    /// <summary>
    /// Captures the resolved metadata for a single grid track so the grid can render templates, validate splitters, and protect reserved gutters.
    /// </summary>
    internal sealed class GridTrackDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridTrackDefinition"/> class.
        /// </summary>
        /// <param name="index">The 1-based track index that aligns with the developer-facing grid authoring surface.</param>
        /// <param name="kind">The logical kind of track being registered.</param>
        /// <param name="size">The declared size token supplied by the author.</param>
        /// <param name="minimumSize">The optional minimum size constraint declared for the track.</param>
        /// <param name="maximumSize">The optional maximum size constraint declared for the track.</param>
        internal GridTrackDefinition(int index, GridTrackKind kind, string? size, string? minimumSize, string? maximumSize)
        {
            // Normalize the raw size first so splitter tracks can apply their built-in fallback thickness before CSS conversion runs.
            Index = index;
            Kind = kind;
            DeclaredSize = NormalizeDeclaredSize(kind, size);
            MinimumSize = minimumSize;
            MaximumSize = maximumSize;
            CssSize = GridTemplateConverter.ConvertTrackSize(DeclaredSize, minimumSize, maximumSize);
        }

        /// <summary>
        /// Gets the 1-based index of the track within its row or column collection.
        /// </summary>
        internal int Index { get; }

        /// <summary>
        /// Gets the logical track kind used when validating and rendering the grid.
        /// </summary>
        internal GridTrackKind Kind { get; }

        /// <summary>
        /// Gets the original normalized size token supplied for the track.
        /// </summary>
        internal string DeclaredSize { get; }

        /// <summary>
        /// Gets the optional minimum size token declared for the track.
        /// </summary>
        internal string? MinimumSize { get; }

        /// <summary>
        /// Gets the optional maximum size token declared for the track.
        /// </summary>
        internal string? MaximumSize { get; }

        /// <summary>
        /// Gets the CSS Grid-compatible track size emitted into the rendered template.
        /// </summary>
        internal string CssSize { get; }

        /// <summary>
        /// Gets a value indicating whether the track is reserved for splitter interaction rather than content.
        /// </summary>
        internal bool IsSplitter => Kind == GridTrackKind.Splitter;

        /// <summary>
        /// Gets a value indicating whether the track is declared as <c>Auto</c> and is therefore not eligible for splitter resizing.
        /// </summary>
        internal bool IsAutoSized => !IsSplitter && GridTemplateConverter.IsAuto(DeclaredSize);

        /// <summary>
        /// Gets a value indicating whether the track can participate in splitter resizing.
        /// </summary>
        internal bool IsResizable => !IsSplitter && !IsAutoSized;

        /// <summary>
        /// Applies the built-in splitter default when the author omits an explicit gutter width or height.
        /// </summary>
        /// <param name="kind">The logical kind of track being registered.</param>
        /// <param name="size">The raw size token supplied by the author.</param>
        /// <returns>The normalized size token that should be stored for the track.</returns>
        private static string NormalizeDeclaredSize(GridTrackKind kind, string? size)
        {
            // Splitter tracks must stay a fixed gutter, so an omitted width or height becomes the documented 4px default.
            if (kind == GridTrackKind.Splitter && string.IsNullOrWhiteSpace(size))
            {
                return "4";
            }

            // Content tracks keep the original behavior where an omitted size means the default star track.
            return size ?? string.Empty;
        }
    }
}
