using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using GeoChemistryNexus.Views;
using unvell.ReoGrid;

namespace GeoChemistryNexus.ViewModels
{
    public partial class GeothermometerPageViewModel : ObservableObject, IRecipient<DeveloperModeChangedMessage>
    {
        /// <summary>
        /// 矿物分组列表（用于左侧导航）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<MineralGroupViewModel> mineralGroups = new();

        /// <summary>
        /// 当前选中的 GTM
        /// </summary>
        [ObservableProperty]
        private Geothermometer? selectedPlugin;

        /// <summary>
        /// 是否显示帮助文档区域（选中温度计时显示，点击确认后隐藏）
        /// </summary>
        [ObservableProperty]
        private bool isHelpDocVisible;

        /// <summary>
        /// 当前选中的温度计是否已应用（控制顶部按钮显示逻辑）
        /// </summary>
        [ObservableProperty]
        private bool isPluginApplied;

        /// <summary>
        /// 是否正在检查更新
        /// </summary>
        [ObservableProperty]
        private bool isCheckingUpdates;

        /// <summary>
        /// 更新状态文本
        /// </summary>
        [ObservableProperty]
        private string updateStatusText = string.Empty;

        /// <summary>
        /// 可用更新数量
        /// </summary>
        [ObservableProperty]
        private int availableUpdateCount;

        /// <summary>
        /// 搜索文本
        /// </summary>
        [ObservableProperty]
        private string searchText = string.Empty;

        /// <summary>
        /// 已加载的 GTM 总数
        /// </summary>
        [ObservableProperty]
        private int totalPluginCount;

        /// <summary>
        /// 自定义温度计列表（平面列表，不按矿物分组）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Geothermometer> customPlugins = new();

        /// <summary>
        /// 官方温度计数量
        /// </summary>
        [ObservableProperty]
        private int officialPluginCount;

        /// <summary>
        /// 自定义温度计数量
        /// </summary>
        [ObservableProperty]
        private int customPluginCount;

        /// <summary>
        /// 是否为开发者模式
        /// </summary>
        [ObservableProperty]
        private bool isDeveloperMode;

        /// <summary>
        /// 中间计算步骤列表（用于详细计算面板展示）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<CalculationStep> calculationSteps = new();

        /// <summary>
        /// 计算详情面板是否最小化（仅显示标题栏）
        /// </summary>
        [ObservableProperty]
        private bool isDetailPanelMinimized = true;

        /// <summary>
        /// 计算详情面板是否最大化（撑满表格区域）
        /// </summary>
        [ObservableProperty]
        private bool isDetailPanelMaximized;

        /// <summary>
        /// 是否有可显示的计算数据
        /// </summary>
        [ObservableProperty]
        private bool hasCalculationData;

        /// <summary>
        /// 用户是否手动最小化了面板（阻止自动展开）
        /// </summary>
        private bool _userMinimized;

        /// <summary>
        /// 选中行的描述信息
        /// </summary>
        [ObservableProperty]
        private string selectedRowInfo = string.Empty;

        /// <summary>
        /// 当前选中温度计的完整数据库实体（含脚本和帮助文档）
        /// </summary>
        private GeothermometerEntity? _selectedFullEntity;

        /// <summary>
        /// 上一次已应用的温度计（用于点击确定时恢复）
        /// </summary>
        private Geothermometer? _appliedPlugin;

        /// <summary>
        /// 上一次已应用的温度计对应的完整实体
        /// </summary>
        private GeothermometerEntity? _appliedFullEntity;

        /// <summary>
        /// 当前选中 GTM 的显示名称
        /// </summary>
        public string SelectedPluginDisplayName
        {
            get
            {
                if (SelectedPlugin == null) return string.Empty;
                if (!string.IsNullOrEmpty(SelectedPlugin.NameLangKey))
                {
                    var localized = LanguageService.Instance[SelectedPlugin.NameLangKey];
                    if (!string.IsNullOrEmpty(localized)) return localized;
                }
                return SelectedPlugin.Name;
            }
        }

