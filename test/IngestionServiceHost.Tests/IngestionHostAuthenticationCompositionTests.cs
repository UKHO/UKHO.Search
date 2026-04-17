using Shouldly;
using Xunit;

namespace IngestionServiceHost.Tests
{
    /// <summary>
    /// Verifies that IngestionServiceHost consumes the shared browser-host authentication path and keeps the protected UI surface aligned with the shared Keycloak model.
    /// </summary>
    public sealed class IngestionHostAuthenticationCompositionTests
    {
        /// <summary>
        /// Verifies that the host bootstrap delegates browser authentication registration, lifecycle endpoint mapping, and authorization middleware ordering to the shared host-auth foundation.
        /// </summary>
        [Fact]
        public void Program_uses_the_shared_browser_host_authentication_path_for_the_ingestion_ui()
        {
            // Read the checked-in startup source because composition-root drift is most obvious at the host bootstrap level.
            var programSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "IngestionServiceHost", "Program.cs"));

            programSource.ShouldContain("AddKeycloakBrowserHostAuthentication(\"search-workbench\", \"ingestion\")");
            programSource.ShouldContain("MapKeycloakBrowserHostAuthenticationEndpoints()");
            programSource.ShouldContain("app.UseAuthentication();");
            programSource.ShouldContain("app.UseAuthorization();");

            // Keep the authentication pipeline ordering explicit so authorization evaluates the authenticated principal before the interactive UI endpoints run.
            programSource.IndexOf("app.MapKeycloakBrowserHostAuthenticationEndpoints();", StringComparison.Ordinal)
                .ShouldBeLessThan(programSource.IndexOf("app.UseAuthentication();", StringComparison.Ordinal));
            programSource.IndexOf("app.UseAuthentication();", StringComparison.Ordinal)
                .ShouldBeLessThan(programSource.IndexOf("app.UseAuthorization();", StringComparison.Ordinal));
            programSource.IndexOf("app.UseAuthorization();", StringComparison.Ordinal)
                .ShouldBeLessThan(programSource.IndexOf("app.MapRazorComponents<App>()", StringComparison.Ordinal));
        }

        /// <summary>
        /// Verifies that the host routes use authorization-aware Blazor routing and redirect unauthenticated users into the shared login lifecycle endpoint.
        /// </summary>
        [Fact]
        public void Routes_component_uses_authorization_aware_routing_and_redirects_unauthenticated_users_to_login()
        {
            // Read the route component source so the test can pin the authenticated routing surface without booting the full ingestion runtime.
            var routesSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "IngestionServiceHost", "Components", "Routes.razor"));

            routesSource.ShouldContain("<AuthorizeRouteView");
            routesSource.ShouldContain("<NotAuthorized>");
            routesSource.ShouldContain("<RedirectToLogin />");
            routesSource.ShouldNotContain("<RouteView RouteData=\"routeData\" DefaultLayout=\"typeof(MainLayout)\"/>");
        }

        /// <summary>
        /// Verifies that the host-local source does not introduce extra anonymous browser routes beyond the shared login and logout lifecycle endpoints.
        /// </summary>
        [Fact]
        public void Host_source_does_not_introduce_extra_local_anonymous_endpoint_mappings()
        {
            // Inspect the host bootstrap and route component source directly because extra anonymous mappings would represent accidental security drift.
            var programSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "IngestionServiceHost", "Program.cs"));
            var routesSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "IngestionServiceHost", "Components", "Routes.razor"));

            programSource.ShouldNotContain("AllowAnonymous");
            routesSource.ShouldNotContain("AllowAnonymous");
            programSource.ShouldNotContain("MapGet(\"/authentication/login\"");
            programSource.ShouldNotContain("MapGet(\"/authentication/logout\"");
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
