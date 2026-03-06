namespace UKHO.Search.Infrastructure.Ingestion.Bootstrap
{
    public interface IBootstrapService
    {
        Task BootstrapAsync(CancellationToken cancellationToken = default);
    }
}