        private RichTextBox? _helpRichTextBox;

        /// <summary>
        /// 标记是否已执行过自动检查更新（每次启动只检查一次）
        /// </summary>
        private static bool _hasAutoCheckedUpdate;

        /// <summary>
        /// 标记当前是否为自动检查更新（区分自动/手动，自动检查时已是最新版不弹窗）
        /// </summary>
        private bool _isAutoChecking;

        public GeothermometerPageViewModel()
        {
            // 注册开发者模式消息
            WeakReferenceMessenger.Default.RegisterAll(this);

            // 读取开发者模式配置
            if (bool.TryParse(ConfigHelper.GetConfig("developer_mode"), out bool devMode))
            {
                IsDeveloperMode = devMode;
            }

            // 初始化 GTM 服务
            GeothermometerService.Initialize();

            // 加载分组数据
            LoadMineralGroups();

            // 首次切换到温度计页面时，如果开启了自动检查更新，则自动检查
            TryAutoCheckUpdate();
        }

        /// <summary>
        /// 尝试自动检查地质温度计更新（仅在开启了设置且本次启动尚未检查过时执行）
        /// </summary>
        private async void TryAutoCheckUpdate()
        {
            if (_hasAutoCheckedUpdate) return;

            if (bool.TryParse(ConfigHelper.GetConfig("auto_check_gtm_update"), out bool autoCheck) && autoCheck)
            {
                _hasAutoCheckedUpdate = true;
                _isAutoChecking = true;
                try
                {
                    await CheckForPluginUpdates();
                }
                finally
                {
                    _isAutoChecking = false;
                }
            }
        }

        public void Receive(DeveloperModeChangedMessage message)
        {
            IsDeveloperMode = message.Value;
        }

        /// <summary>
        /// 设置帮助文档 RichTextBox 引用
        /// </summary>
        public void SetHelpRichTextBox(RichTextBox richTextBox)
        {
            _helpRichTextBox = richTextBox;
        }

        /// <summary>
        /// 加载矿物分组数据到 UI（官方按矿物分组，自定义为平面列表）
        /// </summary>
        private void LoadMineralGroups()
        {
            MineralGroups.Clear();
            CustomPlugins.Clear();

            // 官方温度计（按矿物分组）
            var officialGroups = GeothermometerService.GetGroupedEntities(true);
            foreach (var group in officialGroups)
            {
                var vm = new MineralGroupViewModel
                {
                    MineralKey = group.MineralKey,
                    DisplayName = group.DisplayName,
                    IconCode = group.IconCode,
                    IconColor = group.IconColor,
                    IsExpanded = false
                };

                foreach (var plugin in group.Plugins)
                {
                    vm.Plugins.Add(plugin);
                }

                MineralGroups.Add(vm);
            }

            // 自定义温度计（平面列表）
            var customList = GeothermometerService.GetCustomPlugins();
            foreach (var plugin in customList)
            {
                CustomPlugins.Add(plugin);
            }

            OfficialPluginCount = MineralGroups.Sum(g => g.Plugins.Count);
            CustomPluginCount = CustomPlugins.Count;
            TotalPluginCount = GeothermometerService.LoadedEntities.Count;
        }

        partial void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // 清空搜索时恢复原始分组
                LoadMineralGroups();
                return;
            }

            string keyword = value.Trim().ToLowerInvariant();
            MineralGroups.Clear();
            CustomPlugins.Clear();

