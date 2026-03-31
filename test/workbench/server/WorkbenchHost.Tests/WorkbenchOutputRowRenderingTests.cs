using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Radzen;
using Shouldly;
using UKHO.Workbench.Output;
using WorkbenchHost.Components.Layout;
using Xunit;

namespace WorkbenchHost.Tests
{
    /// <summary>
    /// Verifies the editor-like row markup used by the Workbench output surface uplift.
    /// </summary>
    public class WorkbenchOutputRowRenderingTests
    {
        /// <summary>
        /// Confirms the collapsed row preserves the timestamp, source, and summary order while omitting redundant fold chrome.
        /// </summary>
        [Fact]
        public async Task RenderTheCollapsedEditorLineWithoutFoldChromeWhenNoExpandedContentExists()
        {
            // Rows without details should keep the editor-like line format without showing a non-functional fold affordance.
            await using var serviceProvider = CreateServiceProvider();
            var renderer = new HtmlRenderer(serviceProvider, serviceProvider.GetRequiredService<ILoggerFactory>());
            var outputEntry = CreateOutputEntry(
                "entry-row-collapsed",
                OutputLevel.Info,
                "Shell",
                "Workbench output stream ready.");

            var html = await RenderOutputRowAsync(renderer, outputEntry, isExpanded: false, isWordWrapEnabled: false);
            var timestampMarkup = $"<span class=\"workbench-shell__output-timestamp\">{outputEntry.TimestampUtc.ToLocalTime():HH:mm:ss}</span>";
            var sourceMarkup = $"<span class=\"workbench-shell__output-source\">{outputEntry.Source}</span>";
            var summaryMarkup = $"<span class=\"workbench-shell__output-summary\">{outputEntry.Summary}</span>";
            var timestampIndex = html.IndexOf(timestampMarkup, StringComparison.Ordinal);
            var sourceIndex = html.IndexOf(sourceMarkup, StringComparison.Ordinal);
            var summaryIndex = html.IndexOf(summaryMarkup, StringComparison.Ordinal);

            html.ShouldContain("workbench-shell__output-row-summary");
            html.ShouldContain("data-role=\"output-entry-summary\"");
            html.ShouldContain("data-role=\"output-entry-text\"");
            html.ShouldContain("data-output-wrap-mode=\"nowrap\"");
            html.ShouldContain("data-output-foldable=\"false\"");
            html.ShouldContain("data-role=\"output-gutter-marker\"");
            html.ShouldNotContain("data-role=\"output-entry-toggle\"");
            html.ShouldNotContain("data-role=\"output-copy-entry\"");
            timestampIndex.ShouldBeGreaterThanOrEqualTo(0);
            sourceIndex.ShouldBeGreaterThan(timestampIndex);
            summaryIndex.ShouldBeGreaterThan(sourceIndex);
        }

        /// <summary>
        /// Confirms the expanded row renders a chrome-less fold affordance and inline detail lines while omitting row-level copy chrome.
        /// </summary>
        [Fact]
        public async Task RenderExpandedDetailsAsInlineTextWithTheFoldAffordance()
        {
            // Expanded diagnostics should render as subordinate text lines inside the same editor-like stream instead of separate row action chrome.
            await using var serviceProvider = CreateServiceProvider();
            var renderer = new HtmlRenderer(serviceProvider, serviceProvider.GetRequiredService<ILoggerFactory>());
            var outputEntry = CreateOutputEntry(
                "entry-row-expanded",
                OutputLevel.Warning,
                "Module loader",
                "Search module restored cached state.",
                "First detail line.\nSecond detail line.",
                "WB-402");

            var html = await RenderOutputRowAsync(renderer, outputEntry, isExpanded: true, isWordWrapEnabled: true);

            html.ShouldContain("data-output-expanded=\"true\"");
            html.ShouldContain("data-output-wrap-mode=\"wrapped\"");
            html.ShouldContain("data-output-foldable=\"true\"");
            html.ShouldContain("data-role=\"output-entry-toggle\"");
            html.ShouldContain("data-output-disclosure-style=\"gutter\"");
            html.ShouldContain("data-role=\"output-entry-details\"");
            html.ShouldContain("data-role=\"output-entry-detail-text\"");
            html.ShouldContain("data-role=\"output-gutter-marker\"");
            html.ShouldContain($"aria-controls=\"output-entry-details-{outputEntry.Id}\"");
            html.ShouldContain("aria-label=\"Collapse output entry from Module loader\"");
            html.ShouldContain("title=\"Collapse inline details for Module loader\"");
            html.ShouldContain("First detail line.");
            html.ShouldContain("Second detail line.");
            html.ShouldContain($"Event code: {outputEntry.EventCode}");
            CountOccurrences(html, "data-role=\"output-detail-line\"").ShouldBe(3);
            html.ShouldNotContain("data-role=\"output-copy-entry\"");
        }

