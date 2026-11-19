using System;
using System.Linq;
using System.Text.Json;

namespace GeoChemistryNexus.Services.Epma;

/// <summary>Authoring options live in getIronOptions() in the calculation script.</summary>
public sealed record EpmaIronMethod
{
    public required string Id { get; init; }
    public bool Enabled { get; init; } = true;
    public JsonElement Label { get; init; } = JsonSerializer.SerializeToElement<object?>(null);
    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplayLabel => EpmaScriptText.Label(Label, Id);
}
public sealed record EpmaIronOptions
{
    public bool Enabled { get; init; } = true;
    public bool Required { get; init; }
    public string[] Forms { get; init; } = ["FeOt"];
    public EpmaIronMethod[] Methods { get; init; } = [];
    public string DefaultForm { get; init; } = "FeOt";
    public string DefaultMethod { get; init; } = "ReportedEquivalentNoValence";
    public double Ratio { get; init; }
    public static readonly string[] ReportingForms = ["FeOt", "Fe2O3t", "Separated"];
    public static string[] Columns(string form) => form switch
    {
        "FeOt" => ["FeOt"], "Fe2O3t" => ["Fe2O3t"], "Separated" => ["FeO", "Fe2O3"],
        _ => throw new InvalidOperationException("Unknown iron reporting form: " + form)
    };
    public static EpmaIronOptions Starter => new()
    {
        Methods = [new() { Id = "ReportedEquivalentNoValence", Label = Text("按报告值使用，不拆分价态", "Use reported iron without valence splitting") },
            new() { Id = "AllFe2", Label = Text("假定全铁二价", "Assume all Fe²⁺"), Enabled = false },
            new() { Id = "UserRatio", Label = Text("用户指定 Fe³⁺/ΣFe", "User specified Fe³⁺/ΣFe"), Enabled = false }]
    };
    private static JsonElement Text(string zh, string en) => JsonSerializer.SerializeToElement(new System.Collections.Generic.Dictionary<string, string> { ["zh-CN"] = zh, ["en-US"] = en });
    public void Validate()
    {
        if (Forms is null || Methods is null || Forms.Length > 3 || Methods.Length > 32
            || Forms.Distinct(StringComparer.Ordinal).Count() != Forms.Length
            || Forms.Any(form => !ReportingForms.Contains(form))
            || Methods.Any(method => method is null || string.IsNullOrWhiteSpace(method.Id))
            || Methods.Select(method => method.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Methods.Length
            || !double.IsFinite(Ratio) || Ratio < 0 || Ratio > 1)
            throw new InvalidOperationException(EpmaLocalizer.Format("epma_iron_invalid"));
        if (Required && !Enabled) throw new InvalidOperationException(EpmaLocalizer.Format("epma_iron_required"));
        if (!Enabled) return;
        if (Forms.Length == 0 || !Forms.Contains(DefaultForm)
            || Forms.Any(form => form != "Separated") && !Methods.Any(method => method.Enabled && method.Id == DefaultMethod))
            throw new InvalidOperationException(EpmaLocalizer.Format("epma_iron_default_required"));
    }
}
