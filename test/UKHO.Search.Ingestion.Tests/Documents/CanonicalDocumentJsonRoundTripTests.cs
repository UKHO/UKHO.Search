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
            var request = new IngestionRequest(IngestionRequestType.AddItem, new AddItemRequest("doc-1", Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList()), null, null, null);
            var doc = CanonicalDocument.CreateMinimal("doc-1", request);
            doc.DocumentType = "type-x";
            doc.AddKeyword("Alpha");
            doc.AddKeyword("BETA");
            doc.SetSearchText("Hello WORLD");
            doc.AddFacetValue("Category", "A");
            doc.AddFacetValue("Category", "B");

            var json = JsonSerializer.Serialize(doc);

            using var parsed = JsonDocument.Parse(json);
            parsed.RootElement.GetProperty("Keywords").ValueKind.ShouldBe(JsonValueKind.Array);
            parsed.RootElement.GetProperty("SearchText").ValueKind.ShouldBe(JsonValueKind.String);
            parsed.RootElement.GetProperty("Facets").ValueKind.ShouldBe(JsonValueKind.Object);
            parsed.RootElement.GetProperty("Facets").GetProperty("category").ValueKind.ShouldBe(JsonValueKind.Array);

            var roundTripped = JsonSerializer.Deserialize<CanonicalDocument>(json);
            roundTripped.ShouldNotBeNull();

            roundTripped!.DocumentId.ShouldBe("doc-1");
            roundTripped.DocumentType.ShouldBe("type-x");
            roundTripped.Source.RequestType.ShouldBe(IngestionRequestType.AddItem);

            roundTripped.Keywords.ShouldBe(new[] { "alpha", "beta" });
            roundTripped.SearchText.ShouldBe("hello world");
            roundTripped.Facets["category"].ShouldBe(new[] { "a", "b" });
        }
    }
}
