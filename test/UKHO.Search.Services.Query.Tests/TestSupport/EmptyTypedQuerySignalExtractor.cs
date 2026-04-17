using UKHO.Search.Query.Abstractions;
using UKHO.Search.Query.Models;

namespace UKHO.Search.Services.Query.Tests.TestSupport
{
    /// <summary>
    /// Provides a deterministic typed extractor for service-layer tests.
    /// </summary>
    internal sealed class EmptyTypedQuerySignalExtractor : ITypedQuerySignalExtractor
    {
        /// <summary>
        /// Returns an empty typed extraction result for the supplied input snapshot.
        /// </summary>
        /// <param name="input">The normalized input snapshot supplied by the planner under test.</param>
        /// <param name="cancellationToken">The cancellation token supplied by the test.</param>
        /// <returns>An empty typed extraction result.</returns>
        public Task<QueryExtractedSignals> ExtractAsync(QueryInputSnapshot input, CancellationToken cancellationToken)
        {
            // Validate the test input so service-layer tests fail clearly if the planner passes a null snapshot.
            ArgumentNullException.ThrowIfNull(input);

            // Return an empty typed signal payload because work item one does not add recognizer-backed extraction yet.
            return Task.FromResult(new QueryExtractedSignals());
        }
    }
}
