using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace UKHO.Search.ServiceDefaults
{
    /// <summary>
    /// Adds the shared cookie-backed OpenID Connect authentication model used by interactive browser hosts.
    /// </summary>
    public static class BrowserHostAuthenticationServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the shared Keycloak authentication flow, claims transformation, Blazor authentication-state support, and fallback authorization policy for a browser host.
        /// </summary>
        /// <param name="services">The service collection that receives the shared browser-host authentication services.</param>
        /// <param name="clientId">The Keycloak client identifier that the consuming host uses for browser sign-in.</param>
        /// <param name="browserHostKey">The short host-specific key used to isolate localhost authentication cookies between browser hosts.</param>
        /// <returns>The same service collection so host startup can continue fluent registration.</returns>
        public static IServiceCollection AddKeycloakBrowserHostAuthentication(this IServiceCollection services, string clientId, string browserHostKey)
        {
            // Validate the shared authentication inputs early so misconfiguration fails during startup instead of during the first browser challenge.
            ArgumentNullException.ThrowIfNull(services);
            ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
            ArgumentException.ThrowIfNullOrWhiteSpace(browserHostKey);

            // Normalize the host key once so every cookie created for this browser host stays isolated from the other localhost browser hosts.
            var normalizedBrowserHostKey = NormalizeBrowserHostKey(browserHostKey);
            var authenticationCookieNamePrefix = $".UKHO.Search.{normalizedBrowserHostKey}";

            // Store authentication tickets server-side so the localhost browser hosts do not each carry a large serialized principal in a cookie.
            services.AddMemoryCache();
            services.AddSingleton<BrowserHostAuthenticationTicketStore>();

            // The interactive hosts keep the local browser session in a cookie and delegate login/logout challenges to the shared OpenID Connect scheme.
            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    // Give each localhost browser host its own auth-cookie name so one host does not try to read another host's session cookie.
                    options.Cookie.Name = $"{authenticationCookieNamePrefix}.Auth";
                })
                .AddKeycloakOpenIdConnect(
                    BrowserHostAuthenticationDefaults.KeycloakServiceName,
                    BrowserHostAuthenticationDefaults.RealmName,
                    OpenIdConnectDefaults.AuthenticationScheme,
                    options =>
                    {
                        // Preserve the existing Workbench flow so the refactor changes composition, not identity-provider behavior.
                        options.ClientId = clientId;
                        options.ResponseType = OpenIdConnectResponseType.Code;
                        options.RequireHttpsMetadata = false;
                        options.TokenValidationParameters.NameClaimType = "name";
                        // Keep the auth cookie small because localhost browser hosts share a domain and otherwise quickly exhaust request-header limits.
                        options.SaveTokens = false;
                        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                        // Isolate the transient OIDC cookies per host so starting authentication in one localhost host does not pollute the others.
                        options.CorrelationCookie.Name = $"{authenticationCookieNamePrefix}.Correlation.";
                        options.NonceCookie.Name = $"{authenticationCookieNamePrefix}.Nonce.";
                    });

            // Keep the browser cookie compact by storing the full authentication ticket in memory instead of round-tripping it through every localhost host request.
            services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
                .Configure<BrowserHostAuthenticationTicketStore>((options, ticketStore) =>
                {
                    options.SessionStore = ticketStore;
                });

            // Normalize Keycloak realm-role claims into ASP.NET role claims before authorization evaluates the active principal.
            services.AddTransient<IClaimsTransformation, KeycloakRealmRoleClaimsTransformation>();

            // Flow the authenticated principal through the interactive Blazor component tree.
            services.AddCascadingAuthenticationState();

            // Protect the browser host by default so only explicitly anonymous lifecycle endpoints bypass authentication.
            services.AddAuthorizationBuilder()
                .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build());

            return services;
        }

        /// <summary>
        /// Converts a host key into a cookie-safe identifier that can be shared across authentication cookie names.
        /// </summary>
        /// <param name="browserHostKey">The raw host key supplied by the consuming browser host.</param>
        /// <returns>A normalized host key containing only lowercase letters and digits.</returns>
        private static string NormalizeBrowserHostKey(string browserHostKey)
        {
            // Strip punctuation so the cookie-name prefix remains stable and safe even if a host passes a descriptive identifier.
            var normalizedHostKey = string.Concat(browserHostKey.Where(char.IsLetterOrDigit))
                .ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(normalizedHostKey))
            {
                throw new ArgumentException("The browser host key must contain at least one letter or digit.", nameof(browserHostKey));
            }

            return normalizedHostKey;
        }
    }
}
