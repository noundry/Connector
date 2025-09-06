namespace Noundry.EnterpriseApiClient.Authentication;

public class OAuthConfiguration
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = string.Empty;
    public string? Scope { get; set; }
}