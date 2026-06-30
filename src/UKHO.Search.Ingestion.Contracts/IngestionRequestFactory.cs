namespace UKHO.Search.Ingestion.Contracts
{
    /// <summary>
    /// Provides producer-safe helper entry points for constructing ingestion request envelopes.
    /// </summary>
    public static class IngestionRequestFactory
    {
        /// <summary>
        /// Creates an index envelope from the supplied payload values.
        /// </summary>
        /// <param name="id">
        /// The identifier of the document being indexed.
        /// </param>
        /// <param name="properties">
        /// The properties to attach to the indexing payload.
        /// </param>
        /// <param name="securityTokens">
        /// The non-empty security-token set supplied by the producer.
        /// </param>
        /// <param name="timestamp">
        /// The timestamp associated with the indexing payload.
        /// </param>
        /// <param name="files">
        /// The file collection carried by the payload.
        /// </param>
        /// <returns>
        /// A validated <see cref="IngestionRequest" /> representing an index operation.
        /// </returns>
        public static IngestionRequest CreateIndex(string id, IReadOnlyList<IngestionProperty> properties, string[] securityTokens, DateTimeOffset timestamp, IngestionFileList files)
        {
            // Construct the canonical payload first so the helper inherits all existing contract validation rules.
            var indexItem = new IndexRequest(id, properties, securityTokens, timestamp, files);
            return CreateIndex(indexItem);
        }

        /// <summary>
        /// Creates an index envelope from a pre-built indexing payload.
        /// </summary>
        /// <param name="indexItem">
        /// The validated indexing payload to wrap.
        /// </param>
        /// <returns>
        /// A validated <see cref="IngestionRequest" /> representing an index operation.
        /// </returns>
        public static IngestionRequest CreateIndex(IndexRequest indexItem)
        {
            ArgumentNullException.ThrowIfNull(indexItem);

            // Construct the envelope through the validated constructor so discriminator and payload alignment stays canonical.
            return new IngestionRequest(IngestionRequestType.IndexItem, indexItem, null, null);
        }

        /// <summary>
        /// Creates a delete envelope for the supplied document identifier.
        /// </summary>
        /// <param name="id">
        /// The identifier of the document to delete.
        /// </param>
        /// <returns>
        /// A validated <see cref="IngestionRequest" /> representing a delete operation.
        /// </returns>
        public static IngestionRequest CreateDelete(string id)
        {
            // Delegate validation to the canonical DTO constructors so the helper cannot drift from the wire contract.
            var deleteItem = new DeleteItemRequest(id);

            // Construct the envelope through the validated constructor so discriminator and payload alignment stays canonical.
            return new IngestionRequest(IngestionRequestType.DeleteItem, null, deleteItem, null);
        }

        /// <summary>
        /// Creates an ACL update envelope for the supplied document identifier and security-token set.
        /// </summary>
        /// <param name="id">
        /// The identifier of the document whose ACL should be updated.
        /// </param>
        /// <param name="securityTokens">
        /// The non-empty security-token set to apply.
        /// </param>
        /// <returns>
        /// A validated <see cref="IngestionRequest" /> representing an ACL update operation.
        /// </returns>
        public static IngestionRequest CreateAclUpdate(string id, string[] securityTokens)
        {
            // Delegate token and identifier validation to the canonical DTO constructor so helper behavior stays aligned.
            var updateAcl = new UpdateAclRequest(id, securityTokens);

            // Construct the envelope through the validated constructor so discriminator and payload alignment stays canonical.
            return new IngestionRequest(IngestionRequestType.UpdateAcl, null, null, updateAcl);
        }
    }
}