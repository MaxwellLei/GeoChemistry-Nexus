using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Controls;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Interfaces;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Views;
using Jint;
using Ookii.Dialogs.Wpf;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Net.Http;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using unvell.ReoGrid;

namespace GeoChemistryNexus.ViewModels
{
    public partial class MainPlotViewModel : ObservableObject, IRecipient<PickPointRequestMessage>, IRecipient<DefaultTreeExpandLevelChangedMessage>, IRecipient<ScriptValidatedMessage>, IRecipient<DeveloperModeChangedMessage>
    {
        // 拾取点模式
        [ObservableProperty]
        private bool _isPickingPointMode = false;

        // 待更新的 PointDefinition 对象
        private PointDefinition? _targetPointDefinition;

        // 用于鼠标命中测试的节流控制
        private long _lastHitTestTimeMs = 0;
        // 定义检测间隔，40ms 大概是 25FPS
        private const long HitTestIntervalMs = 40;

        // 坐标更新节流控制
        private long _lastCoordinateUpdateMs = 0;
        private const long CoordinateUpdateIntervalMs = 40; // 40ms update rate (~25fps)

        // 属性编辑器属性对象
        [ObservableProperty]
        private object? _propertyGridModel;

        // 开发者模式
        [ObservableProperty]
        private bool _isDeveloperMode;

        public void Receive(DeveloperModeChangedMessage message)
        {
            IsDeveloperMode = message.Value;
        }

        [RelayCommand]
        private void ConvertToBuiltIn(TemplateCardViewModel card)
        {
            if (card == null || !card.TemplateId.HasValue) return;

            try
            {
                var oldId = card.TemplateId.Value;
                var entity = GraphMapDatabaseService.Instance.GetTemplate(oldId);
                if (entity != null)
                {
                    // 1. 根据非自定义模板规则计算新 ID
                    Guid newId = GraphMapDatabaseService.GenerateId(entity.GraphMapPath, false);

                    if (oldId != newId)
                    {
                        // 2. 处理缩略图
                        byte[]? thumbnailBytes = null;
                        using (var stream = GraphMapDatabaseService.Instance.GetThumbnail(oldId))
                        {
                            if (stream != null)
                            {
                                using (var ms = new MemoryStream())
                                {
                                    stream.CopyTo(ms);
                                    thumbnailBytes = ms.ToArray();
                                }
                            }
                        }

                        // 3. 删除旧模板
                        GraphMapDatabaseService.Instance.DeleteTemplate(oldId);

                        // 4. 用新ID和IsCustom标志更新实体
                        entity.Id = newId;
                        entity.IsCustom = false;
                        entity.IsNewTemplate = true;

                        // 5. Save new template
                        GraphMapDatabaseService.Instance.UpsertTemplate(entity);

                        // 6. Restore Thumbnail
                        if (thumbnailBytes != null)
                        {
                            using (var ms = new MemoryStream(thumbnailBytes))
                            {
                                GraphMapDatabaseService.Instance.UploadThumbnail(newId, ms);
                            }
                        }

                        // 7. Update local VM
                        card.TemplateId = newId;
                        card.IsCustomTemplate = false;
                    }
                    else
                    {
                        // 如果 ID 恰好相同，就仅更新 IsCustom
                        entity.IsCustom = false;
                        entity.IsNewTemplate = true;
                        GraphMapDatabaseService.Instance.UpsertTemplate(entity);
                        card.IsCustomTemplate = false;
                    }

                    MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
                }
            }
            catch (Exception ex)
            {
                MessageHelper.Error(ex.Message);
            }
        }

        [RelayCommand]
        private void ExportUpdateList()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
            dialog.FileName = "GraphMapList.json";

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var allTemplates = GraphMapDatabaseService.Instance.GetSummaries();
                    var systemTemplates = allTemplates.Where(x => !x.IsCustom).ToList();

                    var exportList = systemTemplates.Select(x => new
                    {
                        ID = x.Id,
                        NodeList = x.NodeList,
                        GraphMapPath = x.GraphMapPath,
                        FileHash = x.FileHash
                    }).ToList();

                    var jsonOptions = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                    string jsonString = JsonSerializer.Serialize(exportList, jsonOptions);

                    string graphMapListPath = dialog.FileName;
                    File.WriteAllText(graphMapListPath, jsonString);

                    // 获取导出目录
                    string exportDir = Path.GetDirectoryName(graphMapListPath);
                    if (string.IsNullOrEmpty(exportDir)) exportDir = AppDomain.CurrentDomain.BaseDirectory;

                    // 2. Export PlotTemplateCategories.json
                    string categoriesFileName = "PlotTemplateCategories.json";
                    string srcCategoriesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "PlotData", categoriesFileName);
                    string dstCategoriesPath = Path.Combine(exportDir, categoriesFileName);

                    if (File.Exists(srcCategoriesPath))
                    {
                        File.Copy(srcCategoriesPath, dstCategoriesPath, true);
                    }

                    // 3. Calculate Hashes
                    string listHash = UpdateHelper.ComputeFileMd5(graphMapListPath);
                    string categoriesHash = File.Exists(dstCategoriesPath) ? UpdateHelper.ComputeFileMd5(dstCategoriesPath) : "";

                    // 4. Export server_info.json
                    var serverInfo = new ServerInfo
                    {
                        ListHash = listHash,
                        ListPlotCategoriesHash = categoriesHash,
                        Announcement = ""
                    };
                    string serverInfoJson = JsonSerializer.Serialize(serverInfo, jsonOptions);
                    File.WriteAllText(Path.Combine(exportDir, "server_info.json"), serverInfoJson);

                    // 5. Export Templates (Condition A & B)
                    string templatesDir = Path.Combine(exportDir, "Templates");
                    if (!Directory.Exists(templatesDir))
                    {
                        Directory.CreateDirectory(templatesDir);
                    }

                    int exportedCount = 0;
                    List<string> newTemplateNames = new List<string>();
                    List<string> outdatedTemplateNames = new List<string>();

                    foreach (var template in systemTemplates)
                    {
                        // Condition A: IsNewTemplate == true
                        bool isNew = template.IsNewTemplate;

                        // Condition B: IsCustom == false && Status == OUTDATED
                        bool isOutdated = template.Status == "OUTDATED";

                        if (isNew || isOutdated)
                        {
                            var fullTemplate = GraphMapDatabaseService.Instance.GetTemplate(template.Id);
                            if (fullTemplate != null && fullTemplate.Content != null)
                            {
                                string fileName = template.GraphMapPath;
                                string templateJson = JsonSerializer.Serialize(fullTemplate.Content, jsonOptions);
                                string zipPath = Path.Combine(templatesDir, $"{fileName}.zip");

                                // Create ZIP
                                if (File.Exists(zipPath)) File.Delete(zipPath);
                                using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                                {
                                    // 1. Export JSON
                                    var entry = archive.CreateEntry($"{fileName}.json");
                                    using (var entryStream = entry.Open())
                                    using (var streamWriter = new StreamWriter(entryStream))
                                    {
                                        streamWriter.Write(templateJson);
                                    }

                                    // 2. Export Thumbnail (thumbnail.jpg)
                                    using (var thumbStream = GraphMapDatabaseService.Instance.GetThumbnail(template.Id))
                                    {
                                        if (thumbStream != null)
                                        {
                                            try
                                            {
                                                using (var skBitmap = SKBitmap.Decode(thumbStream))
                                                {
                                                    if (skBitmap != null)
                                                    {
                                                        var thumbEntry = archive.CreateEntry("thumbnail.jpg");
                                                        using (var thumbEntryStream = thumbEntry.Open())
                                                        using (var wStream = new SKManagedWStream(thumbEntryStream))
                                                        {
                                                            skBitmap.Encode(wStream, SKEncodedImageFormat.Jpeg, 85);
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Debug.WriteLine($"Error converting thumbnail: {ex.Message}");
                                            }
                                        }
                                    }

                                    // 3. Export Help Documents ({lg}.rtf)
                                    if (fullTemplate.HelpDocuments != null)
                                    {
                                        foreach (var kvp in fullTemplate.HelpDocuments)
                                        {
                                            string langCode = kvp.Key;
                                            string rtfContent = kvp.Value;
                                            if (!string.IsNullOrWhiteSpace(rtfContent))
                                            {
                                                var helpEntry = archive.CreateEntry($"{langCode}.rtf");
                                                using (var helpEntryStream = helpEntry.Open())
                                                using (var streamWriter = new StreamWriter(helpEntryStream))
                                                {
                                                    streamWriter.Write(rtfContent);
                                                }
                                            }
                                        }
                                    }
                                }
                                exportedCount++;

                                if (isNew) newTemplateNames.Add(template.Name);
                                if (isOutdated) outdatedTemplateNames.Add(template.Name);
                            }
                        }
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine($"新增模板：{newTemplateNames.Count} 个。{(newTemplateNames.Count > 0 ? string.Join(", ", newTemplateNames) : "")}");
                    sb.AppendLine($"更新模板：{outdatedTemplateNames.Count} 个。{(outdatedTemplateNames.Count > 0 ? string.Join(", ", outdatedTemplateNames) : "")}");

                    NotificationManager.Instance.Show(LanguageService.Instance["ExportSuccess"] ?? "导出成功", sb.ToString(), NotificationType.Success, 0);
                }
                catch (Exception ex)
                {
                    MessageHelper.Error(ex.Message);
                }
            }
        }

        partial void OnScriptsPropertyGridChanged(bool value)
        {
            // 当显示脚本面板时，隐藏帮助文档和属性面板
            if (value)
            {
                PropertyGridModel = nullObject;
            }
        }

        public MainPlotViewModel()
        {
            WeakReferenceMessenger.Default.RegisterAll(this);

            if (bool.TryParse(Helpers.ConfigHelper.GetConfig("developer_mode"), out bool devMode))
            {
                IsDeveloperMode = devMode;
            }

            WpfPlot1 = new WpfPlot();
        }

        public void Receive(DefaultTreeExpandLevelChangedMessage message)
        {
            // 处理默认展开层级变更消息
            if (GraphMapTemplateNode != null)
            {
                // 先折叠所有节点，确保状态一致
                // CollapseAll(GraphMapTemplateNode); 

                // 重新展开到指定层级
                ExpandNodes(GraphMapTemplateNode, 1, message.Value);
            }
        }

        public void Receive(ScriptValidatedMessage message)
        {
            if (message.Value)
            {
                PrepareDataGridForInput();
            }
        }

        private void LanguageService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Item[]")
            {
                // 标记为脏
                _isTemplateLibraryDirty = true;

                // 如果当前在模板浏览模式，立即重新加载以更新名称
                if (IsTemplateMode)
                {
                    _ = InitializeAsync();
                }
            }
        }

        // 模板列表绑定
        [ObservableProperty]
        private GraphMapTemplateNode _graphMapTemplateNode;

        // 绑定到图层列表的数据源
        [ObservableProperty]
        private ObservableCollection<LayerItemViewModel> _layerTree = new ObservableCollection<LayerItemViewModel>();

        // 当前加载的、完整的模板数据
        [ObservableProperty]
        private GraphMapTemplate _currentTemplate;

        // 全局变量-指示底图类型，方便三元图的坐标转换显示
        // 底图类型：笛卡尔坐标系(Cartesian)，三元坐标系(Ternary)
        public static string BaseMapType = String.Empty;

        // 全局变量-指示三元图是否是顺时针或者逆时针
        public static bool Clockwise = true;

        // 标记是否已经检查过更新（确保仅在第一次加载时检查）
        private static bool _hasCheckedUpdates = false;

        // 绘图控件
        private WpfPlot WpfPlot1;

        // 数据表格控件
        private unvell.ReoGrid.ReoGridControl _dataGrid;

        // 说明控件
        private System.Windows.Controls.RichTextBox _richTextBox;

        // 用于追踪当前鼠标悬浮的绘图对象及其对应的图层
        private ScottPlot.IPlottable? _lastHoveredPlottable;
        private LayerItemViewModel? _lastHoveredLayer;

        // 用于绑定吸附选择按钮的状态
        [ObservableProperty]
        private bool _isSnapSelectionEnabled = false;

        [ObservableProperty]
        private bool _isTemplateMode = true; // 默认为模板浏览模式

        [ObservableProperty]
        private bool _isPlotMode = false; // 绘图模式

        [ObservableProperty]
        private bool _isGridSettingEnabled = true; // 网格设置是否可用

        [ObservableProperty]
        private bool _isShowTemplateInfo = false;   // 显示绘图模板的说明帮助

        // 卡片展示用的模板集合
        [ObservableProperty]
        private ObservableCollection<TemplateCardViewModel> _templateCards = new();

        public ICollectionView TemplateCardsView { get; private set; }

        // 当前显示的模板路径（用于面包屑导航）
        [ObservableProperty]
        private string _currentTemplatePath = "";

        // 面包屑导航
        [ObservableProperty]
        private ObservableCollection<BreadcrumbItem> _breadcrumbs = new();

        // 脚本属性面板显示
        [ObservableProperty]
        private bool _scriptsPropertyGrid = false;

        // 脚本对象
        [ObservableProperty]
        private ScriptDefinition _currentScript;

        // 十字轴
        private Crosshair CrosshairPlot { get; set; }

        // 三元坐标轴引用
        private ScottPlot.Plottables.TriangularAxis? _triangularAxis;

        // 十字轴显示
        [ObservableProperty]
        private bool _isCrosshairVisible = false;

        partial void OnIsCrosshairVisibleChanged(bool value)
        {
            if (CrosshairPlot != null)
            {
                if (!value)
                {
                    CrosshairPlot.IsVisible = false;
                    WpfPlot1.Refresh();
                }
                else
                {
                    // 如果开启，且鼠标在控件内，则显示
                    if (WpfPlot1.IsMouseOver)
                    {
                        CrosshairPlot.IsVisible = true;
                        WpfPlot1.Refresh();
                    }
                }
            }
        }

        // 追踪当前在TreeView中被选中的图层ViewModel
        private LayerItemViewModel _selectedLayer;

        // 空属性编辑对象占位
        private object nullObject = new EmptyPropertyModel();

        // 标记是否已经确认过进入编辑模式
        private bool _hasConfirmedEditMode = false;

        [ObservableProperty]
        private bool _isHelpDocReadOnly = true;

        private bool _isCurrentTemplateCustom = false;
        private string _originalTemplateJson = string.Empty;

        // 标记模板库是否需要刷新 (Dirty Flag)
        private bool _isTemplateLibraryDirty = true; // 默认为 true，确保首次加载

        private void UpdateHelpDocReadOnlyState()
        {
            // 在确认进入编辑模式下，且绘图模板为自定义模板，帮助文档的richtextbox设置为可编辑模式
            bool isEditable = RibbonTabIndex == 2 && _hasConfirmedEditMode && _isCurrentTemplateCustom;
            IsHelpDocReadOnly = !isEditable;
        }

        // 记录当前图解是否已经查看过帮助
        private bool _hasViewedHelpForCurrentDiagram = false;

        [ObservableProperty]
        private bool _isDataStateReminderVisible = false;

        private CancellationTokenSource? _reminderCts;

        partial void OnIsDataStateReminderVisibleChanged(bool value)
        {
            if (!value)
            {
                _reminderCts?.Cancel();
                _reminderCts = null;
                // 提示关闭后，标记为不再显示
                _hasViewedHelpForCurrentDiagram = true;
            }
        }

        private async void StartReminderAutoCloseTimer()
        {
            _reminderCts?.Cancel();
            _reminderCts = new CancellationTokenSource();

            try
            {
                await Task.Delay(10000, _reminderCts.Token);
                IsDataStateReminderVisible = false;
            }
            catch (TaskCanceledException)
            {
                // 忽略
            }
        }

        // Ribbon 选项卡 Index
        private int _ribbonTabIndex = 0;
        public int RibbonTabIndex
        {
            get => _ribbonTabIndex;
            set
            {
                if (_ribbonTabIndex == value) return;

                // 如果尝试切换到编辑标签页 (Index = 2)
                if (value == 2 && !_hasConfirmedEditMode)
                {
                    // 强制通知 UI 属性值未改变，以恢复 RadioButton 的选中状态
                    OnPropertyChanged(nameof(RibbonTabIndex));

                    // 异步显示确认对话框
                    _ = ConfirmEditModeAsync();
                    return;
                }

                SetProperty(ref _ribbonTabIndex, value);

                // 切换到数据状态 (Index 1) 且未查看过帮助时，显示提示
                if (value == 1 && !_hasViewedHelpForCurrentDiagram)
                {
                    IsDataStateReminderVisible = true;
                    StartReminderAutoCloseTimer();
                }
                else
                {
                    IsDataStateReminderVisible = false;
                }

                UpdateHelpDocReadOnlyState();
                ResetEditModes();
            }
        }

        private async Task ConfirmEditModeAsync()
        {
            var result = await NotificationManager.Instance.ShowDialogAsync(
                LanguageService.Instance["information"],
                LanguageService.Instance["confirm_enter_edit_mode_base_map"],
                LanguageService.Instance["Confirm"],
                LanguageService.Instance["Cancel"]);

            if (result)
            {
                _hasConfirmedEditMode = true;
                RibbonTabIndex = 2;
            }
        }

        [ObservableProperty]
        private bool _isAddingText = false; // 标记是否正处于添加文本的模式

        [ObservableProperty]
        private bool _isAddingLine = false; // 标记是否正处于添加线条的模式

        private Coordinates? _lineStartPoint = null; // 用于存储线条的起点
        private ScottPlot.Plottables.LinePlot? _tempLinePlot; // 用于实时预览的临时线条

        [ObservableProperty]
        private bool _isAddingPolygon = false; // 标记是否正处于添加多边形的模式

        private List<Coordinates> _polygonVertices = new(); // 用于存储多边形顶点的临时列表
        private ScottPlot.Plottables.Polygon? _tempPreviewPolygon; // 用于实时预览的临时多边形
        private ScottPlot.Plottables.LinePlot? _tempRubberBandLine; // 用于预览下一段连线的"橡皮筋"/

        // 用于存储当前正在编辑的模板的完整文件路径
        private string _currentTemplateFilePath;
        private Guid? _currentTemplateId;
        // 用于存储当前加载的 RTF 说明文件的路径
        private string _currentRtfFilePath;

        [ObservableProperty]
        private bool _isAddingArrow = false; // 标记是否处于添加箭头模式
        private Coordinates? _arrowStartPoint = null; // 存储箭头的起点
        private ScottPlot.Plottables.Arrow? _tempArrowPlot; // 用于实时预览箭头

        // 吸附标记
        private ScottPlot.Plottables.Marker? _snapMarker;

        // 存储所有潜在吸附点的标记（提示用）
        private List<ScottPlot.Plottables.Marker> _potentialSnapMarkers = new();

        // 数据点高亮标记
        private ScottPlot.Plottables.Marker _selectedDataPointMarker;

        // 标志位：防止表格选择和绘图点击选择互相触发循环
        private bool _isSyncingSelection = false;

        partial void OnIsAddingTextChanged(bool value)
        {
            if (value)
            {
                ClearLayerSelection();
                IsAddingLine = false;
                IsAddingPolygon = false;
                IsAddingArrow = false;
            }
            UpdatePotentialSnapPoints(value || IsPickingPointMode || IsAddingLine || IsAddingArrow || IsAddingPolygon);
        }

        partial void OnIsAddingLineChanged(bool value)
        {
            if (value)
            {
                ClearLayerSelection();
                _lineStartPoint = null;
                IsAddingText = false;
                IsAddingPolygon = false;
                IsAddingArrow = false;
            }
            UpdatePotentialSnapPoints(value || IsPickingPointMode || IsAddingArrow || IsAddingPolygon || IsAddingText);
        }

        partial void OnIsAddingPolygonChanged(bool value)
        {
            if (value)
            {
                ClearLayerSelection();
                _polygonVertices.Clear();
                IsAddingText = false;
                IsAddingLine = false;
                IsAddingArrow = false;
            }
            UpdatePotentialSnapPoints(value || IsPickingPointMode || IsAddingLine || IsAddingArrow || IsAddingText);
        }

        partial void OnIsAddingArrowChanged(bool value)
        {
            if (value)
            {
                ClearLayerSelection();
                _arrowStartPoint = null;
                IsAddingText = false;
                IsAddingLine = false;
                IsAddingPolygon = false;
            }
            UpdatePotentialSnapPoints(value || IsPickingPointMode || IsAddingLine || IsAddingPolygon || IsAddingText);
        }

        // 初始化
        public MainPlotViewModel(WpfPlot wpfPlot, System.Windows.Controls.RichTextBox richTextBox, unvell.ReoGrid.ReoGridControl dataGrid)
        {
            // 监听语言变化
            if (LanguageService.Instance != null)
            {
                LanguageService.Instance.PropertyChanged += LanguageService_PropertyChanged;
            }

            WpfPlot1 = wpfPlot;      // 获取绘图控件
            _richTextBox = richTextBox;      // 富文本框
            _dataGrid = dataGrid;        // 获取数据表格控件
            IsSnapSelectionEnabled = true;  // 吸附选择开启
            IsShowTemplateInfo = false;

            TemplateCardsView = CollectionViewSource.GetDefaultView(TemplateCards);

            // 异步初始化
            _ = InitializeAsync();

            // 初始化十字轴并设置样式
            CrosshairPlot = WpfPlot1.Plot.Add.Crosshair(0, 0);
            CrosshairPlot.IsVisible = false;
            CrosshairPlot.TextColor = ScottPlot.Colors.White;
            CrosshairPlot.TextBackgroundColor = CrosshairPlot.HorizontalLine.Color;

            // 初始化吸附标记
            _snapMarker = WpfPlot1.Plot.Add.Marker(0, 0);
            _snapMarker.IsVisible = false;
            _snapMarker.Color = Colors.Orange; // 使用醒目的橙色
            _snapMarker.Size = 15; // 稍微大一点
            _snapMarker.Shape = MarkerShape.OpenCircle;
            _snapMarker.LineWidth = 3; // 加粗

            // 初始化选中数据点标记
            _selectedDataPointMarker = WpfPlot1.Plot.Add.Marker(0, 0);
            _selectedDataPointMarker.IsVisible = false;
            _selectedDataPointMarker.Color = Colors.Red; // 选中点使用红色
            _selectedDataPointMarker.Size = 20; // 比数据点大（数据点通常是10）
            _selectedDataPointMarker.Shape = MarkerShape.OpenCircle; // 空心圆圈
            _selectedDataPointMarker.LineWidth = 2; // 线宽

            // 订阅绘图控件的鼠标事件
            WpfPlot1.MouseEnter += WpfPlot1_MouseEnter;
            WpfPlot1.MouseLeave += WpfPlot1_MouseLeave;
            WpfPlot1.MouseMove += WpfPlot1_MouseMove;

            // 订阅线条绘制事件
            WpfPlot1.MouseUp += WpfPlot1_MouseUp;
            WpfPlot1.MouseRightButtonUp += WpfPlot1_MouseRightButtonUp;

            // 订阅数据表格行数自动扩充
            if (_dataGrid.CurrentWorksheet != null)
            {
                _dataGrid.CurrentWorksheet.BeforePaste += CurrentWorksheet_BeforePaste;
                _dataGrid.CurrentWorksheet.SelectionRangeChanged += CurrentWorksheet_SelectionRangeChanged;
                _dataGrid.CurrentWorksheet.CellDataChanged += CurrentWorksheet_CellDataChanged;
            }

            WpfPlot1.Menu.Clear();      // 禁用原生右键菜单

            // 禁用双击帧率显示
            WpfPlot1.UserInputProcessor.DoubleLeftClickBenchmark(false);

            // 注册消息接收
            WeakReferenceMessenger.Default.RegisterAll(this);
        }



        public void LoadSettings()
        {
            // 加载默认第三方应用设置
            string defaultApp = ConfigHelper.GetConfig("default_third_party_app");
            if (!string.IsNullOrEmpty(defaultApp) && ThirdPartyApps.Contains(defaultApp))
            {
                SelectedThirdPartyApp = defaultApp;
            }
        }

        /// <summary>
        /// 检查更新（仅在第一次加载页面时执行）
        /// </summary>
        public void CheckUpdatesIfNeeded()
        {
            // 自动检查更新（仅在第一次加载时执行）
            if (!_hasCheckedUpdates)
            {
                _hasCheckedUpdates = true;

                // 检查模板更新
                if (bool.TryParse(ConfigHelper.GetConfig("auto_check_template_update"), out bool checkTemplate) && checkTemplate)
                {
                    _ = CheckForTemplateUpdates();
                }
            }
        }

        /// <summary>
        /// 在粘贴操作发生前处理的事件。
        /// 用于检查粘贴数据的行数并根据需要自动扩展表格。
        /// </summary>
        /// <param name="sender">事件发送者，即 Worksheet 对象</param>
        /// <param name="e">事件参数</param>
        private void CurrentWorksheet_BeforePaste(object sender, unvell.ReoGrid.Events.BeforeRangeOperationEventArgs e)
        {
            var worksheet = sender as Worksheet;
            if (worksheet == null) return;

            // 从剪贴板获取将要粘贴的文本数据
            if (!Clipboard.ContainsText()) return;
            string pasteText = Clipboard.GetText();
            if (string.IsNullOrEmpty(pasteText)) return;

            // 计算粘贴文本包含的行数
            var lines = pasteText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int pastedRowCount = lines.Length;

            // 如果粘贴的文本以换行符结尾，Split会产生一个额外的空数组元素，需要排除掉
            if (string.IsNullOrEmpty(lines.Last()))
            {
                pastedRowCount--;
            }

            if (pastedRowCount <= 0) return;

            // 获取粘贴操作的目标起始行
            int startRow = worksheet.SelectionRange.Row;

            // 计算粘贴完成后所需要的总行数
            int requiredTotalRows = startRow + pastedRowCount;

            // 如果需要的总行数大于当前表格的总行数，则扩展表格
            if (requiredTotalRows > worksheet.RowCount)
            {
                // 动态设置工作表的总行数
                worksheet.RowCount = requiredTotalRows;
            }
        }

        /// <summary>
        /// 将图层恢复到其在当前选择状态下应有的样式
        /// 先恢复原始样式，再根据情况应用遮罩
        /// </summary>
        private void RestoreLayerToCorrectState(LayerItemViewModel layerToRestore)
        {
            if (layerToRestore is not IPlotLayer plotLayer || plotLayer.Plottable == null)
                return;

            // 恢复原始样式
            plotLayer.Restore();

            // 判断是否需要应用遮罩
            // 如果当前有选中的图层，且正在恢复的图层不是选中的那个，说明它应该处于“变暗”状态
            // 排除选中项为 CategoryLayerItemViewModel 的情况（选中父类不应触发遮罩）
            bool isSelectionActive = _selectedLayer != null && !(_selectedLayer is CategoryLayerItemViewModel);
            bool isLayerSelected = layerToRestore == _selectedLayer || SelectedLayers.Contains(layerToRestore);

            if (isSelectionActive && !isLayerSelected)
            {
                plotLayer.Dim();
            }
        }

        /// <summary>
        /// 接收到拾取点请求消息
        /// </summary>
        public void Receive(PickPointRequestMessage message)
        {
            if (message.Value == null) return;

            // 进入拾取模式
            IsPickingPointMode = true;
            _targetPointDefinition = message.Value;

            // 高亮目标点
            _targetPointDefinition.IsHighlighted = true;
            RefreshPlotFromLayers(true);

            // 提示用户-请在绘图区域点击以拾取坐标
            MessageHelper.Info(LanguageService.Instance["click_to_pick_coordinates"]);

            // 改变鼠标光标为十字准星
            if (WpfPlot1 != null)
            {
                WpfPlot1.Cursor = Cursors.Cross;
            }
        }

        /// <summary>
        /// 高亮显示指定的图层
        /// </summary>
        private void HighlightLayer(LayerItemViewModel layer)
        {
            if (layer is IPlotLayer plotLayer)
            {
                plotLayer.Highlight();
            }
        }

        /// <summary>
        /// 获取指定像素位置下的绘图对象
        /// </summary>
        /// <param name="pixel">鼠标像素位置</param>
        /// <param name="radius">搜索半径（像素）</param>
        /// <returns>返回找到的 Plottable，否则返回 null</returns>
        private IPlottable? GetPlottableAtPixel(Pixel pixel, float radius = 5)
        {
            Coordinates mouseCoordinates = WpfPlot1.Plot.GetCoordinates(pixel);

            // 从最上层的图层开始反向遍历，这样可以优先选中顶层的对象
            foreach (var plottable in WpfPlot1.Plot.GetPlottables().Reverse())
            {
                // 跳过不可见，十字轴或不参与图例的对象
                if (!plottable.IsVisible || plottable is ScottPlot.Plottables.Crosshair)
                    continue;

                bool isHovered = false;

                // 根据不同的 Plottable 类型执行不同的命中测试逻辑
                switch (plottable)
                {
                    case ScottPlot.Plottables.Scatter scatter:
                        // 数据点吸附
                        if (scatter is IGetNearest hittable)
                        {
                            DataPoint nearest = hittable.GetNearest(mouseCoordinates, WpfPlot1.Plot.LastRender, radius);
                            if (nearest.IsReal)
                            {
                                isHovered = true;
                            }
                        }
                        break;

                    case ScottPlot.Plottables.LinePlot line:
                        // 对于线条，计算点到线段的距离
                        double distanceToLine = GetDistanceToLineSegment(mouseCoordinates, line.Start, line.End);
                        if (distanceToLine <= radius)
                        {
                            isHovered = true;
                        }
                        break;

                    case ScottPlot.Plottables.Text text:
                        // 1. 获取文本的锚点像素位置
                        Pixel textPixel = WpfPlot1.Plot.GetPixel(text.Location);

                        // 2. 测量文本未旋转时的尺寸和相对矩形
                        MeasuredText measured = text.LabelStyle.Measure();
                        PixelRect relativeRect = measured.Rect(text.LabelStyle.Alignment);

                        // 3. 计算鼠标相对于文本锚点的坐标
                        float mouseX_relative = pixel.X - textPixel.X;
                        float mouseY_relative = pixel.Y - textPixel.Y;

                        // 4. 如果文本有旋转，则将鼠标的相对坐标进行“反向旋转”
                        if (text.LabelStyle.Rotation != 0)
                        {
                            // 将旋转角度从度转换为弧度
                            double angleRadians = -text.LabelStyle.Rotation * Math.PI / 180.0;
                            double cos = Math.Cos(angleRadians);
                            double sin = Math.Sin(angleRadians);

                            // 应用旋转矩阵的逆运算
                            float rotatedX = (float)(mouseX_relative * cos - mouseY_relative * sin);
                            float rotatedY = (float)(mouseX_relative * sin + mouseY_relative * cos);

                            mouseX_relative = rotatedX;
                            mouseY_relative = rotatedY;
                        }

                        // 5. 判断“反向旋转”后的鼠标点是否在文本未旋转的相对矩形内
                        if (relativeRect.Contains(mouseX_relative, mouseY_relative))
                        {
                            isHovered = true;
                        }
                        break;

                    // 箭头处理
                    case ScottPlot.Plottables.Arrow arrowPlot:
                        double distanceToArrow = GetDistanceToLineSegment(mouseCoordinates, arrowPlot.Base, arrowPlot.Tip);
                        if (distanceToArrow <= radius)
                        {
                            isHovered = true;
                        }
                        break;

                    case ScottPlot.Plottables.Polygon polygon:
                        // 对于多边形，检查鼠标坐标是否在多边形内部
                        if (IsPointInPolygon(mouseCoordinates, polygon.Coordinates))
                        {
                            isHovered = true;
                        }
                        break;

                        // TODO: 添加其他类型的 Plottable 命中测试
                }

                if (isHovered)
                {
                    return plottable; // 如果命中，立即返回该对象
                }
            }

            // 如果没有找到任何 Plottable，返回 null
            return null;
        }

        /// <summary>
        /// 计算一个点到线段的最短距离（像素单位）
        /// </summary>
        private double GetDistanceToLineSegment(Coordinates point, Coordinates p1, Coordinates p2)
        {
            // 将坐标单位转换为像素单位
            Pixel ptPixel = WpfPlot1.Plot.GetPixel(point);
            Pixel p1Pixel = WpfPlot1.Plot.GetPixel(p1);
            Pixel p2Pixel = WpfPlot1.Plot.GetPixel(p2);

            float dx = p2Pixel.X - p1Pixel.X;
            float dy = p2Pixel.Y - p1Pixel.Y;

            if (dx == 0 && dy == 0) // 线段退化成一个点
            {
                return ptPixel.DistanceFrom(p1Pixel);
            }

            // 计算点在线段上的投影比例
            float t = ((ptPixel.X - p1Pixel.X) * dx + (ptPixel.Y - p1Pixel.Y) * dy) / (dx * dx + dy * dy);

            Pixel closestPoint;
            if (t < 0)
            {
                closestPoint = p1Pixel; // 投影在线段起点之外
            }
            else if (t > 1)
            {
                closestPoint = p2Pixel; // 投影在线段终点之外
            }
            else
            {
                closestPoint = new Pixel(p1Pixel.X + t * dx, p1Pixel.Y + t * dy); // 投影在线段上
            }

            return ptPixel.DistanceFrom(closestPoint);
        }

