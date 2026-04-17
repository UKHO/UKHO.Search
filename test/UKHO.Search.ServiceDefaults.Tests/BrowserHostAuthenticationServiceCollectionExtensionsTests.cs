using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace UKHO.Search.ServiceDefaults.Tests
{
    /// <summary>
    /// Verifies the shared browser-host authentication composition so every consuming host inherits the same default schemes, fallback policy, and lifecycle routes.
    /// </summary>
    public sealed class BrowserHostAuthenticationServiceCollectionExtensionsTests
    {
        /// <summary>
        /// Verifies that the shared registration configures the expected cookie and OpenID Connect schemes together with the authenticated-user fallback policy.
        /// </summary>
        [Fact]
        public async Task AddKeycloakBrowserHostAuthentication_registers_the_expected_default_schemes_and_fallback_policy()
        {
            // Build a minimal service provider that exercises only the shared authentication composition root.
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
            services.AddLogging();
            services.AddKeycloakBrowserHostAuthentication("search-workbench", "query");

            await using var provider = services.BuildServiceProvider();

            // Read the configured authentication options so the regression test can pin the shared default schemes.
            var authenticationOptions = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
            authenticationOptions.DefaultScheme.ShouldBe(CookieAuthenticationDefaults.AuthenticationScheme);
            authenticationOptions.DefaultAuthenticateScheme.ShouldBe(CookieAuthenticationDefaults.AuthenticationScheme);
            authenticationOptions.DefaultSignInScheme.ShouldBe(CookieAuthenticationDefaults.AuthenticationScheme);
            authenticationOptions.DefaultChallengeScheme.ShouldBe(OpenIdConnectDefaults.AuthenticationScheme);

            // Confirm that both concrete schemes are present because the browser hosts depend on the cookie/OIDC split.
            var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
            var schemeNames = (await schemeProvider.GetAllSchemesAsync()).Select(scheme => scheme.Name).ToArray();
            schemeNames.ShouldContain(CookieAuthenticationDefaults.AuthenticationScheme);
            schemeNames.ShouldContain(OpenIdConnectDefaults.AuthenticationScheme);

            // Read the concrete cookie and OIDC options so the test can pin the localhost cookie-isolation settings that prevent cross-host collisions.
            var cookieAuthenticationOptionsMonitor = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
            var cookieAuthenticationOptions = cookieAuthenticationOptionsMonitor.Get(CookieAuthenticationDefaults.AuthenticationScheme);
            cookieAuthenticationOptions.Cookie.Name.ShouldBe(".UKHO.Search.query.Auth");
            cookieAuthenticationOptions.SessionStore.ShouldNotBeNull();

            var openIdConnectOptionsMonitor = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>();
            var openIdConnectOptions = openIdConnectOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme);
            openIdConnectOptions.SaveTokens.ShouldBeFalse();
            openIdConnectOptions.CorrelationCookie.Name.ShouldBe(".UKHO.Search.query.Correlation.");
            openIdConnectOptions.NonceCookie.Name.ShouldBe(".UKHO.Search.query.Nonce.");

            // Assert that the shared fallback policy still requires an authenticated user for normal browser routes.
            var authorizationOptions = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
            authorizationOptions.FallbackPolicy.ShouldNotBeNull();
            authorizationOptions.FallbackPolicy!.Requirements.OfType<DenyAnonymousAuthorizationRequirement>().Any().ShouldBeTrue();
        }

        /// <summary>
        /// Verifies that the shared endpoint mapping exposes only the expected anonymous login and logout lifecycle routes.
        /// </summary>
        [Fact]
        public void MapKeycloakBrowserHostAuthenticationEndpoints_maps_only_the_expected_anonymous_lifecycle_routes()
        {
            // Build a minimal application so the test can inspect the generated route metadata without booting a full host.
            var builder = WebApplication.CreateBuilder();
            var app = builder.Build();

            app.MapKeycloakBrowserHostAuthenticationEndpoints();

            // Collect the route endpoints and pin the exact lifecycle surface that hosts inherit from the shared extension.
            var routeBuilder = (IEndpointRouteBuilder)app;
            var routeEndpoints = routeBuilder.DataSources.SelectMany(dataSource => dataSource.Endpoints).OfType<RouteEndpoint>().ToArray();
            routeEndpoints.Length.ShouldBe(3);

            AssertAnonymousLifecycleEndpoint(routeEndpoints, "GET", "/authentication/login");
            AssertAnonymousLifecycleEndpoint(routeEndpoints, "GET", "/authentication/logout");
            AssertAnonymousLifecycleEndpoint(routeEndpoints, "POST", "/authentication/logout");
        }

        /// <summary>
        /// Finds one lifecycle endpoint and verifies that it stays anonymously reachable.
        /// </summary>
        /// <param name="routeEndpoints">The route endpoints generated by the shared lifecycle mapping.</param>
        /// <param name="httpMethod">The expected HTTP method for the lifecycle endpoint.</param>
        /// <param name="routePattern">The expected route pattern exposed by the lifecycle endpoint.</param>
        private static void AssertAnonymousLifecycleEndpoint(RouteEndpoint[] routeEndpoints, string httpMethod, string routePattern)
        {
            // Match by both route pattern and method so the test guards the exact anonymous exception set.
            var endpoint = routeEndpoints.Single(candidate =>
                string.Equals(candidate.RoutePattern.RawText, routePattern, StringComparison.Ordinal) &&
                candidate.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains(httpMethod, StringComparer.OrdinalIgnoreCase) == true);

            endpoint.Metadata.GetMetadata<IAllowAnonymous>().ShouldNotBeNull();
        }

        /// <summary>
        /// Supplies the minimal host-environment information required when the shared authentication options are materialized in tests.
        /// </summary>
        private sealed class TestHostEnvironment : IHostEnvironment
        {
            /// <summary>
            /// Gets or sets the logical environment name exposed to options configuration.
            /// </summary>
            public string EnvironmentName { get; set; } = Environments.Development;

            /// <summary>
            /// Gets or sets the logical application name exposed to options configuration.
            /// </summary>
            public string ApplicationName { get; set; } = "UKHO.Search.ServiceDefaults.Tests";

            /// <summary>
            /// Gets or sets the content-root path exposed to options configuration.
            /// </summary>
            public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

            /// <summary>
            /// Gets or sets the content-root file provider exposed to options configuration.
            /// </summary>
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }
    }
}
