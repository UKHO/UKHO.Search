namespace UKHO.Search.Ingestion.Providers.FileShare
{
    public class FileShareIngestionProvider : IIngestionDataProvider
    {
        public string Name => "file-share";

        public string QueueName => "file-share-queue";
    }
}