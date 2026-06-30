namespace UKHO.Search.Ingestion.Contracts
{
    /// <summary>
    /// Represents the file collection carried by an indexing payload.
    /// </summary>
    public sealed class IngestionFileList : List<IngestionFile>
    {
        /// <summary>
        /// Initializes an empty file list.
        /// </summary>
        public IngestionFileList()
        {
        }

        /// <summary>
        /// Initializes the file list from an existing sequence.
        /// </summary>
        /// <param name="files">
        /// The files to add to the list.
        /// </param>
        public IngestionFileList(IEnumerable<IngestionFile> files)
            : base(files)
        {
        }
    }
}