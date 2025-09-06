namespace Noundry.EnterpriseApiClient.Configuration;

public class EnterpriseApiClientOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
}