using Azure.Core;
using Azure.Core.Pipeline;

namespace UKHO.Search.Ingestion.Tests.DeadLetter
{
    public sealed class RecordingTransport : HttpPipelineTransport
    {
        private readonly int _statusCode;

        public RecordingTransport(int statusCode)
        {
            _statusCode = statusCode;
        }

        public List<RecordedRequest> Requests { get; } = new();

        public override Request CreateRequest()
        {
            return new RecordingPipelineRequest();
        }

        public override void Process(HttpMessage message)
        {
            ProcessAsync(message)
                .AsTask()
                .GetAwaiter()
                .GetResult();
        }

        public override async ValueTask ProcessAsync(HttpMessage message)
        {
            var request = message.Request;

            byte[]? body = null;
            if (request.Content is not null)
            {
                using var ms = new MemoryStream();

                await request.Content.WriteToAsync(ms, CancellationToken.None)
                             .ConfigureAwait(false);
                body = ms.ToArray();
            }

            Requests.Add(new RecordedRequest(request.Method.Method, request.Uri.ToUri(), body));

            message.Response = new SimpleResponse(_statusCode);
        }
    }
}