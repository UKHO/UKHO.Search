using System.Text.Json;
using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Documents
{
    public sealed class CanonicalDocumentJsonRoundTripTests
    {
        [Fact]
        public void CanonicalDocument_round_trips_via_system_text_json()
        {
            var timestamp = new DateTimeOffset(2024, 01, 02, 03, 04, 05, TimeSpan.Zero);
            var source = new[]
            {
                new IngestionProperty { Name = "Category", Type = IngestionPropertyType.String, Value = "A" }
            };

            var doc = CanonicalDocument.CreateMinimal("doc-1", source, timestamp);
            doc.DocumentType = "type-x";
            doc.AddKeyword("Alpha");
            doc.AddKeyword("BETA");
            doc.SetSearchText("Hello WORLD");
            doc.SetContent("Hello BODY");
            doc.AddFacetValue("Category", "A");
            doc.AddFacetValue("Category", "B");

            var json = JsonSerializer.Serialize(doc);

            using var parsed = JsonDocument.Parse(json);
            parsed.RootElement.GetProperty("Source").ValueKind.ShouldBe(JsonValueKind.Array);
            parsed.RootElement.GetProperty("Timestamp").ValueKind.ShouldBe(JsonValueKind.String);
            parsed.RootElement.GetProperty("Keywords").ValueKind.ShouldBe(JsonValueKind.Array);
            parsed.RootElement.GetProperty("SearchText").ValueKind.ShouldBe(JsonValueKind.String);
            parsed.RootElement.GetProperty("Content").ValueKind.ShouldBe(JsonValueKind.String);
            parsed.RootElement.GetProperty("Facets").ValueKind.ShouldBe(JsonValueKind.Object);
            parsed.RootElement.GetProperty("Facets").GetProperty("category").ValueKind.ShouldBe(JsonValueKind.Array);

            var roundTripped = JsonSerializer.Deserialize<CanonicalDocument>(json);
            roundTripped.ShouldNotBeNull();

            roundTripped!.DocumentId.ShouldBe("doc-1");
            roundTripped.DocumentType.ShouldBe("type-x");
            roundTripped.Source.Count.ShouldBe(1);
            roundTripped.Source[0].Name.ShouldBe("Category");
            roundTripped.Source[0].Type.ShouldBe(IngestionPropertyType.String);
            roundTripped.Source[0].Value.ShouldBe("A");
            roundTripped.Timestamp.ShouldBe(timestamp);

            roundTripped.Keywords.ShouldBe(new[] { "alpha", "beta" });
            roundTripped.SearchText.ShouldBe("hello world");
            roundTripped.Content.ShouldBe("hello body");
            roundTripped.Facets["category"].ShouldBe(new[] { "a", "b" });
        }
    }
}
