using Shouldly;
using Xunit;

namespace AppHost.Tests
{
    /// <summary>
    /// Verifies that the AppHost wires QueryServiceHost to the shared Keycloak resource so browser authentication can resolve the identity provider endpoint at runtime.
    /// </summary>
    public sealed class QueryHostKeycloakReferenceTests
    {
        /// <summary>
        /// Verifies that the services-mode AppHost graph gives QueryServiceHost a Keycloak reference and startup dependency.
        /// </summary>
        [Fact]
        public void AppHost_services_mode_wires_the_query_host_to_keycloak()
        {
            // Read the AppHost source because this regression is defined by the orchestration graph rather than by isolated host code.
            var appHostSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "AppHost", "AppHost.cs"));

            appHostSource.ShouldContain(".WithReference(keycloak)");
            appHostSource.ShouldContain(".WaitFor(keycloak)");

            // Pin the query-service slice specifically so future edits do not move the Keycloak reference onto only other hosts.
            var queryServiceDeclarationIndex = appHostSource.IndexOf("var queryService = builder.AddProject<QueryServiceHost>", StringComparison.Ordinal);
            queryServiceDeclarationIndex.ShouldBeGreaterThanOrEqualTo(0);

            var fileShareEmulatorDeclarationIndex = appHostSource.IndexOf("var fileShareEmulator = builder.AddProject<FileShareEmulator>", StringComparison.Ordinal);
            fileShareEmulatorDeclarationIndex.ShouldBeGreaterThan(queryServiceDeclarationIndex);

            var queryServiceSource = appHostSource.Substring(queryServiceDeclarationIndex, fileShareEmulatorDeclarationIndex - queryServiceDeclarationIndex);
            queryServiceSource.ShouldContain(".WithReference(keycloak)");
            queryServiceSource.ShouldContain(".WaitFor(keycloak)");
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
