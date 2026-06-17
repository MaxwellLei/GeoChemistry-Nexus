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
using System.ComponentModel;
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
using unvell.ReoGrid.Events;

namespace GeoChemistryNexus.ViewModels
{
    public partial class GeothermometerPageViewModel : ObservableObject,
        IRecipient<DeveloperModeChangedMessage>,
        IRecipient<GeoTMineralCategoryUpdatedMessage>
    {
        /// <summary>
        /// 应用温压计后工作表的初始行数（含表头与示例行）
        /// </summary>
        private const int InitialWorksheetRowCount = 500;

        private Worksheet? _rowExpansionWorksheet;
        /// <summary>
        /// 官方温压计类别分组列表（用于左侧导航）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<CategoryGroupViewModel> categoryGroups = new();

        /// <summary>
        /// 当前选中的 GTM
        /// </summary>
        [ObservableProperty]
        private Geothermometer? selectedPlugin;

        /// <summary>
        /// 是否显示帮助文档区域（选中温压计时显示，点击确认后隐藏）
        /// </summary>
        [ObservableProperty]
        private bool isHelpDocVisible;

        /// <summary>
        /// 当前选中的温压计是否已应用（控制顶部按钮显示逻辑）
        /// </summary>
        [ObservableProperty]
        private bool isPluginApplied;

        /// <summary>
        /// 是否正在检查更新
        /// </summary>
        [ObservableProperty]
        private bool isCheckingUpdates;

        /// <summary>
        /// 更新遮罩层是否可见
        /// </summary>
        [ObservableProperty]
        private bool isUpdateOverlayVisible;

        /// <summary>
        /// 更新进度（0-100）
        /// </summary>
        [ObservableProperty]
        private double updateProgress;

        /// <summary>
        /// 更新进度条是否为不确定模式（检查更新阶段）
        /// </summary>
        [ObservableProperty]
        private bool isUpdateProgressIndeterminate = true;

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
        /// 自定义温压计列表（平面列表，不按矿物分组）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Geothermometer> customPlugins = new();

        /// <summary>
        /// 官方温压计数量
        /// </summary>
        [ObservableProperty]
        private int officialPluginCount;

        /// <summary>
        /// 自定义温压计数量
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
        /// 上一次用于详细计算的行输入值（语言切换时用于刷新步骤注释）
        /// </summary>
        private double[]? _lastCalculationInputValues;

        /// <summary>
        /// 选中行的描述信息
        /// </summary>
        [ObservableProperty]
        private string selectedRowInfo = string.Empty;

        /// <summary>
        /// 当前选中温压计的完整数据库实体（含脚本和帮助文档）
        /// </summary>
        private GeothermometerEntity? _selectedFullEntity;

        /// <summary>
        /// 上一次已应用的温压计（用于点击确定时恢复）
        /// </summary>
        private Geothermometer? _appliedPlugin;

        /// <summary>
        /// 上一次已应用的温压计对应的完整实体
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
            LoadCategoryGroups();

            LanguageService.Instance.PropertyChanged += OnAppLanguageChanged;

            // 首次切换到温压计页面时，如果开启了自动检查更新，则自动检查
            TryAutoCheckUpdate();
        }

        private void OnAppLanguageChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Item[]")
                return;

            ReloadCategoryGroupsAndSelection();

            if (_lastCalculationInputValues == null || _selectedFullEntity == null || !HasCalculationData)
                return;

