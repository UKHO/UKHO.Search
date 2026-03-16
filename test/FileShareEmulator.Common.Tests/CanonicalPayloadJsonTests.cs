using System.Text.Encodings.Web;
using System.Text.Json;
using FileShareEmulator.Common;
using Shouldly;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace FileShareEmulator.Common.Tests
{
    public sealed class CanonicalPayloadJsonTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        [Fact]
        public void CreateIndexIngestionRequest_SerializesDeterministically_ForSameInputs()
        {
            var attributes = new List<IngestionProperty>
            {
                new()
                {
                    Name = "Attribute1",
                    Type = IngestionPropertyType.String,
                    Value = "Value1"
                }
            };

            var files = new IngestionFileList();

            var request1 = FileShareIngestionMessageFactory.CreateIndexIngestionRequest(
                batchId: "batch-1",
                attributes: attributes,
                batchCreatedOn: DateTimeOffset.Parse("2026-03-16T00:00:00Z"),
                files: files,
                activeBusinessUnitName: "Sales");

            var request2 = FileShareIngestionMessageFactory.CreateIndexIngestionRequest(
                batchId: "batch-1",
                attributes: attributes,
                batchCreatedOn: DateTimeOffset.Parse("2026-03-16T00:00:00Z"),
                files: files,
                activeBusinessUnitName: "Sales");

            var json1 = JsonSerializer.Serialize(request1, JsonOptions);
            var json2 = JsonSerializer.Serialize(request2, JsonOptions);

            json1.ShouldBe(json2);
        }
    }
}
