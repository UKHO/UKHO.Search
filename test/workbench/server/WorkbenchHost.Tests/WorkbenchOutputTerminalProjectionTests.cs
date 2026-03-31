using Shouldly;
using System.Reflection;
using UKHO.Workbench.Output;
using WorkbenchHost.Components.Layout;
using XtermBlazor;
using Xunit;

namespace WorkbenchHost.Tests
{
    /// <summary>
    /// Verifies the terminal projection helpers that now define the Workbench output-panel rendering contract.
    /// </summary>
    public class WorkbenchOutputTerminalProjectionTests
    {
        /// <summary>
        /// Confirms one terminal block contains the summary line followed by any projected detail lines.
        /// </summary>
        [Fact]
        public void BuildOneTerminalBlockPerEntryUsingSummaryAndInlineDetails()
        {
            // The terminal projection keeps one chronological block per retained entry so detail lines remain grouped beneath the matching summary line.
            var outputEntry = CreateOutputEntry(
                "entry-block",
                OutputLevel.Warning,
                "Module loader",
                "Search module restored cached state.",
                "First detail line.\nSecond detail line.");

            var projectedLines = InvokeStaticPrivateMethod(
                typeof(MainLayout),
                "BuildOutputTerminalLines",
                [outputEntry]) as IReadOnlyList<string>;

            projectedLines.ShouldNotBeNull();
            projectedLines.Count.ShouldBe(3);
            projectedLines[0].ShouldBe($"{outputEntry.TimestampUtc.ToLocalTime():HH:mm:ss} {outputEntry.Source} {outputEntry.Summary}");
            projectedLines[1].ShouldBe("  First detail line.");
            projectedLines[2].ShouldBe("  Second detail line.");
        }

        /// <summary>
        /// Confirms event-code projection appends after detail lines using the inline terminal indentation contract.
        /// </summary>
        [Fact]
        public void AppendTheEventCodeAfterAnyInlineDetailLines()
        {
            // Event-code projection should stay grouped with the related summary block so terminal readers keep context while scanning retained history.
            var outputEntry = CreateOutputEntry(
                "entry-event-code",
                OutputLevel.Error,
                "Diagnostics",
                "Administration module failed to initialise.",
                "The module assembly could not load a dependent service.",
                "WB-402");

            var projectedLines = InvokeStaticPrivateMethod(
                typeof(MainLayout),
                "BuildOutputTerminalLines",
                [outputEntry]) as IReadOnlyList<string>;

            projectedLines.ShouldNotBeNull();
            projectedLines.Count.ShouldBe(3);
            projectedLines[1].ShouldBe("  The module assembly could not load a dependent service.");
            projectedLines[2].ShouldBe("  Event code: WB-402");
        }

        /// <summary>
        /// Confirms mixed newline formats normalize into deterministic inline detail lines.
        /// </summary>
        [Fact]
        public void NormalizeMixedNewlineFormatsWhenProjectingDetailLines()
        {
            // Output entries can originate from different platforms, so the terminal projection should flatten supported newline variants consistently.
            var outputEntry = CreateOutputEntry(
                "entry-newlines",
                OutputLevel.Info,
                "Shell",
                "Workbench output stream ready.",
                "First detail line.\r\nSecond detail line.\rThird detail line.");

            var projectedLines = InvokeStaticPrivateMethod(
                typeof(MainLayout),
                "BuildOutputTerminalLines",
                [outputEntry]) as IReadOnlyList<string>;

            projectedLines.ShouldNotBeNull();
            projectedLines.Count.ShouldBe(4);
            projectedLines[1].ShouldBe("  First detail line.");
            projectedLines[2].ShouldBe("  Second detail line.");
            projectedLines[3].ShouldBe("  Third detail line.");
        }

