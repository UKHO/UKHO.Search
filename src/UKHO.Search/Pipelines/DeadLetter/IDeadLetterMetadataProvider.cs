namespace UKHO.Search.Pipelines.DeadLetter
{
    public interface IDeadLetterMetadataProvider
    {
        string? AppVersion { get; }

        string? CommitId { get; }

        string? HostName { get; }
    }
}