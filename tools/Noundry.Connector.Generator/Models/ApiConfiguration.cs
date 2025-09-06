namespace Noundry.Connector.Generator.Models;

public class ApiConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;

    // Authentication
    public AuthenticationType AuthType { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiKeyHeader { get; set; }
    public string? BearerToken { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? TokenEndpoint { get; set; }
    public string? Scope { get; set; }

    // Discovery
    public List<ApiEndpoint> Endpoints { get; set; } = new();
    public List<SchemaModel> Schemas { get; set; } = new();

    // Generation Options
    public bool GenerateModels { get; set; } = true;
    public bool GenerateInterface { get; set; } = true;
    public bool GenerateClient { get; set; } = true;
    public bool GenerateExtensions { get; set; } = true;
    public bool UseRecords { get; set; } = true;
    public bool UseNullableReferenceTypes { get; set; } = true;
}

public enum AuthenticationType
{
    None,
    ApiKey,
    BearerToken,
    BasicAuth,
    OAuth2
}

public class ApiEndpoint
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();
    public string? ResponseSchema { get; set; }
    public int? StatusCode { get; set; }
}

public class SchemaModel
{
    public string Name { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public Dictionary<string, SchemaProperty> Properties { get; set; } = new();
    public bool IsCollection { get; set; }
    public string? Description { get; set; }
    public List<string> RequiredProperties { get; set; } = new();
}

public class SchemaProperty
{
    public string Name { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string CSharpType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsCollection { get; set; }
    public string? Description { get; set; }
    public object? DefaultValue { get; set; }
    public string? Format { get; set; }
    public SchemaModel? NestedModel { get; set; }
}

public class TestResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ResponseContent { get; set; }
    public int StatusCode { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}

public class GenerationResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public List<GeneratedFile> GeneratedFiles { get; set; } = new();
}

public class GeneratedFile
{
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public GeneratedFileType FileType { get; set; }
}

public enum GeneratedFileType
{
    Model,
    Interface,
    Client,
    Extensions,
    Configuration
}