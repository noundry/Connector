# 🛠️ Noundry.Connector Tools

This directory contains developer tools for working with the Noundry.Connector library.

## 🎮 Connector Generator

**Noundry.Connector.Generator** is a CLI tool that automatically generates strongly-typed API clients for any REST API.

### Quick Start

```bash
# Install the tool globally
dotnet tool install -g Noundry.Connector.Generator

# Run the interactive wizard
connector-gen generate
```

### Features

- 🧙‍♂️ **Interactive Wizard** - Beautiful console interface powered by Spectre.Console
- 🔍 **API Discovery** - Automatically discovers endpoints from OpenAPI/Swagger specs
- 📊 **Schema Analysis** - Analyzes JSON responses to understand data structures  
- 🏗️ **Code Generation** - Generates models, Refit interfaces, and client classes
- 🔐 **Authentication Support** - API Key, Bearer Token, Basic Auth, OAuth 2.0
- ⚙️ **DI Integration** - Generates extension methods for dependency injection

### Generated Output

The tool generates a complete API client package:

```
Generated/
├── Models/           # Strongly-typed C# models with JsonPropertyName attributes
├── IMyApi.cs        # Refit interface with HTTP method attributes
├── MyApiClient.cs   # Client wrapper with convenience methods
└── MyApiExtensions.cs  # Dependency injection extension methods
```

### Documentation

For complete documentation and walkthrough, see the [main README](../README.md#-cli-tool-walkthrough).

### Development

To run the tool in development mode:

```bash
cd tools/Noundry.Connector.Generator
dotnet run -- generate
```

### Building the Tool

```bash
# Build the tool
dotnet build tools/Noundry.Connector.Generator

# Pack as a tool
dotnet pack tools/Noundry.Connector.Generator -c Release

# Install locally for testing
dotnet tool install -g Noundry.Connector.Generator --add-source ./tools/Noundry.Connector.Generator/nupkg
```