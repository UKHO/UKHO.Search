namespace UKHO.Search.Ingestion.Contracts
{
    /// <summary>
    /// Represents the non-throwing validation result for a queue-message contract instance.
    /// </summary>
    public sealed class IngestionContractValidationResult
    {
        /// <summary>
        /// Initializes a validation result from the supplied error collection.
        /// </summary>
        /// <param name="errors">
        /// The validation errors associated with the inspected contract instance.
        /// </param>
        public IngestionContractValidationResult(IReadOnlyList<IngestionContractValidationError> errors)
        {
            ArgumentNullException.ThrowIfNull(errors);

            Errors = errors;
        }

        /// <summary>
        /// Gets a value indicating whether validation succeeded.
        /// </summary>
        public bool IsValid => Errors.Count == 0;

        /// <summary>
        /// Gets the flat validation errors associated with the inspected contract instance.
        /// </summary>
        public IReadOnlyList<IngestionContractValidationError> Errors { get; }
    }
}