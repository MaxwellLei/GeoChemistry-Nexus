using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using GeoChemistryNexus.Services.Epma.Input;

namespace GeoChemistryNexus.Services.Epma;

public sealed record EpmaScriptManifest
{
    public required string PackageId { get; init; }
    /// <summary>"x.y.z": x.y is the EPMA module version the package was written for (the app refuses a higher one), z is the package revision.</summary>
    public required string Version { get; init; }
    public required JsonElement Name { get; init; }
    public JsonElement Description { get; init; } = JsonSerializer.SerializeToElement<object?>(null);
    public JsonElement[] Tags { get; init; } = [];
    /// <summary>Optional multilingual help: language code to RTF document (see <see cref="EpmaHelpDocuments"/>).</summary>
    public JsonElement Help { get; init; } = JsonSerializer.SerializeToElement<object?>(null);
    public string? Author { get; init; }
    public string Icon { get; init; } = "Σ";
    public string? UpdatedAt { get; init; }
    public required string[] Scripts { get; init; }
    public required EpmaScriptInput[] Inputs { get; init; }
    public required EpmaScriptOutput[] Outputs { get; init; }
    public JsonElement References { get; init; } = JsonSerializer.SerializeToElement<object?>(null);
    /// <summary>Optional capability names the package needs (see <see cref="EpmaScriptPackage.SupportedCapabilities"/>). Plain optional fields never belong here.</summary>
    public string[] Requires { get; init; } = [];
}
public sealed record EpmaScriptInput
{
    public required string Id { get; init; }
    public string Type { get; init; } = "number";
    public JsonElement Unit { get; init; } = JsonSerializer.SerializeToElement<object?>(null);
    /// <summary>Optional heading over a run of input columns (text or a language dictionary); neighbouring columns with the same heading share one band.</summary>
    public JsonElement Group { get; init; } = JsonSerializer.SerializeToElement<object?>(null);
    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplayGroup => EpmaScriptText.Display(Group);
}
public sealed record EpmaScriptOutput
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplayLabel => EpmaScriptText.Label(Label, Id);
    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplayDescription => EpmaScriptText.Format(Description);
    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplayUnit => EpmaScriptText.Format(Unit);
    public required string Id { get; init; }
    public string Type { get; init; } = "number";
    public JsonElement Label { get; init; } = JsonSerializer.SerializeToElement<object?>(null);
    public JsonElement Description { get; init; } = JsonSerializer.SerializeToElement<object?>(null);
    public JsonElement Unit { get; init; } = JsonSerializer.SerializeToElement<object?>(null);
    /// <summary>Optional heading over a run of result columns (text or a language dictionary); neighbouring columns with the same heading share one band.</summary>
    public JsonElement Group { get; init; } = JsonSerializer.SerializeToElement<object?>(null);
    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplayGroup => EpmaScriptText.Display(Group);
}
public sealed record EpmaScriptFlag
{
    public required string Code { get; init; }
    public string Severity { get; init; } = "Info";
    public string Target { get; init; } = "Row";
    public JsonElement Actual { get; init; } = JsonSerializer.SerializeToElement<object?>(null);
    public JsonElement Expected { get; init; } = JsonSerializer.SerializeToElement<object?>(null);
    public string? Source { get; init; }
    public JsonElement Message { get; init; } = JsonSerializer.SerializeToElement<object?>(null);
    public JsonElement[] Args { get; init; } = [];
    [System.Text.Json.Serialization.JsonIgnore]
    public string ActualText => EpmaScriptText.Scalar(Actual);
    [System.Text.Json.Serialization.JsonIgnore]
    public string ExpectedText => Expected.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? "" : Expected.GetRawText();
    [System.Text.Json.Serialization.JsonIgnore]
    public string Text => EpmaScriptText.Format(Message) + (Args.Length == 0 ? "" : " " + string.Join(", ", Args.Select(a =>
        a.ValueKind == JsonValueKind.Object ? EpmaScriptText.Format(a) : EpmaScriptText.Scalar(a))));
    public static EpmaScriptFlag Error(string code, string message) => new() { Code = code, Severity = "Error", Message = JsonSerializer.SerializeToElement(message) };
}
/// <summary>
/// What <c>calculate</c> returns for one row: the status, the result values (a <c>null</c> value is an undefined result) and the QC flags.
/// The inspector's detail lines are not part of it; they come from the optional <c>calculateDetailed</c> entry, asked for the selected row only.
/// </summary>
public sealed record EpmaScriptResponse
{
    public string Status { get; init; } = "Computed";
    public Dictionary<string, JsonElement> Values { get; init; } = new();
    public EpmaScriptFlag[] Flags { get; init; } = [];
}
public sealed record EpmaScriptRow(int InputRow, IReadOnlyDictionary<string, EpmaScriptCell> Inputs, EpmaScriptResponse Result)
{
    public IEnumerable<EpmaScriptFlag> Flags => Result.Flags;
    public string HighestSeverity => Flags.Any(f => f.Severity == "Error") ? "Error" : Flags.Any(f => f.Severity == "Warning") ? "Warning" : Flags.Any() ? "Info" : "";
}
/// <param name="Batch">The once-per-run <c>prepare</c> result; <c>calculateDetailed</c> receives it again as <c>context.batch</c>.</param>
public sealed record EpmaScriptRun(EpmaScriptPackage Package, InputTableSnapshot Input,
    NumericFormatChoice NumericFormat, DateTimeOffset CalculatedAt, IReadOnlyList<EpmaScriptRow> Rows, JsonElement? Batch = null)
{
    /// <summary>The result columns of this run: the package's declared outputs.</summary>
    public IReadOnlyList<EpmaScriptOutput> Fields => Package.Manifest.Outputs;
    public string EngineVersion => "Jint " + typeof(Jint.Engine).Assembly.GetName().Version;
}
public sealed record EpmaScriptCell(string Kind, string Raw, double? Number = null, double? Limit = null);
public sealed record EpmaExampleDataset
{
    public required Dictionary<string, JsonElement>[] Inputs { get; init; }
}
public static class EpmaScriptText
{
    public static string Label(JsonElement value, string fallback)
    {
        string text = Format(value);
        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }
    public static string Format(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            // 以软件语言为准而不是线程文化：线程文化可能尚未同步（启动阶段/后台线程），会导致卡片停留在默认语言
            string language = GeoChemistryNexus.Services.LanguageService.CurrentLanguage;
            string baseLanguage = language.Split('-')[0];
            if (value.TryGetProperty(language, out var text) || value.TryGetProperty(baseLanguage, out text)
                || value.TryGetProperty("en-US", out text) || value.TryGetProperty("en", out text)) return Format(text);
            return value.EnumerateObject().Select(p => Format(p.Value)).FirstOrDefault() ?? "";
        }
        return value.ValueKind == JsonValueKind.String ? value.GetString()! : "";
    }
    public static string Scalar(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Undefined or JsonValueKind.Null => "", JsonValueKind.String => value.GetString()!,
        JsonValueKind.Number => value.GetDouble().ToString("R", CultureInfo.InvariantCulture), _ => value.GetRawText(),
    };
    /// <summary>Text, number or language dictionary shown as a single line; anything else is empty.</summary>
    public static string Display(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => Format(value), JsonValueKind.String => value.GetString()!,
        JsonValueKind.Number => Scalar(value), _ => "",
    };
    /// <summary>A column group is absent, a non-empty title, or a language dictionary of non-empty titles.</summary>
    public static bool ValidGroup(JsonElement group) => group.ValueKind switch
    {
        JsonValueKind.Undefined or JsonValueKind.Null => true,
        JsonValueKind.String => group.GetString() is { Length: > 0 and <= 80 },
        JsonValueKind.Object => group.EnumerateObject().Any() && group.EnumerateObject()
            .All(p => p.Value.ValueKind == JsonValueKind.String && p.Value.GetString() is { Length: > 0 and <= 80 }),
        _ => false,
    };
}
