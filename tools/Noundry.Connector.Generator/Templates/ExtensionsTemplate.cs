using Noundry.Connector.Generator.Extensions;
using Noundry.Connector.Generator.Models;
using System.Text;

namespace Noundry.Connector.Generator.Templates;

public class ExtensionsTemplate
{
    public string Generate(ApiConfiguration config)
    {
        var sb = new StringBuilder();

        // File header
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using Noundry.Connector.Extensions;");
        sb.AppendLine("using Noundry.Connector.Authentication;");
        
        if (config.GenerateModels && config.Schemas.Any())
        {
            sb.AppendLine($"using {config.Namespace}.Models;");
        }
        
        sb.AppendLine();

        // Namespace
        sb.AppendLine($"namespace {config.Namespace}.Extensions;");
        sb.AppendLine();

        // Class documentation
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Dependency injection extensions for {config.ApiName} API client");
        sb.AppendLine("/// </summary>");

        // Static class declaration
        sb.AppendLine($"public static class {config.ApiName}ServiceExtensions");
        sb.AppendLine("{");

        // Generate extension methods
        GenerateBasicExtensionMethod(sb, config, "    ");
        GenerateTokenAuthExtensionMethod(sb, config, "    ");
        GenerateOAuthExtensionMethod(sb, config, "    ");
        GenerateAdvancedExtensionMethod(sb, config, "    ");

        sb.AppendLine("}");

        return sb.ToString();
    }

