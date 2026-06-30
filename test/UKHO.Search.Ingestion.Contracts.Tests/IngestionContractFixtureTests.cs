using System.Text.Json;
using System.Text.Json.Nodes;
using UKHO.Search.Ingestion.Contracts.Serialization;
using Xunit;

namespace UKHO.Search.Ingestion.Contracts.Tests
{
    /// <summary>
    /// Verifies that the published fixture envelopes remain compatible with the canonical serializer and DTO surface.
    /// </summary>
    public sealed class IngestionContractFixtureTests
    {
        private static readonly JsonSerializerOptions _serializerOptions = IngestionJsonSerializerOptions.Create();

        /// <summary>
        /// Confirms that each published envelope fixture deserializes into the expected request type and re-serializes without changing its JSON meaning.
        /// </summary>
        /// <param name="fixtureName">
        /// The repository fixture file name to load.
        /// </param>
        /// <param name="expectedRequestType">
        /// The request discriminator that the fixture is expected to declare.
        /// </param>
        /// <param name="expectedId">
        /// The document identifier that the active payload is expected to carry.
        /// </param>
        [Theory]
        [InlineData("index-item-envelope.json", IngestionRequestType.IndexItem, "ABC123")]
        [InlineData("delete-item-envelope.json", IngestionRequestType.DeleteItem, "ABC123")]
        [InlineData("update-acl-envelope.json", IngestionRequestType.UpdateAcl, "ABC123")]
        public void PublishedEnvelopeFixture_RoundTripsThroughCanonicalSerializer(string fixtureName, IngestionRequestType expectedRequestType, string expectedId)
        {
            // Load the checked-in fixture so the test validates the human-readable example that producers will inspect.
            var fixtureJson = LoadFixtureJson(fixtureName);

            // Deserialize the envelope through the canonical serializer options that the package publishes.
            var envelope = JsonSerializer.Deserialize<IngestionRequest>(fixtureJson, _serializerOptions);

            Assert.NotNull(envelope);
            Assert.Equal(expectedRequestType, envelope!.RequestType);

            // Verify that the active payload is the expected one before checking serialization compatibility.
            AssertEnvelopePayload(envelope, expectedRequestType, expectedId);

            // Serialize back through the same options and compare JSON meaning rather than raw formatting.
            var serializedJson = JsonSerializer.Serialize(envelope, _serializerOptions);
            AssertJsonEquivalent(fixtureJson, serializedJson);
        }

        /// <summary>
        /// Loads a fixture file from the repository so tests exercise the canonical checked-in examples.
        /// </summary>
        /// <param name="fixtureName">
        /// The file name beneath the fixture folder.
        /// </param>
        /// <returns>
        /// The raw JSON fixture text.
        /// </returns>
        private static string LoadFixtureJson(string fixtureName)
        {
            // Resolve the repository path dynamically so the test keeps working from the normal test output directory.
            var fixturePath = Path.Combine(
                FindRepositoryRoot(),
                "test",
                "UKHO.Search.Ingestion.Contracts.Tests",
                "Fixtures",
                fixtureName);

            Assert.True(File.Exists(fixturePath), $"Expected fixture file was not found at '{fixturePath}'.");

            return File.ReadAllText(fixturePath);
        }

        /// <summary>
        /// Verifies that the deserialized envelope populated the expected payload property for the requested discriminator.
        /// </summary>
        /// <param name="envelope">
        /// The deserialized envelope instance.
        /// </param>
        /// <param name="expectedRequestType">
        /// The payload discriminator under test.
        /// </param>
        /// <param name="expectedId">
        /// The identifier that should be present in the active payload.
        /// </param>
        private static void AssertEnvelopePayload(IngestionRequest envelope, IngestionRequestType expectedRequestType, string expectedId)
        {
            // Switch on the discriminator so the test proves the fixture activates the correct payload property.
            switch (expectedRequestType)
            {
                case IngestionRequestType.IndexItem:
                    Assert.NotNull(envelope.IndexItem);
                    Assert.Equal(expectedId, envelope.IndexItem!.Id);
                    Assert.Single(envelope.IndexItem.SecurityTokens);
                    break;
                case IngestionRequestType.DeleteItem:
                    Assert.NotNull(envelope.DeleteItem);
                    Assert.Equal(expectedId, envelope.DeleteItem!.Id);
                    break;
                case IngestionRequestType.UpdateAcl:
                    Assert.NotNull(envelope.UpdateAcl);
                    Assert.Equal(expectedId, envelope.UpdateAcl!.Id);
                    Assert.Equal(2, envelope.UpdateAcl.SecurityTokens.Length);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported request type '{expectedRequestType}'.");
            }
        }

        /// <summary>
        /// Compares two JSON payloads semantically so fixture formatting can stay readable without weakening the contract assertion.
        /// </summary>
        /// <param name="expectedJson">
        /// The checked-in fixture JSON.
        /// </param>
        /// <param name="actualJson">
        /// The JSON emitted by the serializer under test.
        /// </param>
        private static void AssertJsonEquivalent(string expectedJson, string actualJson)
        {
            // Parse both documents into nodes so the assertion ignores whitespace formatting while preserving object and array structure.
            var expectedNode = JsonNode.Parse(expectedJson);
            var actualNode = JsonNode.Parse(actualJson);

            Assert.NotNull(expectedNode);
            Assert.NotNull(actualNode);
            Assert.True(
                JsonNode.DeepEquals(expectedNode, actualNode),
                $"Serialized JSON did not match the checked-in fixture. Expected:{Environment.NewLine}{expectedJson}{Environment.NewLine}Actual:{Environment.NewLine}{actualJson}");
        }

        /// <summary>
        /// Walks upward from the test output directory until the repository root marker is found.
        /// </summary>
        /// <returns>
        /// The absolute repository root path.
        /// </returns>
        private static string FindRepositoryRoot()
        {
            // Start from the test host output directory so the lookup works regardless of the invoking working directory.
            var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

            while (currentDirectory is not null)
            {
                var solutionPath = Path.Combine(currentDirectory.FullName, "Search.slnx");
                if (File.Exists(solutionPath))
                {
                    return currentDirectory.FullName;
                }

                currentDirectory = currentDirectory.Parent;
            }

            throw new InvalidOperationException("Could not locate the repository root from the test output directory.");
        }
    }
}