            // 过滤官方温度计
            var officialGroups = GeothermometerService.GetGroupedEntities(true);
            foreach (var group in officialGroups)
            {
                var matchedPlugins = group.Plugins.Where(p =>
                    p.Name.ToLowerInvariant().Contains(keyword) ||
                    p.Mineral.ToLowerInvariant().Contains(keyword) ||
                    p.Author.ToLowerInvariant().Contains(keyword) ||
                    p.Description.ToLowerInvariant().Contains(keyword) ||
                    p.Year.ToString().Contains(keyword)
                ).ToList();

                if (matchedPlugins.Any())
                {
                    var vm = new MineralGroupViewModel
                    {
                        MineralKey = group.MineralKey,
                        DisplayName = group.DisplayName,
                        IconCode = group.IconCode,
                        IconColor = group.IconColor,
                        IsExpanded = true
                    };

                    foreach (var plugin in matchedPlugins)
                    {
                        vm.Plugins.Add(plugin);
                    }

                    MineralGroups.Add(vm);
                }
            }

            // 过滤自定义温度计
            var customList = GeothermometerService.GetCustomPlugins();
            foreach (var plugin in customList)
            {
                if (plugin.Name.ToLowerInvariant().Contains(keyword) ||
                    plugin.Mineral.ToLowerInvariant().Contains(keyword) ||
                    plugin.Author.ToLowerInvariant().Contains(keyword) ||
                    plugin.Description.ToLowerInvariant().Contains(keyword) ||
                    plugin.Year.ToString().Contains(keyword))
                {
                    CustomPlugins.Add(plugin);
                }
            }
        }

        /// <summary>
        /// 选中一个 GTM：加载完整实体并显示帮助文档
        /// </summary>
        [RelayCommand]
        private void SelectPlugin(Geothermometer plugin)
        {
            if (plugin == null) return;
            SelectedPlugin = plugin;
            IsPluginApplied = false;

            // 从数据库加载完整实体（含脚本和帮助文档）
            var entityId = GeothermometerDatabaseService.GenerateId(plugin.Id);
            _selectedFullEntity = GeothermometerDatabaseService.Instance.GetEntity(entityId);

            // 加载帮助文档并显示
            LoadHelpDocFromEntity();
            IsHelpDocVisible = true;

            OnPropertyChanged(nameof(SelectedPluginDisplayName));
        }

        /// <summary>
        /// 确认阅读完帮助文档，隐藏文档区域，恢复之前已应用的温度计界面
        /// </summary>
        [RelayCommand]
        private void ConfirmAndShowTable()
        {
            // 如果之前有已应用的温度计，恢复到该温度计的状态
            if (_appliedPlugin != null)
            {
                SelectedPlugin = _appliedPlugin;
                _selectedFullEntity = _appliedFullEntity;
                IsPluginApplied = true;
                OnPropertyChanged(nameof(SelectedPluginDisplayName));
            }

            IsHelpDocVisible = false;
        }

        /// <summary>
        /// 切换帮助文档显示状态
        /// </summary>
        [RelayCommand]
        private void ToggleHelpDocument()
        {
            if (SelectedPlugin == null) return;

            if (IsHelpDocVisible)
            {
                IsHelpDocVisible = false;
            }
            else
            {
                LoadHelpDocFromEntity();
                IsHelpDocVisible = true;
            }
        }

        /// <summary>
        /// 从数据库实体的 HelpDocuments 字段加载帮助文档到 RichTextBox
        /// </summary>
        private void LoadHelpDocFromEntity()
        {
            if (_helpRichTextBox == null) return;

            _helpRichTextBox.Document.Blocks.Clear();

            if (_selectedFullEntity == null || _selectedFullEntity.HelpDocuments == null
                || _selectedFullEntity.HelpDocuments.Count == 0)
            {
                var run = new Run(LanguageService.Instance["geo_help_no_doc_text"])
                {
                    Foreground = Brushes.Gray,
                    FontSize = 14
                };
                _helpRichTextBox.Document.Blocks.Add(new Paragraph(run));
                return;
            }

            try
            {
                string langCode = LanguageService.GetLanguage();
                string rtfContent = null;

                if (_selectedFullEntity.HelpDocuments.ContainsKey(langCode))
                    rtfContent = _selectedFullEntity.HelpDocuments[langCode];
                else if (_selectedFullEntity.HelpDocuments.ContainsKey("en-US"))
                    rtfContent = _selectedFullEntity.HelpDocuments["en-US"];
                else
                    rtfContent = _selectedFullEntity.HelpDocuments.Values.FirstOrDefault();

                if (!string.IsNullOrEmpty(rtfContent))
                {
                    RtfHelper.LoadRtfString(_helpRichTextBox, rtfContent);
                }
                else
                {
                    var run = new Run(LanguageService.Instance["geo_help_no_doc_text"])
                    {
                        Foreground = Brushes.Gray,
                        FontSize = 14
                    };
                    _helpRichTextBox.Document.Blocks.Add(new Paragraph(run));
                }
            }
            catch (Exception ex)
            {
                MessageHelper.Error(string.Format(LanguageService.Instance["geo_msg_load_help_doc_failed"], ex.Message));
            }
        }

        /// <summary>
        /// 导出当前选中的温度计为 ZIP 文件
        /// </summary>
        [RelayCommand]
        private async Task ExportPlugin()
        {
            if (_selectedFullEntity == null || SelectedPlugin == null)
            {
                MessageHelper.Info(LanguageService.Instance["geo_msg_select_thermometer_first"]);
                return;
            }

            try
            {
                string filePath = await FileHelper.GetSaveFilePath2Async(
                    title: LanguageService.Instance["geo_msg_export_dialog_title"],
                    filter: "ZIP files (*.zip)|*.zip",
                    defaultExt: ".zip",
                    defaultFileName: SelectedPlugin.Name);
                if (string.IsNullOrEmpty(filePath)) return;

                var entityId = _selectedFullEntity.Id;
                await Task.Run(() => GeothermometerService.ExportToZip(entityId, filePath));
                MessageHelper.Success(LanguageService.Instance["geo_msg_export_success"]);
            }
            catch (Exception ex)
            {
                MessageHelper.Error(string.Format(LanguageService.Instance["geo_msg_export_failed"], ex.Message));
            }
        }

        /// <summary>
        /// 从 ZIP 文件导入 GTM
        /// </summary>
        [RelayCommand]
        private void ImportPlugin()
        {
            try
            {
                string filePath = FileHelper.GetFilePath("ZIP files (*.zip)|*.zip");
                if (string.IsNullOrEmpty(filePath)) return;

                var entity = GeothermometerService.ImportFromZip(filePath);
                if (entity != null)
                {
                    GeothermometerService.ReloadPlugins();
                    LoadMineralGroups();
                    MessageHelper.Success(LanguageService.Instance["geo_msg_import_success"]);
                }
                else
                {
                    MessageHelper.Error(LanguageService.Instance["geo_msg_import_failed"]);
                }
            }
            catch (Exception ex)
            {
                MessageHelper.Error(string.Format(LanguageService.Instance["geo_msg_import_failed_detail"], ex.Message));
            }
        }

        /// <summary>
        /// 应用选中的 GTM 到 ReoGrid 表格
        /// </summary>
        [RelayCommand]
        private async Task ApplyPlugin(ReoGridControl reoGridControl)
        {
            if (SelectedPlugin == null || reoGridControl == null) return;

            try
            {
                bool isConfirmed = await MessageHelper.ShowAsyncDialog(
                    LanguageService.Instance["geo_dialog_apply_overwrite"],
                    LanguageService.Instance["Cancel"],
                    LanguageService.Instance["Confirm"]);

                if (!isConfirmed) return;

                Worksheet worksheet = reoGridControl.CurrentWorksheet;

                var currentScale = worksheet.ScaleFactor;
                worksheet.Reset();
                worksheet.ScaleFactor = currentScale;

                // 设置整个工作表默认居中对齐
                worksheet.SetRangeStyles(RangePosition.EntireRange, new WorksheetRangeStyle
                {
                    Flag = PlainStyleFlag.HorizontalAlign,
                    HAlign = ReoGridHorAlign.Center
                });

                var headers = SelectedPlugin.Headers;
                var exampleRow = SelectedPlugin.ExampleRow;

                // 设置表头
                for (int i = 0; i < headers.Count; i++)
                {
                    worksheet[0, i] = headers[i];
                    worksheet.Cells[0, i].IsReadOnly = true;
                }

                // 设置示例数据
                for (int i = 0; i < exampleRow.Count && i < headers.Count; i++)
                {
                    worksheet[1, i] = exampleRow[i];
                }

                // 设置表头样式
                var headerRange = new RangePosition(0, 0, 1, headers.Count);
                worksheet.SetRangeStyles(headerRange, new WorksheetRangeStyle
                {
                    Flag = PlainStyleFlag.BackColor | PlainStyleFlag.TextColor |
                           PlainStyleFlag.FontStyleBold | PlainStyleFlag.HorizontalAlign,
                    HAlign = ReoGridHorAlign.Center,
                    BackColor = Colors.LightGray,
                    TextColor = Colors.Black,
                    Bold = true
                });

                // 设置示例行样式
                var exampleRange = new RangePosition(1, 0, 1, exampleRow.Count);
                worksheet.SetRangeStyles(exampleRange, new WorksheetRangeStyle
                {
                    Flag = PlainStyleFlag.BackColor | PlainStyleFlag.TextColor |
                           PlainStyleFlag.FontStyleBold | PlainStyleFlag.HorizontalAlign,
                    HAlign = ReoGridHorAlign.Center,
                    TextColor = Colors.Black,
                });

                // 设置列宽和温度列格式
                for (int i = 0; i < headers.Count; i++)
                {
                    worksheet.AutoFitColumnWidth(i);
                    var currentWidth = worksheet.GetColumnWidth(i);
                    worksheet.SetColumnsWidth(i, 1, (ushort)(currentWidth + 10));

                    if (headers[i].Contains("T(K)") || headers[i].Contains("T(\u2103)"))
                    {
                        var range = new RangePosition(1, i, worksheet.RowCount - 1, 1);
                        worksheet.SetRangeDataFormat(range,
                            unvell.ReoGrid.DataFormat.CellDataFormatFlag.Number,
                            new unvell.ReoGrid.DataFormat.NumberDataFormatter.NumberFormatArgs
                            {
                                DecimalPlaces = 1
                            });
                    }
                }

                // 冻结表头行，使滚动时始终显示
                worksheet.FreezeToCell(1, 0);

                // 记录已应用的温度计，用于确定按钮恢复
                _appliedPlugin = SelectedPlugin;
                _appliedFullEntity = _selectedFullEntity;

                // 应用后确保切换到表格视图
                IsHelpDocVisible = false;
                IsPluginApplied = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("创建表格失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 处理 ReoGrid 行选中事件，提取输入参数并计算中间步骤
        /// 由 View 的 code-behind 调用
        /// </summary>
        public void OnRowSelected(Worksheet worksheet, int row)
        {
            if (_selectedFullEntity == null || worksheet == null || row < 1)
            {
                ClearCalculationData();
                return;
            }

            var inputColumns = _selectedFullEntity.InputColumns;
            if (inputColumns == null || inputColumns.Count == 0)
            {
                ClearCalculationData();
                return;
            }

            var headers = _selectedFullEntity.Headers;
            if (headers == null || headers.Count == 0)
            {
                ClearCalculationData();
                return;
            }

            try
            {
                // 根据 InputColumns 定义，从表头中找到对应列索引，提取该行数据
                var inputValues = new List<double>();
                bool allValid = true;

                foreach (var inputCol in inputColumns)
                {
                    int colIndex = headers.IndexOf(inputCol);
                    if (colIndex < 0)
                    {
                        allValid = false;
                        break;
                    }

                    var cellData = worksheet.GetCellData(row, colIndex);
                    if (cellData != null && double.TryParse(cellData.ToString(), out double val))
                    {
                        inputValues.Add(val);
                    }
                    else
                    {
                        allValid = false;
                        break;
                    }
                }

                if (!allValid || inputValues.Count == 0)
                {
                    ClearCalculationData();
                    return;
                }

                // 调用 calculateDetailed
                var steps = GeothermometerService.ExecuteDetailedCalculation(
                    _selectedFullEntity, inputValues.ToArray());

                CalculationSteps.Clear();
                foreach (var step in steps)
                {
                    CalculationSteps.Add(step);
                }

                if (steps.Count > 0)
                {
                    SelectedRowInfo = $"Row {row} - {SelectedPluginDisplayName}";
                    // 用户手动最小化后不再自动展开
                    if (!_userMinimized)
                    {
                        IsDetailPanelMinimized = false;
                    }
                    HasCalculationData = true;
                }
                else
                {
                    ClearCalculationData();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ViewModel] 详细计算失败: {ex.Message}");
                ClearCalculationData();
            }
        }

        /// <summary>
        /// 清除计算数据，回到最小化状态
        /// </summary>
        private void ClearCalculationData()
        {
            CalculationSteps.Clear();
            HasCalculationData = false;
            IsDetailPanelMaximized = false;
            IsDetailPanelMinimized = true;
            SelectedRowInfo = string.Empty;
        }

        /// <summary>
        /// 最小化计算详情面板（收起为底部长条）
        /// </summary>
        [RelayCommand]
        private void MinimizeDetailPanel()
        {
            IsDetailPanelMinimized = true;
            IsDetailPanelMaximized = false;
            _userMinimized = true;
        }

        /// <summary>
        /// 切换最大化/还原计算详情面板
        /// </summary>
        [RelayCommand]
        private void ToggleMaximizeDetailPanel()
        {
            if (IsDetailPanelMaximized)
            {
                // 从最大化恢复到正常
                IsDetailPanelMaximized = false;
            }
            else
            {
                // 最大化
                IsDetailPanelMaximized = true;
                IsDetailPanelMinimized = false;
            }
            _userMinimized = false;
        }

        /// <summary>
        /// 从最小化状态还原计算详情面板
        /// </summary>
        [RelayCommand]
        private void RestoreDetailPanel()
        {
            IsDetailPanelMinimized = false;
            _userMinimized = false;
        }

        /// <summary>
        /// 检查 GTM 更新
        /// </summary>
        [RelayCommand]
        private async Task CheckForPluginUpdates()
        {
            if (IsCheckingUpdates) return;

            IsCheckingUpdates = true;
            UpdateStatusText = LanguageService.Instance["geo_msg_checking_update"];

            try
            {
                var updates = await GeothermometerService.CheckForUpdatesAsync();
                AvailableUpdateCount = updates.Count;

                if (updates.Count > 0)
                {
                    string msg = string.Format(LanguageService.Instance["geo_msg_update_available"], updates.Count);
                    UpdateStatusText = msg;

                    bool confirmed = await MessageHelper.ShowAsyncDialog(
                        msg,
                        LanguageService.Instance["Cancel"],
                        LanguageService.Instance["Confirm"]);

                    if (confirmed)
                    {
                        UpdateStatusText = LanguageService.Instance["geo_msg_downloading_update"];
                        int count = await GeothermometerService.DownloadAndReloadAsync(updates);
                        LoadMineralGroups();

                        string result = string.Format(LanguageService.Instance["geo_msg_update_downloaded"], count);
                        MessageHelper.Success(result);
                        UpdateStatusText = result;
                    }
                    else
                    {
                        UpdateStatusText = string.Empty;
                    }
                }
                else
                {
                    UpdateStatusText = LanguageService.Instance["geo_msg_already_latest"];
                    if (!_isAutoChecking)
                    {
                        MessageHelper.Info(UpdateStatusText);
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText = LanguageService.Instance["geo_msg_check_update_failed"];
                MessageHelper.Error($"{UpdateStatusText}: {ex.Message}");
            }
            finally
            {
                IsCheckingUpdates = false;
            }
        }

        /// <summary>
        /// 打开数据文件
        /// </summary>
        [RelayCommand]
        public void OpenExcelFile(ReoGridControl reoGridControl)
        {
            string filePath = FileHelper.GetFilePath(LanguageService.Instance["csv_file_filter"]);
            if (filePath != null)
            {
                reoGridControl.Load(filePath);
                MessageHelper.Success(LanguageService.Instance["geo_msg_import_success"]);
            }
            else
            {
                MessageHelper.Info(LanguageService.Instance["geo_msg_import_cancelled"]);
            }
        }

        /// <summary>
        /// 导出当前工作表
        /// </summary>
        [RelayCommand]
        public async Task ExportWorksheet(ReoGridControl reoGridControl)
        {
            var worksheet = reoGridControl.CurrentWorksheet;
            if (worksheet == null) return;

            string tempFilePath = await FileHelper.GetSaveFilePath2Async(
                title: LanguageService.Instance["geo_msg_csv_save_title"],
                filter: LanguageService.Instance["csv_file_filter"],
                defaultExt: ".csv",
                defaultFileName: worksheet.Name);
            if (string.IsNullOrEmpty(tempFilePath)) return;

            try
            {
                var range = worksheet.UsedRange;
                var csvBuilder = new StringBuilder();

                for (int r = range.Row; r <= range.EndRow; r++)
                {
                    var rowValues = new List<string>();
                    for (int c = range.Col; c <= range.EndCol; c++)
                    {
                        string cellValue = worksheet.GetCellText(r, c) ?? "";
                        if (cellValue.Contains(",") || cellValue.Contains("\""))
                        {
                            cellValue = $"\"{cellValue.Replace("\"", "\"\"")}\"";
                        }
                        rowValues.Add(cellValue);
                    }
                    csvBuilder.AppendLine(string.Join(",", rowValues));
                }

                var csvContent = csvBuilder.ToString();
                await Task.Run(() => File.WriteAllText(tempFilePath, csvContent, new UTF8Encoding(true)));
                MessageHelper.Success(LanguageService.Instance["geo_msg_export_success"]);
            }
            catch (Exception ex)
            {
                MessageHelper.Error(string.Format(LanguageService.Instance["geo_msg_export_failed"], ex.Message));
            }
        }

        /// <summary>
        /// 新建工作表
        /// </summary>
        [RelayCommand]
        public void CreateWorkSheet(ReoGridControl reoGridControl)
        {
            try
            {
                Worksheet newWorksheet = reoGridControl.CreateWorksheet();
                reoGridControl.Worksheets.Add(newWorksheet);
                reoGridControl.CurrentWorksheet = newWorksheet;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(LanguageService.Instance["geo_msg_create_table_failed"], ex.Message));
            }
        }

        // ==================== 温度计管理 ====================

        /// <summary>
        /// 新建温度计（打开编辑器窗口）
        /// </summary>
        [RelayCommand]
        private void CreateCustomThermometer()
        {
            var editorVm = new GeothermometerEditorViewModel();
            editorVm.OnSaved = () =>
            {
                GeothermometerService.ReloadPlugins();
                LoadMineralGroups();
            };

            var window = new GeothermometerEditorWindow(editorVm);
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }

        /// <summary>
        /// 编辑温度计（支持官方和自定义）
        /// </summary>
        [RelayCommand]
        private void EditCustomThermometer(Geothermometer plugin)
        {
            if (plugin == null) return;

            var entityId = GeothermometerDatabaseService.GenerateId(plugin.Id);
            var entity = GeothermometerDatabaseService.Instance.GetEntity(entityId);
            if (entity == null) return;

            var editorVm = new GeothermometerEditorViewModel();
            editorVm.LoadEntity(entity);
            editorVm.OnSaved = () =>
            {
                GeothermometerService.ReloadPlugins();
                LoadMineralGroups();
            };

            var window = new GeothermometerEditorWindow(editorVm);
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }

        /// <summary>
        /// 删除温度计（支持官方和自定义）
        /// </summary>
        [RelayCommand]
        private async Task DeleteCustomThermometer(Geothermometer plugin)
        {
            if (plugin == null) return;

            string typeLabel = plugin.IsBuiltIn
                ? LanguageService.Instance["geo_label_official"]
                : LanguageService.Instance["geo_label_custom"];
            bool confirmed = await MessageHelper.ShowAsyncDialog(
                string.Format(LanguageService.Instance["geo_dialog_delete_thermometer"], typeLabel, plugin.Name),
                LanguageService.Instance["Cancel"],
                LanguageService.Instance["Confirm"]);

            if (!confirmed) return;

            var entityId = GeothermometerDatabaseService.GenerateId(plugin.Id);
            if (GeothermometerService.DeleteEntity(entityId))
            {
                LoadMineralGroups();
                MessageHelper.Success(LanguageService.Instance["geo_msg_delete_success"]);
            }
        }

        /// <summary>
        /// 转换为官方温度计
        /// </summary>
        [RelayCommand]
        private void ConvertToOfficial(Geothermometer plugin)
        {
            if (plugin == null) return;

            var entityId = GeothermometerDatabaseService.GenerateId(plugin.Id);
            if (GeothermometerService.ConvertToOfficial(entityId))
            {
                LoadMineralGroups();
                MessageHelper.Success(LanguageService.Instance["geo_msg_promoted"]);
            }
        }

        /// <summary>
        /// 降级为自定义温度计
        /// </summary>
        [RelayCommand]
        private void ConvertToCustom(Geothermometer plugin)
        {
            if (plugin == null) return;

            var entityId = GeothermometerDatabaseService.GenerateId(plugin.Id);
            if (GeothermometerService.ConvertToCustom(entityId))
            {
                LoadMineralGroups();
                MessageHelper.Success(LanguageService.Instance["geo_msg_demoted"]);
            }
        }

        /// <summary>
        /// 增量导出官方温度计到目录（含 GeoT-index.json + GeoT-List.json）
        /// </summary>
        [RelayCommand]
        private void ExportAllOfficial()
        {
            try
            {
                string folderPath = FileHelper.GetFolderPath();
                if (string.IsNullOrEmpty(folderPath)) return;

                var (exported, total) = GeothermometerService.ExportAllOfficialToDirectory(folderPath);
                MessageHelper.Success(string.Format(LanguageService.Instance["geo_msg_batch_export_success"], total, exported, folderPath));
            }
            catch (Exception ex)
            {
                MessageHelper.Error(string.Format(LanguageService.Instance["geo_msg_batch_export_failed"], ex.Message));
            }
        }

        /// <summary>
        /// 展开/折叠矿物分组
        /// </summary>
        [RelayCommand]
        private void ToggleMineralGroup(MineralGroupViewModel group)
        {
            if (group == null) return;
            group.IsExpanded = !group.IsExpanded;
        }

        /// <summary>
        /// 生成唯一的工作表名称
        /// </summary>
        private string GetUniqueWorksheetName(ReoGridControl reoGridControl, string baseName)
        {
            string uniqueName = baseName;
            int counter = 1;

            while (WorksheetExists(reoGridControl, uniqueName))
            {
                uniqueName = $"{baseName}_{counter}";
                counter++;
            }

            return uniqueName;
        }

        private bool WorksheetExists(ReoGridControl reoGridControl, string worksheetName)
        {
            foreach (var worksheet in reoGridControl.Worksheets)
            {
                if (worksheet.Name.Equals(worksheetName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 矿物分组 ViewModel（支持 UI 绑定和展开/折叠）
    /// </summary>
    public partial class MineralGroupViewModel : ObservableObject
    {
        public string MineralKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string IconCode { get; set; } = string.Empty;
        public string IconColor { get; set; } = "#555555";

        [ObservableProperty]
        private bool isExpanded;

        public ObservableCollection<Geothermometer> Plugins { get; set; } = new();

        public int PluginCount => Plugins.Count;
    }
}
