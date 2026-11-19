using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace GeoChemistryNexus.Services.Epma;

/// <summary>Shared EPMA input conventions. This selects inputs; it never converts iron or infers an algorithm.</summary>
public static class EpmaInputPolicy
{
    public static string[] IronForms(EpmaScriptManifest manifest)
        => IronForms(manifest.Inputs.Select(input => input.Id));
    public static string[] IronForms(IEnumerable<string> inputIds)
    {
        var ids = inputIds.ToHashSet(StringComparer.Ordinal);
        var forms = new List<string>();
        if (ids.Contains("FeOt")) forms.Add("FeOt");
        if (ids.Contains("Fe2O3t")) forms.Add("Fe2O3t");
        if (ids.Contains("FeO") && ids.Contains("Fe2O3")) forms.Add("Separated");
        return forms.ToArray();
    }
    public static EpmaScriptSetting[] Parameters(EpmaScriptPackage package)
        => Parameters(package.Manifest, package.Settings, package.IronOptions is null);
    internal static EpmaScriptSetting[] Parameters(EpmaScriptManifest manifest, IEnumerable<EpmaScriptSetting> settings, bool implicitIronReporting)
    {
        var forms = IronForms(manifest);
        var parameters = settings.ToList();
        if (implicitIronReporting && forms.Length > 0 && !parameters.Any(parameter => parameter.Id == "ironForm"))
            parameters.Add(new EpmaScriptSetting
            {
                Id = "ironForm", Type = "choice", Label = JsonSerializer.SerializeToElement(new Dictionary<string, string> { ["zh-CN"] = "铁报告形式", ["en-US"] = "Iron reporting" }),
                Editable = forms.Length > 1, Value = JsonSerializer.SerializeToElement(forms[0]),
                Choices = forms.Select(form => new EpmaScriptChoice { Value = form, Label = JsonSerializer.SerializeToElement(form == "Separated" ? "FeO + Fe₂O₃" : form) }).ToArray()
            });
        return parameters.ToArray();
    }
    public static bool ParameterActive(EpmaScriptPackage package, EpmaScriptSetting parameter, IReadOnlyDictionary<string, JsonElement> settings)
        => ParameterActive(parameter, settings);
    internal static bool ParameterActive(EpmaScriptSetting parameter, IReadOnlyDictionary<string, JsonElement> settings)
    {
        if (parameter.Id is "ironMethod" or "fe3Ratio"
            && settings.TryGetValue("ironForm", out var form) && form.ValueKind == JsonValueKind.String && form.GetString() == "Separated") return false;
        return EpmaScriptText.Matches(parameter.When, settings);
    }
    public static bool ParameterVisible(EpmaScriptPackage package, EpmaScriptSetting parameter, IReadOnlyDictionary<string, JsonElement> settings) =>
        ParameterActive(package, parameter, settings);
    public static EpmaScriptInput[] ActiveInputs(EpmaScriptManifest manifest, IReadOnlyDictionary<string, JsonElement> settings)
    {
        var forms = IronForms(manifest);
        string? selected = settings.TryGetValue("ironForm", out var mode) && mode.ValueKind == JsonValueKind.String ? mode.GetString() : null;
        return manifest.Inputs.Where(input =>
        {
            string? form = input.Id switch
            {
                "FeOt" => "FeOt", "Fe2O3t" => "Fe2O3t",
                "FeO" or "Fe2O3" when forms.Contains("Separated") => "Separated", _ => null
            };
            return form is not null && selected is not null ? form == selected : EpmaScriptText.Matches(input.When, settings);
        }).ToArray();
    }
}
