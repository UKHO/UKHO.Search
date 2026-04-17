using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Elastic;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Elastic
{
    /// <summary>
    /// Verifies the defensive mapping validation that protects the canonical index against dynamic-mapping drift.
    /// </summary>
    public sealed class ElasticsearchBulkIndexClientMappingValidationTests
    {
        /// <summary>
        /// Confirms that the validator accepts the canonical mapping when all expected exact-match fields are present.
        /// </summary>
        [Fact]
        public void ValidateExpectedFieldMappings_when_documentId_is_absent_should_succeed()
        {
            var fields = new Dictionary<string, IReadOnlyDictionary<string, object>>(StringComparer.Ordinal)
            {
                ["provider"] = CreateTypes("keyword"),
                ["keywords"] = CreateTypes("keyword"),
                ["securityTokens"] = CreateTypes("keyword"),
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

        /// <summary>
        /// Creates the minimal field-capabilities shape needed by the mapping validator.
        /// </summary>
        private static IReadOnlyDictionary<string, object> CreateTypes(params string[] types)
        {
            return types.ToDictionary(type => type, _ => new object(), StringComparer.Ordinal);
        }
    }
}
