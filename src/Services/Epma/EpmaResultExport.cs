using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GeoChemistryNexus.Services.Epma.Export;
using GeoChemistryNexus.Services.Epma.Input;

namespace GeoChemistryNexus.Services.Epma;

/// <summary>Shared full-precision export. Static run data is saved once.</summary>
public static class EpmaResultExport
{
    private static ExportCell Cell(JsonElement value) => value.ValueKind switch {
        JsonValueKind.Number => new ExportCell.Number(value.GetDouble()), JsonValueKind.String => new ExportCell.Text(value.GetString()!), _ => new ExportCell.Empty() };
    public static ExportTable Full(EpmaScriptRun run, IReadOnlySet<int> stale)
    {
        var headers = run.Input.Headers.Select(h => "Input:" + h).Concat(new[] { "Input_Row", "Row_Status", "QC:Severity", "QC:Flags", "QC:Stale" })
            .Concat(run.Fields.Select(f => "Result:" + f.Id)).ToArray();
        var rows = run.Rows.Where(r => r.Result.Status != "Empty").Select(row => {
            var cells = Enumerable.Range(0, run.Input.Headers.Count).Select(c => (ExportCell)new ExportCell.Text(ValueParser.RawText(run.Input.Rows[row.InputRow - 1][c]))).ToList();
            cells.AddRange(new ExportCell[] { new ExportCell.Number(row.InputRow), new ExportCell.Text(row.Result.Status), new ExportCell.Text(row.HighestSeverity),
                new ExportCell.Text(JsonSerializer.Serialize(row.Flags)), new ExportCell.Text(stale.Contains(row.InputRow) ? "true" : "false") });
            cells.AddRange(run.Fields.Select(f => row.Result.Values.TryGetValue(f.Id, out var v) ? Cell(v) : new ExportCell.Empty()));
            return (IReadOnlyList<ExportCell>)cells.ToArray();
        }).ToArray();
        return new(headers, rows);
    }
    public static JsonElement Metadata(EpmaScriptRun run) => JsonSerializer.SerializeToElement(new {
        format = "GeoChemistryNexus.Epma.Result", version = 1,
        method = new { id = run.Package.Manifest.PackageId, version = run.Package.Manifest.Version, sha256 = run.Package.Sha256 },
        engine = run.EngineVersion, softwareVersion = typeof(EpmaResultExport).Assembly.GetName().Version?.ToString(),
        calculatedAt = run.CalculatedAt, numericFormat = run.NumericFormat.ToString(),
    });
    public static ExportTable MetadataTable(EpmaScriptRun run)
    {
        var rows = new List<IReadOnlyList<ExportCell>>();
        // Chunk large JSON properties to fit Excel's 32767-character cell limit, preserving exact concatenation.
        foreach (var property in Metadata(run).EnumerateObject())
        {
            string value = property.Value.GetRawText();
            foreach (var (part, text) in Parts(value))
                rows.Add(new ExportCell[] { new ExportCell.Text(property.Name), new ExportCell.Number(part), new ExportCell.Text(text) });
        }
        return new(new[] { "Key", "Part", "JSON" }, rows);
    }
    private static readonly string[] RecordHeaders = { "QC:Flags" };
    public static (ExportTable Results, ExportTable Records) WorkbookTables(ExportTable full)
    {
        var results = Enumerable.Range(0, full.Headers.Count).Where(i => !RecordHeaders.Contains(full.Headers[i])).ToArray();
        int rowIndex = full.Headers.ToList().IndexOf("Input_Row");
        var records = new List<IReadOnlyList<ExportCell>>();
        foreach (var row in full.Rows)
            foreach (var header in RecordHeaders)
                foreach (var (part, text) in Parts(EpmaCsvWriter.CellText(row[full.Headers.ToList().IndexOf(header)])))
                    records.Add(new ExportCell[] { row[rowIndex], new ExportCell.Text(header), new ExportCell.Number(part), new ExportCell.Text(text) });
        return (new(results.Select(i => full.Headers[i]).ToArray(), full.Rows.Select(row => (IReadOnlyList<ExportCell>)results.Select(i => row[i]).ToArray()).ToArray()),
            new(new[] { "Input_Row", "Key", "Part", "JSON" }, records));
    }
    private static IEnumerable<(int Part, string Text)> Parts(string value)
    {
        for (int start = 0, part = 1; start < value.Length; part++)
        {
            int length = Math.Min(30000, value.Length - start);
            if (start + length < value.Length && char.IsHighSurrogate(value[start + length - 1]) && char.IsLowSurrogate(value[start + length])) length--;
            yield return (part, value.Substring(start, length));
            start += length;
        }
    }
}
