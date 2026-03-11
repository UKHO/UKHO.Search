using System.Text.Json;
using Shouldly;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Ingestion.Requests.Serialization;
using Xunit;

namespace UKHO.Search.Ingestion.Tests
{
    public sealed class IngestionModelJsonTests
    {
        private static readonly JsonSerializerOptions _options = IngestionJsonSerializerOptions.Create();

        [Fact]
        public void IngestionRequestEnvelope_RoundTrips_DeleteItem()
        {
            var envelope = new IngestionRequest
            {
                RequestType = IngestionRequestType.DeleteItem,
                DeleteItem = new DeleteItemRequest { Id = "ABC123" }
            };

            var json = JsonSerializer.Serialize(envelope, _options);
            var hydrated = JsonSerializer.Deserialize<IngestionRequest>(json, _options);
            hydrated.ShouldNotBeNull();
            hydrated.RequestType.ShouldBe(IngestionRequestType.DeleteItem);
            hydrated.DeleteItem.ShouldNotBeNull();
            hydrated.DeleteItem!.Id.ShouldBe("ABC123");
        }

        [Fact]
        public void IngestionRequestEnvelope_RoundTrips_UpdateItem()
        {
            var envelope = new IngestionRequest
            {
                RequestType = IngestionRequestType.UpdateItem,
                UpdateItem = new UpdateItemRequest
                {
                    Id = "ABC123",
                    SecurityTokens = ["token-a"],
                    Timestamp = new DateTimeOffset(2026, 3, 5, 10, 15, 30, TimeSpan.Zero),
                    Files = new IngestionFileList(),
                    Properties = [new IngestionProperty { Name = "Title", Type = IngestionPropertyType.String, Value = "Updated" }]
                }
            };

            var json = JsonSerializer.Serialize(envelope, _options);
            var hydrated = JsonSerializer.Deserialize<IngestionRequest>(json, _options);
            hydrated.ShouldNotBeNull();
            hydrated.RequestType.ShouldBe(IngestionRequestType.UpdateItem);
            hydrated.UpdateItem.ShouldNotBeNull();
            hydrated.UpdateItem!.Id.ShouldBe("ABC123");
            hydrated.UpdateItem.SecurityTokens.ShouldBe(["token-a"]);
        }

        [Fact]
        public void IngestionRequestEnvelope_RoundTrips_UpdateAcl()
        {
            var envelope = new IngestionRequest
            {
                RequestType = IngestionRequestType.UpdateAcl,
                UpdateAcl = new UpdateAclRequest
                {
                    Id = "ABC123",
                    SecurityTokens = ["token-a", "token-b"]
                }
            };

            var json = JsonSerializer.Serialize(envelope, _options);
            var hydrated = JsonSerializer.Deserialize<IngestionRequest>(json, _options);
            hydrated.ShouldNotBeNull();
            hydrated.RequestType.ShouldBe(IngestionRequestType.UpdateAcl);
            hydrated.UpdateAcl.ShouldNotBeNull();
            hydrated.UpdateAcl!.Id.ShouldBe("ABC123");
            hydrated.UpdateAcl.SecurityTokens.ShouldBe(["token-a", "token-b"]);
        }

        [Fact]
        public void AddItemRequest_Rejects_EmptySecurityTokens()
        {
            var json = "{" + "\"Id\":\"ABC123\"," + "\"Timestamp\":\"2026-03-05T10:15:30+00:00\"," + "\"Files\":[]," + "\"Properties\":[]," + "\"SecurityTokens\":[]" + "}";
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<AddItemRequest>(json, _options));
        }

