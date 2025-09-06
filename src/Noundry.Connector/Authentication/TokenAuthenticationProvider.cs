namespace Noundry.Connector.Authentication;

public class TokenAuthenticationProvider : IAuthenticationProvider
{
    private readonly string _token;

    public TokenAuthenticationProvider(string token)
    {
        _token = token ?? throw new ArgumentNullException(nameof(token));
    }

    public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_token);
    }

    public Task RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}