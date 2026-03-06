using UKHO.Search.Infrastructure.Ingestion.Statistics.Models;

namespace UKHO.Search.Infrastructure.Ingestion.Statistics
{
    public interface IStatisticsService
    {
        Task<IndexStatistics> GetIndexStatisticsAsync(CancellationToken cancellationToken = default);
    }
}
