using System.Globalization;

namespace UKHO.Workbench.Layout
{
	/// <summary>
	/// Builds the CSS Grid template metadata used by the Workbench layout components.
	/// </summary>
	public class GridWrapper
	{
		private readonly List<GridTrackDefinition> _columns = new List<GridTrackDefinition>();
		private readonly CultureInfo _enCulture = new CultureInfo("en-GB");
		private readonly List<GridTrackDefinition> _rows = new List<GridTrackDefinition>();
		private double? _height;
		private double? _width;

		/// <summary>
		/// Gets the inline CSS used to render the configured grid.
		/// </summary>
		public string Css => $"display: grid; {Width()}{Height()}{GenerateTemplateColumnsIfAny()}{GenerateTemplateRowsIfAny()}".TrimEnd();

		/// <summary>
		/// Gets a value indicating whether the grid contains any dedicated splitter columns.
		/// </summary>
		internal bool HasColumnSplitters => _columns.Any(column => column.IsSplitter);

		/// <summary>
		/// Gets a value indicating whether the grid contains any dedicated splitter rows.
		/// </summary>
		internal bool HasRowSplitters => _rows.Any(row => row.IsSplitter);

		/// <summary>
		/// Registers a normal content column in authored order.
		/// </summary>
		/// <param name="width">The authored width token for the column.</param>
		/// <param name="minimumWidth">The optional minimum width constraint for the column.</param>
		/// <param name="maximumWidth">The optional maximum width constraint for the column.</param>
		public void AddColumn(string? width, string? minimumWidth = null, string? maximumWidth = null)
		{
			// Columns are stored as metadata so the grid can later validate splitter placement without losing the original authored ordering.
			_columns.Add(new GridTrackDefinition(_columns.Count + 1, GridTrackKind.Content, width, minimumWidth, maximumWidth));
		}

		/// <summary>
		/// Registers a dedicated splitter gutter column in authored order.
		/// </summary>
		/// <param name="width">The optional authored width token for the splitter column.</param>
		public void AddSplitterColumn(string? width)
		{
			// Splitter gutters are represented as first-class tracks so indexing remains aligned with the developer-facing layout model.
			_columns.Add(new GridTrackDefinition(_columns.Count + 1, GridTrackKind.Splitter, width, null, null));
		}

		/// <summary>
		/// Registers a normal content row in authored order.
		/// </summary>
		/// <param name="height">The authored height token for the row.</param>
		/// <param name="minimumHeight">The optional minimum height constraint for the row.</param>
		/// <param name="maximumHeight">The optional maximum height constraint for the row.</param>
		public void AddRow(string? height, string? minimumHeight = null, string? maximumHeight = null)
		{
			// Rows continue to follow the original wrapper model while still benefiting from the shared track metadata representation.
			_rows.Add(new GridTrackDefinition(_rows.Count + 1, GridTrackKind.Content, height, minimumHeight, maximumHeight));
		}

		/// <summary>
		/// Registers a dedicated splitter gutter row in authored order.
		/// </summary>
		/// <param name="height">The optional authored height token for the splitter row.</param>
		public void AddSplitterRow(string? height)
		{
			// Splitter gutters are represented as first-class tracks so indexing remains aligned with the developer-facing layout model.
			_rows.Add(new GridTrackDefinition(_rows.Count + 1, GridTrackKind.Splitter, height, null, null));
		}

		/// <summary>
		/// Returns the validated splitter columns that should render gutter elements.
		/// </summary>
		/// <returns>The validated splitter column definitions in authored order.</returns>
		internal IReadOnlyList<GridTrackDefinition> GetValidatedColumnSplitters()
		{
			// Validate every authored splitter before exposing it so rendering and JS initialization share the same fail-fast rules.
			return GetValidatedSplitters(
               _columns,
				ValidateSplitterColumn);
		}

		/// <summary>
		/// Returns the validated splitter rows that should render gutter elements.
		/// </summary>
		/// <returns>The validated splitter row definitions in authored order.</returns>
		internal IReadOnlyList<GridTrackDefinition> GetValidatedRowSplitters()
		{
			// Validate every authored splitter before exposing it so rendering and JS initialization share the same fail-fast rules.
			return GetValidatedSplitters(
                _rows,
				ValidateSplitterRow);
		}

