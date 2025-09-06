using Noundry.Connector.Generator.Extensions;
using Noundry.Connector.Generator.Models;
using Noundry.Connector.Generator.Services;
using Spectre.Console;

namespace Noundry.Connector.Generator.Commands;

public class ApiGeneratorWizard
{
    private readonly ApiDiscoveryService _discoveryService;
    private readonly SchemaAnalysisService _schemaService;
    private readonly CodeGenerationService _codeGenService;

    public ApiGeneratorWizard()
    {
        _discoveryService = new ApiDiscoveryService();
        _schemaService = new SchemaAnalysisService();
        _codeGenService = new CodeGenerationService();
    }

    public async Task RunAsync(string? apiUrl, string? outputPath, bool interactive)
    {
        var config = new ApiConfiguration();

        if (interactive)
        {
            await RunInteractiveWizardAsync(config);
        }
        else
        {
            config.BaseUrl = apiUrl ?? throw new ArgumentException("API URL is required in non-interactive mode");
            config.OutputPath = outputPath ?? Directory.GetCurrentDirectory();
        }

        await GenerateClientCodeAsync(config);
    }

    private async Task RunInteractiveWizardAsync(ApiConfiguration config)
    {
        // Step 1: API Configuration
        await ConfigureApiAsync(config);

        // Step 2: Authentication Setup
        await ConfigureAuthenticationAsync(config);

        // Step 3: Endpoint Discovery
        await DiscoverEndpointsAsync(config);

        // Step 4: Schema Analysis
        await AnalyzeSchemaAsync(config);

        // Step 5: Generation Options
        await ConfigureGenerationOptionsAsync(config);
    }

