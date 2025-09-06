using Noundry.Connector.Generator.Extensions;
using Noundry.Connector.Generator.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Noundry.Connector.Generator.Services;

public class ApiDiscoveryService
{
    private readonly HttpClient _httpClient;

    public ApiDiscoveryService()
    {
        _httpClient = new HttpClient(new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        })
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task<TestResult> TestConnectivityAsync(string baseUrl)
    {
        try
        {
            var response = await _httpClient.GetAsync(baseUrl);
            
            return new TestResult
            {
                IsSuccess = true,
                StatusCode = (int)response.StatusCode,
                ResponseContent = await response.Content.ReadAsStringAsync()
            };
        }
        catch (HttpRequestException ex)
        {
            return new TestResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
        catch (TaskCanceledException ex)
        {
            return new TestResult
            {
                IsSuccess = false,
                ErrorMessage = "Request timeout"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<TestResult> TestAuthenticationAsync(ApiConfiguration config)
    {
        try
        {
            using var client = CreateAuthenticatedClient(config);
            
            // Try a simple GET request to the base URL or common endpoints
            var testUrls = new[]
            {
                config.BaseUrl,
                $"{config.BaseUrl.TrimEnd('/')}/",
                $"{config.BaseUrl.TrimEnd('/')}/api",
                $"{config.BaseUrl.TrimEnd('/')}/health",
                $"{config.BaseUrl.TrimEnd('/')}/ping"
            };

            TestResult? lastResult = null;
            
            foreach (var url in testUrls)
            {
                try
                {
                    var response = await client.GetAsync(url);
                    lastResult = new TestResult
                    {
                        IsSuccess = response.IsSuccessStatusCode,
                        StatusCode = (int)response.StatusCode,
                        ResponseContent = await response.Content.ReadAsStringAsync()
                    };

                    if (response.IsSuccessStatusCode)
                    {
                        return lastResult;
                    }
                }
                catch (Exception ex)
                {
                    lastResult = new TestResult
                    {
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    };
                }
            }

            return lastResult ?? new TestResult { IsSuccess = false, ErrorMessage = "All test URLs failed" };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<List<ApiEndpoint>> DiscoverEndpointsAsync(ApiConfiguration config)
    {
        var endpoints = new List<ApiEndpoint>();

        try
        {
            using var client = CreateAuthenticatedClient(config);

            // Try to discover endpoints through common patterns
            await TryDiscoverOpenApiEndpointsAsync(client, config.BaseUrl, endpoints);
            await TryDiscoverCommonEndpointsAsync(client, config.BaseUrl, endpoints);
            await TryDiscoverRestfulEndpointsAsync(client, config.BaseUrl, endpoints);

            return endpoints.DistinctBy(e => $"{e.Method}:{e.Path}").ToList();
        }
        catch
        {
            return endpoints;
        }
    }

    private async Task TryDiscoverOpenApiEndpointsAsync(HttpClient client, string baseUrl, List<ApiEndpoint> endpoints)
    {
        var openApiUrls = new[]
        {
            $"{baseUrl.TrimEnd('/')}/swagger.json",
            $"{baseUrl.TrimEnd('/')}/swagger/v1/swagger.json",
            $"{baseUrl.TrimEnd('/')}/api/swagger.json",
            $"{baseUrl.TrimEnd('/')}/openapi.json",
            $"{baseUrl.TrimEnd('/')}/api-docs",
            $"{baseUrl.TrimEnd('/')}/docs.json"
        };

        foreach (var url in openApiUrls)
        {
            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var swaggerEndpoints = ParseSwaggerJson(content);
                    endpoints.AddRange(swaggerEndpoints);
                    return; // Found OpenAPI spec, no need to try others
                }
            }
            catch
            {
                // Continue to next URL
            }
        }
    }

    private async Task TryDiscoverCommonEndpointsAsync(HttpClient client, string baseUrl, List<ApiEndpoint> endpoints)
    {
        var commonPaths = new[]
        {
            "/api", "/v1", "/v2", "/v3",
            "/users", "/user", "/customers", "/customer",
            "/products", "/product", "/items", "/item",
            "/orders", "/order", "/transactions", "/transaction",
            "/posts", "/post", "/articles", "/article",
            "/auth", "/login", "/token",
            "/health", "/status", "/ping", "/version"
        };

        var basePath = baseUrl.TrimEnd('/');
        
        foreach (var path in commonPaths)
        {
            try
            {
                var response = await client.GetAsync($"{basePath}{path}");
                
                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    endpoints.Add(new ApiEndpoint
                    {
                        Method = "GET",
                        Path = path,
                        Description = $"GET {path}",
                        StatusCode = (int)response.StatusCode
                    });
                }
            }
            catch
            {
                // Continue to next path
            }
        }
    }

    private async Task TryDiscoverRestfulEndpointsAsync(HttpClient client, string baseUrl, List<ApiEndpoint> endpoints)
    {
        // Look for RESTful patterns based on discovered endpoints
        var basePaths = endpoints
            .Where(e => e.Method == "GET")
            .Select(e => e.Path)
            .Where(p => !p.Contains("{") && !p.Contains(":"))
            .ToList();

        foreach (var basePath in basePaths)
        {
            // Try RESTful operations
            var restfulEndpoints = new[]
            {
                new { Method = "POST", Path = basePath, Description = $"Create new {basePath.TrimStart('/').Singularize()}" },
                new { Method = "GET", Path = $"{basePath}/{{id}}", Description = $"Get {basePath.TrimStart('/').Singularize()} by ID" },
                new { Method = "PUT", Path = $"{basePath}/{{id}}", Description = $"Update {basePath.TrimStart('/').Singularize()}" },
                new { Method = "PATCH", Path = $"{basePath}/{{id}}", Description = $"Partially update {basePath.TrimStart('/').Singularize()}" },
                new { Method = "DELETE", Path = $"{basePath}/{{id}}", Description = $"Delete {basePath.TrimStart('/').Singularize()}" }
            };

            foreach (var endpoint in restfulEndpoints)
            {
                if (!endpoints.Any(e => e.Method == endpoint.Method && e.Path == endpoint.Path))
                {
                    endpoints.Add(new ApiEndpoint
                    {
                        Method = endpoint.Method,
                        Path = endpoint.Path,
                        Description = endpoint.Description
                    });
                }
            }
        }
    }

    private List<ApiEndpoint> ParseSwaggerJson(string swaggerJson)
    {
        var endpoints = new List<ApiEndpoint>();
        
        try
        {
            using var doc = JsonDocument.Parse(swaggerJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("paths", out var paths))
            {
                foreach (var path in paths.EnumerateObject())
                {
                    foreach (var method in path.Value.EnumerateObject())
                    {
                        var endpoint = new ApiEndpoint
                        {
                            Method = method.Name.ToUpper(),
                            Path = path.Name,
                            Description = GetSwaggerDescription(method.Value)
                        };

                        endpoints.Add(endpoint);
                    }
                }
            }
        }
        catch
        {
            // If parsing fails, return empty list
        }

        return endpoints;
    }

    private string GetSwaggerDescription(JsonElement operation)
    {
        if (operation.TryGetProperty("summary", out var summary))
        {
            return summary.GetString() ?? "";
        }

        if (operation.TryGetProperty("description", out var description))
        {
            return description.GetString() ?? "";
        }

        if (operation.TryGetProperty("operationId", out var operationId))
        {
            return operationId.GetString() ?? "";
        }

        return "";
    }

    private HttpClient CreateAuthenticatedClient(ApiConfiguration config)
    {
        var client = new HttpClient(new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        })
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Add authentication headers
        switch (config.AuthType)
        {
            case AuthenticationType.ApiKey:
                if (!string.IsNullOrEmpty(config.ApiKey))
                {
                    client.DefaultRequestHeaders.Add(config.ApiKeyHeader ?? "X-API-Key", config.ApiKey);
                }
                break;

            case AuthenticationType.BearerToken:
                if (!string.IsNullOrEmpty(config.BearerToken))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.BearerToken);
                }
                break;

            case AuthenticationType.BasicAuth:
                if (!string.IsNullOrEmpty(config.Username) && !string.IsNullOrEmpty(config.Password))
                {
                    var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.Username}:{config.Password}"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                }
                break;

            case AuthenticationType.OAuth2:
                // For OAuth2, we would need to get a token first
                // This is a simplified version - in reality, you'd implement the OAuth flow
                if (!string.IsNullOrEmpty(config.ClientId) && !string.IsNullOrEmpty(config.ClientSecret))
                {
                    // This is a placeholder - implement proper OAuth2 flow based on the API
                    // For now, we'll just add the client credentials as a header (some APIs support this)
                    var clientCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.ClientId}:{config.ClientSecret}"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", clientCredentials);
                }
                break;
        }

        // Add common headers
        client.DefaultRequestHeaders.Add("User-Agent", "Noundry.Connector.Generator/1.0.0");
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        return client;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}