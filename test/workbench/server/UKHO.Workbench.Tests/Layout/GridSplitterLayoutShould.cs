using Shouldly;
using Xunit;

namespace UKHO.Workbench.Layout.Tests
{
    /// <summary>
    /// Verifies the Workbench-specific splitter registration, defaults, and fail-fast validation rules.
    /// </summary>
    public class GridSplitterLayoutShould
    {
        /// <summary>
        /// Ensures an omitted splitter width falls back to the documented 4px gutter thickness.
        /// </summary>
        [Fact]
        public void GenerateAColumnTemplateThatUsesTheDefaultSplitterThicknessWhenWidthIsOmitted()
        {
            // Arrange a grid with a fixed column, the new dedicated splitter column, and a star-sized content column.
            var wrapper = new GridWrapper();
            wrapper.AddColumn("280");
            wrapper.AddSplitterColumn(null);
            wrapper.AddColumn("*");

            // Act by asking the wrapper for its CSS template output.
            var css = wrapper.Css;

            // Assert that the splitter track contributes a 4px gutter to the generated template.
            css.ShouldBe("display: grid; width: 100%; height: 100%; grid-template-columns: 280px 4px 1fr;");
        }

        /// <summary>
        /// Ensures validated splitter metadata preserves the authored 1-based track ordering used by content placement.
        /// </summary>
        [Fact]
        public void ExposeValidatedSplitterMetadataUsingOneBasedTrackIndices()
        {
            // Arrange a splitter-enabled grid with the splitter declared between two resizable content tracks.
            var wrapper = new GridWrapper();
            wrapper.AddColumn("240");
            wrapper.AddSplitterColumn("6");
            wrapper.AddColumn("*");

            // Act by requesting the validated splitter metadata.
            var splitter = Assert.Single(wrapper.GetValidatedColumnSplitters());

            // Assert that the metadata identifies the authored splitter track position.
            splitter.Index.ShouldBe(2);
            splitter.CssSize.ShouldBe("6px");
        }

        /// <summary>
        /// Ensures the wrapper can still distinguish fixed and flexible adjacent tracks so splitter dragging can preserve star-based stretch behavior.
        /// </summary>
        [Fact]
        public void IdentifyWhichAdjacentTracksRemainFlexibleAfterSplitterResizing()
        {
            // Arrange a grid that mirrors the Workbench shell case where a fixed explorer column sits next to a star-sized center column.
            var wrapper = new GridWrapper();
            wrapper.AddColumn("280");
            wrapper.AddSplitterColumn(null);
            wrapper.AddColumn("*");

            // Act and assert that only the authored star-sized column is reported as flexible.
            wrapper.IsFlexibleColumn(1).ShouldBeFalse();
            wrapper.IsFlexibleColumn(3).ShouldBeTrue();
        }

        /// <summary>
        /// Ensures authored proportional star-sized columns are preserved in the generated CSS template.
        /// </summary>
        [Fact]
        public void GenerateAColumnTemplateThatPreservesProportionalStarSizing()
        {
            // Arrange a grid that mirrors the Workbench startup layout where the fixed activity rail sits beside a 1:4 explorer-center split.
            var wrapper = new GridWrapper();
            wrapper.AddColumn("64");
            wrapper.AddColumn("*");
            wrapper.AddSplitterColumn(null);
            wrapper.AddColumn("4*");

            // Act by asking the wrapper for the generated CSS template.
            var css = wrapper.Css;

            // Assert that the activity rail stays fixed while the explorer-center proportional star values remain intact.
            css.ShouldBe("display: grid; width: 100%; height: 100%; grid-template-columns: 64px 1fr 4px 4fr;");
        }