    private async Task ConfigureApiAsync(ApiConfiguration config)
    {
        AnsiConsole.Write(new Rule("[bold blue]🌐 API Configuration[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();

        config.BaseUrl = AnsiConsole.Ask<string>("[green]Enter the API base URL:[/]", "https://api.example.com");

        config.ApiName = AnsiConsole.Ask("[green]Enter a name for your API client:[/]",
            ExtractApiNameFromUrl(config.BaseUrl));

        config.Namespace = AnsiConsole.Ask("[green]Enter the namespace for generated classes:[/]",
            $"MyApp.ApiClients.{config.ApiName}");

        // Test API connectivity
        AnsiConsole.WriteLine();
        var testResult = await AnsiConsole.Status()
            .StartAsync("Testing API connectivity...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("green"));

                return await _discoveryService.TestConnectivityAsync(config.BaseUrl);
            });

        if (testResult.IsSuccess)
        {
            AnsiConsole.MarkupLine("[green]✓[/] API is accessible");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗[/] API test failed: {testResult.ErrorMessage}");
            
            if (!AnsiConsole.Confirm("Continue anyway?"))
            {
                throw new OperationCanceledException("API connectivity test failed");
            }
        }
    }

    private async Task ConfigureAuthenticationAsync(ApiConfiguration config)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold blue]🔐 Authentication Setup[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();

        var authType = AnsiConsole.Prompt(
            new SelectionPrompt<AuthenticationType>()
                .Title("[green]Select authentication method:[/]")
                .AddChoices(
                    AuthenticationType.None,
                    AuthenticationType.ApiKey,
                    AuthenticationType.BearerToken,
                    AuthenticationType.BasicAuth,
                    AuthenticationType.OAuth2)
                .UseConverter(auth => auth switch
                {
                    AuthenticationType.None => "No Authentication",
                    AuthenticationType.ApiKey => "API Key",
                    AuthenticationType.BearerToken => "Bearer Token",
                    AuthenticationType.BasicAuth => "Basic Authentication (Username/Password)",
                    AuthenticationType.OAuth2 => "OAuth 2.0 (Client ID/Secret)",
                    _ => auth.ToString()
                }));

        config.AuthType = authType;

        switch (authType)
        {
            case AuthenticationType.ApiKey:
                config.ApiKey = AnsiConsole.Prompt(
                    new TextPrompt<string>("[green]Enter API key:[/]")
                        .Secret());
                
                config.ApiKeyHeader = AnsiConsole.Ask("[green]API key header name:[/]", "X-API-Key");
                break;

            case AuthenticationType.BearerToken:
                config.BearerToken = AnsiConsole.Prompt(
                    new TextPrompt<string>("[green]Enter bearer token:[/]")
                        .Secret());
                break;

            case AuthenticationType.BasicAuth:
                config.Username = AnsiConsole.Ask<string>("[green]Username:[/]");
                config.Password = AnsiConsole.Prompt(
                    new TextPrompt<string>("[green]Password:[/]")
                        .Secret());
                break;

            case AuthenticationType.OAuth2:
                config.ClientId = AnsiConsole.Ask<string>("[green]Client ID:[/]");
                config.ClientSecret = AnsiConsole.Prompt(
                    new TextPrompt<string>("[green]Client Secret:[/]")
                        .Secret());
                config.TokenEndpoint = AnsiConsole.Ask<string>("[green]Token Endpoint:[/]");
                config.Scope = AnsiConsole.Ask("[green]Scope (optional):[/]", string.Empty);
                break;
        }

        // Test authentication if credentials provided
        if (authType != AuthenticationType.None)
        {
            AnsiConsole.WriteLine();
            var authResult = await AnsiConsole.Status()
                .StartAsync("Testing authentication...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    return await _discoveryService.TestAuthenticationAsync(config);
                });

            if (authResult.IsSuccess)
            {
                AnsiConsole.MarkupLine("[green]✓[/] Authentication successful");
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]⚠[/] Authentication test inconclusive: {authResult.ErrorMessage}");
            }
        }
    }

    private async Task DiscoverEndpointsAsync(ApiConfiguration config)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold blue]🔍 Endpoint Discovery[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();

        var discoverEndpoints = AnsiConsole.Confirm("Automatically discover API endpoints?", true);

        if (discoverEndpoints)
        {
            var endpoints = await AnsiConsole.Status()
                .StartAsync("Discovering API endpoints...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots2);
                    return await _discoveryService.DiscoverEndpointsAsync(config);
                });

            if (endpoints.Any())
            {
                AnsiConsole.MarkupLine($"[green]✓[/] Found {endpoints.Count} endpoints");
                
                // Display discovered endpoints
                var table = new Table()
                    .AddColumn("Method")
                    .AddColumn("Path")
                    .AddColumn("Description");

                foreach (var endpoint in endpoints.Take(10))
                {
                    table.AddRow(
                        $"[{GetMethodColor(endpoint.Method)}]{endpoint.Method}[/]",
                        endpoint.Path,
                        endpoint.Description ?? "");
                }

                if (endpoints.Count > 10)
                {
                    table.AddRow("...", "...", $"({endpoints.Count - 10} more endpoints)");
                }

                AnsiConsole.Write(table);

                config.Endpoints = endpoints;
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]⚠[/] No endpoints discovered automatically");
            }
        }

        // Allow manual endpoint specification
        var addManualEndpoints = AnsiConsole.Confirm("Add manual endpoints for testing?", !config.Endpoints.Any());

        if (addManualEndpoints)
        {
            config.Endpoints.AddRange(await CollectManualEndpointsAsync());
        }
    }

    private async Task AnalyzeSchemaAsync(ApiConfiguration config)
    {
        if (!config.Endpoints.Any())
        {
            AnsiConsole.MarkupLine("[yellow]⚠[/] No endpoints available for schema analysis");
            return;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold blue]📊 Schema Analysis[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();

        var schemas = await AnsiConsole.Status()
            .StartAsync("Analyzing API responses...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Bounce);
                return await _schemaService.AnalyzeEndpointsAsync(config);
            });

        if (schemas.Any())
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Analyzed {schemas.Count} response schemas");

            // Display discovered models
            var table = new Table()
                .AddColumn("Model Name")
                .AddColumn("Properties")
                .AddColumn("Type");

            foreach (var schema in schemas.Take(8))
            {
                table.AddRow(
                    $"[bold]{schema.Name}[/]",
                    schema.Properties.Count.ToString(),
                    schema.IsCollection ? "Collection" : "Object");
            }

            if (schemas.Count > 8)
            {
                table.AddRow("...", "...", $"({schemas.Count - 8} more models)");
            }

            AnsiConsole.Write(table);
            config.Schemas = schemas;
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]⚠[/] No schemas could be analyzed");
        }
    }

    private async Task ConfigureGenerationOptionsAsync(ApiConfiguration config)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold blue]⚙️ Generation Options[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();

        config.OutputPath = AnsiConsole.Ask("[green]Output directory:[/]", 
            Path.Combine(Directory.GetCurrentDirectory(), "Generated"));

        config.GenerateModels = AnsiConsole.Confirm("Generate model classes?", true);
        config.GenerateInterface = AnsiConsole.Confirm("Generate Refit interface?", true);
        config.GenerateClient = AnsiConsole.Confirm("Generate client wrapper class?", true);
        config.GenerateExtensions = AnsiConsole.Confirm("Generate DI extension methods?", true);

        if (config.GenerateModels)
        {
            config.UseRecords = AnsiConsole.Confirm("Use C# records for models?", true);
            config.UseNullableReferenceTypes = AnsiConsole.Confirm("Use nullable reference types?", true);
        }

        AnsiConsole.WriteLine();
        DisplayGenerationSummary(config);
    }

    private async Task GenerateClientCodeAsync(ApiConfiguration config)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold blue]🔧 Code Generation[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();

        await AnsiConsole.Status()
            .StartAsync("Generating code...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Star);
                ctx.SpinnerStyle(Style.Parse("green"));

                var result = await _codeGenService.GenerateAsync(config);
                
                if (result.IsSuccess)
                {
                    ctx.Status("Writing files...");
                    await _codeGenService.WriteFilesAsync(result.GeneratedFiles, config.OutputPath);
                }
                else
                {
                    throw new InvalidOperationException($"Code generation failed: {result.ErrorMessage}");
                }
            });

        DisplaySuccessMessage(config);
    }

    // Helper methods
    private string ExtractApiNameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var host = uri.Host.Replace("api.", "").Replace("www.", "");
            var parts = host.Split('.');
            return parts[0].ToPascalCase();
        }
        catch
        {
            return "MyApi";
        }
    }

    private string GetMethodColor(string method) => method.ToUpper() switch
    {
        "GET" => "green",
        "POST" => "blue",
        "PUT" => "yellow",
        "PATCH" => "orange3",
        "DELETE" => "red",
        _ => "white"
    };

    private async Task<List<ApiEndpoint>> CollectManualEndpointsAsync()
    {
        var endpoints = new List<ApiEndpoint>();

        while (true)
        {
            var method = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("HTTP Method:")
                    .AddChoices("GET", "POST", "PUT", "PATCH", "DELETE"));

            var path = AnsiConsole.Ask<string>("Endpoint path (e.g., /users):");

            endpoints.Add(new ApiEndpoint
            {
                Method = method,
                Path = path,
                Description = $"{method} {path}"
            });

            if (!AnsiConsole.Confirm("Add another endpoint?"))
                break;
        }

        return endpoints;
    }

    private void DisplayGenerationSummary(ApiConfiguration config)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn("Setting")
            .AddColumn("Value");

        table.AddRow("API Name", config.ApiName);
        table.AddRow("Namespace", config.Namespace);
        table.AddRow("Output Path", config.OutputPath);
        table.AddRow("Generate Models", config.GenerateModels ? "[green]Yes[/]" : "[red]No[/]");
        table.AddRow("Generate Interface", config.GenerateInterface ? "[green]Yes[/]" : "[red]No[/]");
        table.AddRow("Generate Client", config.GenerateClient ? "[green]Yes[/]" : "[red]No[/]");
        table.AddRow("Use Records", config.UseRecords ? "[green]Yes[/]" : "[red]No[/]");

        AnsiConsole.Write(new Panel(table)
        {
            Header = new PanelHeader(" [bold]Generation Summary[/] "),
            Border = BoxBorder.Rounded
        });

        if (!AnsiConsole.Confirm("\n[green]Proceed with code generation?[/]"))
        {
            throw new OperationCanceledException("Code generation cancelled by user");
        }
    }

    private void DisplaySuccessMessage(ApiConfiguration config)
    {
        AnsiConsole.WriteLine();

        var successPanel = new Panel(
            new Markup($"[green]✓ Code generation completed successfully![/]\n\n" +
                      $"Generated files in: [bold]{config.OutputPath}[/]\n\n" +
                      $"[dim]Next steps:[/]\n" +
                      $"[green]1.[/] Add the generated files to your project\n" +
                      $"[green]2.[/] Install Noundry.Connector NuGet package\n" +
                      $"[green]3.[/] Configure DI in your startup code\n" +
                      $"[green]4.[/] Start using your strongly-typed API client!"))
        {
            Header = new PanelHeader(" [bold green]🎉 Success![/] "),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green)
        };

        AnsiConsole.Write(successPanel);
    }
}