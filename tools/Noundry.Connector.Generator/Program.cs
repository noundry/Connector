using Noundry.Connector.Generator.Commands;
using Noundry.Connector.Generator.Services;
using Spectre.Console;
using System.CommandLine;

namespace Noundry.Connector.Generator;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Noundry Connector Generator - Generate strongly-typed API clients with ease");

        var apiUrlOption = new Option<string?>("--api-url", "Base URL of the API");
        var outputOption = new Option<string?>("--output", "Output directory for generated files");
        var interactiveOption = new Option<bool>("--interactive", () => true, "Run in interactive mode");
        
        var generateCommand = new Command("generate", "Generate API client code");
        generateCommand.Add(apiUrlOption);
        generateCommand.Add(outputOption);
        generateCommand.Add(interactiveOption);

        generateCommand.SetHandler(async (apiUrl, output, interactive) =>
        {
            try
            {
                DisplayWelcomeBanner();
                
                var wizard = new ApiGeneratorWizard();
                await wizard.RunAsync(apiUrl, output, interactive);
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
                return;
            }
        }, apiUrlOption, outputOption, interactiveOption);

        rootCommand.Add(generateCommand);

        return await rootCommand.InvokeAsync(args);
    }

    private static void DisplayWelcomeBanner()
    {
        var banner = new FigletText("Connector Generator")
            .Centered()
            .Color(Color.Cyan1);

        AnsiConsole.Write(banner);
        
        var panel = new Panel(new Markup(
            "[bold cyan]Welcome to Noundry Connector Generator![/]\n\n" +
            "This wizard will help you:\n" +
            "[green]•[/] Connect to any REST API\n" +
            "[green]•[/] Generate strongly-typed C# models\n" +
            "[green]•[/] Create Refit interfaces\n" +
            "[green]•[/] Build ready-to-use API clients\n\n" +
            "[dim]Powered by Noundry.Connector[/]"))
        {
            Header = new PanelHeader(" [bold yellow]🚀 API Client Code Generator[/] "),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }
}