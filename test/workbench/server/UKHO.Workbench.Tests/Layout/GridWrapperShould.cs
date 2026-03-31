using Xunit;

namespace UKHO.Workbench.Layout.Tests
{
    public class GridWrapperShould
    {
        [Theory]
        [ClassData(typeof(GenerateColumnsTemplatesData))]
        public void GenerateColumnsTemplates(List<string> columns, string expected)
        {
            var wrapper = new GridWrapper();
            columns.ForEach(a => wrapper.AddColumn(a));
            Assert.Equal(expected, wrapper.Css);
        }

        [Theory]
        [ClassData(typeof(GenerateColumnsTemplateWithMinAndMaxData))]
        public void GenerateColumnsTemplateWithMinAndMax(List<(string width, string min, string max)> columns, string expected)
        {
            var wrapper = new GridWrapper();
            columns.ForEach(a => wrapper.AddColumn(a.width, a.min, a.max));
            Assert.Equal(expected, wrapper.Css);
        }

        [Theory]
        [ClassData(typeof(GenerateRowsTemplatesData))]
        public void GenerateRowsTemplates(List<string> rows, string expected)
        {
            var wrapper = new GridWrapper();
            rows.ForEach(a => wrapper.AddRow(a));
            Assert.Equal(expected, wrapper.Css);
        }

        [Theory]
        [ClassData(typeof(GenerateRowsTemplateWithMinAndMaxData))]
        public void GenerateRowsTemplateWithMinAndMax(List<(string width, string min, string max)> columns, string expected)
        {
            var wrapper = new GridWrapper();
            columns.ForEach(a => wrapper.AddRow(a.width, a.min, a.max));
            Assert.Equal(expected, wrapper.Css);
        }

        [Theory]
        [ClassData(typeof(CombineRowsAndColumnsTemplateData))]
        public void CombineRowsAndColumnsTemplate(List<string> columns, List<string> rows, string expected)
        {
            var wrapper = new GridWrapper();
            columns.ForEach(a => wrapper.AddColumn(a));
            rows.ForEach(a => wrapper.AddRow(a));
            Assert.Equal(expected, wrapper.Css);
        }

        [Theory]
        [InlineData("test", null, null)]
        [InlineData(null, "test", null)]
        [InlineData(null, null, "test")]
        public void ThrowErrorIfAddColumnWithBadUint(string width, string min, string max)
        {
            var wrapper = new GridWrapper();
            Assert.Throws<GridLayoutException>(() => wrapper.AddColumn(width, min, max));
        }

        [Theory]
        [InlineData("test", null, null)]
        [InlineData(null, "test", null)]
        [InlineData(null, null, "test")]
        public void ThrowErrorIfAddRowWithBadUint(string height, string min, string max)
        {
            var wrapper = new GridWrapper();
            Assert.Throws<GridLayoutException>(() => wrapper.AddRow(height, min, max));
        }

        [Theory]
        [InlineData(null, "display: grid; width: 100%; height: 100%;")]
        [InlineData(20d, "display: grid; width: 20px; height: 100%;")]
        [InlineData(133.7, "display: grid; width: 133.7px; height: 100%;")]
        public void AddGridWidth(double? value, string expected)
        {
            var wrapper = new GridWrapper();
            wrapper.SetWidth(value);
            Assert.Equal(expected, wrapper.Css);
        }

        [Theory]
        [InlineData(null, "display: grid; width: 100%; height: 100%;")]
        [InlineData(20d, "display: grid; width: 100%; height: 20px;")]
        [InlineData(133.7, "display: grid; width: 100%; height: 133.7px;")]
        public void AddGridHeight(double? value, string expected)
        {
            var wrapper = new GridWrapper();
            wrapper.SetHeight(value);
            Assert.Equal(expected, wrapper.Css);
        }

