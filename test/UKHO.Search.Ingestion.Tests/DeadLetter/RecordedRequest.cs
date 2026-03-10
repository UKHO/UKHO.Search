namespace UKHO.Search.Ingestion.Tests.DeadLetter
{
    public sealed class RecordedRequest
    {
        public RecordedRequest(string method, Uri uri, byte[]? body)
        {
            Method = method;
            Uri = uri;
            Body = body;
        }

        public string Method { get; }

        public Uri Uri { get; }

        public byte[]? Body { get; }
    }
}