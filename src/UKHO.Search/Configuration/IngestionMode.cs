namespace UKHO.Search.Configuration
{
    public enum IngestionMode
    {
        /// <summary>
        ///     Requires all ingested items to have a corresponding binary stream
        /// </summary>
        Strict = 0,

        /// <summary>
        ///     Allows ingestion to proceed even if some items do not have a corresponding binary stream
        /// </summary>
        BestEffort = 1
    }
}