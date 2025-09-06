namespace Noundry.EnterpriseApiClient.Authentication;

public interface IAuthenticationProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    Task RefreshTokenAsync(CancellationToken cancellationToken = default);
}