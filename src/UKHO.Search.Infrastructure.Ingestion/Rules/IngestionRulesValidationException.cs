namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    public sealed class IngestionRulesValidationException : Exception
    {
        public IngestionRulesValidationException(string message, IReadOnlyList<string>? errors = null, Exception? innerException = null) : base(message, innerException)
        {
            Errors = errors ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Errors { get; }
    }
}