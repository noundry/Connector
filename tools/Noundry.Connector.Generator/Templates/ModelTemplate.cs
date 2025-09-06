using Noundry.Connector.Generator.Extensions;
using Noundry.Connector.Generator.Models;
using System.Text;

namespace Noundry.Connector.Generator.Templates;

public class ModelTemplate
{
    public string Generate(SchemaModel schema, ApiConfiguration config)
    {
        var sb = new StringBuilder();

        // File header
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();

        // Namespace
        sb.AppendLine($"namespace {config.Namespace}.Models;");
        sb.AppendLine();

        // XML documentation
        if (!string.IsNullOrEmpty(schema.Description))
        {
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// {schema.Description}");
            sb.AppendLine("/// </summary>");
        }

        // Class or record declaration
        var typeKeyword = config.UseRecords ? "record" : "class";
        var nullableContext = config.UseNullableReferenceTypes ? "?" : "";
        
        sb.AppendLine($"public {typeKeyword} {schema.Name}");
        sb.AppendLine("{");

        // Properties
        foreach (var property in schema.Properties.Values)
        {
            GenerateProperty(sb, property, config, "    ");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private void GenerateProperty(StringBuilder sb, SchemaProperty property, ApiConfiguration config, string indent)
    {
        // XML documentation
        if (!string.IsNullOrEmpty(property.Description))
        {
            sb.AppendLine($"{indent}/// <summary>");
            sb.AppendLine($"{indent}/// {property.Description}");
            sb.AppendLine($"{indent}/// </summary>");
        }

        // JsonPropertyName attribute
        if (property.Name != property.OriginalName)
        {
            sb.AppendLine($"{indent}[JsonPropertyName(\"{property.OriginalName}\")]");
        }

        // Property declaration
        var propertyType = GetPropertyType(property, config);
        var nullableSuffix = GetNullableSuffix(property, config);

        if (config.UseRecords)
        {
            sb.AppendLine($"{indent}public {propertyType}{nullableSuffix} {property.Name} {{ get; init; }}");
        }
        else
        {
            sb.AppendLine($"{indent}public {propertyType}{nullableSuffix} {property.Name} {{ get; set; }}");
        }

        sb.AppendLine();
    }

    private string GetPropertyType(SchemaProperty property, ApiConfiguration config)
    {
        if (property.NestedModel != null)
        {
            return property.IsCollection 
                ? $"List<{property.NestedModel.Name}>" 
                : property.NestedModel.Name;
        }

        return property.CSharpType;
    }

    private string GetNullableSuffix(SchemaProperty property, ApiConfiguration config)
    {
        if (!config.UseNullableReferenceTypes)
            return "";

        // Value types that can be nullable
        var nullableValueTypes = new[] { "int", "long", "decimal", "double", "float", "bool", "DateTime", "Guid" };
        
        if (property.IsNullable)
        {
            if (nullableValueTypes.Contains(property.CSharpType))
                return "?";
            else
                return "?"; // Reference types
        }

        // Non-nullable reference types get the ? suffix in nullable context
        if (!nullableValueTypes.Contains(property.CSharpType) && property.CSharpType != "string")
            return "";

        return "";
    }
}