        /// <summary>
        /// Confirms the disclosure control is the only interactive chrome rendered for foldable rows so ordinary text remains selection-first.
        /// </summary>
        [Fact]
        public async Task RenderOnlyTheDisclosureControlAsInteractiveChromeForFoldableRows()
        {
            // Foldable rows must stay easy to expand with keyboard and pointer input without making the timestamp, source, or summary text behave like buttons.
            await using var serviceProvider = CreateServiceProvider();
            var renderer = new HtmlRenderer(serviceProvider, serviceProvider.GetRequiredService<ILoggerFactory>());
            var outputEntry = CreateOutputEntry(
                "entry-row-foldable",
                OutputLevel.Error,
                "Diagnostics",
                "Foldable row for interaction coverage.",
                "One detail line.");

            var html = await RenderOutputRowAsync(renderer, outputEntry, isExpanded: false, isWordWrapEnabled: false);

            html.ShouldContain("data-role=\"output-entry-toggle\"");
            html.ShouldContain("title=\"Expand inline details for Diagnostics\"");
            html.ShouldContain("aria-label=\"Expand output entry from Diagnostics\"");
            html.ShouldContain("data-role=\"output-entry-text\"");
            CountOccurrences(html, "<button type=\"button\"").ShouldBe(1);
        }

        /// <summary>
        /// Counts how many times a deterministic markup fragment appears in the rendered HTML.
        /// </summary>
        /// <param name="html">The rendered HTML to inspect.</param>
        /// <param name="fragment">The exact fragment to count.</param>
        /// <returns>The number of exact fragment matches present in the supplied HTML.</returns>
        private static int CountOccurrences(string html, string fragment)
        {
            // Counting exact attribute fragments keeps the tests focused on rendered contract markers without adding an HTML parser dependency.
            ArgumentNullException.ThrowIfNull(html);
            ArgumentException.ThrowIfNullOrWhiteSpace(fragment);

            var occurrenceCount = 0;
            var currentIndex = 0;

            while ((currentIndex = html.IndexOf(fragment, currentIndex, StringComparison.Ordinal)) >= 0)
            {
                occurrenceCount++;
                currentIndex += fragment.Length;
            }

            return occurrenceCount;
        }

