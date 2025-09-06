using Noundry.Connector.Generator.Extensions;
using Noundry.Connector.Generator.Models;
using Noundry.Connector.Generator.Templates;
using System.Text;

namespace Noundry.Connector.Generator.Services;

public class CodeGenerationService
{
    public async Task<GenerationResult> GenerateAsync(ApiConfiguration config)
    {
        try
        {
            var generatedFiles = new List<GeneratedFile>();

            // Generate model classes
            if (config.GenerateModels && config.Schemas.Any())
            {
                var modelFiles = GenerateModels(config);
                generatedFiles.AddRange(modelFiles);
            }

            // Generate Refit interface
            if (config.GenerateInterface && config.Endpoints.Any())
            {
                var interfaceFile = GenerateRefitInterface(config);
                generatedFiles.Add(interfaceFile);
            }

            // Generate client wrapper class
            if (config.GenerateClient)
            {
                var clientFile = GenerateClientClass(config);
                generatedFiles.Add(clientFile);
            }

            // Generate DI extension methods
            if (config.GenerateExtensions)
            {
                var extensionsFile = GenerateExtensionMethods(config);
                generatedFiles.Add(extensionsFile);
            }

            return new GenerationResult
            {
                IsSuccess = true,
                GeneratedFiles = generatedFiles
            };
        }
        catch (Exception ex)
        {
            return new GenerationResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task WriteFilesAsync(List<GeneratedFile> files, string outputPath)
    {
        Directory.CreateDirectory(outputPath);

        foreach (var file in files)
        {
            var fullPath = Path.Combine(outputPath, file.RelativePath, file.FileName);
            var directory = Path.GetDirectoryName(fullPath);
            
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(fullPath, file.Content, Encoding.UTF8);
        }
    }

    private List<GeneratedFile> GenerateModels(ApiConfiguration config)
    {
        var files = new List<GeneratedFile>();
        var template = new ModelTemplate();

        foreach (var schema in config.Schemas)
        {
            var content = template.Generate(schema, config);
            
            files.Add(new GeneratedFile
            {
                FileName = $"{schema.Name}.cs",
                Content = content,
                RelativePath = "Models",
                FileType = GeneratedFileType.Model
            });

            // Generate nested models
            foreach (var property in schema.Properties.Values)
            {
                if (property.NestedModel != null && !files.Any(f => f.FileName == $"{property.NestedModel.Name}.cs"))
                {
                    var nestedContent = template.Generate(property.NestedModel, config);
                    
                    files.Add(new GeneratedFile
                    {
                        FileName = $"{property.NestedModel.Name}.cs",
                        Content = nestedContent,
                        RelativePath = "Models",
                        FileType = GeneratedFileType.Model
                    });
                }
            }
        }

        return files;
    }

    private GeneratedFile GenerateRefitInterface(ApiConfiguration config)
    {
        var template = new InterfaceTemplate();
        var content = template.Generate(config);

        return new GeneratedFile
        {
            FileName = $"I{config.ApiName}Api.cs",
            Content = content,
            RelativePath = "",
            FileType = GeneratedFileType.Interface
        };
    }

    private GeneratedFile GenerateClientClass(ApiConfiguration config)
    {
        var template = new ClientTemplate();
        var content = template.Generate(config);

        return new GeneratedFile
        {
            FileName = $"{config.ApiName}Client.cs",
            Content = content,
            RelativePath = "",
            FileType = GeneratedFileType.Client
        };
    }

    private GeneratedFile GenerateExtensionMethods(ApiConfiguration config)
    {
        var template = new ExtensionsTemplate();
        var content = template.Generate(config);

        return new GeneratedFile
        {
            FileName = $"{config.ApiName}Extensions.cs",
            Content = content,
            RelativePath = "",
            FileType = GeneratedFileType.Extensions
        };
    }
}