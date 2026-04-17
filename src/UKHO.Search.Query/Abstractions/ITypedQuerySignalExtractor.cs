using UKHO.Search.Query.Models;

namespace UKHO.Search.Query.Abstractions
{
    /// <summary>
    /// Defines the abstraction that extracts typed signals from normalized query input without leaking recognizer-specific types.
    /// </summary>
    public interface ITypedQuerySignalExtractor
    {
        /// <summary>
        /// Extracts typed signals from a normalized query input snapshot.
        /// </summary>
        /// <param name="input">The normalized query input snapshot that should be inspected for typed signals.</param>
        /// <param name="cancellationToken">The cancellation token that stops extraction when the caller no longer needs the result.</param>
        /// <returns>The normalized typed extraction output that should be retained on the query plan, including temporal and numeric repository-owned signals.</returns>
        Task<QueryExtractedSignals> ExtractAsync(QueryInputSnapshot input, CancellationToken cancellationToken);
    }
}
