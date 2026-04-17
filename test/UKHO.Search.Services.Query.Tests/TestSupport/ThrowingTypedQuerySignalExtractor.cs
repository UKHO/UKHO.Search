using UKHO.Search.Query.Abstractions;
using UKHO.Search.Query.Models;

namespace UKHO.Search.Services.Query.Tests.TestSupport
{
    /// <summary>
    /// Provides a deterministic typed extractor that always throws to verify planner fallback behavior.
    /// </summary>
    internal sealed class ThrowingTypedQuerySignalExtractor : ITypedQuerySignalExtractor
    {
        /// <summary>
        /// Throws an exception for every extraction request.
        /// </summary>
        /// <param name="input">The normalized input snapshot supplied by the planner under test.</param>
        /// <param name="cancellationToken">The cancellation token supplied by the test.</param>
        /// <returns>This method never returns a successful result.</returns>
        public Task<QueryExtractedSignals> ExtractAsync(QueryInputSnapshot input, CancellationToken cancellationToken)
        {
            // Validate the planner input so the failure tested here is the intended extraction failure rather than null misuse.
            ArgumentNullException.ThrowIfNull(input);

            // Throw deterministically so the planner's safe-degradation path can be asserted directly.
            throw new InvalidOperationException("Recognizer failure for test coverage.");
        }
    }
}
