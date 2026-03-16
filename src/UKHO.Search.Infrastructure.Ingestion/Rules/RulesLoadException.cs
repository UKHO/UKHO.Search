namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    internal class RulesLoadException : Exception
    {
        public RulesLoadException(string message)
            : base(message)
        {
        }

        public RulesLoadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
