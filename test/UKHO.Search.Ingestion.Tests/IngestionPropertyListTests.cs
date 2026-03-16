using System.Text.Json;
using Shouldly;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests
{
    public sealed class IngestionPropertyListTests
    {
        [Fact]
        public void Add_Rejects_Duplicate_Names_Ignoring_Case()
        {
            var list = new IngestionPropertyList();

            list.Add(new IngestionProperty
            {
                Name = "Week Number",
                Type = IngestionPropertyType.String,
                Value = "10"
            });

            Should.Throw<JsonException>(() =>
            {
                list.Add(new IngestionProperty
                {
                    Name = "week number",
                    Type = IngestionPropertyType.String,
                    Value = "11"
                });
            });

            list[0].Name.ShouldBe("week number");
        }

        [Fact]
        public void Add_Allows_Distinct_Names()
        {
            var list = new IngestionPropertyList();

            list.Add(new IngestionProperty
            {
                Name = "Year",
                Type = IngestionPropertyType.String,
                Value = "2026"
            });

            list.Add(new IngestionProperty
            {
                Name = "Week Number",
                Type = IngestionPropertyType.String,
                Value = "10"
            });

            list.Count.ShouldBe(2);
            list[0].Name.ShouldBe("year");
            list[1].Name.ShouldBe("week number");
        }

        [Fact]
        public void Add_Rejects_Empty_Name()
        {
            var list = new IngestionPropertyList();

            Should.Throw<JsonException>(() =>
            {
                list.Add(new IngestionProperty
                {
                    Name = " ",
                    Type = IngestionPropertyType.String,
                    Value = "x"
                });
            });
        }
    }
}
