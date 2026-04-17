using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;

namespace UKHO.Search.Infrastructure.Ingestion.Elastic
{
    /// <summary>
    /// Defines the Elasticsearch mapping for canonical ingestion documents.
    /// </summary>
    public sealed class CanonicalIndexDefinition
    {
        /// <summary>
        /// Configures the canonical index mapping on the supplied Elasticsearch descriptor.
        /// </summary>
        /// <param name="descriptor">The descriptor that will receive the canonical mapping.</param>
        /// <returns>The same descriptor populated with the canonical field definitions.</returns>
        public CreateIndexRequestDescriptor Configure(CreateIndexRequestDescriptor descriptor)
        {
            // Keep exact-match fields on keyword mappings and reserve text analysis for the analyzed search surfaces.
            return descriptor.Mappings(m => m.Properties(p => p.Object("source", o => o.Enabled(false))
                                                               .Keyword("provider")
                                                               .Keyword("title")
                                                               .Date("timestamp")
                                                               .Keyword("keywords")
                                                               .Keyword("securityTokens")
                                                               .Keyword("authority")
                                                               .Keyword("region")
                                                               .Keyword("format")
                                                               .Keyword("majorVersion")
                                                               .Keyword("minorVersion")
                                                               .Keyword("category")
                                                               .Keyword("series")
                                                               .Keyword("instance")
                                                               .Text("searchText", t => t.Analyzer("english"))
                                                               .Text("content", t => t.Analyzer("english"))
                                                                .GeoShape("geoPolygons")));
        }
    }
}