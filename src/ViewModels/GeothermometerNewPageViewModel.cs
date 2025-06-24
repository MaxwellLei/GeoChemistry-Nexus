using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using HandyControl.Controls;
using ScottPlot.Colormaps;
using ScottPlot.Palettes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using unvell.ReoGrid;


namespace GeoChemistryNexus.ViewModels
{
    public partial class GeothermometerNewPageViewModel : ObservableObject
    {
        // 导航对象
        [ObservableProperty]
        private object? currentView;

        // 初始化
        public GeothermometerNewPageViewModel()
        {
            // 注册自定义函数
            CustomizeFuncHelper.RegisterAllFunctions();
        }

        /// <summary>
        /// 检查是否选中大于两个以上的区域
        /// </summary>
        /// <param name="selection">用户选中的区域</param>
        /// <returns>返回校验结果</returns>
        private bool CheckSelectedArea(RangePosition selection)
        {
            // 确保选区不为空
            if (selection.IsEmpty)
            {
                MessageHelper.Error("请先在表格中选中要计算的区域");
                return false;
            }

            // 确保选区足够大
            if (selection.Cols <= 2 && selection.Rows == 1)
            {
                MessageHelper.Error("选区范围太小");
                return false;
            }
            return true;
        }


        /// <summary>
        /// 检查表头是否包含所需的特征列
        /// </summary>
        /// <param name="worksheet">当前激活的表格</param>
        /// <param name="requiredHeaders">需要的特征列名称列表</param>
        /// <returns>符合条件返回 True</returns>
        private bool ValidateHeaders(Worksheet worksheet, List<string> requiredHeaders)
        {
            RangePosition selection = worksheet.SelectionRange;
            // 从选中区域的第一行提取所有表头文本
            var actualHeaders = new HashSet<string>();
            for (int col = selection.Col; col <= selection.EndCol; col++)
            {
                var cellData = worksheet.GetCellData(selection.Row, col)?.ToString();
                if (!string.IsNullOrWhiteSpace(cellData))
                {
                    actualHeaders.Add(cellData.Trim());
                }
            }
            // 找出缺失的表头
            var missingHeaders = requiredHeaders.Except(actualHeaders).ToList();
            if (missingHeaders.Any())
            {
                MessageHelper.Error($"操作失败：选区的第一行缺少以下必需的特征列：\n{string.Join(", ", missingHeaders)}");
                return false;
            }
            return true;
        }


        /// <summary>
        /// 检查选区内容是否为空值或者非数值类型
        /// </summary>
        /// <param name="worksheet">当前激活的表格</param>
        /// <returns>不存在非法值，返回 True</returns>
        private bool ValidateDataBody(Worksheet worksheet)
        {
            RangePosition selection = worksheet.SelectionRange;
            // 从选区的第二行开始遍历
            for (int r = selection.Row + 1; r <= selection.EndRow; r++)
            {
                for (int c = selection.Col; c <= selection.EndCol; c++)
                {
                    var cell = worksheet.Cells[r, c];
                    var cellData = cell.Data;

                    // 检查空值
                    if (cellData == null || cellData is DBNull || string.IsNullOrWhiteSpace(cellData.ToString()))
                    {
                        MessageHelper.Error($"数据错误：单元格 {cell.Position.ToAddress()} 的内容不能为空。");
                        return false;
                    }

                    // 检查是否为数值
                    if (!double.TryParse(cellData.ToString(), out _))
                    {
                        MessageHelper.Error($"数据错误：单元格 {cell.Position.ToAddress()} 的内容 “{cellData}” 不是有效的数值。");
                        return false;
                    }
                }
            }
            return true;
        }


