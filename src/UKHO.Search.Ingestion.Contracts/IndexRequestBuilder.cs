using System.Text.Json;

namespace UKHO.Search.Ingestion.Contracts
{
    /// <summary>
    /// Provides a producer-safe builder for constructing validated <see cref="IndexRequest" /> payloads.
    /// </summary>
    public sealed class IndexRequestBuilder
    {
        private readonly List<IngestionFile> _files = new();
        private readonly List<IngestionProperty> _properties = new();
        private readonly List<string> _securityTokens = new();
        private string? _id;
        private DateTimeOffset? _timestamp;

        /// <summary>
        /// Sets the identifier of the document being indexed.
        /// </summary>
        /// <param name="id">
        /// The document identifier.
        /// </param>
        /// <returns>
        /// The same builder instance for fluent chaining.
        /// </returns>
        public IndexRequestBuilder WithId(string id)
        {
            _id = id;
            return this;
        }

        /// <summary>
        /// Sets the timestamp associated with the indexing payload.
        /// </summary>
        /// <param name="timestamp">
        /// The payload timestamp.
        /// </param>
        /// <returns>
        /// The same builder instance for fluent chaining.
        /// </returns>
        public IndexRequestBuilder WithTimestamp(DateTimeOffset timestamp)
        {
            _timestamp = timestamp;
            return this;
        }

        /// <summary>
        /// Adds one security token to the payload under construction.
        /// </summary>
        /// <param name="securityToken">
        /// The security token to add.
        /// </param>
        /// <returns>
        /// The same builder instance for fluent chaining.
        /// </returns>
        public IndexRequestBuilder AddSecurityToken(string securityToken)
        {
            _securityTokens.Add(securityToken);
            return this;
        }

        /// <summary>
        /// Adds a property to the payload under construction.
        /// </summary>
        /// <param name="property">
        /// The property to add.
        /// </param>
        /// <returns>
        /// The same builder instance for fluent chaining.
        /// </returns>
        public IndexRequestBuilder AddProperty(IngestionProperty property)
        {
            ArgumentNullException.ThrowIfNull(property);

            _properties.Add(property);
            return this;
        }

        /// <summary>
        /// Adds a file to the payload under construction.
        /// </summary>
        /// <param name="file">
        /// The file to add.
        /// </param>
        /// <returns>
        /// The same builder instance for fluent chaining.
        /// </returns>
        public IndexRequestBuilder AddFile(IngestionFile file)
        {
            ArgumentNullException.ThrowIfNull(file);

            _files.Add(file);
            return this;
        }

        /// <summary>
        /// Adds a file to the payload under construction from primitive values.
        /// </summary>
        /// <param name="filename">
        /// The file name.
        /// </param>
        /// <param name="size">
        /// The file size in bytes.
        /// </param>
        /// <param name="timestamp">
        /// The file timestamp.
        /// </param>
        /// <param name="mimeType">
        /// The file MIME type.
        /// </param>
        /// <returns>
        /// The same builder instance for fluent chaining.
        /// </returns>
        public IndexRequestBuilder AddFile(string filename, long size, DateTimeOffset timestamp, string mimeType)
        {
            // Reuse the canonical DTO constructor so file validation stays in one contract-owned place.
            return AddFile(new IngestionFile(filename, size, timestamp, mimeType));
        }

        /// <summary>
        /// Builds a validated <see cref="IndexRequest" /> from the current builder state.
        /// </summary>
        /// <returns>
        /// A validated indexing payload.
        /// </returns>
        /// <exception cref="JsonException">
        /// Thrown when the current builder state does not satisfy the queue-message contract.
        /// </exception>
        public IndexRequest Build()
        {
            if (TryBuild(out var request, out var errors))
            {
                return request!;
            }

            throw new JsonException(string.Join(" ", errors));
        }

        /// <summary>
        /// Attempts to build a validated <see cref="IndexRequest" /> without using exceptions for expected invalid states.
        /// </summary>
        /// <param name="request">
        /// Receives the validated request when the build succeeds.
        /// </param>
        /// <param name="errors">
        /// Receives one or more error messages when the build fails.
        /// </param>
        /// <returns>
        /// <c>true</c> when the request was built successfully; otherwise <c>false</c>.
        /// </returns>
        public bool TryBuild(out IndexRequest? request, out IReadOnlyList<string> errors)
        {
            var validationErrors = new List<string>();

            // Enforce the builder-owned required-state rules first so callers get a non-throwing path for common omissions.
            if (string.IsNullOrWhiteSpace(_id))
            {
                validationErrors.Add("IndexRequest.Id is required.");
            }

            if (!_timestamp.HasValue)
            {
                validationErrors.Add("IndexRequest.Timestamp is required.");
            }

            if (_securityTokens.Count == 0)
            {
                validationErrors.Add("IndexRequest.SecurityTokens is required and must be non-empty.");
            }

            if (validationErrors.Count > 0)
            {
                request = null;
                errors = validationErrors;
                return false;
            }

            try
            {
                // Delegate list normalization, duplicate detection, and payload validation to the canonical DTO surface.
                request = new IndexRequest(
                    _id!,
                    new IngestionPropertyList(_properties),
                    _securityTokens.ToArray(),
                    _timestamp!.Value,
                    new IngestionFileList(_files));

                errors = Array.Empty<string>();
                return true;
            }
            catch (JsonException ex)
            {
                request = null;
                errors = [ex.Message];
                return false;
            }
        }
    }
}