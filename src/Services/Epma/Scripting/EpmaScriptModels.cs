using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using GeoChemistryNexus.Services.Epma.Input;

namespace GeoChemistryNexus.Services.Epma;

public sealed record EpmaScriptManifest
{
    public required int FormatVersion { get; init; }
    public required string PackageId { get; init; }
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
}
public sealed record EpmaScriptInput
{
    public required string Id { get; init; }
    public string Type { get; init; } = "number";
    public JsonElement Unit { get; init; } = JsonSerializer.SerializeToElement<object?>(null);
    public Dictionary<string, string[]>? When { get; init; }
}
public sealed record EpmaScriptChoice
{
    public required string Value { get; init; }
    public JsonElement Label { get; init; } = JsonSerializer.SerializeToElement<object?>(null);
    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplayLabel => EpmaScriptText.Label(Label, Value);
}
public sealed record EpmaScriptSetting
{
    public required string Id { get; init; }
    public string Type { get; init; } = "number";
    public JsonElement Label { get; init; } = JsonSerializer.SerializeToElement<object?>(null);
    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplayLabel => EpmaScriptText.Label(Label, Id);
    public bool Editable { get; init; }
    public JsonElement Value { get; init; } = JsonSerializer.SerializeToElement<object?>(null);
    public double? Min { get; init; }
    public double? Max { get; init; }
    public JsonElement Unit { get; init; } = JsonSerializer.SerializeToElement<object?>(null);
    public EpmaScriptChoice[] Choices { get; init; } = [];
    public Dictionary<string, string[]>? When { get; init; }
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
public sealed record EpmaScriptResponse
{
    public string Status { get; init; } = "Computed";
    public Dictionary<string, JsonElement> Values { get; init; } = new();
    public Dictionary<string, string> UndefinedReasons { get; init; } = new();
    public Dictionary<string, string> FieldSources { get; init; } = new();
    public EpmaScriptFlag[] Flags { get; init; } = [];
    public JsonElement Trace { get; init; } = JsonSerializer.SerializeToElement<object?>(null);
}
public sealed record EpmaScriptRow(int InputRow, IReadOnlyDictionary<string, EpmaScriptCell> Inputs, EpmaScriptResponse Result)
{
    public IEnumerable<EpmaScriptFlag> Flags => Result.Flags;
    public string HighestSeverity => Flags.Any(f => f.Severity == "Error") ? "Error" : Flags.Any(f => f.Severity == "Warning") ? "Warning" : Flags.Any() ? "Info" : "";
}
public sealed record EpmaScriptRun(EpmaScriptPackage Package, InputTableSnapshot Input,
    IReadOnlyDictionary<string, JsonElement> Parameters, NumericFormatChoice NumericFormat, DateTimeOffset CalculatedAt,
    IReadOnlyList<EpmaScriptRow> Rows)
{
    public IReadOnlyList<EpmaScriptOutput> Fields => Package.Manifest.Outputs;
    public string EngineVersion => "Jint " + typeof(Jint.Engine).Assembly.GetName().Version;
}
public sealed record EpmaScriptCell(string Kind, string Raw, double? Number = null, double? Limit = null);
public sealed record EpmaExampleDataset
{
    public Dictionary<string, JsonElement> Parameters { get; init; } = new();
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
            string language = CultureInfo.CurrentUICulture.Name;
            if (value.TryGetProperty(language, out var text) || value.TryGetProperty(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, out text)
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
    public static bool Matches(IReadOnlyDictionary<string, string[]>? when, IReadOnlyDictionary<string, JsonElement> parameters)
    {
        if (when is null) return true;
        var condition = when.Single();
        return parameters.TryGetValue(condition.Key, out var actual) && actual.ValueKind == JsonValueKind.String
            && condition.Value.Contains(actual.GetString(), StringComparer.Ordinal);
    }
}
