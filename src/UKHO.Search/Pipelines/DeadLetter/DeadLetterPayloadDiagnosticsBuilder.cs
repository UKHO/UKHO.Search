using System.Text.Json;

namespace UKHO.Search.Pipelines.DeadLetter
{
    public static class DeadLetterPayloadDiagnosticsBuilder
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public static DeadLetterPayloadDiagnostics Create<TPayload>(TPayload payload)
        {
            var runtimePayloadType = payload?.GetType() ?? typeof(TPayload);

            try
            {
                var payloadSnapshot = JsonSerializer.SerializeToElement((object?)payload, runtimePayloadType, _jsonOptions);

                return new DeadLetterPayloadDiagnostics
                {
                    RuntimePayloadType = runtimePayloadType.FullName ?? runtimePayloadType.Name,
                    PayloadSnapshot = payloadSnapshot
                };
            }
            catch (Exception ex)
            {
                return new DeadLetterPayloadDiagnostics
                {
                    RuntimePayloadType = runtimePayloadType.FullName ?? runtimePayloadType.Name,
                    SnapshotError = new DeadLetterPayloadSnapshotError
                    {
                        ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
                        ExceptionMessage = ex.Message
                    }
                };
            }
        }
    }
}
