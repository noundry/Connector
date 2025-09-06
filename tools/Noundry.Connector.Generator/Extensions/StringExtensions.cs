using Humanizer;
using System.Text.RegularExpressions;

namespace Noundry.Connector.Generator.Extensions;

public static class StringExtensions
{
    public static string ToPascalCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Handle snake_case, kebab-case, and camelCase
        var words = Regex.Split(input, @"[_\-\s]+")
            .Where(w => !string.IsNullOrEmpty(w))
            .Select(w => char.ToUpperInvariant(w[0]) + w.Substring(1).ToLowerInvariant());

        var result = string.Join("", words);

        // Handle camelCase input
        if (result.Length > 1 && char.IsLower(result[0]))
        {
            result = char.ToUpperInvariant(result[0]) + result.Substring(1);
        }

        return result;
    }

    public static string ToCamelCase(this string input)
    {
        var pascalCase = input.ToPascalCase();
        if (string.IsNullOrEmpty(pascalCase))
            return pascalCase;

        return char.ToLowerInvariant(pascalCase[0]) + pascalCase.Substring(1);
    }

    public static string Singularize(this string input)
    {
        return Humanizer.InflectorExtensions.Singularize(input, inputIsKnownToBePlural: false);
    }

    public static string Pluralize(this string input)
    {
        return Humanizer.InflectorExtensions.Pluralize(input, inputIsKnownToBeSingular: false);
    }

    public static string ToValidCSharpIdentifier(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return "Item";

        // Replace invalid characters
        var result = Regex.Replace(input, @"[^\w]", "");
        
        // Ensure it starts with a letter or underscore
        if (result.Length > 0 && !char.IsLetter(result[0]) && result[0] != '_')
        {
            result = "_" + result;
        }

        // Handle C# keywords
        var keywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this",
            "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
            "using", "virtual", "void", "volatile", "while"
        };

        if (keywords.Contains(result.ToLowerInvariant()))
        {
            result = "@" + result;
        }

        return string.IsNullOrEmpty(result) ? "Item" : result;
    }

    public static string SplitCamelCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return Regex.Replace(input, @"(?<!^)(?=[A-Z])", " ");
    }

    public static string ToApiPath(this string input)
    {
        return "/" + input.ToLowerInvariant().Replace(" ", "-");
    }
}