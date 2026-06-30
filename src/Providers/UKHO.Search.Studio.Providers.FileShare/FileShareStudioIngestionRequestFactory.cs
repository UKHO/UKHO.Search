using System.Text.Json;
using UKHO.Search.Ingestion.Contracts;

namespace UKHO.Search.Studio.Providers.FileShare
{
    internal static class FileShareStudioIngestionRequestFactory
    {
        public static IngestionRequest Create(FileShareStudioBatchPayloadSource payloadSource)
        {
            ArgumentNullException.ThrowIfNull(payloadSource);

            var properties = new IngestionPropertyList();
            foreach (var attribute in payloadSource.Attributes)
            {
                if (string.IsNullOrWhiteSpace(attribute.Key))
                {
                    continue;
                }

                properties.Add(new IngestionProperty
                {
                    Name = attribute.Key,
                    Type = IngestionPropertyType.String,
                    Value = attribute.Value
                });
            }

            properties.Add(new IngestionProperty
            {
                Name = "BusinessUnitName",
                Type = IngestionPropertyType.String,
                Value = payloadSource.ActiveBusinessUnitName ?? string.Empty
            });

            var securityTokens = FileShareStudioSecurityTokenPolicy.CreateTokens(payloadSource.ActiveBusinessUnitName);

            try
            {
                return new IngestionRequest
                {
                    RequestType = IngestionRequestType.IndexItem,
                    IndexItem = new IndexRequest(
                        payloadSource.BatchId.ToString("D"),
                        properties,
                        securityTokens,
                        payloadSource.CreatedOn,
                        payloadSource.Files)
                };
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to create a file-share ingestion payload from the current batch data.", ex);
            }
        }
    }
}
