using Shouldly;
using Xunit;

namespace AppHost.Tests
{
    /// <summary>
    /// Verifies the AppHost source contract that keeps local rule seeding rooted at the repository rules directory while preserving the shared rules prefix.
    /// </summary>
    public sealed class RuleConfigurationSeederPathTests
    {
        /// <summary>
        /// Verifies that services-mode AppHost configuration seeding still points at the repository rules root and writes keys beneath the shared rules prefix.
        /// </summary>
        [Fact]
        public void AppHost_services_mode_seeds_the_repository_rules_root_with_the_rules_prefix()
        {
            // Read the AppHost source because this regression is defined by the orchestration call site rather than by isolated executable logic.
            var appHostSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "AppHost", "AppHost.cs"));

            // Pin the repository-relative rules path so local seeding continues to discover rules from the top-level rules directory.
            appHostSource.ShouldContain("var rulesPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, \"..\", \"..\", \"..\", \"rules\"));");

            // Pin the AddConfigurationEmulator call so the AppHost keeps using the shared rules prefix for additional configuration seeding.
            appHostSource.ShouldContain("additionalConfigurationPath: rulesPath");
            appHostSource.ShouldContain("additionalConfigurationPrefix: \"rules\"");
        }

        /// <summary>
        /// Resolves a repository-relative file path from the test output directory.
        /// </summary>
        /// <param name="pathSegments">The repository-relative path segments to combine.</param>
        /// <returns>The absolute path to the requested repository file.</returns>
        private static string GetRepositoryFilePath(params string[] pathSegments)
        {
            // Walk up from the test output directory until the repository root marker is found.
            var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

            while (currentDirectory is not null)
            {
                var solutionPath = Path.Combine(currentDirectory.FullName, "Search.slnx");

                if (File.Exists(solutionPath))
                {
                    return Path.Combine([currentDirectory.FullName, .. pathSegments]);
                }

                currentDirectory = currentDirectory.Parent;
            }

            throw new InvalidOperationException("The repository root could not be located from the test output directory.");
        }
    }
}
