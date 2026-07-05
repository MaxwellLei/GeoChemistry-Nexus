using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using unvell.ReoGrid;

namespace GeoChemistryNexus.ViewModels
{
    public sealed class LocalizedOptionItem
    {
        public string Value { get; set; } = string.Empty;
        public string Display { get; set; } = string.Empty;
    }

    public partial class DataPreprocessingPageViewModel : ObservableObject
    {
        private ReoGridControl? _dataGrid;
        private bool _isUpdatingSelectedCellEditor;
        private readonly string[] _templateHeaders =
        {
            "Sample", "SiO2", "TiO2", "Al2O3", "FeOT", "FeO", "Fe2O3", "MnO",
            "MgO", "CaO", "Na2O", "K2O", "P2O5", "LOI", "H2O", "CO2"
        };

        public ObservableCollection<LocalizedOptionItem> IronValenceMethods { get; } = new();

        public ObservableCollection<LocalizedOptionItem> OutlierStrategies { get; } = new();

        public ObservableCollection<LocalizedOptionItem> MissingValueStrategies { get; } = new();

        public ObservableCollection<LocalizedOptionItem> DetectionLimitStrategies { get; } = new();

        public ObservableCollection<string> WorkflowSteps { get; } = new();

        public ObservableCollection<string> OutputFields { get; } = new()
        {
            "Clean_*",
            "Norm_SiO2 ~ Norm_P2O5",
            "Anhydrous_Total",
            "FeO_Est / Fe2O3_Est",
            "Mg_Number",
            "A_CNK / A_NK",
            "Cleaning_Flag / Cleaning_Notes"
        };

        [ObservableProperty]
        private string datasetName = string.Empty;

        [ObservableProperty]
        private bool includeAnhydrousNormalization = true;

        [ObservableProperty]
        private bool includeIronValenceEstimation = true;

        [ObservableProperty]
        private bool includeGeochemicalIndexCalculation = true;

        [ObservableProperty]
        private bool includeDataCleaning = false;

        [ObservableProperty]
        private bool excludeVolatiles = true;

        [ObservableProperty]
        private bool normalizeToHundred = true;

        [ObservableProperty]
        private bool autoBackfillIronOxides = true;

        [ObservableProperty]
        private bool calculateMgNumber = true;

        [ObservableProperty]
        private bool calculateACNK = true;

        [ObservableProperty]
        private bool calculateANK = true;

        [ObservableProperty]
        private bool standardizeBelowDetectionLimitText = false;

        [ObservableProperty]
        private bool createAuditColumns = false;

        [ObservableProperty]
        private bool keepOriginalColumns = true;

        [ObservableProperty]
        private double fe3Fraction = 0.15;

        [ObservableProperty]
        private string selectedIronValenceMethod;

        [ObservableProperty]
        private string selectedOutlierStrategy;

        [ObservableProperty]
        private string selectedMissingValueStrategy;

        [ObservableProperty]
        private string selectedDetectionLimitStrategy;

        [ObservableProperty]
        private string detectionLimitPreview = string.Empty;

        [ObservableProperty]
        private string previewSummary = string.Empty;

        [ObservableProperty]
        private string currentWorksheetName = "Raw_Data";

        [ObservableProperty]
        private int worksheetRowCount;

        [ObservableProperty]
        private int worksheetColumnCount;

        [ObservableProperty]
        private string detectedOxideColumns = string.Empty;

        [ObservableProperty]
        private string selectedCellDisplayText = string.Empty;

        [ObservableProperty]
        private string selectedCellAddress = "--";

        [ObservableProperty]
        private string selectedCellContent = string.Empty;

        [ObservableProperty]
        private bool isSelectedCellEditable;

