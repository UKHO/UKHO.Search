using Shouldly;
using Xunit;

namespace WorkbenchHost.Tests
{
    /// <summary>
    /// Verifies that WorkbenchHost consumes the shared browser-host authentication composition instead of keeping host-local Keycloak wiring.
    /// </summary>
    public sealed class BrowserHostAuthenticationCompositionTests
    {
        /// <summary>
        /// Verifies that the Workbench host startup now delegates browser authentication registration and lifecycle endpoint mapping to the shared service-defaults extensions.
        /// </summary>
        [Fact]
        public void WorkbenchHost_program_uses_the_shared_browser_host_authentication_extensions()
        {
            // Read the checked-in startup source because composition-root drift is easiest to catch at the source level.
            var programSource = File.ReadAllText(GetRepositoryFilePath("src", "workbench", "server", "WorkbenchHost", "Program.cs"));

            programSource.ShouldContain("AddKeycloakBrowserHostAuthentication(\"search-workbench\", \"workbench\")");
            programSource.ShouldContain("MapKeycloakBrowserHostAuthenticationEndpoints()");
            programSource.ShouldNotContain("AddKeycloakOpenIdConnect(");
            programSource.ShouldNotContain("MapLoginAndLogout(");
        }

        /// <summary>
        /// Verifies that the Workbench host project no longer carries the Keycloak package references that now belong to the shared service-defaults project.
        /// </summary>
        [Fact]
        public void WorkbenchHost_project_moves_keycloak_package_references_to_the_shared_service_defaults_project()
        {
            // Read the checked-in project file so the shared authentication composition remains the only supported dependency path.
            var projectFileSource = File.ReadAllText(GetRepositoryFilePath("src", "workbench", "server", "WorkbenchHost", "WorkbenchHost.csproj"));

            projectFileSource.ShouldNotContain("Keycloak.AuthServices.Authentication");
            projectFileSource.ShouldNotContain("Aspire.Keycloak.Authentication");
        }

        /// <summary>
        /// Verifies that the host-local authentication helper files have been removed so future authentication changes cannot diverge silently inside WorkbenchHost.
        /// </summary>
        [Fact]
        public void WorkbenchHost_source_removes_the_host_local_authentication_helpers()
        {
            // Check the repository layout directly because deleted files are the clearest signal that the shared path replaced the host-local helpers.
            File.Exists(GetRepositoryFilePath("src", "workbench", "server", "WorkbenchHost", "Extensions", "LoginLogoutEndpointRouteBuilderExtensions.cs")).ShouldBeFalse();
            File.Exists(GetRepositoryFilePath("src", "workbench", "server", "WorkbenchHost", "Extensions", "KeycloakRealmRoleClaimsTransformation.cs")).ShouldBeFalse();
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
