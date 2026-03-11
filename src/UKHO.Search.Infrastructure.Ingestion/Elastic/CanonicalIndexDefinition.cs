using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;

namespace UKHO.Search.Infrastructure.Ingestion.Elastic
{
    public sealed class CanonicalIndexDefinition
    {
        public CreateIndexRequestDescriptor Configure(CreateIndexRequestDescriptor descriptor)
        {
            return descriptor.Mappings(m => m.DynamicTemplates(new[]
                                             {
                                                 new KeyValuePair<string, DynamicTemplate>("facets_as_keyword", new DynamicTemplate
                                                 {
                                                     PathMatch = new[] { "facets.*" },
                                                     Mapping = new KeywordProperty()
                                                 })
                                             })
                                             .Properties(p => p.Object("source", o => o.Enabled(false))
                                                               .Date("timestamp")
                                                               .Keyword("keywords")
                                                               .Text("searchText", t => t.Analyzer("english"))
                                                               .Text("content", t => t.Analyzer("english"))
                                                               .Object("facets", o => o.Dynamic(DynamicMapping.True))));
        }
    }
}