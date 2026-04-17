using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace UKHO.Search.ServiceDefaults
{
    /// <summary>
    /// Maps the shared login and logout lifecycle endpoints used by interactive browser hosts.
    /// </summary>
    public static class BrowserHostAuthenticationEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Maps the anonymous authentication lifecycle endpoints that start sign-in and complete sign-out for browser hosts.
        /// </summary>
        /// <param name="endpoints">The route builder that receives the shared authentication lifecycle endpoints.</param>
        /// <returns>The endpoint convention builder for the grouped lifecycle routes.</returns>
        public static IEndpointConventionBuilder MapKeycloakBrowserHostAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
        {
            // Keep the lifecycle routes grouped under one prefix so every host exposes the same login and logout surface.
            ArgumentNullException.ThrowIfNull(endpoints);

            var group = endpoints.MapGroup(BrowserHostAuthenticationDefaults.AuthenticationPathPrefix);

            // Login must remain anonymous so an unauthenticated browser can start the OpenID Connect challenge flow.
            group.MapGet("login", OnLogin).AllowAnonymous();

            // Logout is also anonymous so stale or partially expired sessions can still be cleared without first re-authenticating.
            group.MapGet("logout", OnLogout).AllowAnonymous();
            group.MapPost("logout", OnLogout).AllowAnonymous();

            return group;
        }

        /// <summary>
        /// Creates the challenge result that sends the browser to Keycloak and then back to the shell root.
        /// </summary>
        /// <returns>The challenge result executed by the login lifecycle endpoint.</returns>
        private static IResult OnLogin()
        {
            // Send the browser back to the shell root after the external identity provider completes sign-in.
            return Results.Challenge(properties: new AuthenticationProperties
            {
                RedirectUri = BrowserHostAuthenticationDefaults.ShellRedirectPath
            });
        }

        /// <summary>
        /// Creates the sign-out result that clears the local cookie session and the upstream OpenID Connect session.
        /// </summary>
        /// <returns>The sign-out result executed by the logout lifecycle endpoints.</returns>
        private static IResult OnLogout()
        {
            // Sign out of both schemes so the next visit performs a fresh round-trip through Keycloak.
            return Results.SignOut(properties: new AuthenticationProperties
                {
                    RedirectUri = BrowserHostAuthenticationDefaults.ShellRedirectPath
                },
                [
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    OpenIdConnectDefaults.AuthenticationScheme
                ]);
        }
    }
}