		/// <summary>
		/// Stores the fixed width, in pixels, used when rendering the grid container.
		/// </summary>
		/// <param name="value">The optional fixed grid width.</param>
		public void SetWidth(double? value)
		{
			// Width is stored separately from track metadata because it affects the grid container rather than an individual track.
			_width = value;
		}

		/// <summary>
		/// Stores the fixed height, in pixels, used when rendering the grid container.
		/// </summary>
		/// <param name="value">The optional fixed grid height.</param>
		public void SetHeight(double? value)
		{
			// Height is stored separately from track metadata because it affects the grid container rather than an individual track.
			_height = value;
		}

		/// <summary>
		/// Returns the CSS needed to apply a row gap when one has been configured.
		/// </summary>
		/// <param name="gap">The configured row gap in pixels.</param>
		/// <returns>The CSS fragment for the row gap, or an empty string when no gap is configured.</returns>
		public string RowGap(double gap)
		{
			// Gaps are emitted separately so they remain additive to splitter track size rather than replacing it.
			return gap > 0 ? $"grid-row-gap: {gap.ToString(_enCulture)}px;" : string.Empty;
		}

		/// <summary>
		/// Returns the CSS needed to apply a column gap when one has been configured.
		/// </summary>
		/// <param name="gap">The configured column gap in pixels.</param>
		/// <returns>The CSS fragment for the column gap, or an empty string when no gap is configured.</returns>
		public string ColumnGap(double gap)
		{
			// Gaps are emitted separately so they remain additive to splitter track size rather than replacing it.
			return gap > 0 ? $"grid-column-gap: {gap.ToString(_enCulture)}px;" : string.Empty;
		}

		/// <summary>
		/// Validates that a content element does not occupy any reserved splitter gutter rows or columns.
		/// </summary>
		/// <param name="column">The 1-based starting column for the content element.</param>
		/// <param name="columnSpan">The ending grid column line used by the content element.</param>
		/// <param name="row">The 1-based starting row for the content element.</param>
		/// <param name="rowSpan">The ending grid row line used by the content element.</param>
		internal void ValidateContentPlacement(int column, int columnSpan, int row, int rowSpan)
		{
			// GridElement uses start/end line semantics, so the occupied tracks are the authored rows and columns before the ending lines.
			ValidateReservedTrackPlacement(_columns, column, columnSpan, "Column");
			ValidateReservedTrackPlacement(_rows, row, rowSpan, "Row");
		}

		/// <summary>
		/// Generates the column template fragment when any columns have been registered.
		/// </summary>
		/// <returns>The CSS fragment for the column template, or an empty string when no columns exist.</returns>
		private string GenerateTemplateColumnsIfAny()
		{
			// The authored track order is preserved exactly so splitter columns participate in template generation like normal columns.
			return _columns.Any()
				? $"grid-template-columns: {string.Join(" ", _columns.Select(column => column.CssSize))}; "
				: string.Empty;
		}

		/// <summary>
		/// Generates the row template fragment when any rows have been registered.
		/// </summary>
		/// <returns>The CSS fragment for the row template, or an empty string when no rows exist.</returns>
		private string GenerateTemplateRowsIfAny()
		{
			// The authored track order is preserved exactly so splitter rows participate in template generation like normal rows.
			return _rows.Any()
				? $"grid-template-rows: {string.Join(" ", _rows.Select(row => row.CssSize))};"
				: string.Empty;
		}

		/// <summary>
		/// Returns the width fragment for the rendered grid container.
		/// </summary>
		/// <returns>The CSS width fragment for the grid container.</returns>
		private string Width()
		{
			// The original behavior defaults the grid to fill the available width when no explicit width is supplied.
			return $"width: {(_width.HasValue ? _width.Value.ToString(_enCulture) + "px;" : "100%;")} ";
		}

		/// <summary>
		/// Returns the height fragment for the rendered grid container.
		/// </summary>
		/// <returns>The CSS height fragment for the grid container.</returns>
		private string Height()
		{
			// The original behavior defaults the grid to fill the available height when no explicit height is supplied.
			return $"height: {(_height.HasValue ? _height.Value.ToString(_enCulture) + "px;" : "100%;")} ";
		}

