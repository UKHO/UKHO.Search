using System.Text.Json;
using Shouldly;
using UKHO.Search.Ingestion;
using UKHO.Search.Ingestion.Serialization;
using Xunit;

namespace UKHO.Search.Tests;

public sealed class IngestionModelJsonTests
{
    private static readonly JsonSerializerOptions Options = IngestionJsonSerializerOptions.Create();

    [Theory]
    [InlineData(IngestionPropertyType.String, "string")]
    [InlineData(IngestionPropertyType.Integer, "integer")]
    [InlineData(IngestionPropertyType.Double, "double")]
    [InlineData(IngestionPropertyType.Decimal, "decimal")]
    [InlineData(IngestionPropertyType.Boolean, "boolean")]
    [InlineData(IngestionPropertyType.DateTime, "datetime")]
    [InlineData(IngestionPropertyType.TimeSpan, "timespan")]
    [InlineData(IngestionPropertyType.Id, "id")]
    [InlineData(IngestionPropertyType.Guid, "guid")]
    [InlineData(IngestionPropertyType.Uri, "uri")]
    [InlineData(IngestionPropertyType.StringArray, "string-array")]
    public void IngestionPropertyType_Serializes_ToLowercaseTokens(IngestionPropertyType type, string expected)
    {
        var json = JsonSerializer.Serialize(type, Options);
        json.ShouldBe($"\"{expected}\"");
    }

