using System.Net.Http.Json;
using System.Text.Json;

namespace Noundry.EnterpriseApiClient.Authentication;

public class OAuthAuthenticationProvider : IAuthenticationProvider
{
    private readonly HttpClient _httpClient;
    private readonly OAuthConfiguration _configuration;
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public OAuthAuthenticationProvider(HttpClient httpClient, OAuthConfiguration configuration)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (string.IsNullOrEmpty(_accessToken) || _tokenExpiry <= DateTime.UtcNow.AddMinutes(-5))
            {
                await RefreshTokenInternalAsync(cancellationToken);
            }

            return _accessToken!;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public async Task RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            await RefreshTokenInternalAsync(cancellationToken);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task RefreshTokenInternalAsync(CancellationToken cancellationToken)
    {
        var requestData = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _configuration.ClientId,
            ["client_secret"] = _configuration.ClientSecret
        };

        if (!string.IsNullOrEmpty(_configuration.Scope))
        {
            requestData["scope"] = _configuration.Scope;
        }

        using var content = new FormUrlEncodedContent(requestData);
        var response = await _httpClient.PostAsync(_configuration.TokenEndpoint, content, cancellationToken);
        
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);
        
        if (tokenResponse?.AccessToken == null)
        {
            throw new InvalidOperationException("Failed to retrieve access token from OAuth provider");
        }

        _accessToken = tokenResponse.AccessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 300); // 5 minutes buffer
    }

    private class TokenResponse
    {
        public string? AccessToken { get; set; }
        public int ExpiresIn { get; set; }
        public string? TokenType { get; set; }
    }
}