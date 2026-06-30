using Xunit;

namespace UKHO.Search.Ingestion.Contracts.Tests
{
    /// <summary>
    /// Verifies the non-throwing validation surface for the producer-safe contracts package.
    /// </summary>
    public sealed class IngestionContractValidationTests
    {
        /// <summary>
        /// Confirms that a helper-created valid delete envelope produces a successful validation result.
        /// </summary>
        [Fact]
        public void IngestionContractValidator_WhenDeleteEnvelopeIsValid_ReturnsValidResult()
        {
            // Validate a helper-created envelope so the test exercises the intended producer-facing surface.
            var result = IngestionContractValidator.Validate(IngestionRequestFactory.CreateDelete("ABC123"));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        /// <summary>
        /// Confirms that the validator reports flat error details for an invalid delete payload without using exceptions.
        /// </summary>
        [Fact]
        public void IngestionContractValidator_WhenDeletePayloadIsInvalid_ReturnsCodePathAndMessage()
        {
            // Build an invalid envelope through object initialization so the validator can inspect an expected producer mistake.
            var result = IngestionContractValidator.Validate(new IngestionRequest
            {
                RequestType = IngestionRequestType.DeleteItem,
                DeleteItem = new DeleteItemRequest { Id = " " }
            });

            Assert.False(result.IsValid);

            var error = Assert.Single(result.Errors);
            Assert.Equal("DeleteItem.Id.Required", error.Code);
            Assert.Equal("DeleteItem.Id", error.Path);
            Assert.Contains("required", error.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Confirms that the validator reports a flat error for missing security tokens on an index payload.
        /// </summary>
        [Fact]
        public void IngestionContractValidator_WhenIndexSecurityTokensMissing_ReturnsExpectedError()
        {
            // Build an invalid index envelope directly so the validator inspects the raw DTO surface rather than only builder output.
            var result = IngestionContractValidator.Validate(new IngestionRequest
            {
                RequestType = IngestionRequestType.IndexItem,
                IndexItem = new IndexRequest
                {
                    Id = "ABC123",
                    Timestamp = new DateTimeOffset(2026, 3, 5, 10, 15, 30, TimeSpan.Zero),
                    Files = new IngestionFileList(),
                    Properties = new IngestionPropertyList(),
                    SecurityTokens = []
                }
            });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.Code == "IndexItem.SecurityTokens.Required" && error.Path == "IndexItem.SecurityTokens");
        }
    }
}