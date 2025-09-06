# Noundry.Connector.Generator

A powerful CLI tool for generating strongly-typed API clients using the Noundry.Connector library. This wizard-style tool guides you through the process of connecting to any REST API and automatically generating C# models, Refit interfaces, and client wrapper classes.

## Features

🎯 **Interactive Wizard** - Beautiful Spectre.Console interface guides you through setup  
🔐 **Multiple Auth Methods** - API Key, Bearer Token, Basic Auth, and OAuth 2.0 support  
🔍 **API Discovery** - Automatic endpoint discovery from OpenAPI/Swagger or manual configuration  
📊 **Schema Analysis** - JSON response analysis to generate strongly-typed models  
🏗️ **Code Generation** - Creates models, Refit interfaces, client classes, and DI extensions  
⚡ **LINQ Support** - Generated clients support LINQ queries on API responses  

## Installation

Install as a global .NET tool:

```bash
dotnet tool install -g Noundry.Connector.Generator
```

Or install locally in your project:

```bash
dotnet tool install Noundry.Connector.Generator
```

## Usage

### Interactive Mode (Recommended)

```bash
connector-gen generate
```

This launches the interactive wizard that will guide you through:

1. **API Configuration** - Enter base URL and test connectivity
2. **Authentication Setup** - Configure API keys, tokens, or OAuth
3. **Endpoint Discovery** - Auto-discover or manually specify endpoints  
4. **Schema Analysis** - Analyze API responses to understand data structures
5. **Generation Options** - Choose what to generate and customize settings
6. **Code Generation** - Generate and save your API client files

### Command Line Mode

```bash
connector-gen generate --api-url https://api.example.com --output ./Generated --interactive false
```

## Generated Files

The tool generates a complete API client package:

```
Generated/
├── Models/
│   ├── User.cs           # Strongly-typed data models
│   ├── Post.cs
│   └── Comment.cs
├── IMyApiApi.cs          # Refit interface definition
├── MyApiClient.cs        # Client wrapper class
└── MyApiExtensions.cs    # DI extension methods
```

## Example Usage

After generating your API client:

```csharp
// In Program.cs or Startup.cs
services.AddMyApiClient("https://api.example.com", "your-api-token");

// In your service or controller
public class UserService
{
    private readonly MyApiClient _apiClient;
    
    public UserService(MyApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    
    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        var users = await _apiClient.GetAllUsersAsync();
        
        // Use LINQ to query results
        return users.Where(u => u.IsActive && u.LastLogin > DateTime.Now.AddDays(-30));
    }
}
```

## Authentication Methods

### API Key
```bash
# The wizard will prompt for:
# - API Key value
# - Header name (default: X-API-Key)
```

### Bearer Token
```bash
# The wizard will prompt for:
# - Bearer token value
```

### Basic Authentication
```bash
# The wizard will prompt for:
# - Username
# - Password
```

### OAuth 2.0
```bash
# The wizard will prompt for:
# - Client ID
# - Client Secret  
# - Token endpoint
# - Scope (optional)
```

## Advanced Features

### Custom Endpoints
If auto-discovery doesn't find all endpoints, you can manually specify:
- HTTP method (GET, POST, PUT, PATCH, DELETE)
- Endpoint path (e.g., `/users/{id}`)
- Description

### Generation Options
- **Models**: Generate C# classes or records
- **Nullable Types**: Enable nullable reference types
- **Refit Interface**: Generate strongly-typed API interface
- **Client Wrapper**: Generate convenience client class
- **DI Extensions**: Generate dependency injection setup methods

### LINQ Support
Generated clients automatically support LINQ operations:

```csharp
// Filter, sort, and transform results
var recentPosts = await client.GetAllPostsAsync()
    .Where(p => p.CreatedAt > DateTime.Now.AddDays(-7))
    .OrderByDescending(p => p.ViewCount)
    .Select(p => new { p.Title, p.Author, p.CreatedAt })
    .ToListAsync();
```

## Integration with Noundry.Connector

The generated code is designed to work seamlessly with Noundry.Connector:

1. Install Noundry.Connector in your project
2. Add generated files to your project  
3. Configure DI using the generated extension methods
4. Inject and use the generated client classes

## Requirements

- .NET 8.0 or later
- Internet connection for API discovery and testing
- Valid API credentials for authenticated endpoints

## Troubleshooting

### API Discovery Issues
- Ensure the API base URL is accessible
- Check firewall and network connectivity
- Verify API credentials are correct

### Generation Issues
- Make sure output directory has write permissions
- Ensure API returns valid JSON responses
- Try manual endpoint specification if auto-discovery fails

### Build Issues
- Add the Noundry.Connector NuGet package to your project
- Ensure generated namespaces don't conflict with existing code
- Check that all required using statements are included

## Contributing

This tool is part of the Noundry.Connector project. Contributions welcome!

## License

MIT License - see the main project for details.