        [Fact]
        public void UpdateItemRequest_Rejects_EmptySecurityTokens()
        {
            var json = "{" + "\"Id\":\"ABC123\"," + "\"Timestamp\":\"2026-03-05T10:15:30+00:00\"," + "\"Files\":[]," + "\"Properties\":[]," + "\"SecurityTokens\":[]" + "}";
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<UpdateItemRequest>(json, _options));
        }

        [Fact]
        public void AddItemRequest_Accepts_EmptyFiles()
        {
            var json = "{" + "\"Id\":\"ABC123\"," + "\"Timestamp\":\"2026-03-05T10:15:30+00:00\"," + "\"Files\":[]," + "\"Properties\":[]," + "\"SecurityTokens\":[\"t\"]" + "}";
            var hydrated = JsonSerializer.Deserialize<AddItemRequest>(json, _options);
            hydrated.ShouldNotBeNull();
            hydrated!.Timestamp.ShouldBe(new DateTimeOffset(2026, 3, 5, 10, 15, 30, TimeSpan.Zero));
            hydrated.Files.Count.ShouldBe(0);
        }

        [Fact]
        public void AddItemRequest_Rejects_MissingFiles()
        {
            var json = "{" + "\"Id\":\"ABC123\"," + "\"Timestamp\":\"2026-03-05T10:15:30+00:00\"," + "\"Properties\":[]," + "\"SecurityTokens\":[\"t\"]" + "}";
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<AddItemRequest>(json, _options));
        }

        [Fact]
        public void AddItemRequest_Rejects_NullFiles()
        {
            var json = "{" + "\"Id\":\"ABC123\"," + "\"Timestamp\":\"2026-03-05T10:15:30+00:00\"," + "\"Files\":null," + "\"Properties\":[]," + "\"SecurityTokens\":[\"t\"]" + "}";
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<AddItemRequest>(json, _options));
        }

        [Fact]
        public void AddItemRequest_Rejects_MissingTimestamp()
        {
            var json = "{" + "\"Id\":\"ABC123\"," + "\"Files\":[]," + "\"Properties\":[]," + "\"SecurityTokens\":[\"t\"]" + "}";
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<AddItemRequest>(json, _options));
        }

        [Fact]
        public void UpdateItemRequest_Accepts_EmptyFiles()
        {
            var json = "{" + "\"Id\":\"ABC123\"," + "\"Timestamp\":\"2026-03-05T10:15:30+00:00\"," + "\"Files\":[]," + "\"Properties\":[]," + "\"SecurityTokens\":[\"t\"]" + "}";
            var hydrated = JsonSerializer.Deserialize<UpdateItemRequest>(json, _options);
            hydrated.ShouldNotBeNull();
            hydrated!.Timestamp.ShouldBe(new DateTimeOffset(2026, 3, 5, 10, 15, 30, TimeSpan.Zero));
            hydrated.Files.Count.ShouldBe(0);
        }

        [Fact]
        public void UpdateItemRequest_Rejects_MissingFiles()
        {
            var json = "{" + "\"Id\":\"ABC123\"," + "\"Timestamp\":\"2026-03-05T10:15:30+00:00\"," + "\"Properties\":[]," + "\"SecurityTokens\":[\"t\"]" + "}";
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<UpdateItemRequest>(json, _options));
        }

        [Fact]
        public void UpdateItemRequest_Rejects_NullFiles()
        {
            var json = "{" + "\"Id\":\"ABC123\"," + "\"Timestamp\":\"2026-03-05T10:15:30+00:00\"," + "\"Files\":null," + "\"Properties\":[]," + "\"SecurityTokens\":[\"t\"]" + "}";
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<UpdateItemRequest>(json, _options));
        }

        [Fact]
        public void UpdateItemRequest_Rejects_MissingTimestamp()
        {
            var json = "{" + "\"Id\":\"ABC123\"," + "\"Files\":[]," + "\"Properties\":[]," + "\"SecurityTokens\":[\"t\"]" + "}";
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<UpdateItemRequest>(json, _options));
        }

        [Fact]
        public void IngestionRequestEnvelope_GoldenJson_AddItem_Deserializes()
        {
            var json = "{" + "\"RequestType\":\"AddItem\"," + "\"AddItem\":{" + "\"Id\":\"ABC123\"," + "\"Timestamp\":\"2026-03-05T10:15:30+00:00\"," + "\"Files\":[{" + "\"Filename\":\"a.txt\"," + "\"Size\":123," + "\"Timestamp\":\"2026-03-05T10:15:31+00:00\"," + "\"MimeType\":\"text/plain\"" + "}]," + "\"Properties\":[{" + "\"Name\":\"Title\"," + "\"Type\":\"string\"," + "\"Value\":\"Hello\"" +
                "}]," + "\"SecurityTokens\":[\"t\"]" + "}" + "}";

            var hydrated = JsonSerializer.Deserialize<IngestionRequest>(json, _options);
            hydrated.ShouldNotBeNull();
            hydrated!.RequestType.ShouldBe(IngestionRequestType.AddItem);
            hydrated.AddItem.ShouldNotBeNull();

            hydrated.AddItem!.Timestamp.ShouldBe(new DateTimeOffset(2026, 3, 5, 10, 15, 30, TimeSpan.Zero));
            hydrated.AddItem.Files.Count.ShouldBe(1);
            hydrated.AddItem.Files[0]
                    .Filename.ShouldBe("a.txt");
            hydrated.AddItem.Files[0]
                    .Size.ShouldBe(123);
            hydrated.AddItem.Files[0]
                    .Timestamp.ShouldBe(new DateTimeOffset(2026, 3, 5, 10, 15, 31, TimeSpan.Zero));
            hydrated.AddItem.Files[0]
                    .MimeType.ShouldBe("text/plain");
        }

        [Fact]
        public void IngestionRequestEnvelope_Serializes_Files_AsJsonArray()
        {
            var envelope = new IngestionRequest
            {
                RequestType = IngestionRequestType.AddItem,
                AddItem = new AddItemRequest
                {
                    Id = "ABC123",
                    Timestamp = new DateTimeOffset(2026, 3, 5, 10, 15, 30, TimeSpan.Zero),
                    Files = new IngestionFileList
                    {
                        new IngestionFile
                        {
                            Filename = "a.txt",
                            Size = 123,
                            Timestamp = new DateTimeOffset(2026, 3, 5, 10, 15, 31, TimeSpan.Zero),
                            MimeType = "text/plain"
                        }
                    },
                    Properties = Array.Empty<IngestionProperty>(),
                    SecurityTokens = ["t"]
                }
            };

            var json = JsonSerializer.Serialize(envelope, _options);
            json.ShouldContain("\"Files\":[");
            json.ShouldNotContain("\"Files\":{");
        }

        [Fact]
        public void IngestionFile_RoundTrips()
        {
            var file = new IngestionFile
            {
                Filename = "a.txt",
                Size = 123,
                Timestamp = new DateTimeOffset(2026, 3, 5, 10, 15, 30, TimeSpan.Zero),
                MimeType = "text/plain"
            };

            var json = JsonSerializer.Serialize(file, _options);
            var hydrated = JsonSerializer.Deserialize<IngestionFile>(json, _options);
            hydrated.ShouldNotBeNull();
            hydrated!.Filename.ShouldBe("a.txt");
            hydrated.Size.ShouldBe(123);
            hydrated.Timestamp.ShouldBe(new DateTimeOffset(2026, 3, 5, 10, 15, 30, TimeSpan.Zero));
            hydrated.MimeType.ShouldBe("text/plain");
        }

        [Fact]
        public void IngestionFileList_Serializes_AsJsonArray()
        {
            var list = new IngestionFileList
            {
                new IngestionFile
                {
                    Filename = "a.txt",
                    Size = 123,
                    Timestamp = new DateTimeOffset(2026, 3, 5, 10, 15, 30, TimeSpan.Zero),
                    MimeType = "text/plain"
                }
            };

            var json = JsonSerializer.Serialize(list, _options);
            json.TrimStart()
                .ShouldStartWith("[");

            var hydrated = JsonSerializer.Deserialize<IngestionFileList>(json, _options);
            hydrated.ShouldNotBeNull();
            hydrated!.Count.ShouldBe(1);
        }

        [Theory]
        [InlineData("{\"Size\":1,\"Timestamp\":\"2026-03-05T10:15:30+00:00\",\"MimeType\":\"text/plain\"}")]
        [InlineData("{\"Filename\":\"a.txt\",\"Timestamp\":\"2026-03-05T10:15:30+00:00\",\"MimeType\":\"text/plain\"}")]
        [InlineData("{\"Filename\":\"a.txt\",\"Size\":1,\"MimeType\":\"text/plain\"}")]
        [InlineData("{\"Filename\":\"a.txt\",\"Size\":1,\"Timestamp\":\"2026-03-05T10:15:30+00:00\"}")]
        [InlineData("{\"Filename\":\"a.txt\",\"Size\":-1,\"Timestamp\":\"2026-03-05T10:15:30+00:00\",\"MimeType\":\"text/plain\"}")]
        public void IngestionFile_Rejects_InvalidOrMissingFields(string json)
        {
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionFile>(json, _options));
        }

        [Fact]
        public void UpdateAclRequest_Rejects_EmptySecurityTokens()
        {
            var json = "{" + "\"Id\":\"ABC123\"," + "\"SecurityTokens\":[]" + "}";
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<UpdateAclRequest>(json, _options));
        }

        [Fact]
        public void IngestionRequestEnvelope_Rejects_MissingPayload()
        {
            var json = "{\"RequestType\":\"AddItem\"}";
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionRequest>(json, _options));
        }

        [Fact]
        public void IngestionRequestEnvelope_Rejects_MultiplePayloads()
        {
            var json = "{" + "\"RequestType\":\"DeleteItem\"," + "\"DeleteItem\":{\"Id\":\"ABC123\"}," + "\"UpdateAcl\":{\"Id\":\"ABC123\",\"SecurityTokens\":[\"t\"]}" + "}";
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionRequest>(json, _options));
        }

        [Fact]
        public void IngestionRequestEnvelope_Rejects_MismatchedRequestTypeAndPayload()
        {
            var json = "{" + "\"RequestType\":\"AddItem\"," + "\"DeleteItem\":{\"Id\":\"ABC123\"}" + "}";
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionRequest>(json, _options));
        }

        [Theory]
        [InlineData(IngestionPropertyType.String, "string")]
        [InlineData(IngestionPropertyType.Text, "text")]
        [InlineData(IngestionPropertyType.Integer, "integer")]
        [InlineData(IngestionPropertyType.Double, "double")]
        [InlineData(IngestionPropertyType.Decimal, "decimal")]
        [InlineData(IngestionPropertyType.Boolean, "boolean")]
        [InlineData(IngestionPropertyType.DateTime, "datetime")]
        [InlineData(IngestionPropertyType.TimeSpan, "timespan")]
        [InlineData(IngestionPropertyType.Guid, "guid")]
        [InlineData(IngestionPropertyType.Uri, "uri")]
        [InlineData(IngestionPropertyType.StringArray, "string-array")]
        public void IngestionPropertyType_Serializes_ToLowercaseTokens(IngestionPropertyType type, string expected)
        {
            var json = JsonSerializer.Serialize(type, _options);
            json.ShouldBe($"\"{expected}\"");
        }

        [Fact]
        public void IngestionPropertyType_Deserialize_IsCaseSensitive()
        {
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionPropertyType>("\"String\"", _options));
        }

        [Fact]
        public void IngestionPropertyType_Deserialize_Id_IsNotSupported()
        {
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionPropertyType>("\"id\"", _options));
        }

        [Fact]
        public void RoundTrip_AllSupportedTypes_Succeeds()
        {
            var addItem = new AddItemRequest
            {
                Id = "123456ID",
                SecurityTokens = ["token-a"],
                Timestamp = new DateTimeOffset(2026, 3, 5, 10, 15, 30, TimeSpan.Zero),
                Files = new IngestionFileList(),
                Properties =
                [
                    new IngestionProperty { Name = "String", Type = IngestionPropertyType.String, Value = "hello" },
                    new IngestionProperty { Name = "Text", Type = IngestionPropertyType.Text, Value = "Human readable text" },
                    new IngestionProperty { Name = "Int64Min", Type = IngestionPropertyType.Integer, Value = long.MinValue },
                    new IngestionProperty { Name = "Int64Max", Type = IngestionPropertyType.Integer, Value = long.MaxValue },
                    new IngestionProperty { Name = "Double", Type = IngestionPropertyType.Double, Value = 1234.5d },
                    new IngestionProperty
                    {
                        Name = "Decimal", Type = IngestionPropertyType.Decimal, Value = 79228162514264337593543950335m
                    },
                    new IngestionProperty { Name = "Boolean", Type = IngestionPropertyType.Boolean, Value = true },
                    new IngestionProperty
                    {
                        Name = "DateTime", Type = IngestionPropertyType.DateTime,
                        Value = new DateTimeOffset(2026, 3, 5, 10, 15, 30, TimeSpan.Zero)
                    },
                    new IngestionProperty { Name = "TimeSpan", Type = IngestionPropertyType.TimeSpan, Value = TimeSpan.FromMinutes(15) },
                    new IngestionProperty
                    {
                        Name = "Guid", Type = IngestionPropertyType.Guid,
                        Value = Guid.Parse("bfb8feca-5f5f-4f55-aa0c-7d7b00b19b46")
                    },
                    new IngestionProperty
                    {
                        Name = "Uri", Type = IngestionPropertyType.Uri,
                        Value = new Uri("https://example.test/resource/123")
                    },
                    new IngestionProperty
                    {
                        Name = "StringArrayEmpty", Type = IngestionPropertyType.StringArray,
                        Value = Array.Empty<string>()
                    },
                    new IngestionProperty { Name = "StringArray", Type = IngestionPropertyType.StringArray, Value = new[] { "a", "b" } }
                ]
            };

            var envelope = new IngestionRequest
            {
                RequestType = IngestionRequestType.AddItem,
                AddItem = addItem
            };

            var json = JsonSerializer.Serialize(envelope, _options);
            json.ShouldNotContain("null");

            var hydratedEnvelope = JsonSerializer.Deserialize<IngestionRequest>(json, _options);
            hydratedEnvelope.ShouldNotBeNull();

            hydratedEnvelope.RequestType.ShouldBe(IngestionRequestType.AddItem);
            hydratedEnvelope.AddItem.ShouldNotBeNull();

            var hydrated = hydratedEnvelope.AddItem!;

            hydrated.Properties.Count.ShouldBe(addItem.Properties.Count);

            hydrated.TryGetString("string", out var s)
                    .ShouldBeTrue();
            s.ShouldBe("hello");

            var text = hydrated.Properties.Single(p => p.Name.Equals("Text", StringComparison.OrdinalIgnoreCase));
            text.Type.ShouldBe(IngestionPropertyType.Text);
            text.Value.ShouldBe("Human readable text");

            hydrated.Id.ShouldBe("123456ID");

            hydrated.TryGetInt64("int64min", out var iMin)
                    .ShouldBeTrue();
            iMin.ShouldBe(long.MinValue);

            hydrated.TryGetInt64("INT64MAX", out var iMax)
                    .ShouldBeTrue();
            iMax.ShouldBe(long.MaxValue);

            hydrated.TryGetDouble("double", out var dbl)
                    .ShouldBeTrue();
            dbl.ShouldBe(1234.5d);

            hydrated.TryGetDecimal("decimal", out var dec)
                    .ShouldBeTrue();
            dec.ShouldBe(79228162514264337593543950335m);

            hydrated.TryGetBoolean("boolean", out var b)
                    .ShouldBeTrue();
            b.ShouldBeTrue();

            hydrated.TryGetDateTimeOffset("datetime", out var dto)
                    .ShouldBeTrue();
            dto.ShouldBe(new DateTimeOffset(2026, 3, 5, 10, 15, 30, TimeSpan.Zero));

            hydrated.TryGetTimeSpan("timespan", out var ts)
                    .ShouldBeTrue();
            ts.ShouldBe(TimeSpan.FromMinutes(15));

            hydrated.TryGetGuid("guid", out var g)
                    .ShouldBeTrue();
            g.ShouldBe(Guid.Parse("bfb8feca-5f5f-4f55-aa0c-7d7b00b19b46"));

            hydrated.TryGetUri("uri", out var uri)
                    .ShouldBeTrue();
            uri.ShouldBe(new Uri("https://example.test/resource/123"));

            hydrated.TryGetStringArray("stringarrayempty", out var empty)
                    .ShouldBeTrue();
            empty.ShouldNotBeNull();
            empty!.Length.ShouldBe(0);

            hydrated.TryGetStringArray("stringarray", out var arr)
                    .ShouldBeTrue();
            arr.ShouldBe(new[] { "a", "b" });
        }

        [Fact]
        public void IngestionProperty_Rejects_MissingName()
        {
            var json = "{\"Type\":\"string\",\"Value\":\"abc\"}";
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionProperty>(json, _options));
        }

        [Fact]
        public void IngestionProperty_Rejects_MissingType()
        {
            var json = "{\"Name\":\"A\",\"Value\":\"abc\"}";
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionProperty>(json, _options));
        }

        [Fact]
        public void IngestionProperty_Rejects_MissingValue()
        {
            var json = "{\"Name\":\"A\",\"Type\":\"string\"}";
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionProperty>(json, _options));
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
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionProperty>(json, _options));
        }

        [Fact]
        public void Integer_Rejects_Fractional()
        {
            var json = "{\"Name\":\"A\",\"Type\":\"integer\",\"Value\":1.23}";
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionProperty>(json, _options));
        }

        [Fact]
        public void Integer_Rejects_OutOfRange()
        {
            var json = "{\"Name\":\"A\",\"Type\":\"integer\",\"Value\":9223372036854775808}";
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionProperty>(json, _options));
        }

        [Fact]
        public void Uri_Rejects_Relative()
        {
            var json = "{\"Name\":\"A\",\"Type\":\"uri\",\"Value\":\"/relative\"}";
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionProperty>(json, _options));
        }

        [Fact]
        public void StringArray_Rejects_NullElements()
        {
            var json = "{\"Name\":\"A\",\"Type\":\"string-array\",\"Value\":[\"a\",null]}";
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<IngestionProperty>(json, _options));
        }
    }
}