    private void GenerateBasicExtensionMethod(StringBuilder sb, ApiConfiguration config, string indent)
    {
        var primaryEntityType = GetPrimaryEntityType(config);
        var primaryKeyType = GetPrimaryKeyType(config);

        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// Add {config.ApiName} API client to the service collection");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine($"{indent}/// <param name=\"services\">The service collection</param>");
        sb.AppendLine($"{indent}/// <param name=\"baseUrl\">Base URL of the API</param>");
        sb.AppendLine($"{indent}/// <returns>The service collection for chaining</returns>");
        
        sb.AppendLine($"{indent}public static IServiceCollection Add{config.ApiName}Client(");
        sb.AppendLine($"{indent}    this IServiceCollection services,");
        sb.AppendLine($"{indent}    string baseUrl)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    services.AddConnector<I{config.ApiName}Api, {primaryEntityType}, {primaryKeyType}>(options =>");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        options.BaseUrl = baseUrl;");
        sb.AppendLine($"{indent}        options.DefaultHeaders[\"User-Agent\"] = \"{config.ApiName}Client/1.0.0\";");
        sb.AppendLine($"{indent}        options.DefaultHeaders[\"Accept\"] = \"application/json\";");
        sb.AppendLine($"{indent}    }});");
        sb.AppendLine();
        sb.AppendLine($"{indent}    services.AddTransient<{config.ApiName}Client>();");
        sb.AppendLine();
        sb.AppendLine($"{indent}    return services;");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    private void GenerateTokenAuthExtensionMethod(StringBuilder sb, ApiConfiguration config, string indent)
    {
        var primaryEntityType = GetPrimaryEntityType(config);
        var primaryKeyType = GetPrimaryKeyType(config);

        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// Add {config.ApiName} API client with token authentication");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine($"{indent}/// <param name=\"services\">The service collection</param>");
        sb.AppendLine($"{indent}/// <param name=\"baseUrl\">Base URL of the API</param>");
        sb.AppendLine($"{indent}/// <param name=\"apiToken\">API token for authentication</param>");
        sb.AppendLine($"{indent}/// <returns>The service collection for chaining</returns>");
        
        sb.AppendLine($"{indent}public static IServiceCollection Add{config.ApiName}Client(");
        sb.AppendLine($"{indent}    this IServiceCollection services,");
        sb.AppendLine($"{indent}    string baseUrl,");
        sb.AppendLine($"{indent}    string apiToken)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    services.AddConnector<I{config.ApiName}Api, {primaryEntityType}, {primaryKeyType}>(options =>");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        options.BaseUrl = baseUrl;");
        sb.AppendLine($"{indent}        options.DefaultHeaders[\"User-Agent\"] = \"{config.ApiName}Client/1.0.0\";");
        sb.AppendLine($"{indent}        options.DefaultHeaders[\"Accept\"] = \"application/json\";");
        sb.AppendLine($"{indent}    }}, new TokenAuthenticationProvider(apiToken));");
        sb.AppendLine();
        sb.AppendLine($"{indent}    services.AddTransient<{config.ApiName}Client>();");
        sb.AppendLine();
        sb.AppendLine($"{indent}    return services;");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    private void GenerateOAuthExtensionMethod(StringBuilder sb, ApiConfiguration config, string indent)
    {
        var primaryEntityType = GetPrimaryEntityType(config);
        var primaryKeyType = GetPrimaryKeyType(config);

        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// Add {config.ApiName} API client with OAuth 2.0 authentication");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine($"{indent}/// <param name=\"services\">The service collection</param>");
        sb.AppendLine($"{indent}/// <param name=\"baseUrl\">Base URL of the API</param>");
        sb.AppendLine($"{indent}/// <param name=\"clientId\">OAuth client ID</param>");
        sb.AppendLine($"{indent}/// <param name=\"clientSecret\">OAuth client secret</param>");
        sb.AppendLine($"{indent}/// <param name=\"tokenEndpoint\">OAuth token endpoint</param>");
        sb.AppendLine($"{indent}/// <param name=\"scope\">OAuth scope (optional)</param>");
        sb.AppendLine($"{indent}/// <returns>The service collection for chaining</returns>");
        
        sb.AppendLine($"{indent}public static IServiceCollection Add{config.ApiName}ClientWithOAuth(");
        sb.AppendLine($"{indent}    this IServiceCollection services,");
        sb.AppendLine($"{indent}    string baseUrl,");
        sb.AppendLine($"{indent}    string clientId,");
        sb.AppendLine($"{indent}    string clientSecret,");
        sb.AppendLine($"{indent}    string tokenEndpoint,");
        sb.AppendLine($"{indent}    string? scope = null)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    services.AddConnector<I{config.ApiName}Api, {primaryEntityType}, {primaryKeyType}>(options =>");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        options.BaseUrl = baseUrl;");
        sb.AppendLine($"{indent}        options.DefaultHeaders[\"User-Agent\"] = \"{config.ApiName}Client/1.0.0\";");
        sb.AppendLine($"{indent}        options.DefaultHeaders[\"Accept\"] = \"application/json\";");
        sb.AppendLine($"{indent}    }});");
        sb.AppendLine();
        sb.AppendLine($"{indent}    services.AddOAuthAuthentication(oauth =>");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        oauth.ClientId = clientId;");
        sb.AppendLine($"{indent}        oauth.ClientSecret = clientSecret;");
        sb.AppendLine($"{indent}        oauth.TokenEndpoint = tokenEndpoint;");
        sb.AppendLine($"{indent}        oauth.Scope = scope;");
        sb.AppendLine($"{indent}    }});");
        sb.AppendLine();
        sb.AppendLine($"{indent}    services.AddTransient<{config.ApiName}Client>();");
        sb.AppendLine();
        sb.AppendLine($"{indent}    return services;");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    private void GenerateAdvancedExtensionMethod(StringBuilder sb, ApiConfiguration config, string indent)
    {
        var primaryEntityType = GetPrimaryEntityType(config);
        var primaryKeyType = GetPrimaryKeyType(config);

        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// Add {config.ApiName} API client with custom configuration");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine($"{indent}/// <param name=\"services\">The service collection</param>");
        sb.AppendLine($"{indent}/// <param name=\"configureOptions\">Action to configure client options</param>");
        sb.AppendLine($"{indent}/// <param name=\"authenticationProvider\">Custom authentication provider</param>");
        sb.AppendLine($"{indent}/// <returns>The service collection for chaining</returns>");
        
        sb.AppendLine($"{indent}public static IServiceCollection Add{config.ApiName}Client(");
        sb.AppendLine($"{indent}    this IServiceCollection services,");
        sb.AppendLine($"{indent}    Action<ConnectorOptions> configureOptions,");
        sb.AppendLine($"{indent}    IAuthenticationProvider? authenticationProvider = null)");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    if (authenticationProvider != null)");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        services.AddConnector<I{config.ApiName}Api, {primaryEntityType}, {primaryKeyType}>(configureOptions, authenticationProvider);");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}    else");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        services.AddConnector<I{config.ApiName}Api>(configureOptions);");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine();
        sb.AppendLine($"{indent}    services.AddTransient<{config.ApiName}Client>();");
        sb.AppendLine();
        sb.AppendLine($"{indent}    return services;");
        sb.AppendLine($"{indent}}}");
    }

    private string GetPrimaryEntityType(ApiConfiguration config)
    {
        return config.Schemas.FirstOrDefault()?.Name ?? "object";
    }

    private string GetPrimaryKeyType(ApiConfiguration config)
    {
        var hasIdParameter = config.Endpoints.Any(e => e.Path.Contains("{id}"));
        if (hasIdParameter) return "int";

        var hasGuidParameter = config.Endpoints.Any(e => e.Path.Contains("{guid}") || e.Path.Contains("{uuid}"));
        if (hasGuidParameter) return "Guid";

        return "string";
    }
}