		/// <summary>
		/// Collects and validates all splitter tracks in the supplied direction.
		/// </summary>
		/// <param name="tracks">The authored tracks for a single direction.</param>
		/// <param name="validator">The validator applied to each authored splitter.</param>
		/// <returns>The validated splitter tracks in authored order.</returns>
		private static IReadOnlyList<GridTrackDefinition> GetValidatedSplitters(
			IReadOnlyList<GridTrackDefinition> tracks,
			Action<GridTrackDefinition> validator)
		{
			// Validation is centralized here so both row and column splitters follow the same enumeration and fail-fast behavior.
			var splitters = tracks.Where(track => track.IsSplitter).ToList();
			foreach (var splitter in splitters)
			{
				validator(splitter);
			}

			return splitters;
		}

		/// <summary>
		/// Validates that a content placement does not occupy any splitter tracks in a single direction.
		/// </summary>
		/// <param name="tracks">The authored tracks for a single direction.</param>
		/// <param name="start">The 1-based starting track index.</param>
		/// <param name="end">The ending grid line.</param>
		/// <param name="axisName">The axis label used in diagnostics.</param>
		private static void ValidateReservedTrackPlacement(IReadOnlyList<GridTrackDefinition> tracks, int start, int end, string axisName)
		{
			// GridElement uses start/end line semantics, so the occupied tracks are the authored tracks before the ending line.
			for (var trackIndex = start; trackIndex < end && trackIndex <= tracks.Count; trackIndex++)
			{
				if (tracks[trackIndex - 1].IsSplitter)
				{
					throw new InvalidOperationException($"{axisName} {trackIndex} is reserved for splitter interaction and cannot host GridElement content.");
				}
			}
		}

		/// <summary>
		/// Validates a single splitter column against the supported authoring rules.
		/// </summary>
		/// <param name="splitter">The splitter column to validate.</param>
		private void ValidateSplitterColumn(GridTrackDefinition splitter)
		{
			// Column splitters must sit between two resizable content columns; edges and auto-sized neighbors are explicitly unsupported.
			ValidateSplitterTrack(splitter, _columns, "column", "columns");
		}

		/// <summary>
		/// Validates a single splitter row against the supported authoring rules.
		/// </summary>
		/// <param name="splitter">The splitter row to validate.</param>
		private void ValidateSplitterRow(GridTrackDefinition splitter)
		{
			// Row splitters must sit between two resizable content rows; edges and auto-sized neighbors are explicitly unsupported.
			ValidateSplitterTrack(splitter, _rows, "row", "rows");
		}

		/// <summary>
		/// Validates a splitter track against the shared Workbench authoring rules for either rows or columns.
		/// </summary>
		/// <param name="splitter">The splitter track to validate.</param>
		/// <param name="tracks">The authored tracks for the relevant direction.</param>
		/// <param name="singularAxisName">The singular axis name used in diagnostics.</param>
		/// <param name="pluralAxisName">The plural axis name used in diagnostics.</param>
		private static void ValidateSplitterTrack(GridTrackDefinition splitter, IReadOnlyList<GridTrackDefinition> tracks, string singularAxisName, string pluralAxisName)
		{
			// Splitters must sit between two resizable content tracks so dragging always maps to a valid adjacent pair.
			if (splitter.Index <= 1 || splitter.Index >= tracks.Count)
			{
				throw new InvalidOperationException($"Splitter {singularAxisName} {splitter.Index} must be placed between two resizable {pluralAxisName} and cannot appear at the outer grid edges.");
			}

			var previousTrack = tracks[splitter.Index - 2];
			var nextTrack = tracks[splitter.Index];
			if (previousTrack.IsSplitter || nextTrack.IsSplitter)
			{
				throw new InvalidOperationException($"Splitter {singularAxisName} {splitter.Index} must be placed between two resizable {pluralAxisName} and cannot border another splitter {singularAxisName}.");
			}

			if (!previousTrack.IsResizable || !nextTrack.IsResizable)
			{
				throw new InvalidOperationException($"Splitter {singularAxisName} {splitter.Index} is invalid because Auto-sized {pluralAxisName} are not supported for splitter resizing.");
			}
		}
	}
}