        /// <summary>
        /// 判断一个点是否在多边形内部（射线法）
        /// </summary>
        private bool IsPointInPolygon(Coordinates point, Coordinates[] polygonVertices)
        {
            if (polygonVertices == null || polygonVertices.Length < 3)
                return false;

            // 包围盒预检查 (性能优化)
            double minX = polygonVertices[0].X;
            double maxX = polygonVertices[0].X;
            double minY = polygonVertices[0].Y;
            double maxY = polygonVertices[0].Y;

            for (int i = 1; i < polygonVertices.Length; i++)
            {
                if (polygonVertices[i].X < minX) minX = polygonVertices[i].X;
                if (polygonVertices[i].X > maxX) maxX = polygonVertices[i].X;
                if (polygonVertices[i].Y < minY) minY = polygonVertices[i].Y;
                if (polygonVertices[i].Y > maxY) maxY = polygonVertices[i].Y;
            }

            // 如果点不在包围盒内，直接返回 false，避免进行射线运算
            if (point.X < minX || point.X > maxX || point.Y < minY || point.Y > maxY)
            {
                return false;
            }

            bool isInside = false;
            int j = polygonVertices.Length - 1;
            for (int i = 0; i < polygonVertices.Length; i++)
            {
                var pi = polygonVertices[i];
                var pj = polygonVertices[j];

                if (((pi.Y > point.Y) != (pj.Y > point.Y)) &&
                    (point.X < (pj.X - pi.X) * (point.Y - pi.Y) / (pj.Y - pi.Y) + pi.X))
                {
                    isInside = !isInside;
                }
                j = i;
            }
            return isInside;
        }

        /// <summary>
        /// 根据 Plottable 对象在图层树中查找对应的 LayerItemViewModel
        /// </summary>
        private LayerItemViewModel? FindLayerByPlottable(ScottPlot.IPlottable plottable)
        {
            if (plottable == null) return null;
            // 使用 FlattenTree 辅助方法获取所有图层的扁平列表，然后查找
            return FlattenTree(LayerTree).FirstOrDefault(layer => layer.Plottable == plottable);
        }

        /// <summary>
        /// 根据当前模板的脚本要求，准备数据输入表格
        /// </summary>
        private void PrepareDataGridForInput()
        {
            if (CurrentTemplate?.Script == null || string.IsNullOrEmpty(CurrentTemplate.Script.RequiredDataSeries))
            {
                MessageHelper.Warning(LanguageService.Instance["script_not_defined_in_template"]);
                _dataGrid.Worksheets[0].Reset(); // 清空表格
                return;
            }

            var worksheet = _dataGrid.Worksheets[0];
            worksheet.Reset(); // 重置表格内容

            // 从脚本定义中获取需要的参数列
            var requiredColumns = CurrentTemplate.Script.RequiredDataSeries
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();

            // 自动添加 Category 变量作为第一个变量
            var categoryIndex = requiredColumns.FindIndex(s => s.Equals("Category", StringComparison.OrdinalIgnoreCase));
            if (categoryIndex == -1)
            {
                requiredColumns.Insert(0, "Category");
            }
            else if (categoryIndex > 0)
            {
                var category = requiredColumns[categoryIndex];
                requiredColumns.RemoveAt(categoryIndex);
                requiredColumns.Insert(0, category);
            }

            worksheet.ColumnCount = requiredColumns.Count;

            // 将参数名设置为表格的表头
            for (int i = 0; i < requiredColumns.Count; i++)
            {
                worksheet.ColumnHeaders[i].Text = requiredColumns[i];

                // 自适应列宽，包含表头
                double pixelsPerDip = 1.0;
                try
                {
                    var mainWindow = System.Windows.Application.Current?.MainWindow;
                    if (mainWindow != null)
                    {
                        pixelsPerDip = System.Windows.Media.VisualTreeHelper.GetDpi(mainWindow).PixelsPerDip;
                    }
                }
                catch
                {
                    // 忽略获取 DPI 失败的情况，使用默认值 1.0
                }

                var formattedText = new System.Windows.Media.FormattedText(
                    requiredColumns[i],
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new System.Windows.Media.Typeface("Microsoft YaHei UI"),
                    14, // 使用稍大的字号进行计算，保证宽度足够
                    System.Windows.Media.Brushes.Black,
                    pixelsPerDip);

                // 设置列宽 (文本宽度 + 20px 缓冲)
                worksheet.SetColumnsWidth(i, 1, (ushort)(formattedText.Width + 20));
            }
        }

        /// <summary>
        /// 鼠标左键抬起事件，用于确定绘图对象的起点和终点
        /// </summary>
        private void UpdateSelectedDataPointMarker(Coordinates location)
        {
            if (WpfPlot1 == null || _selectedDataPointMarker == null) return;

            // 移除并重新添加，确保标记显示在最上层
            WpfPlot1.Plot.Remove(_selectedDataPointMarker);
            WpfPlot1.Plot.Add.Plottable(_selectedDataPointMarker);

            _selectedDataPointMarker.Location = location;
            _selectedDataPointMarker.IsVisible = true;
            WpfPlot1.Refresh();
        }

        private void CurrentWorksheet_CellDataChanged(object? sender, unvell.ReoGrid.Events.CellEventArgs e)
        {
            // 自动重新投点
            if (CurrentTemplate != null && CurrentTemplate.Script != null)
            {
                // 获取当前单元格数据
                var sheet = sender as unvell.ReoGrid.Worksheet;
                if (sheet != null)
                {
                    // 1. 获取需要的数据列名
                    var requiredSeriesStr = CurrentTemplate.Script.RequiredDataSeries;
                    if (string.IsNullOrWhiteSpace(requiredSeriesStr)) return;

                    var requiredSeries = requiredSeriesStr
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToList();

                    if (requiredSeries.Count == 0) return;

                    // 2. 检查表头，找到对应的列索引
                    var colIndicesToCheck = new List<int>();

                    for (int c = 0; c < sheet.ColumnCount; c++)
                    {
                        var header = sheet.ColumnHeaders[c];
                        if (header != null && requiredSeries.Contains(header.Text))
                        {
                            colIndicesToCheck.Add(c);
                        }
                    }

                    // 如果找到的列比要求的少，说明表头可能还没配好，不触发
                    if (colIndicesToCheck.Count < requiredSeries.Count) return;

                    // 3. 检查当前修改行，在这些列上是否都有值
                    int row = e.Cell.Row;
                    bool isRowComplete = true;
                    foreach (var colIndex in colIndicesToCheck)
                    {
                        var cellData = sheet.GetCellData(row, colIndex);
                        if (cellData == null || string.IsNullOrWhiteSpace(cellData.ToString()))
                        {
                            isRowComplete = false;
                            break;
                        }
                    }

                    // 4. 只有数据完整才触发投点
                    if (isRowComplete)
                    {
                        try
                        {
                            PlotDataFromGrid();
                        }
                        catch (Exception ex)
                        {
                            // 自动投点失败
                            MessageHelper.Error(LanguageService.Instance["auto_datapoint_projection_failed"] + ex.Message);
                        }
                    }
                }
            }
        }

        private void CurrentWorksheet_SelectionRangeChanged(object? sender, unvell.ReoGrid.Events.RangeEventArgs e)
        {
            if (_isSyncingSelection) return;

            try
            {
                _isSyncingSelection = true;
                HighlightDataPointByRowIndex(e.Range.Row);
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        private void HighlightDataPointByRowIndex(int rowIndex)
        {
            if (WpfPlot1 == null) return;

            bool found = false;

            // 遍历所有 ScatterLayer
            foreach (var layer in FlattenTree(LayerTree).OfType<ScatterLayerItemViewModel>())
            {
                if (!layer.IsVisible || layer.OriginalRowIndices == null) continue;

                // 查找该行号在图层中的索引
                int indexInLayer = layer.OriginalRowIndices.IndexOf(rowIndex);
                if (indexInLayer >= 0 && indexInLayer < layer.DataPoints.Count)
                {
                    // 找到对应的点
                    var point = layer.DataPoints[indexInLayer];

                    // 更新并显示高亮标记
                    UpdateSelectedDataPointMarker(point);

                    // 选中对应的图层
                    if (SelectLayerCommand.CanExecute(layer))
                    {
                        // 临时禁用 Sync，防止反向触发
                        SelectLayerCommand.Execute(layer);
                    }

                    found = true;
                    break;
                }
            }

            if (!found)
            {
                // 如果没找到（比如选中了空行），隐藏高亮标记
                if (_selectedDataPointMarker.IsVisible)
                {
                    _selectedDataPointMarker.IsVisible = false;
                    WpfPlot1.Refresh();
                }
            }
        }

        private void ClearReoGridSelection()
        {
            if (_dataGrid == null || _dataGrid.Worksheets.Count == 0) return;
            var sheet = _dataGrid.Worksheets[0];

            // 隐藏绘图区的高亮标记
            if (_selectedDataPointMarker.IsVisible)
            {
                _selectedDataPointMarker.IsVisible = false;
                WpfPlot1.Refresh();
            }
        }

        private bool SelectDataPointAtMouse(Coordinates mouseCoordinates)
        {
            // 遍历所有 ScatterLayer
            foreach (var layer in FlattenTree(LayerTree).OfType<ScatterLayerItemViewModel>())
            {
                if (!layer.IsVisible || !(layer.Plottable is ScottPlot.Plottables.Scatter scatter)) continue;

                if (scatter is IGetNearest hittable)
                {
                    // 使用小半径进行点击判定 (10像素)
                    DataPoint nearest = hittable.GetNearest(mouseCoordinates, WpfPlot1.Plot.LastRender, 10);
                    if (nearest.IsReal)
                    {
                        int index = nearest.Index;
                        if (layer.OriginalRowIndices != null && index >= 0 && index < layer.OriginalRowIndices.Count)
                        {
                            int originalRowIndex = layer.OriginalRowIndices[index];

                            try
                            {
                                _isSyncingSelection = true;
                                SelectReoGridRow(originalRowIndex);
                            }
                            finally
                            {
                                _isSyncingSelection = false;
                            }

                            // 显示高亮标记
                            UpdateSelectedDataPointMarker(layer.DataPoints[index]);

                            // 同时也选中图层
                            if (SelectLayerCommand.CanExecute(layer))
                                SelectLayerCommand.Execute(layer);

                            return true; // 找到一个点后即停止
                        }
                    }
                }
            }
            return false;
        }

        private void SelectReoGridRow(int row)
        {
            if (_dataGrid == null || _dataGrid.Worksheets.Count == 0) return;
            var sheet = _dataGrid.Worksheets[0];
            if (row < 0 || row >= sheet.RowCount) return;

            sheet.SelectionRange = new unvell.ReoGrid.RangePosition(row, 0, 1, sheet.ColumnCount);
        }

        private void WpfPlot1_MouseUp(object? sender, MouseButtonEventArgs e)
        {
            // 鼠标左键抬起事件
            if (e.ChangedButton != MouseButton.Left)
                return;

            // 处理拾取点模式
            if (IsPickingPointMode)
            {
                var pickMousePos = e.GetPosition(WpfPlot1);
                Pixel pickMousePixel = new(pickMousePos.X * WpfPlot1.DisplayScale, pickMousePos.Y * WpfPlot1.DisplayScale);
                Coordinates pickMouseCoordinates = WpfPlot1.Plot.GetCoordinates(pickMousePixel);

                // 尝试吸附
                var snapPoint = GetSnapPoint(pickMousePixel);
                if (snapPoint.HasValue)
                {
                    pickMouseCoordinates = snapPoint.Value;
                }

                if (_targetPointDefinition != null)
                {
                    _targetPointDefinition.X = pickMouseCoordinates.X;
                    _targetPointDefinition.Y = pickMouseCoordinates.Y;
                    // 坐标拾取成功
                    MessageHelper.Success(LanguageService.Instance["coordinates_picked_success"]);

                    // 刷新绘图
                    RefreshPlotFromLayers(true);
                    ReapplySelectionVisualState();

                    // 取消高亮
                    _targetPointDefinition.IsHighlighted = false;
                    RefreshPlotFromLayers(true);
                }

                // 退出拾取模式
                IsPickingPointMode = false;
                _targetPointDefinition = null;
                WpfPlot1.Cursor = Cursors.Arrow;
                // 隐藏吸附标记
                if (_snapMarker != null) _snapMarker.IsVisible = false;
                return;
            }

            // 处理吸附选择的点击逻辑（在添加/拾取模式下不触发选中）
            bool isDrawingOrAdding = IsPickingPointMode || IsAddingLine || IsAddingArrow || IsAddingPolygon || IsAddingText;

            // 优先尝试选中数据点
            if (!isDrawingOrAdding)
            {
                var pointSelectMousePos = e.GetPosition(WpfPlot1);
                Pixel pointSelectMousePixel = new(pointSelectMousePos.X * WpfPlot1.DisplayScale, pointSelectMousePos.Y * WpfPlot1.DisplayScale);
                Coordinates pointSelectMouseCoordinates = WpfPlot1.Plot.GetCoordinates(pointSelectMousePixel);
                if (SelectDataPointAtMouse(pointSelectMouseCoordinates))
                {
                    return;
                }
                else
                {
                    // 如果点击了非数据点区域，尝试取消数据点高亮
                    ClearReoGridSelection();
                }
            }

            if (!isDrawingOrAdding && IsSnapSelectionEnabled && _lastHoveredLayer != null)
            {
                if (SelectLayerCommand.CanExecute(_lastHoveredLayer))
                {
                    SelectLayerCommand.Execute(_lastHoveredLayer);
                }
                return;
            }

            // 检查是否点击了坐标轴
            if (!isDrawingOrAdding && _lastHoveredLayer == null && IsSnapSelectionEnabled)
            {
                var mousePosForAxis = e.GetPosition(WpfPlot1);
                Pixel mousePixelForAxis = new(mousePosForAxis.X * WpfPlot1.DisplayScale, mousePosForAxis.Y * WpfPlot1.DisplayScale);

                if (BaseMapType == "Ternary")
                {
                    var ternaryLayout = WpfPlot1.Plot.RenderManager.LastRender.DataRect;

                    // 0. 标题点击检测 (最优先)
                    if (WpfPlot1.Plot.Axes.Title.IsVisible &&
                        !string.IsNullOrEmpty(WpfPlot1.Plot.Axes.Title.Label.Text) &&
                        mousePixelForAxis.Y < ternaryLayout.Top * 0.6)
                    {
                        PlotSettingCommand.Execute(null);
                        return;
                    }

                    // 三元图坐标轴点击检测
                    Coordinates cA = new Coordinates(0, 0);
                    Coordinates cB = new Coordinates(1, 0);
                    Coordinates cC = new Coordinates(0.5, Math.Sqrt(3) / 2);
                    Pixel pA = WpfPlot1.Plot.GetPixel(cA);
                    Pixel pB = WpfPlot1.Plot.GetPixel(cB);
                    Pixel pC = WpfPlot1.Plot.GetPixel(cC);

                    var ternaryPlot = WpfPlot1.Plot.GetPlottables().OfType<ScottPlot.Plottables.TriangularAxis>().FirstOrDefault();

                    double GetTernaryAxisHitScore(double dist, Pixel start, Pixel end, Pixel opposite, ScottPlot.TriangularAxisEdge? edge)
                    {
                        // 1. 标题点击检测 (高优先级)
                        if (edge != null)
                        {
                            Pixel mid = new((start.X + end.X) / 2, (start.Y + end.Y) / 2);
                            Pixel labelPos = new(mid.X + edge.LabelStyle.OffsetX, mid.Y + edge.LabelStyle.OffsetY);
                            double distLabel = Math.Sqrt(Math.Pow(mousePixelForAxis.X - labelPos.X, 2) + Math.Pow(mousePixelForAxis.Y - labelPos.Y, 2));
                            if (distLabel < 50) return 0.1; // 标题点击，给予极高优先级
                        }

                        // 2. 轴线点击检测 (严格范围)
                        if (dist < 20) return dist;

                        // 3. 轴线点击检测 (宽松范围，仅限三角形外侧)
                        double cpRef = (end.X - start.X) * (opposite.Y - start.Y) - (end.Y - start.Y) * (opposite.X - start.X);
                        double cpMouse = (end.X - start.X) * (mousePixelForAxis.Y - start.Y) - (end.Y - start.Y) * (mousePixelForAxis.X - start.X);
                        bool isOutside = Math.Sign(cpRef) != Math.Sign(cpMouse);

                        if (isOutside && dist < 60) return dist;

                        return double.MaxValue;
                    }

                    double distBottom = DistancePointToSegment(mousePixelForAxis, pA, pB);
                    double distLeft = DistancePointToSegment(mousePixelForAxis, pA, pC);
                    double distRight = DistancePointToSegment(mousePixelForAxis, pB, pC);

                    double scoreBottom = GetTernaryAxisHitScore(distBottom, pA, pB, pC, ternaryPlot?.Bottom);
                    double scoreLeft = GetTernaryAxisHitScore(distLeft, pA, pC, pB, ternaryPlot?.Left);
                    double scoreRight = GetTernaryAxisHitScore(distRight, pB, pC, pA, ternaryPlot?.Right);

                    string? ternaryClickedAxis = null;
                    double bestScore = double.MaxValue;

                    if (scoreBottom < bestScore) { ternaryClickedAxis = "Bottom"; bestScore = scoreBottom; }
                    if (scoreLeft < bestScore) { ternaryClickedAxis = "Left"; bestScore = scoreLeft; }
                    if (scoreRight < bestScore) { ternaryClickedAxis = "Right"; bestScore = scoreRight; }

                    if (ternaryClickedAxis != null)
                    {
                        var axisDef = CurrentTemplate.Info.Axes.FirstOrDefault(a => a is TernaryAxisDefinition t && t.Type == ternaryClickedAxis);
                        if (axisDef != null)
                        {
                            // 清除其他选中
                            CancelSelected();
                            PropertyGridModel = axisDef;
                            // 重新绑定事件
                            // PropertyGridModel = axisDef 会自动触发 partial void OnPropertyGridModelChanged
                            // 进而订阅 PropertyGridModel_PropertyChanged，无需手动订阅
                            WpfPlot1.Refresh();
                            return;
                        }
                    }
                    return; // 三元图模式下不执行后续的笛卡尔轴检测
                }

                var layout = WpfPlot1.Plot.RenderManager.LastRender.DataRect;
                string? clickedAxis = null;

                // 简单的范围判定
                if (mousePixelForAxis.X < layout.Left && mousePixelForAxis.Y > layout.Top && mousePixelForAxis.Y < layout.Bottom)
                {
                    clickedAxis = "Left";
                }
                else if (mousePixelForAxis.X > layout.Right && mousePixelForAxis.Y > layout.Top && mousePixelForAxis.Y < layout.Bottom)
                {
                    clickedAxis = "Right";
                }
                else if (mousePixelForAxis.Y > layout.Bottom && mousePixelForAxis.X > layout.Left && mousePixelForAxis.X < layout.Right)
                {
                    clickedAxis = "Bottom";
                }
                else if (mousePixelForAxis.Y < layout.Top && mousePixelForAxis.X > layout.Left && mousePixelForAxis.X < layout.Right)
                {
                    // 检查是否点击了标题 (判定方法：如果点击位置位于上边距的上半部分，且标题可见)
                    // 假设标题占据上边距的前 60% 区域
                    if (WpfPlot1.Plot.Axes.Title.IsVisible &&
                        !string.IsNullOrEmpty(WpfPlot1.Plot.Axes.Title.Label.Text) &&
                        mousePixelForAxis.Y < layout.Top * 0.6)
                    {
                        PlotSettingCommand.Execute(null);
                        return;
                    }

                    clickedAxis = "Top";
                }

                if (clickedAxis != null)
                {
                    var axisLayer = FlattenTree(LayerTree)
                        .OfType<AxisLayerItemViewModel>()
                        .FirstOrDefault(l => l.AxisDefinition != null && l.AxisDefinition.Type == clickedAxis);

                    if (axisLayer != null)
                    {
                        SelectLayerCommand.Execute(axisLayer);
                        return;
                    }
                }
            }

            var mousePos = e.GetPosition(WpfPlot1);
            Pixel mousePixel = new(mousePos.X * WpfPlot1.DisplayScale, mousePos.Y * WpfPlot1.DisplayScale);
            Coordinates mouseCoordinates = WpfPlot1.Plot.GetCoordinates(mousePixel);

            // 绘图时的吸附逻辑
            if (IsAddingLine || IsAddingArrow || IsAddingPolygon)
            {
                var snapPoint = GetSnapPoint(mousePixel);
                if (snapPoint.HasValue)
                {
                    mouseCoordinates = snapPoint.Value;
                }
            }

            // 添加多边形
            if (IsAddingPolygon)
            {
                // 添加新顶点
                _polygonVertices.Add(mouseCoordinates);

                // 移除旧的预览多边形
                if (_tempPreviewPolygon != null)
                {
                    WpfPlot1.Plot.Remove(_tempPreviewPolygon);
                }

                // 如果顶点数大于等于2，则可以开始预览
                if (_polygonVertices.Count >= 2)
                {
                    _tempPreviewPolygon = WpfPlot1.Plot.Add.Polygon(_polygonVertices.ToArray());
                    _tempPreviewPolygon.FillStyle.Color = Colors.Transparent; // 预览时内部透明
                    _tempPreviewPolygon.LineStyle.Color = Colors.Red;
                    _tempPreviewPolygon.LineStyle.Pattern = LinePattern.Dashed;
                    _tempPreviewPolygon.LineStyle.Width = 1.5f;
                }
                WpfPlot1.Refresh();
                return; // 处理完毕，直接返回
            }

            // 添加线条
            if (IsAddingLine)
            {

                if (!_lineStartPoint.HasValue)
                {
                    // 第一次点击：设置起点
                    _lineStartPoint = mouseCoordinates;
                }
                else
                {
                    // 第二次点击：设置终点，正式创建线条

                    // 获取起点的真实数据值
                    var realStart = PlotTransformHelper.ToRealDataCoordinates(WpfPlot1.Plot, _lineStartPoint.Value);
                    // 获取终点的真实数据值
                    var realEnd = PlotTransformHelper.ToRealDataCoordinates(WpfPlot1.Plot, mouseCoordinates);

                    // 在图层树中找到 "线" 分类
                    var linesCategory = GetOrCreateCategory(LanguageService.Instance["line"]);

                    // 创建新的 LineDefinition 对象来存储线条的属性
                    var newLineDef = new LineDefinition
                    {
                        // 转换后的真实坐标
                        Start = new PointDefinition { X = realStart.X, Y = realStart.Y },
                        End = new PointDefinition { X = realEnd.X, Y = realEnd.Y },
                        // 设置默认样式
                        Color = "#0078D4", // 默认蓝色
                        Width = 2,
                        Style = LineDefinition.LineType.Solid
                    };

                    // 创建新的 LineLayerItemViewModel
                    var lineLayer = new LineLayerItemViewModel(newLineDef, linesCategory.Children.Count);
                    // 订阅刷新事件
                    lineLayer.RequestRefresh += (s, ev) => RefreshPlotFromLayers(true);

                    // 将新图层添加到图层树
                    linesCategory.Children.Add(lineLayer);

                    // 刷新整个绘图
                    RefreshPlotFromLayers(true);

                    // 选中新添加的图层
                    SelectLayer(lineLayer);

                    // 记录 Undo/Redo
                    Action undoLine = () =>
                    {
                        var cat = LayerTree.FirstOrDefault(c => c is CategoryLayerItemViewModel vm && vm.Name == LanguageService.Instance["line"]) as CategoryLayerItemViewModel;
                        if (cat != null)
                        {
                            if (cat.Children.Contains(lineLayer)) cat.Children.Remove(lineLayer);
                            if (cat.Children.Count == 0) LayerTree.Remove(cat);
                        }
                        RefreshPlotFromLayers(true);
                    };
                    Action redoLine = () =>
                    {
                        var cat = GetOrCreateCategory(LanguageService.Instance["line"]);
                        if (!cat.Children.Contains(lineLayer)) cat.Children.Add(lineLayer);
                        RefreshPlotFromLayers(true);
                    };
                    AddUndoState(undoLine, redoLine);

                    // 重置画线状态
                    if (_tempLinePlot != null)
                    {
                        WpfPlot1.Plot.Remove(_tempLinePlot);
                        _tempLinePlot = null;
                    }
                    _lineStartPoint = null;
                    // IsAddingLine = false; // 保持添加模式
                    WpfPlot1.Refresh(); // 最后刷新一次，确保临时线完全消失
                }
                return; // 处理完毕，直接返回
            }

            // 添加文本
            if (IsAddingText)
            {
                var textCategory = GetOrCreateCategory(LanguageService.Instance["text"]);

                // 确定当前模板支持的语言和默认语言
                string defaultLang;
                List<string> allLangs;

                if (CurrentTemplate != null && CurrentTemplate.NodeList.Translations.Any())
                {
                    // 从当前加载的模板中获取语言信息
                    defaultLang = CurrentTemplate.DefaultLanguage;
                    allLangs = CurrentTemplate.NodeList.Translations.Keys.ToList();
                }
                else
                {
                    // 如果没有当前模板或模板没有语言信息，回退方案
                    defaultLang = "en-US";
                    allLangs = new List<string> { "en-US", "zh-CN" }; // 通用回退
                }

                // 使用工厂创建多语言占位符
                var contentString = LocalizedPlaceholderFactory.Create("Placeholder_Text", defaultLang, allLangs);

                // 获取当前语言下的文本用于字体检测
                string placeholder = contentString.Get();


                // 将鼠标点击坐标转换为真实数据坐标
                var realLocation = PlotTransformHelper.ToRealDataCoordinates(WpfPlot1.Plot, mouseCoordinates);

                // 创建新的 TextDefinition 对象来存储文本的属性
                var newTextDef = new TextDefinition
                {
                    Content = contentString,
                    // 使用转换后的【真实坐标】进行存储
                    StartAndEnd = new PointDefinition { X = realLocation.X, Y = realLocation.Y },

                    // 设置默认样式
                    Color = "#FF000000",
                    Size = 12,
                    Family = Fonts.Detect(placeholder),     // 自动字体
                    BackgroundColor = "#00FFFFFF",
                    BorderColor = "#00FFFFFF"
                };

                // 创建新的 TextLayerItemViewModel
                var textLayer = new TextLayerItemViewModel(newTextDef, textCategory.Children.Count);
                textLayer.RequestRefresh += (s, ev) => RefreshPlotFromLayers(true);
                textCategory.Children.Add(textLayer);

                RefreshPlotFromLayers(true);

                // 选中新添加的图层
                SelectLayer(textLayer);

                // 记录 Undo/Redo
                Action undoText = () =>
                {
                    var cat = LayerTree.FirstOrDefault(c => c is CategoryLayerItemViewModel vm && vm.Name == LanguageService.Instance["text"]) as CategoryLayerItemViewModel;
                    if (cat != null)
                    {
                        if (cat.Children.Contains(textLayer)) cat.Children.Remove(textLayer);
                        if (cat.Children.Count == 0) LayerTree.Remove(cat);
                    }
                    RefreshPlotFromLayers(true);
                };
                Action redoText = () =>
                {
                    var cat = GetOrCreateCategory(LanguageService.Instance["text"]);
                    if (!cat.Children.Contains(textLayer)) cat.Children.Add(textLayer);
                    RefreshPlotFromLayers(true);
                };
                AddUndoState(undoText, redoText);

            }

            // 添加箭头
            if (IsAddingArrow)
            {
                if (!_arrowStartPoint.HasValue)
                {
                    _arrowStartPoint = mouseCoordinates;
                }
                else
                {
                    var startCoord = _arrowStartPoint.Value;
                    var endCoord = mouseCoordinates;

                    PointDefinition finalStartPoint;
                    PointDefinition finalEndPoint;

                    // 如果是三元图，将笛卡尔坐标转换为三元坐标进行存储
                    if (BaseMapType == "Ternary")
                    {
                        // 三元图 使用绘图坐标进行三角转换
                        var ternaryStart = ToTernary(startCoord.X, startCoord.Y, Clockwise);
                        var ternaryEnd = ToTernary(endCoord.X, endCoord.Y, Clockwise);

                        finalStartPoint = new PointDefinition { X = ternaryStart.Item1, Y = ternaryStart.Item2 };
                        finalEndPoint = new PointDefinition { X = ternaryEnd.Item1, Y = ternaryEnd.Item2 };
                    }
                    else
                    {
                        // 笛卡尔坐标系：绘图坐标(Log) -> 真实坐标(Real)
                        var realStart = PlotTransformHelper.ToRealDataCoordinates(WpfPlot1.Plot, startCoord);
                        var realEnd = PlotTransformHelper.ToRealDataCoordinates(WpfPlot1.Plot, endCoord);

                        finalStartPoint = new PointDefinition { X = realStart.X, Y = realStart.Y };
                        finalEndPoint = new PointDefinition { X = realEnd.X, Y = realEnd.Y };
                    }

                    var arrowsCategory = GetOrCreateCategory(LanguageService.Instance["arrow"]);

                    var newArrowDef = new ArrowDefinition
                    {
                        Start = finalStartPoint,
                        End = finalEndPoint,
                        Color = "#000000",
                        ArrowWidth = 1.5f,
                        ArrowheadWidth = 18f,
                        ArrowheadLength = 18f
                    };

                    var arrowLayer = new ArrowLayerItemViewModel(newArrowDef, arrowsCategory.Children.Count);
                    arrowLayer.RequestRefresh += (s, ev) => RefreshPlotFromLayers(true);
                    arrowsCategory.Children.Add(arrowLayer);

                    RefreshPlotFromLayers(true);

                    // 选中新添加的图层
                    SelectLayer(arrowLayer);

                    // 记录 Undo/Redo
                    Action undoArrow = () =>
                    {
                        var cat = LayerTree.FirstOrDefault(c => c is CategoryLayerItemViewModel vm && vm.Name == LanguageService.Instance["arrow"]) as CategoryLayerItemViewModel;
                        if (cat != null)
                        {
                            if (cat.Children.Contains(arrowLayer)) cat.Children.Remove(arrowLayer);
                            if (cat.Children.Count == 0) LayerTree.Remove(cat);
                        }
                        RefreshPlotFromLayers(true);
                    };
                    Action redoArrow = () =>
                    {
                        var cat = GetOrCreateCategory(LanguageService.Instance["arrow"]);
                        if (!cat.Children.Contains(arrowLayer)) cat.Children.Add(arrowLayer);
                        RefreshPlotFromLayers(true);
                    };
                    AddUndoState(undoArrow, redoArrow);

                    // 重置箭头绘制状态
                    if (_tempArrowPlot != null)
                    {
                        WpfPlot1.Plot.Remove(_tempArrowPlot);
                        _tempArrowPlot = null;
                    }
                    _arrowStartPoint = null;
                    // IsAddingArrow = false; // 保持添加模式
                    WpfPlot1.Refresh();
                }
                return;
            }

        }

        /// <summary>
        /// 鼠标右键抬起事件，用于取消画线操作
        /// </summary>
        private void WpfPlot1_MouseRightButtonUp(object? sender, MouseButtonEventArgs e)
        {
            WpfPlot1.Focus();

            // 如果正在拾取点，右键取消
            if (IsPickingPointMode)
            {
                // 取消高亮
                if (_targetPointDefinition != null)
                {
                    _targetPointDefinition.IsHighlighted = false;
                    RefreshPlotFromLayers(true);
                }

                IsPickingPointMode = false;
                _targetPointDefinition = null;
                WpfPlot1.Cursor = Cursors.Arrow;
                MessageHelper.Info(LanguageService.Instance["picking_operation_cancelled"]);      // 拾取操作已取消
                return;
            }

            // 如果当前是高亮状态，或者属性面板打开，鼠标右键单击就是取消选择
            if (_selectedLayer != null || PropertyGridModel != null)
            {
                // 取消选择
                CancelSelected();
                return;
            }

            // 处理多边形绘制完成或取消
            if (IsAddingPolygon)
            {
                // 如果没有顶点，说明处于空闲添加状态，右键直接退出
                if (_polygonVertices.Count == 0)
                {
                    IsAddingPolygon = false;
                    WpfPlot1.Refresh();
                    MessageHelper.Info(LanguageService.Instance["not_enough_vertices_add_polygon_canceled"]);
                    return;
                }

                // 清理预览用的"橡皮筋"线
                if (_tempRubberBandLine != null)
                {
                    WpfPlot1.Plot.Remove(_tempRubberBandLine);
                    _tempRubberBandLine = null;
                }
                // 清理预览用的多边形
                if (_tempPreviewPolygon != null)
                {
                    WpfPlot1.Plot.Remove(_tempPreviewPolygon);
                    _tempPreviewPolygon = null;
                }

                // 如果顶点数少于3，无法构成多边形，视为取消操作
                if (_polygonVertices.Count < 3)
                {
                    // IsAddingPolygon = false; // 保持模式
                    _polygonVertices.Clear();
                    WpfPlot1.Refresh();
                    MessageHelper.Info(LanguageService.Instance["not_enough_vertices_add_polygon_canceled"]);
                    return;
                }

                // 创建多边形
                // 在图层树中找到 "多边形" 分类
                var polygonsCategory = GetOrCreateCategory(LanguageService.Instance["polygon"]);

                // 使用 PlotTransformHelper 将临时绘图坐标转换为真实数据坐标
                var realVertices = _polygonVertices.Select(c => PlotTransformHelper.ToRealDataCoordinates(WpfPlot1.Plot, c));

                // 创建 PolygonDefinition 用于存储属性
                var newPolygonDef = new PolygonDefinition
                {
                    // 现在存入的是真实数据
                    Vertices = new ObservableCollection<PointDefinition>(realVertices.Select(c => new PointDefinition { X = c.X, Y = c.Y })),
                };

                // 创建 PolygonLayerItemViewModel
                var polygonLayer = new PolygonLayerItemViewModel(newPolygonDef, polygonsCategory.Children.Count);
                polygonLayer.RequestRefresh += (s, ev) => RefreshPlotFromLayers(true);
                polygonsCategory.Children.Add(polygonLayer);

                // 刷新整个绘图
                RefreshPlotFromLayers(true);

                // 选中新添加的图层
                SelectLayer(polygonLayer);

                // 记录 Undo/Redo
                Action undoPolygon = () =>
                {
                    var cat = LayerTree.FirstOrDefault(c => c is CategoryLayerItemViewModel vm && vm.Name == LanguageService.Instance["polygon"]) as CategoryLayerItemViewModel;
                    if (cat != null)
                    {
                        if (cat.Children.Contains(polygonLayer)) cat.Children.Remove(polygonLayer);
                        if (cat.Children.Count == 0) LayerTree.Remove(cat);
                    }
                    RefreshPlotFromLayers(true);
                };
                Action redoPolygon = () =>
                {
                    var cat = GetOrCreateCategory(LanguageService.Instance["polygon"]);
                    if (!cat.Children.Contains(polygonLayer)) cat.Children.Add(polygonLayer);
                    RefreshPlotFromLayers(true);
                };
                AddUndoState(undoPolygon, redoPolygon);

                // 重置状态
                _polygonVertices.Clear();
                WpfPlot1.Refresh();
                return;
            }

            if (IsAddingLine)
            {
                // 如果正在添加线条，右键点击则取消操作
                if (_lineStartPoint != null)
                {
                    if (_tempLinePlot != null)
                    {
                        WpfPlot1.Plot.Remove(_tempLinePlot);
                        _tempLinePlot = null;
                    }
                    _lineStartPoint = null;
                    WpfPlot1.Refresh();
                }
                else
                {
                    // 空闲状态，退出模式
                    IsAddingLine = false;
                    WpfPlot1.Refresh();
                    MessageHelper.Info(LanguageService.Instance["add_line_operation_canceled"]);
                }
            }

            // 取消添加文本
            if (IsAddingText)
            {
                IsAddingText = false;
                MessageHelper.Info(LanguageService.Instance["add_text_operation_canceled"]);
            }

            // 取消添加箭头
            if (IsAddingArrow)
            {
                if (_arrowStartPoint != null)
                {
                    if (_tempArrowPlot != null)
                    {
                        WpfPlot1.Plot.Remove(_tempArrowPlot);
                        _tempArrowPlot = null;
                    }
                    _arrowStartPoint = null;
                    WpfPlot1.Refresh();
                }
                else
                {
                    IsAddingArrow = false;
                    WpfPlot1.Refresh();
                    MessageHelper.Info(LanguageService.Instance["add_arrow_operation_canceled"]);
                }
            }
        }

        /// <summary>
        /// 切换绘图模板语言
        /// </summary>
        [RelayCommand]
        private async Task SwitchTemplateLanguage()
        {
            try
            {
                if (CurrentTemplate == null) return;

                // 0. 检查是否有未保存的更改
                if (HasUnsavedChanges)
                {
                    // 当前图解模板有未保存的内容，是否保存？
                    var result = HandyControl.Controls.MessageBox.Show(
                        LanguageService.Instance["unsaved_diagram_template_prompt"],
                        LanguageService.Instance["tips"] ?? "tips",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                    else if (result == MessageBoxResult.Yes)
                    {
                        await PerformSave();
                    }
                }

                // 1. 收集可用语言
                var languages = new HashSet<string>();
                if (!string.IsNullOrEmpty(CurrentTemplate.DefaultLanguage))
                {
                    languages.Add(CurrentTemplate.DefaultLanguage);
                }

                // 从标题获取翻译列表作为参考
                if (CurrentTemplate.Info?.Title?.Label?.Translations != null)
                {
                    foreach (var key in CurrentTemplate.Info.Title.Label.Translations.Keys)
                    {
                        languages.Add(key);
                    }
                }

                // 从分类节点获取
                if (CurrentTemplate.NodeList?.Translations != null)
                {
                    foreach (var key in CurrentTemplate.NodeList.Translations.Keys)
                    {
                        languages.Add(key);
                    }
                }

                if (languages.Count == 0)
                {
                    // 当前模板没有多语言信息
                    MessageHelper.Warning(LanguageService.Instance["template_no_multilingual_info"]);
                    return;
                }

                // 2. 使用通知选择系统
                string currentLang = CurrentDiagramLanguage;
                if (string.IsNullOrEmpty(currentLang))
                {
                    currentLang = LocalizedString.OverrideLanguage ?? LanguageService.CurrentLanguage;
                }

                // 确保选中项存在
                if (!languages.Contains(currentLang) && languages.Count > 0)
                {
                    currentLang = languages.First();
                }

                // 构建显示名称映射
                var displayMap = new Dictionary<string, string>(); // DisplayName -> Code
                var displayList = new List<string>();
                string defaultLang = CurrentTemplate.DefaultLanguage;

                foreach (var code in languages.OrderBy(x => x))
                {
                    string displayName = LanguageService.GetLanguageDisplayName(code);
                    if (!string.IsNullOrEmpty(defaultLang) && code == defaultLang)
                    {
                        displayName = "⭐ " + displayName;
                    }

                    // 防止重复名称
                    if (!displayMap.ContainsKey(displayName))
                    {
                        displayMap[displayName] = code;
                        displayList.Add(displayName);
                    }
                }

                string currentDisplayName = LanguageService.GetLanguageDisplayName(currentLang);
                if (!string.IsNullOrEmpty(defaultLang) && currentLang == defaultLang)
                {
                    currentDisplayName = "⭐ " + currentDisplayName;
                }

                // 异步显示语言选择通知
                var selectedDisplayName = await NotificationManager.Instance.ShowLanguageSelectionAsync(displayList, currentDisplayName);

                if (!string.IsNullOrEmpty(selectedDisplayName) && displayMap.ContainsKey(selectedDisplayName))
                {
                    // 获取对应的语言代码
                    string finalLanguage = displayMap[selectedDisplayName];

                    // 设置当前图解语言，触发 OnCurrentDiagramLanguageChanged 进行更新
                    CurrentDiagramLanguage = finalLanguage;
                    MessageHelper.Success(LanguageService.Instance["language_switch_success"]);
                }
            }
            catch (Exception ex)
            {
                MessageHelper.Error(LanguageService.Instance["language_switch_error"] + ex.Message);
                // 输出记录
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        [RelayCommand]
        private void AddPolygon()
        {
            IsAddingPolygon = true;
        }

        /// <summary>
        /// “添加线条”按钮的命令
        /// </summary>
        [RelayCommand]
        private void AddLine()
        {
            IsAddingLine = true;
        }

        /// <summary>
        /// “添加文本”按钮的命令
        /// </summary>
        [RelayCommand]
        private void AddText()
        {
            IsAddingText = true;
        }

        /// <summary>
        /// 在上方添加行
        /// </summary>
        [RelayCommand]
        private void AddRowUp()
        {
            var worksheet = _dataGrid.CurrentWorksheet;
            if (worksheet == null) return;

            var selection = worksheet.SelectionRange;
            worksheet.InsertRows(selection.Row, 1);
        }

        /// <summary>
        /// 在下方添加行
        /// </summary>
        [RelayCommand]
        private void AddRowDown()
        {
            var worksheet = _dataGrid.CurrentWorksheet;
            if (worksheet == null) return;

            var selection = worksheet.SelectionRange;
            worksheet.InsertRows(selection.Row + selection.Rows, 1);
        }

        /// <summary>
        /// 删除选定行
        /// </summary>
        [RelayCommand]
        private async Task DeleteRow()
        {
            var worksheet = _dataGrid.CurrentWorksheet;
            if (worksheet == null) return;

            // 二次确认
            if (!await MessageHelper.ShowAsyncDialog(LanguageService.Instance["confirm_delete_row"], LanguageService.Instance["Cancel"], LanguageService.Instance["Confirm"]))
            {
                return;
            }

            var selection = worksheet.SelectionRange;

            try
            {
                // 检查是否删除了所有行，ReoGrid 不允许行数为0
                if (selection.Rows >= worksheet.RowCount)
                {
                    // 如果选了所有行，保留一行
                    if (worksheet.RowCount > 1)
                    {
                        worksheet.DeleteRows(selection.Row, worksheet.RowCount - 1);
                        // 自动触发投点
                        PlotDataFromGrid();
                    }
                    return;
                }

                worksheet.DeleteRows(selection.Row, selection.Rows);

                // 自动触发投点
                PlotDataFromGrid();
            }
            catch (Exception ex)
            {
                // 无法删除行:
                MessageHelper.Warning(LanguageService.Instance["failed_to_delete_row"] + ex.Message);
            }
        }

        /// <summary>
        /// 在左侧添加列
        /// </summary>
        [RelayCommand]
        private void AddColumnLeft()
        {
            var worksheet = _dataGrid.CurrentWorksheet;
            if (worksheet == null) return;

            var selection = worksheet.SelectionRange;
            worksheet.InsertColumns(selection.Col, 1);
        }

        /// <summary>
        /// 在右侧添加列
        /// </summary>
        [RelayCommand]
        private void AddColumnRight()
        {
            var worksheet = _dataGrid.CurrentWorksheet;
            if (worksheet == null) return;

            var selection = worksheet.SelectionRange;
            worksheet.InsertColumns(selection.Col + 1, 1);
        }

        /// <summary>
        /// 删除选定列
        /// </summary>
        [RelayCommand]
        private async Task DeleteColumn()
        {
            var worksheet = _dataGrid.CurrentWorksheet;
            if (worksheet == null) return;

            // 二次确认
            if (!await MessageHelper.ShowAsyncDialog(LanguageService.Instance["confirm_delete_column"], LanguageService.Instance["Cancel"], LanguageService.Instance["Confirm"]))
            {
                return;
            }

            var selection = worksheet.SelectionRange;

            // 检查是否有受保护的列
            var protectedColumns = new HashSet<int>();

            // 1. 获取脚本定义的必须列
            if (CurrentTemplate?.Script != null && !string.IsNullOrEmpty(CurrentTemplate.Script.RequiredDataSeries))
            {
                var requiredSeries = CurrentTemplate.Script.RequiredDataSeries
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();

                // 遍历当前表格的所有列头，找到对应列的索引
                for (int col = 0; col < worksheet.ColumnCount; col++)
                {
                    string headerText = worksheet.ColumnHeaders[col].Text;

                    // Category 列
                    if (string.Equals(headerText, "Category", StringComparison.OrdinalIgnoreCase))
                    {
                        protectedColumns.Add(col);
                    }
                    // 脚本定义的列
                    else if (requiredSeries.Contains(headerText))
                    {
                        protectedColumns.Add(col);
                    }
                }
            }

            // 2. 检查选区是否包含受保护的列
            for (int col = selection.Col; col < selection.Col + selection.Cols; col++)
            {
                if (protectedColumns.Contains(col))
                {
                    MessageHelper.Warning(LanguageService.Instance["cannot_delete_protected_column"]);
                    return;
                }
            }

            // 执行删除
            worksheet.DeleteColumns(selection.Col, selection.Cols);
        }

        private System.Threading.CancellationTokenSource _initCts;

        public async Task InitializeAsync(string customJsonContent = null)
        {
            // 0. 取消上一次正在进行的初始化任务，避免并发冲突
            if (_initCts != null)
            {
                try { _initCts.Cancel(); } catch { }
                _initCts.Dispose();
            }
            _initCts = new System.Threading.CancellationTokenSource();
            var token = _initCts.Token;

            // 计时开始
            //var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // 1. 异步加载模板列表
                await InitTemplateAsync(customJsonContent, token);

                if (token.IsCancellationRequested) return;

                // 2. 初始化面包屑
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (token.IsCancellationRequested) return;
                    InitializeBreadcrumbs();
                });

                if (token.IsCancellationRequested) return;

                // 3. 加载所有模板卡片（包含 UI 更新和后台检查）
                var loadTask = await Application.Current.Dispatcher.InvokeAsync(() => LoadAllTemplateCardsAsync(token));
                await loadTask;

                if (token.IsCancellationRequested) return;

                // 4. 刷新视图
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (token.IsCancellationRequested) return;
                    TemplateCardsView.Refresh();
                    // 加载设置
                    LoadSettings();
                });

                // 5. 标记模板库为已刷新 (Clean)
                if (!token.IsCancellationRequested)
                {
                    _isTemplateLibraryDirty = false;
                }
            }
            catch (OperationCanceledException)
            {
                // 忽略取消异常
            }
            catch (Exception ex)
            {
                // 仅在非取消异常时记录或提示
                if (!token.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine($"InitializeAsync Error: {ex.Message}");
                }
            }

            // 计时结束
            //stopwatch.Stop();
            //MessageHelper.Info($"绘图模块加载完成，耗时: {stopwatch.ElapsedMilliseconds} ms");
        }

