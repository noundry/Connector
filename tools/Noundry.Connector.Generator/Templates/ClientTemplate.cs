using Noundry.Connector.Generator.Extensions;
using Noundry.Connector.Generator.Models;
using System.Text;

namespace Noundry.Connector.Generator.Templates;

public class ClientTemplate
{
    public string Generate(ApiConfiguration config)
    {
        var sb = new StringBuilder();

        // File header
        sb.AppendLine("using Noundry.Connector.Clients;");
        
        if (config.GenerateModels && config.Schemas.Any())
        {
            sb.AppendLine($"using {config.Namespace}.Models;");
        }
        
        sb.AppendLine();

        // Namespace
        sb.AppendLine($"namespace {config.Namespace};");
        sb.AppendLine();

        // Class documentation
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Client wrapper for {config.ApiName} API using Noundry.Connector");
        sb.AppendLine("/// </summary>");

        // Class declaration
        var primaryEntityType = GetPrimaryEntityType(config);
        var primaryKeyType = GetPrimaryKeyType(config);

        sb.AppendLine($"public class {config.ApiName}Client : BaseApiClient<{primaryEntityType}, {primaryKeyType}>");
        sb.AppendLine("{");

        // Private field for the Refit client
        sb.AppendLine($"    private readonly I{config.ApiName}Api _api;");
        sb.AppendLine();

        // Constructor
        sb.AppendLine($"    public {config.ApiName}Client(I{config.ApiName}Api api) : base(api as IRefitClient<{primaryEntityType}, {primaryKeyType}>)");
        sb.AppendLine("    {");
        sb.AppendLine("        _api = api ?? throw new ArgumentNullException(nameof(api));");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate custom methods for each endpoint group
        GenerateCustomMethods(sb, config, "    ");

        sb.AppendLine("}");

        return sb.ToString();
    }

    private void GenerateCustomMethods(StringBuilder sb, ApiConfiguration config, string indent)
    {
        // Group endpoints by resource
        var endpointGroups = config.Endpoints
            .GroupBy(e => ExtractResourceName(e.Path))
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .ToList();

        foreach (var group in endpointGroups)
        {
            var resourceName = group.Key.ToPascalCase();
            var endpoints = group.ToList();

            // Generate methods for this resource group
            foreach (var endpoint in endpoints)
            {
                GenerateCustomMethod(sb, endpoint, resourceName, config, indent);
            }
        }

        // Add convenience methods
        GenerateConvenienceMethods(sb, config, indent);
    }

