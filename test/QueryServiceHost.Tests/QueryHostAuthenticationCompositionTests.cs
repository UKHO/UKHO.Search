using Shouldly;
using Xunit;

namespace QueryServiceHost.Tests
{
    /// <summary>
    /// Verifies that QueryServiceHost consumes the shared browser-host authentication path and protects the interactive query UI.
    /// </summary>
    public sealed class QueryHostAuthenticationCompositionTests
    {
        /// <summary>
        /// Verifies that the host bootstrap delegates browser authentication registration, lifecycle endpoint mapping, and middleware ordering to the shared host-auth foundation.
        /// </summary>
        [Fact]
        public void Program_uses_the_shared_browser_host_authentication_path_for_the_query_ui()
        {
            // Read the checked-in startup source because authentication-composition drift is easiest to detect at the host bootstrap level.
            var programSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "QueryServiceHost", "Program.cs"));

            programSource.ShouldContain("AddKeycloakBrowserHostAuthentication(\"search-workbench\", \"query\")");
            programSource.ShouldContain("MapKeycloakBrowserHostAuthenticationEndpoints()");
            programSource.ShouldContain("app.UseAuthentication();");
            programSource.ShouldContain("app.UseAuthorization();");
            programSource.ShouldContain("AddSingleton<IQueryUiSearchClient, QueryUiSearchClient>()");
            programSource.ShouldNotContain("AddKeycloakOpenIdConnect(");
            programSource.ShouldNotContain("AddSingleton<IQueryUiSearchClient, StubQueryUiSearchClient>()");

            // Keep the authentication pipeline ordering explicit so the authenticated principal exists before the protected Query UI endpoints are mapped.
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
            // Read the route component source so the test can pin the authenticated routing surface without booting the full Query host runtime.
            var routesSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "QueryServiceHost", "Components", "Routes.razor"));
            var redirectComponentSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "QueryServiceHost", "Components", "Authentication", "RedirectToLogin.razor.cs"));
            var homePageSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "QueryServiceHost", "Components", "Pages", "Home.razor"));

            routesSource.ShouldContain("<AuthorizeRouteView");
            routesSource.ShouldContain("<NotAuthorized>");
            routesSource.ShouldContain("<RedirectToLogin />");
            routesSource.ShouldNotContain("<RouteView RouteData=\"routeData\" DefaultLayout=\"typeof(MainLayout)\"/>");
            redirectComponentSource.ShouldContain("BrowserHostAuthenticationDefaults.AuthenticationPathPrefix");
            redirectComponentSource.ShouldContain("forceLoad: true");
            homePageSource.ShouldContain("@rendermode InteractiveServer");
        }

        /// <summary>
        /// Verifies that the state-driven query UI components subscribe to shared state changes so completed searches refresh the visible panels.
        /// </summary>
        [Fact]
        public void State_driven_query_ui_components_subscribe_to_shared_state_changes()
        {
            // Read the component sources directly because the regression is about component subscription wiring rather than runtime behavior under a test host.
            var searchBarSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "QueryServiceHost", "Components", "SearchBar.razor"));
            var resultsPanelSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "QueryServiceHost", "Components", "ResultsPanel.razor"));
            var queryPlanPanelSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "QueryServiceHost", "Components", "QueryPlanPanel.razor"));
            var queryInsightPanelSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "QueryServiceHost", "Components", "QueryInsightPanel.razor"));
            var queryDiagnosticsPanelSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "QueryServiceHost", "Components", "QueryDiagnosticsPanel.razor"));
            var resultExplainDrawerSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "QueryServiceHost", "Components", "ResultExplainDrawer.razor"));

            searchBarSource.ShouldContain("@implements IDisposable");
            searchBarSource.ShouldContain("State.Changed += OnStateChanged;");
            searchBarSource.ShouldContain("State.Changed -= OnStateChanged;");
            resultsPanelSource.ShouldContain("@implements IDisposable");
            resultsPanelSource.ShouldContain("State.Changed += OnStateChanged;");
            resultsPanelSource.ShouldContain("State.Changed -= OnStateChanged;");
            queryPlanPanelSource.ShouldContain("@implements IDisposable");
            queryPlanPanelSource.ShouldContain("State.Changed += OnStateChanged;");
            queryPlanPanelSource.ShouldContain("State.Changed -= OnStateChanged;");
            queryInsightPanelSource.ShouldContain("@implements IDisposable");
            queryInsightPanelSource.ShouldContain("State.Changed += OnStateChanged;");
            queryInsightPanelSource.ShouldContain("State.Changed -= OnStateChanged;");
            queryDiagnosticsPanelSource.ShouldContain("@implements IDisposable");
            queryDiagnosticsPanelSource.ShouldContain("State.Changed += OnStateChanged;");
            queryDiagnosticsPanelSource.ShouldContain("State.Changed -= OnStateChanged;");
            resultExplainDrawerSource.ShouldContain("@implements IDisposable");
            resultExplainDrawerSource.ShouldContain("State.Changed += OnStateChanged;");
            resultExplainDrawerSource.ShouldContain("State.Changed -= OnStateChanged;");
        }

        /// <summary>
        /// Verifies that the home page uses the new single-screen workspace shell, replaces the old details column, and loads the Monaco prerequisites.
        /// </summary>
        [Fact]
        public void Home_page_uses_the_single_screen_workspace_shell_and_loads_monaco_prerequisites()
        {
            // Read the checked-in host sources directly because this regression is about shell composition and startup assets.
            var homePageSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "QueryServiceHost", "Components", "Pages", "Home.razor"));
            var searchBarSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "QueryServiceHost", "Components", "SearchBar.razor"));
            var appSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "QueryServiceHost", "Components", "App.razor"));
            var monacoInteropSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "QueryServiceHost", "wwwroot", "js", "monacoEditorInterop.js"));

            homePageSource.ShouldContain("<QueryInsightPanel />");
            homePageSource.ShouldContain("<QueryPlanPanel />");
            homePageSource.ShouldContain("<ResultsPanel />");
            homePageSource.ShouldContain("<QueryDiagnosticsPanel />");
            homePageSource.ShouldContain("<ResultExplainDrawer />");
            homePageSource.ShouldNotContain("<DetailsPanel />");
            searchBarSource.ShouldContain("Text=\"Run raw query\"");
            homePageSource.ShouldNotContain("State.EditablePlanText");
            var queryPlanPanelSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "QueryServiceHost", "Components", "QueryPlanPanel.razor"));
            queryPlanPanelSource.ShouldContain("Value=\"@State.EditablePlanText\"");
            appSource.ShouldContain("require.min.js");
            monacoInteropSource.ShouldContain("cdn.jsdelivr.net/npm/requirejs@2.3.6/require.min.js");
            monacoInteropSource.ShouldContain("monacoLoaderPromise = null;");
        }

        /// <summary>
        /// Verifies that the host-local source does not introduce extra anonymous endpoint mappings beyond the shared authentication lifecycle routes.
        /// </summary>
        [Fact]
        public void Host_source_does_not_introduce_extra_local_anonymous_endpoint_mappings()
        {
            // Inspect the host bootstrap and route component source directly because extra anonymous mappings would represent accidental security drift.
            var programSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "QueryServiceHost", "Program.cs"));
            var routesSource = File.ReadAllText(GetRepositoryFilePath("src", "Hosts", "QueryServiceHost", "Components", "Routes.razor"));

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
