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
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using unvell.ReoGrid;


namespace GeoChemistryNexus.ViewModels
{
    public partial class GeothermometerNewPageViewModel : ObservableObject
    {
        // 帮助抽屉是否打开
        [ObservableProperty]
        private bool isHelpDrawerOpen;

        // 引用 View 中的 RichTextBox 控件
        private RichTextBox? _helpRichTextBox;

        // 初始化
        public GeothermometerNewPageViewModel()
        {
            // 注册自定义函数
            CustomizeFuncHelper.RegisterAllFunctions();
        }

        /// <summary>
        /// 生成唯一的工作表名称
        /// </summary>
        /// <param name="reoGridControl">当前的工作簿</param>
        /// <param name="baseName">工作表名称</param>
        /// <returns>生成后的唯一名称</returns>
        private string GetUniqueWorksheetName(ReoGridControl reoGridControl, string baseName)
        {
            string uniqueName = baseName;
            int counter = 1;

            // 检查是否存在重名的工作表
            while (WorksheetExists(reoGridControl, uniqueName))
            {
                uniqueName = $"{baseName}_{counter}";
                counter++;
            }

            return uniqueName;
        }

        /// <summary>
        /// 检查工作表是否存在
        /// </summary>
        /// <param name="reoGridControl">当前的工作簿</param>
        /// <param name="worksheetName">工作表名称</param>
        /// <returns>存在则返回 True</returns>
        private bool WorksheetExists(ReoGridControl reoGridControl, string worksheetName)
        {
            foreach (var worksheet in reoGridControl.Worksheets)
            {
                if (worksheet.Name.Equals(worksheetName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// 创建并锁定表格表头的核心逻辑。
        /// </summary>
        /// <param name="worksheet">从CommandParameter传入的Worksheet对象。</param>
        /// <param name="requiredHeaders">需要设置的表头字符串列表。</param>
        private async Task ExecuteCreateGridHeader(ReoGridControl reoGridControl, List<string> requiredHeaders, 
            List<string> secondRowData, string worksheetName)
        {
            try
            {
                bool isConfirmed = await MessageHelper.ShowAsyncDialog(
                    LanguageService.Instance["confirm_new_worksheet"],
                    LanguageService.Instance["Cancel"],
                    LanguageService.Instance["Confirm"]);
                if (isConfirmed)
                {
                    // 处理重名工作表
                    string uniqueWorksheetName = GetUniqueWorksheetName(reoGridControl, worksheetName);
                    // 插入新工作表
                    var newWorksheet = reoGridControl.CreateWorksheet(uniqueWorksheetName);
                    reoGridControl.AddWorksheet(newWorksheet);
                    reoGridControl.CurrentWorksheet = newWorksheet;
                }

                // 获取当前工作表
                Worksheet worksheet = reoGridControl.CurrentWorksheet;

                // 清空工作表
                if (!isConfirmed) 
                {
                    // 保存当前的缩放比例
                    var currentScale = worksheet.ScaleFactor;
                    // 重置表格
                    worksheet.Reset();
                    //worksheet.DeleteRangeData(RangePosition.EntireRange); 
                    // 恢复缩放比例
                    worksheet.ScaleFactor = currentScale;
                }

                // 隐藏行列
                //worksheet.SetSettings(WorksheetSettings.View_ShowRowHeader, false);
                //worksheet.SetSettings(WorksheetSettings.View_ShowColumnHeader, false);

                //MessageHelper.Info($"工作表数量: {reoGridControl.Worksheets.Count}");
                //MessageHelper.Info($"当前工作表名称: {reoGridControl.CurrentWorksheet?.Name}");

                // 设置表头
                for (int i = 0; i < requiredHeaders.Count; i++)
                {
                    worksheet[0, i] = requiredHeaders[i];
                    worksheet.Cells[0, i].IsReadOnly = true;
                }

                // 设置示例样本
                for (int i = 0; i < secondRowData.Count && i < requiredHeaders.Count; i++)
                {
                    worksheet[1, i] = secondRowData[i];
                }

                // 设置表头样式
                var headerRange = new RangePosition(0, 0, 1, requiredHeaders.Count);

                // 设置示例样式
                var exampleRange = new RangePosition(1, 0, 1, secondRowData.Count);

                // 设置表头背景色和字体样式
                worksheet.SetRangeStyles(headerRange, new WorksheetRangeStyle
                {
                    Flag = PlainStyleFlag.BackColor | PlainStyleFlag.TextColor | 
                            PlainStyleFlag.FontStyleBold | PlainStyleFlag.HorizontalAlign,
                    HAlign = ReoGridHorAlign.Center,
                    BackColor = System.Windows.Media.Colors.LightGray,
                    TextColor = System.Windows.Media.Colors.Black,
                    Bold = true
                });

                // 设置表头背景色和字体样式
                worksheet.SetRangeStyles(exampleRange, new WorksheetRangeStyle
                {
                    Flag = PlainStyleFlag.BackColor | PlainStyleFlag.TextColor |
                            PlainStyleFlag.FontStyleBold | PlainStyleFlag.HorizontalAlign,
                    HAlign = ReoGridHorAlign.Center,
                    TextColor = System.Windows.Media.Colors.Black,
                });

                // 设置列宽自适应
                for (int i = 0; i < requiredHeaders.Count; i++)
                {
                    worksheet.AutoFitColumnWidth(i);
                    // 获取当前列宽
                    var currentWidth = worksheet.GetColumnWidth(i);
                    // 增加额外宽度
                    var extraWidth = 10;
                    worksheet.SetColumnsWidth(i, 1, (ushort)(currentWidth + extraWidth));

                    // 检查是否是温度列，如果是则设置保留一位小数
                    if (requiredHeaders[i].Contains("T(K)") || requiredHeaders[i].Contains("T(℃)"))
                    {
                        var range = new RangePosition(1, i, worksheet.RowCount - 1, 1);
                        worksheet.SetRangeDataFormat(range, unvell.ReoGrid.DataFormat.CellDataFormatFlag.Number,
                            new unvell.ReoGrid.DataFormat.NumberDataFormatter.NumberFormatArgs
                            {
                                DecimalPlaces = 1
                            });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(LanguageService.Instance["error_creating_table"] + ex.Message);
            }

        }


        /// <summary>
        /// 设置帮助文档显示的 RichTextBox 控件引用
        /// </summary>
        /// <param name="richTextBox">View 层传递进来的控件</param>
        public void SetHelpRichTextBox(RichTextBox richTextBox)
        {
            _helpRichTextBox = richTextBox;
        }

        /// <summary>
        /// 加载帮助文档命令
        /// </summary>
        /// <param name="relativePath">RTF 文档的相对路径</param>
        [RelayCommand]
        private void LoadHelpDocument(string? relativePath)
        {
            if (_helpRichTextBox == null)
            {
                MessageHelper.Error("RichTextBox 控件未初始化");
                return;
            }

            try
            {
                // 组合绝对路径
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string fullPath = string.Empty;
                bool success;

                if (!string.IsNullOrEmpty(relativePath))
                {
                    fullPath = System.IO.Path.Combine(baseDir, relativePath ,
                        LanguageService.GetLanguage() + ".rtf");
                }

                if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
                {
                    // 加载文档
                    success = RtfHelper.LoadRtfToRichTextBox(fullPath, _helpRichTextBox);
                    if (!success)
                    {
                        // todo
                    }
                }
                else
                {
                    fullPath = System.IO.Path.Combine(baseDir, relativePath, "en-US" + ".rtf");
                    success = RtfHelper.LoadRtfToRichTextBox(fullPath, _helpRichTextBox);
                    if (!success)
                    {
                        // 文件不存在
                        _helpRichTextBox.Document.Blocks.Clear();
                        // 未找到帮助文档,路径为空
                        var run = new Run($"{LanguageService.Instance["help_document_not_found"]}" +
                            $"\n{relativePath ?? LanguageService.Instance["path_is_empty"]}")
                        {
                            Foreground = Brushes.Red
                        };
                        _helpRichTextBox.Document.Blocks.Add(new Paragraph(run));
                    }
                }

                // 打开侧边抽屉
                IsHelpDrawerOpen = true;
            }
            catch (Exception ex)
            {
                MessageHelper.Error($"{LanguageService.Instance["failed_to_load_help_document"]} {ex.Message}");
            }
        }


        /// <summary>
        /// 打开读取数据文件
        /// TODO：完善读取 xlsx, xls 和 csv 文件
        /// </summary>
        /// <param name="reoGridControl"></param>
        [RelayCommand]
        public void OpenExcelFile(ReoGridControl reoGridControl)
        {
            string filePath = FileHelper.GetFilePath(LanguageService.Instance["csv_file_filter"]);
            if (filePath != null) { reoGridControl.Load(filePath); MessageHelper.Success(LanguageService.Instance["file_import_successful"]); } else
            {
                MessageHelper.Info(LanguageService.Instance["cancel_import"]);
            }
        }


        /// <summary>
        /// 保存当前工作表为 CSV 文件
        /// </summary>
        /// <param name="reoGridControl">当前工作簿</param>
        [RelayCommand]
        public void ExportWorksheet(ReoGridControl reoGridControl)
        {
            // 获取当前活动的工作表
            var worksheet = reoGridControl.CurrentWorksheet;
            if (worksheet == null) return;

            string tempFilePath = FileHelper.GetSaveFilePath2(title: LanguageService.Instance["Save as CSV File"], 
                filter: LanguageService.Instance["csv_file_filter"], defaultExt: ".csv", defaultFileName:worksheet.Name);
            if (string.IsNullOrEmpty(tempFilePath)) return;

            try
            {
                // 获取数据范围
                var range = worksheet.UsedRange;
                var csvBuilder = new StringBuilder();

                // 遍历数据
                for (int r = range.Row; r <= range.EndRow; r++)
                {
                    var rowValues = new List<string>();
                    for (int c = range.Col; c <= range.EndCol; c++)
                    {
                        // 获取单元格显示的文本，如果为空则返回空字符串
                        string cellValue = worksheet.GetCellText(r, c) ?? "";

                        // CSV转义处理
                        if (cellValue.Contains(",") || cellValue.Contains("\""))
                        {
                            cellValue = $"\"{cellValue.Replace("\"", "\"\"")}\"";
                        }
                        rowValues.Add(cellValue);
                    }
                    csvBuilder.AppendLine(string.Join(",", rowValues));
                }

                // 写入文件
                System.IO.File.WriteAllText(tempFilePath, csvBuilder.ToString(), new UTF8Encoding(true));

                MessageHelper.Success(LanguageService.Instance["export_successful"]);
            }
            catch (Exception ex)
            {
                MessageHelper.Error(LanguageService.Instance["export_failed"] + ex.Message);
            }
        }

        /// <summary>
        /// 新建工作表并插入到末尾
        /// </summary>
        /// <param name="reoGridControl">工作簿</param>
        /// <exception cref="Exception">创建失败抛出</exception>
        [RelayCommand]
        public void CreateWorkSheet(ReoGridControl reoGridControl)
        {
            try
            {
                // 生成唯一的工作表名称
                //string uniqueName = GetUniqueWorksheetName(reoGridControl, "Sheet");

                // 创建新工作表
                Worksheet newWorksheet= reoGridControl.CreateWorksheet();
                
                // 添加到末尾
                reoGridControl.Worksheets.Add(newWorksheet);

                // 激活新创建的工作表
                reoGridControl.CurrentWorksheet = newWorksheet;
            }
            catch (Exception ex)
            {
                throw new Exception(LanguageService.Instance["create_worksheet_failed"] + ex.Message);
            }
        }

        /// <summary>
        /// 模板：锆石 Ti 温度，Loucks et al. (2020)
        /// </summary>
        /// <param name="reoGridControl">当前工作簿</param>
        [RelayCommand]
        public async Task Zircon_Ti_Loucks_2020(ReoGridControl reoGridControl)
        {
            await ExecuteCreateGridHeader(reoGridControl
                    ,new List<string> { "ID", "Ti(ppm)", "P(MPa)", "α(TiO2)", "α(SiO2)", "T(K)", "T(℃)"}
                    ,new List<string> { "Example", "4.12", "300", "0.8", "1",
                                        "=Zircon_Loucks_et_al_2020(B2,C2,D2,E2)", "=F2-273.15"}
                    ,"Zircon_Ti_2020");
        }

        /// <summary>
        /// 模板：锆石 Zr 温度，主量，Watson and Harrison (1983)
        /// </summary>
        /// <param name="reoGridControl">当前工作簿</param>
        /// <returns>无</returns>
        [RelayCommand]
        public async Task Zircon_Zr_Principal_Watson_and_Harrison_1983(ReoGridControl reoGridControl)
        {
            await ExecuteCreateGridHeader(reoGridControl
                    , new List<string> { "ID", "Zr(ppm)", "SiO2(ppm)", "Al2O3(ppm)", "Fe2O3(ppm)", "FeO(ppm)", 
                                            "MgO(ppm)", "P2O5(ppm)", "CaO(ppm)", "K2O(ppm)", "Na2O(ppm)", "T(K)", "T(℃)" }
                    , new List<string> { "Example", "325.76", "67.89", "15.13", "4.1", "0", 
                                            "0.08","0.14", "2.11", "4.86", "3.46", "=Zircon_Watson_and_Harrison_1983(B2,C2,D2,E2,F2,G2,H2,I2,J2,K2)", "=L2-273.15" }
                    , "Zircon_Zr_1983");
        }

        /// <summary>
        /// 模板：闪锌矿 GGIMGis 温度，Frenzel et al. (2016)
        /// </summary>
        /// <param name="reoGridControl">当前工作簿</param>
        /// <returns>无</returns>
        [RelayCommand]
        public async Task Sphalerite_GGIMFis_Frenzel_2016(ReoGridControl reoGridControl)
        {
            await ExecuteCreateGridHeader(reoGridControl
                    , new List<string> { "ID", "Ga(ppm)", "Ge(ppm)", "Fe(ppm)", "Mn(ppm)", "In(ppm)",
                                            "T(K)", "T(℃)" }
                    , new List<string> { "Example", "202.9", "237.5", "7.465", "33.27", "20.46",
                                            "=Sphalerite_Frenzel_2016(B2,C2,D2,E2,F2)", "=G2-273.15" }
                    , "Sphalerite_GGIMFis_2016");
        }


        /// <summary>
        /// 模板：闪锌矿 ΔFeS 温度，Scott and Barnes (1971)
        /// </summary>
        /// <param name="reoGridControl">当前工作簿</param>
        /// <returns>无</returns>
        [RelayCommand]
        public async Task Sphalerite_FeS_Scott_and_Barne_1971(ReoGridControl reoGridControl)
        {
            await ExecuteCreateGridHeader(reoGridControl
                    , new List<string> { "ID", "FeS(mol%)_matrix", "FeS(mol%)_patch", "T(K)", "T(℃)" }
                    , new List<string> { "Example", "19.3", "21.5",
                                            "=Sphalerite_Scott_and_Barne_1971(B2,C2)", "=D2-273.15" }
                    , "Sphalerite_FeS_1971");
        }


        /// <summary>
        /// 模板：石英 TitaniQ 温度计, Wark and Watson (2006)
        /// </summary>
        /// <param name="reoGridControl">当前工作簿</param>
        /// <returns>无</returns>
        [RelayCommand]
        public async Task Quatz_Ti_Wark_and_Watson_2006(ReoGridControl reoGridControl)
        {
            await ExecuteCreateGridHeader(reoGridControl
                    , new List<string> { "ID", "Ti(ppm)", "α(TiO2)", "T(K)", "T(℃)" }
                    , new List<string> { "Example", "2.2", "1",
                                            "=Quatz_Wark_and_Watson_2006(B2,C2)", "=D2-273.15" }
                    , "Quatz_Ti_2006");
        }


        /// <summary>
        /// 模板：黑云母 Ti 温度计，Henry et al. (2005) 
        /// </summary>
        /// <param name="reoGridControl">当前工作簿</param>
        /// <returns>无</returns>
        [RelayCommand]
        public async Task Biotite_Ti_Henry_2005(ReoGridControl reoGridControl)
        {
            await ExecuteCreateGridHeader(reoGridControl
                    , new List<string> { "ID", "Ti(apfu)", "Mg(apfu)", "Fe(apfu)", "T(K)", "T(℃)" }
                    , new List<string> { "Example", "0.484", "3.195", "1.984",
                                            "=Biotite_Henry_et_al_2005(B2,C2,D2)", "=E2-273.15" }
                    , "Biotite_Ti_2005");
        }


        /// <summary>
        /// 模板：角闪石 Si* 温度计, Ridolfi et al. (2010)
        /// </summary>
        /// <param name="reoGridControl">当前工作簿</param>
        /// <returns>无</returns>
        [RelayCommand]
        public async Task Amphibole_Si_Ridolfi_2010(ReoGridControl reoGridControl)
        {
            await ExecuteCreateGridHeader(reoGridControl
                    , new List<string> { "ID", "SiO2(wt.%)", "TiO2(wt.%)", "Al2O3(wt.%)", "Cr2O3(wt.%)", "FeO(wt.%)"
                                            , "MnO(wt.%)", "MgO(wt.%)", "CaO(wt.%)", "Na2O(wt.%)", "K2O(wt.%)"
                                            , "F(wt.%)", "Cl(wt.%)", "T(K)", "T(℃)"}
                    , new List<string> { "Example", "45.47", "2.53", "9.07", "0.03", "12.68"
                                            , "0.24", "14.21", "11.32", "2.04", "0.62"
                                            , "0.17", "0.07"
                                            , "=Amphibole_Ridolfi_et_al_2010(B2,C2,D2,E2,F2,G2,H2,I2,J2,K2,L2,M2)", "=N2-273.15" }
                    , "Amphibole_Si_2010");
        }


        /// <summary>
        /// 模板：绿泥石温度计,  Jowett (1991)
        /// </summary>
        /// <param name="reoGridControl"></param>
        /// <returns></returns>
        [RelayCommand]
        public async Task Chlorite_Al4_Jowett_1991(ReoGridControl reoGridControl)
        {
            await ExecuteCreateGridHeader(reoGridControl
                    , new List<string> { "ID", "SiO2(wt.%)", "TiO2(wt.%)", "Al2O3(wt.%)", "FeO(wt.%)", "MnO(wt.%)"
                                            , "MgO(wt.%)", "CaO(wt.%)", "Na2O(wt.%)", "K2O(wt.%)", "BaO(wt.%)"
                                            , "Rb2O(wt.%)", "Cs2O(wt.%)", "ZnO(wt.%)", "F(wt.%)", "Cl(wt.%)"
                                            , "Cr2O3(wt.%)", "NiO(wt.%)", "T(K)", "T(℃)"}
                    , new List<string> { "Example", "37.663","0.045","23.254","12.791","0.058"
                                            ,"0.032","23.047","0.024","0","0"
                                            ,"0","0","0","0","0","0","0"
                                            ,"=Chlorite_Jowett_1991(B2,C2,D2,E2,F2,G2,H2,I2,J2,K2,L2,M2,N2,O2,P2,Q2,R2)", "=S2-273.15" }
                    , "Chlorite_Al4_1991");
        }


        /// <summary>
        /// 模板：毒砂温度计, Kretschmar and Scott (1976)
        /// </summary>
        /// <param name="reoGridControl"></param>
        /// <returns></returns>
        [RelayCommand]
        public async Task Arsenopyrite_Assemblage_Kretschmar_and_Scott_1976(ReoGridControl reoGridControl)
        {
            await ExecuteCreateGridHeader(reoGridControl
                    , new List<string> { "ID", "AtomicPercentAs(at.%)", "Assemblage"
                                            , "T(K)", "T(℃)"}
                    , new List<string> { "Example", "32.4","Asp_Py_Po"
                                            ,"=Arsenopyrite_Kretschmar_and_Scott_1976(B2,DefineArsenopyriteAssemblage(C2))", "=D2-273.15" }
                    , "Arsenopyrite_Assemblage_1976");
        }

    }
}
