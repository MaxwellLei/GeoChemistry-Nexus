using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using unvell.ReoGrid;
using unvell.ReoGrid.Events;

namespace GeoChemistryNexus.ViewModels
{
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "WPF binding requires instance members.")]
    public partial class GeothermometerFreeSheetViewModel : ObservableObject
    {
        private const int InitialColumnCount = 20;

        private ReoGridControl? _grid;
        private Worksheet? _rowExpansionWorksheet;
        private bool _isUpdatingSelectedCellEditor;
        private bool _isSyncingToolbarState;
        private bool _isApplyingFormatPainter;
        private CellStyleSnapshot? _formatPainterStyle;
        private int _formatPainterSourceRow = -1;
        private int _formatPainterSourceCol = -1;
        private IFreeSheetNotificationHost? _notificationHost;

        [ObservableProperty]
        private string selectedCellAddress = "--";

        [ObservableProperty]
        private string selectedCellContent = string.Empty;

        [ObservableProperty]
        private bool isSelectedCellEditable;

        [ObservableProperty]
        private bool canUndo;

        [ObservableProperty]
        private bool canRedo;

        [ObservableProperty]
        private bool isBold;

        [ObservableProperty]
        private bool isItalic;

        [ObservableProperty]
        private bool isUnderline;

        [ObservableProperty]
        private string selectedFontName = "Segoe UI";

        [ObservableProperty]
        private float selectedFontSize = 10f;

        [ObservableProperty]
        private bool isFormatPainterActive;

        [ObservableProperty]
        private ReoGridHorAlign selectedHAlign = ReoGridHorAlign.General;

        [ObservableProperty]
        private Color selectedTextColor = Colors.Black;

        [ObservableProperty]
        private Color selectedFillColor = Colors.White;

        public string WindowTitle => LanguageService.Instance["geo_free_sheet_title"];

        public string StatusHint => LanguageService.Instance["geo_free_sheet_hint"];

        partial void OnSelectedTextColorChanged(Color value) => ApplySelectedTextColorToGrid(value);

        partial void OnSelectedFillColorChanged(Color value) => ApplySelectedFillColorToGrid(value);

        private void ApplySelectedTextColorToGrid(Color value)
        {
            if (_isSyncingToolbarState || _grid?.CurrentWorksheet == null)
                return;

            var range = ReoGridFormatHelper.GetSelectionRange(_grid.CurrentWorksheet);
            ReoGridFormatHelper.ApplyTextColor(_grid.CurrentWorksheet, range, value);
            RefreshUndoState(_grid);
        }

        private void ApplySelectedFillColorToGrid(Color value)
        {
            if (_isSyncingToolbarState || _grid?.CurrentWorksheet == null)
                return;

            var range = ReoGridFormatHelper.GetSelectionRange(_grid.CurrentWorksheet);
            ReoGridFormatHelper.ApplyBackColor(_grid.CurrentWorksheet, range, value);
            RefreshUndoState(_grid);
        }

        public void AttachNotificationHost(IFreeSheetNotificationHost? host) => _notificationHost = host;

        public void AttachGrid(ReoGridControl grid)
        {
            _grid = grid;
            ReoGridImeHelper.Attach(grid);
            InitializeWorksheet(grid);
        }

        public void InitializeWorksheet(ReoGridControl grid)
        {
            var worksheet = grid.CurrentWorksheet;
            if (worksheet == null)
            {
                worksheet = grid.CreateWorksheet();
                grid.Worksheets.Clear();
                grid.Worksheets.Add(worksheet);
                grid.CurrentWorksheet = worksheet;
            }

            worksheet.Reset();
            worksheet.Resize(
                WorksheetDefaultsHelper.GetDefaultRowCount(WorksheetDefaultsHelper.GtmConfigKey),
                InitialColumnCount);
            worksheet.Name = LanguageService.Instance["geo_free_sheet_worksheet_name"];

            var defaultStyle = new WorksheetRangeStyle
            {
                Flag = PlainStyleFlag.FontName | PlainStyleFlag.FontSize |
                       PlainStyleFlag.HorizontalAlign | PlainStyleFlag.VerticalAlign,
                FontName = "Segoe UI",
                FontSize = 10f,
                HAlign = ReoGridHorAlign.General,
                VAlign = ReoGridVerAlign.Middle
            };
            worksheet.SetRangeStyles(RangePosition.EntireRange, defaultStyle);

            for (int c = 0; c < InitialColumnCount; c++)
                worksheet.SetColumnsWidth(c, 1, 88);

            AttachWorksheetRowExpansionEvents(worksheet);
            RefreshUndoState(grid);
            UpdateSelectionDisplay(worksheet);
        }

        public void OnSelectionChanged(Worksheet worksheet)
        {
            TryApplyFormatPainter(worksheet);
            UpdateSelectionDisplay(worksheet);
            SyncToolbarFromSelection(worksheet);
            RefreshUndoState(_grid);
        }

        private void TryApplyFormatPainter(Worksheet? worksheet)
        {
            if (!IsFormatPainterActive || _formatPainterStyle == null || worksheet == null || _isApplyingFormatPainter)
                return;

            if (!ReoGridFormatHelper.TryGetPrimaryCell(worksheet, out int row, out int col))
                return;

            if (row == _formatPainterSourceRow && col == _formatPainterSourceCol)
                return;

            _isApplyingFormatPainter = true;
            try
            {
                var range = ReoGridFormatHelper.GetSelectionRange(worksheet);
                ReoGridFormatHelper.ApplyCellStyle(worksheet, range, _formatPainterStyle);
                RefreshUndoState(_grid);
                IsFormatPainterActive = false;
                _formatPainterStyle = null;
            }
            finally
            {
                _isApplyingFormatPainter = false;
            }
        }

        public void RefreshUndoState(ReoGridControl? grid)
        {
            var target = grid ?? _grid;
            if (target == null)
            {
                CanUndo = false;
                CanRedo = false;
            }
            else
            {
                CanUndo = target.CanUndo();
                CanRedo = target.CanRedo();
            }

            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        }

        public void AttachWorksheetRowExpansionEvents(Worksheet? worksheet)
        {
            if (_rowExpansionWorksheet != null)
            {
                _rowExpansionWorksheet.BeforePaste -= Worksheet_BeforePaste;
                _rowExpansionWorksheet.CellDataChanged -= Worksheet_CellDataChanged;
            }

            _rowExpansionWorksheet = worksheet;
            if (worksheet == null)
                return;

            worksheet.BeforePaste += Worksheet_BeforePaste;
            worksheet.CellDataChanged += Worksheet_CellDataChanged;
        }

        private void Worksheet_BeforePaste(object? sender, BeforeRangeOperationEventArgs e)
        {
            if (sender is not Worksheet worksheet)
                return;

            if (!Clipboard.ContainsText())
                return;

            string pasteText = Clipboard.GetText();
            if (string.IsNullOrEmpty(pasteText))
                return;

            var lines = pasteText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int pastedRowCount = lines.Length;
            if (string.IsNullOrEmpty(lines.Last()))
                pastedRowCount--;

            if (pastedRowCount <= 0)
                return;

            int startRow = worksheet.SelectionRange.Row;
            int requiredTotalRows = startRow + pastedRowCount;
            if (requiredTotalRows > worksheet.RowCount)
                worksheet.RowCount = requiredTotalRows;
        }

        private void Worksheet_CellDataChanged(object? sender, CellEventArgs e)
        {
            if (sender is not Worksheet worksheet)
                return;

            int row = e.Cell.Position.Row;
            if (row >= worksheet.RowCount - 1)
                worksheet.RowCount++;
        }

        private void UpdateSelectionDisplay(Worksheet? worksheet)
        {
            if (worksheet == null)
            {
                SelectedCellAddress = "--";
                IsSelectedCellEditable = false;
                SetSelectedCellContentSilently(string.Empty);
                return;
            }

            var range = ReoGridFormatHelper.GetSelectionRange(worksheet);
            SelectedCellAddress = ReoGridFormatHelper.FormatRangeAddress(range);

            if (!ReoGridFormatHelper.TryGetPrimaryCell(worksheet, out int row, out int col))
            {
                IsSelectedCellEditable = false;
                SetSelectedCellContentSilently(string.Empty);
                return;
            }

            if (row < 0 || col < 0 || row >= worksheet.RowCount || col >= worksheet.ColumnCount)
            {
                IsSelectedCellEditable = false;
                SetSelectedCellContentSilently(string.Empty);
                return;
            }

            string cellValue = worksheet.GetCellData(row, col)?.ToString() ?? string.Empty;
            IsSelectedCellEditable = true;
            SetSelectedCellContentSilently(cellValue);
        }

        private void SyncToolbarFromSelection(Worksheet? worksheet)
        {
            if (worksheet == null || !ReoGridFormatHelper.TryGetPrimaryCell(worksheet, out int row, out int col))
                return;

            _isSyncingToolbarState = true;
            try
            {
                var snapshot = ReoGridFormatHelper.ReadCellStyle(worksheet, row, col);
                IsBold = snapshot.Bold;
                IsItalic = snapshot.Italic;
                IsUnderline = snapshot.Underline;
                SelectedFontName = snapshot.FontName;
                SelectedFontSize = snapshot.FontSize;
                SelectedHAlign = snapshot.HAlign;
                SelectedTextColor = snapshot.TextColor;
                SelectedFillColor = snapshot.BackColor;
            }
            finally
            {
                _isSyncingToolbarState = false;
            }
        }

        private void SetSelectedCellContentSilently(string value)
        {
            _isUpdatingSelectedCellEditor = true;
            try
            {
                SelectedCellContent = value;
            }
            finally
            {
                _isUpdatingSelectedCellEditor = false;
            }
        }

        partial void OnSelectedCellContentChanged(string value)
        {
            if (_isUpdatingSelectedCellEditor)
                return;

            var worksheet = _grid?.CurrentWorksheet;
            if (worksheet == null)
                return;

            if (!ReoGridFormatHelper.TryGetPrimaryCell(worksheet, out int row, out int col))
                return;

            string newValue = value ?? string.Empty;
            string currentValue = worksheet.GetCellData(row, col)?.ToString() ?? string.Empty;
            if (string.Equals(currentValue, newValue, StringComparison.Ordinal))
                return;

            worksheet[row, col] = newValue;
        }

        partial void OnSelectedFontNameChanged(string value)
        {
            if (_isSyncingToolbarState || _grid?.CurrentWorksheet == null || string.IsNullOrWhiteSpace(value))
                return;

            var range = ReoGridFormatHelper.GetSelectionRange(_grid.CurrentWorksheet);
            ReoGridFormatHelper.ApplyFontName(_grid.CurrentWorksheet, range, value);
            RefreshUndoState(_grid);
        }

        partial void OnSelectedFontSizeChanged(float value)
        {
            if (_isSyncingToolbarState || _grid?.CurrentWorksheet == null || value <= 0)
                return;

            var range = ReoGridFormatHelper.GetSelectionRange(_grid.CurrentWorksheet);
            ReoGridFormatHelper.ApplyFontSize(_grid.CurrentWorksheet, range, value);
            RefreshUndoState(_grid);
        }

        private static Worksheet? GetWorksheet(ReoGridControl? grid) => grid?.CurrentWorksheet;

        [RelayCommand(CanExecute = nameof(CanExecuteUndo))]
        private void Undo(ReoGridControl? grid)
        {
            var target = grid ?? _grid;
            if (target == null) return;

            target.Undo();
            RefreshUndoState(target);
            UpdateSelectionDisplay(GetWorksheet(target));
        }

        private bool CanExecuteUndo(ReoGridControl? grid)
        {
            var target = grid ?? _grid;
            return target != null && target.CanUndo();
        }

        [RelayCommand(CanExecute = nameof(CanExecuteRedo))]
        private void Redo(ReoGridControl? grid)
        {
            var target = grid ?? _grid;
            if (target == null) return;

            target.Redo();
            RefreshUndoState(target);
            UpdateSelectionDisplay(GetWorksheet(target));
        }

        private bool CanExecuteRedo(ReoGridControl? grid)
        {
            var target = grid ?? _grid;
            return target != null && target.CanRedo();
        }

        partial void OnIsFormatPainterActiveChanged(bool value)
        {
            if (!value)
            {
                _formatPainterStyle = null;
                _formatPainterSourceRow = -1;
                _formatPainterSourceCol = -1;
            }
        }

        [RelayCommand]
        private void ToggleFormatPainter(ReoGridControl grid)
        {
            if (!IsFormatPainterActive)
                return;

            var worksheet = GetWorksheet(grid);
            if (worksheet == null || !ReoGridFormatHelper.TryGetPrimaryCell(worksheet, out int row, out int col))
            {
                IsFormatPainterActive = false;
                return;
            }

            _formatPainterStyle = ReoGridFormatHelper.ReadCellStyle(worksheet, row, col);
            _formatPainterSourceRow = row;
            _formatPainterSourceCol = col;
        }

        [RelayCommand]
        private void ToggleBold(ReoGridControl grid)
        {
            var worksheet = GetWorksheet(grid);
            if (worksheet == null) return;

            var range = ReoGridFormatHelper.GetSelectionRange(worksheet);
            ReoGridFormatHelper.ApplyBold(worksheet, range, !IsBold);
            IsBold = !IsBold;
            RefreshUndoState(grid);
        }

        [RelayCommand]
        private void ToggleItalic(ReoGridControl grid)
        {
            var worksheet = GetWorksheet(grid);
            if (worksheet == null) return;

            var range = ReoGridFormatHelper.GetSelectionRange(worksheet);
            ReoGridFormatHelper.ApplyItalic(worksheet, range, !IsItalic);
            IsItalic = !IsItalic;
            RefreshUndoState(grid);
        }

        [RelayCommand]
        private void ToggleUnderline(ReoGridControl grid)
        {
            var worksheet = GetWorksheet(grid);
            if (worksheet == null) return;

            var range = ReoGridFormatHelper.GetSelectionRange(worksheet);
            ReoGridFormatHelper.ApplyUnderline(worksheet, range, !IsUnderline);
            IsUnderline = !IsUnderline;
            RefreshUndoState(grid);
        }

        [RelayCommand]
        private void AlignLeft(ReoGridControl grid) => ApplyHorizontalAlign(grid, ReoGridHorAlign.Left);

        [RelayCommand]
        private void AlignCenter(ReoGridControl grid) => ApplyHorizontalAlign(grid, ReoGridHorAlign.Center);

        [RelayCommand]
        private void AlignRight(ReoGridControl grid) => ApplyHorizontalAlign(grid, ReoGridHorAlign.Right);

        [RelayCommand]
        private void AlignTop(ReoGridControl grid) => ApplyVerticalAlign(grid, ReoGridVerAlign.Top);

        [RelayCommand]
        private void AlignMiddle(ReoGridControl grid) => ApplyVerticalAlign(grid, ReoGridVerAlign.Middle);

        [RelayCommand]
        private void AlignBottom(ReoGridControl grid) => ApplyVerticalAlign(grid, ReoGridVerAlign.Bottom);

        private void ApplyHorizontalAlign(ReoGridControl grid, ReoGridHorAlign align)
        {
            var worksheet = GetWorksheet(grid);
            if (worksheet == null) return;

            var range = ReoGridFormatHelper.GetSelectionRange(worksheet);
            ReoGridFormatHelper.ApplyHorizontalAlign(worksheet, range, align);
            SelectedHAlign = align;
            RefreshUndoState(grid);
        }

        private void ApplyVerticalAlign(ReoGridControl grid, ReoGridVerAlign align)
        {
            var worksheet = GetWorksheet(grid);
            if (worksheet == null) return;

            var range = ReoGridFormatHelper.GetSelectionRange(worksheet);
            ReoGridFormatHelper.ApplyVerticalAlign(worksheet, range, align);
            RefreshUndoState(grid);
        }

        [RelayCommand]
        private void MergeCells(ReoGridControl grid)
        {
            var worksheet = GetWorksheet(grid);
            if (worksheet == null) return;

            var range = ReoGridFormatHelper.GetSelectionRange(worksheet);
            ReoGridFormatHelper.MergeOrUnmerge(worksheet, range);
            RefreshUndoState(grid);
        }

        [RelayCommand]
        private void AddRowUp(ReoGridControl grid)
        {
            var worksheet = GetWorksheet(grid);
            if (worksheet == null) return;

            var selection = worksheet.SelectionRange;
            worksheet.InsertRows(selection.Row, 1);
            RefreshUndoState(grid);
        }

        [RelayCommand]
        private void AddRowDown(ReoGridControl grid)
        {
            var worksheet = GetWorksheet(grid);
            if (worksheet == null) return;

            var selection = worksheet.SelectionRange;
            worksheet.InsertRows(selection.Row + selection.Rows, 1);
            RefreshUndoState(grid);
        }

        [RelayCommand]
        private async Task DeleteRow(ReoGridControl grid)
        {
            var worksheet = GetWorksheet(grid);
            if (worksheet == null) return;

            if (!await NotifyConfirmAsync(
                    LanguageService.Instance["geo_free_sheet_delete_row_confirm"],
                    LanguageService.Instance["Cancel"],
                    LanguageService.Instance["Confirm"]))
                return;

            var selection = worksheet.SelectionRange;
            try
            {
                if (selection.Rows >= worksheet.RowCount)
                {
                    if (worksheet.RowCount > 1)
                        worksheet.DeleteRows(selection.Row, worksheet.RowCount - 1);
                }
                else
                {
                    worksheet.DeleteRows(selection.Row, selection.Rows);
                }

                RefreshUndoState(grid);
            }
            catch (Exception ex)
            {
                NotifyWarning(LanguageService.Instance["failed_to_delete_row"] + ex.Message);
            }
        }

        [RelayCommand]
        private void AddColumnLeft(ReoGridControl grid)
        {
            var worksheet = GetWorksheet(grid);
            if (worksheet == null) return;

            var selection = worksheet.SelectionRange;
            worksheet.InsertColumns(selection.Col, 1);
            RefreshUndoState(grid);
        }

        [RelayCommand]
        private void AddColumnRight(ReoGridControl grid)
        {
            var worksheet = GetWorksheet(grid);
            if (worksheet == null) return;

            var selection = worksheet.SelectionRange;
            worksheet.InsertColumns(selection.Col + selection.Cols, 1);
            RefreshUndoState(grid);
        }

        [RelayCommand]
        private async Task DeleteColumn(ReoGridControl grid)
        {
            var worksheet = GetWorksheet(grid);
            if (worksheet == null) return;

            if (!await NotifyConfirmAsync(
                    LanguageService.Instance["geo_free_sheet_delete_col_confirm"],
                    LanguageService.Instance["Cancel"],
                    LanguageService.Instance["Confirm"]))
                return;

            var selection = worksheet.SelectionRange;
            try
            {
                if (selection.Cols >= worksheet.ColumnCount)
                {
                    if (worksheet.ColumnCount > 1)
                        worksheet.DeleteColumns(selection.Col, worksheet.ColumnCount - 1);
                }
                else
                {
                    worksheet.DeleteColumns(selection.Col, selection.Cols);
                }

                RefreshUndoState(grid);
            }
            catch (Exception ex)
            {
                NotifyWarning(LanguageService.Instance["geo_free_sheet_delete_col_failed"] + ex.Message);
            }
        }

        [RelayCommand]
        private void ImportWorksheet(ReoGridControl grid)
        {
            string? filePath = FileHelper.GetFilePath(
                LanguageService.Instance["csv_file_filter"],
                _notificationHost?.OwnerWindow);
            if (filePath == null)
            {
                NotifyInfo(LanguageService.Instance["geo_msg_import_cancelled"]);
                return;
            }

            try
            {
                grid.Load(filePath);
                AttachWorksheetRowExpansionEvents(grid.CurrentWorksheet);
                RefreshUndoState(grid);
                UpdateSelectionDisplay(grid.CurrentWorksheet);
                NotifySuccess(LanguageService.Instance["geo_msg_import_success"]);
            }
            catch (Exception ex)
            {
                NotifyError(string.Format(LanguageService.Instance["geo_free_sheet_import_failed"], ex.Message));
            }
        }

        [RelayCommand]
        private async Task ExportWorksheet(ReoGridControl grid)
        {
            var worksheet = grid.CurrentWorksheet;
            if (worksheet == null || _notificationHost == null)
                return;

            FreeSheetCsvExportMode? exportMode = await _notificationHost.ShowExportModeAsync();
            if (exportMode == null)
                return;

            string? filePath = await FileHelper.GetSaveFilePath2Async(
                title: LanguageService.Instance["geo_msg_csv_save_title"],
                filter: LanguageService.Instance["csv_file_filter"],
                defaultExt: ".csv",
                defaultFileName: worksheet.Name,
                owner: _notificationHost.OwnerWindow);
            if (string.IsNullOrEmpty(filePath))
                return;

            try
            {
                string csvContent = ReoGridFormatHelper.BuildCsv(worksheet, exportMode.Value);
                await Task.Run(() => File.WriteAllText(filePath, csvContent, new UTF8Encoding(true)));
                NotifySuccess(LanguageService.Instance["geo_msg_export_success"]);
            }
            catch (Exception ex)
            {
                NotifyError(string.Format(LanguageService.Instance["geo_msg_export_failed"], ex.Message));
            }
        }

        [RelayCommand]
        private async Task ClearWorksheet(ReoGridControl grid)
        {
            if (!await NotifyConfirmAsync(
                    LanguageService.Instance["geo_free_sheet_clear_confirm"],
                    LanguageService.Instance["Cancel"],
                    LanguageService.Instance["Confirm"]))
                return;

            InitializeWorksheet(grid);
            NotifySuccess(LanguageService.Instance["geo_free_sheet_clear_success"]);
        }

        private void NotifyInfo(string message)
        {
            if (_notificationHost != null)
                _notificationHost.ShowInfo(message);
            else
                MessageHelper.Info(message);
        }

        private void NotifySuccess(string message)
        {
            if (_notificationHost != null)
                _notificationHost.ShowSuccess(message);
            else
                MessageHelper.Success(message);
        }

        private void NotifyWarning(string message)
        {
            if (_notificationHost != null)
                _notificationHost.ShowWarning(message);
            else
                MessageHelper.Warning(message);
        }

        private void NotifyError(string message)
        {
            if (_notificationHost != null)
                _notificationHost.ShowError(message);
            else
                MessageHelper.Error(message);
        }

        private Task<bool> NotifyConfirmAsync(string message, string cancelText, string confirmText)
        {
            if (_notificationHost != null)
                return _notificationHost.ShowConfirmAsync(message, cancelText, confirmText);

            return MessageHelper.ShowAsyncDialog(message, cancelText, confirmText);
        }
    }
}
