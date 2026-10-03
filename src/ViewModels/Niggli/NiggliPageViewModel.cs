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

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// Niggli 标准分子矿物计算页面 ViewModel
    /// </summary>
    public partial class NiggliPageViewModel : ObservableObject
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
        /// 诊断面板是否展开
        /// </summary>
        [ObservableProperty]
        private bool _isDiagnosticPanelExpanded = false;

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
        private ReoGridControl? _grid;

        public NiggliPageViewModel()
        {
            LanguageService.Instance.PropertyChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Item[]" || _grid?.CurrentWorksheet == null)
                return;

            RefreshLocalizedHeaders(_grid.CurrentWorksheet);
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

            // 输入列标题
            for (int c = 0; c < InputColumns.Length; c++)
            {
                int col = InputStartColumn + c;
                sheet[0, col] = InputColumns[c];
                sheet.SetColumnsWidth(col, 1, 70);
            }

            // 分隔列
            int separatorCol = InputStartColumn + InputColumns.Length;
            sheet[0, separatorCol] = "│";
            sheet.SetColumnsWidth(separatorCol, 1, 20);
            var separatorStyle = new WorksheetRangeStyle
            {
                Flag = PlainStyleFlag.BackColor,
                BackColor = Color.FromRgb(240, 240, 240)
            };
            sheet.SetRangeStyles(new RangePosition(0, separatorCol, InitialWorksheetRowCount, 1), separatorStyle);

            // 表头样式
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

            // 锁定表头
            for (int i = 0; i < totalCols; i++)
            {
                sheet.Cells[0, i].IsReadOnly = true;
            }

            // 数据区域居中对齐
            var dataStyle = new WorksheetRangeStyle
            {
                Flag = PlainStyleFlag.HorizontalAlign,
                HAlign = ReoGridHorAlign.Center
            };
            sheet.SetRangeStyles(new RangePosition(1, 0, InitialWorksheetRowCount - 1, totalCols), dataStyle);

            // 冻结首行
            sheet.FreezeToCell(1, 0);

            RefreshLocalizedHeaders(sheet);
        }

        private void RefreshLocalizedHeaders(Worksheet sheet)
        {
            sheet.Name = LanguageService.Instance["niggli_sheet_name"] ?? "Niggli";

            int col = SampleColumnIndex;
            var sampleHeader = LanguageService.Instance["niggli_col_sample"] ?? "Sample";
            sheet[0, col] = sampleHeader;
            SetColumnWidthForHeader(sheet, col, sampleHeader, minWidth: 90);

            // 跳过输入列和分隔列
            col = InputStartColumn + InputColumns.Length + 1;

            // 尼格里参数列标题
            foreach (var niggliVal in NiggliValueColumns)
            {
                var headerText = niggliVal;
                sheet[0, col] = headerText;
                SetColumnWidthForHeader(sheet, col, headerText, minWidth: 65);
                col++;
            }

            // 诊断列标题
            foreach (var diag in DiagnosticColumns)
            {
                var headerText = LanguageService.Instance[diag] ?? diag;
                sheet[0, col] = headerText;
                SetColumnWidthForHeader(sheet, col, headerText, minWidth: 100);
                col++;
            }

            // 矿物列标题
            foreach (var mineral in ResultMineralOrder)
            {
                var headerText = FormatMineralHeader(mineral);
                sheet[0, col] = headerText;
                SetColumnWidthForHeader(sheet, col, headerText, minWidth: 75);
                col++;
            }
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

        [RelayCommand]
        private void Calculate(ReoGridControl grid)
        {
            if (grid == null) return;

            ClampFe3Fraction();
            var sheet = grid.CurrentWorksheet;
            _rowResults.Clear();

            int successCount = 0;
            int totalCount = 0;

            int separatorCol = InputStartColumn + InputColumns.Length;
            int niggliValStartCol = separatorCol + 1;
            int diagStartCol = niggliValStartCol + NiggliValueColumns.Length;
            int mineralStartCol = diagStartCol + DiagnosticColumns.Length;

            // 清除旧计算结果
            for (int row = 1; row < sheet.RowCount; row++)
            {
                for (int c = niggliValStartCol; c < sheet.ColumnCount; c++)
                {
                    sheet[row, c] = null;
                }
            }

            // 逐行执行计算
            for (int row = 1; row < sheet.RowCount; row++)
            {
                var oxides = new Dictionary<string, double>();
                bool hasData = false;

                for (int c = 0; c < InputColumns.Length; c++)
                {
                    var cellValue = sheet[row, InputStartColumn + c];
                    if (cellValue != null && double.TryParse(cellValue.ToString(), out double value) && value > 0)
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
                    sheet[row, dc] = result.MineralSum.ToString("F2");

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
                    sheet[row, diagStartCol] = LanguageService.Instance["niggli_error"] ?? "Error";
                    string err = !string.IsNullOrEmpty(result.ErrorKey)
                        ? (LanguageService.Instance[result.ErrorKey] ?? result.ErrorMessage)
                        : result.ErrorMessage;
                    sheet[row, diagStartCol + 1] = err;
                }
            }

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
                for (int c = 0; c < sheet.ColumnCount; c++)
                {
                    var headerVal = sheet[0, c]?.ToString() ?? "";
                    if (headerVal == "│") headerVal = "---";
                    headers.Add(headerVal);
                }
                sb.AppendLine(string.Join(",", headers));

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

                await Task.Run(() => File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8));
                var template = LanguageService.Instance["niggli_msg_export_success"] ?? "Results exported to: {0}";
                MessageHelper.Success(string.Format(template, Path.GetFileName(filePath)));
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
                int row = i + 1;
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
                : string.Format(LanguageService.Instance["niggli_row_format"] ?? "Row {0}", row);

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
            double pixelsPerDip = 1.0;
            try
            {
                var mainWindow = Application.Current?.MainWindow;
                if (mainWindow != null)
                {
                    pixelsPerDip = VisualTreeHelper.GetDpi(mainWindow).PixelsPerDip;
                }
            }
            catch { }

            var formattedText = new FormattedText(
                headerText ?? string.Empty,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Microsoft YaHei UI"),
                13,
                Brushes.Black,
                pixelsPerDip);

            var width = Math.Clamp((int)Math.Ceiling(formattedText.Width + 26), minWidth, 200);
            sheet.SetColumnsWidth(column, 1, (ushort)width);
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
