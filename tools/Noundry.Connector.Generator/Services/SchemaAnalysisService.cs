using Noundry.Connector.Generator.Extensions;
using Noundry.Connector.Generator.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Noundry.Connector.Generator.Services;

public class SchemaAnalysisService
{
    private readonly HttpClient _httpClient;

    public SchemaAnalysisService()
    {
        _httpClient = new HttpClient(new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        })
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task<List<SchemaModel>> AnalyzeEndpointsAsync(ApiConfiguration config)
    {
        var schemas = new Dictionary<string, SchemaModel>();
        var client = CreateAuthenticatedClient(config);

        foreach (var endpoint in config.Endpoints.Where(e => e.Method == "GET").Take(10)) // Analyze up to 10 GET endpoints
        {
            try
            {
                var response = await client.GetAsync($"{config.BaseUrl.TrimEnd('/')}{endpoint.Path}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    if (!string.IsNullOrEmpty(content) && IsJson(content))
                    {
                        var schema = AnalyzeJsonResponse(content, endpoint);
                        if (schema != null && !schemas.ContainsKey(schema.Name))
                        {
                            schemas[schema.Name] = schema;
                        }
                    }
                }
            }
            catch
            {
                // Continue with next endpoint if this one fails
            }
        }

        return schemas.Values.ToList();
    }

    private SchemaModel? AnalyzeJsonResponse(string jsonContent, ApiEndpoint endpoint)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            var modelName = InferModelNameFromEndpoint(endpoint.Path);

            if (root.ValueKind == JsonValueKind.Array)
            {
                // Handle array responses
                if (root.GetArrayLength() > 0)
                {
                    var firstElement = root.EnumerateArray().First();
                    var itemSchema = AnalyzeJsonElement(firstElement, modelName.Singularize());
                    
                    return new SchemaModel
                    {
                        Name = modelName,
                        OriginalName = modelName,
                        IsCollection = true,
                        Properties = itemSchema?.Properties ?? new Dictionary<string, SchemaProperty>(),
                        Description = $"Collection of {modelName.Singularize()} items"
                    };
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                return AnalyzeJsonElement(root, modelName);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private SchemaModel? AnalyzeJsonElement(JsonElement element, string modelName)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        var schema = new SchemaModel
        {
            Name = modelName.ToPascalCase(),
            OriginalName = modelName,
            Properties = new Dictionary<string, SchemaProperty>()
        };

        foreach (var property in element.EnumerateObject())
        {
            var schemaProperty = AnalyzeJsonProperty(property);
            if (schemaProperty != null)
            {
                schema.Properties[schemaProperty.Name] = schemaProperty;
            }
        }

        return schema;
    }

    private SchemaProperty? AnalyzeJsonProperty(JsonProperty property)
    {
        var propertyName = property.Name.ToPascalCase();
        var schemaProperty = new SchemaProperty
        {
            Name = propertyName,
            OriginalName = property.Name
        };

        switch (property.Value.ValueKind)
        {
            case JsonValueKind.String:
                schemaProperty.Type = "string";
                schemaProperty.CSharpType = "string";
                
                // Try to infer more specific types based on format
                var stringValue = property.Value.GetString() ?? "";
                if (DateTime.TryParse(stringValue, out _))
                {
                    schemaProperty.CSharpType = "DateTime";
                    schemaProperty.Format = "date-time";
                }
                else if (Guid.TryParse(stringValue, out _))
                {
                    schemaProperty.CSharpType = "Guid";
                    schemaProperty.Format = "guid";
                }
                else if (Uri.TryCreate(stringValue, UriKind.Absolute, out _))
                {
                    schemaProperty.CSharpType = "Uri";
                    schemaProperty.Format = "uri";
                }
                break;

            case JsonValueKind.Number:
                schemaProperty.Type = "number";
                if (property.Value.TryGetInt32(out _))
                {
                    schemaProperty.CSharpType = "int";
                }
                else if (property.Value.TryGetInt64(out _))
                {
                    schemaProperty.CSharpType = "long";
                }
                else
                {
                    schemaProperty.CSharpType = "decimal";
                }
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                schemaProperty.Type = "boolean";
                schemaProperty.CSharpType = "bool";
                break;

            case JsonValueKind.Array:
                schemaProperty.Type = "array";
                schemaProperty.IsCollection = true;
                
                if (property.Value.GetArrayLength() > 0)
                {
                    var firstElement = property.Value.EnumerateArray().First();
                    var elementType = InferArrayElementType(firstElement);
                    schemaProperty.CSharpType = $"List<{elementType}>";
                    
                    if (firstElement.ValueKind == JsonValueKind.Object)
                    {
                        // Create nested model for complex objects
                        var nestedModelName = propertyName.Singularize();
                        schemaProperty.NestedModel = AnalyzeJsonElement(firstElement, nestedModelName);
                    }
                }
                else
                {
                    schemaProperty.CSharpType = "List<object>";
                }
                break;

            case JsonValueKind.Object:
                schemaProperty.Type = "object";
                var nestedObjectName = propertyName;
                schemaProperty.CSharpType = nestedObjectName;
                schemaProperty.NestedModel = AnalyzeJsonElement(property.Value, nestedObjectName);
                break;

            case JsonValueKind.Null:
                schemaProperty.Type = "null";
                schemaProperty.CSharpType = "object";
                schemaProperty.IsNullable = true;
                break;

            default:
                schemaProperty.Type = "unknown";
                schemaProperty.CSharpType = "object";
                break;
        }

        return schemaProperty;
    }

    private string InferArrayElementType(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => "string",
            JsonValueKind.Number => element.TryGetInt32(out _) ? "int" : "decimal",
            JsonValueKind.True or JsonValueKind.False => "bool",
            JsonValueKind.Object => "object", // Will be replaced with specific type name
            _ => "object"
        };
    }

    private string InferModelNameFromEndpoint(string path)
    {
        // Remove leading slash and parameters
        var cleanPath = path.TrimStart('/');
        
        // Remove path parameters like {id}
        var parts = cleanPath.Split('/')
            .Where(p => !p.StartsWith("{") && !p.StartsWith(":"))
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        if (parts.Length > 0)
        {
            var lastPart = parts.Last();
            
            // Remove version numbers
            if (lastPart.StartsWith("v") && char.IsDigit(lastPart[1]))
            {
                lastPart = parts.Length > 1 ? parts[^2] : "Item";
            }

            return lastPart.ToPascalCase();
        }

        return "ApiResponse";
    }

    private bool IsJson(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        content = content.Trim();
        return (content.StartsWith("{") && content.EndsWith("}")) ||
               (content.StartsWith("[") && content.EndsWith("]"));
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
                if (!string.IsNullOrEmpty(config.ClientId) && !string.IsNullOrEmpty(config.ClientSecret))
                {
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