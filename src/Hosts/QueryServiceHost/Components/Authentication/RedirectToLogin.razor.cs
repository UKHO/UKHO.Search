using Microsoft.AspNetCore.Components;
using UKHO.Search.ServiceDefaults;

namespace QueryServiceHost.Components.Authentication
{
    /// <summary>
    /// Redirects unauthenticated users from the Blazor router to the shared browser-host login lifecycle endpoint.
    /// </summary>
    public partial class RedirectToLogin : ComponentBase
    {
        /// <summary>
        /// Gets or sets the navigation manager used to send the browser to the shared login endpoint.
        /// </summary>
        [Inject]
        public NavigationManager NavigationManager { get; set; } = null!;

        /// <summary>
        /// Navigates the browser to the shared login endpoint when the unauthorized route content is rendered.
        /// </summary>
        protected override void OnInitialized()
        {
            // Force a full-page navigation so the browser leaves the interactive circuit and begins the server-side OpenID Connect challenge flow.
            NavigationManager.NavigateTo(
                $"{BrowserHostAuthenticationDefaults.AuthenticationPathPrefix}/login",
                forceLoad: true);
        }
    }
}
