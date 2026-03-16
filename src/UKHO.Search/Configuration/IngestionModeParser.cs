namespace UKHO.Search.Configuration
{
    public static class IngestionModeParser
    {
        public static IngestionMode Parse(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException("Missing required environment variable 'ingestionmode'.");
            }

            if (!Enum.TryParse<IngestionMode>(value, ignoreCase: true, out var parsed))
            {
                throw new InvalidOperationException($"Invalid value '{value}' for environment variable 'ingestionmode'.");
            }

            return parsed;
        }
    }
}
