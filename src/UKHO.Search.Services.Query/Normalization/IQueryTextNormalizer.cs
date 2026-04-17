using UKHO.Search.Query.Models;

namespace UKHO.Search.Services.Query.Normalization
{
    /// <summary>
    /// Defines the service that normalizes raw user query text into a deterministic planning snapshot.
    /// </summary>
    public interface IQueryTextNormalizer
    {
        /// <summary>
        /// Normalizes the supplied raw query text.
        /// </summary>
        /// <param name="queryText">The raw query text submitted by the caller.</param>
        /// <returns>The normalized query snapshot used by planning.</returns>
        QueryInputSnapshot Normalize(string? queryText);
    }
}
