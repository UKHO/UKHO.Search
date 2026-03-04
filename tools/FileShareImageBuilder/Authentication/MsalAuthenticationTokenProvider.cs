using Microsoft.Identity.Client;
using UKHO.ADDS.Clients.Common.Authentication;

namespace FileShareImageBuilder.Authentication;

public sealed class MsalAuthenticationTokenProvider : IAuthenticationTokenProvider
{
    private readonly IPublicClientApplication _app;
    private readonly string[] _scopes;
    private AuthenticationResult? _result;

    public MsalAuthenticationTokenProvider(IPublicClientApplication app, IEnumerable<string> scopes)
    {
        _app = app ?? throw new ArgumentNullException(nameof(app));
        _scopes = (scopes ?? throw new ArgumentNullException(nameof(scopes))).ToArray();

        if (_scopes.Length == 0) throw new ArgumentException("At least one scope is required.", nameof(scopes));
    }

    public async Task<string> GetTokenAsync()
    {
        var accounts = (await _app.GetAccountsAsync().ConfigureAwait(false)).ToList();
        try
        {
            _result = await _app
                .AcquireTokenSilent(_scopes, accounts.FirstOrDefault())
                .ExecuteAsync()
                .ConfigureAwait(false);
        }
        catch (MsalUiRequiredException)
        {
            _result = await _app
                .AcquireTokenInteractive(_scopes)
                .ExecuteAsync()
                .ConfigureAwait(false);
        }

        return _result.AccessToken;
    }
}