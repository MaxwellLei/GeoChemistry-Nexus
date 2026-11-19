using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services.Epma;
using Microsoft.Win32;

namespace GeoChemistryNexus.ViewModels;

public enum EpmaLocalizationRowKind { Name, Description, Tag }

public enum EpmaLocalizationColumnState { Empty, Partial, Complete }

/// <summary>One item of the localization table; <see cref="Values"/> has one entry per language column.</summary>
public sealed class EpmaLocalizationRow
{
    public EpmaLocalizationRow(EpmaLocalizationRowKind kind, string label, int languageCount)
    {
        Kind = kind; Label = label; Values = Enumerable.Repeat("", languageCount).ToArray();
    }
    public EpmaLocalizationRowKind Kind { get; }
    public string Label { get; }
    public string[] Values { get; }
}

/// <summary>The editable wording of one item in one language.</summary>
public partial class EpmaLocalizationCell : ObservableObject
{
    [ObservableProperty] private string _text;
    public string Placeholder { get; }
    /// <summary>Long wrapped text (the description) uses a taller multi-line box.</summary>
    public bool IsMultiline { get; }
    /// <summary>The language column this cell belongs to; the cell takes its width from it.</summary>
    public EpmaLocalizationColumn? Column { get; }
    public EpmaLocalizationCell(string text, string placeholder, bool isMultiline, EpmaLocalizationColumn? column = null)
    {
        _text = text; Placeholder = placeholder; IsMultiline = isMultiline; Column = column;
    }
}

/// <summary>One item row of the table as displayed: label on the left, one cell per language.</summary>
public sealed partial class EpmaLocalizationTableRow : ObservableObject
{
    public EpmaLocalizationTableRow(EpmaLocalizationRow source, IReadOnlyList<EpmaLocalizationCell> cells)
    {
        Source = source; Cells = cells;
        _rowHeight = cells.Count > 0 && cells[0].IsMultiline ? 106 : 42;
    }

    /// <summary>Row height, shared by the frozen and the scrolling part of the table; the user can drag it to adjust.</summary>
    [ObservableProperty] private double _rowHeight;
    public EpmaLocalizationRow Source { get; }
    public string Label => Source.Label;
    public bool IsDescription => Source.Kind == EpmaLocalizationRowKind.Description;
    public bool IsTag => Source.Kind == EpmaLocalizationRowKind.Tag;
    public IReadOnlyList<EpmaLocalizationCell> Cells { get; }
    /// <summary>The default language cell, shown in the frozen column.</summary>
    public EpmaLocalizationCell DefaultCell => Cells[0];
    /// <summary>The cells of every other language, shown in the horizontally scrolling area.</summary>
    public IReadOnlyList<EpmaLocalizationCell> OtherCells => Cells.Skip(1).ToList();
}

/// <summary>Column header: language name, default badge and the fill progress of that language.</summary>
public partial class EpmaLocalizationColumn : ObservableObject
{
    public EpmaLocalizationColumn(string header, bool isDefault) { Header = header; IsDefault = isDefault; }
    public string Header { get; }
    public bool IsDefault { get; }
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Progress))] private int _filled;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Progress))] private int _total;
    [ObservableProperty] private EpmaLocalizationColumnState _state;
    public string Progress => $"{Filled}/{Total}";

    /// <summary>Column width; all columns start with the same width and the user can drag it to adjust.</summary>
    [ObservableProperty] private double _width = 280;

    /// <summary>True once the user dragged this column; such a column no longer follows the automatic equal split.</summary>
    public bool IsWidthCustomized { get; private set; }

    private bool _settingAutoWidth;

    partial void OnWidthChanged(double value)
    {
        if (!_settingAutoWidth) IsWidthCustomized = true;
    }

    /// <summary>Applies the automatic equal width unless the user has adjusted this column.</summary>
    public void SetAutoWidth(double width)
    {
        if (IsWidthCustomized) return;
        _settingAutoWidth = true;
        try { Width = width; }
        finally { _settingAutoWidth = false; }
    }
}

/// <summary>Table dialog: rows are name, description and tags; columns are languages, the default language first.</summary>
public partial class EpmaLocalizationSheetViewModel : ObservableObject
{
    /// <summary>导出成功提示沿这个窗口的 Owner 链找到通知中心。</summary>
    public Window? HostWindow { get; set; }

    public IReadOnlyList<CultureOption> Languages { get; }
    public IReadOnlyList<EpmaLocalizationRow> Rows { get; }
    public IReadOnlyList<EpmaLocalizationColumn> Columns { get; }
    /// <summary>The default language column, frozen next to the item names.</summary>
    public EpmaLocalizationColumn DefaultColumn => Columns[0];
    public IReadOnlyList<EpmaLocalizationColumn> OtherColumns { get; }
    public IReadOnlyList<EpmaLocalizationTableRow> TableRows { get; }
    public bool IsReadOnly { get; }
    public string Title => EpmaLocalizer.Format("epma_editor_localization_title");
    public bool CanEdit => !IsReadOnly;
    public int LanguageCount => Languages.Count;
    /// <summary>Raised with true when the edited values should be applied.</summary>
    public event Action<bool>? CloseRequested;

