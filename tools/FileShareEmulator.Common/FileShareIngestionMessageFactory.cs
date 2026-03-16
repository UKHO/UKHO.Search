using System.Text.Json;
using UKHO.Search.Ingestion.Requests;

namespace FileShareEmulator.Common
{
    public static class FileShareIngestionMessageFactory
    {
        public static IngestionRequest CreateIndexIngestionRequest(
            string batchId,
            IReadOnlyCollection<IngestionProperty> attributes,
            DateTimeOffset batchCreatedOn,
            IngestionFileList files,
            string? activeBusinessUnitName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(batchId);
            ArgumentNullException.ThrowIfNull(attributes);
            ArgumentNullException.ThrowIfNull(files);

            var properties = new IngestionPropertyList();

            foreach (var attr in attributes)
            {
                if (attr is null)
                {
                    continue;
                }

                properties.Add(attr);
            }

            properties.Add(new IngestionProperty
            {
                Name = "BusinessUnitName",
                Type = IngestionPropertyType.String,
                Value = activeBusinessUnitName ?? string.Empty
            });

            var securityTokens = SecurityTokenPolicy.CreateTokens(activeBusinessUnitName);

            try
            {
                var indexItem = new IndexRequest(batchId, properties, securityTokens, batchCreatedOn, files);

                return new IngestionRequest
                {
                    RequestType = IngestionRequestType.IndexItem,
                    IndexItem = indexItem
                };
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to create ingestion request due to invalid payload data.", ex);
            }
        }
    }
}
