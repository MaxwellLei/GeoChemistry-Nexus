using System;

namespace GeoChemistryNexus.Services.Epma.Export;

/// <summary>Counts formula-prefixed text cells without changing export contents.</summary>
public static class CsvFormulaPrefixDetector
{
    public static int Count(ExportTable table)
    {
        ArgumentNullException.ThrowIfNull(table);
        int count = 0;
        foreach (var row in table.Rows)
            foreach (var cell in row)
                if (cell is ExportCell.Text { Value.Length: > 0 } text && text.Value[0] is '=' or '+' or '-' or '@')
                    count++;
        return count;
    }
}
