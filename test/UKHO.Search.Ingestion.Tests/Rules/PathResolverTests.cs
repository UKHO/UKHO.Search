using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Rules.Evaluation;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Rules
{
    public sealed class PathResolverTests
    {
        [Fact]
        public void Resolve_id_returns_payload_id()
        {
            var payload = CreateAddItem();
            var resolver = new IngestionRulesPathResolver();

            resolver.Resolve(payload, "id")
                    .ShouldBe(new[] { "doc-1" });
        }

        [Fact]
        public void Resolve_files_wildcard_mimetype_returns_all_values()
        {
            var payload = CreateAddItem();
            var resolver = new IngestionRulesPathResolver();

            resolver.Resolve(payload, "files[*].mimeType")
                    .ShouldBe(new[] { "app/s63", "text/plain" });
        }

        [Fact]
        public void Resolve_properties_dot_name_returns_value()
        {
            var payload = CreateAddItem();
            var resolver = new IngestionRulesPathResolver();

            resolver.Resolve(payload, "properties.abcdef")
                    .ShouldBe(new[] { "a value" });
        }

        [Fact]
        public void Resolve_properties_bracket_name_returns_value()
        {
            var payload = CreateAddItem();
            var resolver = new IngestionRulesPathResolver();

            resolver.Resolve(payload, "properties[\"abcdef\"]")
                    .ShouldBe(new[] { "a value" });
        }

        private static AddItemRequest CreateAddItem()
        {
            return new AddItemRequest("doc-1", [
                new IngestionProperty { Name = "abcdef", Type = IngestionPropertyType.String, Value = "a value" }
            ], ["token"], DateTimeOffset.UtcNow, new IngestionFileList
            {
                new IngestionFile("f1", 1, DateTimeOffset.UtcNow, "app/s63"),
                new IngestionFile("f2", 1, DateTimeOffset.UtcNow, "text/plain")
            });
        }
    }
}