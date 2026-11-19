using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GeoChemistryNexus.Services.Epma.Export;
using GeoChemistryNexus.Helpers;
using unvell.ReoGrid;
using unvell.ReoGrid.DataFormat;
using unvell.ReoGrid.IO;

namespace GeoChemistryNexus.Services.Epma;

/// <summary>Writes full-precision results to CSV or XLSX; input is entered in the fixed worksheet.</summary>
public sealed class EpmaFileService
{
    public async Task WriteRunAsync(string path, EpmaScriptRun run, IReadOnlySet<int> stale, CancellationToken token = default)
    {
        var table = EpmaResultExport.Full(run, stale);
        string staged = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        var files = new List<(string Staged, string Target)> { (staged, path) };
        try
        {
            if (string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase))
            {
                int prefixed = CsvFormulaPrefixDetector.Count(table);
                if (prefixed > 0) MessageHelper.Warning(EpmaLocalizer.Format("epma_csv_formula_warning", prefixed));
                await EpmaCsvWriter.WriteAsync(staged, table, token);
                files.Add((staged + ".json", Path.ChangeExtension(path, ".run.json")));
                await File.WriteAllTextAsync(staged + ".json", EpmaResultExport.Metadata(run).GetRawText(), token);
            }
            else if (string.Equals(Path.GetExtension(path), ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                token.ThrowIfCancellationRequested();
                var workbook = ReoGridControl.CreateMemoryWorkbook();
                var tables = EpmaResultExport.WorkbookTables(table);
                Fill(workbook.Worksheets[0], tables.Results, token);
                workbook.Worksheets[0].Name = "Results";
                var metadata = workbook.CreateWorksheet("Run");
                workbook.Worksheets.Add(metadata);
                Fill(metadata, EpmaResultExport.MetadataTable(run), token);
                var records = workbook.CreateWorksheet("Records");
                workbook.Worksheets.Add(records);
                Fill(records, tables.Records, token);
                workbook.Save(staged, FileFormat.Excel2007);
            }
            else
                throw new IOException(EpmaLocalizer.Format("epma_file_unsupported"));
            files.Add((staged + ".zip", Path.ChangeExtension(path, ".method.epma.zip")));
            await File.WriteAllBytesAsync(staged + ".zip", run.Package.CopyBytes(), token);
            token.ThrowIfCancellationRequested();
            Commit(files);
        }
        finally { foreach (var file in files) if (File.Exists(file.Staged)) File.Delete(file.Staged); }
    }
    private static void Commit(IReadOnlyList<(string Staged, string Target)> files)
    {
        var committed = new List<(string Target, string? Backup)>();
        try
        {
            foreach (var file in files)
            {
                string? backup = File.Exists(file.Target) ? file.Staged + ".previous" : null;
                if (backup is not null) File.Move(file.Target, backup);
                committed.Add((file.Target, backup));
                File.Move(file.Staged, file.Target);
            }
        }
        catch
        {
            foreach (var file in committed.AsEnumerable().Reverse())
            {
                if (File.Exists(file.Target)) File.Delete(file.Target);
                if (file.Backup is not null) File.Move(file.Backup, file.Target);
            }
            throw;
        }
        foreach (var file in committed) if (file.Backup is not null) File.Delete(file.Backup);
    }
    private static void Fill(Worksheet sheet, ExportTable table, CancellationToken token)
    {
        sheet.Resize(Math.Max(1, table.Rows.Count + 1), Math.Max(1, table.Headers.Count));
        sheet.SetSettings(WorksheetSettings.Edit_AutoFormatCell, false);
        sheet.SetRangeDataFormat(new RangePosition(0, 0, sheet.RowCount, sheet.ColumnCount), CellDataFormatFlag.Text);
        for (int c = 0; c < table.Headers.Count; c++) sheet[0, c] = table.Headers[c];
        for (int r = 0; r < table.Rows.Count; r++)
        {
            token.ThrowIfCancellationRequested();
            for (int c = 0; c < table.Headers.Count; c++)
                sheet[r + 1, c] = table.Rows[r][c] switch
                {
                    ExportCell.Number n when double.IsFinite(n.Value) => (object)n.Value,
                    ExportCell.Text t => t.Value, _ => null,
                };
        }
    }
}