        /// <summary>
        /// Confirms renderable terminal lines preserve the summary text while adding ANSI severity styling to the summary line.
        /// </summary>
        [Fact]
        public void ApplySeverityStylingToTheRenderableSummaryLineWithoutChangingTheTextContract()
        {
            // Severity styling should remain terminal-only so the readable text contract stays stable for tests, copy operations, and retained-history reasoning.
            var outputEntry = CreateOutputEntry(
                "entry-severity",
                OutputLevel.Error,
                "Diagnostics",
                "Search module failed to initialise.");

            var projectedLines = InvokeStaticPrivateMethod(
                typeof(MainLayout),
                "BuildOutputTerminalRenderableLines",
                [outputEntry]) as IReadOnlyList<string>;

            projectedLines.ShouldNotBeNull();
            projectedLines.Count.ShouldBe(1);
            projectedLines[0].ShouldStartWith("\u001b[91;1m");
            projectedLines[0].ShouldContain("14:05:06 Diagnostics Search module failed to initialise.");
            projectedLines[0].ShouldEndWith("\u001b[0m");
        }

        /// <summary>
        /// Confirms append-only terminal updates return only the newly retained tail entries.
        /// </summary>
        [Fact]
        public void ReturnOnlyTailEntriesWhenTheRetainedHistoryCanBeAppended()
        {
            // Append detection should let the visible terminal write only the newest retained entries when the shared history still matches the already projected prefix.
            var firstEntry = CreateOutputEntry("entry-append-1", OutputLevel.Info, "Shell", "Workbench ready.");
            var secondEntry = CreateOutputEntry("entry-append-2", OutputLevel.Warning, "Shell", "Workbench warning.");

            var appendedEntries = InvokeStaticPrivateMethod(
                typeof(MainLayout),
                "BuildOutputTerminalAppendEntries",
                [new[] { firstEntry, secondEntry }, new[] { firstEntry.Id }]) as IReadOnlyList<OutputEntry>;

            appendedEntries.ShouldNotBeNull();
            appendedEntries.Count.ShouldBe(1);
            appendedEntries[0].Id.ShouldBe(secondEntry.Id);
        }

        /// <summary>
        /// Confirms terminal append mode is rejected when the retained history no longer matches the already projected prefix.
        /// </summary>
        [Fact]
        public void RejectAppendModeWhenTheRetainedHistoryRequiresARebuild()
        {
            // Rebuild detection protects the terminal from drifting away from the shared output service when history is cleared, trimmed, or otherwise rewritten.
            var firstEntry = CreateOutputEntry("entry-rebuild-1", OutputLevel.Info, "Shell", "Workbench ready.");
            var secondEntry = CreateOutputEntry("entry-rebuild-2", OutputLevel.Warning, "Shell", "Workbench warning.");

            var appendedEntries = InvokeStaticPrivateMethod(
                typeof(MainLayout),
                "BuildOutputTerminalAppendEntries",
                [new[] { secondEntry }, new[] { firstEntry.Id }]) as IReadOnlyList<OutputEntry>;

            appendedEntries.ShouldBeNull();
        }

        /// <summary>
        /// Confirms browser-derived shell theme values are mapped into terminal options used by the Workbench output surface.
        /// </summary>
        [Fact]
        public void BuildTerminalOptionsUsingTheSuppliedShellThemeValues()
        {
            // Theme mapping should flow through the terminal options factory so both light and dark shells apply a consistent palette to the hosted output surface.
            var themeValues = new Dictionary<string, string>
            {
                ["background"] = "#101828",
                ["foreground"] = "#f8fafc",
                ["cursor"] = "#f8fafc",
                ["cursorAccent"] = "#101828",
                ["selectionBackground"] = "rgba(132, 202, 255, 0.3)",
                ["selectionInactiveBackground"] = "rgba(132, 202, 255, 0.18)",
                ["scrollbarSliderBackground"] = "rgba(248, 250, 252, 0.18)",
                ["scrollbarSliderHoverBackground"] = "rgba(248, 250, 252, 0.28)",
                ["scrollbarSliderActiveBackground"] = "rgba(248, 250, 252, 0.38)",
                ["black"] = "#1f2937",
                ["red"] = "#f04438",
                ["green"] = "#12b76a",
                ["yellow"] = "#eaaa08",
                ["blue"] = "#2e90fa",
                ["magenta"] = "#a855f7",
                ["cyan"] = "#06b6d4",
                ["white"] = "#d0d5dd",
                ["brightBlack"] = "#98a2b3",
                ["brightRed"] = "#fda29b",
                ["brightGreen"] = "#6ce9a6",
                ["brightYellow"] = "#fde272",
                ["brightBlue"] = "#b2ddff",
                ["brightMagenta"] = "#e9d5ff",
                ["brightCyan"] = "#a5f3fc",
                ["brightWhite"] = "#f8fafc"
            };

            var terminalOptions = InvokeStaticPrivateMethod(
                typeof(MainLayout),
                "CreateOutputTerminalOptions",
                [themeValues]) as TerminalOptions;

            terminalOptions.ShouldNotBeNull();
            terminalOptions.DisableStdin.ShouldBe(true);
            terminalOptions.Theme.Background.ShouldBe("#101828");
            terminalOptions.Theme.Foreground.ShouldBe("#f8fafc");
            terminalOptions.Theme.Red.ShouldBe("#f04438");
            terminalOptions.Theme.BrightBlue.ShouldBe("#b2ddff");
        }

