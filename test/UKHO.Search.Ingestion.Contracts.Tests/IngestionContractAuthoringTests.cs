using System.Text.Json;
using System.Text.Json.Nodes;
using UKHO.Search.Ingestion.Contracts.Serialization;
using Xunit;

namespace UKHO.Search.Ingestion.Contracts.Tests
{
    /// <summary>
    /// Verifies the first producer-safe authoring slice for factory, serializer, and contract-version behavior.
    /// </summary>
    public sealed class IngestionContractAuthoringTests
    {
        private static readonly JsonSerializerOptions _serializerOptions = IngestionJsonSerializerOptions.Create();

        /// <summary>
        /// Confirms that the delete factory produces the same envelope semantics as direct DTO construction.
        /// </summary>
        [Fact]
        public void IngestionRequestFactory_CreateDelete_ReturnsExpectedEnvelope()
        {
            // Build the delete envelope through the new producer-safe factory surface.
            var envelope = IngestionRequestFactory.CreateDelete("ABC123");

            Assert.Equal(IngestionRequestType.DeleteItem, envelope.RequestType);
            Assert.NotNull(envelope.DeleteItem);
            Assert.Equal("ABC123", envelope.DeleteItem!.Id);
            Assert.Null(envelope.IndexItem);
            Assert.Null(envelope.UpdateAcl);
        }

        /// <summary>
        /// Confirms that the ACL update factory preserves the expected payload and token values.
        /// </summary>
        [Fact]
        public void IngestionRequestFactory_CreateAclUpdate_ReturnsExpectedEnvelope()
        {
            // Build the ACL envelope through the new producer-safe factory surface.
            var envelope = IngestionRequestFactory.CreateAclUpdate("ABC123", ["token-a", "token-b"]);

            Assert.Equal(IngestionRequestType.UpdateAcl, envelope.RequestType);
            Assert.NotNull(envelope.UpdateAcl);
            Assert.Equal("ABC123", envelope.UpdateAcl!.Id);
            Assert.Equal(["token-a", "token-b"], envelope.UpdateAcl.SecurityTokens);
            Assert.Null(envelope.IndexItem);
            Assert.Null(envelope.DeleteItem);
        }

        /// <summary>
        /// Confirms that the package-owned serializer facade emits the same JSON contract as the canonical raw serializer path.
        /// </summary>
        [Fact]
        public void IngestionContractSerializer_WhenSerializingDeleteEnvelope_MatchesCanonicalJson()
        {
            // Use the factory so the test covers the intended producer authoring path rather than only direct DTO construction.
            var envelope = IngestionRequestFactory.CreateDelete("ABC123");

            // Serialize through both the facade and the raw canonical serializer configuration to prove parity.
            var facadeJson = IngestionContractSerializer.Serialize(envelope);
            var canonicalJson = JsonSerializer.Serialize(envelope, _serializerOptions);

            AssertJsonEquivalent(canonicalJson, facadeJson);
        }

        /// <summary>
        /// Confirms that the package-owned serializer facade can deserialize canonical JSON back into the validated envelope model.
        /// </summary>
        [Fact]
        public void IngestionContractSerializer_WhenDeserializingDeleteEnvelope_RestoresExpectedPayload()
        {
            // Produce canonical JSON through the raw serializer path so the test validates facade compatibility in the reverse direction.
            var json = JsonSerializer.Serialize(IngestionRequestFactory.CreateDelete("ABC123"), _serializerOptions);

            // Deserialize through the facade and verify the validated envelope shape.
            var envelope = IngestionContractSerializer.DeserializeIngestionRequest(json);

            Assert.Equal(IngestionRequestType.DeleteItem, envelope.RequestType);
            Assert.NotNull(envelope.DeleteItem);
            Assert.Equal("ABC123", envelope.DeleteItem!.Id);
        }

        /// <summary>
        /// Confirms that the package exposes a visible contract-version marker for compatibility discussions.
        /// </summary>
        [Fact]
        public void IngestionContractsPackage_ExposesVisibleContractVersion()
        {
            // The version marker must be public, non-blank, and stable enough for tests, docs, and samples to reference.
            Assert.False(string.IsNullOrWhiteSpace(IngestionContractsPackage.ContractVersion));
        }

        /// <summary>
        /// Compares two JSON payloads semantically so helper tests assert contract meaning rather than formatting.
        /// </summary>
        /// <param name="expectedJson">
        /// The expected canonical JSON payload.
        /// </param>
        /// <param name="actualJson">
        /// The actual JSON emitted by the API under test.
        /// </param>
        private static void AssertJsonEquivalent(string expectedJson, string actualJson)
        {
            // Parse both documents into nodes so whitespace and property formatting do not affect the comparison.
            var expectedNode = JsonNode.Parse(expectedJson);
            var actualNode = JsonNode.Parse(actualJson);

            Assert.NotNull(expectedNode);
            Assert.NotNull(actualNode);
            Assert.True(
                JsonNode.DeepEquals(expectedNode, actualNode),
                $"Serialized JSON did not match the canonical contract. Expected:{Environment.NewLine}{expectedJson}{Environment.NewLine}Actual:{Environment.NewLine}{actualJson}");
        }
    }
}