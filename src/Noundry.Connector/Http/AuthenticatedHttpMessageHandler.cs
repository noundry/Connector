using Noundry.Connector.Authentication;

namespace Noundry.Connector.Http;

public class AuthenticatedHttpMessageHandler : DelegatingHandler
{
    private readonly IAuthenticationProvider _authProvider;

    public AuthenticatedHttpMessageHandler(IAuthenticationProvider authProvider)
    {
        _authProvider = authProvider ?? throw new ArgumentNullException(nameof(authProvider));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _authProvider.GetAccessTokenAsync(cancellationToken);
        
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Handle 401 Unauthorized by refreshing token and retrying once
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await _authProvider.RefreshTokenAsync(cancellationToken);
            
            var newToken = await _authProvider.GetAccessTokenAsync(cancellationToken);
            if (!string.IsNullOrEmpty(newToken))
            {
                // Clone the request for retry
                var retryRequest = await CloneRequestAsync(request);
                retryRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newToken);
                
                response.Dispose();
                response = await base.SendAsync(retryRequest, cancellationToken);
            }
        }

        return response;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri)
        {
            Version = original.Version
        };

        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (original.Content != null)
        {
            var contentBytes = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);

            foreach (var header in original.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }
}