using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UKHO.Workbench.Layout
{
    /// <summary>
	/// Converts WPF-style grid size tokens into CSS Grid-compatible template values.
	/// </summary>
	public class GridTemplateConverter : IEnumerable<string>
	{
		private static readonly Regex FixedSizePattern = new Regex("^[0-9]*$", RegexOptions.Compiled);
		private static readonly Regex ProportionPattern = new Regex("^[0-9]*\\*$", RegexOptions.Compiled);
		private readonly List<string> _convertedData = new List<string>();

		/// <summary>
		/// Converts and stores a new template segment.
		/// </summary>
		/// <param name="data">The authored size token to convert.</param>
		/// <param name="min">The optional minimum size token.</param>
		/// <param name="max">The optional maximum size token.</param>
		public void AddData(string? data, string? min = null, string? max = null)
		{
			// Instance storage is still used by legacy tests, while the static conversion helpers support the new splitter metadata path.
			_convertedData.Add(ConvertTrackSize(data, min, max));
		}

		/// <summary>
		/// Converts a single track definition into the CSS fragment used in a template.
		/// </summary>
		/// <param name="data">The authored size token to convert.</param>
		/// <param name="min">The optional minimum size token.</param>
		/// <param name="max">The optional maximum size token.</param>
		/// <returns>The CSS fragment for the supplied track definition.</returns>
		internal static string ConvertTrackSize(string? data, string? min = null, string? max = null)
		{
			// Min/max constraints are expressed through CSS minmax() exactly as the original wrapper implementation did.
			if (min.IsDefined() || max.IsDefined())
			{
				return $"minmax({ConvertToCss(min.IsDefined() ? min : "1")},{ConvertToCss(max.IsDefined() ? max : "*")})";
			}

			return ConvertToCss(data);
		}

		/// <summary>
		/// Determines whether an authored size token represents an Auto-sized track.
		/// </summary>
		/// <param name="data">The authored size token to inspect.</param>
		/// <returns><c>true</c> when the token represents <c>Auto</c>; otherwise, <c>false</c>.</returns>
		internal static bool IsAuto(string? data)
		{
			// Splitter validation needs access to the authored token rather than the converted CSS representation.
			return string.Equals(data, "auto", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Returns an enumerator over the converted template segments.
		/// </summary>
		/// <returns>An enumerator over the converted template segments.</returns>
		public IEnumerator<string> GetEnumerator()
		{
			// Enumeration support keeps the legacy wrapper tests and usage patterns intact.
			return _convertedData.GetEnumerator();
		}

		/// <summary>
		/// Returns a non-generic enumerator over the converted template segments.
		/// </summary>
		/// <returns>A non-generic enumerator over the converted template segments.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			// Delegate to the generic enumerator so both interfaces stay consistent.
			return GetEnumerator();
		}

		/// <summary>
		/// Converts a single authored token into the CSS Grid value expected by the browser.
		/// </summary>
		/// <param name="data">The authored size token to convert.</param>
		/// <returns>The CSS Grid-compatible value.</returns>
		private static string ConvertToCss(string? data)
		{
			// The converter preserves the original shorthand semantics where omitted sizes default to a single star track.
			if (data.IsEmpty())
			{
				return "1fr";
			}

			if (data == "*")
			{
				return "1fr";
			}

			if (IsAuto(data))
			{
				return "auto";
			}

			if (FixedSizePattern.IsMatch(data!))
			{
				return data + "px";
			}

			if (ProportionPattern.IsMatch(data!))
			{
				return data.Replace("*", "fr");
			}

			throw new GridLayoutException(data!);
		}
	}
}