        /// <summary>
        /// 进行数据和选区校验
        /// </summary>
        /// <param name="worksheet">当前激活的表格</param>
        /// <returns>无误，返回 True</returns>
        private bool DataValidation(Worksheet worksheet, List<string> requiredHeaders)
        {
            // 数据不为空
            if (worksheet == null) { MessageHelper.Error("选区数据为空"); return false; }
            // 选择区域不为空且足够大
            if (!CheckSelectedArea(worksheet.SelectionRange))   return false;
            // 选择区域包含计算的特征列
            if (!ValidateHeaders(worksheet, requiredHeaders))   return false;
            // 数据没问题
            return true;
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
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(LanguageService.Instance["error_creating_table"] + ex.Message);
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
            string filePath = FileHelper.GetFilePath("CSV文件|*.csv");
            if (filePath != null) { reoGridControl.Load(filePath); MessageHelper.Success(LanguageService.Instance["file_import_successful"]); }
            MessageHelper.Info(LanguageService.Instance["cancel_import"]);
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

            string tempFilePath = FileHelper.GetSaveFilePath2(title: "保存为csv文件", filter: "CSV文件|*.csv",
                                                                defaultExt: ".csv", defaultFileName:worksheet.Name);
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
                    ,new List<string> { "ID", "Ti(ppm)", "P(MPa)", "α(TiO2)", "α(SiO2)", "T(K)", "T(℃)"
                    ,"Loucks, R. R., O'Connell, R. J., Zanazzi, P. F., & Kelemen, P. B. (2020). \"The role of zircon in tracing Ti diffusion and temperature in metamorphic rocks.\""}
                    ,new List<string> { "Example", "4.12", "300", "0.8", "1", 
                                        "=((-4800+(0.4748*(C2-1000)))/(LOG10(B2)-5.711-LOG10(D2)+LOG10(E2)))", "=F2-273.15"}
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
                                            "0.08","0.14", "2.11", "4.86", "3.46", "=Zircon_Zr_Principal_Watson_and_Harrison_1983(B2,C2,D2,E2,F2,G2,H2,I2,J2,K2)", "=L2-273.15" }
                    , "Zircon_Zr_1983");
        }


        /// <summary>
        /// 模板：锆石 Zr 温度，饱和，Watson and Harrison (1983)
        /// </summary>
        /// <param name="reoGridControl">当前工作簿</param>
        /// <returns>无</returns>
        [RelayCommand]
        public async Task Zircon_Zr_Saturation_Watson_and_Harrison_1983(ReoGridControl reoGridControl)
        {
            await ExecuteCreateGridHeader(reoGridControl
                    , new List<string> { "ID", "Zr(ppm)", "SiO2(ppm)", "Al2O3(ppm)", "CaO(ppm)",
                                            "K2O(ppm)", "Na2O(ppm)", "T(K)", "T(℃)" }
                    , new List<string> { "Example", "99", "65.11", "15.68", "3.42",
                                            "3.12","3.85", "=Zircon_Zr_Saturation_Watson_and_Harrison_1983(B2,C2,D2,E2,F2,G2)", "=G2-273.15" }
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
                    , new List<string> { "ID", "Ga(ppm)", "Ge(ppm)", "Fe*(ppm)", "Mn(ppm)", "In(ppm)",
                                            "T(K)", "T(℃)" }
                    , new List<string> { "Example", "202.9", "237.5", "7.465", "33.27", "20.46",
                                            "=Sphalerite_GGIMFis_Frenzel_2016(B2,C2,D2,E2,F2)+273.15", "=G2-273.15" }
                    , "Sphalerite_GGIMFis_2016");
        }


        /// <summary>
        /// 模板：闪锌矿 ΔFeS 温度，Frenzel et al. (2016)
        /// </summary>
        /// <param name="reoGridControl">当前工作簿</param>
        /// <returns>无</returns>
        [RelayCommand]
        public async Task Sphalerite_FeS_Scott_and_Barne_1971(ReoGridControl reoGridControl)
        {
            await ExecuteCreateGridHeader(reoGridControl
                    , new List<string> { "ID", "ΔFeS(mol%)","T(K)", "T(℃)" }
                    , new List<string> { "Example", "2.2", 
                                            "=Sphalerite_FeS_Scott_and_Barne_1971(B2)+273.15", "=C2-273.15" }
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
                                            "=Quatz_Ti_Wark_and_Watson_2006(B2,C2)", "=D2-273.15" }
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
                    , new List<string> { "ID", "Ti(apfu)", "xMg", "T(K)", "T(℃)" }
                    , new List<string> { "Example", "0.53", "0.57",
                                            "=Biotite_Ti_Henry_2005(B2,C2)+273.15", "=D2-273.15" }
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
                                            , "=Amphibole_Si_Ridolfi_2010(B2,C2,D2,E2,F2,G2,H2,I2,J2,K2,L2,M2)+273.15", "=N2-273.15" }
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
                                            ,"=Chlorite_Al4_Jowett_1991(B2,C2,D2,E2,F2,G2,H2,I2,J2,K2,L2,M2,N2,O2,P2,Q2,R2)+273.15", "=S2-273.15" }
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
                                            ,"=Arsenopyrite_Assemblage_Kretschmar_and_Scott_1976(B2,DefineArsenopyriteAssemblage(C2))+273.15", "=D2-273.15" }
                    , "Arsenopyrite_Assemblage_1976");
        }

    }
}
