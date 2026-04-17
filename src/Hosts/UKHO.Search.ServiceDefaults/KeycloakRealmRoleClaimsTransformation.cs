using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace UKHO.Search.ServiceDefaults
{
    /// <summary>
    /// Translates Keycloak realm-role claim shapes into the ASP.NET Core role-claim shape used by authorization policies.
    /// </summary>
    public sealed class KeycloakRealmRoleClaimsTransformation : IClaimsTransformation
    {
        private const string RealmAccessClaimType = "realm_access";
        private const string RealmAccessRolesClaimType = "realm_access.roles";
        private const string RolesClaimType = "roles";

        /// <summary>
        /// Adds normalized role claims to the supplied principal when Keycloak realm-role claims are present.
        /// </summary>
        /// <param name="principal">The current authenticated principal whose claims may require Keycloak-specific normalization.</param>
        /// <returns>The same principal instance after any missing ASP.NET role claims have been added.</returns>
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            // Claims transformations can run multiple times, so the implementation only adds role claims that do not already exist.
            ArgumentNullException.ThrowIfNull(principal);

            if (principal.Identity is not ClaimsIdentity identity)
            {
                return Task.FromResult(principal);
            }

            // Read the supported Keycloak claim shapes before mutating the identity so authorization sees one normalized role view.
            if (!TryGetRoles(identity.Claims, out var roles))
            {
                return Task.FromResult(principal);
            }

            foreach (var role in roles)
            {
                // Preserve idempotency because the transformation can be executed more than once in the same request pipeline.
                if (!identity.HasClaim(ClaimTypes.Role, role))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }
            }

            return Task.FromResult(principal);
        }

        /// <summary>
        /// Reads Keycloak realm roles from direct role claims or the embedded <c>realm_access</c> JSON claim.
        /// </summary>
        /// <param name="claims">The claims exposed by the current identity.</param>
        /// <param name="roles">The distinct realm roles discovered for the identity.</param>
        /// <returns><c>true</c> when one or more Keycloak realm roles were found; otherwise, <c>false</c>.</returns>
        private static bool TryGetRoles(IEnumerable<Claim> claims, out IReadOnlyList<string> roles)
        {
            // Prefer the already-flattened role claim shapes because they avoid reparsing JSON on every transformation.
            roles = claims
                .Where(claim => claim.Type is RolesClaimType or RealmAccessRolesClaimType)
                .Select(claim => claim.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (roles.Count > 0)
            {
                return true;
            }

            // Fall back to Keycloak's structured realm_access JSON when the direct claim mappers are not present.
            var realmAccess = claims.FirstOrDefault(claim => claim.Type == RealmAccessClaimType)?.Value;
            if (string.IsNullOrWhiteSpace(realmAccess))
            {
                return false;
            }

            return TryReadRealmRoles(realmAccess, out roles);
        }

        /// <summary>
        /// Parses the Keycloak <c>realm_access</c> JSON payload and extracts distinct string role values.
        /// </summary>
        /// <param name="realmAccessJson">The JSON payload stored in the Keycloak <c>realm_access</c> claim.</param>
        /// <param name="roles">The distinct roles discovered in the JSON payload.</param>
        /// <returns><c>true</c> when at least one valid role was read; otherwise, <c>false</c>.</returns>
        private static bool TryReadRealmRoles(string realmAccessJson, out IReadOnlyList<string> roles)
        {
            // Default to an empty role set so callers can safely use the out parameter on all failure paths.
            roles = Array.Empty<string>();

            try
            {
                using var document = JsonDocument.Parse(realmAccessJson);

                // Keycloak stores realm roles under the JSON property named "roles" when the claim is present.
                if (!document.RootElement.TryGetProperty("roles", out var rolesElement) || rolesElement.ValueKind != JsonValueKind.Array)
                {
                    return false;
                }

                var list = new List<string>();
                foreach (var item in rolesElement.EnumerateArray())
                {
                    // Ignore non-string entries so malformed claim payloads do not break the host request pipeline.
                    if (item.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var value = item.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        list.Add(value);
                    }
                }

                if (list.Count == 0)
                {
                    return false;
                }

                roles = list.Distinct(StringComparer.Ordinal).ToArray();
                return true;
            }
            catch (JsonException)
            {
                // Invalid realm_access JSON should simply leave the principal unchanged rather than failing the entire authentication flow.
                return false;
            }
        }
    }
}
