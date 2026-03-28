namespace UKHO.Workbench.Layout
{
    /// <summary>
    /// Identifies whether a grid track represents user content space or a dedicated splitter gutter.
    /// </summary>
    internal enum GridTrackKind
    {
        /// <summary>
        /// Represents a normal row or column that can host layout content.
        /// </summary>
        Content,

        /// <summary>
        /// Represents a dedicated gutter track that exists only to host resize interaction.
        /// </summary>
        Splitter
    }
}