        /// <summary>
        /// Confirms the wrapper can discard a previous render pass before re-registering a different authored track order.
        /// </summary>
        [Fact]
        public void ResetPreviouslyRegisteredTracksBeforeRebuildingAConditionalLayout()
        {
            // Conditional Workbench layouts can insert splitter rows between existing rows on rerender, so stale tracks must be cleared before the next pass re-registers them.
            var wrapper = new GridWrapper();

            wrapper.AddRow("48");
            wrapper.AddRow("44");
            wrapper.AddRow("*");
            wrapper.AddRow("30");

            wrapper.ResetTracks();

            wrapper.AddRow("48");
            wrapper.AddRow("44");
            wrapper.AddRow("4*");
            wrapper.AddSplitterRow("4");
            wrapper.AddRow("1*");
            wrapper.AddRow("30");

            Assert.Equal(
                "display: grid; width: 100%; height: 100%; grid-template-rows: 48px 44px 4fr 4px 1fr 30px;",
                wrapper.Css);
        }

        /// <summary>
        /// Confirms the wrapper accepts resolved CSS pixel tokens when a resized splitter height is restored in the same session.
        /// </summary>
        [Fact]
        public void PreserveResolvedPixelTrackTokensWhenReopeningAResizedLayout()
        {
            // The output-panel session state stores splitter results as CSS pixel tokens, so the grid must accept those values when the layout is rebuilt.
            var wrapper = new GridWrapper();

            wrapper.AddRow("48");
            wrapper.AddRow("44");
            wrapper.AddRow("559.39px");
            wrapper.AddSplitterRow("4");
            wrapper.AddRow("186.46px");
            wrapper.AddRow("30");

            Assert.Equal(
                "display: grid; width: 100%; height: 100%; grid-template-rows: 48px 44px 559.39px 4px 186.46px 30px;",
                wrapper.Css);
        }

        /// <summary>
        /// Confirms the wrapper accepts fractional star track tokens when proportional splitter ratios are restored after browser resizing.
        /// </summary>
        [Fact]
        public void PreserveFractionalStarTrackTokensWhenReopeningAResizedLayout()
        {
            // Browser resize handling now persists splitter ratios as fractional star tracks so the shell can scale with the viewport while keeping the original pane ratio.
            var wrapper = new GridWrapper();

            wrapper.AddRow("48");
            wrapper.AddRow("44");
            wrapper.AddRow("1.303*");
            wrapper.AddSplitterRow("4");
            wrapper.AddRow("1*");
            wrapper.AddRow("30");

            Assert.Equal(
                "display: grid; width: 100%; height: 100%; grid-template-rows: 48px 44px 1.303fr 4px 1fr 30px;",
                wrapper.Css);
        }

        private class GenerateColumnsTemplatesData : TheoryData<List<string>, string>
        {
            public GenerateColumnsTemplatesData()
            {
                Add(new List<string> { "*" }, "display: grid; width: 100%; height: 100%; grid-template-columns: 1fr;");
                Add(new List<string> { "1*", "2*" }, "display: grid; width: 100%; height: 100%; grid-template-columns: 1fr 2fr;");
                Add(new List<string> { "1*", "12*" }, "display: grid; width: 100%; height: 100%; grid-template-columns: 1fr 12fr;");
                Add(new List<string> { "1*", "25" }, "display: grid; width: 100%; height: 100%; grid-template-columns: 1fr 25px;");
                Add(new List<string> { "auto" }, "display: grid; width: 100%; height: 100%; grid-template-columns: auto;");
                Add(new List<string> { "" }, "display: grid; width: 100%; height: 100%; grid-template-columns: 1fr;");
                Add(new List<string> { "", "*", "2*", "35", "Auto" }, "display: grid; width: 100%; height: 100%; grid-template-columns: 1fr 1fr 2fr 35px auto;");
            }
        }

