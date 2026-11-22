using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models.Cipw;
using GeoChemistryNexus.Services;
using Microsoft.Win32;
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
    /// CIPW标准矿物计算页面ViewModel
    /// <para>
    /// 同时实现公共表格控件的两个可选接口：
    /// <see cref="IWorksheetRowStatusProvider"/>（行头状态点）与 <see cref="IWorksheetColumnHeaderProvider"/>（列头分组行 / 单位）。
    /// </para>
    /// </summary>
    public partial class CipwPageViewModel : ObservableObject, IWorksheetRowStatusProvider, IWorksheetColumnHeaderProvider
    {
        /// <summary>
        /// Fe3+/Fe总比值
        /// </summary>
        [ObservableProperty]
        private double _fe3Fraction = 0.15;

        /// <summary>
        /// 当前选中行的诊断信息
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<CipwDiagnosticItem> _diagnosticItems = new();

        /// <summary>
        /// 是否有诊断数据
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ExportDiagnosticDetailsCommand))]
        [NotifyCanExecuteChangedFor(nameof(CopyDiagnosticDetailsCommand))]
        private bool _hasDiagnosticData;

        /// <summary>
        /// 选中行信息
        /// </summary>
        [ObservableProperty]
        private string _selectedRowInfo = string.Empty;

        /// <summary>
        /// 当前选中行的硅饱和状态（用于顶部快速摘要）
        /// </summary>
        [ObservableProperty]
        private string _selectedSilicaState = string.Empty;

        /// <summary>
        /// 当前选中行的铝饱和状态（用于顶部快速摘要）
        /// </summary>
        [ObservableProperty]
        private string _selectedAluminaState = string.Empty;

        /// <summary>
        /// 当前选中行硅饱和度是否为"不饱和"（用于 DataTrigger 高亮，避免依赖本地化文本）
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

        /// <summary>
        /// 校验并限制 Fe3+/Fe 比值在 [0, 1] 范围内
        /// </summary>
        public void ClampFe3Fraction()
        {
            if (Fe3Fraction < 0) Fe3Fraction = 0;
            else if (Fe3Fraction > 1) Fe3Fraction = 1;
        }

        /// <summary>
        /// 初始工作表行数（含表头）
        /// </summary>
        private const int InitialWorksheetRowCount = 500;

        /// <summary>
        /// 样品号列索引（仅作标识，不参与计算）
        /// </summary>
        private const int SampleColumnIndex = 0;

        /// <summary>
        /// 氧化物输入起始列（样品号列之后）
        /// </summary>
        private const int InputStartColumn = 1;

        /// <summary>
        /// 输入氧化物列
        /// </summary>
        private static readonly string[] InputColumns = CipwConstants.InputOxides;

        /// <summary>
        /// 结果矿物列（按出现频率排序）
        /// </summary>
        private static readonly string[] ResultMineralOrder =
        {
            "Q", "Or", "Ab", "An", "Le", "Ne", "Kp",
            "Cor", "Ac", "Di", "Hd", "Wo", "Cs", "En", "Fs",
            "Fo", "Fa", "Mt", "Hm", "Ilm", "Cm", "Ru",
            "Tn", "Pf", "Z", "Ap", "Cc", "Py", "Fl", "Hl", "Th", "ns", "ks"
        };

        /// <summary>
        /// 诊断列（存储本地化资源键，渲染时通过 LanguageService 翻译为当前语言）
        /// </summary>
        private static readonly string[] DiagnosticColumns =
        {
            "cipw_col_silica_saturation", "cipw_col_alumina_state", "cipw_col_mass_balance_error"
        };

        /// <summary>
        /// 存储每行的计算结果
        /// </summary>
        private readonly Dictionary<int, CipwResult> _rowResults = new();

        /// <summary>最近一次计算失败的行（用于行头状态点）。</summary>
        private readonly HashSet<int> _failedRows = new();

        /// <summary>计算之后输入被修改、结果已过期的行（用于行头状态点）。</summary>
        private readonly HashSet<int> _staleRows = new();

        /// <summary>氧化物输入列中属于“主量元素”的个数（SiO2 … P2O5），其后为微量 / 挥发分。</summary>
        private static readonly int MajorOxideCount = Array.IndexOf(InputColumns, "P2O5") + 1;

        public event EventHandler? RowStatusChanged;
        public event EventHandler? ColumnHeaderInfoChanged;

        /// <summary>
        /// 当前绑定的表格控件（用于语言切换时刷新表头）
        /// </summary>
        private ReoGridControl? _grid;

        public CipwPageViewModel()
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
        /// 初始化ReoGrid工作表
        /// </summary>
        public void InitializeWorksheet(ReoGridControl grid)
        {
            _grid = grid;
            ReoGridImeHelper.Attach(grid);
            ReoGridDoubleClickFillHelper.Attach(grid); // 双击填充柄自动向下填充（类似 Excel）

            var sheet = grid.CurrentWorksheet;

            // 隐藏工作表标签栏
            grid.SetSettings(unvell.ReoGrid.WorkbookSettings.View_ShowSheetTabControl, false);

            // 设置列数 = 样品号列 + 输入列 + 分隔列 + 诊断列 + 结果矿物列
            int totalCols = 1 + InputColumns.Length + 1 + DiagnosticColumns.Length + ResultMineralOrder.Length;
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

            // 输入列标题（氧化物符号不随语言变化；Sample/诊断/矿物表头由 RefreshLocalizedHeaders 写入）
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

            // 设置全部数据区域居中对齐
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
                return result.Warnings.Count > 0 || result.MassBalanceWarning ? WorksheetRowStatus.Warning : WorksheetRowStatus.Ok;
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
            if (column < InputStartColumn || column == separatorCol) return null;   // 样品列、分隔列不属于任何分组

            if (column < separatorCol)
            {
                return column - InputStartColumn < MajorOxideCount
                    ? LanguageService.GetString("sheet_group_major_elements", "Major elements")
                    : LanguageService.GetString("sheet_group_other_oxides", "Other oxides / volatiles");
            }

            return column < separatorCol + 1 + DiagnosticColumns.Length
                ? LanguageService.GetString("sheet_group_diagnostics", "Diagnostics")
                : LanguageService.GetString("cipw_group_minerals", "CIPW normative minerals");
        }

        public string? GetUnit(int column)
        {
            int separatorCol = InputStartColumn + InputColumns.Length;
            int diagEnd = separatorCol + 1 + DiagnosticColumns.Length;
            // 样品列、分隔列、诊断列没有单位；氧化物与标准矿物均为质量百分比。
            if (column >= InputStartColumn && column < separatorCol) return "wt%";
            if (column >= diagEnd) return "wt%";
            return null;
        }

        #endregion

        /// <summary>
        /// 按当前语言刷新表头与工作表名称
        /// </summary>
        private void RefreshLocalizedHeaders(Worksheet sheet)
        {
            sheet.Name = LanguageService.Instance["cipw_sheet_name"] ?? "CIPW";

            int col = SampleColumnIndex;

            var sampleHeader = LanguageService.Instance["cipw_col_sample"] ?? "Sample";
            sheet.ColumnHeaders[col].Text = sampleHeader;
            SetColumnWidthForHeader(sheet, col, sampleHeader, minWidth: 90);
            col = InputStartColumn + InputColumns.Length + 1; // 跳过氧化物列与分隔列

            foreach (var diag in DiagnosticColumns)
            {
                var headerText = LanguageService.Instance[diag] ?? diag;
                sheet.ColumnHeaders[col].Text = headerText;
                SetColumnWidthForHeader(sheet, col, headerText, minWidth: 110);
                col++;
            }

            foreach (var mineral in ResultMineralOrder)
            {
                var headerText = FormatMineralName(mineral);
                sheet.ColumnHeaders[col].Text = headerText;
                SetColumnWidthForHeader(sheet, col, headerText, minWidth: 90);
                col++;
            }

            // 列头文字在绘制时读取，这里只需触发重绘
            sheet.RequestInvalidate();
        }

        /// <summary>输入列（样品号 + 氧化物）为输入样式，分隔列之后的诊断/矿物列为结果样式。</summary>
        private static ReoGridColumnKind GetColumnKind(int column, int separatorCol)
        {
            if (column < separatorCol) return ReoGridColumnKind.Input;
            return column == separatorCol ? ReoGridColumnKind.Separator : ReoGridColumnKind.Output;
        }

        /// <summary>
        /// 执行CIPW计算
        /// </summary>
        [RelayCommand]
        private void Calculate(ReoGridControl grid)
        {
            if (grid == null) return;

            // 确保 Fe3Fraction 在有效范围内
            ClampFe3Fraction();

            var sheet = grid.CurrentWorksheet;
            _rowResults.Clear();
            _failedRows.Clear();
            _staleRows.Clear();
            int successCount = 0;
            int totalCount = 0;

            int separatorCol = InputStartColumn + InputColumns.Length;
            int diagStartCol = separatorCol + 1;
            int mineralStartCol = diagStartCol + DiagnosticColumns.Length;

            // 清除旧结果
            for (int row = 0; row < sheet.RowCount; row++)
            {
                for (int c = diagStartCol; c < sheet.ColumnCount; c++)
                {
                    sheet[row, c] = null;
                }
            }

            // 逐行计算
            for (int row = 0; row < sheet.RowCount; row++)
            {
                // 读取输入数据（跳过样品号列，仅读取氧化物）
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

                // 执行计算
                var result = CipwCalculator.Calculate(oxides, Fe3Fraction);

                if (result.Success)
                {
                    successCount++;
                    _rowResults[row] = result;

                    // 写入诊断列
                    int dc = diagStartCol;
                    sheet[row, dc++] = TranslateSilicaSaturation(result.SilicaSaturation);
                    sheet[row, dc++] = TranslateAluminaState(result.AluminaState);
                    sheet[row, dc] = result.MassBalanceError < 0.01
                        ? InvariantNumberParser.Format(result.MassBalanceError, "E2")
                        : InvariantNumberParser.Format(result.MassBalanceError, "F4");

                    // 写入矿物结果
                    for (int m = 0; m < ResultMineralOrder.Length; m++)
                    {
                        string mineral = ResultMineralOrder[m];
                        if (result.MineralsWtPercent.TryGetValue(mineral, out double wtPct) && wtPct > 0.001)
                        {
                            sheet[row, mineralStartCol + m] = Math.Round(wtPct, 3);
                        }
                    }
                }
                else
                {
                    // 写入错误信息
                    _failedRows.Add(row);
                    sheet[row, diagStartCol] = LanguageService.Instance["cipw_error"] ?? "Error";
                    sheet[row, diagStartCol + 1] = FormatCipwMessage(result.ErrorMessageKey, result.ErrorMessageArgs, result.ErrorMessage);
                }
            }

            // 结果已更新，刷新行头状态点
            RowStatusChanged?.Invoke(this, EventArgs.Empty);

            // 计算完成，显示通知
            if (totalCount == 0)
            {
                MessageHelper.Warning(LanguageService.Instance["cipw_msg_no_data"] ?? "No valid data");
            }
            else if (successCount == totalCount)
            {
                var template = LanguageService.Instance["cipw_msg_calc_all_success"] ?? "All {0} samples succeeded.";
                MessageHelper.Success(string.Format(template, successCount));
            }
            else
            {
                var template = LanguageService.Instance["cipw_msg_calc_partial_success"] ?? "{0}/{1} samples succeeded.";
                MessageHelper.Info(string.Format(template, successCount, totalCount));
            }
        }

        /// <summary>
        /// 导出结果到CSV
        /// </summary>
        [RelayCommand]
        private async Task ExportCsv(ReoGridControl grid)
        {
            if (grid == null) return;

            string? filePath = await FileHelper.GetSaveFilePath2Async(
                title: LanguageService.Instance["cipw_export_dialog_title"] ?? "Export CIPW Results",
                filter: LanguageService.Instance["cipw_csv_filter"] ?? "CSV File|*.csv",
                defaultExt: ".csv",
                defaultFileName: "CIPW_Results.csv");
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                var sheet = grid.CurrentWorksheet;
                var sb = new StringBuilder();

                // 写入表头
                var headers = new List<string>();
                int separatorCol = InputStartColumn + InputColumns.Length;
                for (int c = 0; c < sheet.ColumnCount; c++)
                {
                    var headerVal = c == separatorCol ? "---" : sheet.ColumnHeaders[c].Text ?? "";
                    headers.Add(ReoGridFormatHelper.EscapeCsvCell(headerVal));
                }
                sb.AppendLine(string.Join(",", headers));

                // 写入数据行
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

                var csvContent = sb.ToString();
                await Task.Run(() => File.WriteAllText(filePath, csvContent, Encoding.UTF8));
                var template = LanguageService.Instance["cipw_msg_export_success"] ?? "Results exported to: {0}";
                MessageHelper.ExportSuccess(string.Format(template, Path.GetFileName(filePath)), filePath);
            }
            catch (Exception ex)
            {
                var template = LanguageService.Instance["cipw_msg_export_failed"] ?? "Export failed: {0}";
                MessageHelper.Error(string.Format(template, ex.Message));
            }
        }

        /// <summary>
        /// 清除所有数据
        /// </summary>
        [RelayCommand]
        private async Task ClearData(ReoGridControl grid)
        {
            if (grid == null) return;

            var confirmed = await MessageHelper.ShowAsyncDialog(
                LanguageService.Instance["cipw_confirm_clear_msg"] ?? "Clear all input and results?",
                LanguageService.Instance["cipw_btn_cancel"] ?? "Cancel",
                LanguageService.Instance["cipw_btn_confirm_clear"] ?? "Confirm Clear");
            if (!confirmed) return;

            var sheet = grid.CurrentWorksheet
                ?? (grid.Worksheets.Count > 0 ? grid.Worksheets[0] : null);
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

            MessageHelper.Success(LanguageService.Instance["cipw_msg_data_cleared"] ?? "Data cleared.");
        }

        /// <summary>
        /// 填充示例数据
        /// </summary>
        [RelayCommand]
        private void FillExample(ReoGridControl grid)
        {
            if (grid == null) return;

            var sheet = grid.CurrentWorksheet;

            // 示例花岗岩样品
            var example1 = new Dictionary<string, double>
            {
                ["SiO2"] = 72.04, ["TiO2"] = 0.30, ["Al2O3"] = 14.42,
                ["Fe2O3"] = 1.12, ["FeO"] = 1.68, ["MnO"] = 0.05,
                ["MgO"] = 0.52, ["CaO"] = 1.82, ["Na2O"] = 3.69,
                ["K2O"] = 4.12, ["P2O5"] = 0.10
            };

            // 示例玄武岩样品
            var example2 = new Dictionary<string, double>
            {
                ["SiO2"] = 49.20, ["TiO2"] = 1.84, ["Al2O3"] = 15.74,
                ["Fe2O3"] = 3.79, ["FeO"] = 7.13, ["MnO"] = 0.20,
                ["MgO"] = 6.73, ["CaO"] = 9.47, ["Na2O"] = 2.91,
                ["K2O"] = 1.10, ["P2O5"] = 0.35
            };

            // 示例安山岩样品
            var example3 = new Dictionary<string, double>
            {
                ["SiO2"] = 57.94, ["TiO2"] = 0.87, ["Al2O3"] = 17.02,
                ["Fe2O3"] = 3.27, ["FeO"] = 4.04, ["MnO"] = 0.14,
                ["MgO"] = 3.33, ["CaO"] = 6.79, ["Na2O"] = 3.48,
                ["K2O"] = 1.62, ["P2O5"] = 0.21
            };

            var examples = new[] { example1, example2, example3 };
            var sampleNames = new[] { "Granite", "Basalt", "Andesite" };

            for (int row = 0; row < examples.Length; row++)
            {
                sheet[row, SampleColumnIndex] = sampleNames[row];

                for (int c = 0; c < InputColumns.Length; c++)
                {
                    if (examples[row].TryGetValue(InputColumns[c], out double val))
                    {
                        sheet[row, InputStartColumn + c] = val;
                    }
                }
            }

            // 示例数据已填充到表格
        }

        /// <summary>
        /// 选中行变更时更新诊断信息
        /// </summary>
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
            if (!string.IsNullOrEmpty(sampleName))
            {
                SelectedRowInfo = sampleName;
            }
            else
            {
                var rowFormat = LanguageService.Instance["cipw_row_format"] ?? "Row {0}";
                SelectedRowInfo = string.Format(rowFormat, row + 1);
            }
            SelectedSilicaState = TranslateSilicaSaturation(result.SilicaSaturation);
            SelectedAluminaState = TranslateAluminaState(result.AluminaState);
            IsSilicaUndersaturated = result.SilicaSaturation == "undersaturated";

            // 添加诊断项
            DiagnosticItems.Add(new CipwDiagnosticItem
            {
                Name = LanguageService.Instance["cipw_diag_silica_saturation"] ?? "Silica saturation",
                Value = TranslateSilicaSaturation(result.SilicaSaturation),
                IsHighlight = result.SilicaSaturation == "undersaturated"
            });

            DiagnosticItems.Add(new CipwDiagnosticItem
            {
                Name = LanguageService.Instance["cipw_diag_alumina_saturation"] ?? "Alumina saturation",
                Value = TranslateAluminaState(result.AluminaState),
                IsHighlight = result.AluminaState == "peralkaline"
            });

            DiagnosticItems.Add(new CipwDiagnosticItem
            {
                Name = LanguageService.Instance["cipw_diag_iron_mode"] ?? "Iron handling mode",
                Value = TranslateIronMode(result.IronMode),
                IsHighlight = result.IronMode != "measured"
            });

            DiagnosticItems.Add(new CipwDiagnosticItem
            {
                Name = LanguageService.Instance["cipw_diag_mass_balance_error"] ?? "Mass balance error",
                Value = $"{result.MassBalanceError:F6} %",
                IsHighlight = result.MassBalanceWarning
            });

            DiagnosticItems.Add(new CipwDiagnosticItem
            {
                Name = LanguageService.Instance["cipw_diag_total_minerals"] ?? "Total minerals",
                Value = $"{result.TotalMassSum:F4} %",
                IsHighlight = false
            });

            // 主要矿物组成
            var sortedMinerals = result.MineralsWtPercent
                .Where(kv => kv.Value > 0.01)
                .OrderByDescending(kv => kv.Value)
                .ToList();

            if (sortedMinerals.Any())
            {
                DiagnosticItems.Add(new CipwDiagnosticItem
                {
                    Name = LanguageService.Instance["cipw_diag_main_minerals"] ?? "── Main minerals ──",
                    Value = "",
                    IsHighlight = false,
                    IsSeparator = true
                });

                foreach (var kv in sortedMinerals)
                {
                    DiagnosticItems.Add(new CipwDiagnosticItem
                    {
                        Name = FormatMineralName(kv.Key, includeSpace: true),
                        Value = $"{kv.Value:F3} %",
                        IsHighlight = kv.Value > 10.0
                    });
                }
            }

            // 警告信息
            if (result.Warnings.Any())
            {
                DiagnosticItems.Add(new CipwDiagnosticItem
                {
                    Name = LanguageService.Instance["cipw_diag_warnings"] ?? "── Warnings ──",
                    Value = "",
                    IsHighlight = false,
                    IsSeparator = true
                });

                foreach (var warning in result.Warnings)
                {
                    DiagnosticItems.Add(new CipwDiagnosticItem
                    {
                        Name = "⚠",
                        Value = FormatCipwMessage(warning),
                        IsHighlight = true
                    });
                }
            }
        }

        private static string FormatMineralName(string mineral, bool includeSpace = false)
        {
            var localizedName = LanguageService.Instance[$"cipw_mineral_{mineral}"];
            if (string.IsNullOrWhiteSpace(localizedName))
            {
                return mineral;
            }

            return includeSpace
                ? $"{mineral} ({localizedName})"
                : $"{mineral}({localizedName})";
        }

        private static void SetColumnWidthForHeader(Worksheet sheet, int column, string headerText, int minWidth)
        {
            // 按表头实际字体（与 EPMA 表格一致的粗体 Segoe UI）计算列宽
            sheet.SetColumnsWidth(column, 1, ReoGridTableAppearance.GetHeaderColumnWidth(headerText, minWidth, 220));
        }

        private static string FormatCipwMessage(CipwMessage message)
        {
            return FormatCipwMessage(message.Key, message.Args, message.Key);
        }

        private static string FormatCipwMessage(string key, object[] args, string fallback)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return fallback ?? string.Empty;
            }

            var template = LanguageService.Instance[key] ?? fallback ?? key;
            return args?.Length > 0 ? string.Format(template, args) : template;
        }

        private static string TranslateSilicaSaturation(string value) => value switch
        {
            "oversaturated" => LanguageService.Instance["cipw_silica_oversaturated"] ?? "Oversaturated",
            "saturated" => LanguageService.Instance["cipw_silica_saturated"] ?? "Saturated",
            "undersaturated" => LanguageService.Instance["cipw_silica_undersaturated"] ?? "Undersaturated",
            _ => value ?? "—"
        };

        private static string TranslateAluminaState(string value) => value switch
        {
            "peralkaline" => LanguageService.Instance["cipw_alumina_peralkaline"] ?? "Peralkaline",
            "metaluminous" => LanguageService.Instance["cipw_alumina_metaluminous"] ?? "Metaluminous",
            "peraluminous" => LanguageService.Instance["cipw_alumina_peraluminous"] ?? "Peraluminous",
            _ => value ?? "—"
        };

        private static string TranslateIronMode(string value) => value switch
        {
            "measured" => LanguageService.Instance["cipw_iron_measured"] ?? "Measured",
            "partial_assumed" => LanguageService.Instance["cipw_iron_partial_assumed"] ?? "Partial assumed",
            "estimated_from_FeOT" => LanguageService.Instance["cipw_iron_estimated_feot"] ?? "Estimated from FeOT",
            "inconsistent_input" => LanguageService.Instance["cipw_iron_inconsistent"] ?? "Inconsistent input",
            "missing" => LanguageService.Instance["cipw_iron_missing"] ?? "Missing",
            _ => value ?? "—"
        };
    }

    /// <summary>
    /// CIPW诊断项数据模型
    /// </summary>
    public class CipwDiagnosticItem
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsHighlight { get; set; }
        public bool IsSeparator { get; set; }
    }
}