    public EpmaLocalizationSheetViewModel(IReadOnlyList<CultureOption> languages, IReadOnlyList<EpmaLocalizationRow> rows, bool isReadOnly)
    {
        Languages = languages; Rows = rows; IsReadOnly = isReadOnly;
        Columns = languages.Select((language, index) => new EpmaLocalizationColumn(language.DisplayName, index == 0)).ToList();
        OtherColumns = Columns.Skip(1).ToList();
        TableRows = rows.Select(row =>
        {
            string placeholder = EpmaLocalizer.Format("epma_loc_placeholder");
            var cells = row.Values.Select((value, index) =>
                new EpmaLocalizationCell(value, placeholder, row.Kind == EpmaLocalizationRowKind.Description, Columns[index])).ToList();
            foreach (var cell in cells) cell.PropertyChanged += (_, _) => RefreshProgress();
            return new EpmaLocalizationTableRow(row, cells);
        }).ToList();
        RefreshProgress();
    }

    /// <summary>
    /// Counts the filled items per language. A tag row stops counting when its default-language tag is cleared,
    /// because that tag is removed when the table is confirmed.
    /// </summary>
    private void RefreshProgress()
    {
        var used = TableRows.Where(row => !row.IsTag || !string.IsNullOrWhiteSpace(row.Cells[0].Text)).ToList();
        for (int column = 0; column < Columns.Count; column++)
        {
            int filled = used.Count(row => !string.IsNullOrWhiteSpace(row.Cells[column].Text));
            var target = Columns[column];
            target.Filled = filled; target.Total = used.Count;
            target.State = filled == 0 ? EpmaLocalizationColumnState.Empty
                : filled >= used.Count ? EpmaLocalizationColumnState.Complete : EpmaLocalizationColumnState.Partial;
        }
    }

    [RelayCommand]
    private void Confirm()
    {
        if (IsReadOnly) { CloseRequested?.Invoke(false); return; }
        for (int row = 0; row < Rows.Count; row++)
            for (int language = 0; language < Languages.Count; language++)
                Rows[row].Values[language] = TableRows[row].Cells[language].Text.Trim();
        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);

    [RelayCommand]
    private async Task ExportAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = EpmaLocalizer.Format("epma_loc_export"),
            Filter = FileDialogFilterHelper.CsvFiles, DefaultExt = ".csv", AddExtension = true, FileName = Title + ".csv",
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var lines = new List<string[]> { Languages.Select(language => language.DisplayName).Prepend(EpmaLocalizer.Format("epma_loc_field_header")).ToArray() };
            lines.AddRange(TableRows.Select(row => row.Cells.Select(cell => cell.Text).Prepend(row.Label).ToArray()));
            await File.WriteAllTextAsync(dialog.FileName, EpmaLocalizationCsv.Serialize(lines), new UTF8Encoding(true));
            MessageHelper.ExportSuccess(EpmaLocalizer.Format("epma_loc_export_done"), dialog.FileName, HostWindow);
        }
        catch (Exception ex) { MessageHelper.Error(EpmaLocalizer.Format("epma_loc_failed", ex.Message)); }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        if (IsReadOnly) return;
        var dialog = new OpenFileDialog { Title = EpmaLocalizer.Format("epma_loc_import"), Filter = FileDialogFilterHelper.CsvFiles + "|" + FileDialogFilterHelper.AllFiles };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var lines = EpmaLocalizationCsv.Parse(await File.ReadAllTextAsync(dialog.FileName));
            if (lines.Count == 0) return;
            // Language columns are matched by their header; a file without matching headers is read by position.
            var header = lines[0];
            int ColumnOf(int language)
            {
                int found = Array.FindIndex(header, 1, name => name.Trim().Equals(Languages[language].DisplayName, StringComparison.OrdinalIgnoreCase)
                    || name.Trim().Equals(Languages[language].Code, StringComparison.OrdinalIgnoreCase));
                return found >= 0 ? found : language + 1;
            }
            for (int row = 0; row < TableRows.Count && row + 1 < lines.Count; row++)
                for (int language = 0; language < Languages.Count; language++)
                {
                    int column = ColumnOf(language);
                    if (column < lines[row + 1].Length) TableRows[row].Cells[language].Text = lines[row + 1][column];
                }
            MessageHelper.Success(EpmaLocalizer.Format("epma_loc_import_done", Path.GetFileName(dialog.FileName)));
        }
        catch (Exception ex) { MessageHelper.Error(EpmaLocalizer.Format("epma_loc_failed", ex.Message)); }
    }
}
