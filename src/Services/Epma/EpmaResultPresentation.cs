using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace GeoChemistryNexus.Services.Epma;

/// <summary>Workbench formatting; scientific values remain unchanged.</summary>
public static class EpmaResultPresentation
{
    private static readonly JsonSerializerOptions PrettyJson = new() { WriteIndented = true };

    public static string Number(double value, int decimals) => value.ToString("F" + Math.Clamp(decimals, 0, 10), CultureInfo.CurrentCulture);

    public static string Process(JsonElement trace) => trace.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
        ? "" : JsonSerializer.Serialize(trace, PrettyJson);

    public static string Provenance(IEnumerable<(string Name, string Value)> entries) =>
        string.Join("\n", entries.Select(p => p.Name + ": " + p.Value));

}
