using System.Text.Json;
using Shouldly;
using UKHO.Search.Geo;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Ingestion.Requests.Serialization;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Documents
{
    public sealed class CanonicalDocumentJsonRoundTripTests
    {
        [Fact]
        public void CanonicalDocument_round_trips_via_system_text_json()
        {
            var timestamp = new DateTimeOffset(2024, 01, 02, 03, 04, 05, TimeSpan.Zero);
            var source = new IndexRequest("doc-1", new[]
            {
                new IngestionProperty { Name = "Category", Type = IngestionPropertyType.String, Value = "A" }
            }, ["t"], timestamp, new IngestionFileList());

            var doc = CanonicalDocument.CreateMinimal("doc-1", "file-share", source, timestamp);
            doc.AddKeyword("Alpha");
            doc.AddKeyword("BETA");
            doc.AddSearchText("Hello WORLD");
            doc.AddContent("Hello BODY");
            doc.AddGeoPolygon(GeoPolygon.Create(new[]
            {
                GeoCoordinate.Create(0d, 0d),
                GeoCoordinate.Create(1d, 0d),
                GeoCoordinate.Create(1d, 1d),
                GeoCoordinate.Create(0d, 0d)
            }));

            var json = JsonSerializer.Serialize(doc);

            using var parsed = JsonDocument.Parse(json);
            parsed.RootElement.GetProperty("Source")
                  .ValueKind.ShouldBe(JsonValueKind.Object);
            parsed.RootElement.GetProperty("Timestamp")
                  .ValueKind.ShouldBe(JsonValueKind.String);
            parsed.RootElement.GetProperty("Provider")
                  .ValueKind.ShouldBe(JsonValueKind.String);
            parsed.RootElement.GetProperty("Keywords")
                  .ValueKind.ShouldBe(JsonValueKind.Array);
            parsed.RootElement.GetProperty("SearchText")
                  .ValueKind.ShouldBe(JsonValueKind.String);
            parsed.RootElement.GetProperty("Content")
                  .ValueKind.ShouldBe(JsonValueKind.String);

            parsed.RootElement.GetProperty("GeoPolygons")
                  .ValueKind.ShouldBe(JsonValueKind.Array);

            var roundTripped = JsonSerializer.Deserialize<CanonicalDocument>(json, IngestionJsonSerializerOptions.Create());
            roundTripped.ShouldNotBeNull();

            roundTripped!.Id.ShouldBe("doc-1");
            roundTripped.Provider.ShouldBe("file-share");
            roundTripped.Source.Properties.Count.ShouldBe(1);
            roundTripped.Source.Properties[0]
                        .Name.ShouldBe("category");
            roundTripped.Source.Properties[0]
                        .Type.ShouldBe(IngestionPropertyType.String);
            roundTripped.Source.Properties[0]
                        .Value.ShouldBe("A");
            roundTripped.Timestamp.ShouldBe(timestamp);

            roundTripped.Keywords.ShouldBe(new[] { "alpha", "beta" });
            roundTripped.SearchText.ShouldBe("hello world");
            roundTripped.Content.ShouldBe("hello body");

            roundTripped.GeoPolygons.Count.ShouldBe(1);
            roundTripped.GeoPolygons[0].Rings.Count.ShouldBe(1);
            roundTripped.GeoPolygons[0].Rings[0].Count.ShouldBe(4);
        }
    }
}