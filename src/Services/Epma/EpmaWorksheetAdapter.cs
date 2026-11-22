using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using GeoChemistryNexus.Services.Epma.Input;
using GeoChemistryNexus.Helpers;
using unvell.ReoGrid;
using unvell.ReoGrid.DataFormat;
using unvell.ReoGrid.Events;

namespace GeoChemistryNexus.Services.Epma;

/// <summary>Only this adapter touches ReoGrid. Input capture and result display always run on the UI thread.</summary>
public sealed class EpmaWorksheetAdapter
{
    private const string CellFontName = ReoGridTableAppearance.CellFontName;
    private const float CellFontSize = ReoGridTableAppearance.CellFontSize;
    private const double ColumnHorizontalPadding = 20;
    private static readonly Typeface CellTypeface = new(CellFontName);
    private static readonly Typeface HeaderTypeface = new(new FontFamily(CellFontName), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
    private readonly ReoGridControl _grid;
    private bool _writing;
    private IReadOnlyList<EpmaScriptOutput> _displayFields = Array.Empty<EpmaScriptOutput>();
    private int InputColumnCount { get; set; }
    public int SelectedInputRow => Sheet.SelectionRange.Row + 1;
    public event Action<int>? InputChanged;
    public event Action? PasteRejected;
    private IReadOnlyList<string> _headers = Array.Empty<string>();
    // Units shown under the column names: input units come from the template manifest, output units from the result fields.
    // The first column is always the "Sample" input: its header Text stays "Sample" (it is the input id), only the displayed name is translated.
    private readonly WorksheetColumnUnitProvider _unitProvider = new(localizedLeadingColumns: 1);
    private IReadOnlyList<string?> _inputUnits = Array.Empty<string?>();
    private IReadOnlyList<string?> _inputGroups = Array.Empty<string?>();
    public event Action<int>? SelectionChanged;
    public event Action? SelectedCellChanged;
    private Worksheet Sheet => _grid.CurrentWorksheet;
    public EpmaWorksheetAdapter(ReoGridControl grid)
    {
        _grid = grid;
        grid.ToolTip = null;
        ConfigureAppearance();
        grid.ColumnHeaderContextMenu = null;
        grid.RowHeaderContextMenu = null;
        grid.LeadHeaderContextMenu = null;
        ReoGridTableAppearance.SetColumnHeaderProvider(grid, _unitProvider);
        ReoGridImeHelper.Attach(grid);
        ReoGridDoubleClickFillHelper.Attach(grid);
        grid.SetSettings(WorkbookSettings.View_ShowSheetTabControl, false);
        Sheet.BeforePaste += BeforePaste;
        Sheet.CellDataChanged += CellChanged;
        Sheet.SelectionRangeChanged += (_, _) =>
        {
            if (_writing) return;
            SelectionChanged?.Invoke(SelectedInputRow);
            SelectedCellChanged?.Invoke();
        };
        Sheet.FocusPosChanged += (_, _) => { if (!_writing) SelectedCellChanged?.Invoke(); };
    }
    public (string Address, string Content, bool CanEdit) GetSelectedCell()
    {
        _grid.Dispatcher.VerifyAccess();
        var position = Sheet.FocusPos;
        if (position.Row < 0 || position.Col < 0 || position.Row >= Sheet.RowCount || position.Col >= Sheet.ColumnCount)
            return ("--", "", false);
        bool canEdit = position.Col < InputColumnCount && !Sheet.Cells[position.Row, position.Col].IsReadOnly;
        return (position.ToAddress(), Convert.ToString(Sheet.GetCellData(position.Row, position.Col), CultureInfo.InvariantCulture) ?? "", canEdit);
    }
    public void SetSelectedCellContent(string value)
    {
        _grid.Dispatcher.VerifyAccess();
        var selected = GetSelectedCell();
        if (!selected.CanEdit || string.Equals(selected.Content, value, StringComparison.Ordinal)) return;
        var position = Sheet.FocusPos;
        // Use the same change event as direct grid edits, including result invalidation.
        Sheet[position.Row, position.Col] = value;
    }
    public InputTableSnapshot Capture()
    {
        _grid.Dispatcher.VerifyAccess();
        int last = Math.Min(Sheet.MaxContentRow, Sheet.RowCount - 1);
        var headers = _headers;
        var rows = Enumerable.Range(0, Math.Max(0, last + 1)).Select(r => (IReadOnlyList<RawCell>)
            Enumerable.Range(0, InputColumnCount).Select(c => Raw(Sheet[r, c])).ToArray()).ToArray();
        // Result data can extend below input after edits; the runtime handles explicit empty rows.
        return new InputTableSnapshot(headers, rows);
    }
    private static RawCell Raw(object? value) => value switch
    {
        null => new RawCell.Empty(), string t => new RawCell.Text(t),
        double d => new RawCell.NativeNumber(d), float f => new RawCell.NativeNumber(f),
        decimal d => new RawCell.NativeNumber((double)d), int i => new RawCell.NativeNumber(i),
        long i => new RawCell.NativeNumber(i), short i => new RawCell.NativeNumber(i),
        _ => new RawCell.Text(Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""),
    };
    /// <summary>Units and group headings of the active input columns, in column order (null or empty when a column has none). Call before <see cref="Load"/>.</summary>
    public void SetInputHeaders(IEnumerable<string?> units, IEnumerable<string?> groups)
    {
        _inputUnits = units.ToArray();
        _inputGroups = groups.ToArray();
    }
    private void RefreshColumnUnits()
    {
        var units = new List<string?>(InputColumnCount + 1 + _displayFields.Count);
        for (int c = 0; c < InputColumnCount; c++) units.Add(c < _inputUnits.Count ? _inputUnits[c] : null);
        units.Add(null);   // separator column
        foreach (var field in _displayFields) units.Add(field.DisplayUnit);
        // Group bands come from the package's "group" of an input or output column.
        var groups = new List<string?>(units.Count);
        for (int c = 0; c < InputColumnCount; c++) groups.Add(c < _inputGroups.Count ? _inputGroups[c] : null);
        groups.Add(null);   // separator column
        foreach (var field in _displayFields) groups.Add(field.DisplayGroup);
        _unitProvider.SetHeaders(units, groups);
    }
    public void Load(InputTableSnapshot input)
    {
        _grid.Dispatcher.VerifyAccess();
        _writing = true;
        try
        {
            Sheet.Reset();
            _displayFields = Array.Empty<EpmaScriptOutput>();
            _headers = input.Headers.ToArray();
            InputColumnCount = _headers.Count;
            Sheet.Resize(Math.Max(WorksheetDefaultsHelper.GetDefaultRowCount(WorksheetDefaultsHelper.EpmaConfigKey), input.Rows.Count + 1), Math.Max(1, InputColumnCount + 1));
            StyleCells();
            ConfigureInput();
            for (int c = 0; c < input.Headers.Count; c++)
            {
                SizeColumnToHeader(c, input.Headers[c]);
            }
            for (int r = 0; r < input.Rows.Count; r++)
                for (int c = 0; c < InputColumnCount; c++) Sheet[r, c] = input.Rows[r][c] switch
                {
                    RawCell.Text t => t.Value, RawCell.NativeNumber n => (object)n.Value, _ => null,
                };
            StyleColumns();
            RefreshColumnUnits();
            ProtectResults();
            Sheet.SelectionRange = new RangePosition(0, 0, 1, 1);
            Sheet.FreezeToCell(0, Math.Min(1, InputColumnCount));
        }
        finally { _writing = false; SelectedCellChanged?.Invoke(); }
    }
    private void ConfigureInput()
    {
        Sheet.SetSettings(WorksheetSettings.Edit_AutoFormatCell, false);
        Sheet.SetSettings(WorksheetSettings.View_AllowCellTextOverflow, false);
        Sheet.SetRangeDataFormat(new RangePosition(0, 0, Sheet.RowCount, InputColumnCount), CellDataFormatFlag.Text);
        Sheet.SetRangeStyles(new RangePosition(0, 0, Sheet.RowCount, InputColumnCount), new WorksheetRangeStyle
        {
            Flag = PlainStyleFlag.HorizontalAlign | PlainStyleFlag.VerticalAlign,
            HAlign = ReoGridHorAlign.Left, VAlign = ReoGridVerAlign.Middle,
        });
    }
    private void SizeColumnToHeader(int column, string header)
    {
        double width = MeasureTextWidth(header, true);
        // The first column shows the translated "Sample" name; make sure it fits in every language.
        if (column == 0) width = Math.Max(width, MeasureTextWidth(BuiltInColumnHeaders.GetDisplayText(header), true));
        SetColumnContentWidth(column, width);
    }

    private double MeasureTextWidth(string value, bool header = false)
    {
        // ReoGrid font sizes are in points; WPF text measurement uses device-independent pixels.
        var text = new FormattedText(value, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            header ? HeaderTypeface : CellTypeface,
            header ? ReoGridTableAppearance.HeaderFontSize : CellFontSize * 96.0 / 72.0,
            Brushes.Black, VisualTreeHelper.GetDpi(_grid).PixelsPerDip);
        return text.WidthIncludingTrailingWhitespace;
    }
    private void SetColumnContentWidth(int column, double contentWidth)
    {
        ushort width = (ushort)Math.Clamp(Math.Ceiling(contentWidth + ColumnHorizontalPadding),
            ColumnHorizontalPadding, ushort.MaxValue);
        Sheet.SetColumnsWidth(column, 1, width);
    }
    private void SizeResultColumns(IEnumerable<int> dataRows, IReadOnlySet<string>? textFields = null)
    {
        int[] rows = dataRows.ToArray();
        // Repeated numeric values share a measurement; fit only populated result rows, not the empty worksheet.
        var measuredValues = new Dictionary<string, double>(StringComparer.Ordinal);
        for (int c = 0; c < _displayFields.Count; c++)
        {
            var field = _displayFields[c];
            if (textFields is not null && !textFields.Contains(field.Id)) continue;
            int column = InputColumnCount + 1 + c;
            double width = MeasureTextWidth(field.DisplayLabel, true);
            foreach (int row in rows)
            {
                string value = Sheet.Cells[row, column].DisplayText;
                if (string.IsNullOrEmpty(value)) continue;
                if (!measuredValues.TryGetValue(value, out double valueWidth))
                    measuredValues[value] = valueWidth = MeasureTextWidth(value);
                width = Math.Max(width, valueWidth);
            }
            SetColumnContentWidth(column, width);
        }
    }
    public void ResetLayout()
    {
        _grid.Dispatcher.VerifyAccess();
        // Restore geometry only: data, results, styles and calculation validity stay intact.
        Sheet.SetRowsHeight(0, Sheet.RowCount, ReoGridTableAppearance.BodyRowHeight);
        for (int c = 0; c < InputColumnCount; c++) SizeColumnToHeader(c, _headers[c]);
        Sheet.SetColumnsWidth(InputColumnCount, 1, 18);
        SizeResultColumns(Enumerable.Range(0, Math.Clamp(Sheet.MaxContentRow + 1, 0, Sheet.RowCount)));
        Sheet.RequestInvalidate();
    }
    private void ConfigureAppearance() => ReoGridTableAppearance.ApplyControlStyle(_grid);
    private void StyleCells()
    {
        ReoGridTableAppearance.ApplyCellDefaults(Sheet);
        Sheet.SetRangeStyles(RangePosition.EntireRange, new WorksheetRangeStyle
        {
            Flag = PlainStyleFlag.FontName | PlainStyleFlag.FontSize | PlainStyleFlag.TextColor
                | PlainStyleFlag.HorizontalAlign | PlainStyleFlag.VerticalAlign,
            FontName = CellFontName, FontSize = CellFontSize, TextColor = Color.FromRgb(51, 65, 85),
            HAlign = ReoGridHorAlign.Center, VAlign = ReoGridVerAlign.Middle,
        });
    }
    private void StyleColumns()
    {
        Sheet.SetSettings(WorksheetSettings.View_ShowColumnHeader, true);
        for (int c = 0; c < InputColumnCount; c++) ReoGridTableAppearance.SetColumnHeader(_grid, c, _headers[c]);
        Sheet.SetColumnsWidth(InputColumnCount, 1, 18);
        ReoGridTableAppearance.SetColumnHeader(_grid, InputColumnCount, "", ReoGridColumnKind.Separator);
        for (int c = 0; c < _displayFields.Count; c++)
            ReoGridTableAppearance.SetColumnHeader(_grid, InputColumnCount + 1 + c, _displayFields[c].DisplayLabel, ReoGridColumnKind.Output);
        ReoGridTableAppearance.ConfigureColumnHeaderHeight(Sheet);
        if (_displayFields.Count == 0) return;
        Sheet.SetRangeStyles(new RangePosition(0, InputColumnCount + 1, Sheet.RowCount, _displayFields.Count),
            new WorksheetRangeStyle { Flag = PlainStyleFlag.BackColor, BackColor = Color.FromRgb(247, 250, 255) });
        // ReoGrid hides default grid lines on filled cells; retain table structure in the tinted result area.
        Sheet.SetRangeBorders(new RangePosition(0, InputColumnCount + 1, Sheet.RowCount, _displayFields.Count),
            BorderPositions.All, new RangeBorderStyle { Color = Color.FromRgb(218, 229, 243), Style = BorderLineStyle.Solid });
    }
    public void Display(EpmaScriptRun run, int decimals)
    {
        _grid.Dispatcher.VerifyAccess();
        var selection = Sheet.SelectionRange;
        var focus = Sheet.FocusPos;
        _writing = true;
        try
        {
            Sheet.ColumnCount = InputColumnCount + 1;
            _displayFields = run.Fields;
            Sheet.ColumnCount += run.Fields.Count;
            Sheet.SetColumnsWidth(InputColumnCount, 1, 18);
            Sheet.SetRangeStyles(new RangePosition(0, InputColumnCount, Sheet.RowCount, 1), new WorksheetRangeStyle
            { Flag = PlainStyleFlag.BackColor, BackColor = Color.FromRgb(226, 232, 240) });
            foreach (var row in run.Rows)
                for (int c = 0; c < run.Fields.Count; c++)
                {
                    string text = "";
                    if (row.Result.Values.TryGetValue(run.Fields[c].Id, out var value))
                        text = value.ValueKind switch
                        {
                            JsonValueKind.Null => "—",
                            JsonValueKind.Number => EpmaResultPresentation.Number(value.GetDouble(), decimals),
                            _ => EpmaScriptText.Scalar(value),
                        };
                    Sheet[row.InputRow - 1, InputColumnCount + 1 + c] = text;
                }
            ProtectResults(); StyleColumns(); RefreshColumnUnits();
            SizeResultColumns(run.Rows.Select(row => row.InputRow - 1));
            // Rebuilding result columns temporarily removes the selected output cell.
            // Restore its address so the toolbar keeps showing the same cell after a refresh.
            if (selection.Row >= 0 && selection.Col >= 0 && selection.EndRow < Sheet.RowCount && selection.EndCol < Sheet.ColumnCount)
            {
                Sheet.SelectionRange = selection;
                if (focus.Row >= 0 && focus.Col >= 0 && focus.Row < Sheet.RowCount && focus.Col < Sheet.ColumnCount)
                    Sheet.FocusPos = focus;
            }
        }
        finally { _writing = false; SelectedCellChanged?.Invoke(); }
    }
    // Only update text fields after background rendering. Keep numeric cells and selection intact.
    public void UpdateDisplayText(EpmaScriptRun run, IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> displays)
    {
        _grid.Dispatcher.VerifyAccess();
        _writing = true;
        try
        {
            var written = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in run.Rows)
            {
                if (!displays.TryGetValue(row.InputRow, out var display)) continue;
                for (int i = 0; i < run.Fields.Count; i++)
                {
                    var field = run.Fields[i];
                    if (!display.TryGetValue(field.Id, out var text)) continue;
                    if (!row.Result.Values.TryGetValue(field.Id, out var original) || original.ValueKind != JsonValueKind.String) continue;
                    Sheet[row.InputRow - 1, InputColumnCount + 1 + i] = text;
                    written.Add(field.Id);
                }
            }
            SizeResultColumns(run.Rows.Select(row => row.InputRow - 1), written);
        }
        finally { _writing = false; SelectedCellChanged?.Invoke(); }
    }
    /// <summary>Registers the dots drawn in the row headers (ok / warning / error / stale / not calculated).</summary>
    public void SetRowStatusProvider(IWorksheetRowStatusProvider provider) => ReoGridTableAppearance.SetRowStatusProvider(_grid, provider);
    /// <summary>Whether the input columns of a worksheet row (zero-based) hold anything; used to tell empty rows from uncalculated ones.</summary>
    public bool HasInput(int row)
    {
        if (row < 0 || row >= Sheet.RowCount) return false;
        for (int c = 0; c < InputColumnCount; c++)
            if (Sheet.GetCellData(row, c) is { } value && (value is not string text || text.Length > 0)) return true;
        return false;
    }
    private void CellChanged(object? sender, CellEventArgs e)
    {
        if (_writing) return;
        SelectedCellChanged?.Invoke();
        if (e.Cell.Position.Col >= InputColumnCount) return;
        int row = e.Cell.Position.Row;
        if (row >= Sheet.RowCount - 1) { Sheet.RowCount++; ConfigureInput(); ProtectResults(); }
        InputChanged?.Invoke(row + 1);
    }
    private void BeforePaste(object? sender, BeforeRangeOperationEventArgs e)
    {
        if (!CanPaste(e.Range.Row, e.Range.Col, e.Range.Cols))
        {
            e.IsCancelled = true; PasteRejected?.Invoke(); return;
        }
        if (!Clipboard.ContainsText()) return;
        string text = Clipboard.GetText();
        var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        int columns = lines.Max(l => l.Split('\t').Length);
        if (!CanPaste(Sheet.SelectionRange.Row, Sheet.SelectionRange.Col, columns))
        {
            e.IsCancelled = true; PasteRejected?.Invoke(); return;
        }
        int count = lines.Length - (lines.LastOrDefault() == "" ? 1 : 0);
        int required = Sheet.SelectionRange.Row + count + 1;
        if (required > Sheet.RowCount) { Sheet.RowCount = required; ConfigureInput(); ProtectResults(); }
    }
    private bool CanPaste(int row, int column, int width) => row >= 0 && column >= 0 && width > 0 && column + width <= InputColumnCount;
    private void ProtectResults()
    {
        for (int c = InputColumnCount; c < Sheet.ColumnCount; c++)
            for (int r = 0; r < Sheet.RowCount; r++) Sheet.Cells[r, c].IsReadOnly = true;
    }
}