            ApplyCalculationSteps(_lastCalculationInputValues);
        }

        private void ReloadCategoryGroupsAndSelection()
        {
            string? selectedPluginId = SelectedPlugin?.Id;
            string? appliedPluginId = _appliedPlugin?.Id;

            LoadCategoryGroups();

            if (!string.IsNullOrEmpty(selectedPluginId))
            {
                SelectedPlugin = FindPluginById(selectedPluginId);
                OnPropertyChanged(nameof(SelectedPluginDisplayName));
            }

            if (!string.IsNullOrEmpty(appliedPluginId))
            {
                _appliedPlugin = FindPluginById(appliedPluginId);
            }
        }

        private Geothermometer? FindPluginById(string pluginId)
        {
            foreach (var group in CategoryGroups)
            {
                var plugin = group.Plugins.FirstOrDefault(p => p.Id == pluginId);
                if (plugin != null)
                    return plugin;
            }

            return CustomPlugins.FirstOrDefault(p => p.Id == pluginId);
        }

        /// <summary>
        /// 尝试自动检查地质温压计更新（仅在开启了设置且本次启动尚未检查过时执行）
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

        public void Receive(GeoTMineralCategoryUpdatedMessage message)
        {
            ReloadCategoryGroupsAndSelection();
        }

        /// <summary>
        /// 设置帮助文档 RichTextBox 引用
        /// </summary>
        public void SetHelpRichTextBox(RichTextBox richTextBox)
        {
            _helpRichTextBox = richTextBox;
        }

        /// <summary>
        /// 加载官方温压计类别分组数据到 UI
        /// </summary>
        private void LoadCategoryGroups()
        {
            CategoryGroups.Clear();
            CustomPlugins.Clear();

            var officialGroups = GeothermometerService.GetGroupedEntities(true);
            foreach (var group in officialGroups)
            {
                var vm = new CategoryGroupViewModel
                {
                    CategoryKey = group.CategoryKey,
                    DisplayName = group.DisplayName,
                    IsExpanded = false
                };

                foreach (var plugin in group.Plugins)
                {
                    vm.Plugins.Add(plugin);
                }

                CategoryGroups.Add(vm);
            }

            var customList = GeothermometerService.GetCustomPlugins();
            foreach (var plugin in customList)
            {
                CustomPlugins.Add(plugin);
            }

            OfficialPluginCount = CategoryGroups.Sum(g => g.Plugins.Count);
            CustomPluginCount = CustomPlugins.Count;
            TotalPluginCount = GeothermometerService.LoadedEntities.Count;
        }

        partial void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // 清空搜索时恢复原始分组
                LoadCategoryGroups();
                return;
            }

            string keyword = value.Trim().ToLowerInvariant();
            CategoryGroups.Clear();
            CustomPlugins.Clear();

            var officialGroups = GeothermometerService.GetGroupedEntities(true);
            foreach (var group in officialGroups)
            {
                var matchedPlugins = group.Plugins.Where(p =>
                    p.Name.ToLowerInvariant().Contains(keyword) ||
                    p.TagsDisplayText.ToLowerInvariant().Contains(keyword) ||
                    GeoTCategoryHelper.GetDisplayName(p.Category).ToLowerInvariant().Contains(keyword) ||
                    p.Author.ToLowerInvariant().Contains(keyword) ||
                    p.Year.ToString().Contains(keyword)
                ).ToList();

                if (matchedPlugins.Any())
                {
                    var vm = new CategoryGroupViewModel
                    {
                        CategoryKey = group.CategoryKey,
                        DisplayName = group.DisplayName,
                        IsExpanded = true
                    };

                    foreach (var plugin in matchedPlugins)
                    {
                        vm.Plugins.Add(plugin);
                    }

                    CategoryGroups.Add(vm);
                }
            }

            var customList = GeothermometerService.GetCustomPlugins();
            foreach (var plugin in customList)
            {
                if (plugin.Name.ToLowerInvariant().Contains(keyword) ||
                    plugin.TagsDisplayText.ToLowerInvariant().Contains(keyword) ||
                    GeoTCategoryHelper.GetDisplayName(plugin.Category).ToLowerInvariant().Contains(keyword) ||
                    plugin.Author.ToLowerInvariant().Contains(keyword) ||
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
        /// 确认阅读完帮助文档，隐藏文档区域，恢复之前已应用的温压计界面
        /// </summary>
        [RelayCommand]
        private void ConfirmAndShowTable()
        {
            // 如果之前有已应用的温压计，恢复到该温压计的状态
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
        /// 导出指定温压计为 ZIP 文件
        /// </summary>
        [RelayCommand]
        private async Task ExportPlugin(Geothermometer? plugin)
        {
            plugin ??= SelectedPlugin;
            if (plugin == null)
            {
                MessageHelper.Info(LanguageService.Instance["geo_msg_select_thermometer_first"]);
                return;
            }

            var entityId = GeothermometerDatabaseService.GenerateId(plugin.Id);
            var entity = GeothermometerDatabaseService.Instance.GetEntity(entityId);
            if (entity == null)
            {
                MessageHelper.Error(LanguageService.Instance["geo_msg_export_failed"]);
                return;
            }

            try
            {
                string filePath = await FileHelper.GetSaveFilePath2Async(
                    title: LanguageService.Instance["geo_msg_export_dialog_title"],
                    filter: "ZIP files (*.zip)|*.zip",
                    defaultExt: ".zip",
                    defaultFileName: plugin.Name);
                if (string.IsNullOrEmpty(filePath)) return;

                await Task.Run(() => GeothermometerService.ExportToZip(entity.Id, filePath));
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
            string filePath = FileHelper.GetFilePath("ZIP files (*.zip)|*.zip");
            ImportPluginFromPath(filePath);
        }

        /// <summary>
        /// 从指定 ZIP 路径导入 GTM（支持拖入文件）
        /// </summary>
        [RelayCommand]
        private void ImportPluginFromPath(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            try
            {
                GeothermometerService.ImportFromZip(filePath);
                GeothermometerService.ReloadPlugins();
                LoadCategoryGroups();
                MessageHelper.Success(LanguageService.Instance["geo_msg_import_success"]);
            }
            catch (GeothermometerImportException ex)
            {
                string message = ex.Reason switch
                {
                    GeothermometerImportFailureReason.VersionIncompatible =>
                        LanguageService.Instance["template_version_too_high"],
                    _ => LanguageService.Instance["geo_msg_import_invalid_format"]
                };
                MessageHelper.Error(message);
            }
            catch (InvalidOperationException ex)
            {
                MessageHelper.Error(string.Format(
                    LanguageService.Instance["geo_msg_import_formula_name_rejected"],
                    ex.Message));
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

                Worksheet? worksheet = reoGridControl.CurrentWorksheet;
                if (worksheet == null)
                {
                    if (reoGridControl.Worksheets.Count == 0) return;
                    worksheet = reoGridControl.Worksheets[0];
                    reoGridControl.CurrentWorksheet = worksheet;
                }

                var currentScale = worksheet.ScaleFactor;
                worksheet.Reset();
                worksheet.ScaleFactor = currentScale;
                worksheet.RowCount = InitialWorksheetRowCount;

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

                    if (IsTemperatureHeader(headers[i]))
                    {
                        var range = new RangePosition(1, i, worksheet.RowCount - 1, 1);
                        worksheet.SetRangeDataFormat(range,
                            unvell.ReoGrid.DataFormat.CellDataFormatFlag.Number,
                            new unvell.ReoGrid.DataFormat.NumberDataFormatter.NumberFormatArgs
                            {
                                DecimalPlaces = 0
                            });
                    }
                }

                // 冻结表头行，使滚动时始终显示
                worksheet.FreezeToCell(1, 0);

                AttachWorksheetRowExpansionEvents(worksheet);

                // 记录已应用的温压计，用于确定按钮恢复
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
        /// 绑定工作表行扩展相关事件（粘贴扩行、末行编辑自动增行）
        /// </summary>
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
                worksheet.RowCount = worksheet.RowCount + 1;
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

                _lastCalculationInputValues = inputValues.ToArray();
                var steps = ApplyCalculationSteps(_lastCalculationInputValues);

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

        private List<CalculationStep> ApplyCalculationSteps(double[] inputValues)
        {
            if (_selectedFullEntity == null)
                return new List<CalculationStep>();

            var steps = GeothermometerService.ExecuteDetailedCalculation(_selectedFullEntity, inputValues);
            CalculationSteps.Clear();
            foreach (var step in steps)
            {
                CalculationSteps.Add(step);
            }

            return steps;
        }

        /// <summary>
        /// 清除计算数据，回到最小化状态
        /// </summary>
        private void ClearCalculationData()
        {
            _lastCalculationInputValues = null;
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

        private void ShowUpdateOverlay(bool indeterminate, string statusText, double progress = 0)
        {
            if (_isAutoChecking && indeterminate)
                return;

            IsUpdateProgressIndeterminate = indeterminate;
            UpdateProgress = progress;
            UpdateStatusText = statusText;
            IsUpdateOverlayVisible = true;
        }

        private void HideUpdateOverlay()
        {
            IsUpdateOverlayVisible = false;
            IsUpdateProgressIndeterminate = true;
            UpdateProgress = 0;
        }

        /// <summary>
        /// 检查 GTM 更新
        /// </summary>
        [RelayCommand]
        private async Task CheckForPluginUpdates()
        {
            if (IsCheckingUpdates) return;

            IsCheckingUpdates = true;
            ShowUpdateOverlay(true, LanguageService.Instance["geo_msg_checking_update"]);

            try
            {
                var checkResult = await GeothermometerService.CheckForUpdatesAsync();

                if (checkResult.Status == GeothermometerUpdateCheckStatus.Failed)
                {
                    UpdateStatusText = LanguageService.Instance["geo_msg_check_update_failed"];
                    if (!_isAutoChecking)
                    {
                        MessageHelper.Error($"{UpdateStatusText}: {checkResult.ErrorMessage}");
                    }
                    return;
                }

                int changeCount = checkResult.Updates.Count + checkResult.Removals.Count;
                AvailableUpdateCount = changeCount;

                if (checkResult.HasChanges)
                {
                    string msg = string.Format(LanguageService.Instance["geo_msg_update_available"], changeCount);
                    UpdateStatusText = msg;

                    HideUpdateOverlay();

                    bool confirmed = await MessageHelper.ShowAsyncDialog(
                        msg,
                        LanguageService.Instance["Cancel"],
                        LanguageService.Instance["Confirm"]);

                    if (confirmed)
                    {
                        ShowUpdateOverlay(false, LanguageService.Instance["geo_msg_downloading_update"]);

                        var progress = new Progress<(int current, int total, string name)>(p =>
                        {
                            UpdateProgress = p.total > 0 ? (double)p.current / p.total * 100 : 0;
                            UpdateStatusText = string.Format(
                                LanguageService.Instance["batch_download_progress"],
                                p.current,
                                p.total,
                                p.name);
                        });

                        int count = await GeothermometerService.DownloadAndReloadAsync(
                            checkResult.Updates,
                            checkResult.Removals,
                            progress);

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

                if (checkResult.MineralCategoriesSynced)
                    ReloadCategoryGroupsAndSelection();
            }
            catch (Exception ex)
            {
                UpdateStatusText = LanguageService.Instance["geo_msg_check_update_failed"];
                MessageHelper.Error($"{UpdateStatusText}: {ex.Message}");
            }
            finally
            {
                HideUpdateOverlay();
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

        // ==================== 温压计管理 ====================

        /// <summary>
        /// 新建温压计（打开编辑器窗口）
        /// </summary>
        [RelayCommand]
        private void CreateCustomThermometer()
        {
            var editorVm = new GeothermometerEditorViewModel();
            editorVm.OnSaved = () =>
            {
                GeothermometerService.ReloadPlugins();
                LoadCategoryGroups();
            };

            var window = new GeothermometerEditorWindow(editorVm);
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }

        /// <summary>
        /// 编辑温压计（支持官方和自定义）
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
                LoadCategoryGroups();
            };

            var window = new GeothermometerEditorWindow(editorVm);
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }

        /// <summary>
        /// 删除温压计（支持官方和自定义）
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
                LoadCategoryGroups();
                MessageHelper.Success(LanguageService.Instance["geo_msg_delete_success"]);
            }
        }

        /// <summary>
        /// 转换为官方温压计
        /// </summary>
        [RelayCommand]
        private void ConvertToOfficial(Geothermometer plugin)
        {
            if (plugin == null) return;

            var entityId = GeothermometerDatabaseService.GenerateId(plugin.Id);
            if (GeothermometerService.ConvertToOfficial(entityId))
            {
                LoadCategoryGroups();
                MessageHelper.Success(LanguageService.Instance["geo_msg_promoted"]);
            }
        }

        /// <summary>
        /// 降级为自定义温压计
        /// </summary>
        [RelayCommand]
        private void ConvertToCustom(Geothermometer plugin)
        {
            if (plugin == null) return;

            var entityId = GeothermometerDatabaseService.GenerateId(plugin.Id);
            if (GeothermometerService.ConvertToCustom(entityId))
            {
                LoadCategoryGroups();
                MessageHelper.Success(LanguageService.Instance["geo_msg_demoted"]);
            }
        }

        /// <summary>
        /// 展开/折叠类别分组
        /// </summary>
        [RelayCommand]
        private void ToggleCategoryGroup(CategoryGroupViewModel group)
        {
            if (group == null) return;
            group.IsExpanded = !group.IsExpanded;
        }

        /// <summary>
        /// 判断表头是否为温度列（T(℃) 或 T(K)）
        /// </summary>
        private static bool IsTemperatureHeader(string header)
        {
            if (string.IsNullOrWhiteSpace(header))
                return false;

            return header.Contains("T(K)", StringComparison.OrdinalIgnoreCase)
                || header.Contains("T(\u2103)", StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// 温压计类别分组 ViewModel（支持 UI 绑定和展开/折叠）
    /// </summary>
    public partial class CategoryGroupViewModel : ObservableObject
    {
        public string CategoryKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        [ObservableProperty]
        private bool isExpanded;

        public ObservableCollection<Geothermometer> Plugins { get; set; } = new();

        public int PluginCount => Plugins.Count;
    }
}
