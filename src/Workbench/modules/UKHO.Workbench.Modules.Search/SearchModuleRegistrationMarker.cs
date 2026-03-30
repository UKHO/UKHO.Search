namespace UKHO.Workbench.Modules.Search
{
    /// <summary>
    /// Represents a marker service registered by the Search module to prove module DI participation during startup.
    /// </summary>
    internal class SearchModuleRegistrationMarker
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchModuleRegistrationMarker"/> class.
        /// </summary>
        public SearchModuleRegistrationMarker()
        {
            // The marker carries no runtime behavior because the current slice only needs to prove module service registration is possible.
        }
    }
}
