using System.Text.Json;
using Shouldly;
using Xunit;

namespace AppHost.Tests
{
    /// <summary>
    /// Verifies that the shared Keycloak browser-host client allows the redirect and logout callback URLs used by the local browser hosts.
    /// </summary>
    public sealed class KeycloakBrowserHostClientConfigurationTests
    {
        /// <summary>
        /// Verifies that the shared <c>search-workbench</c> client includes the redirect, logout, and web-origin values needed by WorkbenchHost, IngestionServiceHost, and QueryServiceHost.
        /// </summary>
        [Fact]
        public void Search_workbench_client_allows_the_local_browser_host_redirect_and_logout_urls()
        {
            // Load the checked-in realm export because the redirect-uri contract is enforced by Keycloak configuration rather than by C# startup code alone.
            using var realmDocument = JsonDocument.Parse(File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "AppHost", "Realms", "ukho-search-realm.json")));

            // Locate the shared browser-host client so the test can pin the exact redirect and origin values that local development requires.
            var searchWorkbenchClient = realmDocument.RootElement
                .GetProperty("clients")
                .EnumerateArray()
                .Single(client => string.Equals(client.GetProperty("clientId").GetString(), "search-workbench", StringComparison.Ordinal));

            var redirectUris = searchWorkbenchClient
                .GetProperty("redirectUris")
                .EnumerateArray()
                .Select(element => element.GetString())
                .Where(value => value is not null)
                .Cast<string>()
                .ToArray();

            var webOrigins = searchWorkbenchClient
                .GetProperty("webOrigins")
                .EnumerateArray()
                .Select(element => element.GetString())
                .Where(value => value is not null)
                .Cast<string>()
                .ToArray();

            var postLogoutRedirectUris = searchWorkbenchClient
                .GetProperty("attributes")
                .GetProperty("post.logout.redirect.uris")
                .GetString()?
                .Split("##", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                ?? [];

            // Workbench keeps its existing stable HTTPS callback while the other browser hosts add their own local HTTPS callbacks.
            redirectUris.ShouldContain("https://localhost:10000/signin-oidc");
            redirectUris.ShouldContain("https://localhost:7152/signin-oidc");
            redirectUris.ShouldContain("https://localhost:7161/signin-oidc");

            // Every participating browser host must be a valid origin for the shared public client during local development.
            webOrigins.ShouldContain("https://localhost:10000");
            webOrigins.ShouldContain("https://localhost:7152");
            webOrigins.ShouldContain("https://localhost:7161");

            // Logout must also permit every browser host to receive the post-logout return navigation.
            postLogoutRedirectUris.ShouldContain("https://localhost:10000/signout-callback-oidc");
            postLogoutRedirectUris.ShouldContain("https://localhost:10000");
            postLogoutRedirectUris.ShouldContain("https://localhost:7152/signout-callback-oidc");
            postLogoutRedirectUris.ShouldContain("https://localhost:7152");
            postLogoutRedirectUris.ShouldContain("https://localhost:7161/signout-callback-oidc");
            postLogoutRedirectUris.ShouldContain("https://localhost:7161");
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
