using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models.Niggli;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    /// <summary>
    /// Niggli 标准分子矿物计算页面 ViewModel
    /// <para>
    /// 同时实现公共表格控件的两个可选接口：
    /// <see cref="IWorksheetRowStatusProvider"/>（行头状态点）与 <see cref="IWorksheetColumnHeaderProvider"/>（列头分组行 / 单位）。
    /// </para>
    /// </summary>
    public partial class NiggliPageViewModel : ObservableObject, IWorksheetRowStatusProvider, IWorksheetColumnHeaderProvider
    {
        /// <summary>
        /// Fe3+/Fe总比值 (默认 0.15)
        /// </summary>
        [ObservableProperty]
        private double _fe3Fraction = NiggliConstants.DefaultFe3Ratio;

        /// <summary>
        /// 当前选中行的诊断详情项
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<NiggliDiagnosticItem> _diagnosticItems = new();

        /// <summary>
        /// 是否有诊断数据
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ExportDiagnosticDetailsCommand))]
        [NotifyCanExecuteChangedFor(nameof(CopyDiagnosticDetailsCommand))]
        private bool _hasDiagnosticData;

        /// <summary>
        /// 选中行样品信息
        /// </summary>
        [ObservableProperty]
        private string _selectedRowInfo = string.Empty;

        /// <summary>
        /// 选中行硅饱和状态
        /// </summary>
        [ObservableProperty]
        private string _selectedSilicaState = string.Empty;

        /// <summary>
        /// 选中行铝饱和状态
        /// </summary>
        [ObservableProperty]
        private string _selectedAluminaState = string.Empty;

        /// <summary>
        /// 硅饱和度是否为"不饱和"（用于徽章高亮）
        /// </summary>
        [ObservableProperty]
        private bool _isSilicaUndersaturated;

        /// <summary>
        /// 右侧检查器（计算详情）是否展开；与表格控件的 IsInspectorOpen 双向绑定，默认收起。
        /// </summary>
        [ObservableProperty]
        private bool _isInspectorOpen = false;

        /// <summary>导出当前检查器显示的计算详情为 CSV。</summary>
        [RelayCommand(CanExecute = nameof(HasDiagnosticData))]
        private Task ExportDiagnosticDetailsAsync() =>
            InspectorDetailsExporter.ExportCsvAsync(GetDiagnosticRows(), SelectedRowInfo);

        /// <summary>复制当前检查器显示的计算详情到剪贴板。</summary>
        [RelayCommand(CanExecute = nameof(HasDiagnosticData))]
        private void CopyDiagnosticDetails() =>
            InspectorDetailsExporter.CopyToClipboard(GetDiagnosticRows());

        private List<(string Name, string Value)> GetDiagnosticRows() =>
            DiagnosticItems.Select(item => (item.Name, item.Value)).ToList();

        public void ClampFe3Fraction()
        {
            if (Fe3Fraction < 0.0) Fe3Fraction = 0.0;
            else if (Fe3Fraction > 1.0) Fe3Fraction = 1.0;
        }

        private const int InitialWorksheetRowCount = 500;
        private const int SampleColumnIndex = 0;
        private const int InputStartColumn = 1;

        private static readonly string[] InputColumns = NiggliConstants.InputOxides;
        private static readonly string[] NiggliValueColumns = NiggliConstants.NiggliValueColumns;
        private static readonly string[] DiagnosticColumns =
        {
            "niggli_col_silica_saturation", "niggli_col_alumina_state", "niggli_col_cation_sum"
        };
        private static readonly string[] ResultMineralOrder =
        {
            "Q", "C", "Or", "Plag", "Ab", "An", "Lc", "Ne", "Kp", "Ac", "Ns", "Ks",
            "Di", "Hy", "Ol", "Cs", "Wo", "Mt", "Hm", "Il", "Tn", "Pf", "Ru",
            "Ap", "Fr", "Py", "Cc", "Sum"
        };

        private readonly Dictionary<int, NiggliResult> _rowResults = new();

        /// <summary>最近一次计算失败的行（用于行头状态点）。</summary>
        private readonly HashSet<int> _failedRows = new();

        /// <summary>计算之后输入被修改、结果已过期的行（用于行头状态点）。</summary>
        private readonly HashSet<int> _staleRows = new();

        /// <summary>氧化物输入列中属于“主量元素”的个数（SiO2 … K2O），其后为其他氧化物 / 挥发分。</summary>
        private static readonly int MajorOxideCount = Array.IndexOf(InputColumns, "K2O") + 1;

        private ReoGridControl? _grid;

        public event EventHandler? RowStatusChanged;
        public event EventHandler? ColumnHeaderInfoChanged;

        public NiggliPageViewModel()
        {
            LanguageService.Instance.PropertyChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Item[]" || _grid?.CurrentWorksheet == null)
                return;

            RefreshLocalizedHeaders(_grid.CurrentWorksheet);
            // 分组名是本地化文字，切换语言后需要重新计算列头布局。
            ColumnHeaderInfoChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 初始化 ReoGrid 工作表
        /// </summary>
        public void InitializeWorksheet(ReoGridControl grid)
        {
            _grid = grid;
            ReoGridImeHelper.Attach(grid);
            ReoGridDoubleClickFillHelper.Attach(grid);

            var sheet = grid.CurrentWorksheet;
            grid.SetSettings(unvell.ReoGrid.WorkbookSettings.View_ShowSheetTabControl, false);

            int totalCols = 1 + InputColumns.Length + 1 + NiggliValueColumns.Length +
                            DiagnosticColumns.Length + ResultMineralOrder.Length;
            sheet.Resize(InitialWorksheetRowCount, totalCols);

            // 统一表格外观（与 EPMA 计算表格一致）：数据区默认字体/行高
            ReoGridTableAppearance.ApplyCellDefaults(sheet);

            int separatorCol = InputStartColumn + InputColumns.Length;

            // 使用 ReoGrid 原生列头作为表头：输入列 / 分隔列 / 结果列分别套用对应的列头样式
            sheet.SetSettings(WorksheetSettings.View_ShowColumnHeader, true);
            for (int c = 0; c < totalCols; c++)
            {
                ReoGridTableAppearance.SetColumnHeader(grid, c, kind: GetColumnKind(c, separatorCol));
            }
            ReoGridTableAppearance.ConfigureColumnHeaderHeight(sheet);

            // 输入列标题
            for (int c = 0; c < InputColumns.Length; c++)
            {
                int col = InputStartColumn + c;
                sheet.ColumnHeaders[col].Text = InputColumns[c];
                sheet.SetColumnsWidth(col, 1, 70);
            }

            // 分隔列
            sheet.ColumnHeaders[separatorCol].Text = string.Empty;
            sheet.SetColumnsWidth(separatorCol, 1, 20);

            // 分隔列、结果列底色
            ReoGridTableAppearance.StyleBodyRange(sheet,
                new RangePosition(0, separatorCol, InitialWorksheetRowCount, 1), ReoGridColumnKind.Separator);
            ReoGridTableAppearance.StyleBodyRange(sheet,
                new RangePosition(0, separatorCol + 1, InitialWorksheetRowCount, totalCols - separatorCol - 1), ReoGridColumnKind.Output);

            // 数据区域居中对齐
            var dataStyle = new WorksheetRangeStyle
            {
                Flag = PlainStyleFlag.HorizontalAlign,
                HAlign = ReoGridHorAlign.Center
            };
            sheet.SetRangeStyles(new RangePosition(0, 0, InitialWorksheetRowCount, totalCols), dataStyle);

            // 冻结第 1 列（样品号），向右滚动时始终可见；只冻结这一列，不冻结行
            sheet.FreezeToCell(0, 1);

            RefreshLocalizedHeaders(sheet);

            // 可选的表格外观扩展：列头分组行 / 单位、行头状态点
            ReoGridTableAppearance.SetColumnHeaderProvider(grid, this);
            ReoGridTableAppearance.SetRowStatusProvider(grid, this);
            sheet.CellDataChanged += OnWorksheetCellDataChanged;
        }

        /// <summary>
        /// 输入单元格被修改后，已有结果的行标记为“过期”；新录入数据的行也需要刷新状态点。
        /// 计算本身写入的是结果列，不在输入列范围内，不会触发这里。
        /// </summary>
        private void OnWorksheetCellDataChanged(object? sender, CellEventArgs e)
        {
            int separatorCol = InputStartColumn + InputColumns.Length;
            int column = e.Cell.Column;
            if (column < InputStartColumn || column >= separatorCol) return;

            int row = e.Cell.Row;
            if (_rowResults.ContainsKey(row) || _failedRows.Contains(row)) _staleRows.Add(row);
            RowStatusChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>该行输入氧化物区域是否有任何内容。</summary>
        private bool HasInputData(Worksheet sheet, int row)
        {
            for (int c = 0; c < InputColumns.Length; c++)
            {
                var value = sheet[row, InputStartColumn + c];
                if (value != null && !(value is string text && string.IsNullOrWhiteSpace(text))) return true;
            }
            return false;
        }

        #region 公共表格控件接口：行头状态点

        public WorksheetRowStatus GetRowStatus(int row)
        {
            var sheet = _grid?.CurrentWorksheet;
            if (sheet == null || row < 0 || row >= sheet.RowCount) return WorksheetRowStatus.None;

            if (_rowResults.TryGetValue(row, out var result))
            {
                if (_staleRows.Contains(row)) return WorksheetRowStatus.Stale;
                return result.Warnings.Count > 0 ? WorksheetRowStatus.Warning : WorksheetRowStatus.Ok;
            }

            if (!HasInputData(sheet, row)) return WorksheetRowStatus.None;
            if (_staleRows.Contains(row)) return WorksheetRowStatus.Stale;
            return _failedRows.Contains(row) ? WorksheetRowStatus.Error : WorksheetRowStatus.NotCalculated;
        }

        #endregion

        #region 公共表格控件接口：列头分组行 / 单位

        public string? GetGroup(int column)
        {
            int separatorCol = InputStartColumn + InputColumns.Length;
            int diagStartCol = separatorCol + 1 + NiggliValueColumns.Length;
            if (column < InputStartColumn || column == separatorCol) return null;   // 样品列、分隔列不属于任何分组

            if (column < separatorCol)
            {
                return column - InputStartColumn < MajorOxideCount
                    ? LanguageService.GetString("sheet_group_major_elements", "Major elements")
                    : LanguageService.GetString("sheet_group_other_oxides", "Other oxides / volatiles");
            }

            if (column < diagStartCol) return LanguageService.GetString("niggli_group_values", "Niggli values");
            return column < diagStartCol + DiagnosticColumns.Length
                ? LanguageService.GetString("sheet_group_diagnostics", "Diagnostics")
                : LanguageService.GetString("niggli_group_minerals", "Normative minerals");
        }

        public string? GetUnit(int column)
        {
            int separatorCol = InputStartColumn + InputColumns.Length;
            int mineralStartCol = separatorCol + 1 + NiggliValueColumns.Length + DiagnosticColumns.Length;
            // 氧化物为质量百分比，标准矿物为阳离子百分比；样品列、尼格里参数（无量纲）和诊断列没有单位。
            if (column >= InputStartColumn && column < separatorCol) return "wt%";
            if (column >= mineralStartCol) return "cat%";
            return null;
        }

        #endregion

        private void RefreshLocalizedHeaders(Worksheet sheet)
        {
            sheet.Name = LanguageService.Instance["niggli_sheet_name"] ?? "Niggli";

            int col = SampleColumnIndex;
            var sampleHeader = LanguageService.Instance["niggli_col_sample"] ?? "Sample";
            sheet.ColumnHeaders[col].Text = sampleHeader;
            SetColumnWidthForHeader(sheet, col, sampleHeader, minWidth: 90);

            // 跳过输入列和分隔列
            col = InputStartColumn + InputColumns.Length + 1;

            // 尼格里参数列标题
            foreach (var niggliVal in NiggliValueColumns)
            {
                var headerText = niggliVal;
                sheet.ColumnHeaders[col].Text = headerText;
                SetColumnWidthForHeader(sheet, col, headerText, minWidth: 65);
                col++;
            }

            // 诊断列标题
            foreach (var diag in DiagnosticColumns)
            {
                var headerText = LanguageService.Instance[diag] ?? diag;
                sheet.ColumnHeaders[col].Text = headerText;
                SetColumnWidthForHeader(sheet, col, headerText, minWidth: 100);
                col++;
            }

            // 矿物列标题
            foreach (var mineral in ResultMineralOrder)
            {
                var headerText = FormatMineralHeader(mineral);
                sheet.ColumnHeaders[col].Text = headerText;
                SetColumnWidthForHeader(sheet, col, headerText, minWidth: 75);
                col++;
            }

            // 列头文字在绘制时读取，这里只需触发重绘
            sheet.RequestInvalidate();
        }

        /// <summary>输入列（样品号 + 氧化物）为输入样式，分隔列之后的尼格里值/诊断/矿物列为结果样式。</summary>
        private static ReoGridColumnKind GetColumnKind(int column, int separatorCol)
        {
            if (column < separatorCol) return ReoGridColumnKind.Input;
            return column == separatorCol ? ReoGridColumnKind.Separator : ReoGridColumnKind.Output;
        }

        [RelayCommand]
        private void Calculate(ReoGridControl grid)
        {
            if (grid == null) return;

            ClampFe3Fraction();
            var sheet = grid.CurrentWorksheet;
            _rowResults.Clear();
            _failedRows.Clear();
            _staleRows.Clear();

            int successCount = 0;
            int totalCount = 0;

            int separatorCol = InputStartColumn + InputColumns.Length;
            int niggliValStartCol = separatorCol + 1;
            int diagStartCol = niggliValStartCol + NiggliValueColumns.Length;
            int mineralStartCol = diagStartCol + DiagnosticColumns.Length;

            // 清除旧计算结果
            for (int row = 0; row < sheet.RowCount; row++)
            {
                for (int c = niggliValStartCol; c < sheet.ColumnCount; c++)
                {
                    sheet[row, c] = null;
                }
            }

            // 逐行执行计算
            for (int row = 0; row < sheet.RowCount; row++)
            {
                var oxides = new Dictionary<string, double>();
                bool hasData = false;

                for (int c = 0; c < InputColumns.Length; c++)
                {
                    var cellValue = sheet[row, InputStartColumn + c];
                    if (InvariantNumberParser.TryParseCell(cellValue, out double value) && value > 0)
                    {
                        oxides[InputColumns[c]] = value;
                        hasData = true;
                    }
                }

                if (!hasData) continue;
                totalCount++;

                var result = NiggliCalculator.Calculate(oxides, Fe3Fraction);
                if (result.Success)
                {
                    successCount++;
                    _rowResults[row] = result;

                    // 1. 写入尼格里参数列 (si, al, fm, c, alk, k, mg, qz)
                    int nc = niggliValStartCol;
                    var pDict = result.Parameters.ToDictionary();
                    foreach (var nv in NiggliValueColumns)
                    {
                        sheet[row, nc++] = pDict.TryGetValue(nv, out double val) ? val : 0.0;
                    }

                    // 2. 写入诊断列 (硅饱和状态, 铝饱和状态, 矿物总和)
                    int dc = diagStartCol;
                    sheet[row, dc++] = TranslateSilicaSaturation(result.SilicaSaturation);
                    sheet[row, dc++] = TranslateAluminaState(result.AluminaState);
                    sheet[row, dc] = InvariantNumberParser.Format(result.MineralSum, "F2");

                    // 3. 写入标准矿物列 (阳离子百分比)
                    for (int m = 0; m < ResultMineralOrder.Length; m++)
                    {
                        string mineral = ResultMineralOrder[m];
                        if (mineral == "Sum")
                        {
                            sheet[row, mineralStartCol + m] = result.MineralSum;
                        }
                        else if (result.Minerals.TryGetValue(mineral, out double pct) && pct > 0.001)
                        {
                            sheet[row, mineralStartCol + m] = Math.Round(pct, 3);
                        }
                    }
                }
                else
                {
                    _failedRows.Add(row);
                    sheet[row, diagStartCol] = LanguageService.Instance["niggli_error"] ?? "Error";
                    string err = !string.IsNullOrEmpty(result.ErrorKey)
                        ? (LanguageService.Instance[result.ErrorKey] ?? result.ErrorMessage)
                        : result.ErrorMessage;
                    sheet[row, diagStartCol + 1] = err;
                }
            }

            // 结果已更新，刷新行头状态点
            RowStatusChanged?.Invoke(this, EventArgs.Empty);

            if (totalCount == 0)
            {
                MessageHelper.Warning(LanguageService.Instance["niggli_msg_no_data"] ?? "No valid data");
            }
            else if (successCount == totalCount)
            {
                var template = LanguageService.Instance["niggli_msg_calc_all_success"] ?? "All {0} samples succeeded.";
                MessageHelper.Success(string.Format(template, successCount));
            }
            else
            {
                var template = LanguageService.Instance["niggli_msg_calc_partial_success"] ?? "{0}/{1} samples succeeded.";
                MessageHelper.Info(string.Format(template, successCount, totalCount));
            }
        }

        [RelayCommand]
        private async Task ExportCsv(ReoGridControl grid)
        {
            if (grid == null) return;

            string? filePath = await FileHelper.GetSaveFilePath2Async(
                title: LanguageService.Instance["niggli_export_dialog_title"] ?? "Export Niggli Results",
                filter: LanguageService.Instance["niggli_csv_filter"] ?? "CSV File|*.csv",
                defaultExt: ".csv",
                defaultFileName: "Niggli_Results.csv");

            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                var sheet = grid.CurrentWorksheet;
                var sb = new StringBuilder();

                var headers = new List<string>();
                int separatorCol = InputStartColumn + InputColumns.Length;
                for (int c = 0; c < sheet.ColumnCount; c++)
                {
                    var headerVal = c == separatorCol ? "---" : sheet.ColumnHeaders[c].Text ?? "";
                    headers.Add(ReoGridFormatHelper.EscapeCsvCell(headerVal));
                }
                sb.AppendLine(string.Join(",", headers));

                for (int row = 0; row < sheet.RowCount; row++)
                {
                    bool hasData = false;
                    var rowData = new List<string>();
                    for (int c = 0; c < sheet.ColumnCount; c++)
                    {
                        var val = InvariantNumberParser.ToText(sheet[row, c]);
                        if (!string.IsNullOrEmpty(val)) hasData = true;
                        rowData.Add(ReoGridFormatHelper.EscapeCsvCell(val));
                    }
                    if (hasData)
                        sb.AppendLine(string.Join(",", rowData));
                }

                await Task.Run(() => File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8));
                var template = LanguageService.Instance["niggli_msg_export_success"] ?? "Results exported to: {0}";
                MessageHelper.ExportSuccess(string.Format(template, Path.GetFileName(filePath)), filePath);
            }
            catch (Exception ex)
            {
                var template = LanguageService.Instance["niggli_msg_export_failed"] ?? "Export failed: {0}";
                MessageHelper.Error(string.Format(template, ex.Message));
            }
        }

        [RelayCommand]
        private async Task ClearData(ReoGridControl grid)
        {
            if (grid == null) return;

            var confirmed = await MessageHelper.ShowAsyncDialog(
                LanguageService.Instance["niggli_confirm_clear_msg"] ?? "Clear all input and results?",
                LanguageService.Instance["niggli_btn_cancel"] ?? "Cancel",
                LanguageService.Instance["niggli_btn_confirm_clear"] ?? "Confirm Clear");
            if (!confirmed) return;

            var sheet = grid.CurrentWorksheet ?? (grid.Worksheets.Count > 0 ? grid.Worksheets[0] : null);
            if (sheet == null) return;

            for (int row = 0; row < sheet.RowCount; row++)
            {
                for (int c = 0; c < sheet.ColumnCount; c++)
                {
                    sheet[row, c] = null;
                }
            }

            _rowResults.Clear();
            _failedRows.Clear();
            _staleRows.Clear();
            RowStatusChanged?.Invoke(this, EventArgs.Empty);
            HasDiagnosticData = false;
            DiagnosticItems.Clear();
            SelectedRowInfo = string.Empty;
            SelectedSilicaState = string.Empty;
            SelectedAluminaState = string.Empty;
            IsSilicaUndersaturated = false;

            MessageHelper.Success(LanguageService.Instance["niggli_msg_data_cleared"] ?? "Data cleared.");
        }

        [RelayCommand]
        private void FillExample(ReoGridControl grid)
        {
            if (grid == null) return;
            var sheet = grid.CurrentWorksheet;

            // 示例 1: 花岗岩 Granite (硅过饱和，Qtz > 0)
            var granite = new Dictionary<string, double>
            {
                ["SiO2"] = 72.04, ["TiO2"] = 0.30, ["Al2O3"] = 14.42,
                ["Fe2O3"] = 1.12, ["FeO"] = 1.68, ["MnO"] = 0.05,
                ["MgO"] = 0.52, ["CaO"] = 1.82, ["Na2O"] = 3.69,
                ["K2O"] = 4.12, ["P2O5"] = 0.10
            };

            // 示例 2: 玄武岩 Basalt (硅饱和，含正辉石/橄榄石)
            var basalt = new Dictionary<string, double>
            {
                ["SiO2"] = 49.20, ["TiO2"] = 1.84, ["Al2O3"] = 15.74,
                ["Fe2O3"] = 3.79, ["FeO"] = 7.13, ["MnO"] = 0.20,
                ["MgO"] = 6.73, ["CaO"] = 9.47, ["Na2O"] = 2.91,
                ["K2O"] = 1.10, ["P2O5"] = 0.35
            };

            // 示例 3: 响岩 / 霞石正长岩 Phonolite (硅强不饱和，含霞石 Ne 与白榴石 Lc)
            var phonolite = new Dictionary<string, double>
            {
                ["SiO2"] = 56.40, ["TiO2"] = 0.55, ["Al2O3"] = 19.80,
                ["Fe2O3"] = 2.45, ["FeO"] = 1.80, ["MnO"] = 0.12,
                ["MgO"] = 0.65, ["CaO"] = 1.95, ["Na2O"] = 8.85,
                ["K2O"] = 5.25, ["P2O5"] = 0.15
            };

            var samples = new[] { granite, basalt, phonolite };
            var names = new[] { "Granite", "Basalt", "Phonolite" };

            for (int i = 0; i < samples.Length; i++)
            {
                int row = i;
                sheet[row, SampleColumnIndex] = names[i];
                for (int c = 0; c < InputColumns.Length; c++)
                {
                    if (samples[i].TryGetValue(InputColumns[c], out double val))
                    {
                        sheet[row, InputStartColumn + c] = val;
                    }
                }
            }
        }

        public void OnRowSelected(Worksheet sheet, int row)
        {
            DiagnosticItems.Clear();

            if (!_rowResults.TryGetValue(row, out var result))
            {
                HasDiagnosticData = false;
                SelectedRowInfo = string.Empty;
                SelectedSilicaState = string.Empty;
                SelectedAluminaState = string.Empty;
                IsSilicaUndersaturated = false;
                return;
            }

            HasDiagnosticData = true;
            var sampleName = sheet[row, SampleColumnIndex]?.ToString()?.Trim();
            SelectedRowInfo = !string.IsNullOrEmpty(sampleName)
                ? sampleName
                : string.Format(LanguageService.Instance["niggli_row_format"] ?? "Row {0}", row + 1);

            SelectedSilicaState = TranslateSilicaSaturation(result.SilicaSaturation);
            SelectedAluminaState = TranslateAluminaState(result.AluminaState);
            IsSilicaUndersaturated = result.SilicaSaturation == "undersaturated";

            // 1. 岩石地球化学分类与状态 (Classification & Saturation)
            DiagnosticItems.Add(new NiggliDiagnosticItem
            {
                Name = LanguageService.Instance["niggli_diag_section_classification"] ?? "── Rock Classification & Saturation ──",
                Value = "",
                IsSeparator = true
            });

            DiagnosticItems.Add(new NiggliDiagnosticItem
            {
                Name = LanguageService.Instance["niggli_col_silica_saturation"] ?? "Silica Saturation",
                Value = TranslateSilicaSaturation(result.SilicaSaturation),
                IsHighlight = result.SilicaSaturation == "undersaturated"
            });

            DiagnosticItems.Add(new NiggliDiagnosticItem
            {
                Name = LanguageService.Instance["niggli_col_alumina_state"] ?? "Alumina Saturation",
                Value = TranslateAluminaState(result.AluminaState),
                IsHighlight = result.AluminaState == "peralkaline"
            });

            DiagnosticItems.Add(new NiggliDiagnosticItem
            {
                Name = LanguageService.Instance["niggli_diag_total_minerals"] ?? "Total Minerals",
                Value = $"{result.MineralSum:F2} %",
                IsHighlight = false
            });

            // 2. 经典尼格里参数 (Niggli Values - 一个一项)
            DiagnosticItems.Add(new NiggliDiagnosticItem
            {
                Name = LanguageService.Instance["niggli_diag_section_parameters"] ?? "── Niggli Values ──",
                Value = "",
                IsSeparator = true
            });

            DiagnosticItems.Add(new NiggliDiagnosticItem
            {
                Name = LanguageService.Instance["niggli_param_si"] ?? "si (Silica index)",
                Value = result.Parameters.Si.ToString("F2")
            });

            DiagnosticItems.Add(new NiggliDiagnosticItem
            {
                Name = LanguageService.Instance["niggli_param_al"] ?? "al (Alumina index)",
                Value = result.Parameters.Al.ToString("F2")
            });

            DiagnosticItems.Add(new NiggliDiagnosticItem
            {
                Name = LanguageService.Instance["niggli_param_fm"] ?? "fm (Iron-magnesium index)",
                Value = result.Parameters.Fm.ToString("F2")
            });

            DiagnosticItems.Add(new NiggliDiagnosticItem
            {
                Name = LanguageService.Instance["niggli_param_c"] ?? "c (Calcium index)",
                Value = result.Parameters.C.ToString("F2")
            });

            DiagnosticItems.Add(new NiggliDiagnosticItem
            {
                Name = LanguageService.Instance["niggli_param_alk"] ?? "alk (Alkali index)",
                Value = result.Parameters.Alk.ToString("F2")
            });

            DiagnosticItems.Add(new NiggliDiagnosticItem
            {
                Name = LanguageService.Instance["niggli_param_ti"] ?? "ti (Titanium index)",
                Value = result.Parameters.Ti.ToString("F2")
            });

            DiagnosticItems.Add(new NiggliDiagnosticItem
            {
                Name = LanguageService.Instance["niggli_param_p"] ?? "p (Phosphorus index)",
                Value = result.Parameters.P.ToString("F2")
            });

            DiagnosticItems.Add(new NiggliDiagnosticItem
            {
                Name = LanguageService.Instance["niggli_param_k"] ?? "k (Potassium ratio)",
                Value = result.Parameters.K.ToString("F3")
            });

            DiagnosticItems.Add(new NiggliDiagnosticItem
            {
                Name = LanguageService.Instance["niggli_param_mg"] ?? "mg (Magnesium ratio)",
                Value = result.Parameters.Mg.ToString("F3")
            });

            DiagnosticItems.Add(new NiggliDiagnosticItem
            {
                Name = LanguageService.Instance["niggli_param_qz"] ?? "qz (Quartz index)",
                Value = (result.Parameters.Qz > 0 ? "+" : "") + result.Parameters.Qz.ToString("F2"),
                IsHighlight = Math.Abs(result.Parameters.Qz) > 0.01
            });

            // 3. 矿物端元成分比 (Endmembers)
            DiagnosticItems.Add(new NiggliDiagnosticItem
            {
                Name = LanguageService.Instance["niggli_diag_section_endmembers"] ?? "── Mineral Endmembers ──",
                Value = "",
                IsSeparator = true
            });

            if (result.Endmembers.AnorthitePercent > 0.001)
            {
                DiagnosticItems.Add(new NiggliDiagnosticItem
                {
                    Name = LanguageService.Instance["niggli_diag_plag_an"] ?? "Plagioclase Endmember (An%)",
                    Value = $"An {result.Endmembers.AnorthitePercent:F1} %"
                });
            }

            if (result.Endmembers.WollastonitePercent > 0.001)
            {
                DiagnosticItems.Add(new NiggliDiagnosticItem
                {
                    Name = LanguageService.Instance["niggli_diag_diopside_ratio"] ?? "Diopside Endmembers (Wo-En-Fs)",
                    Value = $"Wo: {result.Endmembers.WollastonitePercent:F1}%,  En: {result.Endmembers.EnstatitePercent:F1}%,  Fs: {result.Endmembers.FerrosilitePercent:F1}%"
                });
            }

            if (result.Endmembers.ForsteritePercent > 0.001)
            {
                DiagnosticItems.Add(new NiggliDiagnosticItem
                {
                    Name = LanguageService.Instance["niggli_diag_olivine_ratio"] ?? "Olivine Endmembers (Fo-Fa)",
                    Value = $"Fo: {result.Endmembers.ForsteritePercent:F1}%,  Fa: {result.Endmembers.FayalitePercent:F1}%"
                });
            }

            if (result.Endmembers.MagnesiumRatio > 0.001)
            {
                DiagnosticItems.Add(new NiggliDiagnosticItem
                {
                    Name = LanguageService.Instance["niggli_diag_mgr"] ?? "Magnesium Ratio (Mgr / Mg#)",
                    Value = $"{result.Endmembers.MagnesiumRatio:F1}"
                });
            }

            // 4. 去硅反应历程
            if (result.DesilicationSteps.Any())
            {
                DiagnosticItems.Add(new NiggliDiagnosticItem
                {
                    Name = LanguageService.Instance["niggli_diag_section_desilication"] ?? "── Desilication Steps ──",
                    Value = "",
                    IsSeparator = true
                });

                foreach (var step in result.DesilicationSteps)
                {
                    DiagnosticItems.Add(new NiggliDiagnosticItem
                    {
                        Name = "⚡",
                        Value = step.Format(k => LanguageService.Instance[k]),
                        IsHighlight = true
                    });
                }
            }

            // 5. 主要矿物百分比 (> 1%)
            var mainMinerals = result.Minerals
                .Where(kv => kv.Value > 1.0 && kv.Key != "Plag")
                .OrderByDescending(kv => kv.Value)
                .ToList();

            if (mainMinerals.Any())
            {
                DiagnosticItems.Add(new NiggliDiagnosticItem
                {
                    Name = LanguageService.Instance["niggli_diag_section_main_minerals"] ?? "── Main Normative Minerals (> 1%) ──",
                    Value = "",
                    IsSeparator = true
                });

                foreach (var m in mainMinerals)
                {
                    DiagnosticItems.Add(new NiggliDiagnosticItem
                    {
                        Name = FormatMineralHeader(m.Key),
                        Value = $"{m.Value:F2} %",
                        IsHighlight = m.Value > 10.0
                    });
                }
            }

            // 6. 警告信息
            if (result.Warnings.Any())
            {
                DiagnosticItems.Add(new NiggliDiagnosticItem
                {
                    Name = LanguageService.Instance["niggli_diag_warnings"] ?? "── Warnings ──",
                    Value = "",
                    IsSeparator = true
                });

                foreach (var w in result.Warnings)
                {
                    DiagnosticItems.Add(new NiggliDiagnosticItem
                    {
                        Name = "⚠",
                        Value = w.Format(k => LanguageService.Instance[k]),
                        IsHighlight = true
                    });
                }
            }
        }

        private static string FormatMineralHeader(string mineral)
        {
            var localized = LanguageService.Instance[$"niggli_mineral_{mineral}"];
            return !string.IsNullOrWhiteSpace(localized) ? $"{mineral}({localized})" : mineral;
        }

        private static void SetColumnWidthForHeader(Worksheet sheet, int column, string headerText, int minWidth)
        {
            // 按表头实际字体（与 EPMA 表格一致的粗体 Segoe UI）计算列宽
            sheet.SetColumnsWidth(column, 1, ReoGridTableAppearance.GetHeaderColumnWidth(headerText, minWidth, 200));
        }

        private static string TranslateSilicaSaturation(string value) => value switch
        {
            "oversaturated" => LanguageService.Instance["niggli_silica_oversaturated"] ?? "Oversaturated",
            "saturated" => LanguageService.Instance["niggli_silica_saturated"] ?? "Saturated",
            "undersaturated" => LanguageService.Instance["niggli_silica_undersaturated"] ?? "Undersaturated",
            _ => value ?? "—"
        };

        private static string TranslateAluminaState(string value) => value switch
        {
            "peralkaline" => LanguageService.Instance["niggli_alumina_peralkaline"] ?? "Peralkaline",
            "metaluminous" => LanguageService.Instance["niggli_alumina_metaluminous"] ?? "Metaluminous",
            "peraluminous" => LanguageService.Instance["niggli_alumina_peraluminous"] ?? "Peraluminous",
            _ => value ?? "—"
        };
    }

    /// <summary>
    /// Niggli 诊断列表项
    /// </summary>
    public class NiggliDiagnosticItem
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsHighlight { get; set; }
        public bool IsSeparator { get; set; }
    }
}
