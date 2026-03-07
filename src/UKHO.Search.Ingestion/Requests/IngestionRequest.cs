using System.Text.Json;
using System.Text.Json.Serialization;

namespace UKHO.Search.Ingestion.Requests
{
    public sealed record IngestionRequest
    {
        [JsonConstructor]
        public IngestionRequest(
            IngestionRequestType requestType,
            AddItemRequest? addItem,
            UpdateItemRequest? updateItem,
            DeleteItemRequest? deleteItem,
            UpdateAclRequest? updateAcl)
        {
            RequestType = requestType;
            AddItem = addItem;
            UpdateItem = updateItem;
            DeleteItem = deleteItem;
            UpdateAcl = updateAcl;

            ValidateOneOf(RequestType, AddItem, UpdateItem, DeleteItem, UpdateAcl);
        }

        public IngestionRequest()
        {
        }

        [JsonPropertyName("RequestType")]
        public IngestionRequestType RequestType { get; init; }

        [JsonPropertyName("AddItem")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public AddItemRequest? AddItem { get; init; }

        [JsonPropertyName("UpdateItem")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public UpdateItemRequest? UpdateItem { get; init; }

        [JsonPropertyName("DeleteItem")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DeleteItemRequest? DeleteItem { get; init; }

        [JsonPropertyName("UpdateAcl")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public UpdateAclRequest? UpdateAcl { get; init; }

        private static void ValidateOneOf(
            IngestionRequestType requestType,
            AddItemRequest? addItem,
            UpdateItemRequest? updateItem,
            DeleteItemRequest? deleteItem,
            UpdateAclRequest? updateAcl)
        {
            var setCount = 0;
            if (addItem is not null) setCount++;
            if (updateItem is not null) setCount++;
            if (deleteItem is not null) setCount++;
            if (updateAcl is not null) setCount++;

            if (setCount != 1)
                throw new JsonException(
                    "IngestionRequest must contain exactly one of AddItem, UpdateItem, DeleteItem, UpdateAcl.");

            var matches = requestType switch
            {
                IngestionRequestType.AddItem => addItem is not null,
                IngestionRequestType.UpdateItem => updateItem is not null,
                IngestionRequestType.DeleteItem => deleteItem is not null,
                IngestionRequestType.UpdateAcl => updateAcl is not null,
                _ => throw new JsonException($"Unsupported IngestionRequestType '{requestType}'.")
            };

            if (!matches)
                throw new JsonException(
                    $"IngestionRequest.RequestType is '{requestType}' but the corresponding payload property is missing.");
        }
    }
}