        private class GenerateColumnsTemplateWithMinAndMaxData : TheoryData<List<(string width, string min, string max)>, string>
        {
            public GenerateColumnsTemplateWithMinAndMaxData()
            {
                Add(new List<(string width, string min, string max)> { ("auto", "25", "") }, "display: grid; width: 100%; height: 100%; grid-template-columns: minmax(25px,1fr);");
                Add(new List<(string width, string min, string max)> { ("*", "25", "") }, "display: grid; width: 100%; height: 100%; grid-template-columns: minmax(25px,1fr);");
                Add(new List<(string width, string min, string max)> { ("*", "", "30") }, "display: grid; width: 100%; height: 100%; grid-template-columns: minmax(1px,30px);");
                Add(new List<(string width, string min, string max)> { ("*", "", "2*") }, "display: grid; width: 100%; height: 100%; grid-template-columns: minmax(1px,2fr);");
                Add(new List<(string width, string min, string max)> { ("*", "1", null), ("50", "", "100") }, "display: grid; width: 100%; height: 100%; grid-template-columns: minmax(1px,1fr) minmax(1px,100px);");
                Add(new List<(string width, string min, string max)> { ("*", null, null), ("50", "", "100") }, "display: grid; width: 100%; height: 100%; grid-template-columns: 1fr minmax(1px,100px);");
            }
        }

        private class GenerateRowsTemplatesData : TheoryData<List<string>, string>
        {
            public GenerateRowsTemplatesData()
            {
                Add(new List<string> { "*" }, "display: grid; width: 100%; height: 100%; grid-template-rows: 1fr;");
                Add(new List<string> { "1*", "2*" }, "display: grid; width: 100%; height: 100%; grid-template-rows: 1fr 2fr;");
                Add(new List<string> { "1*", "12*" }, "display: grid; width: 100%; height: 100%; grid-template-rows: 1fr 12fr;");
                Add(new List<string> { "1*", "25" }, "display: grid; width: 100%; height: 100%; grid-template-rows: 1fr 25px;");
                Add(new List<string> { "auto" }, "display: grid; width: 100%; height: 100%; grid-template-rows: auto;");
                Add(new List<string> { "" }, "display: grid; width: 100%; height: 100%; grid-template-rows: 1fr;");
                Add(new List<string> { "", "*", "2*", "35", "Auto" }, "display: grid; width: 100%; height: 100%; grid-template-rows: 1fr 1fr 2fr 35px auto;");
            }
        }

        public class CombineRowsAndColumnsTemplateData : TheoryData<List<string>, List<string>, string>
        {
            public CombineRowsAndColumnsTemplateData()
            {
                Add(new List<string> { "*" }, new List<string> { "*" }, "display: grid; width: 100%; height: 100%; grid-template-columns: 1fr; grid-template-rows: 1fr;");
                Add(new List<string> { "*", "20", "auto", "2*" }, new List<string> { "*", "20", "auto", "2*" }, "display: grid; width: 100%; height: 100%; grid-template-columns: 1fr 20px auto 2fr; grid-template-rows: 1fr 20px auto 2fr;");
            }
        }

        private class GenerateRowsTemplateWithMinAndMaxData : TheoryData<List<(string width, string min, string max)>, string>
        {
            public GenerateRowsTemplateWithMinAndMaxData()
            {
                Add(new List<(string width, string min, string max)> { ("auto", "25", "") }, "display: grid; width: 100%; height: 100%; grid-template-rows: minmax(25px,1fr);");
                Add(new List<(string width, string min, string max)> { ("*", "25", "") }, "display: grid; width: 100%; height: 100%; grid-template-rows: minmax(25px,1fr);");
                Add(new List<(string width, string min, string max)> { ("*", "", "30") }, "display: grid; width: 100%; height: 100%; grid-template-rows: minmax(1px,30px);");
                Add(new List<(string width, string min, string max)> { ("*", "", "2*") }, "display: grid; width: 100%; height: 100%; grid-template-rows: minmax(1px,2fr);");
                Add(new List<(string width, string min, string max)> { ("*", "1", null), ("50", "", "100") }, "display: grid; width: 100%; height: 100%; grid-template-rows: minmax(1px,1fr) minmax(1px,100px);");
                Add(new List<(string width, string min, string max)> { ("*", null, null), ("50", "", "100") }, "display: grid; width: 100%; height: 100%; grid-template-rows: 1fr minmax(1px,100px);");
            }
        }
    }
}