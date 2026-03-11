namespace UKHO.Search.Ingestion.Rules
{
    public interface IIngestionProviderContext
    {
        string? ProviderName { get; set; }
    }
}