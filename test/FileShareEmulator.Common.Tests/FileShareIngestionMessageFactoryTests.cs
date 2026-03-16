using FileShareEmulator.Common;
using Shouldly;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace FileShareEmulator.Common.Tests
{
    public sealed class FileShareIngestionMessageFactoryTests
    {
        [Fact]
        public void CreateIndexIngestionRequest_SetsBusinessUnitNamePropertyAndStrictTokens_WhenBuProvided()
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

            var request = FileShareIngestionMessageFactory.CreateIndexIngestionRequest(
                batchId: "batch-1",
                attributes: attributes,
                batchCreatedOn: DateTimeOffset.Parse("2026-03-16T00:00:00Z"),
                files: files,
                activeBusinessUnitName: "Sales");

            request.RequestType.ShouldBe(IngestionRequestType.IndexItem);
            request.IndexItem.ShouldNotBeNull();

            request.IndexItem!.SecurityTokens.ShouldBe(["batchcreate", "batchcreate_sales", "public"]);

            var buProperty = request.IndexItem.Properties.Single(p => p.Name == "businessunitname");
            buProperty.Value.ShouldBeOfType<string>().ShouldBe("Sales");
        }

        [Fact]
        public void CreateIndexIngestionRequest_UsesEmptyBusinessUnitNameAndStrictTokens_WhenBuMissing()
        {
            var attributes = new List<IngestionProperty>();
            var files = new IngestionFileList();

            var request = FileShareIngestionMessageFactory.CreateIndexIngestionRequest(
                batchId: "batch-1",
                attributes: attributes,
                batchCreatedOn: DateTimeOffset.Parse("2026-03-16T00:00:00Z"),
                files: files,
                activeBusinessUnitName: null);

            request.IndexItem!.SecurityTokens.ShouldBe(["batchcreate", "public"]);
            var buProperty = request.IndexItem.Properties.Single(p => p.Name == "businessunitname");
            buProperty.Value.ShouldBeOfType<string>().ShouldBe(string.Empty);
        }
    }
}
