using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GeoChemistryNexus.Services.Epma;

/// <summary>
/// Name, description and tags of a package in every language. Languages[0] is the default language and
/// is written first, so a reopened package recovers it as the first key of the name dictionary.
/// Values that were not changed are kept exactly as they were in the original manifest.
/// </summary>
public sealed class EpmaBasicLocalization
{
    public List<string> Languages { get; init; } = [];
    /// <summary>Language that plain-string manifest values belonged to when the package was read.</summary>
    public string SourceLanguage { get; init; } = "";
    public Dictionary<string, string> Name { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> Description { get; init; } = new(StringComparer.Ordinal);
    /// <summary>One entry per tag; each maps language to that tag's wording.</summary>
    public List<Dictionary<string, string>> Tags { get; init; } = [];
    public string DefaultLanguage => Languages[0];

    public static EpmaBasicLocalization FromManifest(EpmaScriptManifest manifest, string fallbackLanguage)
    {
        string defaultLanguage = FirstLanguage(manifest.Name) ?? fallbackLanguage;
        var result = new EpmaBasicLocalization
        {
            Languages = [defaultLanguage], SourceLanguage = fallbackLanguage,
            Name = Read(manifest.Name, fallbackLanguage), Description = Read(manifest.Description, fallbackLanguage),
            Tags = manifest.Tags.Select(tag => Read(tag, fallbackLanguage)).ToList(),
        };
        foreach (string language in result.Name.Keys.Concat(result.Description.Keys).Concat(result.Tags.SelectMany(tag => tag.Keys)))
            if (!result.Languages.Contains(language)) result.Languages.Add(language);
        return result;
    }

    /// <summary>A plain string belongs to <paramref name="fallbackLanguage"/>; empty wording is treated as absent.</summary>
    public static Dictionary<string, string> Read(JsonElement value, string fallbackLanguage)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (value.ValueKind == JsonValueKind.String)
        {
            string text = value.GetString()!.Trim();
            if (text.Length > 0) result[fallbackLanguage] = text;
        }
        else if (value.ValueKind == JsonValueKind.Object)
            foreach (var property in value.EnumerateObject())
                if (property.Value.ValueKind == JsonValueKind.String && property.Value.GetString()!.Trim() is { Length: > 0 } text)
                    result[property.Name] = text;
        return result;
    }

    private static string? FirstLanguage(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object) return null;
        foreach (var property in value.EnumerateObject())
            if (property.Value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(property.Value.GetString()))
                return property.Name;
        return null;
    }

    /// <summary>Keeps only listed languages with wording, in language order (default first).</summary>
    public Dictionary<string, string> Compact(IReadOnlyDictionary<string, string> values)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string language in Languages)
            if (values.TryGetValue(language, out var text) && !string.IsNullOrWhiteSpace(text)) result[language] = text.Trim();
        return result;
    }

    private static bool Same(IReadOnlyDictionary<string, string> a, IReadOnlyDictionary<string, string> b, bool sameFirstKey)
    {
        if (a.Count != b.Count || a.Any(pair => !b.TryGetValue(pair.Key, out var text) || text != pair.Value)) return false;
        return !sameFirstKey || a.Count == 0 || a.Keys.First() == b.Keys.First();
    }

    private static JsonNode ToNode(IReadOnlyDictionary<string, string> values)
    {
        var node = new JsonObject();
        foreach (var pair in values) node[pair.Key] = pair.Value;
        return node;
    }

    /// <summary>Writes name, description and tags into <paramref name="manifest"/> when they differ from <paramref name="original"/>.</summary>
    public void Apply(JsonObject manifest, EpmaScriptManifest original)
    {
        Replace(manifest, "name", original.Name, Name, true);
        Replace(manifest, "description", original.Description, Description, false);
        var before = original.Tags.Select(tag => Read(tag, SourceLanguage)).Where(tag => tag.Count > 0).ToList();
        var after = Tags.Select(Compact).Where(tag => tag.Count > 0).ToList();
        if (before.Count == after.Count && before.Zip(after, (a, b) => Same(a, b, false)).All(same => same)) return;
        manifest["tags"] = new JsonArray(after.Select(tag => (JsonNode?)ToNode(tag)).ToArray());
    }

    private void Replace(JsonObject manifest, string key, JsonElement original, IReadOnlyDictionary<string, string> values, bool definesDefault)
    {
        var after = Compact(values);
        // The name's first key is how the default language is recovered, so its order matters.
        if (Same(Read(original, SourceLanguage), after, definesDefault)) return;
        manifest[key] = after.Count == 0 ? JsonValue.Create("") : ToNode(after);
    }
}
