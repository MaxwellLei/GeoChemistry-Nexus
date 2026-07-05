using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models.Cipw;
using GeoChemistryNexus.Services;
using Microsoft.Win32;
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
    /// <summary>
    /// CIPW标准矿物计算页面ViewModel
    /// </summary>
    public partial class CipwPageViewModel : ObservableObject
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
        /// 诊断面板是否展开（默认收缩为状态条）
        /// </summary>
        [ObservableProperty]
        private bool _isDiagnosticPanelExpanded = false;

        /// <summary>
        /// 诊断面板是否最大化
        /// </summary>
        [ObservableProperty]
        private bool _isDiagnosticMaximized = false;

        /// <summary>
        /// 校验并限制 Fe3+/Fe 比值在 [0, 1] 范围内
        /// </summary>
        public void ClampFe3Fraction()
        {
            if (Fe3Fraction < 0) Fe3Fraction = 0;
            else if (Fe3Fraction > 1) Fe3Fraction = 1;
        }

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
            "Cor", "Ac", "Di", "Hd", "Wo", "En", "Fs",
            "Fo", "Fa", "Mt", "Hm", "Ilm", "Cm", "Ru",
            "Tn", "Z", "Ap", "Cc", "Py", "Fl", "Hl", "Th", "ns", "ks"
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

        /// <summary>
        /// 初始化ReoGrid工作表
        /// </summary>
        public void InitializeWorksheet(ReoGridControl grid)
        {
            var sheet = grid.CurrentWorksheet;
            sheet.Name = LanguageService.Instance["cipw_sheet_name"] ?? "CIPW";

            // 隐藏工作表标签栏
            grid.SetSettings(unvell.ReoGrid.WorkbookSettings.View_ShowSheetTabControl, false);

            // 设置列数 = 输入列 + 分隔列 + 诊断列 + 结果矿物列
            int totalCols = InputColumns.Length + 1 + DiagnosticColumns.Length + ResultMineralOrder.Length;
            sheet.Resize(102, totalCols); // 100行数据 + 1行表头 + 1行备用

            int col = 0;

            // 输入列标题
            foreach (var oxide in InputColumns)
            {
                sheet[0, col] = oxide;
                sheet.SetColumnsWidth(col, 1, 70);
                col++;
            }

            // 分隔列
            sheet[0, col] = "│";
            sheet.SetColumnsWidth(col, 1, 20);
            var separatorStyle = new WorksheetRangeStyle
            {
                Flag = PlainStyleFlag.BackColor,
                BackColor = Color.FromRgb(240, 240, 240)
            };
            sheet.SetRangeStyles(new RangePosition(0, col, 102, 1), separatorStyle);
            col++;

            // 诊断列标题
            foreach (var diag in DiagnosticColumns)
            {
                var headerText = LanguageService.Instance[diag] ?? diag;
                sheet[0, col] = headerText;
                SetColumnWidthForHeader(sheet, col, headerText, minWidth: 110);
                col++;
            }

            // 结果矿物列标题
            foreach (var mineral in ResultMineralOrder)
            {
                var headerText = FormatMineralName(mineral);
                sheet[0, col] = headerText;
                SetColumnWidthForHeader(sheet, col, headerText, minWidth: 90);
                col++;
            }

            // 设置表头样式
            var headerStyle = new WorksheetRangeStyle
            {
                Flag = PlainStyleFlag.BackColor | PlainStyleFlag.TextColor |
                       PlainStyleFlag.FontStyleBold | PlainStyleFlag.HorizontalAlign,
                Bold = true,
                BackColor = Colors.LightGray,
                TextColor = Colors.Black,
                HAlign = ReoGridHorAlign.Center
            };
            sheet.SetRangeStyles(new RangePosition(0, 0, 1, totalCols), headerStyle);
            ApplyHeaderSideBorders(sheet, totalCols);

            // 锁定表头行（逐单元格设为只读）
            for (int i = 0; i < totalCols; i++)
            {
                sheet.Cells[0, i].IsReadOnly = true;
            }

            // 设置全部数据区域居中对齐
            var dataStyle = new WorksheetRangeStyle
            {
                Flag = PlainStyleFlag.HorizontalAlign,
                HAlign = ReoGridHorAlign.Center
            };
            sheet.SetRangeStyles(new RangePosition(1, 0, 101, totalCols), dataStyle);

            // 冻结表头行
            sheet.FreezeToCell(1, 0);
        }

        private static void ApplyHeaderSideBorders(Worksheet sheet, int totalCols)
        {
            for (int i = 0; i < totalCols; i++)
            {
                var cellRange = new RangePosition(0, i, 1, 1);
                sheet.SetRangeBorders(cellRange, BorderPositions.Left, RangeBorderStyle.SilverSolid);
                sheet.SetRangeBorders(cellRange, BorderPositions.Right, RangeBorderStyle.SilverSolid);
            }
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
            int successCount = 0;
            int totalCount = 0;

            int separatorCol = InputColumns.Length;
            int diagStartCol = separatorCol + 1;
            int mineralStartCol = diagStartCol + DiagnosticColumns.Length;

            // 清除旧结果
            for (int row = 1; row < sheet.RowCount; row++)
            {
                for (int c = diagStartCol; c < sheet.ColumnCount; c++)
                {
                    sheet[row, c] = null;
                }
            }

            // 逐行计算
            for (int row = 1; row < sheet.RowCount; row++)
            {
                // 读取输入数据
                var oxides = new Dictionary<string, double>();
                bool hasData = false;

                for (int c = 0; c < InputColumns.Length; c++)
                {
                    var cellValue = sheet[row, c];
                    if (cellValue != null && double.TryParse(cellValue.ToString(), out double value) && value > 0)
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
                        ? result.MassBalanceError.ToString("E2")
                        : result.MassBalanceError.ToString("F4");

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
                    sheet[row, diagStartCol] = LanguageService.Instance["cipw_error"] ?? "Error";
                    sheet[row, diagStartCol + 1] = FormatCipwMessage(result.ErrorMessageKey, result.ErrorMessageArgs, result.ErrorMessage);
                }
            }

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

            string filePath = await FileHelper.GetSaveFilePath2Async(
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
                for (int c = 0; c < sheet.ColumnCount; c++)
                {
                    var headerVal = sheet[0, c]?.ToString() ?? "";
                    if (headerVal == "│") headerVal = "---";
                    headers.Add(headerVal);
                }
                sb.AppendLine(string.Join(",", headers));

                // 写入数据行
                for (int row = 1; row < sheet.RowCount; row++)
                {
                    bool hasData = false;
                    var rowData = new List<string>();
                    for (int c = 0; c < sheet.ColumnCount; c++)
                    {
                        var val = sheet[row, c]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(val)) hasData = true;
                        rowData.Add(val);
                    }
                    if (hasData)
                        sb.AppendLine(string.Join(",", rowData));
                }

                var csvContent = sb.ToString();
                await Task.Run(() => File.WriteAllText(filePath, csvContent, Encoding.UTF8));
                var template = LanguageService.Instance["cipw_msg_export_success"] ?? "Results exported to: {0}";
                MessageHelper.Success(string.Format(template, Path.GetFileName(filePath)));
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

            for (int row = 1; row < sheet.RowCount; row++)
            {
                for (int c = 0; c < sheet.ColumnCount; c++)
                {
                    sheet[row, c] = null;
                }
            }

            _rowResults.Clear();
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

            for (int row = 0; row < examples.Length; row++)
            {
                for (int c = 0; c < InputColumns.Length; c++)
                {
                    if (examples[row].TryGetValue(InputColumns[c], out double val))
                    {
                        sheet[row + 1, c] = val;
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
            var rowFormat = LanguageService.Instance["cipw_row_format"] ?? "Row {0}";
            SelectedRowInfo = string.Format(rowFormat, row);
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
            double pixelsPerDip = 1.0;
            try
            {
                var mainWindow = Application.Current?.MainWindow;
                if (mainWindow != null)
                {
                    pixelsPerDip = VisualTreeHelper.GetDpi(mainWindow).PixelsPerDip;
                }
            }
            catch
            {
                // Fall back to 1.0 when DPI information is unavailable during design-time initialization.
            }

            var formattedText = new FormattedText(
                headerText ?? string.Empty,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Microsoft YaHei UI"),
                13,
                Brushes.Black,
                pixelsPerDip);

            var width = Math.Clamp((int)Math.Ceiling(formattedText.Width + 28), minWidth, 220);
            sheet.SetColumnsWidth(column, 1, (ushort)width);
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
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsHighlight { get; set; }
        public bool IsSeparator { get; set; }
    }
}
