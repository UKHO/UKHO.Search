using System.Reflection;

namespace UKHO.Search.Pipelines.DeadLetter
{
    public sealed class DefaultDeadLetterMetadataProvider : IDeadLetterMetadataProvider
    {
        public string? AppVersion
        {
            get
            {
                var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                   ?.InformationalVersion;
                if (!string.IsNullOrWhiteSpace(info))
                {
                    return info;
                }

                return assembly.GetName()
                               .Version?.ToString();
            }
        }

        public string? CommitId
        {
            get
            {
                var values = new[]
                {
                    Environment.GetEnvironmentVariable("BUILD_SOURCEVERSION"),
                    Environment.GetEnvironmentVariable("GITHUB_SHA"),
                    Environment.GetEnvironmentVariable("GIT_COMMIT")
                };

                foreach (var value in values)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }

                return null;
            }
        }

        public string? HostName => Environment.MachineName;
    }
}