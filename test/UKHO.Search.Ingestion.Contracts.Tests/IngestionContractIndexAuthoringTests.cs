using Xunit;

namespace UKHO.Search.Ingestion.Contracts.Tests
{
    /// <summary>
    /// Verifies typed property helpers and index authoring helpers for the producer-safe `IndexItem` slice.
    /// </summary>
    public sealed class IngestionContractIndexAuthoringTests
    {
        /// <summary>
        /// Confirms that the string property helper pairs the expected name, type, and value.
        /// </summary>
        [Fact]
        public void IngestionPropertyFactory_String_ReturnsExpectedProperty()
        {
            // Create a simple scalar property through the typed helper surface.
            var property = IngestionPropertyFactory.String("Title", "Hello");

            Assert.Equal("Title", property.Name);
            Assert.Equal(IngestionPropertyType.String, property.Type);
            Assert.Equal("Hello", property.Value);
        }

        /// <summary>
        /// Confirms that the string-array property helper preserves the array payload and declared contract type.
        /// </summary>
        [Fact]
        public void IngestionPropertyFactory_StringArray_ReturnsExpectedProperty()
        {
            // Create a string-array property through the typed helper surface.
            var property = IngestionPropertyFactory.StringArray("Keywords", ["alpha", "beta"]);

            Assert.Equal("Keywords", property.Name);
            Assert.Equal(IngestionPropertyType.StringArray, property.Type);
            Assert.Equal(["alpha", "beta"], Assert.IsType<string[]>(property.Value));
        }

        /// <summary>
        /// Confirms that the index factory creates the same envelope shape as direct DTO construction.
        /// </summary>
        [Fact]
        public void IngestionRequestFactory_CreateIndex_ReturnsExpectedEnvelope()
        {
            // Create a canonical index envelope through the package-owned helper path.
            var envelope = IngestionRequestFactory.CreateIndex(
                id: "ABC123",
                properties: [IngestionPropertyFactory.String("Title", "Hello")],
                securityTokens: ["token-a"],
                timestamp: new DateTimeOffset(2026, 3, 5, 10, 15, 30, TimeSpan.Zero),
                files: new IngestionFileList
                {
                    new IngestionFile("a.txt", 123, new DateTimeOffset(2026, 3, 5, 10, 15, 31, TimeSpan.Zero), "text/plain")
                });

            Assert.Equal(IngestionRequestType.IndexItem, envelope.RequestType);
            Assert.NotNull(envelope.IndexItem);
            Assert.Equal("ABC123", envelope.IndexItem!.Id);
            Assert.Equal(["token-a"], envelope.IndexItem.SecurityTokens);
            Assert.Single(envelope.IndexItem.Properties);
            Assert.Equal("title", envelope.IndexItem.Properties[0].Name);
        }

        /// <summary>
        /// Confirms that the builder can create a validated index request without requiring callers to assemble DTO collections manually.
        /// </summary>
        [Fact]
        public void IndexRequestBuilder_Build_ReturnsValidatedRequest()
        {
            // Build an index payload through the fluent builder surface.
            var request = new IndexRequestBuilder()
                .WithId("ABC123")
                .WithTimestamp(new DateTimeOffset(2026, 3, 5, 10, 15, 30, TimeSpan.Zero))
                .AddSecurityToken("token-a")
                .AddProperty(IngestionPropertyFactory.String("Title", "Hello"))
                .AddFile("a.txt", 123, new DateTimeOffset(2026, 3, 5, 10, 15, 31, TimeSpan.Zero), "text/plain")
                .Build();

            Assert.Equal("ABC123", request.Id);
            Assert.Equal(["token-a"], request.SecurityTokens);
            Assert.Single(request.Properties);
            Assert.Single(request.Files);
        }

        /// <summary>
        /// Confirms that the builder exposes a non-throwing path for expected invalid authoring states.
        /// </summary>
        [Fact]
        public void IndexRequestBuilder_TryBuild_ReturnsFalse_WhenSecurityTokensMissing()
        {
            // Omit security tokens deliberately so the builder exercises its non-throwing failure path.
            var built = new IndexRequestBuilder()
                .WithId("ABC123")
                .WithTimestamp(new DateTimeOffset(2026, 3, 5, 10, 15, 30, TimeSpan.Zero))
                .AddProperty(IngestionPropertyFactory.String("Title", "Hello"))
                .TryBuild(out var request, out var errors);

            Assert.False(built);
            Assert.Null(request);
            Assert.NotEmpty(errors);
        }
    }
}