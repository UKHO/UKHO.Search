namespace UKHO.Search.Ingestion.Requests
{
    public sealed class IngestionFileList : List<IngestionFile>
    {
        public IngestionFileList()
        {
        }

        public IngestionFileList(IEnumerable<IngestionFile> files) : base(files)
        {
        }
    }
}