        public DataPreprocessingPageViewModel()
        {
            RefreshLocalizedOptions();
            RefreshWorkflowSteps();
            SelectedIronValenceMethod = DataPreprocessingOptionCodes.IronValenceAutoEstimate;
            SelectedOutlierStrategy = DataPreprocessingOptionCodes.OutlierMarkOnly;
            SelectedMissingValueStrategy = DataPreprocessingOptionCodes.MissingKeep;
            SelectedDetectionLimitStrategy = DataPreprocessingOptionCodes.DetectionReplaceHalf;
            SelectedCellDisplayText = L("dataPrep_noCellSelected", "No cell selected");
            DetectedOxideColumns = L("dataPrep_notDetected", "Not detected");
            LanguageService.Instance.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == "Item[]")
                {
                    RefreshLocalizedOptions();
                    RefreshWorkflowSteps();
                    RefreshLocalizedStateTexts();
                    UpdatePreviewTexts();
                }
            };
            UpdatePreviewTexts();
        }

        public void InitializeWorksheet(ReoGridControl grid)
        {
            if (grid == null)
            {
                return;
            }

            _dataGrid = grid;

            if (grid.Worksheets.Count == 0)
            {
                grid.Worksheets.Add(grid.CreateWorksheet());
            }

            grid.SetSettings(WorkbookSettings.View_ShowSheetTabControl, false);

            var worksheet = grid.CurrentWorksheet ?? grid.Worksheets[0];
            grid.CurrentWorksheet = worksheet;
            if (string.IsNullOrWhiteSpace(worksheet.Name) || worksheet.Name.StartsWith("Sheet", StringComparison.OrdinalIgnoreCase))
            {
                worksheet.Name = "Data_Table";
            }
            worksheet.Resize(200, _templateHeaders.Length);

            for (int i = 0; i < _templateHeaders.Length; i++)
            {
                worksheet.ColumnHeaders[i].Text = _templateHeaders[i];
                worksheet.SetColumnsWidth(i, 1, i == 0 ? (ushort)120 : (ushort)88);
            }

            var dataStyle = new WorksheetRangeStyle
            {
                Flag = PlainStyleFlag.HorizontalAlign,
                HAlign = ReoGridHorAlign.Center
            };
            worksheet.SetRangeStyles(new RangePosition(0, 0, worksheet.RowCount, _templateHeaders.Length), dataStyle);
            UpdateWorksheetSummary(worksheet);
            UpdateSelectedCellState(worksheet, 0, 0);
        }

        public void UpdateWorksheetSummary(Worksheet worksheet)
        {
            if (worksheet == null)
            {
                return;
            }

            CurrentWorksheetName = worksheet.Name;
            var range = worksheet.UsedRange;
            WorksheetColumnCount = GetEffectiveColumnCount(worksheet, range);
            WorksheetRowCount = range.Rows > 0 ? Math.Max(0, range.EndRow - range.Row + 1) : 0;

            var headers = new List<string>();
            for (int col = 0; col < WorksheetColumnCount; col++)
            {
                string? header = worksheet.ColumnHeaders[col]?.Text;
                if (!string.IsNullOrWhiteSpace(header))
                {
                    headers.Add(header.Trim());
                }
            }

            var oxides = DataPreprocessingService.DetectOxideColumns(headers);
            DetectedOxideColumns = oxides.Count > 0
                ? string.Join("、", oxides)
                : L("dataPrep_noOxideColumnsDetected", "No major oxide columns detected");
            DatasetName = string.Empty;
            UpdatePreviewTexts();
        }

        public void UpdateSelectedCellState(Worksheet worksheet, int? row = null, int? col = null)
        {
            if (worksheet == null)
            {
                SelectedCellDisplayText = L("dataPrep_noCellSelected", "No cell selected");
                SelectedCellAddress = "--";
                IsSelectedCellEditable = false;

                _isUpdatingSelectedCellEditor = true;
                try
                {
                    SelectedCellContent = string.Empty;
                }
                finally
                {
                    _isUpdatingSelectedCellEditor = false;
                }

                return;
            }

            int targetRow = row ?? worksheet.SelectionRange.Row;
            int targetCol = col ?? worksheet.SelectionRange.Col;
            if (targetRow < 0 || targetCol < 0 || targetRow >= worksheet.RowCount || targetCol >= worksheet.ColumnCount)
            {
                SelectedCellDisplayText = L("dataPrep_noCellSelected", "No cell selected");
                SelectedCellAddress = "--";
                IsSelectedCellEditable = false;

                _isUpdatingSelectedCellEditor = true;
                try
                {
                    SelectedCellContent = string.Empty;
                }
                finally
                {
                    _isUpdatingSelectedCellEditor = false;
                }

                return;
            }

            string cellAddress = $"{ColumnIndexToName(targetCol)}{targetRow + 1}";
            string cellValue = worksheet.GetCellData(targetRow, targetCol)?.ToString() ?? string.Empty;

            _isUpdatingSelectedCellEditor = true;
            try
            {
                SelectedCellAddress = cellAddress;
                SelectedCellContent = cellValue;
                IsSelectedCellEditable = true;
            }
            finally
            {
                _isUpdatingSelectedCellEditor = false;
            }

            SelectedCellDisplayText = FormatSelectedCellDisplay(cellAddress, cellValue);
        }

        [RelayCommand]
        private void LoadSampleData(ReoGridControl grid)
        {
            if (grid?.CurrentWorksheet == null)
            {
                return;
            }

            try
            {
                var worksheet = grid.CurrentWorksheet;
                ClearWorksheet(worksheet);
                worksheet.Name = "Example_Data";

                string[][] sampleRows =
                {
                    new[] { "GX-01", "49.82", "1.12", "16.34", "8.65", "7.42", "1.37", "0.14", "7.86", "10.21", "3.12", "1.26", "0.22", "1.75", "0.44", "0.18" },
                    new[] { "GX-02", "52.17", "0.96", "17.48", "7.92", "6.74", "1.31", "0.13", "6.41", "8.94", "3.44", "1.68", "0.19", "1.32", "0.36", "0.12" },
                    new[] { "GX-03", "47.95", "1.36", "15.87", "9.84", "8.53", "1.45", "0.17", "8.92", "11.08", "2.78", "0.94", "0.28", "1.96", "0.51", "0.21" },
                    new[] { "GX-04", "56.48", "0.78", "18.92", "6.14", "5.06", "1.20", "0.09", "4.92", "7.35", "3.85", "2.44", "0.17", "0.98", "0.24", "0.10" },
                    new[] { "GX-05", "61.23", "0.61", "17.65", "4.88", "3.92", "1.07", "0.08", "2.85", "5.64", "4.12", "3.36", "0.14", "0.76", "0.18", "0.07" },
                    new[] { "GX-06", "54.06", "0.88", "18.14", "7.05", "5.91", "1.27", "0.11", "5.48", "8.17", "3.58", "2.06", "0.16", "1.08", "0.27", "0.09" },
                    new[] { "GX-07", "50.41", "1.04", "17.02", "8.24", "7.01", "1.36", "0.12", "6.97", "9.46", "3.05", "1.49", "0.21", "1.54", "0.39", "0.15" },
                    new[] { "GX-08", "58.92", "0.69", "19.11", "5.42", "4.33", "1.21", "0.07", "3.74", "6.18", "4.06", "2.87", "0.15", "0.84", "0.20", "0.08" }
                };

                for (int row = 0; row < sampleRows.Length; row++)
                {
                    for (int col = 0; col < sampleRows[row].Length; col++)
                    {
                        worksheet[row, col] = sampleRows[row][col];
                    }
                }

                UpdateWorksheetSummary(worksheet);
                UpdateSelectedCellState(worksheet, 0, 0);
                MessageHelper.Success(L("dataPrep_sampleDataGenerated", "Sample data generated."));
            }
            catch (Exception ex)
            {
                MessageHelper.Error(L("dataPrep_sampleDataGenerateFailed", "Failed to generate sample data:") + ex.Message);
            }
        }

        [RelayCommand]
        private void ClearTemplateTable(ReoGridControl grid)
        {
            if (grid?.CurrentWorksheet == null)
            {
                return;
            }

            try
            {
                ClearWorksheet(grid.CurrentWorksheet);
                grid.CurrentWorksheet.Name = "Data_Table";
                MessageHelper.Success(L("dataPrep_tableCleared", "Current data table cleared."));
            }
            catch (Exception ex)
            {
                MessageHelper.Error(L("dataPrep_clearTableFailed", "Failed to clear data table:") + ex.Message);
            }
        }

        [RelayCommand]
        private void StartProcessing(ReoGridControl grid)
        {
            if (grid?.CurrentWorksheet == null)
            {
                MessageHelper.Warning(L("dataPrep_noWorksheetToProcess", "There is no worksheet available for processing."));
                return;
            }

            try
            {
                var options = BuildOptions();
                var processed = DataPreprocessingService.ProcessWorksheet(grid.CurrentWorksheet, options);
                WriteProcessedWorksheet(grid.CurrentWorksheet, processed);

                PreviewSummary = processed.RunResult.Summary;
                MessageHelper.Success(L("dataPrep_processingCompleted", "Data preprocessing completed."));
            }
            catch (Exception ex)
            {
                MessageHelper.Error(L("dataPrep_processingFailed", "Processing failed:") + ex.Message);
            }
        }

        [RelayCommand]
        private async Task ExportWorksheet(ReoGridControl grid)
        {
            if (grid?.CurrentWorksheet == null)
            {
                return;
            }

            var worksheet = grid.CurrentWorksheet;
            string? filePath = await FileHelper.GetSaveFilePath2Async(
                title: L("dataPrep_exportTitle", "Export Preprocessing Result"),
                filter: L("dataPrep_exportFilter", "CSV Files|*.csv"),
                defaultExt: ".csv",
                defaultFileName: worksheet.Name);

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            try
            {
                var builder = new StringBuilder();
                int columnCount = GetEffectiveColumnCount(worksheet, worksheet.UsedRange);

                var headerValues = new List<string>();
                for (int col = 0; col < columnCount; col++)
                {
                    string header = worksheet.ColumnHeaders[col]?.Text ?? $"Column_{col + 1}";
                    if (header.Contains(",") || header.Contains("\""))
                    {
                        header = $"\"{header.Replace("\"", "\"\"")}\"";
                    }
                    headerValues.Add(header);
                }
                builder.AppendLine(string.Join(",", headerValues));

                var range = worksheet.UsedRange;

                for (int row = range.Row; row <= range.EndRow; row++)
                {
                    var cells = new List<string>();
                    for (int col = 0; col < columnCount; col++)
                    {
                        string value = worksheet.GetCellText(row, col) ?? string.Empty;
                        if (value.Contains(",") || value.Contains("\""))
                        {
                            value = $"\"{value.Replace("\"", "\"\"")}\"";
                        }
                        cells.Add(value);
                    }
                    builder.AppendLine(string.Join(",", cells));
                }

                await Task.Run(() => File.WriteAllText(filePath, builder.ToString(), new UTF8Encoding(true)));
                MessageHelper.Success(L("dataPrep_exportSucceeded", "Current worksheet exported."));
            }
            catch (Exception ex)
            {
                MessageHelper.Error(L("dataPrep_exportFailed", "Export failed:") + ex.Message);
            }
        }

        [RelayCommand]
        private void ResetOptions()
        {
            IncludeAnhydrousNormalization = true;
            IncludeIronValenceEstimation = true;
            IncludeGeochemicalIndexCalculation = true;
            IncludeDataCleaning = false;
            ExcludeVolatiles = true;
            NormalizeToHundred = true;
            AutoBackfillIronOxides = true;
            CalculateMgNumber = true;
            CalculateACNK = true;
            CalculateANK = true;
            StandardizeBelowDetectionLimitText = false;
            CreateAuditColumns = false;
            KeepOriginalColumns = true;
            Fe3Fraction = 0.15;
            SelectedIronValenceMethod = DataPreprocessingOptionCodes.IronValenceAutoEstimate;
            SelectedOutlierStrategy = DataPreprocessingOptionCodes.OutlierMarkOnly;
            SelectedMissingValueStrategy = DataPreprocessingOptionCodes.MissingKeep;
            SelectedDetectionLimitStrategy = DataPreprocessingOptionCodes.DetectionReplaceHalf;

            UpdatePreviewTexts();
            MessageHelper.Info(L("dataPrep_resetToRecommended", "Restored recommended presets."));
        }

        partial void OnIncludeAnhydrousNormalizationChanged(bool value) => UpdatePreviewTexts();
        partial void OnIncludeIronValenceEstimationChanged(bool value) => UpdatePreviewTexts();
        partial void OnIncludeGeochemicalIndexCalculationChanged(bool value) => UpdatePreviewTexts();
        partial void OnIncludeDataCleaningChanged(bool value) => UpdatePreviewTexts();
        partial void OnSelectedIronValenceMethodChanged(string value) => UpdatePreviewTexts();
        partial void OnSelectedDetectionLimitStrategyChanged(string value) => UpdatePreviewTexts();
        partial void OnSelectedOutlierStrategyChanged(string value) => UpdatePreviewTexts();
        partial void OnSelectedMissingValueStrategyChanged(string value) => UpdatePreviewTexts();
        partial void OnFe3FractionChanged(double value) => UpdatePreviewTexts();
        partial void OnSelectedCellContentChanged(string value)
        {
            if (_isUpdatingSelectedCellEditor)
            {
                return;
            }

            var worksheet = _dataGrid?.CurrentWorksheet;
            if (worksheet == null)
            {
                return;
            }

            int targetRow = worksheet.SelectionRange.Row;
            int targetCol = worksheet.SelectionRange.Col;
            if (targetRow < 0 || targetCol < 0 || targetRow >= worksheet.RowCount || targetCol >= worksheet.ColumnCount)
            {
                return;
            }

            string newValue = value ?? string.Empty;
            string currentValue = worksheet.GetCellData(targetRow, targetCol)?.ToString() ?? string.Empty;
            if (string.Equals(currentValue, newValue, StringComparison.Ordinal))
            {
                return;
            }

            worksheet[targetRow, targetCol] = newValue;
            SelectedCellDisplayText = FormatSelectedCellDisplay(SelectedCellAddress, newValue);
        }

        private DataPreprocessingOptions BuildOptions()
        {
            return new DataPreprocessingOptions
            {
                IncludeAnhydrousNormalization = IncludeAnhydrousNormalization,
                IncludeIronValenceEstimation = IncludeIronValenceEstimation,
                IncludeGeochemicalIndexCalculation = IncludeGeochemicalIndexCalculation,
                IncludeDataCleaning = false,
                ExcludeVolatiles = ExcludeVolatiles,
                NormalizeToHundred = NormalizeToHundred,
                AutoBackfillIronOxides = AutoBackfillIronOxides,
                CalculateMgNumber = CalculateMgNumber,
                CalculateACNK = CalculateACNK,
                CalculateANK = CalculateANK,
                StandardizeBelowDetectionLimitText = false,
                CreateAuditColumns = false,
                KeepOriginalColumns = KeepOriginalColumns,
                IronValenceMethod = SelectedIronValenceMethod,
                OutlierStrategy = SelectedOutlierStrategy,
                MissingValueStrategy = SelectedMissingValueStrategy,
                DetectionLimitStrategy = SelectedDetectionLimitStrategy,
                Fe3Fraction = Fe3Fraction
            };
        }

        private void WriteProcessedWorksheet(Worksheet worksheet, DataPreprocessingWorksheetData processed)
        {
            worksheet.Name = processed.WorksheetName;
            int rowCount = Math.Max(2, processed.Rows.Count);
            int colCount = Math.Max(1, processed.Headers.Count);
            worksheet.Resize(rowCount, colCount);

            for (int col = 0; col < processed.Headers.Count; col++)
            {
                worksheet.ColumnHeaders[col].Text = processed.Headers[col];
                worksheet.SetColumnsWidth(col, 1, processed.Headers[col].Length > 14 ? (ushort)120 : (ushort)92);
            }

            for (int row = 0; row < processed.Rows.Count; row++)
            {
                for (int col = 0; col < processed.Rows[row].Count; col++)
                {
                    worksheet[row, col] = processed.Rows[row][col];
                }
            }

            var dataStyle = new WorksheetRangeStyle
            {
                Flag = PlainStyleFlag.HorizontalAlign,
                HAlign = ReoGridHorAlign.Center
            };
            worksheet.SetRangeStyles(new RangePosition(0, 0, rowCount, colCount), dataStyle);

            UpdateWorksheetSummary(worksheet);
        }

        private void RefreshWorkflowSteps()
        {
            WorkflowSteps.Clear();
            WorkflowSteps.Add(L("dataPrep_workflowStep1", "1. Import a CSV / Excel table and confirm major-element headers"));
            WorkflowSteps.Add(L("dataPrep_workflowStep2", "2. Set detection limits, missing values, outliers, and iron valence strategy"));
            WorkflowSteps.Add(L("dataPrep_workflowStep3", "3. Batch-generate cleaned, normalized, and geochemical indicator columns"));
            WorkflowSteps.Add(L("dataPrep_workflowStep4", "4. Output to a new Processed worksheet and export results"));
        }

        private void UpdatePreviewTexts()
        {
            DetectionLimitPreview = L("dataPrep_detectionLimitPreview", "The current page does not include detection-limit replacement or data-cleaning parameters.");

            var enabledModules = new List<string>();
            if (IncludeAnhydrousNormalization) enabledModules.Add(L("dataPrep_moduleAnhydrous", "Anhydrous normalization"));
            if (IncludeIronValenceEstimation) enabledModules.Add(L("dataPrep_moduleIronValence", "Iron valence"));
            if (IncludeGeochemicalIndexCalculation) enabledModules.Add(L("dataPrep_moduleIndexCalculation", "Index calculation"));
            string outputSummary = string.Join(L("dataPrep_listSeparator", ", "), enabledModules);
            if (string.IsNullOrWhiteSpace(outputSummary))
            {
                outputSummary = L("dataPrep_keepOriginalOnly", "Keep original worksheet only");
            }

            PreviewSummary = string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                L("dataPrep_previewSummary", "Detected oxide columns: {0}; enabled modules: {1}; iron valence method: \"{2}\"; Fe3+/Fe empirical ratio: {3:0.00}."),
                DetectedOxideColumns,
                outputSummary,
                GetIronValenceMethodDisplay(SelectedIronValenceMethod),
                Fe3Fraction);
        }

        private void PromoteFirstRowToColumnHeaders(Worksheet worksheet)
        {
            if (worksheet == null)
            {
                return;
            }

            var range = worksheet.UsedRange;
            if (range.Rows <= 0 || range.Cols <= 0)
            {
                return;
            }

            int columnCount = Math.Max(range.EndCol + 1, _templateHeaders.Length);
            int rowCount = Math.Max(0, range.EndRow - range.Row + 1);

            var headers = new List<string>();
            for (int col = 0; col < columnCount; col++)
            {
                string header = worksheet.GetCellText(range.Row, col);
                if (string.IsNullOrWhiteSpace(header))
                {
                    header = col < _templateHeaders.Length ? _templateHeaders[col] : $"Column_{col + 1}";
                }
                headers.Add(header.Trim());
            }

            var dataRows = new List<List<string>>();
            for (int row = range.Row + 1; row <= range.EndRow; row++)
            {
                var values = new List<string>();
                for (int col = 0; col < columnCount; col++)
                {
                    values.Add(worksheet.GetCellText(row, col) ?? worksheet.GetCellData(row, col)?.ToString() ?? string.Empty);
                }
                dataRows.Add(values);
            }

            worksheet.Resize(Math.Max(200, dataRows.Count + 20), columnCount);
            ClearWorksheetCellsOnly(worksheet);

            for (int col = 0; col < columnCount; col++)
            {
                worksheet.ColumnHeaders[col].Text = headers[col];
                worksheet.SetColumnsWidth(col, 1, col == 0 ? (ushort)120 : (ushort)88);
            }

            for (int row = 0; row < dataRows.Count; row++)
            {
                for (int col = 0; col < dataRows[row].Count; col++)
                {
                    worksheet[row, col] = dataRows[row][col];
                }
            }
        }

        private void ClearWorksheet(Worksheet worksheet)
        {
            worksheet.Resize(200, _templateHeaders.Length);
            ClearWorksheetCellsOnly(worksheet);

            for (int i = 0; i < _templateHeaders.Length; i++)
            {
                worksheet.ColumnHeaders[i].Text = _templateHeaders[i];
                worksheet.SetColumnsWidth(i, 1, i == 0 ? (ushort)120 : (ushort)88);
            }

            var dataStyle = new WorksheetRangeStyle
            {
                Flag = PlainStyleFlag.HorizontalAlign,
                HAlign = ReoGridHorAlign.Center
            };
            worksheet.SetRangeStyles(new RangePosition(0, 0, worksheet.RowCount, _templateHeaders.Length), dataStyle);
            UpdateWorksheetSummary(worksheet);
            UpdateSelectedCellState(worksheet, 0, 0);
        }

        private void ClearWorksheetCellsOnly(Worksheet worksheet)
        {
            for (int row = 0; row < worksheet.RowCount; row++)
            {
                for (int col = 0; col < worksheet.ColumnCount; col++)
                {
                    worksheet[row, col] = null;
                }
            }
        }

        private int GetEffectiveColumnCount(Worksheet worksheet, RangePosition range)
        {
            int lastHeaderIndex = -1;
            for (int col = 0; col < worksheet.ColumnCount; col++)
            {
                string? header = worksheet.ColumnHeaders[col]?.Text;
                if (!string.IsNullOrWhiteSpace(header))
                {
                    lastHeaderIndex = col;
                }
            }

            int usedColumns = range.Cols > 0 ? range.EndCol + 1 : 0;
            return Math.Max(lastHeaderIndex + 1, usedColumns);
        }

        private void RefreshLocalizedOptions()
        {
            RebuildOptions(
                IronValenceMethods,
                (DataPreprocessingOptionCodes.IronValenceAutoEstimate, L("dataPrep_ironMethodAutoEstimate", "Auto estimate from total iron")),
                (DataPreprocessingOptionCodes.IronValenceBackCalculate, L("dataPrep_ironMethodBackCalculate", "Back-calculate from FeO / Fe2O3")),
                (DataPreprocessingOptionCodes.IronValenceEmpiricalRatio, L("dataPrep_ironMethodEmpiricalRatio", "Empirical ratio correction")));

            RebuildOptions(
                OutlierStrategies,
                (DataPreprocessingOptionCodes.OutlierMarkOnly, L("dataPrep_outlierMarkOnly", "Mark outliers only")),
                (DataPreprocessingOptionCodes.OutlierIqr, L("dataPrep_outlierIqr", "IQR method")),
                (DataPreprocessingOptionCodes.OutlierThreeSigma, L("dataPrep_outlierThreeSigma", "3-sigma method")));

            RebuildOptions(
                MissingValueStrategies,
                (DataPreprocessingOptionCodes.MissingKeep, L("dataPrep_missingKeep", "Keep missing values")),
                (DataPreprocessingOptionCodes.MissingMean, L("dataPrep_missingMean", "Mean imputation")),
                (DataPreprocessingOptionCodes.MissingMedian, L("dataPrep_missingMedian", "Median imputation")));

            RebuildOptions(
                DetectionLimitStrategies,
                (DataPreprocessingOptionCodes.DetectionReplaceZero, L("dataPrep_detectionReplaceZero", "Replace with 0")),
                (DataPreprocessingOptionCodes.DetectionReplaceHalf, L("dataPrep_detectionReplaceHalf", "Replace with half of detection limit")),
                (DataPreprocessingOptionCodes.DetectionReplaceNull, L("dataPrep_detectionReplaceNull", "Replace with null")));
        }

        private void RefreshLocalizedStateTexts()
        {
            var worksheet = _dataGrid?.CurrentWorksheet;
            if (worksheet == null)
            {
                DetectedOxideColumns = L("dataPrep_notDetected", "Not detected");
                SelectedCellDisplayText = L("dataPrep_noCellSelected", "No cell selected");
                return;
            }

            UpdateWorksheetSummary(worksheet);
            UpdateSelectedCellState(worksheet);
        }

        private void RebuildOptions(ObservableCollection<LocalizedOptionItem> target, params (string Value, string Display)[] items)
        {
            target.Clear();
            foreach (var item in items)
            {
                target.Add(new LocalizedOptionItem
                {
                    Value = item.Value,
                    Display = item.Display
                });
            }
        }

        private string GetIronValenceMethodDisplay(string value)
        {
            return IronValenceMethods.FirstOrDefault(x => x.Value == value)?.Display ?? value ?? string.Empty;
        }

        private string FormatSelectedCellDisplay(string cellAddress, string cellValue)
        {
            return string.IsNullOrWhiteSpace(cellValue)
                ? string.Format(System.Globalization.CultureInfo.CurrentCulture, L("dataPrep_selectedCellEmpty", "{0}: <empty>"), cellAddress)
                : string.Format(System.Globalization.CultureInfo.CurrentCulture, L("dataPrep_selectedCellValue", "{0}: {1}"), cellAddress, cellValue);
        }

        private string L(string key, string fallback)
        {
            return LanguageService.Instance[key] ?? fallback;
        }

        private static string ColumnIndexToName(int columnIndex)
        {
            if (columnIndex < 0)
            {
                return "?";
            }

            string columnName = string.Empty;
            int currentIndex = columnIndex;
            do
            {
                columnName = (char)('A' + (currentIndex % 26)) + columnName;
                currentIndex = (currentIndex / 26) - 1;
            }
            while (currentIndex >= 0);

            return columnName;
        }
    }
}