        // 切换语言——刷新
        public async Task InitTemplateAsync(string customJsonContent = null, System.Threading.CancellationToken token = default)
        {
            try
            {
                var newNode = await Task.Run(() =>
                {
                    if (token.IsCancellationRequested) return null;

                    // 从数据库加载模板摘要
                    var summaries = GraphMapDatabaseService.Instance.GetSummaries();

                    if (token.IsCancellationRequested) return null;

                    // 构建模板树
                    var node = GraphMapTemplateService.BuildTreeFromEntities(summaries);

                    // -------------------------------------------------------------------------
                    // 加载服务器端哈希值并注入到节点中，确保 ServerHash 始终为原始值
                    // -------------------------------------------------------------------------
                    try
                    {
                        string localListPath = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "GraphMapList.json");
                        if (File.Exists(localListPath))
                        {
                            string jsonContent = File.ReadAllText(localListPath);
                            var serverTemplates = JsonSerializer.Deserialize<List<GraphMapTemplateService.JsonTemplateItem>>(jsonContent);

                            if (serverTemplates != null)
                            {
                                var serverHashMap = new Dictionary<Guid, string>();
                                foreach (var item in serverTemplates)
                                {
                                    if (Guid.TryParse(item.ID, out Guid guid))
                                    {
                                        serverHashMap[guid] = item.FileHash;
                                    }
                                }

                                UpdateNodeHashesFromServer(node, serverHashMap);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to inject server hashes: {ex.Message}");
                    }

                    return node;
                });

                if (token.IsCancellationRequested || newNode == null) return;

                // 在 UI 线程上赋值和展开，避免跨线程绑定问题
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    GraphMapTemplateNode = newNode;

                    // 读取配置中的展开层级，默认为2
                    if (!int.TryParse(ConfigHelper.GetConfig("default_tree_expand_level"), out int expandLevel))
                    {
                        expandLevel = 2;
                    }

                    // 递归展开
                    if (GraphMapTemplateNode != null)
                    {
                        ExpandNodes(GraphMapTemplateNode, 1, expandLevel);
                    }
                });
            }
            catch
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // 找不到文件图解模板，请尝试在主页使用'模板修复工具'进行修复。
                    MessageHelper.Error(LanguageService.Instance["diagram_template_not_found_repair_hint"]);
                });
            }
        }

        /// <summary>
        /// 递归展开节点
        /// </summary>
        /// <param name="node">当前节点</param>
        /// <param name="currentLevel">当前层级</param>
        /// <param name="targetLevel">目标展开层级</param>
        private void ExpandNodes(GraphMapTemplateNode node, int currentLevel, int targetLevel)
        {
            if (node == null || currentLevel >= targetLevel) return;

            // 创建副本以避免多线程集合修改异常
            var children = node.Children.ToList();

            foreach (var child in children)
            {
                child.IsExpanded = true;
                ExpandNodes(child, currentLevel + 1, targetLevel);
            }
        }

        /// <summary>
        /// 递归更新节点的哈希值为服务器原始值
        /// </summary>
        private void UpdateNodeHashesFromServer(GraphMapTemplateNode node, Dictionary<Guid, string> serverHashes)
        {
            if (node == null) return;

            // 如果匹配到服务器哈希，强制覆盖 FileHash
            // 这样做的目的是让 UI 显示的树（以及后续生成的 TemplateCardViewModel）携带的是服务器原始 Hash
            // 而数据库中存储的可能是用户修改后的 Hash
            // 从而在 CheckSingleTemplateUpdate 中能正确比较出差异
            if (node.TemplateId.HasValue && !node.IsCustomTemplate)
            {
                if (serverHashes.TryGetValue(node.TemplateId.Value, out string hash))
                {
                    node.FileHash = hash;
                }
            }

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    UpdateNodeHashesFromServer(child, serverHashes);
                }
            }
        }

        /// <summary>
        /// 当鼠标进入绘图区域时调用
        /// </summary>
        private void WpfPlot1_MouseEnter(object? sender, MouseEventArgs e)
        {
            // 仅当追踪模式开启时，才显示十字轴
            if (IsCrosshairVisible)
            {
                CrosshairPlot.IsVisible = true;
                WpfPlot1.Refresh();
            }

            CoordinateStatus = "";
        }

        /// <summary>
        /// 当鼠标离开绘图区域时调用
        /// </summary>
        private void WpfPlot1_MouseLeave(object? sender, MouseEventArgs e)
        {
            // 当鼠标移出时，恢复高亮的对象
            if (_lastHoveredLayer != null)
            {
                RestoreLayerToCorrectState(_lastHoveredLayer);
                _lastHoveredLayer = null;
                _lastHoveredPlottable = null;
                WpfPlot1.Cursor = Cursors.Arrow;
                WpfPlot1.Refresh();
            }

            // 仅当追踪模式开启时，才隐藏十字轴
            if (IsCrosshairVisible)
            {
                CrosshairPlot.IsVisible = false;
                WpfPlot1.Refresh();
            }
        }

        // 缓存的坐标轴定义，避免在 MouseMove 中频繁查询
        private CartesianAxisDefinition? _cachedXAxisDef;
        private CartesianAxisDefinition? _cachedYAxisDef;

        private void UpdateAxisCache()
        {
            _cachedXAxisDef = CurrentTemplate?.Info?.Axes
                .OfType<CartesianAxisDefinition>()
                .FirstOrDefault(ax => ax.Type == "Bottom");

            _cachedYAxisDef = CurrentTemplate?.Info?.Axes
                .OfType<CartesianAxisDefinition>()
                .FirstOrDefault(ax => ax.Type == "Left");
        }

        /// <summary>
        /// 更新所有潜在吸附点的显示
        /// </summary>
        /// <param name="isVisible">是否显示</param>
        private void UpdatePotentialSnapPoints(bool isVisible)
        {
            if (WpfPlot1?.Plot == null) return;

            // 如果不可见，或者要求隐藏，则清理所有标记
            if (!isVisible)
            {
                if (_potentialSnapMarkers.Any())
                {
                    foreach (var marker in _potentialSnapMarkers)
                    {
                        WpfPlot1.Plot.Remove(marker);
                    }
                    _potentialSnapMarkers.Clear();
                    WpfPlot1.Refresh();
                }
                return;
            }

            // 如果要求显示，且已经显示了，先不重复添加（除非列表为空）
            if (_potentialSnapMarkers.Any())
            {
                foreach (var marker in _potentialSnapMarkers)
                {
                    WpfPlot1.Plot.Remove(marker);
                }
                _potentialSnapMarkers.Clear();
            }

            // 收集所有可见图层的端点（排除散点图，防止过密）
            var visibleLayers = FlattenTree(LayerTree).Where(l => l.IsVisible && l.Plottable != null);
            List<Coordinates> pointsToShow = new List<Coordinates>();

            foreach (var layer in visibleLayers)
            {
                if (layer is LineLayerItemViewModel lineLayer && lineLayer.Plottable is ScottPlot.Plottables.LinePlot linePlot)
                {
                    pointsToShow.Add(linePlot.Start);
                    pointsToShow.Add(linePlot.End);
                }
                else if (layer is ArrowLayerItemViewModel arrowLayer && arrowLayer.Plottable is ScottPlot.Plottables.Arrow arrowPlot)
                {
                    pointsToShow.Add(arrowPlot.Base);
                    pointsToShow.Add(arrowPlot.Tip);
                }
                else if (layer is PolygonLayerItemViewModel polygonLayer)
                {
                    if (polygonLayer.PolygonDefinition?.Vertices != null)
                    {
                        foreach (var v in polygonLayer.PolygonDefinition.Vertices)
                        {
                            pointsToShow.Add(PlotTransformHelper.ToRenderCoordinates(WpfPlot1.Plot, v.X, v.Y));
                        }
                    }
                }
            }

            // 为每个端点添加灰色圆圈标记
            foreach (var pt in pointsToShow)
            {
                var marker = WpfPlot1.Plot.Add.Marker(pt);
                marker.Shape = MarkerShape.OpenCircle;
                marker.Size = 6;
                marker.Color = Colors.Gray.WithAlpha(150); // 半透明灰色
                marker.LineWidth = 1;

                _potentialSnapMarkers.Add(marker);
            }

            WpfPlot1.Refresh();
        }

        // 监听绘图模式属性变化，自动更新潜在吸附点显示

        partial void OnIsPickingPointModeChanged(bool value)
        {
            UpdatePotentialSnapPoints(value || IsAddingLine || IsAddingArrow || IsAddingPolygon || IsAddingText);
        }



        /// <summary>
        /// 获取吸附点
        /// </summary>
        /// <param name="mousePixel">鼠标像素位置</param>
        /// <param name="snapDistancePixels">吸附距离阈值</param>
        /// <returns>吸附点的坐标，如果没有吸附则返回 null</returns>
        private Coordinates? GetSnapPoint(Pixel mousePixel, double snapDistancePixels = 10)
        {
            Coordinates? bestSnap = null;
            double minDistanceSq = snapDistancePixels * snapDistancePixels;

            var visibleLayers = FlattenTree(LayerTree).Where(l => l.IsVisible && l.Plottable != null);

            foreach (var layer in visibleLayers)
            {
                List<Coordinates> pointsToCheck = new List<Coordinates>();

                if (layer is LineLayerItemViewModel lineLayer && lineLayer.Plottable is ScottPlot.Plottables.LinePlot linePlot)
                {
                    pointsToCheck.Add(linePlot.Start);
                    pointsToCheck.Add(linePlot.End);
                }
                else if (layer is ArrowLayerItemViewModel arrowLayer && arrowLayer.Plottable is ScottPlot.Plottables.Arrow arrowPlot)
                {
                    pointsToCheck.Add(arrowPlot.Base);
                    pointsToCheck.Add(arrowPlot.Tip);
                }
                else if (layer is PolygonLayerItemViewModel polygonLayer)
                {
                    if (polygonLayer.PolygonDefinition?.Vertices != null)
                    {
                        foreach (var v in polygonLayer.PolygonDefinition.Vertices)
                        {
                            pointsToCheck.Add(PlotTransformHelper.ToRenderCoordinates(WpfPlot1.Plot, v.X, v.Y));
                        }
                    }
                }
                else if (layer is ScatterLayerItemViewModel scatterLayer)
                {
                    if (scatterLayer.DataPoints != null)
                    {
                        foreach (var p in scatterLayer.DataPoints)
                        {
                            pointsToCheck.Add(PlotTransformHelper.ToRenderCoordinates(WpfPlot1.Plot, p));
                        }
                    }
                }

                foreach (var pt in pointsToCheck)
                {
                    Pixel ptPixel = WpfPlot1.Plot.GetPixel(pt);
                    double dx = ptPixel.X - mousePixel.X;
                    double dy = ptPixel.Y - mousePixel.Y;
                    double distSq = dx * dx + dy * dy;

                    if (distSq < minDistanceSq)
                    {
                        minDistanceSq = distSq;
                        bestSnap = pt;
                    }
                }
            }

            return bestSnap;
        }

        [ObservableProperty]
        private string _diagramLanguageStatus = "";

        [ObservableProperty]
        private string _currentDiagramLanguage = "";

        private void AutoDetectFonts()
        {
            if (CurrentTemplate?.Info == null) return;

            // Title
            if (CurrentTemplate.Info.Title != null)
            {
                CurrentTemplate.Info.Title.Family = ScottPlot.Fonts.Detect(CurrentTemplate.Info.Title.Label.Get());
            }

            // Legend
            if (CurrentTemplate.Info.Legend != null)
            {
                string sampleText = CurrentTemplate.Info.Title?.Label.Get() ?? "Legend";
                CurrentTemplate.Info.Legend.Font = ScottPlot.Fonts.Detect(sampleText);
            }

            // Axes
            if (CurrentTemplate.Info.Axes != null)
            {
                foreach (var axis in CurrentTemplate.Info.Axes)
                {
                    axis.Family = ScottPlot.Fonts.Detect(axis.Label.Get());
                }
            }

            // Texts
            if (CurrentTemplate.Info.Texts != null)
            {
                foreach (var text in CurrentTemplate.Info.Texts)
                {
                    text.Family = ScottPlot.Fonts.Detect(text.Content.Get());
                }
            }
        }



        partial void OnCurrentDiagramLanguageChanged(string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            DiagramLanguageStatus = LanguageService.GetLanguageDisplayName(value);
            LocalizedString.OverrideLanguage = value;

            // 自动根据内容检测并设置字体
            AutoDetectFonts();

            // 重新构建图层树 (以更新树节点名称中的多语言文本)
            if (CurrentTemplate != null)
            {
                BuildLayerTreeFromTemplate(CurrentTemplate);
            }

            // 刷新帮助文档
            ReloadHelpDocument(value);

            RefreshPlotFromLayers(true);
        }

        partial void OnCurrentTemplateChanged(GraphMapTemplate value)
        {
            // 确保先关闭提示
            IsDataStateReminderVisible = false;
            // 然后再重置状态，确保新模板开始时是false
            _hasViewedHelpForCurrentDiagram = false;

            if (value != null)
            {
                // 设置网格属性按钮是否可用
                IsGridSettingEnabled = true;

                // 设置当前图解语言为模板默认语言
                CurrentDiagramLanguage = value.DefaultLanguage;
            }
            else
            {
                DiagramLanguageStatus = "";
                CurrentDiagramLanguage = "";
                IsGridSettingEnabled = false;
            }
        }

        /// <summary>
        /// 检查模板是否支持指定语言
        /// </summary>
        private bool IsLanguageSupportedByTemplate(GraphMapTemplate template, string language)
        {
            if (template == null || string.IsNullOrEmpty(language)) return false;

            // 1. 检查 NodeList
            if (template.NodeList?.Translations?.ContainsKey(language) == true) return true;

            // 2. 检查 Title
            if (template.Info?.Title?.Label?.Translations?.ContainsKey(language) == true) return true;

            // 3. 检查 Axes
            if (template.Info?.Axes != null)
            {
                foreach (var axis in template.Info.Axes)
                {
                    if (axis.Label?.Translations?.ContainsKey(language) == true) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 当鼠标在绘图区域移动时调用
        /// </summary>
        [ObservableProperty]
        private string _coordinateStatus = "";

        private double _lastTernaryBottom = -999;
        private double _lastTernaryLeft = -999;
        private double _lastCartesianX = -999;
        private double _lastCartesianY = -999;

        private bool UpdateCoordinateStatus(Coordinates mouseCoordinates, Coordinates? cachedRealCoordinates = null)
        {
            if (BaseMapType == "Ternary")
            {
                var (bottom, left) = ToTernary(mouseCoordinates.X, mouseCoordinates.Y, Clockwise);
                double right = 1 - bottom - left;

                // 检查是否超出三元图实际区域
                if (bottom < 0 || bottom > 1 || left < 0 || left > 1 || right < 0 || right > 1)
                {
                    CoordinateStatus = "";
                    return false;
                }

                // 阈值检查 (约0.01%)
                if (Math.Abs(bottom - _lastTernaryBottom) < 1e-4 && Math.Abs(left - _lastTernaryLeft) < 1e-4)
                {
                    return false;
                }
                _lastTernaryBottom = bottom;
                _lastTernaryLeft = left;

                // Get labels
                string labelA = "A";
                string labelB = "B";
                string labelC = "C";

                if (CurrentTemplate?.Info?.Axes != null)
                {
                    var bottomAxis = CurrentTemplate.Info.Axes.FirstOrDefault(a => a.Type == "Bottom");
                    var leftAxis = CurrentTemplate.Info.Axes.FirstOrDefault(a => a.Type == "Left");
                    var rightAxis = CurrentTemplate.Info.Axes.FirstOrDefault(a => a.Type == "Right");

                    if (bottomAxis != null && bottomAxis.Label != null)
                    {
                        var text = bottomAxis.Label.Get();
                        if (!string.IsNullOrEmpty(text)) labelA = text;
                    }
                    if (leftAxis != null && leftAxis.Label != null)
                    {
                        var text = leftAxis.Label.Get();
                        if (!string.IsNullOrEmpty(text)) labelB = text;
                    }
                    if (rightAxis != null && rightAxis.Label != null)
                    {
                        var text = rightAxis.Label.Get();
                        if (!string.IsNullOrEmpty(text)) labelC = text;
                    }
                }

                CoordinateStatus = $"{labelA}={bottom * 100:F2}%, {labelB}={left * 100:F2}%, {labelC}={right * 100:F2}%";
                return true;
            }
            else
            {
                var realCoords = cachedRealCoordinates ?? PlotTransformHelper.ToRealDataCoordinates(WpfPlot1.Plot, mouseCoordinates);

                // 阈值检查 (保留4位小数，精度0.0001)
                if (Math.Abs(realCoords.X - _lastCartesianX) < 1e-4 && Math.Abs(realCoords.Y - _lastCartesianY) < 1e-4)
                {
                    return false;
                }
                _lastCartesianX = realCoords.X;
                _lastCartesianY = realCoords.Y;

                CoordinateStatus = $"X={realCoords.X:F4}, Y={realCoords.Y:F4}";
                return true;
            }
        }

        private void WpfPlot1_MouseMove(object? sender, MouseEventArgs e)
        {
            // 将WPF的鼠标位置转换为ScottPlot的像素单位
            Pixel mousePixel = new(e.GetPosition(WpfPlot1).X * WpfPlot1.DisplayScale,
                                  e.GetPosition(WpfPlot1).Y * WpfPlot1.DisplayScale);

            // 将像素位置转换为图表的坐标单位
            Coordinates mouseCoordinates = WpfPlot1.Plot.GetCoordinates(mousePixel);

            // 预先计算真实坐标 (用于状态栏和十字定位)
            Coordinates? realCoordinates = null;
            if (BaseMapType != "Ternary")
            {
                realCoordinates = PlotTransformHelper.ToRealDataCoordinates(WpfPlot1.Plot, mouseCoordinates);
            }

            // 吸附逻辑
            bool isDrawingOrPicking = IsPickingPointMode || IsAddingLine || IsAddingArrow || IsAddingPolygon || IsAddingText;
            bool snapChanged = false;
            
            // 引入刷新标记
            bool needRefresh = false;

            if (isDrawingOrPicking)
            {
                var snapPoint = GetSnapPoint(mousePixel);
                if (snapPoint.HasValue)
                {
                    mouseCoordinates = snapPoint.Value;
                    // 如果发生了吸附，需要重新计算真实坐标
                    if (BaseMapType != "Ternary")
                    {
                        realCoordinates = PlotTransformHelper.ToRealDataCoordinates(WpfPlot1.Plot, mouseCoordinates);
                    }

                    if (_snapMarker != null)
                    {
                        if (!_snapMarker.IsVisible || _snapMarker.Location.X != mouseCoordinates.X || _snapMarker.Location.Y != mouseCoordinates.Y)
                        {
                            _snapMarker.Location = mouseCoordinates;
                            _snapMarker.IsVisible = true;
                            snapChanged = true;
                        }
                    }
                }
                else
                {
                    if (_snapMarker != null && _snapMarker.IsVisible)
                    {
                        _snapMarker.IsVisible = false;
                        snapChanged = true;
                    }
                }
            }
            else
            {
                if (_snapMarker != null && _snapMarker.IsVisible)
                {
                    _snapMarker.IsVisible = false;
                    snapChanged = true;
                }
            }

            if (snapChanged) needRefresh = true;

            // 获取当前时间 (毫秒)
            long currentMs = Environment.TickCount64;

            // 坐标更新节流
            if (currentMs - _lastCoordinateUpdateMs > CoordinateUpdateIntervalMs)
            {
                _lastCoordinateUpdateMs = currentMs;
                bool coordChanged = UpdateCoordinateStatus(mouseCoordinates, realCoordinates);

                // 十字轴节流更新逻辑 (仅当坐标值显著变化时才更新)
                if (IsCrosshairVisible && coordChanged)
                {
                    // 确保十字轴可见
                    if (!CrosshairPlot.IsVisible) CrosshairPlot.IsVisible = true;

                    // 更新十字轴的位置
                    CrosshairPlot.Position = mouseCoordinates;

                    // 使用统一计算的真实坐标 (与状态栏保持一致)
                    double xValueToDisplay = realCoordinates?.X ?? mouseCoordinates.X;
                    double yValueToDisplay = realCoordinates?.Y ?? mouseCoordinates.Y;

                    // 更新十字轴上的文本标签以显示实时坐标 (保留4位小数的数字)
                    CrosshairPlot.VerticalLine.Text = $"{xValueToDisplay:N4}";
                    CrosshairPlot.HorizontalLine.Text = $"{yValueToDisplay:N4}";

                    // 刷新图表以应用更改
                    needRefresh = true;
                }
            }

            // 只有当开启吸附，且距离上次检测超过 40ms 时，才执行命中测试
            if (IsSnapSelectionEnabled && !(IsPickingPointMode || IsAddingLine || IsAddingArrow || IsAddingPolygon || IsAddingText) && (currentMs - _lastHitTestTimeMs > HitTestIntervalMs))
            {
                // 更新最后检测时间
                _lastHitTestTimeMs = currentMs;

                var currentHoveredPlottable = GetPlottableAtPixel(mousePixel, 10);
                if (currentHoveredPlottable is Crosshair) currentHoveredPlottable = null;

                if (currentHoveredPlottable != _lastHoveredPlottable)
                {
                    // 1. 恢复上一个高亮的对象
                    if (_lastHoveredLayer != null)
                    {
                        RestoreLayerToCorrectState(_lastHoveredLayer);
                        _lastHoveredLayer = null;
                    }

                    // 2. 高亮当前的新对象
                    if (currentHoveredPlottable != null)
                    {
                        var currentLayer = FindLayerByPlottable(currentHoveredPlottable);
                        if (currentLayer != null)
                        {
                            // 确保不会高亮当前已经选中的对象
                            if (currentLayer != _selectedLayer)
                            {
                                HighlightLayer(currentLayer);
                                _lastHoveredLayer = currentLayer;
                            }
                        }
                    }

                    // 3. 更新鼠标指针
                    WpfPlot1.Cursor = (currentHoveredPlottable != null && currentHoveredPlottable != _selectedLayer?.Plottable) ? Cursors.Hand : Cursors.Arrow;

                    // 4. 记录当前悬浮对象并刷新
                    _lastHoveredPlottable = currentHoveredPlottable;
                    needRefresh = true;
                }
            }
            else
            {
                // 如果关闭了吸附，确保鼠标变回箭头
                if (WpfPlot1.Cursor != Cursors.Arrow)
                    WpfPlot1.Cursor = Cursors.Arrow;
            }

            //  如果正在添加线条且起点已确定，则实时预览线条
            if (IsAddingLine && _lineStartPoint.HasValue)
            {
                // 如果尚未创建临时预览线，则创建
                if (_tempLinePlot == null)
                {
                    _tempLinePlot = WpfPlot1.Plot.Add.Line(_lineStartPoint.Value, mouseCoordinates);
                    _tempLinePlot.Color = Colors.Red; // 设置预览线为红色虚线
                    _tempLinePlot.LinePattern = LinePattern.Dashed;
                }
                else
                {
                    // 如果已存在，直接更新终点坐标，避免重复创建和移除对象
                    _tempLinePlot.End = mouseCoordinates;
                }
                needRefresh = true;
            }
            // 实时预览箭头
            else if (IsAddingArrow && _arrowStartPoint.HasValue)
            {
                if (_tempArrowPlot == null)
                {
                    _tempArrowPlot = WpfPlot1.Plot.Add.Arrow(_arrowStartPoint.Value, mouseCoordinates);
                    _tempArrowPlot.ArrowFillColor = Colors.Red;
                }
                else
                {
                    _tempArrowPlot.Tip = mouseCoordinates;
                }
                needRefresh = true;
            }
            // 处理添加多边形时的鼠标移动预览
            else if (IsAddingPolygon && _polygonVertices.Any())
            {
                var lastVertex = _polygonVertices.Last();
                // 如果橡皮筋线未创建，则创建
                if (_tempRubberBandLine == null)
                {
                    _tempRubberBandLine = WpfPlot1.Plot.Add.Line(lastVertex, mouseCoordinates);
                    _tempRubberBandLine.Color = Colors.Red;
                    _tempRubberBandLine.LinePattern = LinePattern.Dashed;
                }
                else
                {
                    // 仅更新终点
                    _tempRubberBandLine.End = mouseCoordinates;
                }
                needRefresh = true;
            }
            
            if (needRefresh)
            {
                WpfPlot1.Refresh();
            }
        }

        /// <summary>
        /// 初始化面包屑导航
        /// </summary>
        private void InitializeBreadcrumbs()
        {
            Breadcrumbs.Clear();
            Breadcrumbs.Add(new BreadcrumbItem { Name = LanguageService.Instance["all_templates"] });
        }

        private System.Threading.CancellationTokenSource _loadTemplatesCts;

        /// <summary>
        /// 加载所有模板卡片
        /// </summary>
        private async Task LoadAllTemplateCardsAsync(System.Threading.CancellationToken parentToken)
        {
            // 取消之前的加载任务 (如果有独立的任务)
            if (_loadTemplatesCts != null)
            {
                try { _loadTemplatesCts.Cancel(); } catch { }
                _loadTemplatesCts.Dispose();
            }
            _loadTemplatesCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken);
            var token = _loadTemplatesCts.Token;

            TemplateCards.Clear();

            // 1. 快速收集所有模板节点（仅创建对象，不进行 IO 操作）
            CollectTemplatesFromNode(GraphMapTemplateNode);

            // 2. 刷新 UI，先显示出卡片骨架
            TemplateCardsView.Refresh();

            // 3. 后台加载详情（文件检查、缩略图）
            try
            {
                await LoadTemplateDetailsAsync(token);
            }
            catch (OperationCanceledException)
            {

            }
        }

        /// <summary>
        /// 后台加载模板详情（文件检查、缩略图）
        /// </summary>
        private async Task LoadTemplateDetailsAsync(System.Threading.CancellationToken token)
        {
            // 在主线程获取快照，避免多线程集合修改异常
            var cardsSnapshot = TemplateCards.ToList();

            await Task.Run(() =>
            {
                var batchUpdateList = new List<(TemplateCardViewModel Card, TemplateState State, string? ThumbPath, string LocalPath, byte[]? ThumbBytes)>();
                const int BATCH_SIZE = 20;

                foreach (var card in cardsSnapshot)
                {
                    if (token.IsCancellationRequested) return;

                    string localJsonPath = string.Empty;
                    string localThumbPath = string.Empty;

                    // 重新构建路径逻辑
                    if (card.IsCustomTemplate)
                    {
                        if (!string.IsNullOrEmpty(card.TemplatePath))
                        {
                            var customRoot = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "Custom");
                            var templateDir = Path.Combine(customRoot, card.TemplatePath);
                            localJsonPath = Path.Combine(templateDir, $"{card.TemplatePath}.json");
                            localThumbPath = Path.Combine(templateDir, "thumbnail.jpg");
                        }
                    }
                    else
                    {
                        var localDir = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "Default", card.TemplatePath);
                        localJsonPath = Path.Combine(localDir, $"{card.TemplatePath}.json");
                        localThumbPath = Path.Combine(localDir, "thumbnail.jpg");
                    }

                    // 检查文件是否存在
                    TemplateState newState = card.State; // 默认保留原状态
                    string thumbPathToSet = null;

                    byte[]? thumbBytes = null;

                    if (card.TemplateId.HasValue)
                    {
                        // 仅当状态未确定时才查 Entity
                        if (card.State == TemplateState.Loading)
                        {
                            // LiteDB 模式：优先读取数据库状态
                            var entity = GraphMapDatabaseService.Instance.GetTemplate(card.TemplateId.Value);
                            if (entity != null && !string.IsNullOrEmpty(entity.Status))
                            {
                                if (entity.Status == "UP_TO_DATE") newState = TemplateState.Ready;
                                else if (entity.Status == "OUTDATED") newState = TemplateState.UpdateAvailable;
                                else newState = TemplateState.NotDownloaded;
                            }
                            else
                            {
                                // 状态为空或实体不存在，保持 Loading，交给 CheckReadiness 处理
                                newState = TemplateState.Loading;
                            }
                        }

                        // 从数据库加载缩略图
                        try
                        {
                            using (var thumbStream = GraphMapDatabaseService.Instance.GetThumbnail(card.TemplateId.Value))
                            {
                                if (thumbStream != null)
                                {
                                    using (var ms = new MemoryStream())
                                    {
                                        thumbStream.CopyTo(ms);
                                        thumbBytes = ms.ToArray();
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to load thumbnail from DB: {ex.Message}");
                        }
                    }
                    else
                    {
                        // 无 ID，视为未下载
                        newState = TemplateState.NotDownloaded;
                    }

                    batchUpdateList.Add((card, newState, thumbPathToSet, localJsonPath, thumbBytes));

                    // 批量更新 UI
                    if (batchUpdateList.Count >= BATCH_SIZE)
                    {
                        UpdateBatchUI(batchUpdateList, token);
                        batchUpdateList.Clear();
                    }
                }

                // 处理剩余的更新
                if (batchUpdateList.Count > 0)
                {
                    UpdateBatchUI(batchUpdateList, token);
                }
            }, token);
        }

        private void UpdateBatchUI(List<(TemplateCardViewModel Card, TemplateState State, string? ThumbPath, string LocalPath, byte[]? ThumbBytes)> batch, System.Threading.CancellationToken token)
        {
            if (token.IsCancellationRequested) return;

            // 克隆列表以在 Dispatcher 中使用
            var updates = batch.ToList();

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (token.IsCancellationRequested) return;

                foreach (var item in updates)
                {
                    item.Card.LocalFilePath = item.LocalPath; // 设置本地路径
                    item.Card.State = item.State;
                    
                    if (item.ThumbPath != null)
                    {
                        // 仅设置路径，由 Converter 异步加载图片
                        item.Card.ThumbnailPath = item.ThumbPath;
                    }

                    if (item.ThumbBytes != null)
                    {
                        try
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = new MemoryStream(item.ThumbBytes);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            bitmap.Freeze();
                            item.Card.ThumbnailImage = bitmap;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error creating bitmap: {ex.Message}");
                        }
                    }
                }
            });
        }


        /// <summary>
        /// 递归收集节点下的所有模板
        /// </summary>
        private void CollectTemplatesFromNode(GraphMapTemplateNode node)
        {
            if (node?.Children == null) return;

            foreach (var child in node.Children)
            {
                if (!string.IsNullOrEmpty(child.GraphMapPath) || child.IsCustomTemplate)
                {
                    // 2. 初始化 ViewModel (轻量级)
                    var cardVm = new TemplateCardViewModel
                    {
                        Name = child.Name,
                        TemplateId = child.TemplateId,
                        TemplatePath = child.GraphMapPath, // 使用相对路径
                        Category = GetNodePath(child),
                        ServerHash = child.FileHash,    // 注入服务器哈希
                        IsCustomTemplate = child.IsCustomTemplate,
                        State = TemplateState.Loading,  // 初始状态为 Loading
                        ThumbnailImage = null,          // 暂无图片

                        // 注入回调
                        OpenHandler = (vm) => SelectTemplateCardCommand.ExecuteAsync(vm),
                        DownloadHandler = DownloadSingleTemplate,
                        CheckUpdateHandler = CheckSingleTemplateUpdate
                    };

                    // 直接从 Node 状态映射，避免后续重复查库
                    if (!string.IsNullOrEmpty(child.Status))
                    {
                        if (child.Status == "UP_TO_DATE") cardVm.State = TemplateState.Ready;
                        else if (child.Status == "OUTDATED") cardVm.State = TemplateState.UpdateAvailable;
                        else if (child.Status == "NOT_INSTALLED") cardVm.State = TemplateState.NotDownloaded;
                    }

                    TemplateCards.Add(cardVm);
                }
                else
                {
                    CollectTemplatesFromNode(child);
                }
            }
        }

        /// <summary>
        /// 删除模板
        /// </summary>
        [RelayCommand]
        private async Task DeleteTemplate(TemplateCardViewModel card)
        {
            if (card == null || !card.IsCustomTemplate || !card.TemplateId.HasValue) return;

            // 确定要删除该自定义模板吗？
            bool confirm = await MessageHelper.ShowAsyncDialog(
                LanguageService.Instance["confirm_delete_custom_diagram_template"],
                LanguageService.Instance["Cancel"],
                LanguageService.Instance["Confirm"]);

            if (!confirm) return;

            try
            {
                // 使用数据库服务删除
                GraphMapDatabaseService.Instance.DeleteTemplate(card.TemplateId.Value);

                // 4. 刷新界面
                // 重新加载所有模板（包括更新后的自定义列表）
                await InitializeAsync();

                // 模板删除成功
                MessageHelper.Success(LanguageService.Instance["template_delete_success"]);
            }
            catch (Exception ex)
            {
                // 删除失败 + ex
                MessageHelper.Error(LanguageService.Instance["delete_failed"] + ex.Message);
            }
        }

        /// <summary>
        /// 检查模板完整度
        /// </summary>
        private List<string> CheckTemplateCompleteness(GraphMapTemplate template)
        {
            var warnings = new List<string>();

            // 1. Drawing objects check
            bool hasObjects = (template.Info.Lines != null && template.Info.Lines.Any()) ||
                              (template.Info.Polygons != null && template.Info.Polygons.Any()) ||
                              (template.Info.Texts != null && template.Info.Texts.Any()) ||
                              (template.Info.Arrows != null && template.Info.Arrows.Any());

            if (!hasObjects)
            {
                // 绘图对象缺失：模板中未包含任何线、多边形、文本或箭头。
                warnings.Add(LanguageService.Instance["missing_drawing_objects_error"]);
            }

            // 2. Script check
            if (template.Script != null)
            {
                // Check variable names
                if (!string.IsNullOrWhiteSpace(template.Script.RequiredDataSeries))
                {
                    var vars = template.Script.RequiredDataSeries.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => s.Trim()).ToList();

                    foreach (var v in vars)
                    {
                        if (!Regex.IsMatch(v, @"^[a-zA-Z_$][a-zA-Z0-9_$]*$"))
                        {
                            // • 脚本变量名格式错误：'{v}' 不是有效的变量名。
                            warnings.Add(LanguageService.Instance["invalid_script_variable_name"] +
                                v + LanguageService.Instance["is_not_a_valid_variable_name"]);
                        }
                    }

                    // Check execution
                    try
                    {
                        var engine = new Jint.Engine();
                        foreach (var v in vars)
                        {
                            engine.SetValue(v, 1.0); // 测试数据
                        }

                        var result = engine.Evaluate(template.Script.ScriptBody);

                        if (!result.IsArray())
                        {
                            // • 脚本返回值错误：脚本必须返回一个数组。
                            warnings.Add(LanguageService.Instance["script_return_value_error_must_be_array"]);
                        }
                        else
                        {
                            var arr = result.AsArray();
                            int expectedLen = template.TemplateType == "Ternary" ? 3 : 2;
                            if (arr.Length != expectedLen)
                            {
                                // • 脚本返回值长度错误：当前为 {template.TemplateType} 类型，
                                // 应返回 {expectedLen} 个元素，实际返回 {arr.Length} 个。
                                warnings.Add(LanguageService.Instance["script_return_value_length_error"] +
                                    template.TemplateType + LanguageService.Instance["return_type_mismatch_error"] +
                                    expectedLen + LanguageService.Instance["elements_actual_returned"] +
                                    arr.Length + LanguageService.Instance["elements_count_suffix"]);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // • 脚本无法执行：
                        warnings.Add(LanguageService.Instance["script_execution_failed"] + ex.Message);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(template.Script.ScriptBody))
                {
                    try
                    {
                        var engine = new Jint.Engine();
                        var result = engine.Evaluate(template.Script.ScriptBody);
                        if (result.IsArray())
                        {
                            var arr = result.AsArray();
                            int expectedLen = template.TemplateType == "Ternary" ? 3 : 2;
                            if (arr.Length != expectedLen)
                            {
                                // • 脚本返回值长度错误：当前为 {template.TemplateType} 类型，
                                // 应返回 {expectedLen} 个元素，实际返回 {arr.Length} 个。
                                warnings.Add(LanguageService.Instance["script_return_value_length_error"] +
                                template.TemplateType + LanguageService.Instance["return_type_mismatch_error"] +
                                expectedLen + LanguageService.Instance["elements_actual_returned"] +
                                arr.Length + LanguageService.Instance["elements_count_suffix"]);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // • 脚本无法执行：
                        warnings.Add(LanguageService.Instance["script_execution_failed"] + ex.Message);
                    }
                }
            }

            // 3. Translation check
            var localizedStrings = new List<LocalizedString>();
            if (template.Info.Title?.Label != null) localizedStrings.Add(template.Info.Title.Label);
            if (template.Info.Texts != null) localizedStrings.AddRange(template.Info.Texts.Select(t => t.Content));
            if (template.Info.Axes != null)
            {
                foreach (var axis in template.Info.Axes)
                {
                    if (axis.Label != null) localizedStrings.Add(axis.Label);
                }
            }

            bool missingTranslations = false;
            foreach (var loc in localizedStrings)
            {
                if (loc != null && loc.Translations != null && loc.Translations.Count <= 1)
                {
                    missingTranslations = true;
                    break;
                }
            }

            if (missingTranslations)
            {
                // • 翻译缺失：部分文本缺少多语言翻译。
                warnings.Add(LanguageService.Instance["missing_translation_warning"]);
            }

            return warnings;
        }

        /// <summary>
        /// 导出模板
        /// </summary>
        [RelayCommand]
        private void ExportTemplate(TemplateCardViewModel card)
        {
            if (card == null || !card.TemplateId.HasValue) return;

            try
            {
                // 1. 获取模板数据
                var entity = GraphMapDatabaseService.Instance.GetTemplate(card.TemplateId.Value);
                if (entity == null || entity.Content == null)
                {
                    MessageHelper.Error(LanguageService.Instance["diagram_template_source_missing"]);
                    return;
                }

                var template = entity.Content;

                // 完整度检查
                var warnings = CheckTemplateCompleteness(template);
                if (warnings.Any())
                {
                    string msg = LanguageService.Instance["template_potential_issues_detected"] +
                                 "\n\n" + string.Join("\n", warnings) +
                                 "\n\n" + LanguageService.Instance["ignore_issues_and_continue_export"];

                    var result = HandyControl.Controls.MessageBox.Show(msg, LanguageService.Instance["completeness_check"], MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result != MessageBoxResult.Yes)
                    {
                        return; // 取消导出
                    }
                }

                // 2. 选择保存位置
                var dialog = new VistaSaveFileDialog();
                dialog.Filter = "Zip Files (*.zip)|*.zip";
                dialog.DefaultExt = "zip";

                string templateFileName = entity.Name;
                if (string.IsNullOrEmpty(templateFileName))
                {
                    templateFileName = Path.GetFileName(entity.GraphMapPath);
                }

                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    templateFileName = templateFileName.Replace(c, '_');
                }

                dialog.FileName = $"{templateFileName}.zip";

                if (dialog.ShowDialog() == true)
                {
                    string destinationPath = dialog.FileName;

                    // 创建临时目录
                    string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    Directory.CreateDirectory(tempDir);

                    try
                    {
                        // 3. 准备文件
                        // 3.1 JSON 文件
                        string jsonFileName = templateFileName + ".json";
                        string jsonPath = Path.Combine(tempDir, jsonFileName);

                        var options = new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                        };
                        string jsonContent = JsonSerializer.Serialize(template, options);
                        File.WriteAllText(jsonPath, jsonContent);

                        // 3.2 缩略图 (如果有)
                        using (var thumbStream = GraphMapDatabaseService.Instance.GetThumbnail(entity.Id))
                        {
                            if (thumbStream != null)
                            {
                                string thumbPath = Path.Combine(tempDir, "thumbnail.jpg");
                                using (var fileStream = File.Create(thumbPath))
                                {
                                    thumbStream.CopyTo(fileStream);
                                }
                            }
                        }

                        // 3.3 导出帮助文档 (RTF)
                        if (entity.HelpDocuments != null && entity.HelpDocuments.Count > 0)
                        {
                            foreach (var doc in entity.HelpDocuments)
                            {
                                try
                                {
                                    string rtfFileName = $"{doc.Key}.rtf";
                                    string rtfPath = Path.Combine(tempDir, rtfFileName);
                                    File.WriteAllText(rtfPath, doc.Value);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"导出帮助文档失败 ({doc.Key}): {ex.Message}");
                                }
                            }
                        }

                        // 4. 打包
                        if (File.Exists(destinationPath))
                        {
                            File.Delete(destinationPath);
                        }

                        ZipFile.CreateFromDirectory(tempDir, destinationPath);

                        MessageHelper.Success(LanguageService.Instance["diagram_template_export_success"]);
                    }
                    finally
                    {
                        // 清理临时目录
                        if (Directory.Exists(tempDir))
                        {
                            Directory.Delete(tempDir, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageHelper.Error(LanguageService.Instance["export_failed"] + ex.Message);
            }
        }

        /// <summary>
        /// 点击模板分类节点
        /// </summary>
        [RelayCommand]
        private async Task SelectTreeViewItem(GraphMapTemplateNode graphMapTemplateNode)
        {
            if (graphMapTemplateNode == null) return;

            // 如果是模板文件（叶子节点）
            if (!string.IsNullOrEmpty(graphMapTemplateNode.GraphMapPath))
            {
                // 显示单个模板卡片
                await ShowSingleTemplateCard(graphMapTemplateNode);
            }
            else
            {
                // 显示分类下的所有模板卡片
                await ShowCategoryTemplateCards(graphMapTemplateNode);
            }
        }

        /// <summary>
        /// 显示单个模板卡片
        /// </summary>
        private async Task ShowSingleTemplateCard(GraphMapTemplateNode templateNode)
        {
            // 取消之前的加载任务
            _loadTemplatesCts?.Cancel();
            _loadTemplatesCts = new System.Threading.CancellationTokenSource();
            var token = _loadTemplatesCts.Token;

            TemplateCards.Clear();

            // 初始化 ViewModel
            var cardVm = new TemplateCardViewModel
            {
                Name = templateNode.Name,
                TemplateId = templateNode.TemplateId,
                TemplatePath = templateNode.GraphMapPath,
                Category = GetNodePath(templateNode),
                ServerHash = templateNode.FileHash,
                IsCustomTemplate = templateNode.IsCustomTemplate,
                State = TemplateState.Loading, // 初始状态为 Loading
                ThumbnailImage = null,

                // 注入回调
                OpenHandler = (vm) => SelectTemplateCardCommand.ExecuteAsync(vm),
                DownloadHandler = DownloadSingleTemplate,
                CheckUpdateHandler = CheckSingleTemplateUpdate
            };

            // 直接从 Node 状态映射，避免后续重复查库
            if (!string.IsNullOrEmpty(templateNode.Status))
            {
                if (templateNode.Status == "UP_TO_DATE") cardVm.State = TemplateState.Ready;
                else if (templateNode.Status == "OUTDATED") cardVm.State = TemplateState.UpdateAvailable;
                else if (templateNode.Status == "NOT_INSTALLED") cardVm.State = TemplateState.NotDownloaded;
            }

            TemplateCards.Add(cardVm);
            UpdateBreadcrumbs(templateNode);

            // 触发后台加载详情
            try
            {
                await LoadTemplateDetailsAsync(token);
            }
            catch (OperationCanceledException) { }
        }

        /// <summary>
        /// 显示分类下的模板卡片
        /// </summary>
        private async Task ShowCategoryTemplateCards(GraphMapTemplateNode categoryNode)
        {
            // 取消之前的加载任务
            _loadTemplatesCts?.Cancel();
            _loadTemplatesCts = new System.Threading.CancellationTokenSource();
            var token = _loadTemplatesCts.Token;

            TemplateCards.Clear();
            // 递归收集该节点下的所有模板文件
            CollectTemplatesFromNode(categoryNode);
            UpdateBreadcrumbs(categoryNode);

            // 触发后台加载详情
            try
            {
                await LoadTemplateDetailsAsync(token);
            }
            catch (OperationCanceledException) { }
        }

        /// <summary>
        /// 更新面包屑导航
        /// </summary>
        private void UpdateBreadcrumbs(GraphMapTemplateNode currentNode)
        {
            if (currentNode == null)
            {
                InitializeBreadcrumbs(); // 如果当前节点为空，则只显示根节点
                return;
            }

            // 创建一个列表，用来存放从当前节点到其父节点的完整路径
            var path = new List<GraphMapTemplateNode>();
            var current = currentNode;

            // 从当前节点开始，利用Parent属性向上遍历，直到某个节点的Parent为空
            while (current != null && current.Parent != null)
            {
                path.Add(current);
                current = current.Parent;
            }

            path.Reverse();

            // 清空并重新构建面包屑集合
            Breadcrumbs.Clear();
            Breadcrumbs.Add(new BreadcrumbItem { Name = LanguageService.Instance["all_templates"] });

            // 将路径上的所有节点添加到面包屑集合中
            foreach (var node in path)
            {
                Breadcrumbs.Add(new BreadcrumbItem { Name = node.Name, Node = node });
            }
        }

        /// <summary>
        /// 获取节点路径
        /// </summary>
        private string GetNodePath(GraphMapTemplateNode node)
        {
            // 获取父节点作为类别
            // 如果父节点为空，则返回一个默认值
            return node?.Parent?.Name ?? LanguageService.Instance["uncategorized"];
        }

        /// <summary>
        /// 点击模板卡片，进入绘图模式
        /// </summary>
        [RelayCommand]
        private async Task SelectTemplateCard(TemplateCardViewModel card)
        {
            if (card == null) return;

            try
            {
                // 确保清除语言覆盖，使用软件默认配置
                LocalizedString.OverrideLanguage = null;

                // 设置是否为自定义模板 (官方模板不显示未保存提醒)
                _isCurrentTemplateCustom = card.IsCustomTemplate;
                UpdateHelpDocReadOnlyState();

                // 切换到绘图模式
                IsTemplateMode = false;
                IsPlotMode = true;

                if (card.TemplateId.HasValue)
                {
                    // LiteDB 模式
                    _currentTemplateId = card.TemplateId.Value;
                    if (!await LoadAndBuildLayersFromDb(card.TemplateId.Value))
                    {
                        await BackToTemplateMode();
                        return;
                    }
                }
                else
                {
                    // 兼容旧的文件系统模式
                    _currentTemplateId = null;
                    // 加载模板文件
                    string baseDir;
                    if (card.IsCustomTemplate)
                    {
                        baseDir = Path.Combine(FileHelper.GetAppPath(),
                            "Data", "PlotData", "Custom", card.TemplatePath);
                    }
                    else
                    {
                        baseDir = Path.Combine(FileHelper.GetAppPath(),
                            "Data", "PlotData", "Default", card.TemplatePath);
                    }

                    if (!Directory.Exists(baseDir))
                    {
                        // 图解模板目录不存在：
                        throw new DirectoryNotFoundException(
                            LanguageService.Instance["diagram_template_directory_not_found"] +
                            baseDir);
                    }

                    var templateFilePath = Path.Combine(baseDir, $"{card.TemplatePath}.json");
                    _currentTemplateFilePath = templateFilePath;
                    if (!await LoadAndBuildLayers(templateFilePath))
                    {
                        await BackToTemplateMode();
                        return;
                    }
                }

                CenterPlot();   // 视图复位

                // 加载底图模板的说明文件
                ReloadHelpDocument(CurrentDiagramLanguage);

                // 加载数据表格控件
                PrepareDataGridForInput();
            }
            catch (Exception ex)
            {
                // 无法加载模板
                MessageHelper.Error(LanguageService.Instance["failed_to_load_diagram_template"] +
                    $"{ex.Message}\n{ex.StackTrace}");
                await BackToTemplateMode();
            }
        }

        /// <summary>
        /// 显示绘图模板的帮助说明
        /// </summary>
        [RelayCommand]
        private void ShowTemplateInfo()
        {
            IsShowTemplateInfo = !IsShowTemplateInfo;

            // 如果打开了帮助，标记为已读，并隐藏提示
            if (IsShowTemplateInfo)
            {
                _hasViewedHelpForCurrentDiagram = true;
                IsDataStateReminderVisible = false;
            }
        }


        /// <summary>
        /// 返回模板库-浏览模式
        /// </summary>
        [RelayCommand]
        private async Task BackToTemplateMode()
        {
            // 检查是否有未保存的更改 (仅针对自定义模板)
            if (_isCurrentTemplateCustom && IsTemplateModified())
            {
                // 当前模板有未保存的内容，是否保存？
                var result = HandyControl.Controls.MessageBox.Show(
                    LanguageService.Instance["unsaved_template_changes_confirm"],
                    LanguageService.Instance["tips"] ?? "提示",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
                else if (result == MessageBoxResult.Yes)
                {
                    await PerformSave();
                }
                // If No, proceed without saving
            }

            // 1. 优先切换 UI 状态，提升响应速度
            IsTemplateMode = true;
            IsPlotMode = false;
            RibbonTabIndex = 0;
            IsShowTemplateInfo = false;

            // 2. 在返回模板库之前，完全重置绘图状态
            ResetPlotStateToDefault();

            // 3. 仅在必要时重新加载所有模板（包括更新后的自定义列表）
            if (_isTemplateLibraryDirty)
            {
                await InitializeAsync();
            }
        }

        /// <summary>
        /// 检查当前模板是否有未保存的更改
        /// </summary>
        [ObservableProperty]
        private bool _hasUnsavedChanges;

        private void CheckUnsavedChanges()
        {
            // 只有自定义模板才检测未保存状态
            if (_isCurrentTemplateCustom)
            {
                HasUnsavedChanges = IsTemplateModified();
            }
            else
            {
                HasUnsavedChanges = false;
            }

            // 发送消息通知主窗口更新标题
            WeakReferenceMessenger.Default.Send(new UnsavedChangesMessage(HasUnsavedChanges));
        }

        private void UpdateTemplateInfoFromLayers(GraphMapTemplate template)
        {
            // 清空模板中原有的动态绘图元素列表
            template.Info.Lines.Clear();
            template.Info.Texts.Clear();
            template.Info.Annotations.Clear();
            template.Info.Points.Clear();
            template.Info.Polygons.Clear();
            template.Info.Arrows.Clear();
            template.Info.Functions.Clear();

            // 遍历当前的图层列表，收集所有图元信息
            var allLayers = FlattenTree(LayerTree);
            foreach (var layer in allLayers)
            {
                switch (layer)
                {
                    case LineLayerItemViewModel lineLayer:
                        template.Info.Lines.Add(lineLayer.LineDefinition);
                        break;
                    case TextLayerItemViewModel textLayer:
                        template.Info.Texts.Add(textLayer.TextDefinition);
                        break;
                    case AnnotationLayerItemViewModel annotationLayer:
                        template.Info.Annotations.Add(annotationLayer.AnnotationDefinition);
                        break;
                    case ArrowLayerItemViewModel arrowLayer:
                        template.Info.Arrows.Add(arrowLayer.ArrowDefinition);
                        break;
                    case PolygonLayerItemViewModel polygonLayer:
                        template.Info.Polygons.Add(polygonLayer.PolygonDefinition);
                        break;
                    case FunctionLayerItemViewModel functionLayer:
                        template.Info.Functions.Add(functionLayer.FunctionDefinition);
                        break;
                }
            }
        }

        private string SerializeTemplate(GraphMapTemplate template)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false, // 紧凑模式
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            return JsonSerializer.Serialize(template, options);
        }

        private bool IsTemplateModified()
        {
            if (CurrentTemplate == null || CurrentTemplate.Info == null) return false;

            // 如果原始 JSON 为空，说明还未初始化完成，或者不是从文件加载的
            if (string.IsNullOrEmpty(_originalTemplateJson)) return false;

            try
            {
                // 1. 克隆当前模板
                string currentJson = SerializeTemplate(CurrentTemplate);
                var clonedTemplate = JsonSerializer.Deserialize<GraphMapTemplate>(currentJson, new JsonSerializerOptions
                {
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                });

                if (clonedTemplate == null) return false;

                // 2. 将当前 LayerTree 的状态同步到克隆对象中
                UpdateTemplateInfoFromLayers(clonedTemplate);

                // 3. 序列化克隆对象，得到“当前完整状态”的 JSON
                string finalJson = SerializeTemplate(clonedTemplate);

                // 4. 与原始 JSON 对比 (忽略字体相关属性的变化)
                return NormalizeJsonForComparison(finalJson) != NormalizeJsonForComparison(_originalTemplateJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IsTemplateModified check failed: {ex.Message}");
                return false;
            }
        }

        private string NormalizeJsonForComparison(string json)
        {
            if (string.IsNullOrEmpty(json)) return json;
            // Ignore font related properties changes: "Family", "Font"
            // We replace their values with a placeholder to ensure consistency
            string pattern = "\"((Family)|(Font))\"\\s*:\\s*\"(.*?)\"";
            return System.Text.RegularExpressions.Regex.Replace(json, pattern, "\"$1\": \"IGNORE\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// 面包屑导航点击
        /// </summary>
        [RelayCommand]
        private async Task NavigateToBreadcrumb(BreadcrumbItem item)
        {
            if (item == null) return;

            if (item.Node == null)
            {
                // 返回全部模板
                await LoadAllTemplateCardsAsync(default);
                InitializeBreadcrumbs();
            }
            else
            {
                // 获取对应的节点
                var targetNode = item.Node;

                await ShowCategoryTemplateCards(targetNode);
            }
        }

        private static IEnumerable<LayerItemViewModel> FlattenTree(IEnumerable<LayerItemViewModel> nodes)
        {
            foreach (var node in nodes)
            {
                yield return node;
                foreach (var child in FlattenTree(node.Children))
                {
                    yield return child;
                }
            }
        }

        /// <summary>
        /// 属性面板对象改变时触发
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        partial void OnPropertyGridModelChanged(object? oldValue, object? newValue)
        {
            // 只有当显示的不是空对象时，才强制关闭脚本面板
            if (newValue is not EmptyPropertyModel)
            {
                ScriptsPropertyGrid = false;
            }

            // 如果旧的对象不为空且实现了 INotifyPropertyChanged，就取消订阅，防止内存泄漏
            if (oldValue is INotifyPropertyChanged oldModel)
            {
                oldModel.PropertyChanged -= PropertyGridModel_PropertyChanged;
            }

            // 如果新的对象不为空且实现了 INotifyPropertyChanged，就订阅它的 PropertyChanged 事件
            if (newValue is INotifyPropertyChanged newModel)
            {
                newModel.PropertyChanged += PropertyGridModel_PropertyChanged;
            }
        }

        /// <summary>
        /// 根据当前的选中状态 (_selectedLayer)，重新应用高亮或遮罩效果
        /// 在全量重绘后调用
        /// </summary>
        private void ReapplySelectionVisualState()
        {
            // 如果当前没有选中任何图层，什么都不用做（Render 默认就是正常状态）
            // 如果选中的是分类文件夹，也不应用遮罩
            if (_selectedLayer == null || _selectedLayer is CategoryLayerItemViewModel) return;

            // 获取所有实现了 IPlotLayer 的图层
            var allPlotLayers = FlattenTree(LayerTree).OfType<IPlotLayer>();

            foreach (var layer in allPlotLayers)
            {
                // 检查是否是主选中项或者在多选列表中
                if (layer == _selectedLayer || (layer is LayerItemViewModel vm && SelectedLayers.Contains(vm)))
                {
                    // 选中项：恢复正常显示
                    layer.Restore();
                }
                else
                {
                    // 非选中项：变暗
                    layer.Dim();
                }
            }
        }

        /// <summary>
        /// 当属性面板中的值改变时，此方法被调用实现更新
        /// </summary>
        private void PropertyGridModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender == null) return;

            // 1. 多选同步逻辑
            var propInfo = sender.GetType().GetProperty(e.PropertyName);
            if (propInfo != null && propInfo.CanRead && propInfo.CanWrite)
            {
                var newValue = propInfo.GetValue(sender);

                // 遍历其他选中项并应用更改
                if (SelectedLayers.Count > 1)
                {
                    foreach (var layer in SelectedLayers)
                    {
                        if (layer == _selectedLayer) continue;

                        var def = GetLayerDefinition(layer);
                        // 确保类型匹配
                        if (def != null && def.GetType() == sender.GetType())
                        {
                            try
                            {
                                propInfo.SetValue(def, newValue);
                                // 应用修改后，恢复显示状态
                                if (layer is IPlotLayer plotLayer)
                                {
                                    plotLayer.Restore();
                                }
                            }
                            catch
                            {
                                // 忽略设置失败
                            }
                        }
                    }
                }
            }

            // 2. 决定是否保留当前视图范围
            // 如果修改的是坐标轴范围，则不保留（使用新设置的范围），否则保留当前平移缩放状态
            bool preserveLimits = true;
            if (sender is Models.CartesianAxisDefinition && 
                (e.PropertyName == "Minimum" || e.PropertyName == "Maximum" || e.PropertyName == "IsAutoRange"))
            {
                preserveLimits = false;
            }

            // 3. 全量重绘
            RefreshPlotFromLayers(preserveLimits);

            // 4. 恢复遮罩
            ReapplySelectionVisualState();

            // 5. 检查未保存更改
            CheckUnsavedChanges();
        }

        /// <summary>
        /// 使用JavaScript脚本计算坐标
        /// </summary>
        /// <param name="dataRow">数据行</param>
        /// <param name="dataColumns">参与计算的数据列名</param>
        /// <param name="scriptBody">脚本内容</param>
        /// <returns>返回计算结果的double数组，如果计算失败或返回类型不正确则返回null</returns>
        private double[]? CalculateCoordinatesUsingScript(Jint.Engine engine, DataRow dataRow, List<string> dataColumns, string scriptBody)
        {
            try
            {

                // 将数据列的值注入到JavaScript环境中
                foreach (string columnName in dataColumns)
                {
                    if (double.TryParse(dataRow[columnName]?.ToString(), out double value))
                    {
                        engine.SetValue(columnName, value);
                    }
                    else
                    {
                        // 如果无法解析为数字，注入null或0
                        engine.SetValue(columnName, 0);
                    }
                }

                // 执行脚本并获取结果
                var result = engine.Evaluate(scriptBody);

                // 检查返回结果是否为数组
                if (result.IsArray())
                {
                    var array = result.AsArray();
                    // 将JavaScript数组转换为C#的double数组
                    var values = new double[array.Length];
                    for (int i = 0; i < array.Length; i++)
                    {
                        if (array[i].IsNumber())
                        {
                            values[i] = array[i].AsNumber();
                        }
                        else
                        {
                            // 如果数组中有任何一个元素不是数字，则认为结果无效
                            return null;
                        }
                    }
                    return values; // 返回包含所有数值的数组
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(LanguageService.Instance["script_execution_failed"] + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 从数据库加载模板、构建图层树并刷新绘图
        /// </summary>
        private async Task<bool> LoadAndBuildLayersFromDb(Guid templateId)
        {
            // 重置编辑确认状态和标签页索引
            _hasConfirmedEditMode = false;
            RibbonTabIndex = 0;

            var entity = await Task.Run(() => GraphMapDatabaseService.Instance.GetTemplate(templateId));

            if (entity == null || entity.Content == null)
            {
                return false;
            }

            CurrentTemplate = entity.Content;

            // 如果 Content 中的 NodeList 丢失，尝试从 Entity 的元数据中恢复
            // 避免在保存时因 NodeList 为空而导致数据库中的分类信息被清空
            if ((CurrentTemplate.NodeList == null || CurrentTemplate.NodeList.Translations.Count == 0) &&
                (entity.NodeList != null && entity.NodeList.Translations.Count > 0))
            {
                CurrentTemplate.NodeList = entity.NodeList;
            }

            // 初始化原始状态
            _originalTemplateJson = SerializeTemplate(CurrentTemplate);

            // 检查模板类型
            if (CurrentTemplate.TemplateType != "Cartesian" && CurrentTemplate.TemplateType != "Ternary")
            {
                MessageHelper.Warning(LanguageService.Instance["template_type_not_supported_please_update"]);
                return false;
            }

            // 初始化网格支持属性
            if (CurrentTemplate.Info != null && CurrentTemplate.Info.Grid != null)
            {
                bool isTernary = CurrentTemplate.TemplateType == "Ternary";
                CurrentTemplate.Info.Grid.IsMinorGridSupported = !isTernary;
                CurrentTemplate.Info.Grid.IsAlternatingFillSupported = !isTernary;
            }

            // 确保 OverrideLanguage 被正确设置
            if (!string.IsNullOrEmpty(CurrentTemplate.DefaultLanguage))
            {
                LocalizedString.OverrideLanguage = CurrentTemplate.DefaultLanguage;
                DiagramLanguageStatus = LanguageService.GetLanguageDisplayName(CurrentTemplate.DefaultLanguage);

                if (_currentDiagramLanguage != CurrentTemplate.DefaultLanguage)
                {
                    CurrentDiagramLanguage = CurrentTemplate.DefaultLanguage;
                }
            }

            // 自动根据内容检测并设置字体
            AutoDetectFonts();

            // 根据加载的模板数据，构建【图层树】
            BuildLayerTreeFromTemplate(CurrentTemplate);

            // 根据新建的【图层树】来渲染前端
            RefreshPlotFromLayers();
            return true;
        }

        /// <summary>
        /// 加载模板、构建图层树并刷新绘图
        /// </summary>
        /// <param name="templatePath">模板文件的路径</param>
        private async Task<bool> LoadAndBuildLayers(string templatePath)
        {
            // 重置编辑确认状态和标签页索引
            _hasConfirmedEditMode = false;
            RibbonTabIndex = 0;

            if (!File.Exists(templatePath))
            {
                // 文件不存在
                return false;
            }

            // 读取并反序列化模板文件
            var templateJsonContent = await File.ReadAllTextAsync(templatePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            CurrentTemplate = JsonSerializer.Deserialize<GraphMapTemplate>(templateJsonContent, options);

            if (CurrentTemplate == null) return false;

            // 初始化原始状态
            _originalTemplateJson = SerializeTemplate(CurrentTemplate);

            // 检查模板类型
            if (CurrentTemplate.TemplateType != "Cartesian" && CurrentTemplate.TemplateType != "Ternary")
            {
                MessageHelper.Warning(LanguageService.Instance["template_type_not_supported_please_update"]);
                return false;
            }

            // 初始化网格支持属性
            if (CurrentTemplate.Info != null && CurrentTemplate.Info.Grid != null)
            {
                bool isTernary = CurrentTemplate.TemplateType == "Ternary";
                CurrentTemplate.Info.Grid.IsMinorGridSupported = !isTernary;
                CurrentTemplate.Info.Grid.IsAlternatingFillSupported = !isTernary;
            }

            // 确保 OverrideLanguage 被正确设置，即使 CurrentDiagramLanguage 没有变化
            if (!string.IsNullOrEmpty(CurrentTemplate.DefaultLanguage))
            {
                LocalizedString.OverrideLanguage = CurrentTemplate.DefaultLanguage;
                DiagramLanguageStatus = LanguageService.GetLanguageDisplayName(CurrentTemplate.DefaultLanguage);

                // 确保 ViewModel 属性同步
                if (_currentDiagramLanguage != CurrentTemplate.DefaultLanguage)
                {
                    CurrentDiagramLanguage = CurrentTemplate.DefaultLanguage;
                }
            }

            // 自动根据内容检测并设置字体 (覆盖 JSON 中的设置)
            AutoDetectFonts();

            // 根据加载的模板数据，构建【图层树】
            BuildLayerTreeFromTemplate(CurrentTemplate);

            // 根据新建的【图层树】来渲染前端
            RefreshPlotFromLayers();
            return true;
        }

        /// <summary>
        /// 使用当前加载的模板数据填充 LayerTree 集合
        /// 负责添加图层对象
        /// </summary>
        private List<CategoryLayerItemViewModel> CreateLayersFromTemplate(GraphMapTemplate template,
            bool attachEvents = true)
        {
            var list = new List<CategoryLayerItemViewModel>();
            var info = template.Info;

            // 1. 坐标轴图层 (最底层)
            var axesCategory = new CategoryLayerItemViewModel(LanguageService.Instance["axes"]);
            foreach (var axis in info.Axes)
            {
                var axisLayer = new AxisLayerItemViewModel(axis);
                if (attachEvents)
                {
                    axisLayer.RequestRefresh += (s, e) => RefreshPlotFromLayers(true);
                    axisLayer.RequestStyleUpdate += (s, e) => WpfPlot1.Refresh();
                }
                axesCategory.Children.Add(axisLayer);
            }
            if (axesCategory.Children.Any()) list.Add(axesCategory);

            // 2. 多边形图层
            var polygonsCategory = new CategoryLayerItemViewModel(LanguageService.Instance["polygon"]);
            for (int i = 0; i < info.Polygons.Count; i++)
            {
                var polygonLayer = new PolygonLayerItemViewModel(info.Polygons[i], i);
                if (attachEvents)
                {
                    polygonLayer.RequestRefresh += (s, e) => RefreshPlotFromLayers();
                    polygonLayer.RequestStyleUpdate += (s, e) => WpfPlot1.Refresh();
                }
                polygonsCategory.Children.Add(polygonLayer);
            }
            if (polygonsCategory.Children.Any()) list.Add(polygonsCategory);

            // 3. 线图层
            var linesCategory = new CategoryLayerItemViewModel(LanguageService.Instance["line"]);
            for (int i = 0; i < info.Lines.Count; i++)
            {
                var lineLayer = new LineLayerItemViewModel(info.Lines[i], i);
                if (attachEvents)
                {
                    lineLayer.RequestRefresh += (s, e) => RefreshPlotFromLayers();
                    lineLayer.RequestStyleUpdate += (s, e) => WpfPlot1.Refresh();
                }
                linesCategory.Children.Add(lineLayer);
            }
            if (linesCategory.Children.Any()) list.Add(linesCategory);

            // 4. 函数图层
            var functionCategory = new CategoryLayerItemViewModel("Function"); // Consider localization
            for (int i = 0; i < info.Functions.Count; i++)
            {
                var funcLayer = new FunctionLayerItemViewModel(info.Functions[i], i);
                if (attachEvents) funcLayer.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(FunctionLayerItemViewModel.IsVisible))
                        RefreshPlotFromLayers();
                };
                functionCategory.Children.Add(funcLayer);
            }
            if (functionCategory.Children.Any()) list.Add(functionCategory);

            // 5. 点图层 (位于线条之上，在文本之下)
            var pointsCategory = new CategoryLayerItemViewModel(LanguageService.Instance["point"]);
            for (int i = 0; i < info.Points.Count; i++)
            {
                var pointLayer = new PointLayerItemViewModel(info.Points[i], i);
                if (attachEvents)
                {
                    pointLayer.RequestRefresh += (s, e) => RefreshPlotFromLayers();
                    pointLayer.RequestStyleUpdate += (s, e) => WpfPlot1.Refresh();
                }
                pointsCategory.Children.Add(pointLayer);
            }
            if (pointsCategory.Children.Any()) list.Add(pointsCategory);

            // 6. 箭头图层
            var arrowsCategory = new CategoryLayerItemViewModel(LanguageService.Instance["arrow"]);
            for (int i = 0; i < template.Info.Arrows.Count; i++)
            {
                var arrowLayer = new ArrowLayerItemViewModel(template.Info.Arrows[i], i);
                if (attachEvents)
                {
                    arrowLayer.RequestRefresh += (s, e) => RefreshPlotFromLayers();
                    arrowLayer.RequestStyleUpdate += (s, e) => WpfPlot1.Refresh();
                }
                arrowsCategory.Children.Add(arrowLayer);
            }
            if (arrowsCategory.Children.Any()) list.Add(arrowsCategory);

            // 7. 注释图层
            var annotationCategory = new CategoryLayerItemViewModel(LanguageService.Instance["annotation"]);
            for (int i = 0; i < info.Annotations.Count; i++)
            {
                var annotationLayer = new AnnotationLayerItemViewModel(info.Annotations[i], i);
                if (attachEvents)
                {
                    annotationLayer.RequestRefresh += (s, e) => RefreshPlotFromLayers();
                    annotationLayer.RequestStyleUpdate += (s, e) => WpfPlot1.Refresh();
                }
                annotationCategory.Children.Add(annotationLayer);
            }
            if (annotationCategory.Children.Any()) list.Add(annotationCategory);

            // 8. 文本图层 (最顶层)
            var textCategory = new CategoryLayerItemViewModel(LanguageService.Instance["text"]);
            for (int i = 0; i < info.Texts.Count; i++)
            {
                var textLayer = new TextLayerItemViewModel(info.Texts[i], i);
                if (attachEvents)
                {
                    textLayer.RequestRefresh += (s, e) => RefreshPlotFromLayers();
                    textLayer.RequestStyleUpdate += (s, e) => WpfPlot1.Refresh();
                }
                textCategory.Children.Add(textLayer);
            }
            if (textCategory.Children.Any()) list.Add(textCategory);

            return list;
        }

        private void BuildLayerTreeFromTemplate(GraphMapTemplate template)
        {
            LayerTree.Clear();
            var layers = CreateLayersFromTemplate(template, true);
            foreach (var layer in layers)
            {
                LayerTree.Add(layer);
            }
        }

        /// <summary>
        /// 根据当前的 LayerTree 状态，完全重绘 ScottPlot 图表
        /// 负责根据图层对象绘制图像，第一次加载对象
        /// </summary>
        public void RefreshPlotFromLayers(bool preserveAxisLimits = false)
        {
            if (WpfPlot1 == null || CurrentTemplate == null) return;

            // 选择性保留视图，规避自动适应缩放视角
            ScottPlot.AxisLimits? currentAxisLimits = null;
            if (preserveAxisLimits)
            {
                currentAxisLimits = WpfPlot1.Plot.Axes.GetLimits();
            }

            WpfPlot1.Plot.Clear();

            // 重新添加吸附标记
            if (_snapMarker != null)
            {
                // 确保它是不可见的，直到被触发
                _snapMarker.IsVisible = false;
                WpfPlot1.Plot.Add.Plottable(_snapMarker);
            }

            // 如果处于吸附/绘图模式，重新添加潜在吸附点标记
            if (IsPickingPointMode || IsAddingLine || IsAddingArrow || IsAddingPolygon)
            {
                // 清空旧列表
                _potentialSnapMarkers.Clear();
                // 重新调用 UpdatePotentialSnapPoints(true) 来生成并添加
                UpdatePotentialSnapPoints(true);
            }

            BaseMapType = CurrentTemplate.TemplateType;

            Clockwise = CurrentTemplate.Clockwise;

            // 根据模板类型选择渲染路径
            if (CurrentTemplate.TemplateType == "Ternary")
            {
                RenderTernaryPlot();
            }
            else // 默认处理笛卡尔坐标系
            {
                RenderCartesianPlot();
            }

            // 如果是三元图，直接修改属性，关闭十字轴
            if (BaseMapType == "Ternary")
            {
                IsCrosshairVisible = false;
            }
            else
            {
                // 最后添加坐标定位十字轴，确保在最顶层
                WpfPlot1.Plot.Add.Plottable(CrosshairPlot);
            }

            // 获取脚本对象
            CurrentScript = CurrentTemplate.Script;

            // 只在需要“保留视图”且并非三元图时，才恢复范围
            if (preserveAxisLimits && currentAxisLimits.HasValue && BaseMapType != "Ternary")
            {
                WpfPlot1.Plot.Axes.SetLimits(currentAxisLimits.Value);
            }

            // 重新应用选中状态的高亮/遮罩效果
            ReapplySelectionVisualState();

            WpfPlot1.Refresh();
        }

        /// <summary>
        /// 渲染标准笛卡尔坐标系图表
        /// </summary>
        private void RenderCartesianPlot()
        {
            // 从三元图回来的时候会隐藏上和右侧边框，需要手动显示出来
            WpfPlot1.Plot.Axes.Right.IsVisible = true;
            WpfPlot1.Plot.Axes.Top.IsVisible = true;

            WpfPlot1.Plot.Axes.Title.IsVisible = true;

            var allNodes = FlattenTree(LayerTree);

            // --- 刷新渲染绘图对象 ---
            // 线条，文本，多边形，箭头，数据点，坐标轴
            foreach (var layer in allNodes.OfType<IPlotLayer>().Where(l => ((LayerItemViewModel)l).IsVisible))
            {
                layer.Render(WpfPlot1.Plot);
            }

            // 全局设置——处理图例
            WpfPlot1.Plot.Legend.Alignment = CurrentTemplate.Info.Legend.Alignment;
            WpfPlot1.Plot.Legend.FontName = CurrentTemplate.Info.Legend.Font;
            WpfPlot1.Plot.Legend.Orientation = CurrentTemplate.Info.Legend.Orientation;
            WpfPlot1.Plot.Legend.IsVisible = CurrentTemplate.Info.Legend.IsVisible;

            // 全局设置——处理标题
            if (CurrentTemplate.Info.Title.Label.Translations.Any())
            {
                WpfPlot1.Plot.Axes.Title.Label.Text = CurrentTemplate.Info.Title.Label.Get();
                WpfPlot1.Plot.Axes.Title.Label.FontName = CurrentTemplate.Info.Title.Family;
                WpfPlot1.Plot.Axes.Title.Label.FontSize = CurrentTemplate.Info.Title.Size;
                WpfPlot1.Plot.Axes.Title.Label.ForeColor = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(CurrentTemplate.Info.Title.Color));
                WpfPlot1.Plot.Axes.Title.Label.Bold = CurrentTemplate.Info.Title.IsBold;
                WpfPlot1.Plot.Axes.Title.Label.Italic = CurrentTemplate.Info.Title.IsItalic;
            }

            // 设置网格样式
            if (CurrentTemplate?.Info?.Grid != null)
            {
                var gridDef = CurrentTemplate.Info.Grid;
                var grid = WpfPlot1.Plot.Grid; // 获取 ScottPlot 的默认网格对象

                // 应用主网格线样式
                grid.XAxisStyle.MajorLineStyle.IsVisible = gridDef.MajorGridLineIsVisible;
                grid.YAxisStyle.MajorLineStyle.IsVisible = gridDef.MajorGridLineIsVisible;
                grid.MajorLineColor = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(gridDef.MajorGridLineColor));
                grid.MajorLineWidth = gridDef.MajorGridLineWidth;
                grid.MajorLinePattern = GraphMapTemplateService.GetLinePattern(gridDef.MajorGridLinePattern.ToString());
                grid.XAxisStyle.MajorLineStyle.AntiAlias = gridDef.MajorGridLineAntiAlias;
                grid.YAxisStyle.MajorLineStyle.AntiAlias = gridDef.MajorGridLineAntiAlias;


                // 应用次网格线样式
                grid.XAxisStyle.MinorLineStyle.IsVisible = gridDef.MinorGridLineIsVisible;
                grid.YAxisStyle.MinorLineStyle.IsVisible = gridDef.MinorGridLineIsVisible;
                grid.MinorLineColor = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(gridDef.MinorGridLineColor));
                grid.MinorLineWidth = gridDef.MinorGridLineWidth;
                grid.XAxisStyle.MinorLineStyle.AntiAlias = gridDef.MinorGridLineAntiAlias;
                grid.YAxisStyle.MinorLineStyle.AntiAlias = gridDef.MinorGridLineAntiAlias;

                // 应用交替填充背景
                if (gridDef.GridAlternateFillingIsEnable)
                {
                    grid.XAxisStyle.FillColor1 = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(gridDef.GridFillColor1));
                    grid.YAxisStyle.FillColor1 = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(gridDef.GridFillColor1));
                    grid.XAxisStyle.FillColor2 = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(gridDef.GridFillColor2));
                    grid.YAxisStyle.FillColor2 = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(gridDef.GridFillColor2));
                }
                else
                {
                    // 如果禁用，则设置为透明
                    grid.XAxisStyle.FillColor1 = Colors.Transparent;
                    grid.YAxisStyle.FillColor1 = Colors.Transparent;
                    grid.XAxisStyle.FillColor2 = Colors.Transparent;
                    grid.YAxisStyle.FillColor2 = Colors.Transparent;
                }


                grid.IsVisible = gridDef.MajorGridLineIsVisible || gridDef.MinorGridLineIsVisible;
            }
        }

        /// <summary>
        /// 渲染三元相图
        /// </summary>
        private void RenderTernaryPlot()
        {
            // 添加三角坐标轴到图表，并获取其引用
            _triangularAxis = WpfPlot1.Plot.Add.TriangularAxis(clockwise: CurrentTemplate.Clockwise);

            // 应用模板中的网格和背景样式
            var gridDef = CurrentTemplate.Info.Grid;
            if (gridDef != null)
            {
                // 应用网格线样式
                _triangularAxis.GridLineStyle.IsVisible = gridDef.MajorGridLineIsVisible;
                _triangularAxis.GridLineStyle.Color = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(gridDef.MajorGridLineColor));
                _triangularAxis.GridLineStyle.Width = gridDef.MajorGridLineWidth;
                _triangularAxis.GridLineStyle.Pattern = GraphMapTemplateService.GetLinePattern(gridDef.MajorGridLinePattern.ToString());
                _triangularAxis.GridLineStyle.AntiAlias = gridDef.MajorGridLineAntiAlias;

                // 应用背景填充样式
                if (gridDef.GridAlternateFillingIsEnable)
                {
                    // 使用FillColor1作为其填充色
                    _triangularAxis.FillStyle.Color = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(gridDef.GridFillColor1));
                }
                else
                {
                    _triangularAxis.FillStyle.Color = Colors.Transparent;
                }
            }

            // 全局设置——处理标题
            WpfPlot1.Plot.Axes.Title.IsVisible = true;
            if (CurrentTemplate.Info.Title.Label.Translations.Any())
            {
                WpfPlot1.Plot.Axes.Title.Label.Text = CurrentTemplate.Info.Title.Label.Get();
                WpfPlot1.Plot.Axes.Title.Label.FontName = CurrentTemplate.Info.Title.Family;
                WpfPlot1.Plot.Axes.Title.Label.FontSize = CurrentTemplate.Info.Title.Size;
                WpfPlot1.Plot.Axes.Title.Label.ForeColor = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(CurrentTemplate.Info.Title.Color));
                WpfPlot1.Plot.Axes.Title.Label.Bold = CurrentTemplate.Info.Title.IsBold;
                WpfPlot1.Plot.Axes.Title.Label.Italic = CurrentTemplate.Info.Title.IsItalic;
            }

            // 全局设置——处理图例
            WpfPlot1.Plot.Legend.Alignment = CurrentTemplate.Info.Legend.Alignment;
            WpfPlot1.Plot.Legend.FontName = CurrentTemplate.Info.Legend.Font;
            WpfPlot1.Plot.Legend.Orientation = CurrentTemplate.Info.Legend.Orientation;
            WpfPlot1.Plot.Legend.IsVisible = CurrentTemplate.Info.Legend.IsVisible;

            // 遍历所有图层节点
            var allNodes = FlattenTree(LayerTree);


            // --- 刷新渲染绘图对象 ---
            // 线条，文本，多边形，箭头，数据点
            foreach (var layer in allNodes.OfType<IPlotLayer>().Where(l => ((LayerItemViewModel)l).IsVisible))
            {
                layer.Render(WpfPlot1.Plot);
            }

            // 三角图需要方形坐标轴以避免变形
            WpfPlot1.Plot.Axes.SquareUnits();

            // 自动复位视图
            WpfPlot1.Plot.Axes.AutoScale();
        }

        /// <summary>
        /// 辅助方法，用于清除当前绘图中的所有由数据导入的点。
        /// 这个方法不会清空数据表格，也不会显示确认弹窗。
        /// </summary>
        private void ClearExistingPlottedData()
        {
            // 在图层树中找到名为 "数据点" 的根分类图层
            var dataRootNode = LayerTree.FirstOrDefault(node => node is CategoryLayerItemViewModel vm && vm.Name == LanguageService.Instance["data_point"]);

            // 如果找到了该节点
            if (dataRootNode != null)
            {
                // 从 ScottPlot 图表中移除其下所有子图层对应的 Plottable 对象
                var allDataLayers = FlattenTree(dataRootNode.Children).ToList();
                foreach (var layer in allDataLayers)
                {
                    if (layer.Plottable != null)
                    {
                        WpfPlot1.Plot.Remove(layer.Plottable);
                    }
                }

                // 从图层树的根集合中移除 "数据点" 这个顶级分类节点
                LayerTree.Remove(dataRootNode);

                // 如果属性面板当前显示的是某个被删除的图层，则清空属性面板
                if (_selectedLayer != null && (allDataLayers.Contains(_selectedLayer) || _selectedLayer == dataRootNode))
                {
                    PropertyGridModel = null;
                    _selectedLayer = null;
                }
            }
        }

        /// <summary>
        /// 导入数据，切换到数据选项卡
        /// </summary>
        [RelayCommand]
        private void ImportDataPlot()
        {
            RibbonTabIndex = 1;
            // 取消遮罩选择
            CancelSelected();
        }

        // 用于存储多选的图层集合
        public ObservableCollection<LayerItemViewModel> SelectedLayers { get; } = new();

        /// <summary>
        /// 点击图层对象, 在图上高亮显示, 并在属性面板显示其属性
        /// 支持 Ctrl/Shift 多选
        /// </summary>
        /// <param name="selectedItem">当前选中的图层对象</param>
        [RelayCommand]
        private void SelectLayer(LayerItemViewModel selectedItem)
        {
            // 获取所有可绘制的图层 (叶子节点)
            var allPlottableLayers = FlattenTree(LayerTree)
                                       .Where(l => l.Plottable != null && l.Children.Count == 0)
                                       .ToList();

            // 如果没有选中任何项, 或者选中的是分类文件夹
            if (selectedItem == null || selectedItem.Children.Count > 0)
            {
                CancelSelected();
                // 如果是分类文件夹，清空属性面板
                PropertyGridModel = null;

                if (selectedItem != null)
                {
                    _selectedLayer = selectedItem; // 更新引用
                    _selectedLayer.IsSelected = true;
                }

                WpfPlot1.Refresh();
                return;
            }

            // 获取当前选中项的父级分类
            var newParent = LayerTree.FirstOrDefault(c => c.Children.Contains(selectedItem));

            // --- 处理多选逻辑 ---
            bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            if (isCtrl)
            {
                // 检查跨父类多选
                if (SelectedLayers.Count > 0)
                {
                    var firstSelected = SelectedLayers.First();
                    var firstParent = LayerTree.FirstOrDefault(c => c.Children.Contains(firstSelected));

                    if (newParent != firstParent)
                    {
                        // 如果跨父类，清除旧的选择
                        foreach (var layer in SelectedLayers.ToList())
                        {
                            layer.IsSelected = false;
                            SelectedLayers.Remove(layer);
                        }
                        if (_selectedLayer != null)
                        {
                            _selectedLayer.IsSelected = false;
                        }
                    }
                }

                if (SelectedLayers.Contains(selectedItem))
                {
                    // 反选
                    selectedItem.IsSelected = false;
                    SelectedLayers.Remove(selectedItem);
                    // 如果取消的是主选定项，转移主选定项
                    if (_selectedLayer == selectedItem)
                    {
                        _selectedLayer = SelectedLayers.LastOrDefault();
                    }
                }
                else
                {
                    // 加选
                    SelectedLayers.Add(selectedItem);
                    _selectedLayer = selectedItem;
                }
            }
            else if (isShift && _selectedLayer != null)
            {
                // 范围选择
                var linearList = FlattenTree(LayerTree).ToList();
                int start = linearList.IndexOf(_selectedLayer);
                int end = linearList.IndexOf(selectedItem);

                // 检查 Shift 选择是否跨父类
                var anchorParent = LayerTree.FirstOrDefault(c => c.Children.Contains(_selectedLayer));
                if (anchorParent != newParent)
                {
                    // 跨父类：清除旧的选择（包括锚点），只保留新父类中的选中项
                    foreach (var item in SelectedLayers.ToList())
                    {
                        item.IsSelected = false;
                        SelectedLayers.Remove(item);
                    }
                    if (_selectedLayer != null) _selectedLayer.IsSelected = false;
                }

                if (start != -1 && end != -1)
                {
                    int min = Math.Min(start, end);
                    int max = Math.Max(start, end);

                    // 清除范围外的选择
                    var toRemove = SelectedLayers.Where(l =>
                    {
                        int idx = linearList.IndexOf(l);
                        return idx < min || idx > max;
                    }).ToList();

                    foreach (var item in toRemove)
                    {
                        item.IsSelected = false;
                        SelectedLayers.Remove(item);
                    }

                    for (int i = min; i <= max; i++)
                    {
                        var item = linearList[i];
                        // 只选择叶子节点
                        if (item.Children.Count == 0)
                        {
                            // 限制：只允许选择属于 newParent 的项
                            var itemParent = LayerTree.FirstOrDefault(c => c.Children.Contains(item));

                            if (itemParent == newParent)
                            {
                                if (!SelectedLayers.Contains(item))
                                {
                                    SelectedLayers.Add(item);
                                    item.IsSelected = true;
                                }
                            }
                            else
                            {
                                // 如果范围内的项不属于当前父类，确保其未被选中
                                if (SelectedLayers.Contains(item))
                                {
                                    item.IsSelected = false;
                                    SelectedLayers.Remove(item);
                                }
                            }
                        }
                    }
                    _selectedLayer = selectedItem; // 更新主选中项为当前点击项
                }
            }
            else
            {
                // 如果之前的选中项是分类文件夹，且不在多选列表中，需要手动取消选中
                if (_selectedLayer != null && !SelectedLayers.Contains(_selectedLayer) && _selectedLayer != selectedItem)
                {
                    _selectedLayer.IsSelected = false;
                }

                // 单选：清除其他
                foreach (var layer in SelectedLayers.ToList())
                {
                    if (layer != selectedItem)
                    {
                        layer.IsSelected = false;
                        SelectedLayers.Remove(layer);
                    }
                }
                if (!SelectedLayers.Contains(selectedItem))
                {
                    SelectedLayers.Add(selectedItem);
                }
                _selectedLayer = selectedItem;
            }

            // 强制刷新选中状态（对抗TreeView的原生单选行为）
            foreach (var layer in SelectedLayers)
            {
                if (!layer.IsSelected) layer.IsSelected = true;
            }

            // 如果没有选中项了
            if (_selectedLayer == null)
            {
                CancelSelected();
                return;
            }

            IsShowTemplateInfo = false;

            // 在属性面板中显示主选中图层的属性
            object? objectToInspect = GetLayerDefinition(_selectedLayer);
            PropertyGridModel = objectToInspect;

            // 应用高亮样式：选中的恢复原样，其他的变暗
            foreach (var layer in allPlottableLayers)
            {
                if (layer is IPlotLayer plotLayer)
                {
                    if (SelectedLayers.Contains(layer))
                    {
                        plotLayer.Restore();
                    }
                    else
                    {
                        plotLayer.Dim();
                    }
                }
            }

            WpfPlot1.Refresh();
        }



        private object? GetLayerDefinition(LayerItemViewModel layer)
        {
            return layer switch
            {
                PointLayerItemViewModel pointLayer => pointLayer.PointDefinition,
                LineLayerItemViewModel lineLayer => lineLayer.LineDefinition,
                TextLayerItemViewModel textLayer => textLayer.TextDefinition,
                ArrowLayerItemViewModel arrowLayer => arrowLayer.ArrowDefinition,
                PolygonLayerItemViewModel polygonLayer => polygonLayer.PolygonDefinition,
                AxisLayerItemViewModel axisLayer => axisLayer.AxisDefinition,
                ScatterLayerItemViewModel scatterLayer => scatterLayer.ScatterDefinition,
                FunctionLayerItemViewModel funcLayer => funcLayer.FunctionDefinition,
                _ => nullObject
            };
        }

        /// <summary>
        /// 清空所有数据（包括数据表格和图上的数据点）清除数据
        /// </summary>
        [RelayCommand]
        private async Task ClearAllData()
        {
            // 1. 弹出二次确认对话框
            bool isConfirmed = await MessageHelper.ShowAsyncDialog(
                LanguageService.Instance["confirm_clear_all_data"],
                LanguageService.Instance["Cancel"],
                LanguageService.Instance["Confirm"]);

            if (isConfirmed)
            {
                // 先触发取消选择
                CancelSelected();

                // 2. 清除绘图中的数据点
                ClearExistingPlottedData();

                // 3. 清空数据表格
                var worksheet = _dataGrid.Worksheets[0];
                worksheet.Reset();

                // 4. 重置表格的表头
                PrepareDataGridForInput();

                // 5. 刷新绘图
                WpfPlot1.Refresh();

                // 6. 成功提示
                MessageHelper.Success(LanguageService.Instance["all_data_cleared"]);
            }
        }

        /// <summary>
        /// 更新数据
        /// 先清除旧的数据点，然后根据当前表格重新投点
        /// </summary>
        [RelayCommand]
        private void UpdateData()
        {
            // 先触发取消选择
            CancelSelected();

            // 清除当前绘图中的所有由数据导入的点
            ClearExistingPlottedData();

            // 根据当前数据表格中的数据重新进行投点
            PlotDataFromGrid();
        }

        /// <summary>
        /// 视图复位
        /// </summary>
        [RelayCommand]
        private void CenterPlot()
        {
            // 默认自动缩放
            bool shouldAutoScale = true;

            // 仅针对笛卡尔坐标系处理
            if (CurrentTemplate?.TemplateType == "Cartesian" && CurrentTemplate.Info.Axes != null)
            {
                // 获取X和Y轴定义
                var xAxisDef = CurrentTemplate.Info.Axes.FirstOrDefault(a => a.Type == "Bottom") as CartesianAxisDefinition;
                var yAxisDef = CurrentTemplate.Info.Axes.FirstOrDefault(a => a.Type == "Left") as CartesianAxisDefinition;

                // 检查是否有设定的范围 (最小值不等于最大值)
                bool isXRangeSet = xAxisDef != null && Math.Abs(xAxisDef.Maximum - xAxisDef.Minimum) > 1e-9;
                bool isYRangeSet = yAxisDef != null && Math.Abs(yAxisDef.Maximum - yAxisDef.Minimum) > 1e-9;

                // 准备比较用的阈值变量
                double xMin = xAxisDef?.Minimum ?? 0;
                double xMax = xAxisDef?.Maximum ?? 0;
                double yMin = yAxisDef?.Minimum ?? 0;
                double yMax = yAxisDef?.Maximum ?? 0;

                // 处理 Log 坐标转换：如果坐标轴是对数类型，需要将用户设定的线性值转换为对数值进行比较和设置
                if (isXRangeSet && xAxisDef.ScaleType == AxisScaleType.Logarithmic)
                {
                    if (xMin > 0 && xMax > 0)
                    {
                        xMin = Math.Log10(xMin);
                        xMax = Math.Log10(xMax);
                    }
                    else
                    {
                        // 非法的 Log 范围 (<=0)，视为未设定，回退到自动缩放
                        isXRangeSet = false;
                    }
                }

                if (isYRangeSet && yAxisDef.ScaleType == AxisScaleType.Logarithmic)
                {
                    if (yMin > 0 && yMax > 0)
                    {
                        yMin = Math.Log10(yMin);
                        yMax = Math.Log10(yMax);
                    }
                    else
                    {
                        isYRangeSet = false;
                    }
                }

                // 如果设定了范围
                if (isXRangeSet || isYRangeSet)
                {
                    // 获取当前数据的边界
                    double dataXMin = double.MaxValue;
                    double dataXMax = double.MinValue;
                    double dataYMin = double.MaxValue;
                    double dataYMax = double.MinValue;
                    bool hasData = false;

                    // 获取所有可见的 Plottables
                    var plottables = WpfPlot1.Plot.GetPlottables().Where(p => p.IsVisible);

                    foreach (var plottable in plottables)
                    {
                        // 忽略一些辅助性的 plottable
                        if (ReferenceEquals(plottable, CrosshairPlot)) continue;
                        if (ReferenceEquals(plottable, _snapMarker)) continue;
                        if (_potentialSnapMarkers != null && _potentialSnapMarkers.Any(m => ReferenceEquals(m, plottable))) continue;

                        // 忽略 Grid 和 Axis
                        string typeName = plottable.GetType().Name;
                        if (typeName.Contains("Grid") || typeName.Contains("Axis")) continue;

                        var limits = plottable.GetAxisLimits();
                        var rect = limits.Rect;

                        // 检查是否有有效范围
                        if (rect.Left <= rect.Right && rect.Bottom <= rect.Top)
                        {
                            // 只有非默认/非无穷大的值才有效
                            if (rect.Left > double.MinValue && rect.Right < double.MaxValue &&
                                rect.Bottom > double.MinValue && rect.Top < double.MaxValue)
                            {
                                if (rect.Left < dataXMin) dataXMin = rect.Left;
                                if (rect.Right > dataXMax) dataXMax = rect.Right;
                                if (rect.Bottom < dataYMin) dataYMin = rect.Bottom;
                                if (rect.Top > dataYMax) dataYMax = rect.Top;
                                hasData = true;
                            }
                        }
                    }

                    // 检查是否超出范围
                    bool outOfRange = false;

                    if (hasData)
                    {
                        // 稍微增加一点容差
                        double tolerance = 1e-9;

                        if (isXRangeSet)
                        {
                            if (dataXMin < xMin - tolerance || dataXMax > xMax + tolerance)
                            {
                                outOfRange = true;
                            }
                        }

                        if (isYRangeSet && !outOfRange)
                        {
                            if (dataYMin < yMin - tolerance || dataYMax > yMax + tolerance)
                            {
                                outOfRange = true;
                            }
                        }
                    }

                    // 如果有设定坐标轴范围且没有绘图对象超出了坐标轴范围，就设定缩放为当前的设定坐标轴范围
                    if (!outOfRange)
                    {
                        shouldAutoScale = false;

                        if (isXRangeSet)
                        {
                            WpfPlot1.Plot.Axes.SetLimitsX(xMin, xMax);
                        }
                        else
                        {
                            WpfPlot1.Plot.Axes.AutoScaleX();
                        }

                        if (isYRangeSet)
                        {
                            WpfPlot1.Plot.Axes.SetLimitsY(yMin, yMax);
                        }
                        else
                        {
                            WpfPlot1.Plot.Axes.AutoScaleY();
                        }
                    }
                }
            }

            if (shouldAutoScale)
            {
                WpfPlot1.Plot.Axes.AutoScale();
            }

            WpfPlot1.Refresh();
        }

        /// <summary>
        /// 取消选择
        /// </summary>
        [RelayCommand]
        private void CancelSelected()
        {
            ResetAddingModes();
            ClearLayerSelection();
        }

        private void ResetAddingModes()
        {
            IsAddingLine = false;
            IsAddingArrow = false;
            IsAddingPolygon = false;
            IsAddingText = false;
        }

        private void ClearLayerSelection()
        {
            // 获取所有可绘制的图层
            var allPlottableLayers = FlattenTree(LayerTree)
                                       .Where(l => l.Plottable != null && l.Children.Count == 0)
                                       .ToList();

            // 恢复所有图层的原始样式
            foreach (var layer in allPlottableLayers)
            {
                if (layer is IPlotLayer plotLayer)
                {
                    plotLayer.Restore();
                }
            }

            WpfPlot1.Refresh();
            // 清楚图层列表选中状态
            if (_selectedLayer != null)
            {
                _selectedLayer.IsSelected = false;
                _selectedLayer = null;
            }

            // 清除多选状态
            foreach (var layer in SelectedLayers)
            {
                layer.IsSelected = false;
            }
            SelectedLayers.Clear();

            // 解绑属性变更事件
            if (PropertyGridModel is INotifyPropertyChanged prop)
            {
                prop.PropertyChanged -= OnPropertyGridModelChanged;
            }

            PropertyGridModel = null;   // 取消属性编辑器
            ScriptsPropertyGrid = false;

            IsShowTemplateInfo = false;     // 取消绘图模板指南显示

            // 清除数据点高亮标记
            if (_selectedDataPointMarker != null && _selectedDataPointMarker.IsVisible)
            {
                _selectedDataPointMarker.IsVisible = false;
                WpfPlot1.Refresh();
            }
        }

        /// <summary>
        /// 切换十字定位轴的显示/隐藏状态
        /// </summary>
        [RelayCommand]
        private void LocationAxis()
        {
            // 三元图状态下用户手动开启提示
            if (BaseMapType == "Ternary")
            {
                IsCrosshairVisible = false;
                // 三元相图暂不支持定位功能
                MessageHelper.Warning(LanguageService.Instance["ternary_diagram_crosshair_not_supported"]);
                return;
            }
        }

        /// <summary>
        /// 图例设置
        /// </summary>
        [RelayCommand]
        private void LegendSetting()
        {
            if (_selectedLayer != null)
            {
                CancelSelected();
            }
            PropertyGridModel = CurrentTemplate.Info.Legend;
        }

        /// <summary>
        /// 网格设置
        /// </summary>
        [RelayCommand]
        private void GridSetting()
        {
            if (_selectedLayer != null)
            {
                CancelSelected();
            }
            PropertyGridModel = CurrentTemplate.Info.Grid;
        }

        /// <summary>
        /// 脚本设置
        /// </summary>
        [RelayCommand]
        private void ScriptSetting()
        {
            if (_selectedLayer != null)
            {
                CancelSelected();
            }
            // 清空属性面板
            PropertyGridModel = null;
            ScriptsPropertyGrid = !ScriptsPropertyGrid;
        }

        /// <summary>
        /// 绘图设置——标题属性
        /// </summary>
        [RelayCommand]
        private void PlotSetting()
        {
            if (_selectedLayer != null)
            {
                CancelSelected();
            }
            PropertyGridModel = CurrentTemplate.Info.Title;
        }

        /// <summary>
        /// 显示导出/外部工具面板
        /// </summary>
        [RelayCommand]
        private void ShowExportPanel()
        {
            PropertyGridModel = new ExportPanelViewModel(this);
        }

        // 导出图片
        [RelayCommand]
        public void ExportImg(string fileType)
        {

            string tempFileName = "OutPut_fig." + fileType;

            // 读取默认文件保存位置
            string temp = FileHelper.GetSaveFilePath(tempFileName);
            if (temp == string.Empty) { return; }

            int tempWidth = (int)WpfPlot1.Plot.LastRender.DataRect.Width;
            int tempHeight = (int)WpfPlot1.Plot.LastRender.DataRect.Height;
            WpfPlot1.Plot.Save(temp, (int)(tempWidth * 1.25), (int)(tempHeight * 1.25));
        }

        // 第三方应用列表
        public ObservableCollection<string> ThirdPartyApps { get; } = new() { "CorelDRAW", "Inkscape", "Adobe Illustrator", "Custom" };

        [ObservableProperty]
        private string _selectedThirdPartyApp = "CorelDRAW";

        /// <summary>
        /// 通过第三方应用打开
        /// </summary>
        [RelayCommand]
        private void OpenWithThirdParty()
        {
            if (WpfPlot1 == null) return;

            // 正在尝试启动
            MessageHelper.Info(LanguageService.Instance["attempting_to_start"] +
                $" {SelectedThirdPartyApp}...");

            int width = (int)WpfPlot1.Plot.LastRender.DataRect.Width;
            int height = (int)WpfPlot1.Plot.LastRender.DataRect.Height;
            // 提高一点分辨率
            int exportWidth = (int)(width * 1.5);
            int exportHeight = (int)(height * 1.5);

            if (SelectedThirdPartyApp == "CorelDRAW")
            {
                try
                {
                    // CorelDRAW: 存为 SVG 并打开
                    string tempPath = Path.Combine(Path.GetTempPath(), $"Plot_{DateTime.Now:Ticks}.svg");
                    WpfPlot1.Plot.Save(tempPath, exportWidth, exportHeight);

                    // 尝试通过 CorelDRAW 打开
                    try
                    {
                        string appPath = ConfigHelper.GetConfig("coreldraw_path");
                        if (!string.IsNullOrEmpty(appPath) && File.Exists(appPath))
                        {
                            Process.Start(appPath, $"\"{tempPath}\"");
                        }
                        else
                        {
                            Process.Start("CorelDRW", $"\"{tempPath}\"");
                        }
                    }
                    catch
                    {
                        // 如果未找到 CorelDRAW 命令，让用户选择打开方式
                        try
                        {
                            Process.Start("rundll32.exe", $"shell32.dll,OpenAs_RunDLL {tempPath}");
                        }
                        catch
                        {
                            // 备用方案：尝试默认打开
                            Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                        }
                        // 未找到 CorelDRAW 环境变量，请选择打开程序
                        MessageHelper.Warning(LanguageService.Instance["coreldraw_env_not_found_select_program"]);
                    }
                }
                catch (Exception ex)
                {
                    // 打开失败： + ex
                    MessageHelper.Error(LanguageService.Instance["failed_to_open"] + $" {ex.Message}");
                }
            }
            else if (SelectedThirdPartyApp == "Inkscape")
            {
                try
                {
                    // Inkscape: 存为 SVG 并打开
                    string tempPath = Path.Combine(Path.GetTempPath(), $"Plot_{DateTime.Now:Ticks}.svg");
                    WpfPlot1.Plot.Save(tempPath, exportWidth, exportHeight);

                    // 尝试通过 Inkscape 打开
                    try
                    {
                        string appPath = ConfigHelper.GetConfig("inkscape_path");
                        if (!string.IsNullOrEmpty(appPath) && File.Exists(appPath))
                        {
                            Process.Start(appPath, $"\"{tempPath}\"");
                        }
                        else
                        {
                            Process.Start("inkscape", $"\"{tempPath}\"");
                        }
                    }
                    catch
                    {
                        // 如果未找到 inkscape 命令，让用户选择打开方式
                        try
                        {
                            Process.Start("rundll32.exe", $"shell32.dll,OpenAs_RunDLL {tempPath}");
                        }
                        catch
                        {
                            // 备用方案：尝试默认打开
                            Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                        }
                        // 未找到 Inkscape 环境变量，请选择打开程序
                        MessageHelper.Warning(LanguageService.Instance["inkscape_env_not_found_select_program"]);
                    }
                }
                catch (Exception ex)
                {
                    // 打开失败 + ex
                    MessageHelper.Error(LanguageService.Instance["failed_to_open"] + $" {ex.Message}");
                }
            }
            else if (SelectedThirdPartyApp == "Adobe Illustrator")
            {
                try
                {
                    // Adobe Illustrator: 存为 SVG 并打开
                    string tempPath = Path.Combine(Path.GetTempPath(), $"Plot_{DateTime.Now:Ticks}.svg");
                    WpfPlot1.Plot.Save(tempPath, exportWidth, exportHeight);

                    // 尝试通过 Adobe Illustrator 打开
                    try
                    {
                        string appPath = ConfigHelper.GetConfig("adobe_illustrator_path");
                        if (!string.IsNullOrEmpty(appPath) && File.Exists(appPath))
                        {
                            Process.Start(appPath, $"\"{tempPath}\"");
                        }
                        else
                        {
                            Process.Start("illustrator", $"\"{tempPath}\"");
                        }
                    }
                    catch
                    {
                        // 如果未找到命令，让用户选择打开方式
                        try
                        {
                            Process.Start("rundll32.exe", $"shell32.dll,OpenAs_RunDLL {tempPath}");
                        }
                        catch
                        {
                            // 备用方案：尝试默认打开
                            Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                        }
                        // 未找到 Adobe Illustrator，请检查路径设置或选择打开程序
                        MessageHelper.Warning(LanguageService.Instance["illustrator_not_found_check_path"]);
                    }
                }
                catch (Exception ex)
                {
                    MessageHelper.Error(LanguageService.Instance["failed_to_open"] + $" {ex.Message}");
                }
            }
            else if (SelectedThirdPartyApp == "Custom")
            {
                try
                {
                    // Custom: 存为 SVG 并打开
                    string tempPath = Path.Combine(Path.GetTempPath(), $"Plot_{DateTime.Now:Ticks}.svg");
                    WpfPlot1.Plot.Save(tempPath, exportWidth, exportHeight);

                    // 尝试通过 Custom 打开
                    try
                    {
                        string appPath = ConfigHelper.GetConfig("custom_third_party_app_path");
                        if (!string.IsNullOrEmpty(appPath) && File.Exists(appPath))
                        {
                            Process.Start(appPath, $"\"{tempPath}\"");
                        }
                        else
                        {
                            // 未设置自定义程序路径，请在设置中配置
                            MessageHelper.Warning(LanguageService.Instance["custom_path_not_set_configure_in_settings"]);
                            // 备用方案：尝试默认打开
                            Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                        }
                    }
                    catch (Exception ex)
                    {
                        // 启动自定义程序失败：
                        MessageHelper.Error(LanguageService.Instance["custom_program_start_failed"] +
                            ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    MessageHelper.Error(LanguageService.Instance["failed_to_open"] + $" {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 加载 RTF 帮助文档 (重构版：优先从 LiteDB 读取)
        /// </summary>
        private void ReloadHelpDocument(string languageCode)
        {
            if (_richTextBox == null) return;
            _richTextBox.Document.Blocks.Clear();

            // 1. 优先尝试从数据库加载 (CurrentTemplate 对应的 Entity)
            string rtfContent = null;

            try
            {
                // 使用在 SelectTemplateCard 时记录的 _currentTemplateId
                Guid? templateId = _currentTemplateId;

                if (templateId.HasValue)
                {
                    var entity = GraphMapDatabaseService.Instance.GetTemplate(templateId.Value);
                    if (entity != null && entity.HelpDocuments != null)
                    {
                        // 1.1 尝试精确匹配语言代码 (如 "zh-CN")
                        if (entity.HelpDocuments.ContainsKey(languageCode))
                        {
                            rtfContent = entity.HelpDocuments[languageCode];
                        }
                        // 1.2 尝试匹配前缀 (如 "zh" 匹配 "zh-CN")
                        else
                        {
                            var key = entity.HelpDocuments.Keys.FirstOrDefault(k => k.StartsWith(languageCode.Split('-')[0], StringComparison.OrdinalIgnoreCase));
                            if (key != null)
                            {
                                rtfContent = entity.HelpDocuments[key];
                            }
                            // 1.3 默认回退到第一个
                            else if (entity.HelpDocuments.Count > 0)
                            {
                                rtfContent = entity.HelpDocuments.Values.First();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load RTF from DB: {ex.Message}");
            }

            // 2. 如果数据库有内容，直接加载内容流
            if (!string.IsNullOrEmpty(rtfContent))
            {
                try
                {
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(rtfContent)))
                    {
                        var range = new TextRange(_richTextBox.Document.ContentStart, _richTextBox.Document.ContentEnd);
                        range.Load(stream, DataFormats.Rtf);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to render RTF content: {ex.Message}");
                }
            }
            else
            {
                // 如果数据库中没有找到内容，尝试加载默认英文
                if (languageCode != "en-US")
                {
                    ReloadHelpDocument("en-US");
                }
            }
        }

        /// <summary>
        /// 刷新帮助文档
        /// </summary>
        [RelayCommand]
        private void RefreshHelpDoc()
        {
            if (!string.IsNullOrEmpty(CurrentDiagramLanguage))
            {
                ReloadHelpDocument(CurrentDiagramLanguage);
            }
            else
            {
                ReloadHelpDocument("en-US");
            }
        }

        /// <summary>
        /// 使用 Word 打开说明文档
        /// </summary>
        [RelayCommand]
        private void OpenHelpDocInWord()
        {
            if (_richTextBox == null) return;

            string rtfPath = _currentRtfFilePath;

            // 如果没有记录加载的 RTF 路径，尝试根据当前模板路径推断
            if (string.IsNullOrEmpty(rtfPath) && !string.IsNullOrEmpty(_currentTemplateFilePath))
            {
                string directory = Path.GetDirectoryName(_currentTemplateFilePath);
                // 优先尝试当前语言
                string filename = CurrentDiagramLanguage + ".rtf";
                rtfPath = Path.Combine(directory, filename);
            }

            if (string.IsNullOrEmpty(rtfPath))
            {
                // 无法确定图解帮助文件路径，请尝试先保存模板。
                MessageHelper.Error(LanguageService.Instance["diagram_help_path_undefined_save_template"]);
                return;
            }

            // 保存当前 RichTextBox 内容到 RTF 文件
            bool saved = RtfHelper.SaveRichTextBoxToRtf(_richTextBox, rtfPath);
            if (!saved) return;

            // 使用默认关联程序（ Word）打开
            try
            {
                Process.Start(new ProcessStartInfo(rtfPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                // 无法打开文件
                MessageHelper.Error(LanguageService.Instance["failed_to_open_file"] +
                    ex.Message);
            }
        }

        /// <summary>
        /// 将当前图像复制到系统剪贴板
        /// </summary>
        [RelayCommand]
        private void CopyToClipboard()
        {
            if (WpfPlot1 == null)
            {
                // 如果绘图控件为空，则不执行任何操作
                return;
            }

            try
            {
                int tempWidth = (int)WpfPlot1.Plot.LastRender.DataRect.Width;
                int tempHeight = (int)WpfPlot1.Plot.LastRender.DataRect.Height;
                // 从 ScottPlot 控件获取图像的字节数据
                byte[] imageBytes = WpfPlot1.Plot.GetImageBytes((int)(tempWidth * 1.25), (int)(tempHeight * 1.25));

                // 将字节数组转换为 WPF 的 BitmapSource
                var bitmapImage = new BitmapImage();
                using (var stream = new MemoryStream(imageBytes))
                {
                    stream.Position = 0; // 重置流的位置
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                }

                // 冻结图像，使其可以安全地被其他线程访问
                bitmapImage.Freeze();

                // 将图像放置到剪贴板
                Clipboard.SetImage(bitmapImage);

                // 前端通知
                MessageHelper.Success(LanguageService.Instance["image_copied_to_clipboard"]);
            }
            catch (Exception ex)
            {

                MessageHelper.Error(LanguageService.Instance["copy_failed"] + ex.Message);
            }
        }



        /// <summary>
        /// 尝试新建图解模板
        /// </summary>
        /// <returns>是否创建成功</returns>
        private async Task<bool> TryCreateNewTemplate(NewTemplateControl newTemplateControl)
        {
            if (newTemplateControl == null) return false;

            // 获取数据
            string language = newTemplateControl.SelectedLanguages;
            string category = newTemplateControl.CategoryHierarchy;
            string plotType = newTemplateControl.PlotType;

            // 检查数据是否有效
            if (string.IsNullOrWhiteSpace(language) || string.IsNullOrWhiteSpace(category)
                 || string.IsNullOrEmpty(plotType))
            {
                // 所有字段均为必填项！
                MessageHelper.Warning(LanguageService.Instance["all_fields_required"]);
                return false;
            }

            var allLanguages = language.Split(new[] { " > " }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(lang => lang.Trim())
                                .ToList();

            // 检查是否有重复语言key
            if (allLanguages.Distinct().Count() != allLanguages.Count)
            {
                // 语言设置中存在重复项，请检查！
                MessageHelper.Warning(LanguageService.Instance["language_setting_duplicate_found"]);
                return false;
            }

            // 获取富文本形式的分类层级数据
            var categoryParts = newTemplateControl.GetCategoryParts().ToList();

            if (categoryParts.Count < 2)
            {
                // 分类结构必须大于等于2！
                MessageHelper.Warning(LanguageService.Instance["category_structure_min_two"]);
                return false;
            }

            // 1. 确定默认语言
            string defaultLanguage = allLanguages.First();

            // 2. 为所有支持的语言构建分类路径字符串
            var categoryTranslations = new Dictionary<string, string>();

            foreach (var lang in allLanguages)
            {
                var pathParts = new List<string>();

                foreach (var part in categoryParts)
                {
                    string partName = string.Empty;

                    // 尝试获取本地化名称
                    if (part.LocalizedNames != null && part.LocalizedNames.ContainsKey(lang))
                    {
                        partName = part.LocalizedNames[lang];
                    }
                    else
                    {
                        // 如果没有对应的本地化名称（或者是手动输入的），则使用默认显示名称
                        partName = part.DisplayName;
                    }

                    pathParts.Add(partName);
                }

                // 组合成路径字符串
                categoryTranslations[lang] = string.Join(" > ", pathParts);
            }

            // 3. 构建 NodeList 对象
            var localizedCategory = new LocalizedString
            {
                Default = defaultLanguage,
                Translations = categoryTranslations
            };

            // 使用最后两级作为文件名的一部分（使用默认语言或显示名称）
            string lastPart = categoryParts[categoryParts.Count - 1].DisplayName;
            string secondLastPart = categoryParts[categoryParts.Count - 2].DisplayName;
            string folderName = $"{secondLastPart}_{lastPart}";

            // 1. 检查数据库中是否存在同名模板
            var existingId = GraphMapDatabaseService.GenerateId(folderName, true);
            var existingTemplate = await Task.Run(() => GraphMapDatabaseService.Instance.GetTemplate(existingId));

            if (existingTemplate != null)
            {
                var result = HandyControl.Controls.MessageBox.Show(
                    LanguageService.Instance["BasemapExisted"],
                    LanguageService.Instance["tips"] ?? "Tips",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    return false;
                }
            }

            // 确保清除语言覆盖
            LocalizedString.OverrideLanguage = null;

            // 2. 准备新的 GraphMapTemplate 对象
            CurrentTemplate = GraphMapTemplate.CreateDefault(allLanguages, plotType, localizedCategory);

            // 计算哈希
            string fileHash = string.Empty;
            try
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    var jsonBytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(CurrentTemplate));
                    var hashBytes = md5.ComputeHash(jsonBytes);
                    fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to calculate hash: {ex.Message}");
            }

            // 3. 准备 Entity
            var newEntity = new GraphMapTemplateEntity
            {
                Id = existingId, // 使用确定性 ID
                GraphMapPath = folderName,
                Name = folderName,
                IsCustom = true,
                LastModified = DateTime.Now,
                NodeList = localizedCategory,
                TemplateType = CurrentTemplate.TemplateType,
                Version = CurrentTemplate.Version,
                Content = CurrentTemplate,
                FileHash = fileHash,
                HelpDocuments = new Dictionary<string, string>()
            };

            // 4. 加载并填充初始 RTF 帮助文档
            // 根据新建模板所支持的语言，复制 template.rtf 文档内容到 HelpDocuments 字段
            string sourceRtfPath = Path.Combine(FileHelper.GetAppPath(), "Data", "Documents", "template.rtf");

            // 开发环境兼容：如果运行目录下没有，尝试查找源码目录 (假设在 bin/Debug/net6.0-windows 下运行)
            if (!File.Exists(sourceRtfPath))
            {
                try
                {
                    string devPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Data", "Documents", "template.rtf"));
                    if (File.Exists(devPath))
                    {
                        sourceRtfPath = devPath;
                    }
                }
                catch
                {
                    // 忽略路径错误
                }
            }

            if (File.Exists(sourceRtfPath))
            {
                try
                {
                    string defaultRtfContent = File.ReadAllText(sourceRtfPath);
                    foreach (var lang in allLanguages)
                    {
                        newEntity.HelpDocuments[lang] = defaultRtfContent;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load template.rtf: {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Warning: template.rtf not found. Expected at: {sourceRtfPath}");
            }

            // 5. 保存到数据库
            await Task.Run(() => GraphMapDatabaseService.Instance.UpsertTemplate(newEntity));

            // 更新当前状态
            _currentTemplateId = newEntity.Id;
            _currentTemplateFilePath = null; // 不再使用文件路径
            _isCurrentTemplateCustom = true;
            UpdateHelpDocReadOnlyState();

            IsTemplateMode = false;
            IsPlotMode = true;


            // 设置 OverrideLanguage。
            LocalizedString.OverrideLanguage = CurrentTemplate.DefaultLanguage;

            BuildLayerTreeFromTemplate(CurrentTemplate);
            RefreshPlotFromLayers();

            // 生成并保存缩略图
            try
            {
                // 生成缩略图 (640*480)
                byte[] thumbnailBytes = WpfPlot1.Plot.GetImageBytes(640, 480);

                using (var stream = new MemoryStream(thumbnailBytes))
                {
                    await Task.Run(() => GraphMapDatabaseService.Instance.UploadThumbnail(newEntity.Id, stream));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to generate thumbnail: {ex.Message}");
            }

            // 刷新模板列表缓存，确保返回列表时能看到新模板
            await InitializeAsync();

            // 加载说明文档 (从数据库)
            ReloadHelpDocument(CurrentDiagramLanguage);

            return true;
        }

        /// <summary>
        /// 新建底图——弹窗新建
        /// </summary>
        [RelayCommand]
        private void NewTemplate()
        {
            try
            {
                var window = new NewTemplateWindow();

                var confirmCommand = new AsyncRelayCommand<NewTemplateControl>(async (control) =>
                {
                    if (await TryCreateNewTemplate(control))
                    {
                        window.Close();
                    }
                });

                var cancelCommand = new RelayCommand<NewTemplateControl>((control) =>
                {
                    window.Close();
                });

                window.ConfirmCommand = confirmCommand;
                window.CancelCommand = cancelCommand;

                // 尝试设置 Owner，避免因 MainWindow 未显示而崩溃
                try
                {
                    if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
                    {
                        window.Owner = Application.Current.MainWindow;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"设置 Owner 失败: {ex.Message}");
                }

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                // 新建模板时发生错误:
                MessageHelper.Error(LanguageService.Instance["error_creating_new_template"] +
                    ex.Message);
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// 将当前底图模板另存为新模板 (保存到数据库)
        /// </summary>
        [RelayCommand]
        private async Task SaveBaseMapAs()
        {
            // 取消选中状态
            CancelSelected();

            // 确保有模板可以保存
            if (CurrentTemplate == null)
            {
                MessageHelper.Error(LanguageService.Instance["no_template_or_path_specified"]);
                return;
            }

            // 配置并显示文件保存对话框 (仅用于获取新名称)
            var saveFileDialog = new VistaSaveFileDialog
            {
                Title = LanguageService.Instance["save_template_as"],
                Filter = $"{LanguageService.Instance["template_files"]} (*.json)|*.json",
                DefaultExt = ".json",
                FileName = "NewTemplate.json"
            };

            // 如果用户选择了路径并点击了 "保存"
            if (saveFileDialog.ShowDialog() == true)
            {
                string newName = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);

                // 1. 生成新 ID (强制为自定义)
                var newId = GraphMapDatabaseService.GenerateId(newName, true);

                // 2. 检查是否重复
                var existing = await Task.Run(() => GraphMapDatabaseService.Instance.GetTemplate(newId));
                if (existing != null)
                {
                    // Ask overwrite
                    bool overwrite = await MessageHelper.ShowAsyncDialog(
                        LanguageService.Instance["BasemapExisted"],
                        LanguageService.Instance["Cancel"],
                        LanguageService.Instance["Confirm"]);

                    if (!overwrite) return;
                }

                // 3. 准备新 Entity
                // 获取当前 Entity 以复制帮助文档
                var currentEntity = await Task.Run(() => GraphMapDatabaseService.Instance.GetTemplate(_currentTemplateId.GetValueOrDefault()));

                var newEntity = new GraphMapTemplateEntity
                {
                    Id = newId,
                    Name = newName,
                    GraphMapPath = newName,
                    IsCustom = true,
                    LastModified = DateTime.Now,
                    TemplateType = CurrentTemplate.TemplateType,
                    Version = CurrentTemplate.Version,
                    Content = CurrentTemplate,
                    NodeList = CurrentTemplate.NodeList,
                    HelpDocuments = currentEntity?.HelpDocuments != null
                                    ? new Dictionary<string, string>(currentEntity.HelpDocuments)
                                    : new Dictionary<string, string>()
                };

                // 4. 保存到 DB
                await Task.Run(() => GraphMapDatabaseService.Instance.UpsertTemplate(newEntity));

                // 5. 切换上下文
                _currentTemplateId = newId;
                _isCurrentTemplateCustom = true;
                _currentTemplateFilePath = null;

                // 6. 执行完整保存 (更新内容、缩略图、当前文档)
                await PerformSave();

                // 7. 刷新列表
                await InitializeAsync();
            }
        }

        /// <summary>
        /// 保存当前底图模板
        /// </summary>
        [RelayCommand]
        private async Task SaveBaseMap()
        {
            // 取消选中状态
            CancelSelected();

            // 确保有模板可供保存
            if (CurrentTemplate == null)
            {
                MessageHelper.Error(LanguageService.Instance["no_template_or_path_specified"]);
                return;
            }

            // 直接保存到数据库
            await PerformSave();
        }

        private class GraphMapListItem
        {
            public LocalizedString NodeList { get; set; }
            public string GraphMapPath { get; set; }
            public string FileHash { get; set; }
        }

        /// <summary>
        /// 导入自定义图解模板
        /// </summary>
        [RelayCommand]
        private async Task ImportCustomTemplate()
        {
            var openFileDialog = new VistaOpenFileDialog
            {
                // 导入自定义图解模板
                Title = LanguageService.Instance["import_custom_diagram_template"],
                Filter = "Template Files (*.zip;*.json)|*.zip;*.json|Zip Files (*.zip)|*.zip|Json Files (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = ".zip",
                CheckFileExists = true,
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != true) return;

            // 检查是否包含 JSON 文件且为多选
            if (openFileDialog.FileNames.Length > 1 && openFileDialog.FileNames.Any(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
            {
                MessageHelper.Warning("json文件不支持多选批量添加。只支持zip文件多选批量添加。");
                return;
            }

            // 标记是否进行了导入操作
            bool imported = false;

            foreach (var selectedPath in openFileDialog.FileNames)
            {
                string ext = Path.GetExtension(selectedPath).ToLower();

                try
                {
                    if (ext == ".zip")
                    {
                        await ImportZipTemplate(selectedPath);
                        imported = true;
                    }
                    else if (ext == ".json")
                    {
                        await ImportJsonTemplate(selectedPath);
                        imported = true;
                    }
                }
                catch (Exception ex)
                {
                    // 导入失败 + ex
                    MessageHelper.Error($"{Path.GetFileName(selectedPath)}: {LanguageService.Instance["import_failed"]} {ex.Message}");
                }
            }

            if (imported)
            {
                // 刷新模板列表
                await InitializeAsync();
            }
        }

        private async Task ImportZipTemplate(string zipPath)
        {
            // 1. 验证 ZIP 包
            string validTemplateName = null;
            GraphMapTemplate template = null;
            string errorMessage = null;

            await Task.Run(() =>
            {
                try
                {
                    using (var archive = ZipFile.OpenRead(zipPath))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            if (entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    using (var stream = entry.Open())
                                    using (var reader = new StreamReader(stream))
                                    {
                                        string jsonContent = reader.ReadToEnd();
                                        var options = new JsonSerializerOptions
                                        {
                                            PropertyNameCaseInsensitive = true,
                                            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                                        };
                                        var tempTemplate = JsonSerializer.Deserialize<GraphMapTemplate>(jsonContent, options);

                                        // 简单验证：检查关键属性是否不为空
                                        if (tempTemplate != null && tempTemplate.Info != null)
                                        {
                                            // 版本校验
                                            if (!GraphMapTemplateService.IsVersionCompatible(tempTemplate))
                                            {
                                                errorMessage = LanguageService.Instance["template_version_too_high"];
                                                return;
                                            }

                                            template = tempTemplate;
                                            // 使用 JSON 文件名作为模板名
                                            validTemplateName = Path.GetFileNameWithoutExtension(entry.Name);
                                            break;
                                        }
                                    }
                                }
                                catch
                                {
                                    // 忽略解析失败的文件
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // ZIP 打开失败
                    errorMessage = LanguageService.Instance["failed_to_open_zip_file"];
                }
            });

            if (errorMessage != null)
            {
                MessageHelper.Error(errorMessage);
                return;
            }

            if (string.IsNullOrEmpty(validTemplateName) || template == null)
            {
                // 模板文件已损坏或未包含有效的模板 JSON 文件。
                MessageHelper.Error(LanguageService.Instance["diagram_template_corrupted_or_invalid"]);
                return;
            }

            await FinalizeImport(validTemplateName, template, async (customDir) =>
            {
                await Task.Run(() =>
                {
                    ZipFile.ExtractToDirectory(zipPath, customDir);
                });
            });
        }

        private async Task ImportJsonTemplate(string jsonPath)
        {
            string jsonContent = await File.ReadAllTextAsync(jsonPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            GraphMapTemplate template = null;
            try
            {
                template = JsonSerializer.Deserialize<GraphMapTemplate>(jsonContent, options);
            }
            catch
            {
                // 无法解析图解模板 JSON 文件
                MessageHelper.Error(LanguageService.Instance["failed_to_parse_diagram_template_json"]);
                return;
            }

            if (template == null || template.Info == null)
            {
                // 无效的图解模板文件
                MessageHelper.Error(LanguageService.Instance["invalid_diagram_template_file"]);
                return;
            }

            if (!GraphMapTemplateService.IsVersionCompatible(template))
            {
                MessageHelper.Error(LanguageService.Instance["template_version_too_high"]);
                return;
            }

            string validTemplateName = Path.GetFileNameWithoutExtension(jsonPath);
            string sourceDir = Path.GetDirectoryName(jsonPath);

            await FinalizeImport(validTemplateName, template, async (customDir) =>
            {
                await Task.Run(() =>
                {
                    // Copy JSON
                    string targetJsonPath = Path.Combine(customDir, Path.GetFileName(jsonPath));
                    File.Copy(jsonPath, targetJsonPath, true);

                    // Copy Thumbnail if exists
                    string sourceThumbnail = Path.Combine(sourceDir, "thumbnail.jpg");
                    string targetThumbnail = Path.Combine(customDir, "thumbnail.jpg");
                    if (File.Exists(sourceThumbnail))
                    {
                        File.Copy(sourceThumbnail, targetThumbnail, true);
                    }
                    else
                    {
                        // Generate thumbnail
                        GenerateThumbnail(template, targetThumbnail);
                    }

                    // Copy RTF files for supported languages
                    // Get languages from NodeList keys
                    if (template.NodeList != null && template.NodeList.Translations != null)
                    {
                        foreach (var lang in template.NodeList.Translations.Keys)
                        {
                            string validLangName = string.Join("_", lang.Split(Path.GetInvalidFileNameChars()));
                            string sourceRtf = Path.Combine(sourceDir, $"{validLangName}.rtf");
                            string targetRtf = Path.Combine(customDir, $"{validLangName}.rtf");

                            if (File.Exists(sourceRtf))
                            {
                                File.Copy(sourceRtf, targetRtf, true);
                            }
                        }
                    }
                });
            });
        }

        private async Task FinalizeImport(string templateName, GraphMapTemplate template, Func<string, Task> copyAction)
        {
            // 检查数据库中是否存在同名模板
            var existingId = GraphMapDatabaseService.GenerateId(templateName, true);
            var existingTemplate = await Task.Run(() => GraphMapDatabaseService.Instance.GetTemplate(existingId));



            if (existingTemplate != null)
            {
                // 自定义图解模板已经存在，是否覆盖？
                bool confirm = await MessageHelper.ShowAsyncDialog(
                    LanguageService.Instance["custom_diagram_template"] + templateName +
                    LanguageService.Instance["already_exists_overwrite_confirm"],
                    LanguageService.Instance["Cancel"],
                    LanguageService.Instance["overwrite"]);

                if (!confirm) return;
            }

            // 创建临时目录用于解压/处理文件 (为了获取 RTF 和缩略图)
            string tempDir = Path.Combine(Path.GetTempPath(), "GeoChemistryNexus", "ImportTemp", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                // 执行解压/复制操作到临时目录
                await copyAction(tempDir);

                // 4. 更新 LiteDB
                // 确保 NodeList 完整性
                if (template.NodeList == null) template.NodeList = new LocalizedString();

                string fileHash = "";
                string targetJsonPath = Path.Combine(tempDir, $"{templateName}.json");
                if (File.Exists(targetJsonPath))
                {
                    fileHash = UpdateHelper.ComputeFileMd5(targetJsonPath);
                }

                // 同步写入 LiteDB
                await Task.Run(() =>
                {
                    var entity = new GraphMapTemplateEntity
                    {
                        Id = existingId, // 使用确定性 ID
                        GraphMapPath = templateName,
                        FileHash = fileHash,
                        IsCustom = true,
                        LastModified = DateTime.Now,
                        Name = templateName,
                        NodeList = template.NodeList,
                        TemplateType = template.TemplateType,
                        Version = template.Version,
                        Content = template
                    };

                    // 加载 RTF 帮助文档
                    if (template.NodeList != null && template.NodeList.Translations != null)
                    {
                        foreach (var lang in template.NodeList.Translations.Keys)
                        {
                            string validLangName = string.Join("_", lang.Split(Path.GetInvalidFileNameChars()));
                            string targetRtf = Path.Combine(tempDir, $"{validLangName}.rtf");

                            if (File.Exists(targetRtf))
                            {
                                try
                                {
                                    string rtfContent = File.ReadAllText(targetRtf);
                                    entity.HelpDocuments[lang] = rtfContent;
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Failed to read RTF for {lang}: {ex.Message}");
                                }
                            }
                        }
                    }

                    GraphMapDatabaseService.Instance.UpsertTemplate(entity);

                    // 同步缩略图
                    // 增强查找逻辑：支持 png/jpg，不区分大小写
                    string[] possibleNames = { "thumbnail.jpg", "thumbnail.png", "Thumbnail.jpg", "Thumbnail.png", "thumbnail.jpeg" };
                    string thumbPath = null;
                    foreach (var name in possibleNames)
                    {
                        string p = Path.Combine(tempDir, name);
                        if (File.Exists(p))
                        {
                            thumbPath = p;
                            break;
                        }
                    }

                    if (thumbPath != null)
                    {
                        try
                        {
                            using var fs = File.OpenRead(thumbPath);
                            GraphMapDatabaseService.Instance.UploadThumbnail(entity.Id, fs);
                        }
                        catch (Exception ex)
                        {
                            Application.Current.Dispatcher.Invoke(() => HandyControl.Controls.MessageBox.Show($"Failed to upload thumbnail: {ex.Message}"));
                        }
                    }
                });

                // 刷新缓存
                await InitializeAsync();

                MessageHelper.Success(LanguageService.Instance["template_saved_successfully"]);
                CheckUnsavedChanges();
            }
            finally
            {
                // 清理临时目录
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        private void GenerateThumbnail(GraphMapTemplate template, string outputPath)
        {
            try
            {
                var plot = new ScottPlot.Plot();

                // Configure plot based on template type
                if (template.TemplateType == "Ternary")
                {
                    var triangularAxis = plot.Add.TriangularAxis(clockwise: template.Clockwise);
                    var gridDef = template.Info.Grid;
                    if (gridDef != null)
                    {
                        triangularAxis.GridLineStyle.Color = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(gridDef.MajorGridLineColor));
                        triangularAxis.GridLineStyle.Width = gridDef.MajorGridLineWidth;
                        triangularAxis.GridLineStyle.Pattern = GraphMapTemplateService.GetLinePattern(gridDef.MajorGridLinePattern.ToString());
                        if (gridDef.GridAlternateFillingIsEnable)
                            triangularAxis.FillStyle.Color = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(gridDef.GridFillColor1));
                        else
                            triangularAxis.FillStyle.Color = ScottPlot.Colors.Transparent;
                    }
                    plot.Axes.SquareUnits();
                }
                else
                {
                    // Cartesian
                    plot.Axes.Right.IsVisible = true;
                    plot.Axes.Top.IsVisible = true;
                    plot.Axes.Title.IsVisible = true;

                    if (template.Info.Grid != null)
                    {
                        var gridDef = template.Info.Grid;
                        var grid = plot.Grid;
                        grid.XAxisStyle.MajorLineStyle.IsVisible = gridDef.MajorGridLineIsVisible;
                        grid.YAxisStyle.MajorLineStyle.IsVisible = gridDef.MajorGridLineIsVisible;
                        grid.MajorLineColor = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(gridDef.MajorGridLineColor));
                        grid.MajorLineWidth = gridDef.MajorGridLineWidth;
                        grid.MajorLinePattern = GraphMapTemplateService.GetLinePattern(gridDef.MajorGridLinePattern.ToString());
                    }
                }

                // Title
                if (template.Info.Title.Label.Translations.Any())
                {
                    plot.Axes.Title.Label.Text = template.Info.Title.Label.Get();
                    plot.Axes.Title.Label.FontName = template.Info.Title.Family;
                    plot.Axes.Title.Label.FontSize = template.Info.Title.Size;
                    plot.Axes.Title.Label.ForeColor = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(template.Info.Title.Color));
                    plot.Axes.Title.Label.Bold = template.Info.Title.IsBold;
                    plot.Axes.Title.Label.Italic = template.Info.Title.IsItalic;
                }

                // Create layers without events
                var layers = CreateLayersFromTemplate(template, false);
                var allNodes = FlattenTree(layers);

                // Render
                foreach (var layer in allNodes.OfType<IPlotLayer>().Where(l => ((LayerItemViewModel)l).IsVisible))
                {
                    layer.Render(plot);
                }

                if (template.TemplateType != "Ternary")
                    plot.Axes.AutoScale();

                plot.SavePng(outputPath, 400, 300);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Thumbnail generation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 打开一个已存在的模板文件
        /// </summary>
        [RelayCommand]
        private async Task OpenTemplate()
        {
            // 配置并显示文件打开对话框
            var openFileDialog = new VistaOpenFileDialog
            {
                Title = LanguageService.Instance["open_template"],
                Filter = $"{LanguageService.Instance["template_files"]} (*.json;*.zip)|*.json;*.zip|{LanguageService.Instance["all_files"]} (*.*)|*.*",
                DefaultExt = ".json",
                CheckFileExists = true, // 确保文件存在
                Multiselect = false
            };

            // 如果用户选择了一个文件并点击了 "确定"
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // 确保清除语言覆盖
                    LocalizedString.OverrideLanguage = null;

                    // 获取用户选择的完整文件路径
                    string filePath = openFileDialog.FileName;

                    // 处理 Zip 文件：解压到临时目录
                    if (Path.GetExtension(filePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        string tempDir = Path.Combine(Path.GetTempPath(), "GeoChemistryNexus", "TempTemplates", Guid.NewGuid().ToString());
                        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                        Directory.CreateDirectory(tempDir);

                        await Task.Run(() => System.IO.Compression.ZipFile.ExtractToDirectory(filePath, tempDir));

                        // 查找 .json 文件
                        var jsonFiles = Directory.GetFiles(tempDir, "*.json", SearchOption.AllDirectories);
                        if (jsonFiles.Length == 0)
                        {
                            // 无效的模板文件：Zip 包中未找到 .json 文件
                            MessageHelper.Error(LanguageService.Instance["invalid_diagram_template_no_json"]);
                            return;
                        }

                        // 优先查找与文件夹同名的 json，否则取第一个
                        string folderName = new DirectoryInfo(tempDir).GetDirectories().FirstOrDefault()?.Name;
                        if (!string.IsNullOrEmpty(folderName))
                        {
                            var match = jsonFiles.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(folderName, StringComparison.OrdinalIgnoreCase));
                            filePath = match ?? jsonFiles[0];
                        }
                        else
                        {
                            filePath = jsonFiles[0];
                        }
                    }

                    // 预先读取并校验版本
                    try
                    {
                        string jsonContent = await File.ReadAllTextAsync(filePath);
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                        };

                        var tempTemplate = JsonSerializer.Deserialize<GraphMapTemplate>(jsonContent, options);

                        if (tempTemplate != null && !GraphMapTemplateService.IsVersionCompatible(tempTemplate))
                        {
                            MessageHelper.Error(LanguageService.Instance["template_version_too_high"]);
                            return;
                        }
                    }
                    catch
                    {
                        // 忽略
                    }

                    // 将当前编辑的文件路径更新为用户选择的路径，以便后续保存操作
                    _currentTemplateFilePath = filePath;

                    string customRoot = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "Custom");
                    // 强制开启编辑模式
                    _isCurrentTemplateCustom = true;
                    UpdateHelpDocReadOnlyState();

                    // 切换到绘图模式
                    IsTemplateMode = false;
                    IsPlotMode = true;

                    // 异步加载模板文件，构建图层并刷新绘图
                    if (!await LoadAndBuildLayers(filePath))
                    {
                        await BackToTemplateMode();
                        return;
                    }

                    // 尝试加载与模板文件位于同一目录下的说明文件 (.rtf)
                    string directoryPath = Path.GetDirectoryName(filePath);
                    var tempRTFfile = FileHelper.FindFileOrGetFirstWithExtension(
                                          directoryPath,
                                          CurrentDiagramLanguage,
                                          ".rtf");
                    _currentRtfFilePath = tempRTFfile;
                    RtfHelper.LoadRtfToRichTextBox(tempRTFfile, _richTextBox);

                    // 加载数据表格控件
                    PrepareDataGridForInput();

                    // 通知
                    MessageHelper.Success(LanguageService.Instance["template_loaded_successfully"]);
                }
                catch (Exception ex)
                {
                    // 如果加载过程中出现任何错误，通知用户
                    MessageHelper.Error($"{LanguageService.Instance["template_load_failed"]}: {ex.Message}");
                    // 加载失败后，返回到模板选择界面
                    await BackToTemplateMode();
                }
            }
        }

        /// <summary>
        /// 从数据表格中读取数据并进行投点
        /// 新的数据导入逻辑
        /// </summary>
        [RelayCommand]
        private void PlotDataFromGrid()
        {
            // 关闭数据状态栏提示
            IsDataStateReminderVisible = false;

            // 验证模板和脚本是否有效
            if (CurrentTemplate?.Script == null || string.IsNullOrEmpty(CurrentTemplate.Script.RequiredDataSeries))
            {
                // 模板中未定义脚本
                MessageHelper.Error(LanguageService.Instance["script_not_defined_in_template"]);
                return;
            }

            var scriptDefinition = CurrentTemplate.Script;

            // 从 ReoGridControl 读取数据到 DataTable
            var worksheet = _dataGrid.Worksheets[0];
            var dataTable = new DataTable();
            var requiredColumns = new List<string>();

            // 根据表头创建 DataTable 的列，并记录列索引映射
            var columnMapping = new List<int>(); // 存储 DataTable 列对应 Worksheet 的列索引

            for (int i = 0; i < worksheet.ColumnCount; i++)
            {
                var header = worksheet.ColumnHeaders[i];
                if (string.IsNullOrEmpty(header.Text)) continue; // 跳过空表头，而不是停止

                // 防止重名列
                string columnName = header.Text;
                int duplicateCount = 1;
                while (dataTable.Columns.Contains(columnName))
                {
                    columnName = $"{header.Text}_{duplicateCount++}";
                }

                dataTable.Columns.Add(columnName);
                requiredColumns.Add(columnName);
                columnMapping.Add(i);
            }

            // 添加原始行号列
            dataTable.Columns.Add("OriginalRowIndex", typeof(int));

            // 如果没有有效的列，则不继续
            if (dataTable.Columns.Count == 0)
            {
                // 未定义数据列
                MessageHelper.Warning(LanguageService.Instance["no_data_columns_defined"]);
                return;
            }

            // 遍历行来填充 DataTable
            for (int r = 0; r <= worksheet.MaxContentRow; r++)
            {
                var newRow = dataTable.NewRow();
                bool isRowEmpty = true;

                for (int c = 0; c < dataTable.Columns.Count - 1; c++) // -1 because of OriginalRowIndex
                {
                    // 使用映射获取对应的 Worksheet 列索引
                    int worksheetColIndex = columnMapping[c];
                    var cellValue = worksheet.GetCellData(r, worksheetColIndex)?.ToString();

                    newRow[c] = cellValue;
                    if (!string.IsNullOrWhiteSpace(cellValue))
                    {
                        isRowEmpty = false;
                    }
                }
                // 如果整行都是空的，则跳过
                if (!isRowEmpty)
                {
                    newRow["OriginalRowIndex"] = r;
                    dataTable.Rows.Add(newRow);
                }
            }

            if (dataTable.Rows.Count == 0)
            {

                // 无数据，请添加数据
                MessageHelper.Info(LanguageService.Instance["no_data_please_add_data"]);
                return;
            }

            // 清除之前导入的数据点
            ClearExistingPlottedData();

            // 1. 确定 Category 列
            // 优先查找名为 "Category" 的列
            string categoryColumn = "Category";
            bool categoryFound = false;
            foreach (DataColumn col in dataTable.Columns)
            {
                if (string.Equals(col.ColumnName, "Category", StringComparison.OrdinalIgnoreCase))
                {
                    categoryColumn = col.ColumnName;
                    categoryFound = true;
                    break;
                }
            }

            // 如果没找到 "Category"，回退到第一列（兼容旧逻辑，防止用户重命名了Category列但仍在第一列）
            if (!categoryFound && dataTable.Columns.Count > 0)
            {
                categoryColumn = dataTable.Columns[0].ColumnName;
            }

            // 2. 确定数据列
            // 从脚本定义中获取需要的数据列名
            var requiredSeries = scriptDefinition.RequiredDataSeries
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();

            // 3. 校验数据列是否存在
            var missingColumns = requiredSeries.Where(s => !dataTable.Columns.Contains(s)).ToList();
            if (missingColumns.Any())
            {
                // 如果缺少必要的列，提示用户
                MessageHelper.Warning(LanguageService.Instance["missing_columns"] + ": " + string.Join(", ", missingColumns));
                return;
            }

            // 使用脚本要求的列作为数据列
            var dataColumns = requiredSeries;

            // 先对数据进行分组
            var groupedData = dataTable.AsEnumerable()
                .Select(row => new { Row = row, OriginalRowIndex = row.Field<int>("OriginalRowIndex") })
                .Where(x => x.Row[categoryColumn] != null && !string.IsNullOrEmpty(x.Row[categoryColumn].ToString()))
                .GroupBy(x => x.Row.Field<string>(categoryColumn));

            // 检查分组是否成功
            if (!groupedData.Any())
            {
                MessageHelper.Warning(LanguageService.Instance["failed_to_parse_category_group"]);
                return;
            }

            // 确认数据有效后，再创建“数据点”的根节点
            var rootDataNode = GetOrCreateCategory(LanguageService.Instance["data_point"]);
            rootDataNode.IsExpanded = true;

            var palette = new ScottPlot.Palettes.Category10();
            int colorIndex = 0;

            var engine = new Jint.Engine();

            // ===================================
            //  根据图表类型选择不同的投点逻辑
            // ===================================
            if (BaseMapType == "Ternary")
            {
                // --- 三元图投点逻辑 ---
                var triangularAxis = WpfPlot1.Plot.GetPlottables().OfType<ScottPlot.Plottables.TriangularAxis>().FirstOrDefault();
                if (triangularAxis == null)
                {
                    MessageHelper.Error("错误：在图中找不到三元坐标轴对象。");
                    return;
                }

                // 用于保存校验失败的数据信息
                var validationErrors = new List<string>();

                foreach (var group in groupedData)
                {
                    string categoryName = group.Key;
                    if (string.IsNullOrWhiteSpace(categoryName)) continue;
                    var groupColor = palette.GetColor(colorIndex++);
                    var cartesianCoords = new List<Coordinates>();
                    var rowIndices = new List<int>();

                    foreach (var item in group)
                    {
                        DataRow row = item.Row;
                        int rowIndex = item.OriginalRowIndex; // 获取原始数据行号
                        try
                        {
                            var ternaryValues = CalculateCoordinatesUsingScript(engine, row, dataColumns, scriptDefinition.ScriptBody);
                            // 脚本必须为三元图返回三个值
                            if (ternaryValues != null && ternaryValues.Length == 3)
                            {
                                double bottomVal = ternaryValues[0];
                                double leftVal = ternaryValues[1];
                                double rightVal = ternaryValues[2];

                                // 计算三个分量的和
                                double sum = bottomVal + leftVal + rightVal;

                                // 如果和接近0，视为无效数据，跳过
                                if (Math.Abs(sum) < 1e-9)
                                {
                                    continue;
                                }

                                // 如果和显著大于1（例如，接近100），则认为是百分比整数形式，进行归一化
                                if (sum > 1.1)
                                {
                                    bottomVal /= sum;
                                    leftVal /= sum;
                                    rightVal /= sum;
                                }

                                // 将三元坐标转换为笛卡尔坐标
                                var cartesianCoord = triangularAxis.GetCoordinates(bottomVal, leftVal, rightVal);
                                cartesianCoords.Add(cartesianCoord);
                                rowIndices.Add(rowIndex);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"计算三元坐标时出错: {ex.Message}");
                        }
                    }

                    if (!cartesianCoords.Any()) continue;

                    var scatterDefForCategory = new ScatterDefinition
                    {
                        Color = groupColor.ToHex(),
                        Size = 10
                    };

                    if (cartesianCoords.Any())
                    {
                        scatterDefForCategory.StartAndEnd.X = cartesianCoords.First().X;
                        scatterDefForCategory.StartAndEnd.Y = cartesianCoords.First().Y;
                    }

                    var categoryViewModel = new ScatterLayerItemViewModel(scatterDefForCategory)
                    {
                        Name = categoryName,
                        //Plottable = scatterPlotForCategory,
                        DataPoints = cartesianCoords,
                        OriginalRowIndices = rowIndices,
                        IsVisible = true
                    };

                    rootDataNode.Children.Add(categoryViewModel);
                }

                // 如果存在校验失败的数据，则进行提示
                if (validationErrors.Any())
                {
                    // 部分数据未通过校验，已跳过绘制
                    string fullErrorMessage = LanguageService.Instance["some_data_failed_validation"] + "：\n" + string.Join("\n", validationErrors);
                    MessageHelper.Warning(fullErrorMessage);
                }
            }
            else // --- 笛卡尔坐标系投点逻辑 ---
            {
                foreach (var group in groupedData)
                {
                    string categoryName = group.Key;
                    if (string.IsNullOrWhiteSpace(categoryName)) continue;

                    var groupColor = palette.GetColor(colorIndex++);
                    var xs = new List<double>();
                    var ys = new List<double>();
                    var rowIndices = new List<int>();

                    foreach (var item in group) // item 包含 Row 和 Index
                    {
                        DataRow row = item.Row;
                        int rowIndex = item.OriginalRowIndex;
                        try
                        {
                            var coordinates = CalculateCoordinatesUsingScript(engine, row, dataColumns, scriptDefinition.ScriptBody);
                            // 脚本必须为笛卡尔坐标图返回两个值
                            if (coordinates != null && coordinates.Length == 2)
                            {
                                xs.Add(coordinates[0]);
                                ys.Add(coordinates[1]);
                                rowIndices.Add(rowIndex);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"计算坐标时出错: {ex.Message}");
                        }
                    }

                    if (!xs.Any()) continue;

                    // 准备坐标点列表
                    var points = new List<Coordinates>();
                    for (int i = 0; i < xs.Count; i++)
                    {
                        points.Add(new Coordinates(xs[i], ys[i]));
                    }

                    var scatterDefForCategory = new ScatterDefinition
                    {
                        Color = groupColor.ToHex(),
                        Size = 10
                    };

                    if (points.Any())
                    {
                        scatterDefForCategory.StartAndEnd.X = points.First().X;
                        scatterDefForCategory.StartAndEnd.Y = points.First().Y;
                    }

                    var categoryViewModel = new ScatterLayerItemViewModel(scatterDefForCategory)
                    {
                        Name = categoryName,
                        //Plottable = scatterPlotForCategory,
                        DataPoints = points,
                        OriginalRowIndices = rowIndices,
                        IsVisible = true
                    };
                    rootDataNode.Children.Add(categoryViewModel);
                }
            }

            // 刷新图表和图例
            WpfPlot1.Plot.Legend.IsVisible = true;
            //WpfPlot1.Refresh();
            RefreshPlotFromLayers();
            CenterPlot();
        }

        /// <summary>
        /// 检查模板列表更新的命令
        /// </summary>
        [RelayCommand]
        private async Task CheckForTemplateUpdates()
        {
            try
            {
                // 获取本地 GraphMapList.json
                string localListPath = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "GraphMapList.json");

                // 计算本地文件的哈希值
                string localHash = UpdateHelper.ComputeFileMd5(localListPath);

                // 获取本地 PlotTemplateCategories.json
                string localCategoryPath = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "PlotTemplateCategories.json");

                // 从服务器获取 server_info.json
                string jsonContent = await UpdateHelper.GetUrlContentAsync();

                // 反序列化 JSON
                var serverInfo = JsonSerializer.Deserialize<ServerInfo>(jsonContent);

                if (serverInfo == null) return;

                // 检查数据库文件是否存在
                string dbPath = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "Templates.db");
                bool isDbMissing = !File.Exists(dbPath);

                // 检查列表是否需要更新
                bool isListOutdated = !string.Equals(localHash, serverInfo.ListHash, StringComparison.OrdinalIgnoreCase);

                // 如果列表需要更新或者数据库缺失，提示用户
                if (isListOutdated || isDbMissing)
                {
                    // 哈希不匹配，提示用户更新
                    // 检测到绘图模板库有新版本，是否立即更新列表？
                    bool confirmUpdate = await NotificationManager.Instance.ShowDialogAsync(
                        LanguageService.Instance["Confirm"],
                        LanguageService.Instance["new_drawing_template_library_version_detected"],
                        LanguageService.Instance["Confirm"],
                        LanguageService.Instance["Cancel"]);

                    if (!confirmUpdate) return;

                    // 如果列表需要更新，执行完整的列表更新 (内部会包含分类更新)
                    await PerformTemplateListUpdate(serverInfo.ListHash, serverInfo.ListPlotCategoriesHash);
                }
                else
                {
                    // 列表已是最新
                    // 检查分类文件是否缺失 (如果不缺失，即使Hash不一致也忽略，等待下次列表更新)
                    if (!File.Exists(localCategoryPath))
                    {
                        // 本地缺失分类文件，静默补全
                        await PerformCategoryListUpdate(serverInfo.ListPlotCategoriesHash, showMessages: false);
                    }

                    // 当前模板列表已是最新版本。
                    MessageHelper.Success(LanguageService.Instance["current_template_list_latest_version"]);
                }
            }
            catch (HttpRequestException netEx)
            {
                // 网络连接失败，无法检查更新
                MessageHelper.Error(LanguageService.Instance["network_connection_failed_cannot_check_for_updates"] + $"{netEx.Message}");
            }
            catch (Exception ex)
            {
                // 检查更新时发生错误：
                MessageHelper.Error(LanguageService.Instance["error_occurred_while_checking_for_updates"] + $"{ex.Message}");
            }
        }

        /// <summary>
        /// 执行具体的列表更新逻辑
        /// </summary>
        /// <param name="expectedHash">从服务器 server_info.json 获取的期望哈希值</param>
        /// <param name="expectedCategoryHash">从服务器 server_info.json 获取的期望分类结构哈希值</param>
        private async Task PerformTemplateListUpdate(string expectedHash = null, string expectedCategoryHash = null)
        {
            // 生成一个唯一的临时文件路径
            string tempFilePath = Path.GetTempFileName();
            string localListPath = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "GraphMapList.json");

            try
            {
                // 服务器端的 GraphMapList.json 下载地址
                string listDownloadUrl = "https://geochemistrynexus-1303234197.cos.ap-hongkong.myqcloud.com/GraphMapList.json";

                // 下载到临时文件
                await UpdateHelper.DownloadFileAsync(listDownloadUrl, tempFilePath);

                // 校验数据完整性
                // 哈希校验 (如果传入了期望值)
                if (!string.IsNullOrEmpty(expectedHash))
                {
                    string downloadedHash = UpdateHelper.ComputeFileMd5(tempFilePath);
                    // 不区分大小写比较
                    if (!string.Equals(downloadedHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                    {
                        // 下载的文件哈希值与服务器不匹配，文件可能已损坏或遭到篡改。
                        throw new Exception(LanguageService.Instance["downloaded_file_hash_mismatch"]);
                    }
                }

                // JSON 格式校验
                try
                {
                    string jsonContent = File.ReadAllText(tempFilePath);
                    // 尝试解析一下，如果格式错误会抛出 JsonException
                    JsonDocument.Parse(jsonContent);
                }
                catch (JsonException)
                {
                    // 下载的内容不是有效的 JSON 格式。
                    throw new Exception(LanguageService.Instance["downloaded_content_not_valid_json"]);
                }

                // 校验通过，安全覆盖本地文件
                // 确保目标目录存在
                string dir = Path.GetDirectoryName(localListPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                // 使用 Move 覆盖
                File.Move(tempFilePath, localListPath, true);

                // 更新分类结构 (静默更新)
                await PerformCategoryListUpdate(expectedCategoryHash, showMessages: false);

                // 模板列表更新成功！正在刷新...
                MessageHelper.Success(LanguageService.Instance["template_list_update_success_refreshing"]);

                // 同步本地数据库与新的模板列表
                await Task.Run(() =>
                {
                    string newListContent = File.ReadAllText(localListPath);
                    var newTemplateList = JsonSerializer.Deserialize<List<GraphMapTemplateService.JsonTemplateItem>>(newListContent);

                    if (newTemplateList != null)
                    {
                        var dbService = GraphMapDatabaseService.Instance;
                        // 获取所有现有模板的摘要，建立 ID -> Entity 映射，减少 DB 查询
                        var existingTemplates = dbService.GetSummaries().ToDictionary(x => x.Id, x => x);

                        foreach (var item in newTemplateList)
                        {
                            if (Guid.TryParse(item.ID, out Guid itemId))
                            {
                                if (existingTemplates.TryGetValue(itemId, out var existingEntity))
                                {
                                    // 存在：比较 Hash
                                    bool isHashSame = string.Equals(existingEntity.FileHash, item.FileHash, StringComparison.OrdinalIgnoreCase);
                                    string newStatus = isHashSame ? "UP_TO_DATE" : "OUTDATED";

                                    // 只有状态改变或 Hash 改变时才更新
                                    if (existingEntity.Status != newStatus || existingEntity.FileHash != item.FileHash)
                                    {
                                        existingEntity.Status = newStatus;
                                        // 因此，这里只更新 Status。
                                        dbService.UpsertTemplate(existingEntity);
                                    }
                                }
                                else
                                {
                                    // 不存在：添加新记录
                                    var newEntity = new GraphMapTemplateEntity
                                    {
                                        Id = itemId,
                                        NodeList = item.NodeList,
                                        GraphMapPath = item.GraphMapPath,
                                        FileHash = item.FileHash, // 这里存的是服务器 Hash，但因为是 NOT_INSTALLED，不影响逻辑
                                        IsCustom = false,
                                        Status = "NOT_INSTALLED",
                                        LastModified = DateTime.Now,
                                        // 其他字段留空
                                        Content = null,
                                        TemplateType = null,
                                        Version = 0,
                                        HelpDocuments = new Dictionary<string, string>()
                                    };
                                    dbService.UpsertTemplate(newEntity);
                                }
                            }
                        }
                    }
                });

                // 刷新 UI (重新加载卡片)
                _isTemplateLibraryDirty = true;
                await BackToTemplateMode();
            }
            catch (Exception ex)
            {
                // 更新列表文件失败
                MessageHelper.Error(LanguageService.Instance["update_list_file_failed"] + $" {ex.Message}");
            }
            finally
            {
                // 清理临时文件
                if (File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                    }
                    catch { /* 临时文件删除失败 */ }
                }
            }
        }


        /// <summary>
        /// 执行具体的类别列表更新逻辑
        /// </summary>
        /// <param name="expectedHash">从服务器 server_info.json 获取的期望哈希值</param>
        /// <param name="showMessages">是否显示消息提示</param>
        private async Task PerformCategoryListUpdate(string expectedHash = null, bool showMessages = true)
        {
            // 生成一个唯一的临时文件路径
            string tempFilePath = Path.GetTempFileName();
            string localListPath = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "PlotTemplateCategories.json");

            try
            {
                // 服务器端的 PlotTemplateCategories.json 下载地址
                string listDownloadUrl = "https://geochemistrynexus-1303234197.cos.ap-hongkong.myqcloud.com/PlotTemplateCategories.json";

                // 下载到临时文件
                await UpdateHelper.DownloadFileAsync(listDownloadUrl, tempFilePath);

                // 校验数据完整性
                if (!string.IsNullOrEmpty(expectedHash))
                {
                    string downloadedHash = UpdateHelper.ComputeFileMd5(tempFilePath);
                    if (!string.Equals(downloadedHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception(LanguageService.Instance["downloaded_file_hash_mismatch"]);
                    }
                }

                // JSON 格式校验
                try
                {
                    string jsonContent = File.ReadAllText(tempFilePath);
                    JsonDocument.Parse(jsonContent);
                }
                catch (JsonException)
                {
                    throw new Exception(LanguageService.Instance["downloaded_content_not_valid_json"]);
                }

                // 校验通过，安全覆盖本地文件
                string dir = Path.GetDirectoryName(localListPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                File.Move(tempFilePath, localListPath, true);

                if (showMessages)
                {
                    MessageHelper.Success(LanguageService.Instance["template_list_update_success_refreshing"]);
                }

                // 发送消息通知 UI 重新加载配置
                WeakReferenceMessenger.Default.Send(new CategoryConfigUpdatedMessage("Updated"));
            }
            catch (Exception ex)
            {
                if (showMessages)
                {
                    MessageHelper.Error(LanguageService.Instance["update_list_file_failed"] + $" {ex.Message}");
                }
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// “添加箭头”按钮的命令
        /// </summary>
        [RelayCommand]
        private void AddArrow()
        {
            IsAddingArrow = true;
        }

        /// <summary>
        /// “添加函数”按钮的命令
        /// </summary>
        [RelayCommand]
        private void AddFunction()
        {
            // 取消高亮选择
            CancelSelected();

            // 关闭其他模式
            IsAddingLine = false;
            IsAddingPolygon = false;
            IsAddingText = false;
            IsAddingArrow = false;

            // 创建默认函数对象
            var funcDef = new FunctionDefinition
            {
                Formula = "Math.sin(x)",
                MinX = -10,
                MaxX = 10,
                PointCount = 1000,
                Color = "#FF0000",
                Width = 2
            };

            // 查找或创建 "Function" 分类
            var funcCategory = GetOrCreateCategory("Function");

            // 添加图层
            var funcLayer = new FunctionLayerItemViewModel(funcDef, funcCategory.Children.Count);
            funcLayer.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(FunctionLayerItemViewModel.IsVisible)) RefreshPlotFromLayers(true); };
            funcCategory.Children.Add(funcLayer);

            RefreshPlotFromLayers(true);
            SelectLayer(funcLayer);

            // 记录撤销/重做
            Action undo = () =>
            {
                var cat = LayerTree.FirstOrDefault(c => c is CategoryLayerItemViewModel vm && vm.Name == "Function") as CategoryLayerItemViewModel;
                if (cat != null)
                {
                    if (cat.Children.Contains(funcLayer)) cat.Children.Remove(funcLayer);
                    if (cat.Children.Count == 0) LayerTree.Remove(cat);
                }
                RefreshPlotFromLayers(true);
            };
            Action redo = () =>
            {
                var cat = GetOrCreateCategory("Function");
                if (!cat.Children.Contains(funcLayer)) cat.Children.Add(funcLayer);
                RefreshPlotFromLayers(true);
            };
            AddUndoState(undo, redo);
        }

        // 撤销/重做 栈
        private readonly Stack<(Action Undo, Action Redo)> _undoStack = new();
        private readonly Stack<(Action Undo, Action Redo)> _redoStack = new();
        private const int UndoRedoLimit = 10;

        private void TrimStack(Stack<(Action Undo, Action Redo)> stack)
        {
            if (stack.Count <= UndoRedoLimit) return;
            var temp = new Stack<(Action Undo, Action Redo)>();
            while (stack.Count > 0) temp.Push(stack.Pop());
            temp.Pop();
            while (temp.Count > 0) stack.Push(temp.Pop());
        }

        /// <summary>
        /// 添加撤销/重做状态
        /// </summary>
        public void AddUndoState(Action undo, Action redo)
        {
            _undoStack.Push((undo, redo));
            _redoStack.Clear();
            TrimStack(_undoStack);
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
            CheckUnsavedChanges();
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void Undo()
        {
            CancelSelected();
            if (_undoStack.TryPop(out var action))
            {
                action.Undo();
                _redoStack.Push(action);
                TrimStack(_redoStack);
                UndoCommand.NotifyCanExecuteChanged();
                RedoCommand.NotifyCanExecuteChanged();
                WpfPlot1.Refresh();
                CheckUnsavedChanges();
            }
        }

        private bool CanUndo() => _undoStack.Count > 0;

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void Redo()
        {
            if (_redoStack.TryPop(out var action))
            {
                action.Redo();
                _undoStack.Push(action);
                TrimStack(_undoStack);
                UndoCommand.NotifyCanExecuteChanged();
                RedoCommand.NotifyCanExecuteChanged();
                WpfPlot1.Refresh();
                CheckUnsavedChanges();
            }
        }

        private bool CanRedo() => _redoStack.Count > 0;

        /// <summary>
        /// 删除当前选中的绘图对象。
        /// </summary>
        [RelayCommand]
        private void DeleteSelectedObject()
        {
            // 检查是否有选中的图层
            if (_selectedLayer == null && SelectedLayers.Count == 0)
            {
                // 请先选择一个要删除的对象.
                MessageHelper.Warning(LanguageService.Instance["please_select_an_object_to_delete_first"]);
                return;
            }

            // 禁止删除坐标轴等基础图层
            if (SelectedLayers.Any(l => l is AxisLayerItemViewModel) || (_selectedLayer is AxisLayerItemViewModel))
            {
                // 无法删除基础图层.
                MessageHelper.Warning(LanguageService.Instance["cannot_delete_base_layers"]);
                return;
            }

            // 确定要删除的图层列表
            var layersToDelete = SelectedLayers.ToList();
            if (layersToDelete.Count == 0 && _selectedLayer != null)
            {
                layersToDelete.Add(_selectedLayer);
            }

            // --- 捕获删除前的状态 ---
            // 存储每个被删图层的信息：(图层对象, ScottPlot对象, 父图层, 在父图层中的索引,父图层在根中的索引)
            var deletedInfos = new List<(LayerItemViewModel Layer, ScottPlot.IPlottable? Plottable, CategoryLayerItemViewModel? Parent, int Index, bool ParentRemoved, int ParentIndex)>();

            foreach (var layer in layersToDelete)
            {
                var parent = FindParentLayer(LayerTree, layer);
                int index = parent != null ? parent.Children.IndexOf(layer) : LayerTree.IndexOf(layer);

                int parentIndex = -1;

                if (parent != null)
                {
                    // 记录父节点在 LayerTree 的索引
                    parentIndex = LayerTree.IndexOf(parent);
                }

                deletedInfos.Add((layer, layer.Plottable, parent, index, false, parentIndex));
            }

            // 定义 Redo 操作 (执行删除)
            Action redo = () =>
            {
                foreach (var info in deletedInfos)
                {
                    // 从图层树移除
                    if (info.Parent != null)
                    {
                        if (info.Parent.Children.Contains(info.Layer))
                            info.Parent.Children.Remove(info.Layer);

                        // 检查父图层是否为空
                        if (info.Parent.Children.Count == 0 && LayerTree.Contains(info.Parent))
                        {
                            LayerTree.Remove(info.Parent);
                        }
                    }
                    else
                    {
                        if (LayerTree.Contains(info.Layer))
                            LayerTree.Remove(info.Layer);
                    }
                }

                // 重置选中状态
                CancelSelected();
                RefreshPlotFromLayers(true);
            };

            // 定义 Undo 操作 (恢复删除)
            Action undo = () =>
            {
                // 逆序恢复，以保持顺序
                for (int i = deletedInfos.Count - 1; i >= 0; i--)
                {
                    var info = deletedInfos[i];

                    // 恢复 Layer
                    if (info.Parent != null)
                    {
                        // 检查父节点是否需要恢复
                        if (!LayerTree.Contains(info.Parent))
                        {
                            // 尝试恢复到原来的位置
                            if (info.ParentIndex >= 0 && info.ParentIndex <= LayerTree.Count)
                                LayerTree.Insert(info.ParentIndex, info.Parent);
                            else
                                LayerTree.Add(info.Parent);
                        }

                        // 将图层加回父节点
                        if (!info.Parent.Children.Contains(info.Layer))
                        {
                            if (info.Index >= 0 && info.Index <= info.Parent.Children.Count)
                                info.Parent.Children.Insert(info.Index, info.Layer);
                            else
                                info.Parent.Children.Add(info.Layer);
                        }
                    }
                    else
                    {
                        // 顶级图层
                        if (!LayerTree.Contains(info.Layer))
                        {
                            if (info.Index >= 0 && info.Index <= LayerTree.Count)
                                LayerTree.Insert(info.Index, info.Layer);
                            else
                                LayerTree.Add(info.Layer);
                        }
                    }
                }
                RefreshPlotFromLayers(true);
            };

            // 记录并执行
            AddUndoState(undo, redo);
            redo();

            // 对象已成功删除.
            MessageHelper.Success(LanguageService.Instance["object_deleted_successfully"]);
        }

        /// <summary>
        /// 递归地在图层树中查找指定子项的父项。
        /// </summary>
        /// <param name="collection">要搜索的图层集合</param>
        /// <param name="childToFind">要查找父项的目标子项</param>
        /// <returns>找到的父图层项，如果未找到则返回 null</returns>
        private CategoryLayerItemViewModel? FindParentLayer(ObservableCollection<LayerItemViewModel> collection, LayerItemViewModel childToFind)
        {
            foreach (var item in collection)
            {
                // 检查当前项是否是 Category 类型并且其子项包含目标
                if (item is CategoryLayerItemViewModel category && category.Children.Contains(childToFind))
                {
                    return category;
                }

                // 如果当前项还有子项，则递归进入其子项中查找
                if (item.Children.Any())
                {
                    var parent = FindParentLayer(item.Children, childToFind);
                    if (parent != null)
                    {
                        return parent;
                    }
                }
            }
            return null; // 在当前层级和所有子层级都未找到
        }



        /// <summary>
        /// 将绘图控件的状态重置为默认值，以确保在加载新模板时有一个干净的环境。
        /// 清除所有绘图对象，并重置坐标轴、布局和所有特殊规则。
        /// </summary>
        /// <summary>
        /// 重置所有添加绘图对象的模式
        /// </summary>
        private void ResetEditModes()
        {
            IsAddingLine = false;
            IsAddingText = false;
            IsAddingPolygon = false;
            IsAddingArrow = false;
        }

        private void ResetPlotStateToDefault()
        {
            // 重置编辑模式
            ResetEditModes();

            // 清除语言覆盖
            LocalizedString.OverrideLanguage = null;

            // 重置编辑确认状态
            _hasConfirmedEditMode = false;

            // 清空撤销/重做栈
            _undoStack.Clear();
            _redoStack.Clear();
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();

            // 清除所有绘图对象
            WpfPlot1.Plot.Clear();

            // 重置坐标轴布局为默认值
            WpfPlot1.Plot.Layout.Default();

            // 移除所有与轴相关的自定义规则
            WpfPlot1.Plot.Axes.UnlinkAll();
            WpfPlot1.Plot.Axes.Rules.Clear();

            // 重新添加十字轴，确保它存在且在最上层
            WpfPlot1.Plot.Add.Plottable(CrosshairPlot);
            // 解决从三元图(被隐藏)切回二维图后，对象依然处于隐藏状态的问题
            CrosshairPlot.IsVisible = false;
            // 默认功能开关重置为关闭
            IsCrosshairVisible = false;

            // 确保将全局变量重置为默认状态
            MainPlotViewModel.BaseMapType = String.Empty;
            MainPlotViewModel.Clockwise = true;

            // 清除图层树和属性面板的绑定
            LayerTree.Clear();
            PropertyGridModel = null;
            _selectedLayer = null;

            // 清除数据表格
            _dataGrid.Worksheets[0].Reset();

            // 重置原始模板记录
            _originalTemplateJson = string.Empty;
            HasUnsavedChanges = false;

            // 刷新一次以应用所有重置
            WpfPlot1.Refresh();
        }

        /// <summary>
        /// 执行核心的保存操作，将CurrentTemplate保存到数据库
        /// </summary>
        private async Task PerformSave()
        {
            // 版本校验：如果当前程序版本大于模板版本，提示用户升级风险
            float currentAppVersion = UpdateHelper.GetCurrentVersionFloat();
            if (currentAppVersion > CurrentTemplate.Version)
            {
                bool confirmUpgrade = await MessageHelper.ShowAsyncDialog(
                    LanguageService.Instance["template_upgrade_warning"],
                    LanguageService.Instance["Cancel"],
                    LanguageService.Instance["Confirm"]);

                if (!confirmUpgrade) return;
            }

            // 更新模板版本为当前程序版本
            CurrentTemplate.Version = currentAppVersion;

            // 清空模板中原有的动态绘图元素列表并从 LayerTree 更新
            UpdateTemplateInfoFromLayers(CurrentTemplate);

            try
            {
                // 获取 Entity
                var entity = await Task.Run(() => GraphMapDatabaseService.Instance.GetTemplate(_currentTemplateId.GetValueOrDefault()));

                if (entity == null)
                {
                    MessageHelper.Error("Template entity not found in database.");
                    return;
                }

                // 更新 Entity 属性
                entity.Content = CurrentTemplate;
                entity.Version = CurrentTemplate.Version;
                entity.LastModified = DateTime.Now;
                entity.NodeList = CurrentTemplate.NodeList;

                if (!entity.IsCustom)
                {
                    entity.Status = null;

                    // Calculate new hash
                    string jsonString = SerializeTemplate(entity.Content);
                    using (var md5 = System.Security.Cryptography.MD5.Create())
                    {
                        byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);
                        byte[] hashBytes = md5.ComputeHash(inputBytes);
                        entity.FileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    }
                }

                // 更新帮助文档 (当前语言)
                if (!string.IsNullOrEmpty(CurrentDiagramLanguage))
                {
                    string rtfContent = RtfHelper.GetRtfString(_richTextBox);
                    if (entity.HelpDocuments == null) entity.HelpDocuments = new Dictionary<string, string>();
                    entity.HelpDocuments[CurrentDiagramLanguage] = rtfContent;
                }

                // 保存到数据库
                await Task.Run(() => GraphMapDatabaseService.Instance.UpsertTemplate(entity));

                // 标记模板库需要刷新
                _isTemplateLibraryDirty = true;

                // 更新原始状态记录
                _originalTemplateJson = SerializeTemplate(CurrentTemplate);

                // 重置未保存状态
                HasUnsavedChanges = false;
                WeakReferenceMessenger.Default.Send(new UnsavedChangesMessage(false));

                // 更新或生成新的缩略图
                try
                {
                    using (var ms = new MemoryStream())
                    {
                        var imageBytes = WpfPlot1.Plot.GetImageBytes(640, 480);
                        await ms.WriteAsync(imageBytes, 0, imageBytes.Length);
                        ms.Position = 0;

                        await Task.Run(() =>
                        {
                            using (var uploadStream = new MemoryStream(imageBytes))
                            {
                                GraphMapDatabaseService.Instance.UploadThumbnail(entity.Id, uploadStream);
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to save thumbnail: {ex.Message}");
                }

                MessageHelper.Success(LanguageService.Instance["template_saved_successfully"]);
            }
            catch (Exception ex)
            {
                MessageHelper.Error(LanguageService.Instance["save_template_failed"] + $": {ex.Message}");
            }
        }

        /// <summary>
        /// 将二维笛卡尔坐标转换为三元坐标。
        /// </summary>
        /// <param name="x">二维X坐标</param>
        /// <param name="y">二维Y坐标</param>
        /// <param name="clockwise">您的三元图是否为顺时针</param>
        /// <returns>三元坐标值</returns>
        public static (double, double) ToTernary(double x, double y, bool clockwise)
        {
            if (clockwise)
            {
                double leftFraction = (2 * y) / Math.Sqrt(3);
                double rightFraction = x - (y / Math.Sqrt(3));
                double bottomFraction = 1 - leftFraction - rightFraction;
                return (bottomFraction, leftFraction);
            }
            else // Counter-clockwise
            {
                double rightFraction = (2 * y) / Math.Sqrt(3);
                double bottomFraction = x - (y / Math.Sqrt(3));
                double leftFraction = 1 - bottomFraction - rightFraction;
                return (bottomFraction, leftFraction);
            }
        }

        private static double DistancePointToSegment(Pixel P, Pixel A, Pixel B)
        {
            double dx = B.X - A.X;
            double dy = B.Y - A.Y;
            if (dx == 0 && dy == 0)
            {
                return Math.Sqrt(Math.Pow(P.X - A.X, 2) + Math.Pow(P.Y - A.Y, 2));
            }

            double t = ((P.X - A.X) * dx + (P.Y - A.Y) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));

            double closestX = A.X + t * dx;
            double closestY = A.Y + t * dy;

            return Math.Sqrt(Math.Pow(P.X - closestX, 2) + Math.Pow(P.Y - closestY, 2));
        }

        /// <summary>
        /// 将三元坐标转换为二维笛卡尔坐标系
        /// </summary>
        /// <param name="bottomFraction">三元图底部坐标轴</param>
        /// <param name="leftFraction">三元图左侧坐标轴</param>
        /// <param name="rightFraction">三元图右侧坐标轴</param>
        /// <returns>转换后的二维笛卡尔坐标系</returns>
        /// <exception cref="ArgumentException"></exception>
        public static (double, double) ToCartesian(double bottomFraction, double leftFraction, double rightFraction)
        {

            if (Math.Abs(bottomFraction + leftFraction + rightFraction - 1) > 1e-6)
            {
                throw new ArgumentException(LanguageService.Instance["ternary_phase_diagram_sum_must_be_one"]);
            }

            double x, y;

            if (!Clockwise)
            {
                x = 0.5 * (2 * bottomFraction + rightFraction);
                y = (Math.Sqrt(3) / 2) * rightFraction;
            }
            else
            {
                x = 0.5 * (2 * rightFraction + leftFraction);
                y = (Math.Sqrt(3) / 2) * leftFraction;
            }

            return (x, y);
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

            // 保存为csv文件
            string tempFilePath = FileHelper.GetSaveFilePath2(title: LanguageService.Instance["save_as_csv_file"], filter: "CSV文件|*.csv",
                                                                defaultExt: ".csv", defaultFileName: worksheet.Name);
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
        /// 检查单个模板的更新状态
        /// </summary>
        private async Task CheckSingleTemplateUpdate(TemplateCardViewModel card)
        {
            if (card.IsCustomTemplate || !card.TemplateId.HasValue) return;

            await Task.Run(() =>
            {
                var dbService = GraphMapDatabaseService.Instance;
                var entity = dbService.GetTemplate(card.TemplateId.Value);
                TemplateState nextState = TemplateState.NotDownloaded;

                if (entity != null)
                {
                    // 找到了，检查状态
                    if (!string.IsNullOrEmpty(entity.Status))
                    {
                        // 如果状态不是空，说明已经检查过了
                        if (entity.Status == "UP_TO_DATE") nextState = TemplateState.Ready;
                        else if (entity.Status == "OUTDATED") nextState = TemplateState.UpdateAvailable;
                        else if (entity.Status == "NOT_INSTALLED") nextState = TemplateState.NotDownloaded;
                    }
                    else
                    {
                        // 状态为空，进行哈希对比
                        bool isHashSame = string.Equals(entity.FileHash, card.ServerHash, StringComparison.OrdinalIgnoreCase);

                        if (isHashSame)
                        {
                            entity.Status = "UP_TO_DATE";
                            nextState = TemplateState.Ready;
                        }
                        else
                        {
                            entity.Status = "OUTDATED";
                            nextState = TemplateState.UpdateAvailable;
                        }

                        // 更新数据库状态
                        dbService.UpsertTemplate(entity);
                    }
                }
                else
                {
                    // 没找到 -> NOT_INSTALLED
                    // 创建占位实体
                    var newEntity = new GraphMapTemplateEntity
                    {
                        Id = card.TemplateId.Value,
                        GraphMapPath = card.TemplatePath,
                        Name = card.Name,
                        FileHash = card.ServerHash, // 暂存服务器哈希
                        Status = "NOT_INSTALLED",
                        IsCustom = false,
                        LastModified = DateTime.Now
                    };
                    dbService.UpsertTemplate(newEntity);

                    nextState = TemplateState.NotDownloaded;
                }

                // Update UI on Dispatcher
                Application.Current.Dispatcher.Invoke(() =>
                {
                    card.State = nextState;
                });
            });
        }

        /// <summary>
        /// 下载单个模板的具体实现 (Updated for LiteDB)
        /// </summary>
        private async Task DownloadSingleTemplate(TemplateCardViewModel card)
        {
            try
            {
                if (!card.TemplateId.HasValue) throw new Exception("Template ID is missing");

                // 切换状态
                card.State = TemplateState.Downloading;
                card.DownloadProgress = 0;

                // 准备路径: {BaseUrl}/{GraphMapPath}.zip
                string baseUrl = "https://geochemistrynexus-1303234197.cos.ap-hongkong.myqcloud.com";
                // string idStr = card.TemplateId.Value.ToString(); 

                // 对路径进行 URL 编码
                string encodedPath = Uri.EscapeDataString(card.TemplatePath);
                string zipUrl = $"{baseUrl}/Templates/{encodedPath}.zip";
                string tempZipPath = Path.Combine(Path.GetTempPath(), $"{card.TemplatePath}_{Guid.NewGuid()}.zip");
                string tempExtractDir = Path.Combine(Path.GetTempPath(), $"{card.TemplatePath}_{Guid.NewGuid()}_extract");

                // 定义进度回调
                var progressIndicator = new Progress<double>(p => card.DownloadProgress = p);

                // 开始下载
                await UpdateHelper.DownloadFileAsync(zipUrl, tempZipPath, progressIndicator);

                await Task.Run(() =>
                {
                    try
                    {
                        // 解压
                        if (Directory.Exists(tempExtractDir)) Directory.Delete(tempExtractDir, true);
                        Directory.CreateDirectory(tempExtractDir);
                        System.IO.Compression.ZipFile.ExtractToDirectory(tempZipPath, tempExtractDir);

                        // 查找 JSON 文件
                        var jsonFiles = Directory.GetFiles(tempExtractDir, "*.json", SearchOption.AllDirectories);
                        if (jsonFiles.Length == 0) throw new Exception("No JSON file found in the archive.");

                        string jsonPath = jsonFiles[0];
                        string jsonContent = File.ReadAllText(jsonPath);

                        // 反序列化
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        options.Converters.Add(new JsonStringEnumConverter());
                        var template = JsonSerializer.Deserialize<GraphMapTemplate>(jsonContent, options);
                        if (template == null) throw new Exception("Failed to deserialize template.");

                        // 准备实体
                        var entity = new GraphMapTemplateEntity
                        {
                            Id = card.TemplateId.Value,
                            GraphMapPath = card.TemplatePath,
                            Name = card.TemplatePath,
                            FileHash = card.ServerHash, // 使用服务器哈希作为最新哈希
                            Status = "UP_TO_DATE",
                            IsCustom = false,
                            LastModified = DateTime.Now,
                            Content = template,
                            TemplateType = template.TemplateType,
                            Version = template.Version
                        };

                        // 查找帮助文档 (RTF)
                        var rtfFiles = Directory.GetFiles(tempExtractDir, "*.rtf", SearchOption.AllDirectories);
                        foreach (var rtfFile in rtfFiles)
                        {
                            string langCode = Path.GetFileNameWithoutExtension(rtfFile);
                            string content = File.ReadAllText(rtfFile);
                            entity.HelpDocuments[langCode] = content;
                        }

                        // 查找缩略图
                        var imgFiles = Directory.GetFiles(tempExtractDir, "*.*", SearchOption.AllDirectories)
                            .Where(s => s.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase));

                        var thumbFile = imgFiles.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).ToLower().Contains("thumbnail") || Path.GetFileNameWithoutExtension(f) == Path.GetFileNameWithoutExtension(jsonPath));

                        // 更新 DB
                        var oldEntity = GraphMapDatabaseService.Instance.GetTemplate(entity.Id);
                        if (oldEntity != null)
                        {
                            entity.NodeList = oldEntity.NodeList;
                        }

                        GraphMapDatabaseService.Instance.UpsertTemplate(entity);

                        if (thumbFile != null)
                        {
                            using var stream = File.OpenRead(thumbFile);
                            GraphMapDatabaseService.Instance.UploadThumbnail(entity.Id, stream);
                        }
                    }
                    finally
                    {
                        // 清理解压目录
                        if (Directory.Exists(tempExtractDir)) Directory.Delete(tempExtractDir, true);
                    }
                });

                // 清理临时zip
                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);

                // 更新卡片状态
                card.State = TemplateState.Ready;

                // 重新加载缩略图
                var thumbStream = GraphMapDatabaseService.Instance.GetThumbnail(card.TemplateId.Value);
                if (thumbStream != null)
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = thumbStream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    card.ThumbnailImage = bitmap;
                    thumbStream.Dispose();
                }

                MessageHelper.Success($"{card.Name} " + LanguageService.Instance["template_ready"]);
            }
            catch (Exception ex)
            {
                card.State = TemplateState.Error;
                MessageHelper.Error(LanguageService.Instance["download_template_failed"] + $" {ex.Message}");
            }
        }

        #region Layer Order Helpers

        /// <summary>
        /// 获取图层类别的渲染优先级（数值越大越靠上层）
        /// </summary>
        private int GetCategoryPriority(string name)
        {
            if (name == LanguageService.Instance["axes"]) return 0;
            if (name == LanguageService.Instance["polygon"]) return 1;
            if (name == LanguageService.Instance["line"]) return 2;
            if (name == LanguageService.Instance["function"]) return 3;
            if (name == LanguageService.Instance["point"]) return 4;
            if (name == LanguageService.Instance["data_point"]) return 5;
            if (name == LanguageService.Instance["arrow"]) return 6;
            if (name == LanguageService.Instance["annotation"]) return 7;
            if (name == LanguageService.Instance["text"]) return 8;
            return 999; // 未知类别，默认放在最上层
        }

        /// <summary>
        /// 获取或创建指定名称的分类图层，并确保其处于正确的渲染顺序位置
        /// </summary>
        private CategoryLayerItemViewModel GetOrCreateCategory(string name)
        {
            var category = LayerTree.FirstOrDefault(c => c is CategoryLayerItemViewModel vm && vm.Name == name) as CategoryLayerItemViewModel;
            if (category != null) return category;

            category = new CategoryLayerItemViewModel(name);
            int newPriority = GetCategoryPriority(name);

            int insertIndex = LayerTree.Count;
            for (int i = 0; i < LayerTree.Count; i++)
            {
                if (LayerTree[i] is CategoryLayerItemViewModel existingCat)
                {
                    int existingPriority = GetCategoryPriority(existingCat.Name);
                    // 如果现有图层的优先级高于新图层，则插入在它前面
                    if (existingPriority > newPriority)
                    {
                        insertIndex = i;
                        break;
                    }
                }
            }

            // 确保插入索引有效
            if (insertIndex > LayerTree.Count) insertIndex = LayerTree.Count;

            LayerTree.Insert(insertIndex, category);
            return category;
        }

        #endregion


    }
}
