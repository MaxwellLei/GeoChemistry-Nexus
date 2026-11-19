using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GeoChemistryNexus.Services.Epma.Export;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Services.Epma;

public static class EpmaCsvWriter
{
    public static string CellText(ExportCell cell) => cell switch
    {
        ExportCell.Number n when double.IsFinite(n.Value) => n.Value.ToString("R", CultureInfo.InvariantCulture),
        ExportCell.Text t => t.Value, _ => "",
    };
    public static string Serialize(ExportTable table)
    {
        var text = new StringBuilder();
        text.Append(string.Join(",", table.Headers.Select(ReoGridFormatHelper.EscapeCsvCell))).Append("\r\n");
        foreach (var row in table.Rows)
            text.Append(string.Join(",", row.Select(c => ReoGridFormatHelper.EscapeCsvCell(CellText(c))))).Append("\r\n");
        return text.ToString();
    }
    public static Task WriteAsync(string path, ExportTable table, CancellationToken token = default) =>
        File.WriteAllTextAsync(path, Serialize(table), new UTF8Encoding(true), token);
}
