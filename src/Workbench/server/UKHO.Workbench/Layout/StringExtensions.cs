namespace UKHO.Workbench.Layout
{
    /// <summary>
	/// Provides small string helpers used by the grid size conversion pipeline.
	/// </summary>
	public static class StringExtensions
	{
        /// <summary>
		/// Determines whether the supplied string is null, empty, or whitespace.
		/// </summary>
		/// <param name="source">The string value to inspect.</param>
		/// <returns><c>true</c> when the value is null, empty, or whitespace; otherwise, <c>false</c>.</returns>
		public static bool IsEmpty(this string? source) => string.IsNullOrWhiteSpace(source);

		/// <summary>
		/// Determines whether the supplied string contains a non-whitespace value.
		/// </summary>
		/// <param name="source">The string value to inspect.</param>
		/// <returns><c>true</c> when the value contains a non-whitespace string; otherwise, <c>false</c>.</returns>
		public static bool IsDefined(this string? source) => !source.IsEmpty();
	}
}