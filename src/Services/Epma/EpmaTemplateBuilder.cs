using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Services.Epma;

public sealed class EpmaInputDraft
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "number";
    public string Unit { get; set; } = "";
    public string Example { get; set; } = "0";
}

public sealed class EpmaOutputDraft
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string Type { get; set; } = "number";
    public string Unit { get; set; } = "";
    public string Expression { get; set; } = "";
}

/// <summary>Creates self-contained starter packages (current EPMA module version) and expression fixtures.</summary>
public static class EpmaTemplateBuilder
{
    public static EpmaScriptPackage CreateStarter(string id)
    {
        string version = ContentVersionHelper.WithAppEpmaFormat(0);
        var seed = Build(id, "New calculation", version, "", "", "", "",
            [new() { Id = EpmaScriptPackage.SampleInputId, Type = "text", Example = "Example" },
                new() { Id = "SiO2", Unit = "wt%", Example = "40" }, new() { Id = "MgO", Unit = "wt%", Example = "50" }],
            [new() { Id = "Total", Unit = "wt%", Expression = "0" }]);
        var document = new EpmaPackageEditorDocument(seed);
        document.Files.Single(f => f.IsScript).Text = """
            function calculate(context) {
                let total=0, supplied=false;
                for(const [id,cell] of Object.entries(context.inputs)) {
                    if(cell.kind==='Text'||cell.kind==='Missing')continue;
                    if(cell.kind!=='Number')return {status:'Skipped',flags:[{code:'FORMULA_INPUT_REQUIRED',severity:'Error',target:id,
                        message:{'zh-CN':'请输入有效数值。','en-US':'A valid numeric input is required.'}}]};
                    total+=cell.number; supplied=true;
                }
                if(!supplied)return {status:'Skipped',flags:[{code:'FORMULA_INPUT_REQUIRED',severity:'Info',message:'No numeric composition supplied.'}]};
                return {values:{Total:total}};
            }

            // Optional: the lines of the inspector for the selected row, same shape as the geothermometers' calculateDetailed.
            // Each line: name / nameLang, value, desc / descLang, isResult, isHighlight, isSeparator (group heading), collapsed, backgroundColor.
            function calculateDetailed(context) {
                const steps = [{isSeparator:true, name:'Inputs', nameLang:{'zh-CN':'输入','en-US':'Inputs'}}];
                for(const [id,cell] of Object.entries(context.inputs))
                    if(cell.kind==='Number') steps.push({name:id, value:cell.number});
                steps.push({isSeparator:true, name:'Result', nameLang:{'zh-CN':'结果','en-US':'Result'}});
                steps.push({name:'Total', value:context.values.Total, isResult:true});
                return steps;
            }
            """;
        return document.Build("New calculation", version, "", "", "", document.DefinitionJson);
    }
    private static JsonElement Json<T>(T value) => JsonSerializer.SerializeToElement(value);
    public static double Number(string value, string field)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) || !double.IsFinite(number))
            throw new InvalidDataException(EpmaLocalizer.Format("epma_editor_invalid_number", field));
        return number;
    }

    public static EpmaScriptPackage Build(string id, string name, string version, string author, string description,
        string tags, string references, IEnumerable<EpmaInputDraft> inputRows,
        IEnumerable<EpmaOutputDraft> outputRows)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(version))
            throw new InvalidDataException(EpmaLocalizer.Format("epma_editor_required"));
        var inputs = inputRows.ToArray();
        var outputs = outputRows.ToArray();
        if (inputs.Length == 0 || outputs.Length == 0 || outputs.Any(o => string.IsNullOrWhiteSpace(o.Expression)))
            throw new InvalidDataException(EpmaLocalizer.Format("epma_editor_fields_required"));
        var manifest = new EpmaScriptManifest
        {
            PackageId = id.Trim(), Name = Json(name.Trim()),
            Version = version.Trim(), Author = author.Trim(), Description = Json(description.Trim()),
            UpdatedAt = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Tags = tags.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(Json).ToArray(),
            References = Json(references.Trim()), Scripts = ["scripts/calculation.js"],
            Inputs = inputs.Select(i => new EpmaScriptInput { Id = i.Id.Trim(), Type = i.Type, Unit = Json(i.Unit.Trim()) }).ToArray(),
            Outputs = outputs.Select(o => new EpmaScriptOutput { Id = o.Id.Trim(), Label = Json(o.Label.Trim()), Type = o.Type, Unit = Json(o.Unit.Trim()) }).ToArray()
        };
        var example = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        var script = new StringBuilder("function calculate(context) {\n'use strict';\nconst input = Object.create(null);\nconst result = Object.create(null);\n");
        foreach (var input in inputs)
        {
            string key = JsonSerializer.Serialize(input.Id.Trim());
            if (!example.TryAdd(input.Id.Trim(), input.Type == "text" ? Json(input.Example) : Json(Number(input.Example, input.Id))))
                throw new InvalidDataException(EpmaLocalizer.Format("epma_editor_duplicate", input.Id));
            // The sample name only labels the row; it is never required and never part of the calculation.
            if (input.Id.Trim() == EpmaScriptPackage.SampleInputId) continue;
            string kind = input.Type == "text" ? "Text" : "Number";
            script.AppendLine($"if (context.inputs[{key}].kind !== '{kind}') return {{status:'Skipped',flags:[{{code:'FORMULA_INPUT_REQUIRED',severity:'Error',target:{key},message:'Required input is missing, invalid or below detection limit.'}}]}};");
            script.AppendLine($"input[{key}] = context.inputs[{key}].{(input.Type == "text" ? "raw" : "number")};");
        }
        foreach (var output in outputs)
        {
            string key = JsonSerializer.Serialize(output.Id.Trim());
            script.AppendLine($"result[{key}] = (\n{output.Expression}\n);");
            // Fail the trial for invalid values rather than silently allowing null, NaN or Infinity.
            script.AppendLine(output.Type == "text"
                ? $"if (typeof result[{key}] !== 'string') throw new Error('Expected text: ' + {key});"
                : $"if (typeof result[{key}] !== 'number' || !Number.isFinite(result[{key}])) throw new Error('Expected a finite number: ' + {key});");
        }
        script.AppendLine("return {values: result};\n}");
        using var memory = new MemoryStream();
        using (var zip = new ZipArchive(memory, ZipArchiveMode.Create, true))
        {
            void Write(string path, string content)
            {
                using var writer = new StreamWriter(zip.CreateEntry(path).Open(), new UTF8Encoding(false));
                writer.Write(content);
            }
            Write("package.json", JsonSerializer.Serialize(manifest, EpmaScriptPackage.JsonOptions));
            Write("scripts/calculation.js", script.ToString());
            Write("examples.json", JsonSerializer.Serialize(new EpmaExampleDataset { Inputs = [example] }, EpmaScriptPackage.JsonOptions));
        }
        return EpmaScriptPackage.Read(memory.ToArray());
    }
}
