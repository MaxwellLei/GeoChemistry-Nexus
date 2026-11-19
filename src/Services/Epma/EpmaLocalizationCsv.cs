using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Services.Epma;

/// <summary>Reads and writes the localization table as CSV: first row is the header, then one row per item.</summary>
public static class EpmaLocalizationCsv
{
    public static string Serialize(IReadOnlyList<string[]> rows)
    {
        var text = new StringBuilder();
        foreach (var row in rows)
            text.Append(string.Join(",", row.Select(ReoGridFormatHelper.EscapeCsvCell))).Append("\r\n");
        return text.ToString();
    }

    /// <summary>Splits CSV text into rows; quoted cells may contain commas, quotes and line breaks.</summary>
    public static List<string[]> Parse(string content)
    {
        var rows = new List<string[]>();
        var cells = new List<string>();
        var cell = new StringBuilder();
        bool quoted = false;
        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];
            if (quoted)
            {
                if (c != '"') cell.Append(c);
                else if (i + 1 < content.Length && content[i + 1] == '"') { cell.Append('"'); i++; }
                else quoted = false;
                continue;
            }
            switch (c)
            {
                case '"': quoted = true; break;
                case ',': cells.Add(cell.ToString()); cell.Clear(); break;
                case '\r': break;
                case '\n': EndRow(); break;
                default: cell.Append(c); break;
            }
        }
        if (cell.Length > 0 || cells.Count > 0) EndRow();
        return rows;

        void EndRow()
        {
            cells.Add(cell.ToString()); cell.Clear();
            // Blank lines are skipped, the same as an empty spreadsheet row.
            if (cells.Count > 1 || cells[0].Trim().Length > 0) rows.Add(cells.ToArray());
            cells.Clear();
        }
    }
}