    [Fact]
    public void IngestionPropertyType_Deserialize_IsCaseSensitive()
    {
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionPropertyType>("\"String\"", Options));
    }

    [Fact]
    public void RoundTrip_AllSupportedTypes_Succeeds()
    {
        var request = new IngestionRequest
        {
            DataCallback = new Uri("https://example.test/callback/123"),
            Properties =
            [
                new IngestionProperty { Name = "String", Type = IngestionPropertyType.String, Value = "hello" },
                new IngestionProperty { Name = "Id", Type = IngestionPropertyType.Id, Value = "123456ID" },
                new IngestionProperty { Name = "Int64Min", Type = IngestionPropertyType.Integer, Value = long.MinValue },
                new IngestionProperty { Name = "Int64Max", Type = IngestionPropertyType.Integer, Value = long.MaxValue },
                new IngestionProperty { Name = "Double", Type = IngestionPropertyType.Double, Value = 1234.5d },
                new IngestionProperty { Name = "Decimal", Type = IngestionPropertyType.Decimal, Value = 79228162514264337593543950335m },
                new IngestionProperty { Name = "Boolean", Type = IngestionPropertyType.Boolean, Value = true },
                new IngestionProperty { Name = "DateTime", Type = IngestionPropertyType.DateTime, Value = new DateTimeOffset(2026, 3, 5, 10, 15, 30, TimeSpan.Zero) },
                new IngestionProperty { Name = "TimeSpan", Type = IngestionPropertyType.TimeSpan, Value = TimeSpan.FromMinutes(15) },
                new IngestionProperty { Name = "Guid", Type = IngestionPropertyType.Guid, Value = Guid.Parse("bfb8feca-5f5f-4f55-aa0c-7d7b00b19b46") },
                new IngestionProperty { Name = "Uri", Type = IngestionPropertyType.Uri, Value = new Uri("https://example.test/resource/123") },
                new IngestionProperty { Name = "StringArrayEmpty", Type = IngestionPropertyType.StringArray, Value = Array.Empty<string>() },
                new IngestionProperty { Name = "StringArray", Type = IngestionPropertyType.StringArray, Value = new[] { "a", "b" } },
            ],
        };

        var json = JsonSerializer.Serialize(request, Options);
        json.ShouldNotContain("null");

        var hydrated = JsonSerializer.Deserialize<IngestionRequest>(json, Options);
        hydrated.ShouldNotBeNull();

        hydrated!.DataCallback.ShouldBe(new Uri("https://example.test/callback/123"));
        hydrated.Properties.Count.ShouldBe(request.Properties.Count);

        hydrated.TryGetString("string", out var s).ShouldBeTrue();
        s.ShouldBe("hello");

        hydrated.TryGetId("id", out var id).ShouldBeTrue();
        id.ShouldBe("123456ID");

        hydrated.TryGetInt64("int64min", out var iMin).ShouldBeTrue();
        iMin.ShouldBe(long.MinValue);

        hydrated.TryGetInt64("INT64MAX", out var iMax).ShouldBeTrue();
        iMax.ShouldBe(long.MaxValue);

        hydrated.TryGetDouble("double", out var dbl).ShouldBeTrue();
        dbl.ShouldBe(1234.5d);

        hydrated.TryGetDecimal("decimal", out var dec).ShouldBeTrue();
        dec.ShouldBe(79228162514264337593543950335m);

        hydrated.TryGetBoolean("boolean", out var b).ShouldBeTrue();
        b.ShouldBeTrue();

        hydrated.TryGetDateTimeOffset("datetime", out var dto).ShouldBeTrue();
        dto.ShouldBe(new DateTimeOffset(2026, 3, 5, 10, 15, 30, TimeSpan.Zero));

        hydrated.TryGetTimeSpan("timespan", out var ts).ShouldBeTrue();
        ts.ShouldBe(TimeSpan.FromMinutes(15));

        hydrated.TryGetGuid("guid", out var g).ShouldBeTrue();
        g.ShouldBe(Guid.Parse("bfb8feca-5f5f-4f55-aa0c-7d7b00b19b46"));

        hydrated.TryGetUri("uri", out var uri).ShouldBeTrue();
        uri.ShouldBe(new Uri("https://example.test/resource/123"));

        hydrated.TryGetStringArray("stringarrayempty", out var empty).ShouldBeTrue();
        empty.ShouldNotBeNull();
        empty!.Length.ShouldBe(0);

        hydrated.TryGetStringArray("stringarray", out var arr).ShouldBeTrue();
        arr.ShouldBe(new[] { "a", "b" });
    }

    [Fact]
    public void IngestionProperty_Rejects_MissingName()
    {
        var json = "{\"Type\":\"string\",\"Value\":\"abc\"}";
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionProperty>(json, Options));
    }

    [Fact]
    public void IngestionProperty_Rejects_MissingType()
    {
        var json = "{\"Name\":\"A\",\"Value\":\"abc\"}";
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionProperty>(json, Options));
    }

    [Fact]
    public void IngestionProperty_Rejects_MissingValue()
    {
        var json = "{\"Name\":\"A\",\"Type\":\"string\"}";
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionProperty>(json, Options));
    }

    [Theory]
    [InlineData("integer", "\"abc\"")]
    [InlineData("double", "\"abc\"")]
    [InlineData("decimal", "\"abc\"")]
    [InlineData("boolean", "123")]
    [InlineData("datetime", "123")]
    [InlineData("timespan", "123")]
    [InlineData("guid", "\"not-a-guid\"")]
    [InlineData("uri", "\"not-a-uri\"")]
    [InlineData("string-array", "\"not-array\"")]
    public void IngestionProperty_Rejects_MismatchedTypeAndValue(string type, string jsonValue)
    {
        var json = $"{{\"Name\":\"A\",\"Type\":\"{type}\",\"Value\":{jsonValue}}}";
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionProperty>(json, Options));
    }

    [Fact]
    public void Integer_Rejects_Fractional()
    {
        var json = "{\"Name\":\"A\",\"Type\":\"integer\",\"Value\":1.23}";
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionProperty>(json, Options));
    }

    [Fact]
    public void Integer_Rejects_OutOfRange()
    {
        var json = "{\"Name\":\"A\",\"Type\":\"integer\",\"Value\":9223372036854775808}";
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionProperty>(json, Options));
    }

    [Fact]
    public void Uri_Rejects_Relative()
    {
        var json = "{\"Name\":\"A\",\"Type\":\"uri\",\"Value\":\"/relative\"}";
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionProperty>(json, Options));
    }

    [Fact]
    public void StringArray_Rejects_NullElements()
    {
        var json = "{\"Name\":\"A\",\"Type\":\"string-array\",\"Value\":[\"a\",null]}";
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionProperty>(json, Options));
    }

    [Fact]
    public void DataCallback_MustBeAbsoluteUri()
    {
        var json = "{\"Properties\":[],\"DataCallback\":\"/relative\"}";
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionRequest>(json, Options));
    }
}
