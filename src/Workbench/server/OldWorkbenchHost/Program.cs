using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Radzen;
using System.IdentityModel.Tokens.Jwt;
using OldWorkbenchHost.Components;
using UKHO.Search.ServiceDefaults;
using UKHO.Workbench.Infrastructure;
using WorkbenchHost.Components;
using WorkbenchHost.Extensions;

namespace WorkbenchHost
{
    /// <summary>
    /// Hosts the temporary Workbench Blazor shell and configures its supporting infrastructure.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Builds and runs the Workbench host application.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the host process.</param>
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Apply the shared service-default configuration used across the repository's hosts.
            builder.AddServiceDefaults();

            // Register the workbench infrastructure services required by the host.
            builder.Services.AddWorkbenchInfrastructure();

            // Enable Razor components and interactive server rendering for the temporary shell.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Register Radzen services so the host can render the temporary shell using Radzen assets.
            builder.Services.AddRadzenComponents();

            // Provide the current HTTP context and authorization helpers required by the host.
            builder.Services.AddHttpContextAccessor().AddTransient<AuthorizationHandler>();

            // Normalize realm-role claims before authorization runs against the current principal.
            builder.Services.AddTransient<IClaimsTransformation, KeycloakRealmRoleClaimsTransformation>();

            var oidcScheme = OpenIdConnectDefaults.AuthenticationScheme;

            // Configure cookie-backed OpenID Connect authentication against the shared Keycloak realm.
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = oidcScheme;
            })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddKeycloakOpenIdConnect("keycloak", "ukho-search", oidcScheme, options =>
                {
                    options.ClientId = "search-workbench";
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
                    options.SaveTokens = true;
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                });

            // Flow the authenticated user through the Blazor component tree.
            builder.Services.AddCascadingAuthenticationState();

            // Require authenticated users by default for the workbench surface.
            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            var app = builder.Build();

            // Map the shared default endpoints such as health checks before middleware execution.
            app.MapDefaultEndpoints();

            if (!app.Environment.IsDevelopment())
            {
                // Use production-friendly exception and transport-security handling outside development.
                app.UseExceptionHandler("/");
                app.UseHsts();
            }

            // Redirect to HTTPS and enable antiforgery protections for interactive component requests.
            app.UseHttpsRedirection();
            app.UseAntiforgery();

            // Expose login and logout endpoints before the authenticated UI pipeline executes.
            app.MapLoginAndLogout();

            // Authenticate requests before enforcing the fallback authorization policy.
            app.UseAuthentication();
            app.UseAuthorization();

            // Serve static assets and map the root Blazor component with interactive server rendering.
            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            // Start processing requests for the configured workbench host pipeline.
            app.Run();
        }
    }
}