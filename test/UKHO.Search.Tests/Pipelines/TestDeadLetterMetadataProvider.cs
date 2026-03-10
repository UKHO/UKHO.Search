using UKHO.Search.Pipelines.DeadLetter;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class TestDeadLetterMetadataProvider : IDeadLetterMetadataProvider
    {
        public TestDeadLetterMetadataProvider(string? appVersion, string? commitId, string? hostName)
        {
            AppVersion = appVersion;
            CommitId = commitId;
            HostName = hostName;
        }

        public string? AppVersion { get; }

        public string? CommitId { get; }

        public string? HostName { get; }
    }
}