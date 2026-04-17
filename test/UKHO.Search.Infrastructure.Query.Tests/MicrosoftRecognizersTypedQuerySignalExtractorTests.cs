using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.Infrastructure.Query.TypedExtraction;
using UKHO.Search.Query.Models;
using Xunit;

namespace UKHO.Search.Infrastructure.Query.Tests
{
    /// <summary>
    /// Verifies the Microsoft Recognizers-backed typed extraction adapter.
    /// </summary>
    public sealed class MicrosoftRecognizersTypedQuerySignalExtractorTests
    {
        /// <summary>
        /// Verifies that a year-bearing query is normalized into the repository-owned temporal and numeric contracts.
        /// </summary>
        [Fact]
        public async Task ExtractAsync_when_query_contains_year_returns_temporal_year_and_numeric_signal()
        {
            // Create the real adapter because this test should validate the infrastructure integration rather than a fake recognizer wrapper.
            var extractor = new MicrosoftRecognizersTypedQuerySignalExtractor(NullLogger<MicrosoftRecognizersTypedQuerySignalExtractor>.Instance);
            var input = new QueryInputSnapshot
            {
                CleanedText = "latest notice from 2024"
            };

            // Execute the adapter against a representative year-bearing query.
            var extracted = await extractor.ExtractAsync(input, CancellationToken.None);

            extracted.Temporal.Years.ShouldBe([2024]);
            extracted.Temporal.Dates.ShouldBeEmpty();
            extracted.Numbers.Any(number => number.NormalizedValue == "2024").ShouldBeTrue();
        }

        /// <summary>
        /// Verifies that explicit numeric content is retained in the repository-owned numeric contract.
        /// </summary>
        [Fact]
        public async Task ExtractAsync_when_query_contains_numeric_value_returns_normalized_number()
        {
            // Create the real adapter because numeric normalization belongs to the infrastructure adapter itself.
            var extractor = new MicrosoftRecognizersTypedQuerySignalExtractor(NullLogger<MicrosoftRecognizersTypedQuerySignalExtractor>.Instance);
            var input = new QueryInputSnapshot
            {
                CleanedText = "show 12 notices"
            };

            // Execute the adapter against a numeric query fragment.
            var extracted = await extractor.ExtractAsync(input, CancellationToken.None);

            extracted.Numbers.Count.ShouldBeGreaterThan(0);
            extracted.Numbers.First().NormalizedValue.ShouldBe("12");
        }

        /// <summary>
        /// Verifies that empty cleaned query text produces deterministic empty extracted-signal collections.
        /// </summary>
        [Fact]
        public async Task ExtractAsync_when_cleaned_text_is_empty_returns_empty_contract()
        {
            // Create the real adapter because the empty-input behavior is part of the infrastructure contract.
            var extractor = new MicrosoftRecognizersTypedQuerySignalExtractor(NullLogger<MicrosoftRecognizersTypedQuerySignalExtractor>.Instance);
            var input = new QueryInputSnapshot
            {
                CleanedText = string.Empty
            };

            // Execute the adapter against empty input so the deterministic no-match behavior can be asserted.
            var extracted = await extractor.ExtractAsync(input, CancellationToken.None);

            extracted.Temporal.Years.ShouldBeEmpty();
            extracted.Temporal.Dates.ShouldBeEmpty();
            extracted.Numbers.ShouldBeEmpty();
        }
    }
}
