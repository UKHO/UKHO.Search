using Azure;
using Azure.Core;

namespace UKHO.Search.Ingestion.Tests.DeadLetter
{
    public sealed class SimpleResponse : Response
    {
        private readonly Dictionary<string, List<string>> _headers = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _reasonPhrase;
        private readonly int _status;

        public SimpleResponse(int status, string reasonPhrase = "")
        {
            _status = status;
            _reasonPhrase = reasonPhrase;
        }

        public override int Status => _status;

        public override string ReasonPhrase => _reasonPhrase;

        public override Stream? ContentStream { get; set; }

        public override string ClientRequestId { get; set; } = string.Empty;

        public override void Dispose()
        {
        }

        protected override bool ContainsHeader(string name)
        {
            return _headers.ContainsKey(name);
        }

        protected override IEnumerable<HttpHeader> EnumerateHeaders()
        {
            foreach (var (name, values) in _headers)
            {
                foreach (var value in values)
                {
                    yield return new HttpHeader(name, value);
                }
            }
        }

        protected override bool TryGetHeader(string name, out string? value)
        {
            if (_headers.TryGetValue(name, out var values) && values.Count > 0)
            {
                value = values[0];
                return true;
            }

            value = null;
            return false;
        }

        protected override bool TryGetHeaderValues(string name, out IEnumerable<string>? values)
        {
            if (_headers.TryGetValue(name, out var list))
            {
                values = list;
                return true;
            }

            values = null;
            return false;
        }
    }
}