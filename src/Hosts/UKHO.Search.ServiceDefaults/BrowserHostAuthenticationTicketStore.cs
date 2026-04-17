using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace UKHO.Search.ServiceDefaults
{
    /// <summary>
    /// Stores browser-host authentication tickets in memory so the authentication cookie only needs to carry a compact session identifier.
    /// </summary>
    internal sealed class BrowserHostAuthenticationTicketStore : ITicketStore
    {
        private static readonly TimeSpan TicketLifetime = TimeSpan.FromHours(8);
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrowserHostAuthenticationTicketStore"/> class.
        /// </summary>
        /// <param name="memoryCache">The in-memory cache that retains browser-host authentication tickets for the lifetime of the host process.</param>
        public BrowserHostAuthenticationTicketStore(IMemoryCache memoryCache)
        {
            // Keep a cache dependency rather than storing tickets statically so each host process owns and cleans up its own authentication sessions.
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        /// <summary>
        /// Stores a new authentication ticket and returns the generated session identifier that will be written into the cookie.
        /// </summary>
        /// <param name="ticket">The authenticated browser-host ticket to retain outside the cookie payload.</param>
        /// <returns>The generated session identifier that the cookie middleware can round-trip in the browser.</returns>
        public Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            // Generate a compact opaque key so the browser cookie contains only an identifier instead of the full serialized principal.
            ArgumentNullException.ThrowIfNull(ticket);

            var key = $"browser-host-ticket:{Guid.NewGuid():N}";
            SetTicket(key, ticket);
            return Task.FromResult(key);
        }

        /// <summary>
        /// Replaces an existing stored ticket with the latest authenticated state for the same session identifier.
        /// </summary>
        /// <param name="key">The opaque session identifier currently stored in the browser cookie.</param>
        /// <param name="ticket">The updated authentication ticket to persist for the active session.</param>
        /// <returns>A completed task after the cached ticket has been refreshed.</returns>
        public Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            // Refresh the cached ticket and its lifetime whenever the cookie middleware renews the browser session.
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(ticket);

            SetTicket(key, ticket);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Retrieves a previously stored authentication ticket for the supplied session identifier.
        /// </summary>
        /// <param name="key">The opaque session identifier read from the browser cookie.</param>
        /// <returns>The stored authentication ticket when present; otherwise <see langword="null"/>.</returns>
        public Task<AuthenticationTicket?> RetrieveAsync(string key)
        {
            // Return the cached ticket if it still exists so the browser host can reconstruct the authenticated principal.
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            _memoryCache.TryGetValue(key, out AuthenticationTicket? ticket);
            return Task.FromResult(ticket);
        }

        /// <summary>
        /// Removes a stored authentication ticket when the browser session signs out or expires.
        /// </summary>
        /// <param name="key">The opaque session identifier that should no longer resolve to an authentication ticket.</param>
        /// <returns>A completed task after the cached ticket has been removed.</returns>
        public Task RemoveAsync(string key)
        {
            // Remove the cached ticket so signed-out browser sessions cannot continue to resolve to an authenticated principal.
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            _memoryCache.Remove(key);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Writes an authentication ticket into the shared in-memory cache with the standard browser-host ticket lifetime.
        /// </summary>
        /// <param name="key">The opaque session identifier under which the ticket should be stored.</param>
        /// <param name="ticket">The authentication ticket to retain outside the browser cookie.</param>
        private void SetTicket(string key, AuthenticationTicket ticket)
        {
            // Use sliding expiration so active local developer sessions remain valid while abandoned sessions age out automatically.
            _memoryCache.Set(
                key,
                ticket,
                new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TicketLifetime
                });
        }
    }
}