        /// <summary>
        /// Ensures an omitted splitter height falls back to the documented 4px gutter thickness for rows.
        /// </summary>
        [Fact]
        public void GenerateARowTemplateThatUsesTheDefaultSplitterThicknessWhenHeightIsOmitted()
        {
            // Arrange a grid with a fixed row, the new dedicated splitter row, and a star-sized content row.
            var wrapper = new GridWrapper();
            wrapper.AddRow("96");
            wrapper.AddSplitterRow(null);
            wrapper.AddRow("*");

            // Act by asking the wrapper for its CSS template output.
            var css = wrapper.Css;

            // Assert that the splitter track contributes a 4px gutter to the generated row template.
            css.ShouldBe("display: grid; width: 100%; height: 100%; grid-template-rows: 96px 4px 1fr;");
        }

        /// <summary>
        /// Ensures validated row splitter metadata preserves the authored 1-based track ordering used by content placement.
        /// </summary>
        [Fact]
        public void ExposeValidatedRowSplitterMetadataUsingOneBasedTrackIndices()
        {
            // Arrange a splitter-enabled grid with the splitter declared between two resizable content tracks.
            var wrapper = new GridWrapper();
            wrapper.AddRow("120");
            wrapper.AddSplitterRow("6");
            wrapper.AddRow("*");

            // Act by requesting the validated splitter metadata.
            var splitter = Assert.Single(wrapper.GetValidatedRowSplitters());

            // Assert that the metadata identifies the authored splitter track position.
            splitter.Index.ShouldBe(2);
            splitter.CssSize.ShouldBe("6px");
        }

        /// <summary>
        /// Ensures a splitter declared at the first or last column position fails fast during validation.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ThrowWhenASplitterIsPlacedAtTheOuterEdgeOfTheGrid(bool splitterIsFirst)
        {
            // Arrange an invalid grid where the splitter is authored on a grid edge instead of between two content columns.
            var wrapper = new GridWrapper();
            if (splitterIsFirst)
            {
                wrapper.AddSplitterColumn(null);
                wrapper.AddColumn("*");
            }
            else
            {
                wrapper.AddColumn("*");
                wrapper.AddSplitterColumn(null);
            }

            // Act and assert that validation stops the unsupported configuration immediately.
            Should.Throw<InvalidOperationException>(() => wrapper.GetValidatedColumnSplitters())
                .Message.ShouldContain("between two resizable columns");
        }

        /// <summary>
        /// Ensures auto-sized columns are rejected when a splitter attempts to resize them.
        /// </summary>
        [Fact]
        public void ThrowWhenASplitterBordersAnAutoSizedColumn()
        {
            // Arrange an invalid grid where one of the adjacent columns is Auto-sized and therefore not resizable.
            var wrapper = new GridWrapper();
            wrapper.AddColumn("Auto");
            wrapper.AddSplitterColumn(null);
            wrapper.AddColumn("*");

            // Act and assert that validation rejects the unsupported Auto-sized pair.
            Should.Throw<InvalidOperationException>(() => wrapper.GetValidatedColumnSplitters())
                .Message.ShouldContain("Auto-sized columns are not supported");
        }

        /// <summary>
        /// Ensures a splitter declared at the first or last row position fails fast during validation.
        /// </summary>
        /// <param name="splitterIsFirst"><c>true</c> when the invalid splitter is placed on the first row; otherwise, <c>false</c>.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ThrowWhenARowSplitterIsPlacedAtTheOuterEdgeOfTheGrid(bool splitterIsFirst)
        {
            // Arrange an invalid grid where the splitter is authored on a grid edge instead of between two content rows.
            var wrapper = new GridWrapper();
            if (splitterIsFirst)
            {
                wrapper.AddSplitterRow(null);
                wrapper.AddRow("*");
            }
            else
            {
                wrapper.AddRow("*");
                wrapper.AddSplitterRow(null);
            }

            // Act and assert that validation stops the unsupported configuration immediately.
            Should.Throw<InvalidOperationException>(() => wrapper.GetValidatedRowSplitters())
                .Message.ShouldContain("between two resizable rows");
        }

