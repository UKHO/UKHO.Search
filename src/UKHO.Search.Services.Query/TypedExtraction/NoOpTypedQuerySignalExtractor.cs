using UKHO.Search.Query.Abstractions;
using UKHO.Search.Query.Models;

namespace UKHO.Search.Services.Query.TypedExtraction
{
    /// <summary>
    /// Provides the slice-one typed extraction implementation that deliberately returns no extracted signals.
    /// </summary>
    public sealed class NoOpTypedQuerySignalExtractor : ITypedQuerySignalExtractor
    {
        /// <summary>
        /// Returns an empty typed extraction result for the supplied query input.
        /// </summary>
        /// <param name="input">The normalized query input snapshot that would normally be inspected for typed signals.</param>
        /// <param name="cancellationToken">The cancellation token that stops extraction when the caller no longer needs the result.</param>
        /// <returns>An empty typed extraction result.</returns>
        public Task<QueryExtractedSignals> ExtractAsync(QueryInputSnapshot input, CancellationToken cancellationToken)
        {
            // Guard the contract explicitly so downstream planners can rely on a non-null input snapshot.
            ArgumentNullException.ThrowIfNull(input);

            // Slice one keeps typed extraction injectable while proving the end-to-end search path without recognizer logic yet.
            return Task.FromResult(new QueryExtractedSignals());
        }
    }
}
