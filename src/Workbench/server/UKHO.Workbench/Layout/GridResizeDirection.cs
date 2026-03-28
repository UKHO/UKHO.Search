namespace UKHO.Workbench.Layout
{
    /// <summary>
    /// Identifies the axis affected by a grid resize notification.
    /// </summary>
    public enum GridResizeDirection
    {
        /// <summary>
        /// Indicates that the resize interaction changed column widths.
        /// </summary>
        Column,

        /// <summary>
        /// Indicates that the resize interaction changed row heights.
        /// </summary>
        Row
    }
}
