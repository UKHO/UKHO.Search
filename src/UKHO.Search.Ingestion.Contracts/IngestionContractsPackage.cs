namespace UKHO.Search.Ingestion.Contracts
{
    /// <summary>
    /// Provides public identity values for the contracts package and its compatibility surface.
    /// </summary>
    public static class IngestionContractsPackage
    {
        /// <summary>
        /// Gets the canonical package identifier that remote producers and internal runtime projects will depend on.
        /// </summary>
        public static string PackageId
        {
            get
            {
                // Keep the package identifier in one place so boundary-oriented tooling and documentation
                // can reference a single canonical value.
                return "UKHO.Search.Ingestion.Contracts";
            }
        }

        /// <summary>
        /// Gets the visible queue-message contract version marker for producer guidance, tests, and compatibility discussions.
        /// </summary>
        public static string ContractVersion
        {
            get
            {
                // Start with a simple visible marker so callers can reference the current helper-era contract surface
                // without introducing a heavier compatibility object model before it is needed.
                return "1.0";
            }
        }
    }
}