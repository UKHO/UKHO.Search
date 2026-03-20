using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;

namespace UKHO.Search.Infrastructure.Ingestion.Elastic
{
    public sealed class CanonicalIndexDefinition
    {
        public CreateIndexRequestDescriptor Configure(CreateIndexRequestDescriptor descriptor)
        {
            return descriptor.Mappings(m => m.Properties(p => p.Object("source", o => o.Enabled(false))
                                                               .Keyword("provider")
                                                               .Date("timestamp")
                                                               .Keyword("keywords")
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