using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services.Epma.Input;
using GeoChemistryNexus.Services.Epma;

namespace GeoChemistryNexus.ViewModels;

public sealed record EpmaTemplateDetails(string Id, string Name, string RawVersion, string DetailDescription,
    IReadOnlyList<string> DeclaredTags, string? DeclaredAuthor, string? UpdatedAt, string Icon = "Σ")
{
    public string Version => "v" + RawVersion;
    public IReadOnlyList<string> Tags => DeclaredTags;
    public bool HasTags => Tags.Count > 0;
    public string Author => DeclaredAuthor ?? "—";
    public string UpdateDate => UpdatedAt ?? "—";
    public static EpmaTemplateDetails FromManifest(EpmaScriptManifest manifest) => new(manifest.PackageId,
        EpmaScriptText.Format(manifest.Name), manifest.Version, EpmaScriptText.Format(manifest.Description),
        manifest.Tags.Select(EpmaScriptText.Format).ToArray(), manifest.Author, manifest.UpdatedAt, manifest.Icon);
    /// <summary>Catalog records carry the same display fields as the manifest, so cards and details never open package bytes.</summary>
    public static EpmaTemplateDetails FromEntity(EpmaTemplateEntity entity) => new(entity.PackageId,
        EpmaTemplateDisplay.Name(entity), entity.Version, EpmaTemplateDisplay.Description(entity), EpmaTemplateDisplay.Tags(entity),
        string.IsNullOrWhiteSpace(entity.Author) ? null : entity.Author,
        string.IsNullOrWhiteSpace(entity.UpdatedAt) ? null : entity.UpdatedAt, entity.Icon);
}

public sealed record EpmaChoice<T>(T Value, string Key) : INotifyPropertyChanged
{
    public string Label => EpmaLocalizer.Format(Key);
    public event PropertyChangedEventHandler? PropertyChanged;
    public void RefreshLabel() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Label)));
    public override string ToString() => Label;
}
public sealed record EpmaDiagnosticInfo(int InputRow, string Sample, string Level, string Code, string Target, string Actual, string Expectation, string Source, string Message, bool Stale)
{
    public string LevelLabel => Level switch
    {
        "Error" => EpmaLocalizer.Format("epma_errors"),
        "Warning" => EpmaLocalizer.Format("epma_warnings"),
        "Info" => EpmaLocalizer.Format("epma_information"),
        _ => Level,
    };
}
public static class EpmaDiagnosticSummary
{
    public static string Format(int rows, IEnumerable<EpmaDiagnosticInfo> diagnostics, int stale)
    {
        var items=diagnostics.ToArray();
        return EpmaLocalizer.Format("epma_qc_dataset_summary", rows, items.Length, items.Select(d=>d.InputRow).Distinct().Count(),
            items.Count(d=>d.Level=="Error"),items.Count(d=>d.Level=="Warning"),items.Count(d=>d.Level=="Info"),stale);
    }
}
