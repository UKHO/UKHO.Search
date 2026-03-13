using QueryServiceHost.Models;

namespace QueryServiceHost.Services
{
    public interface IQueryUiSearchClient
    {
        Task<QueryResponse> SearchAsync(QueryRequest request, CancellationToken cancellationToken);
    }
}
