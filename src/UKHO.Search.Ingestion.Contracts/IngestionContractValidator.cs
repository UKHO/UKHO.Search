namespace UKHO.Search.Ingestion.Contracts
{
    /// <summary>
    /// Provides non-throwing validation entry points for the ingestion queue-message contract.
    /// </summary>
    public static class IngestionContractValidator
    {
        /// <summary>
        /// Validates a top-level ingestion request envelope.
        /// </summary>
        /// <param name="request">
        /// The envelope to validate.
        /// </param>
        /// <returns>
        /// A flat validation result containing any discovered contract errors.
        /// </returns>
        public static IngestionContractValidationResult Validate(IngestionRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var errors = new List<IngestionContractValidationError>();

            // Validate envelope structure first so payload-specific results are interpreted in the correct context.
            var payloadCount = 0;
            if (request.IndexItem is not null)
            {
                payloadCount++;
            }

            if (request.DeleteItem is not null)
            {
                payloadCount++;
            }

            if (request.UpdateAcl is not null)
            {
                payloadCount++;
            }

            if (payloadCount == 0)
            {
                errors.Add(CreateError("Envelope.Payload.Required", "RequestType", "IngestionRequest must contain exactly one payload."));
                return new IngestionContractValidationResult(errors);
            }

            if (payloadCount > 1)
            {
                errors.Add(CreateError("Envelope.Payload.Multiple", "RequestType", "IngestionRequest must not contain multiple payloads."));
                return new IngestionContractValidationResult(errors);
            }

            // After the one-of check succeeds, validate the payload that matches the discriminator and report missing alignment explicitly.
            switch (request.RequestType)
            {
                case IngestionRequestType.IndexItem:
                    if (request.IndexItem is null)
                    {
                        errors.Add(CreateError("Envelope.RequestType.Mismatch", "IndexItem", "IngestionRequest.RequestType is 'IndexItem' but the IndexItem payload is missing."));
                        break;
                    }

                    Validate(request.IndexItem, errors, "IndexItem");
                    break;

                case IngestionRequestType.DeleteItem:
                    if (request.DeleteItem is null)
                    {
                        errors.Add(CreateError("Envelope.RequestType.Mismatch", "DeleteItem", "IngestionRequest.RequestType is 'DeleteItem' but the DeleteItem payload is missing."));
                        break;
                    }

                    Validate(request.DeleteItem, errors, "DeleteItem");
                    break;

                case IngestionRequestType.UpdateAcl:
                    if (request.UpdateAcl is null)
                    {
                        errors.Add(CreateError("Envelope.RequestType.Mismatch", "UpdateAcl", "IngestionRequest.RequestType is 'UpdateAcl' but the UpdateAcl payload is missing."));
                        break;
                    }

                    Validate(request.UpdateAcl, errors, "UpdateAcl");
                    break;

                default:
                    errors.Add(CreateError("Envelope.RequestType.Unsupported", "RequestType", $"Unsupported IngestionRequestType '{request.RequestType}'."));
                    break;
            }

            return new IngestionContractValidationResult(errors);
        }

        /// <summary>
        /// Validates an index payload.
        /// </summary>
        /// <param name="request">
        /// The index payload to validate.
        /// </param>
        /// <returns>
        /// A flat validation result containing any discovered contract errors.
        /// </returns>
        public static IngestionContractValidationResult Validate(IndexRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var errors = new List<IngestionContractValidationError>();
            Validate(request, errors, "IndexItem");
            return new IngestionContractValidationResult(errors);
        }

        /// <summary>
        /// Validates a delete payload.
        /// </summary>
        /// <param name="request">
        /// The delete payload to validate.
        /// </param>
        /// <returns>
        /// A flat validation result containing any discovered contract errors.
        /// </returns>
        public static IngestionContractValidationResult Validate(DeleteItemRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var errors = new List<IngestionContractValidationError>();
            Validate(request, errors, "DeleteItem");
            return new IngestionContractValidationResult(errors);
        }

        /// <summary>
        /// Validates an ACL update payload.
        /// </summary>
        /// <param name="request">
        /// The ACL update payload to validate.
        /// </param>
        /// <returns>
        /// A flat validation result containing any discovered contract errors.
        /// </returns>
        public static IngestionContractValidationResult Validate(UpdateAclRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var errors = new List<IngestionContractValidationError>();
            Validate(request, errors, "UpdateAcl");
            return new IngestionContractValidationResult(errors);
        }

        /// <summary>
        /// Validates the index payload fields directly against the current contract rules.
        /// </summary>
        /// <param name="request">
        /// The payload to validate.
        /// </param>
        /// <param name="errors">
        /// The accumulating validation error collection.
        /// </param>
        /// <param name="pathPrefix">
        /// The payload path prefix to use in reported errors.
        /// </param>
        private static void Validate(IndexRequest request, ICollection<IngestionContractValidationError> errors, string pathPrefix)
        {
            if (string.IsNullOrWhiteSpace(request.Id))
            {
                errors.Add(CreateError("IndexItem.Id.Required", $"{pathPrefix}.Id", "IndexRequest.Id is required."));
            }

            if (request.Properties is null)
            {
                errors.Add(CreateError("IndexItem.Properties.Required", $"{pathPrefix}.Properties", "IndexRequest.Properties cannot be null."));
            }
            else if (request.Properties.Any(property => string.Equals(property.Name, "Id", StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add(CreateError("IndexItem.Properties.IdReserved", $"{pathPrefix}.Properties", "IndexRequest.Properties cannot contain an IngestionProperty named 'Id'. Id is a first-class property."));
            }

            if (request.SecurityTokens is null || request.SecurityTokens.Length == 0)
            {
                errors.Add(CreateError("IndexItem.SecurityTokens.Required", $"{pathPrefix}.SecurityTokens", "IndexRequest.SecurityTokens is required and must be non-empty."));
            }
            else if (request.SecurityTokens.Any(string.IsNullOrWhiteSpace))
            {
                errors.Add(CreateError("IndexItem.SecurityTokens.EntryRequired", $"{pathPrefix}.SecurityTokens", "IndexRequest.SecurityTokens cannot contain null or blank entries."));
            }

            if (request.Files is null)
            {
                errors.Add(CreateError("IndexItem.Files.Required", $"{pathPrefix}.Files", "IndexRequest.Files cannot be null."));
            }
            else if (request.Files.Any(file => file is null))
            {
                errors.Add(CreateError("IndexItem.Files.EntryRequired", $"{pathPrefix}.Files", "IndexRequest.Files cannot contain null entries."));
            }
        }

        /// <summary>
        /// Validates the delete payload fields directly against the current contract rules.
        /// </summary>
        /// <param name="request">
        /// The payload to validate.
        /// </param>
        /// <param name="errors">
        /// The accumulating validation error collection.
        /// </param>
        /// <param name="pathPrefix">
        /// The payload path prefix to use in reported errors.
        /// </param>
        private static void Validate(DeleteItemRequest request, ICollection<IngestionContractValidationError> errors, string pathPrefix)
        {
            if (string.IsNullOrWhiteSpace(request.Id))
            {
                errors.Add(CreateError("DeleteItem.Id.Required", $"{pathPrefix}.Id", "DeleteItemRequest.Id is required."));
            }
        }

        /// <summary>
        /// Validates the ACL update payload fields directly against the current contract rules.
        /// </summary>
        /// <param name="request">
        /// The payload to validate.
        /// </param>
        /// <param name="errors">
        /// The accumulating validation error collection.
        /// </param>
        /// <param name="pathPrefix">
        /// The payload path prefix to use in reported errors.
        /// </param>
        private static void Validate(UpdateAclRequest request, ICollection<IngestionContractValidationError> errors, string pathPrefix)
        {
            if (string.IsNullOrWhiteSpace(request.Id))
            {
                errors.Add(CreateError("UpdateAcl.Id.Required", $"{pathPrefix}.Id", "UpdateAclRequest.Id is required."));
            }

            if (request.SecurityTokens is null || request.SecurityTokens.Length == 0)
            {
                errors.Add(CreateError("UpdateAcl.SecurityTokens.Required", $"{pathPrefix}.SecurityTokens", "UpdateAclRequest.SecurityTokens is required and must be non-empty."));
            }
            else if (request.SecurityTokens.Any(string.IsNullOrWhiteSpace))
            {
                errors.Add(CreateError("UpdateAcl.SecurityTokens.EntryRequired", $"{pathPrefix}.SecurityTokens", "UpdateAclRequest.SecurityTokens cannot contain null or blank entries."));
            }
        }

        /// <summary>
        /// Creates one flat validation error instance.
        /// </summary>
        /// <param name="code">
        /// The stable validation error code.
        /// </param>
        /// <param name="path">
        /// The contract path associated with the error.
        /// </param>
        /// <param name="message">
        /// The human-readable error message.
        /// </param>
        /// <returns>
        /// A flat validation error instance.
        /// </returns>
        private static IngestionContractValidationError CreateError(string code, string path, string message)
        {
            return new IngestionContractValidationError(code, path, message);
        }
    }
}