        /// <summary>
        /// Confirms terminal search options stay incremental and case-insensitive for the toolbar and keyboard find workflow.
        /// </summary>
        [Fact]
        public void BuildTerminalSearchOptionsForIncrementalCaseInsensitiveMatching()
        {
            // The shell keeps the first-delivery find workflow lightweight, so the shared search options should bias toward immediate user-visible matches.
            var searchOptions = InvokeStaticPrivateMethod(
                typeof(MainLayout),
                "CreateOutputTerminalSearchOptions") as IReadOnlyDictionary<string, object>;

            searchOptions.ShouldNotBeNull();
            searchOptions["caseSensitive"].ShouldBe(false);
            searchOptions["incremental"].ShouldBe(true);
            searchOptions["regex"].ShouldBe(false);
            searchOptions["wholeWord"].ShouldBe(false);
        }

        /// <summary>
        /// Creates a deterministic output entry for terminal projection verification.
        /// </summary>
        /// <param name="id">The stable output-entry identifier.</param>
        /// <param name="level">The severity level to assign to the test entry.</param>
        /// <param name="source">The source value that should be rendered for the test entry.</param>
        /// <param name="summary">The compact summary that should be rendered for the test entry.</param>
        /// <param name="details">Optional inline diagnostic detail text for the test entry.</param>
        /// <param name="eventCode">Optional event code rendered beneath the related summary block.</param>
        /// <returns>A deterministic immutable output entry suitable for terminal projection verification.</returns>
        private static OutputEntry CreateOutputEntry(
            string id,
            OutputLevel level,
            string source,
            string summary,
            string? details = null,
            string? eventCode = null)
        {
            // Fixed timestamps keep the projection assertions stable regardless of the machine or time zone that runs the tests.
            return new OutputEntry(
                id,
                new DateTimeOffset(2026, 2, 3, 14, 5, 6, TimeSpan.Zero),
                level,
                source,
                summary,
                details,
                eventCode);
        }

        /// <summary>
        /// Invokes a private static helper on <see cref="MainLayout"/> and returns its result.
        /// </summary>
        /// <param name="declaringType">The type that owns the private static method.</param>
        /// <param name="methodName">The private static method name to invoke.</param>
        /// <param name="arguments">The arguments that should be supplied to the private static method.</param>
        /// <returns>The result returned by the invoked private static method.</returns>
        private static object? InvokeStaticPrivateMethod(Type declaringType, string methodName, object?[]? arguments = null)
        {
            // Reflection keeps the terminal projection helpers private while still letting the focused migration tests verify the text and search contracts.
            var methodInfo = declaringType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);

            if (methodInfo is null)
            {
                throw new InvalidOperationException($"The static method '{methodName}' was not found on {declaringType.Name}.");
            }

            return methodInfo.Invoke(null, arguments);
        }
    }
}
