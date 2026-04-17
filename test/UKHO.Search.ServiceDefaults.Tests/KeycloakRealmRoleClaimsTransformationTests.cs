using System.Security.Claims;
using Shouldly;
using Xunit;

namespace UKHO.Search.ServiceDefaults.Tests
{
    /// <summary>
    /// Verifies the shared Keycloak realm-role normalization so browser hosts evaluate one consistent role-claim shape.
    /// </summary>
    public sealed class KeycloakRealmRoleClaimsTransformationTests
    {
        /// <summary>
        /// Verifies that roles stored inside the Keycloak <c>realm_access</c> JSON claim are promoted to ASP.NET Core role claims.
        /// </summary>
        [Fact]
        public async Task TransformAsync_adds_distinct_role_claims_from_the_realm_access_json_payload()
        {
            // Create a minimal Keycloak-style principal that carries realm roles in the structured JSON claim.
            var identity = new ClaimsIdentity(authenticationType: "test");
            identity.AddClaim(new Claim("realm_access", "{\"roles\":[\"role-a\",\"role-b\",\"role-a\"]}"));
            var principal = new ClaimsPrincipal(identity);

            var transformer = new KeycloakRealmRoleClaimsTransformation();
            var transformed = await transformer.TransformAsync(principal);

            // The shared transformer should add each distinct role exactly once in the ASP.NET role-claim shape.
            transformed.Claims.Count(claim => claim.Type == ClaimTypes.Role && claim.Value == "role-a").ShouldBe(1);
            transformed.Claims.Count(claim => claim.Type == ClaimTypes.Role && claim.Value == "role-b").ShouldBe(1);
        }

        /// <summary>
        /// Verifies that direct Keycloak role claims are normalized without duplicating role claims that already exist on the identity.
        /// </summary>
        [Fact]
        public async Task TransformAsync_uses_direct_role_claims_without_duplicating_existing_role_claims()
        {
            // Combine the direct Keycloak role claim shape with an existing ASP.NET role claim to exercise the idempotent merge path.
            var identity = new ClaimsIdentity(authenticationType: "test");
            identity.AddClaim(new Claim("roles", "role-a"));
            identity.AddClaim(new Claim("realm_access.roles", "role-b"));
            identity.AddClaim(new Claim(ClaimTypes.Role, "role-b"));
            var principal = new ClaimsPrincipal(identity);

            var transformer = new KeycloakRealmRoleClaimsTransformation();
            var transformed = await transformer.TransformAsync(principal);

            // The shared normalization should add the missing role while leaving the existing role claim un-duplicated.
            transformed.Claims.Count(claim => claim.Type == ClaimTypes.Role && claim.Value == "role-a").ShouldBe(1);
            transformed.Claims.Count(claim => claim.Type == ClaimTypes.Role && claim.Value == "role-b").ShouldBe(1);
        }
    }
}
