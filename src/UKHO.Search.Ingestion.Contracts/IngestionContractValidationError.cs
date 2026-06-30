namespace UKHO.Search.Ingestion.Contracts
{
    /// <summary>
    /// Represents one flat validation error reported by the producer-safe contract validator.
    /// </summary>
    public sealed record IngestionContractValidationError
    {
        /// <summary>
        /// Initializes a validation error with a stable code, path, and human-readable message.
        /// </summary>
        /// <param name="code">
        /// The stable validation error code.
        /// </param>
        /// <param name="path">
        /// The contract path associated with the validation failure.
        /// </param>
        /// <param name="message">
        /// The human-readable description of the validation failure.
        /// </param>
        public IngestionContractValidationError(string code, string path, string message)
        {
            Code = code;
            Path = path;
            Message = message;
        }

        /// <summary>
        /// Gets the stable validation error code.
        /// </summary>
        public string Code { get; init; }

        /// <summary>
        /// Gets the contract path associated with the validation failure.
        /// </summary>
        public string Path { get; init; }

        /// <summary>
        /// Gets the human-readable description of the validation failure.
        /// </summary>
        public string Message { get; init; }
    }
}