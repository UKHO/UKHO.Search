using Elastic.Clients.Elasticsearch.IndexManagement;

namespace UKHO.Search.Infrastructure.Ingestion.Elastic
{
    public sealed class CanonicalIndexDefinition
    {
        public CreateIndexRequestDescriptor Configure(CreateIndexRequestDescriptor descriptor)
        {
            return descriptor.Mappings(m => m.Properties(p => p
                .Object("source", o => o.Enabled(false))
                .Date("timestamp")
                .Keyword("keywords")
                .Text("searchText", t => t.Analyzer("english"))
                .Text("content", t => t.Analyzer("english"))
                .Flattened("facets")));
        }
    }
}
