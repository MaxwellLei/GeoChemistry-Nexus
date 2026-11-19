using System;
using System.Collections.Generic;
using System.Linq;
using GeoChemistryNexus.Services.Epma.Input;

namespace GeoChemistryNexus.Services.Epma;

/// <summary>Fixed input IDs. Inactive columns retain their raw cells until explicitly cleared.</summary>
public sealed class EpmaInputTemplate
{
    private readonly Dictionary<string, RawCell[]> _cells = new(StringComparer.Ordinal);
    private int _rowCount;

    public void Clear() { _cells.Clear(); _rowCount = 0; }
    public void Remember(InputTableSnapshot input)
    {
        _rowCount = Math.Max(_rowCount, input.Rows.Count);
        for (int c = 0; c < input.Headers.Count; c++)
        {
            string header = input.Headers[c];
            // Visible blanks/deletions supersede previously stored values.
            _cells[header] = Enumerable.Range(0, input.Rows.Count).Select(r => input.Rows[r][c]).ToArray();
        }
    }
    public InputTableSnapshot Build(IReadOnlyList<string> headers) => new(headers,
        Enumerable.Range(0, _rowCount).Select(row => (IReadOnlyList<RawCell>)headers.Select(header =>
            _cells.TryGetValue(header, out var values) && row < values.Length ? values[row] : new RawCell.Empty()).ToArray()).ToArray());
    public IReadOnlyList<string> RetainedColumns(IReadOnlyList<string> visible) => _cells
        .Where(p => !visible.Contains(p.Key) && p.Value.Any(HasValue)).Select(p => p.Key).ToArray();
    private static bool HasValue(RawCell cell) => cell is RawCell.NativeNumber || cell is RawCell.Text t && !string.IsNullOrWhiteSpace(t.Value);
}
