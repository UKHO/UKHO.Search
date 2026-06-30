using System.Text.Json;
using UKHO.Search.Ingestion.Contracts.Serialization;
using Xunit;

namespace UKHO.Search.Ingestion.Contracts.Tests
{
    /// <summary>
    /// Verifies the final Arc 01 contracts-only usage path from the perspective of a minimal external producer.
    /// </summary>
    public sealed class ExternalConsumerContractUsageTests
    {
        /// <summary>
        /// Confirms that a contracts-only consumer can create and serialize a delete message through the final package surface.
        /// </summary>
        [Fact]
        public void ExternalConsumer_CanCreateAndSerializeDeleteItem()
        {
            // Create the envelope through the producer-safe factory path that an external producer is expected to use.
            var envelope = IngestionRequestFactory.CreateDelete("ABC123");

            // Serialize through the canonical package serializer and verify the essential wire-contract shape.
            var json = IngestionContractSerializer.Serialize(envelope);
            using var document = JsonDocument.Parse(json);

            Assert.Equal("DeleteItem", document.RootElement.GetProperty("RequestType").GetString());
            Assert.Equal("ABC123", document.RootElement.GetProperty("DeleteItem").GetProperty("Id").GetString());
        }

        /// <summary>
        /// Confirms that a contracts-only consumer can create and serialize an ACL update message through the final package surface.
        /// </summary>
        [Fact]
        public void ExternalConsumer_CanCreateAndSerializeUpdateAcl()
        {
            // Create the envelope through the producer-safe factory path that an external producer is expected to use.
            var envelope = IngestionRequestFactory.CreateAclUpdate("ABC123", ["token-a", "token-b"]);

            // Serialize through the canonical package serializer and verify the essential wire-contract shape.
            var json = IngestionContractSerializer.Serialize(envelope);
            using var document = JsonDocument.Parse(json);

            Assert.Equal("UpdateAcl", document.RootElement.GetProperty("RequestType").GetString());
            Assert.Equal("ABC123", document.RootElement.GetProperty("UpdateAcl").GetProperty("Id").GetString());
            Assert.Equal(2, document.RootElement.GetProperty("UpdateAcl").GetProperty("SecurityTokens").GetArrayLength());
        }

        /// <summary>
        /// Confirms that a contracts-only consumer can create and serialize an index message through the final package surface.
        /// </summary>
        [Fact]
        public void ExternalConsumer_CanCreateAndSerializeIndexItem()
        {
            // Create the payload through the builder and typed property helpers so the test exercises the recommended contracts-only path.
            var indexRequest = new IndexRequestBuilder()
                .WithId("ABC123")
                .WithTimestamp(new DateTimeOffset(2026, 3, 5, 10, 15, 30, TimeSpan.Zero))
                .AddSecurityToken("token-a")
                .AddProperty(IngestionPropertyFactory.String("Title", "Example document"))
                .AddFile("a.txt", 123, new DateTimeOffset(2026, 3, 5, 10, 15, 31, TimeSpan.Zero), "text/plain")
                .Build();

            var envelope = IngestionRequestFactory.CreateIndex(indexRequest);

            // Serialize through the canonical package serializer and verify the essential wire-contract shape.
            var json = IngestionContractSerializer.Serialize(envelope);
            using var document = JsonDocument.Parse(json);

            Assert.Equal("IndexItem", document.RootElement.GetProperty("RequestType").GetString());
            Assert.Equal("ABC123", document.RootElement.GetProperty("IndexItem").GetProperty("Id").GetString());
            Assert.Equal(1, document.RootElement.GetProperty("IndexItem").GetProperty("SecurityTokens").GetArrayLength());
            Assert.Equal(1, document.RootElement.GetProperty("IndexItem").GetProperty("Properties").GetArrayLength());
            Assert.Equal(1, document.RootElement.GetProperty("IndexItem").GetProperty("Files").GetArrayLength());
        }
    }
}