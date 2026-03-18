using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Elastic;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Elastic
{
    public sealed class ElasticsearchBulkIndexClientMappingValidationTests
    {
        [Fact]
        public void ValidateExpectedFieldMappings_when_documentId_is_absent_should_succeed()
        {
            var fields = new Dictionary<string, IReadOnlyDictionary<string, object>>(StringComparer.Ordinal)
            {
                ["keywords"] = CreateTypes("keyword"),
                ["authority"] = CreateTypes("keyword"),
                ["region"] = CreateTypes("keyword"),
                ["format"] = CreateTypes("keyword"),
                ["majorVersion"] = CreateTypes("keyword"),
                ["minorVersion"] = CreateTypes("keyword"),
                ["category"] = CreateTypes("keyword"),
                ["series"] = CreateTypes("keyword"),
                ["instance"] = CreateTypes("keyword"),
                ["searchText"] = CreateTypes("text"),
                ["content"] = CreateTypes("text")
            };

            Should.NotThrow(() => ElasticsearchBulkIndexClient.ValidateExpectedFieldMappings(fields));
        }

        private static IReadOnlyDictionary<string, object> CreateTypes(params string[] types)
        {
            return types.ToDictionary(type => type, _ => new object(), StringComparer.Ordinal);
        }
    }
}
