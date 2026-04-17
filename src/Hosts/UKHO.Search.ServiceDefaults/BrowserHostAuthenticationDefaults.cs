namespace UKHO.Search.ServiceDefaults
{
    /// <summary>
    /// Defines the shared constants that keep browser-host authentication wiring aligned across hosts.
    /// </summary>
    public static class BrowserHostAuthenticationDefaults
    {
        /// <summary>
        /// The shared Aspire resource name that resolves the Keycloak identity provider inside the local developer environment.
        /// </summary>
        public const string KeycloakServiceName = "keycloak";

        /// <summary>
        /// The Keycloak realm that currently backs browser-host authentication for the repository.
        /// </summary>
        public const string RealmName = "ukho-search";

        /// <summary>
        /// The route prefix that hosts use for explicit authentication lifecycle endpoints.
        /// </summary>
        public const string AuthenticationPathPrefix = "/authentication";

        /// <summary>
        /// The shell-relative path that the login and logout lifecycle endpoints redirect back to after authentication completes.
        /// </summary>
        public const string ShellRedirectPath = "/";
    }
}
