using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GeoChemistryNexus.Services.Epma;

/// <summary>
/// Multilingual help documents of a calculation package. They live in the "help" member of package.json
/// as an object that maps a language code to an RTF document, for example {"zh-CN": "{\\rtf1...}"}.
/// </summary>
public static class EpmaHelpDocuments
{
    public const int MaximumLanguages = 32;
    public const int MaximumDocumentLength = 256 * 1024;

    /// <summary>Documents with wording, keyed by language. A missing or malformed member reads as empty.</summary>
    public static Dictionary<string, string> Read(JsonElement help)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (help.ValueKind != JsonValueKind.Object) return result;
        foreach (var property in help.EnumerateObject())
            if (property.Value.ValueKind == JsonValueKind.String && property.Value.GetString() is { Length: > 0 } text)
                result[property.Name] = text;
        return result;
    }

    public static void Validate(JsonElement help)
    {
        if (help.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return;
        if (help.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Package help must map languages to RTF documents.");
        int count = 0;
        foreach (var property in help.EnumerateObject())
        {
            if (++count > MaximumLanguages) throw new InvalidDataException("Too many help document languages.");
            if (property.Value.ValueKind != JsonValueKind.String) throw new InvalidDataException("Help document must be an RTF string: " + property.Name);
            string text = property.Value.GetString()!;
            if (text.Length > MaximumDocumentLength) throw new InvalidDataException("Help document is too large: " + property.Name);
            if (text.Length > 0 && !text.TrimStart().StartsWith("{\\rtf", StringComparison.Ordinal))
                throw new InvalidDataException("Help document must be RTF: " + property.Name);
        }
    }

    /// <summary>
    /// The document for the software language: exact language, same base language, English, then any document.
    /// Null when the package has no help document.
    /// </summary>
    public static string? Resolve(JsonElement help)
    {
        var documents = Read(help);
        if (documents.Count == 0) return null;
        var culture = CultureInfo.CurrentUICulture;
        foreach (string code in new[] { culture.Name, culture.TwoLetterISOLanguageName, "en-US", "en" })
            if (code.Length > 0 && documents.TryGetValue(code, out var text)) return text;
        string baseLanguage = culture.TwoLetterISOLanguageName;
        foreach (var pair in documents)
            if (pair.Key.Split('-')[0].Equals(baseLanguage, StringComparison.OrdinalIgnoreCase)) return pair.Value;
        return documents.Values.First();
    }

    public static bool Same(IReadOnlyDictionary<string, string> a, IReadOnlyDictionary<string, string> b)
        => a.Count == b.Count && a.All(pair => b.TryGetValue(pair.Key, out var text) && text == pair.Value);

    public static JsonNode ToNode(IReadOnlyDictionary<string, string> documents)
    {
        var node = new JsonObject();
        foreach (var pair in documents) node[pair.Key] = pair.Value;
        return node;
    }
}
