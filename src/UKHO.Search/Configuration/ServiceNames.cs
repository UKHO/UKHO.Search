namespace UKHO.Search.Configuration
{
    /// <summary>
    /// Defines the canonical Aspire resource and service identifiers that remain part of the active UKHO Search workflow.
    /// </summary>
    public static class ServiceNames
    {
        /// <summary>
        /// Gets the service identifier used for the Keycloak identity container.
        /// </summary>
        public const string KeyCloak = "keycloak";

        /// <summary>
        /// Gets the service identifier used for the shared Azure Storage emulator container.
        /// </summary>
        public const string Storage = "storage";

        /// <summary>
        /// Gets the service identifier used for the Azure Queue Storage resource.
        /// </summary>
        public const string Queues = "queues";

        /// <summary>
        /// Gets the service identifier used for the Azure Table Storage resource.
        /// </summary>
        public const string Tables = "tables";

        /// <summary>
        /// Gets the service identifier used for the Azure Blob Storage resource.
        /// </summary>
        public const string Blobs = "blobs";

        /// <summary>
        /// Gets the service identifier used for the SQL Server container.
        /// </summary>
        public const string SqlServer = "sql-server";

        /// <summary>
        /// Gets the service identifier used for the Elasticsearch container.
        /// </summary>
        public const string ElasticSearch = "elasticsearch";

        /// <summary>
        /// Gets the service identifier used for the shared configuration source.
        /// </summary>
        public const string Configuration = "configuration";

        /// <summary>
        /// Gets the service identifier used for the ingestion host.
        /// </summary>
        public const string Ingestion = "ingestion";

        /// <summary>
        /// Gets the service identifier used for the query host.
        /// </summary>
        public const string Query = "query";

        /// <summary>
        /// Gets the service identifier used for the file-share emulator tool.
        /// </summary>
        public const string FileShareEmulator = "tools-fileshare-emulator";

        /// <summary>
        /// Gets the service identifier used for the file-share export image builder tool.
        /// </summary>
        public const string FileShareBuilder = "tools-fileshare-builder";

        /// <summary>
        /// Gets the service identifier used for the file-share import image loader tool.
        /// </summary>
        public const string FileShareLoader = "tools-fileshare-loader";

        /// <summary>
        /// Gets the service identifier used for the rules workbench tool.
        /// </summary>
        public const string RulesWorkbench = "tools-rules-workbench";

    }
}