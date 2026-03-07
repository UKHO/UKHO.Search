namespace UKHO.Search.Pipelines.DeadLetter
{
    public sealed class DeadLetterMetadata
    {
        public string? AppVersion { get; init; }

        public string? CommitId { get; init; }

        public string? HostName { get; init; }
    }
}