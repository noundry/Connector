using Noundry.Connector.Generator.Extensions;
using Noundry.Connector.Generator.Models;
using System.Text;

namespace Noundry.Connector.Generator.Templates;

public class InterfaceTemplate
{
    public string Generate(ApiConfiguration config)
    {
        var sb = new StringBuilder();

        // File header
        sb.AppendLine("using Refit;");
        
        if (config.GenerateModels && config.Schemas.Any())
        {
            sb.AppendLine($"using {config.Namespace}.Models;");
        }
        
        sb.AppendLine();

        // Namespace
        sb.AppendLine($"namespace {config.Namespace};");
        sb.AppendLine();

        // Interface documentation
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Refit interface for {config.ApiName} API");
        sb.AppendLine("/// </summary>");

        // Interface declaration
        sb.AppendLine($"public interface I{config.ApiName}Api");
        sb.AppendLine("{");

        // Generate methods for each endpoint
        foreach (var endpoint in config.Endpoints)
        {
            GenerateMethod(sb, endpoint, config, "    ");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private void GenerateMethod(StringBuilder sb, ApiEndpoint endpoint, ApiConfiguration config, string indent)
    {
        // Method documentation
        if (!string.IsNullOrEmpty(endpoint.Description))
        {
            sb.AppendLine($"{indent}/// <summary>");
            sb.AppendLine($"{indent}/// {endpoint.Description}");
            sb.AppendLine($"{indent}/// </summary>");
        }

        // Refit attribute
        var httpMethod = endpoint.Method.ToUpperInvariant();
        sb.AppendLine($"{indent}[{httpMethod}(\"{endpoint.Path}\")]");

        // Method signature
        var methodName = GenerateMethodName(endpoint);
        var returnType = GetReturnType(endpoint, config);
        var parameters = GenerateParameters(endpoint);

        sb.AppendLine($"{indent}Task<{returnType}> {methodName}({parameters}CancellationToken cancellationToken = default);");
        sb.AppendLine();
    }

    private string GenerateMethodName(ApiEndpoint endpoint)
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

    private string GetReturnType(ApiEndpoint endpoint, ApiConfiguration config)
    {
        // Try to find matching schema based on endpoint path
        var pathParts = endpoint.Path.TrimStart('/').Split('/')
            .Where(p => !p.StartsWith("{") && !p.StartsWith(":"))
            .ToList();

        if (pathParts.Any())
        {
            var resourceName = pathParts.Last().ToPascalCase();
            var matchingSchema = config.Schemas.FirstOrDefault(s => 
                s.Name.Equals(resourceName, StringComparison.OrdinalIgnoreCase) ||
                s.Name.Equals(resourceName.Singularize(), StringComparison.OrdinalIgnoreCase));

            if (matchingSchema != null)
            {
                // For collection endpoints, return List<T>
                if (endpoint.Method.ToLowerInvariant() == "get" && !endpoint.Path.Contains("{"))
                {
                    return $"List<{matchingSchema.Name}>";
                }

                // For single item endpoints
                if (endpoint.Method.ToLowerInvariant() == "get" && endpoint.Path.Contains("{"))
                {
                    return matchingSchema.Name;
                }

                // For create/update operations
                if (endpoint.Method.ToLowerInvariant() is "post" or "put" or "patch")
                {
                    return matchingSchema.Name;
                }
            }
        }

        // Default return types
        return endpoint.Method.ToLowerInvariant() switch
        {
            "get" when !endpoint.Path.Contains("{") => "List<object>",
            "get" => "object",
            "delete" => "object",
            _ => "object"
        };
    }

    private string GenerateParameters(ApiEndpoint endpoint)
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
            var bodyType = GetBodyParameterType(endpoint);
            parameters.Add($"[Body] {bodyType} request");
        }

        // Query parameters could be added here if discovered

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
        // Try to infer type from parameter name
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

    private string GetBodyParameterType(ApiEndpoint endpoint)
    {
        // Try to infer the body type from the endpoint path
        var pathParts = endpoint.Path.TrimStart('/').Split('/')
            .Where(p => !p.StartsWith("{") && !p.StartsWith(":"))
            .ToList();

        if (pathParts.Any())
        {
            return pathParts.Last().ToPascalCase().Singularize();
        }

        return "object";
    }
}