        /// <summary>
        /// Ensures auto-sized rows are rejected when a splitter attempts to resize them.
        /// </summary>
        [Fact]
        public void ThrowWhenARowSplitterBordersAnAutoSizedRow()
        {
            // Arrange an invalid grid where one of the adjacent rows is Auto-sized and therefore not resizable.
            var wrapper = new GridWrapper();
            wrapper.AddRow("Auto");
            wrapper.AddSplitterRow(null);
            wrapper.AddRow("*");

            // Act and assert that validation rejects the unsupported Auto-sized pair.
            Should.Throw<InvalidOperationException>(() => wrapper.GetValidatedRowSplitters())
                .Message.ShouldContain("Auto-sized rows are not supported");
        }

        /// <summary>
        /// Ensures splitter columns remain reserved gutters rather than becoming valid content targets.
        /// </summary>
        [Fact]
        public void ThrowWhenContentIsAuthoredIntoASplitterColumn()
        {
            // Arrange a grid with a splitter in the second authored column.
            var wrapper = new GridWrapper();
            wrapper.AddColumn("240");
            wrapper.AddSplitterColumn(null);
            wrapper.AddColumn("*");

            // Act and assert that content placement into the splitter column fails fast.
            Should.Throw<InvalidOperationException>(() => wrapper.ValidateContentPlacement(2, 3, 1, 2))
                .Message.ShouldContain("reserved for splitter interaction");
        }

        /// <summary>
        /// Ensures splitter rows remain reserved gutters rather than becoming valid content targets.
        /// </summary>
        [Fact]
        public void ThrowWhenContentIsAuthoredIntoASplitterRow()
        {
            // Arrange a grid with a splitter in the second authored row.
            var wrapper = new GridWrapper();
            wrapper.AddRow("140");
            wrapper.AddSplitterRow(null);
            wrapper.AddRow("*");

            // Act and assert that content placement into the splitter row fails fast.
            Should.Throw<InvalidOperationException>(() => wrapper.ValidateContentPlacement(1, 2, 2, 3))
                .Message.ShouldContain("reserved for splitter interaction");
        }

        /// <summary>
        /// Ensures a single grid can combine row and column splitters while preserving 1-based developer-facing indexing.
        /// </summary>
        [Fact]
        public void GenerateTemplatesForCombinedRowAndColumnSplitterLayouts()
        {
            // Arrange a grid that mixes one column splitter and one row splitter in the same authored layout.
            var wrapper = new GridWrapper();
            wrapper.AddColumn("240");
            wrapper.AddSplitterColumn(null);
            wrapper.AddColumn("*");
            wrapper.AddRow("120");
            wrapper.AddSplitterRow(null);
            wrapper.AddRow("*");

            // Act by requesting the generated CSS and validated splitter metadata.
            var css = wrapper.Css;
            var columnSplitter = Assert.Single(wrapper.GetValidatedColumnSplitters());
            var rowSplitter = Assert.Single(wrapper.GetValidatedRowSplitters());

            // Assert that both directions participate in the generated template and retain their authored track numbers.
            css.ShouldBe("display: grid; width: 100%; height: 100%; grid-template-columns: 240px 4px 1fr; grid-template-rows: 120px 4px 1fr;");
            columnSplitter.Index.ShouldBe(2);
            rowSplitter.Index.ShouldBe(2);
        }

