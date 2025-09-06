using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Noundry.Connector.Authentication;
using Noundry.Connector.Configuration;
using Noundry.Connector.Http;
using Refit;
using System.Text.Json;

namespace Noundry.Connector.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConnector<TClient, TEntity, TKey>(
        this IServiceCollection services,
        Action<ConnectorOptions> configureOptions,
        IAuthenticationProvider authenticationProvider)
        where TClient : class
        where TEntity : class
    {
        services.Configure(configureOptions);
        services.AddSingleton(authenticationProvider);
        services.AddTransient<AuthenticatedHttpMessageHandler>();

        services.AddHttpClient<TClient>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ConnectorOptions>>().Value;
            httpClient.BaseAddress = new Uri(options.BaseUrl);
            httpClient.Timeout = options.Timeout;

            foreach (var header in options.DefaultHeaders)
            {
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        })
        .AddHttpMessageHandler<AuthenticatedHttpMessageHandler>();

        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            })
        };

        services.AddRefitClient<TClient>(refitSettings)
            .ConfigureHttpClient((serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<ConnectorOptions>>().Value;
                httpClient.BaseAddress = new Uri(options.BaseUrl);
            })
            .AddHttpMessageHandler<AuthenticatedHttpMessageHandler>();

        return services;
    }

    public static IServiceCollection AddConnector<TClient>(
        this IServiceCollection services,
        Action<ConnectorOptions> configureOptions)
        where TClient : class
    {
        services.Configure(configureOptions);
        services.AddTransient<AuthenticatedHttpMessageHandler>();

        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            })
        };

        services.AddRefitClient<TClient>(refitSettings)
            .ConfigureHttpClient((serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<ConnectorOptions>>().Value;
                httpClient.BaseAddress = new Uri(options.BaseUrl);
                httpClient.Timeout = options.Timeout;

                foreach (var header in options.DefaultHeaders)
                {
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            })
            .AddHttpMessageHandler<AuthenticatedHttpMessageHandler>();

        return services;
    }

    public static IServiceCollection AddTokenAuthentication(
        this IServiceCollection services,
        string token)
    {
        services.AddSingleton<IAuthenticationProvider>(new TokenAuthenticationProvider(token));
        return services;
    }

    public static IServiceCollection AddOAuthAuthentication(
        this IServiceCollection services,
        Action<OAuthConfiguration> configureOAuth)
    {
        var oauthConfig = new OAuthConfiguration();
        configureOAuth(oauthConfig);

        services.AddHttpClient<OAuthAuthenticationProvider>();
        services.AddSingleton<IAuthenticationProvider>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(OAuthAuthenticationProvider));
            return new OAuthAuthenticationProvider(httpClient, oauthConfig);
        });

        return services;
    }
}