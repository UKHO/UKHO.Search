using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion.Contracts
{
    /// <summary>
    /// Represents the top-level ingestion queue-message envelope.
    /// </summary>
    public sealed record IngestionRequest
    {
        /// <summary>
        /// Initializes a validated ingestion request envelope.
        /// </summary>
        /// <param name="requestType">
        /// The discriminator describing which payload property is active.
        /// </param>
        /// <param name="indexItem">
        /// The indexing payload when <paramref name="requestType" /> is <see cref="IngestionRequestType.IndexItem" />.
        /// </param>
        /// <param name="deleteItem">
        /// The delete payload when <paramref name="requestType" /> is <see cref="IngestionRequestType.DeleteItem" />.
        /// </param>
        /// <param name="updateAcl">
        /// The ACL update payload when <paramref name="requestType" /> is <see cref="IngestionRequestType.UpdateAcl" />.
        /// </param>
        [JsonConstructor]
        public IngestionRequest(IngestionRequestType requestType, IndexRequest? indexItem, DeleteItemRequest? deleteItem, UpdateAclRequest? updateAcl)
        {
            RequestType = requestType;
            IndexItem = indexItem;
            DeleteItem = deleteItem;
            UpdateAcl = updateAcl;

            ValidateOneOf(RequestType, IndexItem, DeleteItem, UpdateAcl);
        }

        /// <summary>
        /// Initializes an empty instance for JSON deserialization.
        /// </summary>
        public IngestionRequest()
        {
        }

        /// <summary>
        /// Gets the discriminator identifying the active payload.
        /// </summary>
        [JsonPropertyName("RequestType")]
        public IngestionRequestType RequestType { get; init; }

        /// <summary>
        /// Gets the indexing payload when the request type is <see cref="IngestionRequestType.IndexItem" />.
        /// </summary>
        [JsonPropertyName("IndexItem")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IndexRequest? IndexItem { get; init; }

        /// <summary>
        /// Gets the delete payload when the request type is <see cref="IngestionRequestType.DeleteItem" />.
        /// </summary>
        [JsonPropertyName("DeleteItem")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DeleteItemRequest? DeleteItem { get; init; }

        /// <summary>
        /// Gets the ACL update payload when the request type is <see cref="IngestionRequestType.UpdateAcl" />.
        /// </summary>
        [JsonPropertyName("UpdateAcl")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public UpdateAclRequest? UpdateAcl { get; init; }

        /// <summary>
        /// Validates that exactly one payload is set and that it matches the request discriminator.
        /// </summary>
        /// <param name="requestType">
        /// The active request discriminator.
        /// </param>
        /// <param name="indexItem">
        /// The candidate indexing payload.
        /// </param>
        /// <param name="deleteItem">
        /// The candidate delete payload.
        /// </param>
        /// <param name="updateAcl">
        /// The candidate ACL update payload.
        /// </param>
        private static void ValidateOneOf(IngestionRequestType requestType, IndexRequest? indexItem, DeleteItemRequest? deleteItem, UpdateAclRequest? updateAcl)
        {
            var setCount = 0;
            if (indexItem is not null)
            {
                setCount++;
            }

            if (deleteItem is not null)
            {
                setCount++;
            }

            if (updateAcl is not null)
            {
                setCount++;
            }

            if (setCount != 1)
            {
                throw new JsonException("IngestionRequest must contain exactly one of IndexItem, DeleteItem, UpdateAcl.");
            }

            // The envelope contract requires the discriminator and the populated payload property to agree.
            var matches = requestType switch
            {
                IngestionRequestType.IndexItem => indexItem is not null,
                IngestionRequestType.DeleteItem => deleteItem is not null,
                IngestionRequestType.UpdateAcl => updateAcl is not null,
                var _ => throw new JsonException($"Unsupported IngestionRequestType '{requestType}'.")
            };

            if (!matches)
            {
                throw new JsonException($"IngestionRequest.RequestType is '{requestType}' but the corresponding payload property is missing.");
            }
        }
    }
}