    private void GenerateCustomMethod(StringBuilder sb, ApiEndpoint endpoint, string resourceName, ApiConfiguration config, string indent)
    {
        var methodName = GenerateMethodName(endpoint, resourceName);
        var returnType = GetReturnType(endpoint, resourceName, config);
        var parameters = GenerateParameters(endpoint, resourceName);

        // Method documentation
        if (!string.IsNullOrEmpty(endpoint.Description))
        {
            sb.AppendLine($"{indent}/// <summary>");
            sb.AppendLine($"{indent}/// {endpoint.Description}");
            sb.AppendLine($"{indent}/// </summary>");
        }

        // Method signature
        sb.AppendLine($"{indent}public async Task<{returnType}> {methodName}({parameters}CancellationToken cancellationToken = default)");
        sb.AppendLine($"{indent}{{");

        // Method body - delegate to Refit client
        var refitMethodName = GenerateRefitMethodName(endpoint);
        var refitParameters = GenerateRefitMethodCall(endpoint);

        sb.AppendLine($"{indent}    return await _api.{refitMethodName}({refitParameters}cancellationToken);");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    private void GenerateConvenienceMethods(StringBuilder sb, ApiConfiguration config, string indent)
    {
        // Add search method if we have GET endpoints
        var getEndpoints = config.Endpoints.Where(e => e.Method.ToLowerInvariant() == "get").ToList();
        
        if (getEndpoints.Any(e => !e.Path.Contains("{")))
        {
            sb.AppendLine($"{indent}/// <summary>");
            sb.AppendLine($"{indent}/// Search and filter results using LINQ");
            sb.AppendLine($"{indent}/// </summary>");
            sb.AppendLine($"{indent}public async Task<IEnumerable<T>> SearchAsync<T>(Func<T, bool> predicate, CancellationToken cancellationToken = default)");
            sb.AppendLine($"{indent}    where T : class");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    // This is a placeholder for custom search logic");
            sb.AppendLine($"{indent}    // Implement based on your specific API requirements");
            sb.AppendLine($"{indent}    throw new NotImplementedException(\"Implement search logic based on your API\");");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
        }
    }

    private string GenerateMethodName(ApiEndpoint endpoint, string resourceName)
    {
        var methodPrefix = endpoint.Method.ToLowerInvariant() switch
        {
            "get" => endpoint.Path.Contains("{") ? "Get" : "GetAll",
            "post" => "Create",
            "put" => "Update",
            "patch" => "UpdatePartial", 
            "delete" => "Delete",
            _ => endpoint.Method.ToPascalCase()
        };

        if (methodPrefix == "GetAll")
        {
            resourceName = resourceName.Pluralize();
        }

        return $"{methodPrefix}{resourceName}Async";
    }

    private string GetReturnType(ApiEndpoint endpoint, string resourceName, ApiConfiguration config)
    {
        // Try to find matching schema
        var matchingSchema = config.Schemas.FirstOrDefault(s => 
            s.Name.Equals(resourceName, StringComparison.OrdinalIgnoreCase) ||
            s.Name.Equals(resourceName.Singularize(), StringComparison.OrdinalIgnoreCase));

        if (matchingSchema != null)
        {
            return endpoint.Method.ToLowerInvariant() switch
            {
                "get" when !endpoint.Path.Contains("{") => $"List<{matchingSchema.Name}>",
                "get" => matchingSchema.Name,
                "post" or "put" or "patch" => matchingSchema.Name,
                "delete" => "bool",
                _ => "object"
            };
        }

        return endpoint.Method.ToLowerInvariant() switch
        {
            "get" when !endpoint.Path.Contains("{") => "List<object>",
            "get" => "object",
            "delete" => "bool",
            _ => "object"
        };
    }

    private string GenerateParameters(ApiEndpoint endpoint, string resourceName)
    {
        var parameters = new List<string>();

        // Path parameters
        var pathParams = ExtractPathParameters(endpoint.Path);
        foreach (var param in pathParams)
        {
            parameters.Add($"{GetParameterType(param)} {param.ToCamelCase()}");
        }

        // Body parameter for POST/PUT/PATCH
        if (endpoint.Method.ToLowerInvariant() is "post" or "put" or "patch")
        {
            parameters.Add($"{resourceName.Singularize()} request");
        }

        return parameters.Any() ? string.Join(", ", parameters) + ", " : "";
    }

    private string GenerateRefitMethodName(ApiEndpoint endpoint)
    {
        var path = endpoint.Path.TrimStart('/');
        var pathParts = path.Split('/')
            .Where(p => !p.StartsWith("{") && !p.StartsWith(":") && !string.IsNullOrEmpty(p))
            .Select(p => p.ToPascalCase())
            .ToList();

        var methodPrefix = endpoint.Method.ToLowerInvariant() switch
        {
            "get" => pathParts.Count > 1 && endpoint.Path.Contains("{") ? "Get" : "GetAll",
            "post" => "Create",
            "put" => "Update", 
            "patch" => "UpdatePartial",
            "delete" => "Delete",
            _ => endpoint.Method.ToPascalCase()
        };

        var resourceName = pathParts.LastOrDefault() ?? "Resource";
        
        if (methodPrefix == "GetAll")
        {
            resourceName = resourceName.Pluralize();
        }

        return $"{methodPrefix}{resourceName}Async";
    }

    private string GenerateRefitMethodCall(ApiEndpoint endpoint)
    {
        var parameters = new List<string>();

        // Path parameters
        var pathParams = ExtractPathParameters(endpoint.Path);
        foreach (var param in pathParams)
        {
            parameters.Add(param.ToCamelCase());
        }

        // Body parameter
        if (endpoint.Method.ToLowerInvariant() is "post" or "put" or "patch")
        {
            parameters.Add("request");
        }

        return parameters.Any() ? string.Join(", ", parameters) + ", " : "";
    }

    private List<string> ExtractPathParameters(string path)
    {
        var parameters = new List<string>();
        var parts = path.Split('/');

        foreach (var part in parts)
        {
            if (part.StartsWith("{") && part.EndsWith("}"))
            {
                var paramName = part.Trim('{', '}');
                parameters.Add(paramName);
            }
        }

        return parameters;
    }

    private string GetParameterType(string parameterName)
    {
        return parameterName.ToLowerInvariant() switch
        {
            "id" => "int",
            "guid" => "Guid",
            "uuid" => "Guid",
            _ when parameterName.EndsWith("Id", StringComparison.OrdinalIgnoreCase) => "int",
            _ when parameterName.EndsWith("Guid", StringComparison.OrdinalIgnoreCase) => "Guid",
            _ => "string"
        };
    }

    private string ExtractResourceName(string path)
    {
        var parts = path.TrimStart('/').Split('/')
            .Where(p => !p.StartsWith("{") && !p.StartsWith(":") && !string.IsNullOrEmpty(p))
            .ToArray();

        return parts.LastOrDefault() ?? "";
    }

    private string GetPrimaryEntityType(ApiConfiguration config)
    {
        // Use the first schema as primary entity, or default to object
        return config.Schemas.FirstOrDefault()?.Name ?? "object";
    }

    private string GetPrimaryKeyType(ApiConfiguration config)
    {
        // Try to infer key type from endpoints with {id} parameters
        var hasIdParameter = config.Endpoints.Any(e => e.Path.Contains("{id}"));
        
        if (hasIdParameter)
        {
            return "int"; // Default to int for {id} parameters
        }

        // Look for guid parameters
        var hasGuidParameter = config.Endpoints.Any(e => e.Path.Contains("{guid}") || e.Path.Contains("{uuid}"));
        if (hasGuidParameter)
        {
            return "Guid";
        }

        return "string"; // Default fallback
    }
}