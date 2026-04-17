using UKHO.Search.Query.Abstractions;
using UKHO.Search.Query.Models;

namespace UKHO.Search.Services.Query.Tests.TestSupport
{
    /// <summary>
    /// Provides a deterministic typed extractor that always returns a supplied extracted-signal contract.
    /// </summary>
    internal sealed class FixedTypedQuerySignalExtractor : ITypedQuerySignalExtractor
    {
        private readonly QueryExtractedSignals _signals;

        /// <summary>
        /// Initializes the extractor with the extracted-signal contract that should be returned for every request.
        /// </summary>
        /// <param name="signals">The deterministic extracted-signal contract to return.</param>
        public FixedTypedQuerySignalExtractor(QueryExtractedSignals signals)
        {
            // Capture the supplied signal contract once so service-layer tests can assert planner behavior deterministically.
            _signals = signals ?? throw new ArgumentNullException(nameof(signals));
        }

        /// <summary>
        /// Returns the predetermined extracted-signal contract for the supplied input snapshot.
        /// </summary>
        /// <param name="input">The normalized input snapshot supplied by the planner under test.</param>
        /// <param name="cancellationToken">The cancellation token supplied by the test.</param>
        /// <returns>The predetermined extracted-signal contract.</returns>
        public Task<QueryExtractedSignals> ExtractAsync(QueryInputSnapshot input, CancellationToken cancellationToken)
        {
            // Validate the planner input so tests fail clearly if a null snapshot ever crosses the abstraction boundary.
            ArgumentNullException.ThrowIfNull(input);

            // Return the fixed contract so each planner assertion isolates service behavior rather than recognizer behavior.
            return Task.FromResult(_signals);
        }
    }
}
