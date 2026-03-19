using System.Text.Json;
using UKHO.Search.Infrastructure.Ingestion.Elastic;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Pipelines.DeadLetter;

namespace UKHO.Search.Infrastructure.Ingestion.DeadLetter
{
    internal static class IngestionDeadLetterPayloadDiagnosticsFactory
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public static DeadLetterPayloadDiagnostics Create<TPayload>(TPayload payload)
        {
            if (payload is not IndexOperation indexOperation)
            {
                return DeadLetterPayloadDiagnosticsBuilder.Create(payload);
            }

            try
            {
                var payloadSnapshot = JsonSerializer.SerializeToElement(CreateSnapshot(indexOperation), _jsonOptions);

                return new DeadLetterPayloadDiagnostics
                {
                    RuntimePayloadType = indexOperation.GetType().FullName ?? indexOperation.GetType().Name,
                    PayloadSnapshot = payloadSnapshot
                };
            }
            catch (Exception ex)
            {
                return new DeadLetterPayloadDiagnostics
                {
                    RuntimePayloadType = indexOperation.GetType().FullName ?? indexOperation.GetType().Name,
                    SnapshotError = new DeadLetterPayloadSnapshotError
                    {
                        ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
                        ExceptionMessage = ex.Message
                    }
                };
            }
        }

        private static object CreateSnapshot(IndexOperation operation)
        {
            return operation switch
            {
                UpsertOperation upsert => new
                {
                    upsert.DocumentId,
                    Document = ElasticsearchBulkIndexClient.CreateIndexDocument(upsert.Document)
                },
                DeleteOperation delete => new
                {
                    delete.DocumentId
                },
                AclUpdateOperation aclUpdate => new
                {
                    aclUpdate.DocumentId,
                    aclUpdate.SecurityTokens
                },
                _ => operation
            };
        }
    }
}
