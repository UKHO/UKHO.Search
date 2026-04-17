using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Recognizers.Text.Number;
using UKHO.Search.Query.Abstractions;
using UKHO.Search.Query.Models;

namespace UKHO.Search.Infrastructure.Query.TypedExtraction
{
    /// <summary>
    /// Adapts Microsoft Recognizers into the repository-owned typed query signal contract.
    /// </summary>
    internal sealed class MicrosoftRecognizersTypedQuerySignalExtractor : ITypedQuerySignalExtractor
    {
        private readonly ILogger<MicrosoftRecognizersTypedQuerySignalExtractor> _logger;

        /// <summary>
        /// Initializes the recognizer-backed typed extractor with the logger used for extraction diagnostics.
        /// </summary>
        /// <param name="logger">The logger used to emit structured extraction diagnostics.</param>
        public MicrosoftRecognizersTypedQuerySignalExtractor(ILogger<MicrosoftRecognizersTypedQuerySignalExtractor> logger)
        {
            // Capture the logger once so every extraction request can publish the same structured observability shape.
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Extracts repository-owned temporal and numeric signals from normalized query input.
        /// </summary>
        /// <param name="input">The normalized query input snapshot that should be inspected for typed signals.</param>
        /// <param name="cancellationToken">The cancellation token that stops extraction when the caller no longer needs the result.</param>
        /// <returns>The repository-owned extracted-signal contract derived from Microsoft Recognizers output.</returns>
        public Task<QueryExtractedSignals> ExtractAsync(QueryInputSnapshot input, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(input);

            // Respect cooperative cancellation before any recognizer work begins.
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(input.CleanedText))
            {
                // Return the empty contract deterministically so the planner can continue without special-case null handling.
                _logger.LogInformation("Skipped typed extraction because the cleaned query text was empty.");
                return Task.FromResult(new QueryExtractedSignals());
            }

            // Run the recognizers over the cleaned text once per category and then normalize their results into repository-owned contracts.
            var dateTimeResults = DateTimeRecognizer.RecognizeDateTime(input.CleanedText, Culture.English);
            var numberResults = NumberRecognizer.RecognizeNumber(input.CleanedText, Culture.English);

            var years = ExtractYears(dateTimeResults);
            var dates = ExtractDates(dateTimeResults);
            var numbers = ExtractNumbers(numberResults);

            var extractedSignals = new QueryExtractedSignals
            {
                Temporal = new QueryTemporalSignals
                {
                    Years = years,
                    Dates = dates
                },
                Numbers = numbers
            };

            // Log the repository-facing signal shape rather than raw recognizer payloads so the abstraction boundary stays clean.
            _logger.LogInformation(
                "Recognized typed query signals. Years={Years} DateCount={DateCount} NumberCount={NumberCount}",
                years.Count == 0 ? "none" : string.Join(",", years),
                dates.Count,
                numbers.Count);

            return Task.FromResult(extractedSignals);
        }

        /// <summary>
        /// Extracts distinct year values from recognizer datetime results.
        /// </summary>
        /// <param name="dateTimeResults">The datetime recognizer results to inspect.</param>
        /// <returns>The ordered year values recognized from the query text.</returns>
        private static IReadOnlyCollection<int> ExtractYears(IEnumerable<ModelResult> dateTimeResults)
        {
            ArgumentNullException.ThrowIfNull(dateTimeResults);

            // Use an ordered set so repeated recognizer hits collapse into one deterministic year list.
            var years = new SortedSet<int>();

            foreach (var result in dateTimeResults)
            {
                if (TryParseYear(result.Text, out var year))
                {
                    years.Add(year);
                }
            }

            return years.ToArray();
        }

        /// <summary>
        /// Extracts non-year temporal matches from recognizer datetime results.
        /// </summary>
        /// <param name="dateTimeResults">The datetime recognizer results to inspect.</param>
        /// <returns>The ordered temporal matches that are richer than simple year values.</returns>
        private static IReadOnlyCollection<QueryTemporalDateSignal> ExtractDates(IEnumerable<ModelResult> dateTimeResults)
        {
            ArgumentNullException.ThrowIfNull(dateTimeResults);

            // Preserve recognizer order so diagnostics and future rule evaluation see a stable sequence.
            var dates = new List<QueryTemporalDateSignal>();

            foreach (var result in dateTimeResults)
            {
                if (string.IsNullOrWhiteSpace(result.Text) || TryParseYear(result.Text, out _))
                {
                    continue;
                }

                dates.Add(new QueryTemporalDateSignal
                {
                    MatchedText = result.Text,
                    Kind = result.TypeName ?? string.Empty
                });
            }

            return dates;
        }

        /// <summary>
        /// Extracts normalized numeric signals from recognizer number results.
        /// </summary>
        /// <param name="numberResults">The numeric recognizer results to inspect.</param>
        /// <returns>The ordered repository-owned numeric signals derived from the query text.</returns>
        private static IReadOnlyCollection<QueryNumericSignal> ExtractNumbers(IEnumerable<ModelResult> numberResults)
        {
            ArgumentNullException.ThrowIfNull(numberResults);

            // Preserve recognizer order so future planner logic can reason about the user's numeric phrasing deterministically.
            var numbers = new List<QueryNumericSignal>();

            foreach (var result in numberResults)
            {
                if (string.IsNullOrWhiteSpace(result.Text))
                {
                    continue;
                }

                numbers.Add(new QueryNumericSignal
                {
                    MatchedText = result.Text,
                    NormalizedValue = NormalizeNumericValue(result.Text)
                });
            }

            return numbers;
        }

        /// <summary>
        /// Attempts to parse one recognizer text fragment as a repository-owned year value.
        /// </summary>
        /// <param name="candidateText">The recognizer text fragment that may represent a year.</param>
        /// <param name="year">The parsed year when the recognizer fragment is a supported four-digit year.</param>
        /// <returns><see langword="true" /> when the recognizer fragment represents a supported year; otherwise, <see langword="false" />.</returns>
        private static bool TryParseYear(string? candidateText, out int year)
        {
            // Restrict the projection to explicit four-digit years because those map directly onto the canonical majorVersion field.
            if (int.TryParse(candidateText, NumberStyles.None, CultureInfo.InvariantCulture, out year)
                && year is >= 1000 and <= 9999)
            {
                return true;
            }

            year = default;
            return false;
        }

        /// <summary>
        /// Normalizes one numeric text fragment into the repository-owned numeric value representation.
        /// </summary>
        /// <param name="candidateText">The recognizer text fragment that should become a numeric signal value.</param>
        /// <returns>The normalized numeric value retained on the query plan.</returns>
        private static string NormalizeNumericValue(string candidateText)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(candidateText);

            // First prefer invariant numeric formatting for plain numeric text so query-plan diagnostics stay culture-neutral.
            if (decimal.TryParse(candidateText, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariantValue))
            {
                return invariantValue.ToString(CultureInfo.InvariantCulture);
            }

            // Fall back to the matched text when the fragment is descriptive wording because the repository contract still needs a deterministic value.
            return candidateText;
        }
    }
}
