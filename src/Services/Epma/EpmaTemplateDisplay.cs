using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace GeoChemistryNexus.Services.Epma;

/// <summary>Resolves the localized text stored as raw manifest JSON on a catalog record (works without reading package bytes).</summary>
public static class EpmaTemplateDisplay
{
    public static string Name(EpmaTemplateEntity entity) => EpmaScriptText.Label(Parse(entity.NameJson), entity.PackageId);

    public static string Description(EpmaTemplateEntity entity) => EpmaScriptText.Format(Parse(entity.DescriptionJson));

    public static IReadOnlyList<string> Tags(EpmaTemplateEntity entity)
    {
        var tags = Parse(entity.TagsJson);
        return tags.ValueKind != JsonValueKind.Array
            ? []
            : tags.EnumerateArray().Select(EpmaScriptText.Format).Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
    }

    private static JsonElement Parse(string? json)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "null" : json);
            return document.RootElement.Clone();
        }
        catch (JsonException) { return default; }
    }
}