        /// <summary>
        /// Ensures the grid wrapper supports more than one splitter in the same direction without losing authored ordering.
        /// </summary>
        [Fact]
        public void GenerateTemplatesForMultipleSplitterTracksInTheSameGrid()
        {
            // Arrange a grid that mixes multiple splitter gutters across both axes.
            var wrapper = new GridWrapper();
            wrapper.AddColumn("180");
            wrapper.AddSplitterColumn(null);
            wrapper.AddColumn("*");
            wrapper.AddSplitterColumn("6");
            wrapper.AddColumn("220");
            wrapper.AddRow("96");
            wrapper.AddSplitterRow(null);
            wrapper.AddRow("*");
            wrapper.AddSplitterRow("8");
            wrapper.AddRow("128");

            // Act by requesting the generated CSS and validated splitter metadata.
            var css = wrapper.Css;
            var columnSplitters = wrapper.GetValidatedColumnSplitters();
            var rowSplitters = wrapper.GetValidatedRowSplitters();

            // Assert that each splitter remains a first-class authored track and preserves its 1-based position.
            css.ShouldBe("display: grid; width: 100%; height: 100%; grid-template-columns: 180px 4px 1fr 6px 220px; grid-template-rows: 96px 4px 1fr 8px 128px;");
            columnSplitters.Select(splitter => splitter.Index).ShouldBe(new[] { 2, 4 });
            rowSplitters.Select(splitter => splitter.Index).ShouldBe(new[] { 2, 4 });
        }

        /// <summary>
        /// Ensures configured row and column gaps remain additional to splitter gutter size rather than replacing it.
        /// </summary>
        [Fact]
        public void KeepConfiguredGapsAdditionalToSplitterTrackSize()
        {
            // Arrange a splitter-enabled grid with explicit row and column gaps.
            var wrapper = new GridWrapper();
            wrapper.AddColumn("240");
            wrapper.AddSplitterColumn(null);
            wrapper.AddColumn("*");
            wrapper.AddRow("120");
            wrapper.AddSplitterRow(null);
            wrapper.AddRow("*");

            // Act by composing the same CSS fragments the Grid component uses when rendering.
            var style = $"{wrapper.Css} {wrapper.RowGap(12)} {wrapper.ColumnGap(8)}".Trim();

            // Assert that the grid keeps both the splitter gutters and the authored gaps in the final style string.
            style.ShouldContain("grid-template-columns: 240px 4px 1fr;");
            style.ShouldContain("grid-template-rows: 120px 4px 1fr;");
            style.ShouldContain("grid-row-gap: 12px;");
            style.ShouldContain("grid-column-gap: 8px;");
        }

        /// <summary>
        /// Ensures separate splitter-enabled grids can be configured independently for nested layout scenarios.
        /// </summary>
        [Fact]
        public void AllowIndependentNestedSplitterEnabledGridDefinitions()
        {
            // Arrange outer and inner grids to model the metadata each nested grid instance owns independently.
            var outerWrapper = new GridWrapper();
            outerWrapper.AddColumn("220");
            outerWrapper.AddSplitterColumn(null);
            outerWrapper.AddColumn("*");
            outerWrapper.AddSplitterColumn(null);
            outerWrapper.AddColumn("180");
            outerWrapper.AddRow("110");
            outerWrapper.AddSplitterRow(null);
            outerWrapper.AddRow("*");

            var innerWrapper = new GridWrapper();
            innerWrapper.AddColumn("*");
            innerWrapper.AddSplitterColumn(null);
            innerWrapper.AddColumn("*");
            innerWrapper.AddRow("90");
            innerWrapper.AddSplitterRow(null);
            innerWrapper.AddRow("*");

            // Act by validating each grid independently.
            var outerColumnSplitters = outerWrapper.GetValidatedColumnSplitters();
            var outerRowSplitters = outerWrapper.GetValidatedRowSplitters();
            var innerColumnSplitters = innerWrapper.GetValidatedColumnSplitters();
            var innerRowSplitters = innerWrapper.GetValidatedRowSplitters();

            // Assert that nested grids can each maintain their own splitter metadata without colliding indices or validation state.
            outerColumnSplitters.Select(splitter => splitter.Index).ShouldBe(new[] { 2, 4 });
            outerRowSplitters.Select(splitter => splitter.Index).ShouldBe(new[] { 2 });
            innerColumnSplitters.Select(splitter => splitter.Index).ShouldBe(new[] { 2 });
            innerRowSplitters.Select(splitter => splitter.Index).ShouldBe(new[] { 2 });
        }
    }
}