        /// <summary>
        /// Creates the service provider used by the focused row rendering tests.
        /// </summary>
        /// <returns>A service provider that can render Radzen-backed Workbench row components.</returns>
        private static ServiceProvider CreateServiceProvider()
        {
            // The row renderer needs the same basic Radzen registrations as the host layout, plus light-weight navigation and JS stubs for static rendering.
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddRadzenComponents();
            services.AddSingleton<IJSRuntime, TestJsRuntime>();
            services.AddSingleton<NavigationManager, TestNavigationManager>();
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Renders one output row with the supplied expansion and wrap flags.
        /// </summary>
        /// <param name="renderer">The renderer used to produce static HTML.</param>
        /// <param name="outputEntry">The deterministic output entry to render.</param>
        /// <param name="isExpanded"><see langword="true"/> when the row should render its inline details.</param>
        /// <param name="isWordWrapEnabled"><see langword="true"/> when the row should render wrapped output metadata.</param>
        /// <returns>The rendered HTML for the requested row state.</returns>
        private static Task<string> RenderOutputRowAsync(
            HtmlRenderer renderer,
            OutputEntry outputEntry,
            bool isExpanded,
            bool isWordWrapEnabled)
        {
            // Rendering through the real component keeps the verification focused on the host markup contract instead of duplicated string building.
            return renderer.Dispatcher.InvokeAsync(async () =>
            {
                var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(WorkbenchOutputRow.Entry)] = outputEntry,
                    [nameof(WorkbenchOutputRow.IsExpanded)] = isExpanded,
                    [nameof(WorkbenchOutputRow.IsWordWrapEnabled)] = isWordWrapEnabled
                });

                var output = await renderer.RenderComponentAsync<WorkbenchOutputRow>(parameters);
                return output.ToHtmlString();
            });
        }

        /// <summary>
        /// Creates a deterministic output entry for row rendering verification.
        /// </summary>
        /// <param name="id">The stable output-entry identifier.</param>
        /// <param name="level">The severity level to assign to the test entry.</param>
        /// <param name="source">The source value that should be rendered for the test entry.</param>
        /// <param name="summary">The compact summary that should be rendered for the test entry.</param>
        /// <param name="details">Optional expanded diagnostic details for the test entry.</param>
        /// <param name="eventCode">Optional event code rendered only in expanded details.</param>
        /// <returns>A deterministic immutable output entry suitable for row rendering verification.</returns>
        private static OutputEntry CreateOutputEntry(
            string id,
            OutputLevel level,
            string source,
            string summary,
            string? details = null,
            string? eventCode = null)
        {
            // Fixed timestamps keep the timestamp assertions stable regardless of the machine or culture that runs the tests.
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
        /// Supplies a minimal JS runtime stub for static component rendering tests.
        /// </summary>
        private sealed class TestJsRuntime : IJSRuntime
        {
            /// <summary>
            /// Returns a default value because the row rendering tests do not execute JavaScript.
            /// </summary>
            /// <typeparam name="TValue">The expected return type.</typeparam>
            /// <param name="identifier">The JavaScript identifier requested by the component.</param>
            /// <param name="args">The arguments that would be passed to JavaScript.</param>
            /// <returns>A completed task containing the default value for <typeparamref name="TValue"/>.</returns>
            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            {
                // The static row renderer only needs JS calls to remain non-throwing.
                return ValueTask.FromResult(default(TValue)!);
            }

            /// <summary>
            /// Returns a default value because the row rendering tests do not execute JavaScript.
            /// </summary>
            /// <typeparam name="TValue">The expected return type.</typeparam>
            /// <param name="identifier">The JavaScript identifier requested by the component.</param>
            /// <param name="cancellationToken">The cancellation token that would flow to the JavaScript invocation.</param>
            /// <param name="args">The arguments that would be passed to JavaScript.</param>
            /// <returns>A completed task containing the default value for <typeparamref name="TValue"/>.</returns>
            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            {
                // The static row renderer only needs JS calls to remain non-throwing.
                return ValueTask.FromResult(default(TValue)!);
            }
        }

        /// <summary>
        /// Supplies a minimal navigation manager for Radzen services during static rendering tests.
        /// </summary>
        private sealed class TestNavigationManager : NavigationManager
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TestNavigationManager"/> class.
            /// </summary>
            public TestNavigationManager()
            {
                // Static rendering only needs a stable base URI so component services can be constructed successfully.
                Initialize("http://localhost/", "http://localhost/");
            }

            /// <summary>
            /// Ignores navigation requests because the row rendering tests do not exercise navigation behavior.
            /// </summary>
            /// <param name="uri">The destination URI.</param>
            /// <param name="options">The navigation options associated with the request.</param>
            protected override void NavigateToCore(string uri, NavigationOptions options)
            {
                // The static renderer never navigates, so the test stub simply tracks the last URI value.
                Uri = ToAbsoluteUri(uri).ToString();
            }
        }
    }
}
