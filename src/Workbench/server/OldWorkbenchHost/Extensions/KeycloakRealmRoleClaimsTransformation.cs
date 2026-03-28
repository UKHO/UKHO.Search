using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace WorkbenchHost.Extensions
{
    public sealed class KeycloakRealmRoleClaimsTransformation : IClaimsTransformation
    {
        private const string RealmAccessClaimType = "realm_access";
        private const string RealmAccessRolesClaimType = "realm_access.roles";
        private const string RolesClaimType = "roles";

        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            ArgumentNullException.ThrowIfNull(principal);

            if (principal.Identity is not ClaimsIdentity identity)
            {
                return Task.FromResult(principal);
            }

            if (!TryGetRoles(identity.Claims, out var roles))
            {
                return Task.FromResult(principal);
            }

            foreach (var role in roles)
            {
                if (!identity.HasClaim(ClaimTypes.Role, role))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }
            }

            return Task.FromResult(principal);
        }

        private static bool TryGetRoles(IEnumerable<Claim> claims, out IReadOnlyList<string> roles)
        {
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

            var realmAccess = claims.FirstOrDefault(claim => claim.Type == RealmAccessClaimType)?.Value;
            if (string.IsNullOrWhiteSpace(realmAccess))
            {
                return false;
            }

            return TryReadRealmRoles(realmAccess, out roles);
        }
        private static bool TryReadRealmRoles(string realmAccessJson, out IReadOnlyList<string> roles)
        {
            roles = Array.Empty<string>();

            try
            {
                using var document = JsonDocument.Parse(realmAccessJson);

                if (!document.RootElement.TryGetProperty("roles", out var rolesElement) || rolesElement.ValueKind != JsonValueKind.Array)
                {
                    return false;
                }

                var list = new List<string>();
                foreach (var item in rolesElement.EnumerateArray())
                {
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
                return false;
            }
        }
    }
}
