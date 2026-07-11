using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Controls;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Interfaces;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Models.SpiderDiagram;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using unvell.ReoGrid;

namespace GeoChemistryNexus.ViewModels
{
    public partial class MainPlotViewModel : ObservableObject, IRecipient<PickPointRequestMessage>, IRecipient<DefaultTreeExpandLevelChangedMessage>, IRecipient<ScriptValidatedMessage>, IRecipient<ScriptValidationRequestMessage>, IRecipient<DeveloperModeChangedMessage>, IRecipient<ObjectSelectionTriggerChangedMessage>, IRecipient<MouseSnapAutoRecognitionFrameRateChangedMessage>, IRecipient<OfficialTemplatesPublishedMessage>, IRecipient<TemplateCardLayoutChangedMessage>
    {
        // 拾取点模式
        [ObservableProperty]
        private bool _isPickingPointMode = false;

        // 待更新的 PointDefinition 对象
        private PointDefinition? _targetPointDefinition;

        // 用于鼠标命中测试的节流控制
        private long _lastHitTestTimeMs = 0;

        // 鼠标吸附/悬浮识别节流间隔，默认 24 FPS 以接近既有行为
        private long _hitTestIntervalMs = GetHitTestIntervalMs(24);

        // 坐标更新节流控制
        private long _lastCoordinateUpdateMs = 0;
        private const long CoordinateUpdateIntervalMs = 40; // 40ms update rate (~25fps)

        // 属性编辑器属性对象
        [ObservableProperty]
        private object? _propertyGridModel;

        private const int PropertyEditRefreshDelayMs = 180;
        private readonly System.Windows.Threading.DispatcherTimer _propertyEditRefreshTimer;
        private PropertyEditRefreshMode _pendingPropertyEditRefreshMode = PropertyEditRefreshMode.None;
        private bool _pendingPropertyEditUnsavedCheck;

        private const int LayerRefreshDelayMs = 50;
        private readonly System.Windows.Threading.DispatcherTimer _layerRefreshTimer;
        private bool _pendingLayerRefreshPreserveLimits = true;

        private List<Coordinates>? _cachedSnapDataPoints;
        private bool _isSnapDataPointsCacheValid;
        private long _lastSnapCheckTimeMs;
        private const long SnapCheckIntervalMs = 16;
        private Coordinates? _lastSnapSearchResult;
        private bool _lastSnapSearchHasResult;
        private readonly HashSet<int> _scriptInvalidRows = new();
        private readonly Dictionary<int, List<ScriptInvalidCellVisualSnapshot>> _scriptInvalidRowVisualSnapshots = new();

        private readonly Dictionary<ScottPlot.IPlottable, LayerItemViewModel> _plottableLayerLookup = new();

        private long _lastPreviewRefreshMs;
        private const long PreviewRefreshIntervalMs = 16;

        private bool _usePreparedCoordinateScript;

        private sealed class ScriptInvalidCellVisualSnapshot
        {
            public int Column { get; init; }
            public bool HasBackColor { get; init; }
            public System.Windows.Media.Color BackColor { get; init; }
            public RangeBorderStyle TopBorder { get; init; }
            public RangeBorderStyle RightBorder { get; init; }
            public RangeBorderStyle BottomBorder { get; init; }
            public RangeBorderStyle LeftBorder { get; init; }
        }

        private enum PropertyEditRefreshMode
        {
            None = 0,
            StyleOnly = 1,
            TemplateAppearanceOnly = 2,
            FullPreserveLimits = 3,
            FullResetLimits = 4
        }

        // 开发者模式
        [ObservableProperty]
        private bool _isDeveloperMode;

        public void Receive(OfficialTemplatesPublishedMessage message)
        {
            SyncTemplateCardPublishFlags();
        }

        public void Receive(DeveloperModeChangedMessage message)
        {
            IsDeveloperMode = message.Value;
            if (!string.IsNullOrEmpty(CurrentDiagramLanguage))
            {
                DiagramLanguageStatus = LanguageService.GetLanguageDisplayName(CurrentDiagramLanguage);
            }
        }

        public void Receive(ObjectSelectionTriggerChangedMessage message)
        {
            _isDoubleClickSelectionMode = message.Value == "DoubleClick";
        }

        public void Receive(MouseSnapAutoRecognitionFrameRateChangedMessage message)
        {
            ApplyMouseSnapAutoRecognitionFrameRate(message.Value);
        }

        public void Receive(TemplateCardLayoutChangedMessage message)
        {
            ApplyTemplateCardLayoutSettings(message.Value);
        }

        private static long GetHitTestIntervalMs(int frameRate)
        {
            int normalizedFrameRate = frameRate is 24 or 30 or 60 or 90 or 144 ? frameRate : 24;
            return Math.Max(1, (long)Math.Round(1000d / normalizedFrameRate));
        }

        private void ApplyMouseSnapAutoRecognitionFrameRate(int frameRate)
        {
            _hitTestIntervalMs = GetHitTestIntervalMs(frameRate);
            _lastHitTestTimeMs = 0;
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
                        if (entity.Content != null)
                            entity.FileHash = GraphMapTemplateService.ComputeTemplateContentHash(entity.Content);

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
                        card.IsNewTemplate = true;
                    }
                    else
                    {
                        // 如果 ID 恰好相同，就仅更新 IsCustom
                        entity.IsCustom = false;
                        entity.IsNewTemplate = true;
                        if (entity.Content != null)
                            entity.FileHash = GraphMapTemplateService.ComputeTemplateContentHash(entity.Content);
                        GraphMapDatabaseService.Instance.UpsertTemplate(entity);
                        card.IsCustomTemplate = false;
                        card.IsNewTemplate = true;
                    }

                    MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
                }
            }
            catch (Exception ex)
            {
                MessageHelper.Error(ex.Message);
            }
        }

        /// <summary>
        /// 收藏模板
        /// </summary>
        [RelayCommand]
        private async Task ToggleFavorite(TemplateCardViewModel card)
        {
            if (card == null || !card.TemplateId.HasValue) return;

            try
            {
                var entity = GraphMapDatabaseService.Instance.GetTemplate(card.TemplateId.Value);
                if (entity != null)
                {
                    // 切换收藏状态
                    entity.IsFavorite = !entity.IsFavorite;
                    GraphMapDatabaseService.Instance.UpsertTemplate(entity);

                    // 更新卡片状态
                    card.IsFavorite = entity.IsFavorite;

                    // 提示
                    string message = entity.IsFavorite 
                        ? LanguageService.Instance["favorite_added"] ?? "已添加到收藏"
                        : LanguageService.Instance["favorite_removed"] ?? "已从收藏中移除";
                    MessageHelper.Success(message);

                    // 如果当前在收藏列表中，刷新收藏列表
                    if (IsFavoriteExpanded)
                    {
                        LoadFavoriteTemplates();
                        InvalidateTemplateCardsCache();
                        // 重新加载卡片
                        await ShowCategoryTemplateCards(FavoriteTemplatesNode);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageHelper.Error(ex.Message);
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

            _propertyEditRefreshTimer = CreatePropertyEditRefreshTimer();
            _layerRefreshTimer = CreateLayerRefreshTimer();
            _propertyGridModel = nullObject;

            if (bool.TryParse(Helpers.ConfigHelper.GetConfig("developer_mode"), out bool devMode))
            {
                IsDeveloperMode = devMode;
            }

            LoadTemplateCardLayoutSettings();

            WpfPlot1 = new WpfPlot();
        }

        private void LoadTemplateCardLayoutSettings()
        {
            ApplyTemplateCardLayoutSettings(TemplateCardLayoutHelper.LoadFromConfig());
        }

        private void ApplyTemplateCardLayoutSettings(TemplateCardLayoutSettings settings)
        {
            TemplateCardSizePreset = settings.SizePreset;
        }

        public void Receive(DefaultTreeExpandLevelChangedMessage message)
        {
            // 处理默认展开层级变更消息，仅对官方图解节点生效
            if (OfficialTemplatesNode != null)
            {
                // 重新展开到指定层级
                ExpandNodes(OfficialTemplatesNode, 1, message.Value);
            }
        }

        public void Receive(ScriptValidatedMessage message)
        {
            if (message.Value)
            {
                // 0. 设置标志位，阻止 TreeView 自动选中节点
                _isBlockingTreeViewSelection = true;

                // 1. 清除数据表格（会触发选中事件，但此时数据点已清除）
                PrepareDataGridForInput();
                        
                // 2. 清除数据点图例
                ClearExistingPlottedData();
                
                // 3. 保存脚本面板状态，然后取消所有选中状态（防止选中坐标轴父类等对象）
                bool wasScriptPanelOpen = ScriptsPropertyGrid;
                CancelSelected();
                // 恢复脚本面板状态（不论验证是否成功，脚本面板保持原状态）
                if (wasScriptPanelOpen)
                {
                    ScriptsPropertyGrid = true;
                }
                        
                // 4. 隐藏数据点选中标记（防止表格选中事件触发错误显示）
                if (_selectedDataPointMarker != null)
                {
                    _selectedDataPointMarker.IsVisible = false;
                }
                if (_selectedDataPointLabel != null)
                {
                    _selectedDataPointLabel.IsVisible = false;
                }
                        
                // 5. 隐藏计算验证区域（因为数据已清空）
                IsCalculationVerificationVisible = false;
                CalculationResultSummary = string.Empty;
                CalculationLogs.Clear();

                ClearDataGridPlotRefreshPending();
                        
                // 6. 刷新绘图（使用 RefreshPlotFromLayers 确保图例被正确更新）
                RefreshPlotFromLayers();

                // 7. 重置标志位（使用异步延迟确保所有清除操作完成后再解除阻止）
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    _isBlockingTreeViewSelection = false;
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        /// <summary>
        /// 处理脚本验证请求消息
        /// </summary>
        public async void Receive(ScriptValidationRequestMessage message)
        {
            // 检查数据表格是否有数据
            bool hasData = false;
            if (_dataGrid != null && _dataGrid.Worksheets.Count > 0)
            {
                var sheet = _dataGrid.Worksheets[0];
                // 检查是否有非空行（排除表头行）
                for (int row = 0; row < sheet.RowCount; row++)
                {
                    bool rowHasData = false;
                    for (int col = 0; col < sheet.ColumnCount; col++)
                    {
                        var cellData = sheet[row, col];
                        if (cellData != null && !string.IsNullOrWhiteSpace(cellData.ToString()))
                        {
                            rowHasData = true;
                            break;
                        }
                    }
                    if (rowHasData)
                    {
                        hasData = true;
                        break;
                    }
                }
            }

            // 如果有数据，弹出确认对话框
            if (hasData)
            {
                var result = await NotificationManager.Instance.ShowDialogAsync(
                    LanguageService.Instance["information"],
                    LanguageService.Instance["confirm_clear_data_for_validation"],
                    LanguageService.Instance["Confirm"],
                    LanguageService.Instance["Cancel"]);

                if (!result)
                {
                    // 用户取消，不执行验证
                    return;
                }
            }

            // 执行验证（发送消息给 ScriptDefinitionControl 进行实际验证）
            WeakReferenceMessenger.Default.Send(new ScriptValidationExecuteMessage());
        }

        private void LanguageService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Item[]")
            {
                RequestTemplateLibraryRefresh(TemplateLibraryRefreshMode.ImmediateIfInTemplateMode);
                SyncSpiderDiagramLanguageWithApplication();
                RefreshSelectedCellDisplayForLanguageChange();
            }
        }

        private void RefreshSelectedCellDisplayForLanguageChange()
        {
            var worksheet = _dataGrid?.CurrentWorksheet;
            if (worksheet == null)
            {
                SelectedCellDisplayText = Lang("dataPrep_noCellSelected", "No cell selected");
                return;
            }

            UpdateSelectedCellDisplayText(worksheet.SelectionRange.Row, worksheet.SelectionRange.Col);
        }

        private static string Lang(string key, string fallback) =>
            LanguageService.Instance[key] ?? fallback;

        private void SyncSpiderDiagramLanguageWithApplication()
        {
            if (CurrentTemplate?.TemplateType != "Spider")
            {
                return;
            }

            string targetLanguage = ResolveInitialSpiderDiagramLanguage(CurrentTemplate);
            if (string.IsNullOrWhiteSpace(targetLanguage))
            {
                return;
            }

            CurrentTemplate.DefaultLanguage = targetLanguage;

            if (!string.Equals(CurrentDiagramLanguage, targetLanguage, StringComparison.OrdinalIgnoreCase))
            {
                CancelSelected();
                CurrentDiagramLanguage = targetLanguage;
                return;
            }

            SyncDiagramLanguageContext(targetLanguage);
            DiagramLanguageStatus = LanguageService.GetLanguageDisplayName(targetLanguage);

            AutoDetectFonts();
            CancelSelected();
            BuildLayerTreeFromTemplate(CurrentTemplate);
            RefreshPlotFromLayers(true);
        }

        /// <summary>
        /// 处理图解帮助RichTextBox内容改变事件
        /// </summary>
        private void RichTextBox_TextChanged(object? sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // 如果正在加载帮助文档，忽略此事件
            if (_isLoadingHelpDocument) return;

            // 只有在编辑模式下，且当前模板为自定义模板时，才检测未保存状态
            if (RibbonTabIndex == 2 && _hasConfirmedEditMode && _isCurrentTemplateCustom)
            {
                // 检查未保存状态
                CheckUnsavedChanges();
            }
        }

        // 模板列表绑定
        [ObservableProperty]
        private GraphMapTemplateNode _graphMapTemplateNode;

        // 三个主要分类节点
        [ObservableProperty]
        private GraphMapTemplateNode _personalTemplatesNode;

        [ObservableProperty]
        private GraphMapTemplateNode _favoriteTemplatesNode;

        [ObservableProperty]
        private GraphMapTemplateNode _officialTemplatesNode;

        // 最近使用节点
        [ObservableProperty]
        private GraphMapTemplateNode _recentsTemplatesNode;

        // 四个分类的展开状态
        [ObservableProperty]
        private bool _isPersonalExpanded = false;

        [ObservableProperty]
        private bool _isFavoriteExpanded = false;

        [ObservableProperty]
        private bool _isOfficialExpanded = true;

        [ObservableProperty]
        private bool _isRecentsExpanded = false;

        // 蛛网图相关属性
        [ObservableProperty]
        private bool _isSpiderDiagramMode = false;

        [ObservableProperty]
        private bool _isHarkerDiagramMode = false;

        [ObservableProperty]
        private SpiderDiagramViewModel _spiderDiagramViewModel = new();

        // 绑定到图层列表的数据源
        [ObservableProperty]
        private ObservableCollection<LayerItemViewModel> _layerTree = new ObservableCollection<LayerItemViewModel>();

        // 当前加载的、完整的模板数据
        [ObservableProperty]
        private GraphMapTemplate _currentTemplate;

        // 页面切换骨架加载状态
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLoadingOverlayVisible))]
        private bool _isTransitionLoading;

        // 数据投图骨架加载状态
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLoadingOverlayVisible))]
        private bool _isDataPlotLoading;

        [ObservableProperty]
        private bool _isEnteringPlotTransition;

        [ObservableProperty]
        private bool _isReturningTemplateLibraryTransition;

        private const int TransitionLoadingRenderLeadMs = 80;
        private const int TransitionLoadingMinimumVisibleMs = 320;
        private readonly Stopwatch _transitionLoadingStopwatch = new();
        private const int DataPlotLoadingRenderLeadMs = 80;
        private const int DataPlotLoadingMinimumVisibleMs = 320;
        private readonly Stopwatch _dataPlotLoadingStopwatch = new();

        public bool IsLoadingOverlayVisible => IsTransitionLoading || IsDataPlotLoading;

        // 全局变量-指示底图类型，方便三元图的坐标转换显示
        // 底图类型：笛卡尔坐标系(Cartesian)，三元坐标系(Ternary)
        public static string BaseMapType = String.Empty;

        // 全局变量-指示三元图是否是顺时针或者逆时针
        public static bool Clockwise = true;

        // 标记是否已经检查过更新（确保仅在第一次加载时检查）
        private static bool _hasCheckedUpdates = false;

        // 标记当前是否为自动检查更新（区分自动/手动，自动检查时已是最新版不弹窗）
        private bool _isAutoCheckingTemplateUpdate;

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

        // 用于绑定数据表格到绘图对象联动按钮的状态
        [ObservableProperty]
        private bool _isDataSelectionLinkEnabled = true;

        // 选中对象触发方式：SingleClick 或 DoubleClick
        private bool _isDoubleClickSelectionMode = false;

        // 计算过程验证区域相关属性
        /// <summary>
        /// 计算过程验证区域是否展开
        /// </summary>
        [ObservableProperty]
        private bool _isCalculationDetailExpanded = false;

        /// <summary>
        /// 计算验证区域的最大高度（数据表格高度的 1/4）
        /// </summary>
        [ObservableProperty]
        private double _dataGridMaxVerificationHeight = 200;

        /// <summary>
        /// 当前选中行的计算结果摘要(X = xxx, Y = xxx)
        /// </summary>
        [ObservableProperty]
        private string _calculationResultSummary = string.Empty;

        /// <summary>
        /// 当前数据表格选中单元格的显示文本。
        /// </summary>
        [ObservableProperty]
        private string _selectedCellDisplayText = string.Empty;

        /// <summary>
        /// 当前选中单元格地址，如 A1。
        /// </summary>
        [ObservableProperty]
        private string _selectedCellAddress = "--";

        /// <summary>
        /// 当前选中单元格内容，可在工具栏中直接编辑。
        /// </summary>
        [ObservableProperty]
        private string _selectedCellContent = string.Empty;

        /// <summary>
        /// 当前是否存在可编辑的选中单元格。
        /// </summary>
        [ObservableProperty]
        private bool _isSelectedCellEditable = false;

        private bool _isUpdatingSelectedCellEditor;

        /// <summary>
        /// 详细计算过程日志列表(图解脚本中的 trace 调用)
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _calculationLogs = new();

        /// <summary>
        /// 计算过程验证区域是否可见（二维坐标系和三元图均支持）
        /// </summary>
        [ObservableProperty]
        private bool _isCalculationVerificationVisible = false;

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

        // 模板卡片为空时的提示
        [ObservableProperty]
        private bool _isTemplateCardsEmpty;

        [ObservableProperty]
        private string _templateCardsEmptyHint = string.Empty;

        // 当前显示的模板路径（用于面包屑导航）
        [ObservableProperty]
        private string _currentTemplatePath = "";

        // 面包屑导航
        [ObservableProperty]
        private ObservableCollection<BreadcrumbItem> _breadcrumbs = new();

        // 当前选中的大类名称（用于顶部标题显示）
        [ObservableProperty]
        private string _currentCategoryName = string.Empty;

        // 搜索文本
        [ObservableProperty]
        private string _searchText = string.Empty;

        // 图解模板卡片布局：自适应大小档位（由设置页控制）
        [ObservableProperty]
        private TemplateCardSizePreset _templateCardSizePreset = TemplateCardSizePreset.Standard;

        // 批量下载按钮显示控制
        [ObservableProperty]
        private bool _isBatchDownloadButtonVisible = false;

        // 批量更新按钮显示控制
        [ObservableProperty]
        private bool _isBatchUpdateButtonVisible = false;

        // 批量下载/更新遮罩层显示控制
        [ObservableProperty]
        private bool _isBatchDownloadOverlayVisible = false;

        // 批量下载/更新遮罩层标题
        [ObservableProperty]
        private string _batchOverlayTitle = string.Empty;

        // 批量下载/更新进度信息
        [ObservableProperty]
        private string _batchDownloadProgressText = string.Empty;

        // 批量下载/更新取消令牌
        private CancellationTokenSource? _batchDownloadCts;

        // 当搜索文本变化时，重新应用过滤
        partial void OnSearchTextChanged(string value)
        {
            ApplyTemplateFilter();
        }

        partial void OnIsPersonalExpandedChanged(bool value) => UpdateTemplateCardsEmptyState();
        partial void OnIsFavoriteExpandedChanged(bool value) => UpdateTemplateCardsEmptyState();
        partial void OnIsOfficialExpandedChanged(bool value) => UpdateTemplateCardsEmptyState();
        partial void OnIsRecentsExpandedChanged(bool value) => UpdateTemplateCardsEmptyState();

        /// <summary>
        /// 模板卡片过滤方法
        /// </summary>
        private bool FilterTemplateCard(object item)
        {
            if (item is not TemplateCardViewModel card)
                return true;

            // 搜索文本过滤
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string searchLower = SearchText.ToLower();
                
                // 搜索模板名称
                if (!string.IsNullOrEmpty(card.Name) && card.Name.ToLower().Contains(searchLower))
                    return true;
                
                // 搜索模板分类
                if (!string.IsNullOrEmpty(card.Category) && card.Category.ToLower().Contains(searchLower))
                    return true;
                
                // 如果都不匹配，则过滤掉
                return false;
            }

            return true;
        }

        /// <summary>
        /// 应用模板过滤逻辑
        /// </summary>
        private void ApplyTemplateFilter()
        {
            if (TemplateCardsView != null)
            {
                TemplateCardsView.Refresh();
            }

            UpdateTemplateCardsEmptyState();
        }

        /// <summary>
        /// 更新模板卡片空状态提示
        /// </summary>
        private void UpdateTemplateCardsEmptyState()
        {
            if (_suppressTemplateCardsEmptyStateUpdates)
                return;

            var isEmpty = !IsSpiderDiagramMode
                          && !IsHarkerDiagramMode
                          && GetFilteredTemplateCardCount() == 0;

            IsTemplateCardsEmpty = isEmpty;
            TemplateCardsEmptyHint = isEmpty ? GetTemplateCardsEmptyHint() : string.Empty;
        }

        private int GetFilteredTemplateCardCount()
        {
            if (string.IsNullOrWhiteSpace(SearchText) || TemplateCardsView == null)
                return TemplateCards.Count;

            return TemplateCardsView.Cast<object>().Count();
        }

        private string GetTemplateCardsEmptyHint()
        {
            if (!string.IsNullOrWhiteSpace(SearchText))
                return LanguageService.Instance["diagram_hint_no_search_results"] ?? "未找到匹配的模板。";

            if (IsPersonalExpanded)
                return LanguageService.Instance["diagram_hint_no_personal"] ?? "暂无个人图解，可通过「文件」菜单新建或导入。";

            if (IsFavoriteExpanded)
                return LanguageService.Instance["diagram_hint_no_favorites"] ?? "暂无收藏的图解模板。";

            if (IsRecentsExpanded)
                return LanguageService.Instance["diagram_hint_no_recents"] ?? "暂无最近使用的图解模板。";

            if (IsOfficialExpanded)
                return LanguageService.Instance["diagram_hint_no_official"] ?? "暂无官方图解模板。";

            return LanguageService.Instance["diagram_hint_no_templates"] ?? "暂无图解模板。";
        }

        /// <summary>
        /// 清除搜索
        /// </summary>
        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty;
        }

        // 脚本属性面板显示
        [ObservableProperty]
        private bool _scriptsPropertyGrid = false;

        // 脚本对象
        [ObservableProperty]
        private ScriptDefinition _currentScript;

        // 三元坐标轴引用
        private ScottPlot.Plottables.TriangularAxis? _triangularAxis;

        // 追踪当前在TreeView中被选中的图层ViewModel
        private LayerItemViewModel _selectedLayer;

        // 空属性编辑对象占位
        private object nullObject = new EmptyPropertyModel();

        // 蛛网图属性定义缓存，模板模式下优先复用 CurrentTemplate.Info
        private Models.TitleDefinition? _spiderTitleDef;
        private Models.LegendDefinition? _spiderLegendDef;
        private Models.GridDefinition? _spiderGridDef;

        // 标记是否已经确认过进入编辑模式
        private bool _hasConfirmedEditMode = false;
        public bool HasConfirmedEditMode => _hasConfirmedEditMode;

        [ObservableProperty]
        private bool _isHelpDocReadOnly = true;

        private bool _isCurrentTemplateCustom = false;
        private string _originalTemplateJson = string.Empty;

        // 标记是否正在加载帮助文档，用于防止加载时触发未保存检测
        private bool _isLoadingHelpDocument = false;

        // 记录加载时的原始 RTF 内容，用于 IsHelpDocumentModified 比较
        // 避免因 RichTextBox 规范化 RTF 内容导致误判
        private string _originalHelpDocumentRtf = string.Empty;

        // 标记模板库是否需要刷新 (Dirty Flag)
        private bool _isTemplateLibraryDirty = true; // 默认为 true，确保首次加载

        private Task _initialLoadTask = Task.CompletedTask;

        /// <summary>
        /// 模板库刷新策略
        /// </summary>
        private enum TemplateLibraryRefreshMode
        {
            /// <summary>仅标记 dirty，返回模板库时再全量刷新</summary>
            Deferred,
            /// <summary>仅在模板浏览模式下立即刷新；绘图模式下只标记 dirty</summary>
            ImmediateIfInTemplateMode
        }

        /// <summary>
        /// 请求刷新模板库（统一入口）
        /// </summary>
        private void RequestTemplateLibraryRefresh(TemplateLibraryRefreshMode mode = TemplateLibraryRefreshMode.Deferred)
        {
            _isTemplateLibraryDirty = true;

            if (mode == TemplateLibraryRefreshMode.ImmediateIfInTemplateMode && IsTemplateMode)
            {
                _ = InitializeAsync();
            }
        }

        /// <summary>
        /// 模板数据变更后刷新：浏览模式下立即刷新，绘图模式下仅标记 dirty（不切换模式、不重置绘图）
        /// </summary>
        private async Task RefreshTemplateLibraryAfterDataChangeAsync()
        {
            _isTemplateLibraryDirty = true;

            if (!IsTemplateMode)
            {
                return;
            }

            SaveTemplateLibraryState();
            await InitializeAsync();
        }

        /// <summary>
        /// 返回模板库时按需刷新或恢复导航状态
        /// </summary>
        private async Task RefreshTemplateLibraryIfNeededAsync()
        {
            // 整个恢复过程抑制 TreeView 选中命令，避免延迟生成的容器触发重建并冲掉滚动位置。
            // 抑制由 BackToTemplateMode 在滚动恢复后再解除。
            _suppressTreeViewSelectionCommand = true;

            if (_isTemplateLibraryDirty)
            {
                // 从绘图模式返回时列表尚未布局完成，偏移为 0；
                // 不可再次 SaveTemplateLibraryState，否则会覆盖进入图解前保存的滚动位置。
                await InitializeAsync(captureCurrentState: false);
            }
            else
            {
                await RestoreTemplateLibraryState();
            }

            // 在过渡遮罩关闭前等待滚动恢复完成（View 侧会先隐藏列表，到位后再显示）
            await RequestRestoreTemplateCardsScrollAsync();
        }

        /// <summary>
        /// 请求 View 恢复模板卡片列表的滚动位置，并等待恢复完成（或放弃重试）
        /// </summary>
        private Task RequestRestoreTemplateCardsScrollAsync()
        {
            if (_lastSelectedCategory is "SpiderREE" or "SpiderTraceElement" or "Harker")
            {
                return Task.CompletedTask;
            }

            var restore = RestoreTemplateCardsScrollAsync;
            if (restore == null)
            {
                return Task.CompletedTask;
            }

            return restore(_lastSavedTemplateCardsScrollOffset);
        }

        // 记录模板库的上次选中状态（用于返回时恢复）
        private string _lastSelectedCategory = "Official"; // 默认为官方图解

        // 记录模板库面包屑导航位置（用于从绘图模式返回时恢复）
        private bool _lastWasAllTemplatesView = false;
        private Guid? _lastSavedNavigationTemplateId;
        private string _lastSavedNavigationGraphMapPath;
        private List<string> _lastSavedNavigationNodeNames = new();
        private double _lastSavedTemplateCardsScrollOffset;

        /// <summary>
        /// 程序化同步 TreeView 选中时抑制 SelectedItemChanged → SelectTreeViewItem，
        /// 避免返回模板库时用偏移 0 覆盖已保存的滚动位置并重复重建卡片。
        /// </summary>
        private bool _suppressTreeViewSelectionCommand;

        /// <summary>
        /// 当前卡片列表对应的缓存键；用于忽略 TreeView 延迟选中触发的重复加载。
        /// </summary>
        private string? _displayedTemplateCardsCacheKey;

        /// <summary>
        /// 由 View 注入：读取模板卡片列表当前垂直滚动偏移
        /// </summary>
        public Func<double>? GetTemplateCardsScrollOffset { get; set; }

        /// <summary>
        /// 由 View 注入：恢复模板卡片列表滚动位置，完成后返回
        /// </summary>
        public Func<double, Task>? RestoreTemplateCardsScrollAsync { get; set; }

        private Models.TitleDefinition GetOrCreateSpiderTitleDefinition()
        {
            if (CurrentTemplate?.Info != null)
            {
                CurrentTemplate.Info.Title ??= _spiderTitleDef ?? CreateSpiderTitleDefinitionFromPlot();
                return CurrentTemplate.Info.Title;
            }

            _spiderTitleDef ??= CreateSpiderTitleDefinitionFromPlot();
            return _spiderTitleDef;
        }

        private Models.LegendDefinition GetOrCreateSpiderLegendDefinition()
        {
            if (CurrentTemplate?.Info != null)
            {
                CurrentTemplate.Info.Legend ??= _spiderLegendDef ?? CreateSpiderLegendDefinitionFromPlot();
                return CurrentTemplate.Info.Legend;
            }

            _spiderLegendDef ??= CreateSpiderLegendDefinitionFromPlot();
            return _spiderLegendDef;
        }

        private Models.GridDefinition GetOrCreateSpiderGridDefinition()
        {
            if (CurrentTemplate?.Info != null)
            {
                CurrentTemplate.Info.Grid ??= _spiderGridDef ?? CreateSpiderGridDefinitionFromPlot();
                return CurrentTemplate.Info.Grid;
            }

            _spiderGridDef ??= CreateSpiderGridDefinitionFromPlot();
            return _spiderGridDef;
        }

        private Models.TitleDefinition CreateSpiderTitleDefinitionFromPlot()
        {
            var definition = new Models.TitleDefinition();
            if (WpfPlot1 == null)
            {
                return definition;
            }

            var titleLabel = new LocalizedString();
            titleLabel.Set(WpfPlot1.Plot.Axes.Title.Label.Text ?? string.Empty, languageCode: Services.LanguageService.CurrentLanguage);
            definition.Label = titleLabel;
            definition.Family = WpfPlot1.Plot.Axes.Title.Label.FontName;
            definition.Size = WpfPlot1.Plot.Axes.Title.Label.FontSize;
            definition.Color = WpfPlot1.Plot.Axes.Title.Label.ForeColor.ToHex();
            definition.IsBold = WpfPlot1.Plot.Axes.Title.Label.Bold;
            definition.IsItalic = WpfPlot1.Plot.Axes.Title.Label.Italic;
            return definition;
        }

        private Models.LegendDefinition CreateSpiderLegendDefinitionFromPlot()
        {
            var definition = new Models.LegendDefinition();
            if (WpfPlot1 == null)
            {
                return definition;
            }

            definition.IsVisible = WpfPlot1.Plot.Legend.IsVisible;
            definition.Alignment = WpfPlot1.Plot.Legend.Alignment;
            definition.Orientation = WpfPlot1.Plot.Legend.Orientation;
            definition.Font = WpfPlot1.Plot.Legend.FontName;
            return definition;
        }

        private Models.GridDefinition CreateSpiderGridDefinitionFromPlot()
        {
            var definition = new Models.GridDefinition
            {
                IsMinorGridSupported = true,
                IsAlternatingFillSupported = false
            };

            if (WpfPlot1 == null)
            {
                return definition;
            }

            var grid = WpfPlot1.Plot.Grid;
            definition.MajorGridLineIsVisible = grid.XAxisStyle.MajorLineStyle.IsVisible;
            definition.MajorGridLineColor = grid.MajorLineColor.ToHex();
            definition.MajorGridLineWidth = grid.XAxisStyle.MajorLineStyle.Width;
            definition.MajorGridLinePattern = Enum.TryParse<LineDefinition.LineType>(grid.XAxisStyle.MajorLineStyle.Pattern.ToString(), out var majorPattern)
                ? majorPattern
                : LineDefinition.LineType.Solid;
            definition.MajorGridLineAntiAlias = grid.XAxisStyle.MajorLineStyle.AntiAlias;
            definition.MinorGridLineIsVisible = grid.XAxisStyle.MinorLineStyle.IsVisible;
            definition.MinorGridLineColor = grid.XAxisStyle.MinorLineStyle.Color.ToHex();
            definition.MinorGridLineWidth = grid.XAxisStyle.MinorLineStyle.Width;
            definition.MinorGridLinePattern = Enum.TryParse<LineDefinition.LineType>(grid.XAxisStyle.MinorLineStyle.Pattern.ToString(), out var minorPattern)
                ? minorPattern
                : LineDefinition.LineType.Solid;
            definition.MinorGridLineAntiAlias = grid.XAxisStyle.MinorLineStyle.AntiAlias;
            return definition;
        }

        private void ConfigureGridDefinitionCapabilities(GraphMapTemplate? template)
        {
            if (template?.Info?.Grid == null)
            {
                return;
            }

            bool isTernary = template.TemplateType == "Ternary";
            bool isSpider = template.TemplateType == "Spider";

            template.Info.Grid.IsMinorGridSupported = !isTernary;
            template.Info.Grid.IsAlternatingFillSupported = !isTernary && !isSpider;
        }

        private void ApplyGridDefinitionToPlot(ScottPlot.Plot plot, Models.GridDefinition gridDef)
        {
            var grid = plot.Grid;

            grid.XAxisStyle.MajorLineStyle.IsVisible = gridDef.MajorGridLineIsVisible;
            grid.YAxisStyle.MajorLineStyle.IsVisible = gridDef.MajorGridLineIsVisible;
            grid.MajorLineColor = ScottPlot.Color.FromHex(
                GraphMapTemplateService.ConvertWpfHexToScottPlotHex(gridDef.MajorGridLineColor));
            grid.MajorLineWidth = gridDef.MajorGridLineWidth;
            grid.MajorLinePattern = GraphMapTemplateService.GetLinePattern(gridDef.MajorGridLinePattern.ToString());
            grid.XAxisStyle.MajorLineStyle.AntiAlias = gridDef.MajorGridLineAntiAlias;
            grid.YAxisStyle.MajorLineStyle.AntiAlias = gridDef.MajorGridLineAntiAlias;

            grid.XAxisStyle.MinorLineStyle.IsVisible = gridDef.MinorGridLineIsVisible;
            grid.YAxisStyle.MinorLineStyle.IsVisible = gridDef.MinorGridLineIsVisible;
            grid.MinorLineColor = ScottPlot.Color.FromHex(
                GraphMapTemplateService.ConvertWpfHexToScottPlotHex(gridDef.MinorGridLineColor));
            grid.MinorLineWidth = gridDef.MinorGridLineWidth;
            grid.XAxisStyle.MinorLineStyle.Pattern = GraphMapTemplateService.GetLinePattern(gridDef.MinorGridLinePattern.ToString());
            grid.YAxisStyle.MinorLineStyle.Pattern = GraphMapTemplateService.GetLinePattern(gridDef.MinorGridLinePattern.ToString());
            grid.XAxisStyle.MinorLineStyle.AntiAlias = gridDef.MinorGridLineAntiAlias;
            grid.YAxisStyle.MinorLineStyle.AntiAlias = gridDef.MinorGridLineAntiAlias;

            if (gridDef.IsAlternatingFillSupported && gridDef.GridAlternateFillingIsEnable)
            {
                grid.XAxisStyle.FillColor1 = ScottPlot.Color.FromHex(
                    GraphMapTemplateService.ConvertWpfHexToScottPlotHex(gridDef.GridFillColor1));
                grid.YAxisStyle.FillColor1 = ScottPlot.Color.FromHex(
                    GraphMapTemplateService.ConvertWpfHexToScottPlotHex(gridDef.GridFillColor1));
                grid.XAxisStyle.FillColor2 = ScottPlot.Color.FromHex(
                    GraphMapTemplateService.ConvertWpfHexToScottPlotHex(gridDef.GridFillColor2));
                grid.YAxisStyle.FillColor2 = ScottPlot.Color.FromHex(
                    GraphMapTemplateService.ConvertWpfHexToScottPlotHex(gridDef.GridFillColor2));
            }
            else
            {
                grid.XAxisStyle.FillColor1 = ScottPlot.Colors.Transparent;
                grid.YAxisStyle.FillColor1 = ScottPlot.Colors.Transparent;
                grid.XAxisStyle.FillColor2 = ScottPlot.Colors.Transparent;
                grid.YAxisStyle.FillColor2 = ScottPlot.Colors.Transparent;
            }

            grid.IsVisible = gridDef.MajorGridLineIsVisible || gridDef.MinorGridLineIsVisible;
        }

        private void ApplyTernaryGridDefinition(ScottPlot.Plottables.TriangularAxis? triangularAxis, Models.GridDefinition gridDef)
        {
            if (triangularAxis == null || gridDef == null)
            {
                return;
            }

            triangularAxis.GridLineStyle.IsVisible = gridDef.MajorGridLineIsVisible;
            triangularAxis.GridLineStyle.Color = ScottPlot.Color.FromHex(
                GraphMapTemplateService.ConvertWpfHexToScottPlotHex(gridDef.MajorGridLineColor));
            triangularAxis.GridLineStyle.Width = gridDef.MajorGridLineWidth;
            triangularAxis.GridLineStyle.Pattern = GraphMapTemplateService.GetLinePattern(gridDef.MajorGridLinePattern.ToString());
            triangularAxis.GridLineStyle.AntiAlias = gridDef.MajorGridLineAntiAlias;

            triangularAxis.FillStyle.Color = gridDef.GridAlternateFillingIsEnable
                ? ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(gridDef.GridFillColor1))
                : ScottPlot.Colors.Transparent;
        }

        private void ApplyTitleDefinitionToPlot(ScottPlot.Plot plot, Models.TitleDefinition? titleDefinition)
        {
            plot.Axes.Title.IsVisible = true;

            if (titleDefinition == null)
            {
                return;
            }

            plot.Axes.Title.Label.Text = titleDefinition.Label?.Get(DiagramLanguage) ?? string.Empty;
            plot.Axes.Title.Label.FontName = titleDefinition.Family;
            plot.Axes.Title.Label.FontSize = titleDefinition.Size;
            plot.Axes.Title.Label.ForeColor = ScottPlot.Color.FromHex(
                GraphMapTemplateService.ConvertWpfHexToScottPlotHex(titleDefinition.Color));
            plot.Axes.Title.Label.Bold = titleDefinition.IsBold;
            plot.Axes.Title.Label.Italic = titleDefinition.IsItalic;
        }

        private void ApplyLegendDefinitionToPlot(ScottPlot.Plot plot, Models.LegendDefinition? legendDefinition)
        {
            if (legendDefinition == null)
            {
                return;
            }

            plot.Legend.Alignment = legendDefinition.Alignment;
            plot.Legend.FontName = legendDefinition.Font;
            plot.Legend.Orientation = legendDefinition.Orientation;
            plot.Legend.IsVisible = legendDefinition.IsVisible;
        }

        private void ApplyCurrentTemplateAppearanceToPlot()
        {
            if (WpfPlot1 == null || CurrentTemplate?.Info == null)
            {
                return;
            }

            var plot = WpfPlot1.Plot;

            ApplyTitleDefinitionToPlot(plot, CurrentTemplate.Info.Title);
            ApplyLegendDefinitionToPlot(plot, CurrentTemplate.Info.Legend);

            if (CurrentTemplate.Info.Grid == null)
            {
                return;
            }

            if (CurrentTemplate.TemplateType == "Ternary")
            {
                ApplyTernaryGridDefinition(_triangularAxis, CurrentTemplate.Info.Grid);
                return;
            }

            ApplyGridDefinitionToPlot(plot, CurrentTemplate.Info.Grid);
        }

        private void SetHasConfirmedEditMode(bool value)
        {
            if (_hasConfirmedEditMode == value)
            {
                return;
            }

            _hasConfirmedEditMode = value;
            OnPropertyChanged(nameof(HasConfirmedEditMode));
        }

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

        /// <summary>
        /// 数据表格已修改但尚未重新投图时，在表格底部状态栏显示提示。
        /// </summary>
        [ObservableProperty]
        private bool _isDataGridPlotRefreshPending;

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
                    if (IsDeveloperMode)
                    {
                        // 开发者模式下首次进入编辑模式无需二次确认
                        SetHasConfirmedEditMode(true);
                        if (CurrentTemplate?.Script != null)
                        {
                            CurrentTemplate.Script.IsReadOnly = false;
                        }
                    }
                    else
                    {
                        // 强制通知 UI 属性值未改变，以恢复 RadioButton 的选中状态
                        OnPropertyChanged(nameof(RibbonTabIndex));

                        // 异步显示确认对话框
                        _ = ConfirmEditModeAsync();
                        return;
                    }
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
                
                // 通知编辑相关命令更新 CanExecute 状态
                AddLineCommand.NotifyCanExecuteChanged();
                AddTextCommand.NotifyCanExecuteChanged();
                AddPolygonCommand.NotifyCanExecuteChanged();
                AddArrowCommand.NotifyCanExecuteChanged();
                AddFunctionCommand.NotifyCanExecuteChanged();
                DeleteSelectedObjectCommand.NotifyCanExecuteChanged();
                HandlePageDeleteKeyCommand.NotifyCanExecuteChanged();
                SaveBaseMapCommand.NotifyCanExecuteChanged();
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
                SetHasConfirmedEditMode(true);
                RibbonTabIndex = 2;
                
                // 进入编辑状态，允许编辑脚本
                if (CurrentTemplate?.Script != null)
                {
                    CurrentTemplate.Script.IsReadOnly = false;
                }
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

        // 数据点坐标标签
        private ScottPlot.Plottables.Text? _selectedDataPointLabel;

        // 标志位：防止表格选择和绘图点击选择互相触发循环
        private bool _isSyncingSelection = false;

        // 标志位：阻止脚本验证清除数据后 TreeView 自动选中其他节点
        private bool _isBlockingTreeViewSelection = false;

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
            _propertyEditRefreshTimer = CreatePropertyEditRefreshTimer();
            _layerRefreshTimer = CreateLayerRefreshTimer();
            _propertyGridModel = nullObject;

            // 监听语言变化
            if (LanguageService.Instance != null)
            {
                LanguageService.Instance.PropertyChanged += LanguageService_PropertyChanged;
            }

            WpfPlot1 = wpfPlot;      // 获取绘图控件
            _richTextBox = richTextBox;      // 富文本框
            _dataGrid = dataGrid;        // 获取数据表格控件
            IsSnapSelectionEnabled = true;  // 吸附选择开启
            _isDoubleClickSelectionMode = ConfigHelper.GetConfig("object_selection_trigger") == "DoubleClick";
            if (int.TryParse(ConfigHelper.GetConfig("mouse_snap_auto_recognition_frame_rate"), out int snapFrameRate))
            {
                ApplyMouseSnapAutoRecognitionFrameRate(snapFrameRate);
            }
            else
            {
                ApplyMouseSnapAutoRecognitionFrameRate(24);
            }
            IsShowTemplateInfo = false;

            // 订阅帮助文档富文本框的内容改变事件
            if (_richTextBox != null)
            {
                _richTextBox.TextChanged += RichTextBox_TextChanged;
            }

            TemplateCardsView = CollectionViewSource.GetDefaultView(TemplateCards);
            TemplateCardsView.Filter = FilterTemplateCard;
            TemplateCards.CollectionChanged += (_, _) =>
            {
                if (!_suppressTemplateCardsEmptyStateUpdates)
                    UpdateTemplateCardsEmptyState();
            };

            // 异步初始化（页面 Loaded 时会等待此任务完成后再检查更新）
            _initialLoadTask = InitializeAsync();

            // 初始化吸附标记
            _snapMarker = WpfPlot1.Plot.Add.Marker(0, 0);
            _snapMarker.IsVisible = false;
            _snapMarker.Color = ScottPlot.Colors.Orange; // 使用醒目的橙色
            _snapMarker.Size = 15; // 稍微大一点
            _snapMarker.Shape = MarkerShape.OpenCircle;
            _snapMarker.LineWidth = 3; // 加粗

            // 初始化选中数据点标记
            _selectedDataPointMarker = WpfPlot1.Plot.Add.Marker(0, 0);
            _selectedDataPointMarker.IsVisible = false;
            _selectedDataPointMarker.Color = ScottPlot.Colors.Red; // 选中点使用红色
            _selectedDataPointMarker.Size = 20; // 比数据点大（数据点通常是10）
            _selectedDataPointMarker.Shape = MarkerShape.OpenCircle; // 空心圆圈
            _selectedDataPointMarker.LineWidth = 2; // 线宽

            // 初始化选中数据点坐标标签
            _selectedDataPointLabel = WpfPlot1.Plot.Add.Text("", 0, 0);
            _selectedDataPointLabel.IsVisible = false;
            _selectedDataPointLabel.LabelFontColor = ScottPlot.Colors.Red;
            _selectedDataPointLabel.LabelFontSize = 12;
            _selectedDataPointLabel.LabelBold = true; // 加粗显示
            _selectedDataPointLabel.LabelBackgroundColor = ScottPlot.Colors.Transparent; // 透明背景
            _selectedDataPointLabel.LabelBorderColor = ScottPlot.Colors.Transparent; // 透明边框
            _selectedDataPointLabel.LabelBorderWidth = 0; // 无边框
            _selectedDataPointLabel.LabelAlignment = Alignment.MiddleCenter; // 标签居中对齐
            _selectedDataPointLabel.OffsetY = -20; // 向上偏移20像素，确保在红色圆圈顶部

            // 订阅绘图控件的鼠标事件
            WpfPlot1.MouseEnter += WpfPlot1_MouseEnter;
            WpfPlot1.MouseLeave += WpfPlot1_MouseLeave;
            WpfPlot1.MouseMove += WpfPlot1_MouseMove;

            // 订阅线条绘制事件
            WpfPlot1.MouseUp += WpfPlot1_MouseUp;
            WpfPlot1.MouseRightButtonUp += WpfPlot1_MouseRightButtonUp;
            WpfPlot1.MouseDoubleClick += WpfPlot1_MouseDoubleClick;

            // 订阅数据表格行数自动扩充
            AttachDataGridWorksheetEvents(_dataGrid.CurrentWorksheet);

            WpfPlot1.Menu.Clear();      // 禁用原生右键菜单

            // 禁用双击帧率显示
            WpfPlot1.UserInputProcessor.DoubleLeftClickBenchmark(false);

            // 输出DPI信息用于调试
            var source = PresentationSource.FromVisual(WpfPlot1);
            double dpiScale = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            System.Diagnostics.Debug.WriteLine($"[MainPlotViewModel] DPI Scale Factor = {dpiScale}, DisplayScale = {WpfPlot1.DisplayScale}");

            // 注册消息接收
            WeakReferenceMessenger.Default.RegisterAll(this);

            // 读取开发者模式配置
            if (bool.TryParse(Helpers.ConfigHelper.GetConfig("developer_mode"), out bool devMode))
            {
                IsDeveloperMode = devMode;
            }

            // 读取图解卡片布局档位（紧凑/标准）
            LoadTemplateCardLayoutSettings();
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
            _ = CheckUpdatesIfNeededAsync();
        }

        /// <summary>
        /// 等待首次模板库加载完成后再检查更新，避免与 InitializeAsync 并发写库/读库
        /// </summary>
        public async Task CheckUpdatesIfNeededAsync()
        {
            if (_hasCheckedUpdates)
            {
                return;
            }

            _hasCheckedUpdates = true;

            try
            {
                await _initialLoadTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Initial template library load failed before update check: {ex.Message}");
            }

            if (bool.TryParse(ConfigHelper.GetConfig("auto_check_template_update"), out bool checkTemplate) && checkTemplate)
            {
                await AutoCheckForTemplateUpdates();
            }
        }

        /// <summary>
        /// 自动检查模板更新（已是最新版时不弹窗）
        /// </summary>
        private async Task AutoCheckForTemplateUpdates()
        {
            _isAutoCheckingTemplateUpdate = true;
            try
            {
                await CheckForTemplateUpdates();
            }
            finally
            {
                _isAutoCheckingTemplateUpdate = false;
            }
        }

        /// <summary>
        /// 在粘贴操作发生前处理的事件。
        /// 用于检查粘贴数据的行数并根据需要自动扩展表格。
        /// </summary>
        /// <param name="sender">事件发送者，即 Worksheet 对象</param>
        /// <param name="e">事件参数</param>
        private void CurrentWorksheet_BeforePaste(object? sender, unvell.ReoGrid.Events.BeforeRangeOperationEventArgs e)
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

            if (isLayerSelected && layerToRestore is SpiderSampleLayerItemViewModel selectedSpiderLayer)
            {
                selectedSpiderLayer.Restore();
                return;
            }

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

            // 如果之前有高亮的点，先取消高亮
            if (_targetPointDefinition != null && _targetPointDefinition != message.Value)
            {
                _targetPointDefinition.IsHighlighted = false;
            }

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
                // 跳过不可见对象
                if (!plottable.IsVisible)
                    continue;

                bool isHovered = false;

                // 根据不同的 Plottable 类型执行不同的命中测试逻辑
                switch (plottable)
                {
                    case ScottPlot.Plottables.Scatter scatter:
                        // 先做数据点吸附，再补充线段吸附（蛛网图是线+点，靠近线也应命中）
                        if (scatter is IGetNearest hittable)
                        {
                            DataPoint nearest = hittable.GetNearest(mouseCoordinates, WpfPlot1.Plot.LastRender, radius);
                            if (nearest.IsReal)
                            {
                                isHovered = true;
                            }
                        }

                        if (!isHovered && IsScatterLineHovered(scatter, mouseCoordinates, radius))
                        {
                            isHovered = true;
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
        /// 判断鼠标是否靠近 Scatter 的折线部分。
        /// </summary>
        private bool IsScatterLineHovered(ScottPlot.Plottables.Scatter scatter, Coordinates point, float radius)
        {
            if (scatter.LineWidth <= 0)
            {
                return false;
            }

            var dataSource = scatter.GetIDataSource();
            if (dataSource == null || dataSource.Length < 2)
            {
                return false;
            }

            Coordinates? previousPoint = null;

            for (int i = 0; i < dataSource.Length; i++)
            {
                var currentPoint = dataSource.GetCoordinateScaled(i);

                if (double.IsNaN(currentPoint.X) || double.IsNaN(currentPoint.Y) ||
                    double.IsInfinity(currentPoint.X) || double.IsInfinity(currentPoint.Y))
                {
                    previousPoint = null;
                    continue;
                }

                if (previousPoint.HasValue &&
                    GetDistanceToLineSegment(point, previousPoint.Value, currentPoint) <= radius)
                {
                    return true;
                }

                previousPoint = currentPoint;
            }

            return false;
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

            if (_plottableLayerLookup.TryGetValue(plottable, out var layer))
            {
                if (layer is SpiderSampleLayerItemViewModel spiderLayer)
                {
                    spiderLayer.SetActivePlottable(plottable);
                }

                return layer;
            }

            return null;
        }

        /// <summary>
        /// 根据当前模板的脚本要求，准备数据输入表格
        /// </summary>
        private void PrepareDataGridForInput()
        {
            var worksheet = _dataGrid.Worksheets[0];

            if (CurrentTemplate?.Script == null || string.IsNullOrEmpty(CurrentTemplate.Script.RequiredDataSeries))
            {
                MessageHelper.Warning(LanguageService.Instance["script_not_defined_in_template"]);
                worksheet.Reset(); // 清空表格
                RestoreWorksheetSelectionToSafeCell(worksheet);
                UpdateSelectedCellDisplayText(0, 0);
                return;
            }

            worksheet.Reset(); // 重置表格内容

            // 脚本数值列 + Category 分组列（Category 不参与脚本计算）
            var dataSeriesColumns = PlotDataGridHelper.ParseScriptDataColumns(CurrentTemplate.Script.RequiredDataSeries);
            if (dataSeriesColumns.Count == 0)
            {
                MessageHelper.Warning(LanguageService.Instance["script_not_defined_in_template"]);
                worksheet.Reset();
                RestoreWorksheetSelectionToSafeCell(worksheet);
                UpdateSelectedCellDisplayText(0, 0);
                return;
            }

            var requiredColumns = new List<string> { "Category" };
            requiredColumns.AddRange(dataSeriesColumns);

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

            RestoreWorksheetSelectionToSafeCell(worksheet);
            UpdateSelectedCellDisplayText(0, 0);
        }

        /// <summary>
        /// 在重置表格结构后恢复到合法的单元格选区，避免 ReoGrid 渲染旧选区时越界。
        /// </summary>
        private void RestoreWorksheetSelectionToSafeCell(Worksheet? worksheet, int row = 0, int col = 0)
        {
            if (worksheet == null)
                return;

            if (worksheet.RowCount <= 0)
                worksheet.RowCount = 1;

            if (worksheet.ColumnCount <= 0)
                worksheet.ColumnCount = 1;

            int safeRow = Math.Clamp(row, 0, worksheet.RowCount - 1);
            int safeCol = Math.Clamp(col, 0, worksheet.ColumnCount - 1);

            try
            {
                _isSyncingSelection = true;
                worksheet.SelectionRange = new RangePosition(safeRow, safeCol, 1, 1);
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        /// <summary>
        /// 鼠标左键抬起事件，用于确定绘图对象的起点和终点
        /// </summary>
        private void UpdateSelectedDataPointMarker(Coordinates location)
        {
            if (WpfPlot1 == null || _selectedDataPointMarker == null) return;

            if (BaseMapType == "Ternary")
            {
                HideSelectedDataPointMarker(refresh: true);
                return;
            }

            // 移除并重新添加，确保标记显示在最上层
            WpfPlot1.Plot.Remove(_selectedDataPointMarker);
            WpfPlot1.Plot.Add.Plottable(_selectedDataPointMarker);

            // 如果有坐标标签，也移除并重新添加
            if (_selectedDataPointLabel != null)
            {
                WpfPlot1.Plot.Remove(_selectedDataPointLabel);
                WpfPlot1.Plot.Add.Plottable(_selectedDataPointLabel);
            }

            // 将真实坐标转换为绘图坐标（处理对数轴）
            var renderLocation = PlotTransformHelper.ToRenderCoordinates(WpfPlot1.Plot, location);

            _selectedDataPointMarker.Location = renderLocation;
            _selectedDataPointMarker.IsVisible = true;

            // 更新坐标标签
            if (_selectedDataPointLabel != null)
            {
                _selectedDataPointLabel.LabelText = $"({location.X:F4}, {location.Y:F4})";
                _selectedDataPointLabel.Location = renderLocation;
                _selectedDataPointLabel.IsVisible = true;
            }

            WpfPlot1.Refresh();
        }

        private void HideSelectedDataPointMarker(bool refresh = false)
        {
            if (_selectedDataPointMarker != null)
            {
                _selectedDataPointMarker.IsVisible = false;
            }

            if (_selectedDataPointLabel != null)
            {
                _selectedDataPointLabel.IsVisible = false;
            }

            if (refresh)
            {
                WpfPlot1?.Refresh();
            }
        }

        private void AttachDataGridWorksheetEvents(Worksheet? worksheet)
        {
            if (worksheet == null)
            {
                return;
            }

            worksheet.BeforePaste += CurrentWorksheet_BeforePaste;
            worksheet.SelectionRangeChanged += CurrentWorksheet_SelectionRangeChanged;
            worksheet.CellDataChanged += CurrentWorksheet_CellDataChanged;
            worksheet.AfterCellKeyDown += CurrentWorksheet_AfterCellKeyDown;
            worksheet.RangeDataChanged += CurrentWorksheet_RangeDataChanged;
        }

        private void CurrentWorksheet_CellDataChanged(object? sender, unvell.ReoGrid.Events.CellEventArgs e)
        {
            // 禁用数据表格变更时的自动投图。
            // 仅保留手动执行投图命令的入口。
            UpdateSelectedCellDisplayText();
            MarkDataGridPlotRefreshPending();
        }

        private void CurrentWorksheet_AfterCellKeyDown(object? sender, unvell.ReoGrid.Events.AfterCellKeyDownEventArgs e)
        {
            if (e.KeyCode is unvell.ReoGrid.Interaction.KeyCode.Delete or unvell.ReoGrid.Interaction.KeyCode.Back)
            {
                MarkDataGridPlotRefreshPending();
            }
        }

        private void CurrentWorksheet_RangeDataChanged(object? sender, unvell.ReoGrid.Events.RangeEventArgs e)
        {
            MarkDataGridPlotRefreshPending();
        }

        private void CurrentWorksheet_SelectionRangeChanged(object? sender, unvell.ReoGrid.Events.RangeEventArgs e)
        {
            if (_isSyncingSelection) return;

            try
            {
                _isSyncingSelection = true;
                UpdateSelectedCellDisplayText(e.Range.Row, e.Range.Col);
                if (IsDataSelectionLinkEnabled)
                {
                    HighlightDataPointByRowIndex(e.Range.Row);
                }
                else if (_selectedDataPointMarker.IsVisible)
                {
                    _selectedDataPointMarker.IsVisible = false;
                    if (_selectedDataPointLabel != null)
                    {
                        _selectedDataPointLabel.IsVisible = false;
                    }

                    WpfPlot1.Refresh();
                }
                
                // 计算并显示当前选中行的计算结果
                CalculateAndDisplayResult(e.Range.Row);
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        private void UpdateSelectedCellDisplayText(int? row = null, int? col = null)
        {
            var worksheet = _dataGrid?.CurrentWorksheet;
            if (worksheet == null)
            {
                SelectedCellDisplayText = Lang("dataPrep_noCellSelected", "No cell selected");
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
                SelectedCellDisplayText = Lang("dataPrep_noCellSelected", "No cell selected");
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

            var cellAddress = $"{ColumnIndexToName(targetCol)}{targetRow + 1}";
            var cellValue = worksheet.GetCellData(targetRow, targetCol)?.ToString() ?? string.Empty;

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

            SelectedCellDisplayText = string.IsNullOrWhiteSpace(cellValue)
                ? string.Format(System.Globalization.CultureInfo.CurrentCulture, Lang("dataPrep_selectedCellEmpty", "{0}: <empty>"), cellAddress)
                : string.Format(System.Globalization.CultureInfo.CurrentCulture, Lang("dataPrep_selectedCellValue", "{0}: {1}"), cellAddress, cellValue);
        }

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

            var newValue = value ?? string.Empty;
            var currentValue = worksheet.GetCellData(targetRow, targetCol)?.ToString() ?? string.Empty;
            if (string.Equals(currentValue, newValue, StringComparison.Ordinal))
            {
                return;
            }

            worksheet[targetRow, targetCol] = newValue;
            MarkDataGridPlotRefreshPending();
        }

        private static string ColumnIndexToName(int columnIndex)
        {
            if (columnIndex < 0)
            {
                return "?";
            }

            var columnName = string.Empty;
            int currentIndex = columnIndex;

            do
            {
                columnName = (char)('A' + (currentIndex % 26)) + columnName;
                currentIndex = (currentIndex / 26) - 1;
            }
            while (currentIndex >= 0);

            return columnName;
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
                foreach (var layer in FlattenTree(LayerTree).OfType<SpiderSampleLayerItemViewModel>())
                {
                    if (!layer.IsVisible || !layer.ContainsSourceRowIndex(rowIndex))
                    {
                        continue;
                    }

                    layer.TrySetActiveByRowIndex(rowIndex);

                    if (_selectedDataPointMarker.IsVisible)
                    {
                        _selectedDataPointMarker.IsVisible = false;
                        if (_selectedDataPointLabel != null)
                        {
                            _selectedDataPointLabel.IsVisible = false;
                        }
                    }

                    if (SelectLayerCommand.CanExecute(layer))
                    {
                        SelectLayerCommand.Execute(layer);
                    }

                    layer.Highlight();
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
                    if (_selectedDataPointLabel != null)
                    {
                        _selectedDataPointLabel.IsVisible = false;
                    }
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
                if (_selectedDataPointLabel != null)
                {
                    _selectedDataPointLabel.IsVisible = false;
                }
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

        private void SelectReoGridRows(IReadOnlyList<int> rows)
        {
            if (_dataGrid == null || _dataGrid.Worksheets.Count == 0 || rows == null || rows.Count == 0)
                return;

            var sheet = _dataGrid.Worksheets[0];
            var validRows = rows
                .Where(row => row >= 0 && row < sheet.RowCount)
                .Distinct()
                .OrderBy(row => row)
                .ToList();

            if (validRows.Count == 0)
                return;

            if (validRows.Count == 1)
            {
                SelectReoGridRow(validRows[0]);
                return;
            }

            int startRow = validRows[0];
            int endRow = validRows[^1];
            bool isContinuous = validRows.Count == endRow - startRow + 1;

            sheet.SelectionRange = isContinuous
                ? new unvell.ReoGrid.RangePosition(startRow, 0, validRows.Count, sheet.ColumnCount)
                : new unvell.ReoGrid.RangePosition(startRow, 0, 1, sheet.ColumnCount);
        }

        private void SyncSpiderSelectionToDataGrid(SpiderSampleLayerItemViewModel spiderLayer)
        {
            if (_dataGrid == null || spiderLayer == null)
                return;

            var rowIndices = spiderLayer.GetSelectedRowIndices();
            if (rowIndices.Count == 0)
                return;

            try
            {
                _isSyncingSelection = true;
                SelectReoGridRows(rowIndices);
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        private void SyncLayerNameToDataGrid(IEnumerable<int> rowIndices, string layerName, params string[] candidateHeaders)
        {
            if (_dataGrid == null || rowIndices == null || _dataGrid.Worksheets.Count == 0)
                return;

            var worksheet = _dataGrid.Worksheets[0];
            int columnIndex = -1;

            for (int col = 0; col < worksheet.ColumnCount; col++)
            {
                var headerText = worksheet.ColumnHeaders[col]?.Text;
                if (candidateHeaders.Any(header => string.Equals(headerText, header, StringComparison.OrdinalIgnoreCase)))
                {
                    columnIndex = col;
                    break;
                }
            }

            if (columnIndex < 0)
                return;

            var validRowIndices = rowIndices
                .Where(rowIndex => rowIndex >= 0 && rowIndex < worksheet.RowCount)
                .Distinct()
                .ToList();

            if (validRowIndices.Count == 0)
                return;

            foreach (var rowIndex in validRowIndices)
            {
                worksheet[rowIndex, columnIndex] = layerName;
            }
        }

        private void SyncScatterNameToDataGrid(ScatterLayerItemViewModel scatterLayer, string scatterName)
        {
            if (scatterLayer == null)
                return;

            SyncLayerNameToDataGrid(
                scatterLayer.OriginalRowIndices,
                scatterName,
                "Category",
                "Sample");
        }

        private void SyncSpiderSampleNameToDataGrid(SpiderSampleLayerItemViewModel spiderLayer, string sampleName)
        {
            if (spiderLayer == null)
                return;

            var rowIndices = spiderLayer.Samples
                .SelectMany(sample => sample.SourceRowIndices)
                .ToList();

            SyncLayerNameToDataGrid(rowIndices, sampleName, "Category", "Sample");
        }

        private void WpfPlot1_MouseUp(object? sender, MouseButtonEventArgs e)
        {
            // 鼠标左键抬起事件
            if (e.ChangedButton != MouseButton.Left)
                return;

            // 获取DPI缩放系数（统一在方法开头获取，避免重复声明）
            var dpiSource = PresentationSource.FromVisual(WpfPlot1);
            double dpiScale = dpiSource?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;

            // 处理拾取点模式
            if (IsPickingPointMode)
            {
                var pickMousePos = e.GetPosition(WpfPlot1);
                Pixel pickMousePixel = new(pickMousePos.X * dpiScale, pickMousePos.Y * dpiScale);
                Coordinates pickMouseCoordinates = WpfPlot1.Plot.GetCoordinates(pickMousePixel);

                // 尝试吸附
                var snapPoint = GetSnapPoint(pickMousePixel);
                if (snapPoint.HasValue)
                {
                    pickMouseCoordinates = snapPoint.Value;
                }

                // 将绘图坐标转换为真实数据坐标（处理对数轴）
                var realCoordinates = PlotTransformHelper.ToRealDataCoordinates(WpfPlot1.Plot, pickMouseCoordinates);

                if (_targetPointDefinition != null)
                {
                    _targetPointDefinition.X = realCoordinates.X;
                    _targetPointDefinition.Y = realCoordinates.Y;
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
                Pixel pointSelectMousePixel = new(pointSelectMousePos.X * dpiScale, pointSelectMousePos.Y * dpiScale);
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

            if (!isDrawingOrAdding && !_isDoubleClickSelectionMode && IsSnapSelectionEnabled && _lastHoveredLayer != null)
            {
                if (SelectLayerCommand.CanExecute(_lastHoveredLayer))
                {
                    SelectLayerCommand.Execute(_lastHoveredLayer);
                }
                return;
            }

            // 检查是否点击了坐标轴
            if (!isDrawingOrAdding && !_isDoubleClickSelectionMode && _lastHoveredLayer == null && IsSnapSelectionEnabled)
            {
                var mousePosForAxis = e.GetPosition(WpfPlot1);
                Pixel mousePixelForAxis = new(mousePosForAxis.X * dpiScale, mousePosForAxis.Y * dpiScale);

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
            Pixel mousePixel = new(mousePos.X * dpiScale, mousePos.Y * dpiScale);
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
                    _tempPreviewPolygon.FillStyle.Color = ScottPlot.Colors.Transparent; // 预览时内部透明
                    _tempPreviewPolygon.LineStyle.Color = ScottPlot.Colors.Red;
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
                    lineLayer.RequestRefresh += OnLayerRequestRefreshPreserveLimits;

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
                string placeholder = contentString.Get(DiagramLanguage);


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
                    Family = ScottPlot.Fonts.Detect(placeholder),     // 自动字体
                    BackgroundColor = "#00FFFFFF",
                    BorderColor = "#00FFFFFF"
                };

                // 创建新的 TextLayerItemViewModel
                var textLayer = new TextLayerItemViewModel(newTextDef, textCategory.Children.Count, DiagramLanguage);
                textLayer.RequestRefresh += OnLayerRequestRefreshPreserveLimits;
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

                    // 统一存储笛卡尔坐标（与多边形、线条等保持一致）
                    // 对于三元图，鼠标坐标已经是笛卡尔坐标系的值，直接使用
                    // 对于笛卡尔图，需要从绘图坐标（Log）转换为真实数据坐标
                    if (BaseMapType == "Ternary")
                    {
                        // 三元图：直接使用笛卡尔坐标
                        finalStartPoint = new PointDefinition { X = startCoord.X, Y = startCoord.Y };
                        finalEndPoint = new PointDefinition { X = endCoord.X, Y = endCoord.Y };
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
                    arrowLayer.RequestRefresh += OnLayerRequestRefreshPreserveLimits;
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
        /// 鼠标双击事件，用于双击选中模式下选中绘图对象
        /// </summary>
        private void WpfPlot1_MouseDoubleClick(object? sender, MouseButtonEventArgs e)
        {
            if (!_isDoubleClickSelectionMode) return;
            if (e.ChangedButton != MouseButton.Left) return;

            // 获取DPI缩放系数
            var dpiSource = PresentationSource.FromVisual(WpfPlot1);
            double dpiScale = dpiSource?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;

            // 在添加/拾取模式下不触发选中
            bool isDrawingOrAdding = IsPickingPointMode || IsAddingLine || IsAddingArrow || IsAddingPolygon || IsAddingText;
            if (isDrawingOrAdding) return;

            // 双击选中图层对象
            if (IsSnapSelectionEnabled && _lastHoveredLayer != null)
            {
                if (SelectLayerCommand.CanExecute(_lastHoveredLayer))
                {
                    SelectLayerCommand.Execute(_lastHoveredLayer);
                }
                return;
            }

            // 双击选中坐标轴
            if (_lastHoveredLayer == null && IsSnapSelectionEnabled)
            {
                var mousePosForAxis = e.GetPosition(WpfPlot1);
                Pixel mousePixelForAxis = new(mousePosForAxis.X * dpiScale, mousePosForAxis.Y * dpiScale);

                if (BaseMapType == "Ternary")
                {
                    var ternaryLayout = WpfPlot1.Plot.RenderManager.LastRender.DataRect;

                    // 标题点击检测
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

                    double GetTernaryAxisHitScoreForDoubleClick(double dist, Pixel start, Pixel end, Pixel opposite, ScottPlot.TriangularAxisEdge? edge)
                    {
                        if (edge != null)
                        {
                            Pixel mid = new((start.X + end.X) / 2, (start.Y + end.Y) / 2);
                            Pixel labelPos = new(mid.X + edge.LabelStyle.OffsetX, mid.Y + edge.LabelStyle.OffsetY);
                            double distLabel = Math.Sqrt(Math.Pow(mousePixelForAxis.X - labelPos.X, 2) + Math.Pow(mousePixelForAxis.Y - labelPos.Y, 2));
                            if (distLabel < 50) return 0.1;
                        }
                        if (dist < 20) return dist;
                        double cpRef = (end.X - start.X) * (opposite.Y - start.Y) - (end.Y - start.Y) * (opposite.X - start.X);
                        double cpMouse = (end.X - start.X) * (mousePixelForAxis.Y - start.Y) - (end.Y - start.Y) * (mousePixelForAxis.X - start.X);
                        bool isOutside = Math.Sign(cpRef) != Math.Sign(cpMouse);
                        if (isOutside && dist < 60) return dist;
                        return double.MaxValue;
                    }

                    double distBottom = DistancePointToSegment(mousePixelForAxis, pA, pB);
                    double distLeft = DistancePointToSegment(mousePixelForAxis, pA, pC);
                    double distRight = DistancePointToSegment(mousePixelForAxis, pB, pC);

                    double scoreBottom = GetTernaryAxisHitScoreForDoubleClick(distBottom, pA, pB, pC, ternaryPlot?.Bottom);
                    double scoreLeft = GetTernaryAxisHitScoreForDoubleClick(distLeft, pA, pC, pB, ternaryPlot?.Left);
                    double scoreRight = GetTernaryAxisHitScoreForDoubleClick(distRight, pB, pC, pA, ternaryPlot?.Right);

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
                            CancelSelected();
                            PropertyGridModel = axisDef;
                            WpfPlot1.Refresh();
                            return;
                        }
                    }
                    return;
                }

                var layout = WpfPlot1.Plot.RenderManager.LastRender.DataRect;
                string? clickedAxis = null;

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
        
            // 处理多边形绘制完成或取消
            if (IsAddingPolygon)
            {
                // 第一层：如果已点击顶点，处理当前多边形
                if (_polygonVertices.Count > 0)
                {
                    // 清理预览用的“橡皮筋”线
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
                    
                    // 如果顶点数少于3，无法构成多边形，视为取消当前多边形
                    if (_polygonVertices.Count < 3)
                    {
                        _polygonVertices.Clear();
                        WpfPlot1.Refresh();
                        MessageHelper.Info(LanguageService.Instance["not_enough_vertices_add_polygon_canceled"]);
                        // 保持 IsAddingPolygon = true，继续添加模式
                        return;
                    }
                    
                    // 顶点数≥ 3，创建多边形
                    // 在图层树中找到 “多边形” 分类
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
                    polygonLayer.RequestRefresh += OnLayerRequestRefreshPreserveLimits;
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
                    // 保持 IsAddingPolygon = true，继续添加模式
                    return;
                }
                
                // 第二层：未点击顶点 + 有选中对象，右键取消选中（不退出添加模式）
                if (_selectedLayer != null || PropertyGridModel != null)
                {
                    ClearLayerSelection();  // 只清除选中状态，不影响添加模式
                    // 保持 IsAddingPolygon = true，继续添加模式
                    return;
                }
                
                // 第三层：未点击顶点 + 无选中对象，右键退出添加多边形模式
                IsAddingPolygon = false;
                WpfPlot1.Refresh();
                MessageHelper.Info(LanguageService.Instance["not_enough_vertices_add_polygon_canceled"]);
                return;
            }
        
            // 处理添加线条逻辑 - 优先于取消选择
            if (IsAddingLine)
            {
                // 第一层：如果已点击第一个点，右键取消当前线条对象的添加
                if (_lineStartPoint != null)
                {
                    if (_tempLinePlot != null)
                    {
                        WpfPlot1.Plot.Remove(_tempLinePlot);
                        _tempLinePlot = null;
                    }
                    _lineStartPoint = null;
                    WpfPlot1.Refresh();
                    // 保持 IsAddingLine = true，继续添加模式
                    return;
                }
                
                // 第二层：未点击点 + 有选中对象，右键取消选中（不退出添加模式）
                if (_selectedLayer != null || PropertyGridModel != null)
                {
                    ClearLayerSelection();  // 只清除选中状态，不影响添加模式
                    // 保持 IsAddingLine = true，继续添加模式
                    return;
                }
                
                // 第三层：未点击点 + 无选中对象，右键退出添加线条模式
                IsAddingLine = false;
                // 确保清除任何可能残留的临时线条
                if (_tempLinePlot != null)
                {
                    WpfPlot1.Plot.Remove(_tempLinePlot);
                    _tempLinePlot = null;
                }
                WpfPlot1.Refresh();
                MessageHelper.Info(LanguageService.Instance["add_line_operation_canceled"]);
                return;
            }
        
            // 取消添加文本
            if (IsAddingText)
            {
                // 第一层：有选中对象，右键取消选中（不退出添加模式）
                if (_selectedLayer != null || PropertyGridModel != null)
                {
                    ClearLayerSelection();  // 只清除选中状态，不影响添加模式
                    // 保持 IsAddingText = true，继续添加模式
                    return;
                }
                
                // 第二层：无选中对象，右键退出添加文本模式
                IsAddingText = false;
                MessageHelper.Info(LanguageService.Instance["add_text_operation_canceled"]);
                return;
            }
        
            // 取消添加箭头
            if (IsAddingArrow)
            {
                // 第一层：如果已点击第一个点，右键取消当前箭头对象的添加
                if (_arrowStartPoint != null)
                {
                    if (_tempArrowPlot != null)
                    {
                        WpfPlot1.Plot.Remove(_tempArrowPlot);
                        _tempArrowPlot = null;
                    }
                    _arrowStartPoint = null;
                    WpfPlot1.Refresh();
                    // 保持 IsAddingArrow = true，继续添加模式
                    return;
                }
                
                // 第二层：未点击点 + 有选中对象，右键取消选中（不退出添加模式）
                if (_selectedLayer != null || PropertyGridModel != null)
                {
                    ClearLayerSelection();  // 只清除选中状态，不影响添加模式
                    // 保持 IsAddingArrow = true，继续添加模式
                    return;
                }
                
                // 第三层：未点击点 + 无选中对象，右键退出添加箭头模式
                IsAddingArrow = false;
                // 确保清除任何可能残留的临时箭头
                if (_tempArrowPlot != null)
                {
                    WpfPlot1.Plot.Remove(_tempArrowPlot);
                    _tempArrowPlot = null;
                }
                WpfPlot1.Refresh();
                MessageHelper.Info(LanguageService.Instance["add_arrow_operation_canceled"]);
                return;
            }
        
            // 如果当前是高亮状态，或者属性面板打开，鼠标右键单击就是取消选择
            if (_selectedLayer != null || PropertyGridModel != null)
            {
                // 取消选择
                CancelSelected();
                return;
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
                    var result = await NotificationManager.Instance.ShowThreeButtonDialogAsync(
                        LanguageService.Instance["tips"] ?? "tips",
                        LanguageService.Instance["unsaved_diagram_template_prompt"],
                        LanguageService.Instance["Save"],
                        LanguageService.Instance["DontSave"],
                        LanguageService.Instance["Cancel"]);

                    if (result == 2) // Cancel
                    {
                        return;
                    }
                    else if (result == 0) // Save
                    {
                        await PerformSave();
                    }
                    // If result == 1 (Don't Save), proceed without saving
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
                    currentLang = DiagramLanguage.ContentLanguage ?? LanguageService.CurrentLanguage;
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

                    // 切换语言前先取消选中对象
                    CancelSelected();

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

        [RelayCommand(CanExecute = nameof(CanAddDrawingObject))]
        private void AddPolygon()
        {
            IsAddingPolygon = true;
        }
        
        /// <summary>
        /// "添加线条"按钮的命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanAddDrawingObject))]
        private void AddLine()
        {
            IsAddingLine = true;
        }
        
        /// <summary>
        /// "添加文本"按钮的命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanAddDrawingObject))]
        private void AddText()
        {
            IsAddingText = true;
        }
        
        /// <summary>
        /// 判断是否可以添加/删除绘图对象（只在编辑状态下可用）
        /// </summary>
        private bool CanAddDrawingObject() => RibbonTabIndex == 2;

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
            MarkDataGridPlotRefreshPending();
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
            MarkDataGridPlotRefreshPending();
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
                        MarkDataGridPlotRefreshPending();
                    }
                    return;
                }

                worksheet.DeleteRows(selection.Row, selection.Rows);
                MarkDataGridPlotRefreshPending();
            }
            catch (Exception ex)
            {
                // 无法删除行:
                MessageHelper.Warning(LanguageService.Instance["failed_to_delete_row"] + ex.Message);
            }
        }

        private System.Threading.CancellationTokenSource _initCts;

        public async Task InitializeAsync(string customJsonContent = null, bool captureCurrentState = true)
        {
            // 0. 取消上一次正在进行的初始化任务，避免并发冲突
            if (_initCts != null)
            {
                try { _initCts.Cancel(); } catch { }
                _initCts.Dispose();
            }
            _initCts = new System.Threading.CancellationTokenSource();
            var token = _initCts.Token;

            InvalidateTemplateCardsCache();

            // 计时开始
            //var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // 刷新前保存当前导航状态，避免重建树后恢复到其他分类的路径。
                // 从绘图模式返回时由调用方传入 captureCurrentState:false，保留进入图解前的滚动位置。
                if (IsTemplateMode && captureCurrentState)
                {
                    await Application.Current.Dispatcher.InvokeAsync(SaveTemplateLibraryState);
                }

                // 1. 异步加载模板列表
                await InitTemplateAsync(customJsonContent, token);

                if (token.IsCancellationRequested) return;

                // 绘图模式下仅同步树数据，避免全量重建卡片；返回模板库时再刷新视图
                if (!IsTemplateMode)
                {
                    return;
                }

                // 2. 清除 TreeView 选中并恢复导航（不在此重置面包屑，避免并发刷新时污染保存状态）
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    if (token.IsCancellationRequested) return;

                    if (GraphMapTemplateNode != null)
                    {
                        ClearTreeViewSelection(GraphMapTemplateNode);
                    }

                    await RestoreTemplateLibraryState();
                });

                if (token.IsCancellationRequested) return;

                // 3. 刷新视图
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (token.IsCancellationRequested) return;
                    TemplateCardsView.Refresh();
                    // 加载设置
                    LoadSettings();
                });

                // 4. 标记模板库为已刷新 (Clean)
                if (!token.IsCancellationRequested)
                {
                    _isTemplateLibraryDirty = false;
                    
                    // 5. 检查是否存在未安装/待更新的模板，更新批量操作按钮显示状态
                    UpdateBatchActionButtonsVisibility();
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
                        string localListPath = FileHelper.GetDataPath("PlotData", "GraphMapList.json");
                        if (File.Exists(localListPath))
                        {
                            string jsonContent = File.ReadAllText(localListPath);
                            var serverTemplates = JsonSerializer.Deserialize<List<GraphMapTemplateService.JsonTemplateItem>>(jsonContent);

                            if (serverTemplates != null)
                            {
                                var serverHashMap = new Dictionary<Guid, string>();
                                var serverVersionMap = new Dictionary<Guid, string>();
                                foreach (var item in serverTemplates)
                                {
                                    if (Guid.TryParse(item.ID, out Guid guid))
                                    {
                                        serverHashMap[guid] = item.FileHash;
                                        if (!string.IsNullOrWhiteSpace(item.Version))
                                            serverVersionMap[guid] = ContentVersionHelper.Normalize(item.Version);
                                    }
                                }

                                UpdateNodeMetadataFromServer(node, serverHashMap, serverVersionMap);
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

                    // 分离三个大类
                    SeparateTemplateCategories();

                    // 读取配置中的展开层级，默认为2
                    if (!int.TryParse(ConfigHelper.GetConfig("default_tree_expand_level"), out int expandLevel))
                    {
                        expandLevel = 2;
                    }

                    // 递归展开官方图解节点
                    if (OfficialTemplatesNode != null)
                    {
                        ExpandNodes(OfficialTemplatesNode, 1, expandLevel);
                    }
                });
            }
            catch
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
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
        /// 分离三个大类：个人图解、收藏、官方图解
        /// </summary>
        private void SeparateTemplateCategories()
        {
            if (GraphMapTemplateNode == null || GraphMapTemplateNode.Children == null)
                return;

            // 1. 个人图解：对应 "custom_templates" 或 "自定义模板" 节点
            string customRoot = LanguageService.Instance["custom_templates"];
            if (string.IsNullOrEmpty(customRoot)) customRoot = "Custom Templates";
            
            var personalNode = GraphMapTemplateNode.Children.FirstOrDefault(c => c.Name == customRoot);
            if (personalNode != null)
            {
                PersonalTemplatesNode = personalNode;
            }
            else
            {
                // 如果没有个人图解，创建一个空节点
                PersonalTemplatesNode = new GraphMapTemplateNode { Name = customRoot };
            }

            // 2. 收藏：从数据库加载 IsFavorite = true 的模板
            LoadFavoriteTemplates();

            // 3. 官方图解：所有非自定义模板
            OfficialTemplatesNode = new GraphMapTemplateNode 
            { 
                Name = LanguageService.Instance["official_templates"] ?? "Official Templates"
            };
            
            // 将非自定义的所有节点复制到官方节点
            var officialChildren = GraphMapTemplateNode.Children
                .Where(c => c != personalNode)
                .ToList();
            
            foreach (var child in officialChildren)
            {
                OfficialTemplatesNode.Children.Add(child);
            }

            // 4. 初始化当前大类名称（后续会在 RestoreTemplateLibraryState 中根据保存的状态覆盖）
            // CurrentCategoryName = OfficialTemplatesNode?.Name ?? LanguageService.Instance["official_templates"];
        }

        /// <summary>
        /// 加载收藏的模板
        /// </summary>
        private void LoadFavoriteTemplates()
        {
            FavoriteTemplatesNode = new GraphMapTemplateNode
            {
                Name = LanguageService.Instance["favorite_templates"] ?? "Favorites"
            };

            try
            {
                // 从数据库加载所有收藏的模板
                var allTemplates = GraphMapDatabaseService.Instance.GetSummaries();
                var favoriteTemplates = allTemplates.Where(t => t.IsFavorite).ToList();

                foreach (var template in favoriteTemplates)
                {
                    var node = new GraphMapTemplateNode
                    {
                        Name = template.Name,
                        GraphMapPath = template.GraphMapPath,
                        TemplateId = template.Id,
                        FileHash = template.FileHash,
                        IsCustomTemplate = template.IsCustom,
                        Status = template.Status,
                        Parent = FavoriteTemplatesNode
                    };
                    FavoriteTemplatesNode.Children.Add(node);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load favorite templates error: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载最近使用的模板
        /// </summary>
        private void LoadRecentsTemplates()
        {
            RecentsTemplatesNode = new GraphMapTemplateNode
            {
                Name = LanguageService.Instance["recents_templates"] ?? "Recents"
            };

            try
            {
                // 从配置文件加载最近使用的模板ID列表
                var recentsConfig = ConfigHelper.GetConfig("recent_templates");
                if (string.IsNullOrEmpty(recentsConfig))
                    return;

                var recentIds = recentsConfig.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => Guid.TryParse(id.Trim(), out var guid) ? guid : Guid.Empty)
                    .Where(id => id != Guid.Empty)
                    .Take(12) // 最多显示12个
                    .ToList();

                var allTemplates = GraphMapDatabaseService.Instance.GetSummaries();
                var templateDict = allTemplates.ToDictionary(t => t.Id);

                foreach (var id in recentIds)
                {
                    if (templateDict.TryGetValue(id, out var template))
                    {
                        var node = new GraphMapTemplateNode
                        {
                            Name = template.Name,
                            GraphMapPath = template.GraphMapPath,
                            TemplateId = template.Id,
                            FileHash = template.FileHash,
                            IsCustomTemplate = template.IsCustom,
                            Status = template.Status,
                            Parent = RecentsTemplatesNode
                        };
                        RecentsTemplatesNode.Children.Add(node);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load recents templates error: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录模板使用记录
        /// </summary>
        private void RecordTemplateUsage(Guid templateId)
        {
            try
            {
                var recentsConfig = ConfigHelper.GetConfig("recent_templates") ?? string.Empty;
                var recentIds = recentsConfig.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => id.Trim())
                    .ToList();

                var idString = templateId.ToString();
                
                // 如果已存在，先移除
                recentIds.Remove(idString);
                
                // 添加到最前面
                recentIds.Insert(0, idString);
                
                // 保留12条
                if (recentIds.Count > 12)
                {
                    recentIds = recentIds.Take(12).ToList();
                }
                
                // 保存
                var newConfig = string.Join(",", recentIds);
                ConfigHelper.SetConfig("recent_templates", newConfig);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Record template usage error: {ex.Message}");
            }
        }

        /// <summary>
        /// 切换分类展开状态，确保同时只有一个展开
        /// </summary>
        [RelayCommand]
        private async Task ToggleCategoryExpansion(string category)
        {
            // 离开蛛网图入口时，统一重置绘图状态，避免旧 plottable 残留
            if (IsSpiderDiagramMode || SpiderDiagramViewModel.IsSpiderPlotMode)
            {
                IsSpiderDiagramMode = false;
                SpiderDiagramViewModel.IsSpiderPlotMode = false;
                SpiderDiagramViewModel.Samples.Clear();
                _spiderTitleDef = null;
                _spiderLegendDef = null;
                _spiderGridDef = null;
                ResetPlotStateToDefault();
            }

            switch (category)
            {
                case "Personal":
                    IsPersonalExpanded = !IsPersonalExpanded;
                    if (IsPersonalExpanded)
                    {
                        IsFavoriteExpanded = false;
                        IsOfficialExpanded = false;
                        IsRecentsExpanded = false;
                        // 清除 TreeView 选中项
                        ClearTreeViewSelection(OfficialTemplatesNode);
                        ClearTreeViewSelection(PersonalTemplatesNode);
                        // 更新标题和导航栏
                        CurrentCategoryName = PersonalTemplatesNode?.Name ?? LanguageService.Instance["personal_templates"];
                        await ShowCategoryTemplateCards(PersonalTemplatesNode);
                        // 记录当前选中的分类
                        _lastSelectedCategory = "Personal";
                        SaveTemplateLibraryState();
                    }
                    break;
                case "Favorite":
                    IsFavoriteExpanded = !IsFavoriteExpanded;
                    if (IsFavoriteExpanded)
                    {
                        IsPersonalExpanded = false;
                        IsOfficialExpanded = false;
                        IsRecentsExpanded = false;
                        // 清除 TreeView 选中项
                        ClearTreeViewSelection(OfficialTemplatesNode);
                        ClearTreeViewSelection(PersonalTemplatesNode);
                        // 刷新收藏列表
                        LoadFavoriteTemplates();
                        // 更新标题和导航栏
                        CurrentCategoryName = FavoriteTemplatesNode?.Name ?? LanguageService.Instance["favorite_templates"];
                        // 显示收藏的模板卡片
                        await ShowCategoryTemplateCards(FavoriteTemplatesNode);
                        // 记录当前选中的分类
                        _lastSelectedCategory = "Favorite";
                        SaveTemplateLibraryState();
                    }
                    break;
                case "Official":
                    IsOfficialExpanded = !IsOfficialExpanded;
                    if (IsOfficialExpanded)
                    {
                        IsPersonalExpanded = false;
                        IsFavoriteExpanded = false;
                        IsRecentsExpanded = false;
                        // 清除 TreeView 选中项
                        ClearTreeViewSelection(OfficialTemplatesNode);
                        ClearTreeViewSelection(PersonalTemplatesNode);
                        // 更新标题和导航栏
                        CurrentCategoryName = OfficialTemplatesNode?.Name ?? LanguageService.Instance["official_templates"];
                        await ShowCategoryTemplateCards(OfficialTemplatesNode);
                        // 记录当前选中的分类
                        _lastSelectedCategory = "Official";
                        SaveTemplateLibraryState();
                    }
                    break;
                case "Recents":
                    IsRecentsExpanded = !IsRecentsExpanded;
                    if (IsRecentsExpanded)
                    {
                        IsPersonalExpanded = false;
                        IsFavoriteExpanded = false;
                        IsOfficialExpanded = false;
                        // 清除 TreeView 选中项
                        ClearTreeViewSelection(OfficialTemplatesNode);
                        ClearTreeViewSelection(PersonalTemplatesNode);
                        // 刷新最近使用列表
                        LoadRecentsTemplates();
                        // 更新标题和导航栏
                        CurrentCategoryName = RecentsTemplatesNode?.Name ?? LanguageService.Instance["recents_templates"];
                        // 显示最近使用的模板卡片
                        await ShowCategoryTemplateCards(RecentsTemplatesNode);
                        // 记录当前选中的分类
                        _lastSelectedCategory = "Recents";
                        SaveTemplateLibraryState();
                    }
                    break;
            }
        }

        /// <summary>
        /// 打开蛛网图工具
        /// </summary>
        [RelayCommand]
        private void OpenSpiderDiagram(string diagramType)
        {
            // 收起所有现有分类
            IsPersonalExpanded = false;
            IsFavoriteExpanded = false;
            IsOfficialExpanded = false;
            IsRecentsExpanded = false;
            IsHarkerDiagramMode = false;

            // 清除 TreeView 选中项
            ClearTreeViewSelection(OfficialTemplatesNode);
            ClearTreeViewSelection(PersonalTemplatesNode);

            // 重置图表：清除所有绘图对象
            WpfPlot1?.Plot.Clear();
            WpfPlot1?.Refresh();

            // 重置选中状态
            CancelSelected();
            ClearLayerTree();

            // 设置蛛网图模式
            IsSpiderDiagramMode = true;

            // 记录当前选中的分类
            _lastSelectedCategory = diagramType == "REE" ? "SpiderREE" : "SpiderTraceElement";

            // 取消旧事件订阅后重新初始化
            SpiderDiagramViewModel.ElementOrderChanged -= OnSpiderElementOrderChanged;
            SpiderDiagramViewModel.PlotSettingsChanged -= OnSpiderPlotSettingsChanged;
            SpiderDiagramViewModel.Initialize(diagramType);
            SpiderDiagramViewModel.ElementOrderChanged += OnSpiderElementOrderChanged;
            SpiderDiagramViewModel.PlotSettingsChanged += OnSpiderPlotSettingsChanged;

            // 使用模板系统创建内置蛛网图模板
            var spiderTemplate = SpiderTemplateFactory.GetSpiderTemplate(diagramType);
            spiderTemplate.DefaultLanguage = ResolveInitialSpiderDiagramLanguage(spiderTemplate);
            CurrentTemplate = spiderTemplate;
            _isCurrentTemplateCustom = true; // 内置模板可编辑

            // 更新标题
            CurrentCategoryName = diagramType == "REE"
                ? (LanguageService.Instance["ree_spider_diagram"] ?? "REE Spider Diagram")
                : (LanguageService.Instance["trace_element_spider_diagram"] ?? "Multi-Element Spider Diagram");
        }

        /// <summary>
        /// 直接打开哈克图解，复用现有笛卡尔模板渲染链路。
        /// </summary>
        [RelayCommand]
        private async Task OpenHarkerDiagram()
        {
            if (IsTransitionLoading)
            {
                return;
            }

            await BeginTransitionLoadingAsync(enteringPlot: true);
            try
            {
                SaveTemplateLibraryState();

                ClearDiagramLanguageContext();

                _currentTemplateId = null;
                _currentTemplateFilePath = string.Empty;
                _isCurrentTemplateCustom = false;
                UpdateHelpDocReadOnlyState();

                IsShowTemplateInfo = false;
                IsHarkerDiagramMode = true;
                IsSpiderDiagramMode = false;
                SpiderDiagramViewModel.IsSpiderPlotMode = false;
                SpiderDiagramViewModel.Samples.Clear();
                _spiderTitleDef = null;
                _spiderLegendDef = null;
                _spiderGridDef = null;

                WpfPlot1?.Plot.Clear();
                WpfPlot1?.Refresh();

                CancelSelected();
                ClearLayerTree();

                CurrentTemplate = ToolDiagramTemplateFactory.CreateHarkerTemplate();
                CurrentCategoryName = LanguageService.Instance["harker_diagram"] ?? "哈克图解";

                IsTemplateMode = false;
                IsPlotMode = true;
                RibbonTabIndex = 0;

                CurrentDiagramLanguage = ResolveInitialDiagramLanguage(CurrentTemplate);

                BuildLayerTreeFromTemplate(CurrentTemplate);
                RefreshPlotFromLayers();
                CenterPlot();
                ReloadHelpDocument(CurrentDiagramLanguage);
                PrepareDataGridForInput();

                _originalTemplateJson = SerializeTemplate(CurrentTemplate);
                _originalHelpDocumentRtf = RtfHelper.GetRtfString(_richTextBox);
                HasUnsavedChanges = false;
            }
            finally
            {
                await EndTransitionLoadingAsync();
            }
        }

        private string ResolveInitialDiagramLanguage(GraphMapTemplate template)
        {
            if (template == null)
                return AppCultureRegistry.DefaultContentLanguage;

            if (IsLanguageSupportedByTemplate(template, LanguageService.CurrentLanguage))
                return LanguageService.CurrentLanguage;

            if (!string.IsNullOrEmpty(template.DefaultLanguage))
                return template.DefaultLanguage;

            return AppCultureRegistry.ResolveDiagramDisplayLanguage(
                LanguageService.CurrentLanguage,
                CollectTemplateLanguages(template),
                template.DefaultLanguage);
        }

        private string ResolveInitialSpiderDiagramLanguage(GraphMapTemplate template)
        {
            return ResolveInitialDiagramLanguage(template);
        }

        private static HashSet<string> CollectTemplateLanguages(GraphMapTemplate template)
        {
            var languages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(template.DefaultLanguage))
                languages.Add(template.DefaultLanguage);

            if (template.Info?.Title?.Label?.Translations != null)
            {
                foreach (var key in template.Info.Title.Label.Translations.Keys)
                    languages.Add(key);
            }

            if (template.NodeList?.Translations != null)
            {
                foreach (var key in template.NodeList.Translations.Keys)
                    languages.Add(key);
            }

            return languages;
        }

        /// <summary>
        /// 蛛网图元素顺序/选择变更时的处理
        /// </summary>
        private void OnSpiderElementOrderChanged()
        {
            // 如果已在绘图模式，同步更新模板中的元素顺序
            if (SpiderDiagramViewModel.IsSpiderPlotMode && CurrentTemplate != null)
            {
                SyncSpiderTemplateSettingsFromViewModel();
                RefreshPlotFromLayers();
            }
        }

        /// <summary>
        /// 蛛网图标准化方案/归一化开关/数据源变化时的处理
        /// </summary>
        private void OnSpiderPlotSettingsChanged()
        {
            if (!SpiderDiagramViewModel.IsSpiderPlotMode || CurrentTemplate == null)
                return;

            SyncSpiderTemplateSettingsFromViewModel();
            RefreshPlotFromLayers();
        }

        /// <summary>
        /// 将蛛网图 ViewModel 中的配置同步回模板轴定义
        /// </summary>
        private void SyncSpiderTemplateSettingsFromViewModel()
        {
            if (CurrentTemplate?.Info?.Axes == null) return;

            string elementOrder = string.Join(",", SpiderDiagramViewModel.ElementOrder);
            string standardName = SpiderDiagramViewModel.SelectedStandard?.Name ?? string.Empty;

            foreach (var spiderAxis in CurrentTemplate.Info.Axes.OfType<SpiderAxisDefinition>())
            {
                bool shouldUpdateAxisLabel = UsesSpiderAutoLabel(spiderAxis);

                spiderAxis.SpiderType = SpiderDiagramViewModel.DiagramType;
                spiderAxis.ElementOrder = elementOrder;
                spiderAxis.NormalizationStandard = standardName;
                spiderAxis.IsNormalizationEnabled = SpiderDiagramViewModel.IsNormalizationEnabled;

                if (shouldUpdateAxisLabel)
                {
                    spiderAxis.Label = CreateSpiderAutoLabel(
                        spiderAxis.Type,
                        spiderAxis.SpiderType,
                        spiderAxis.NormalizationStandard,
                        spiderAxis.IsNormalizationEnabled);
                    spiderAxis.Family = ScottPlot.Fonts.Detect(spiderAxis.Label.Get(DiagramLanguage));
                }
            }
        }

        private bool UsesSpiderAutoLabel(SpiderAxisDefinition spiderAxis)
        {
            var autoLabel = CreateSpiderAutoLabel(
                spiderAxis.Type,
                spiderAxis.SpiderType,
                spiderAxis.NormalizationStandard,
                spiderAxis.IsNormalizationEnabled);
            string currentLabel = spiderAxis.Label.Get(DiagramLanguage);
            return autoLabel.Translations.Values.Contains(currentLabel);
        }

        private LocalizedString CreateSpiderAutoLabel(string axisType, string spiderType, string standardName, bool isNormalizationEnabled)
        {
            if (axisType == "Bottom")
            {
                return new LocalizedString
                {
                    Translations = new Dictionary<string, string>
                    {
                        { "en-US", "Elements" },
                        { "zh-CN", "元素" }
                    }
                };
            }

            string enLabel = "Concentration (ppm)";
            string zhLabel = "浓度 (ppm)";

            if (axisType == "Left" && isNormalizationEnabled)
            {
                var allStandards = spiderType == "REE"
                    ? NormalizationData.GetReeStandards()
                    : NormalizationData.GetTraceElementStandards();
                var standard = allStandards.FirstOrDefault(s => s.Name == standardName);
                string standardText = standard?.ShortName ?? standardName;

                enLabel = $"Sample / {(string.IsNullOrWhiteSpace(standardText) ? "Standard" : standardText)}";
                zhLabel = $"样品 / {(string.IsNullOrWhiteSpace(standardText) ? "标准" : standardText)}";
            }

            return new LocalizedString
            {
                Translations = new Dictionary<string, string>
                {
                    { "en-US", enLabel },
                    { "zh-CN", zhLabel }
                }
            };
        }

        /// <summary>
        /// 进入蛛网图绘图模式
        /// </summary>
        [RelayCommand]
        private async Task EnterSpiderPlotMode()
        {
            if (IsTransitionLoading) return;

            await BeginTransitionLoadingAsync(enteringPlot: true);
            try
            {
                // 切换到绘图模式，复用现有的 PlotMode 机制
                IsPlotMode = true;
                IsTemplateMode = false;
                RibbonTabIndex = 0; // 默认显示图层面板

                // 重置图表：清除所有绘图对象
                WpfPlot1?.Plot.Clear();
                WpfPlot1?.Refresh();

                // 重置选中状态
                CancelSelected();
                ClearLayerTree();

                // 设置蛛网图 ViewModel 的绘图模式
                SpiderDiagramViewModel.IsSpiderPlotMode = true;
                SpiderDiagramViewModel.SetPlotControl(WpfPlot1);

                // 首次进入绘图区前，将当前蛛网图设置同步回模板
                if (CurrentTemplate?.TemplateType == "Spider")
                {
                    SyncSpiderTemplateSettingsFromViewModel();
                }

                // 使用模板系统初始化绘图区域
                InitializeSpiderPlotAreaFromTemplate();
            }
            finally
            {
                await EndTransitionLoadingAsync();
            }
        }

        /// <summary>
        /// 使用模板系统初始化蛛网图绘图区域
        /// </summary>
        private void InitializeSpiderPlotAreaFromTemplate()
        {
            if (WpfPlot1 == null || CurrentTemplate == null) return;

            // 确保清空图表（防止残留旧的绘图对象）
            WpfPlot1.Plot.Clear();

            // 准备数据表格表头（根据元素列表）
            if (_dataGrid != null && SpiderDiagramViewModel != null)
            {
                var elements = SpiderDiagramViewModel.ElementOrder;
                var sheet = _dataGrid.CurrentWorksheet;
                sheet.Reset();

                // 设置列数
                sheet.ColumnCount = elements.Count + 1;

                // 使用 ColumnHeaders 设置表头
                sheet.ColumnHeaders[0].Text = "Category";
                for (int i = 0; i < elements.Count; i++)
                {
                    sheet.ColumnHeaders[i + 1].Text = elements[i];
                }

                // 设置列宽
                sheet.SetColumnsWidth(0, 1, 80);
                for (int i = 0; i < elements.Count; i++)
                {
                    sheet.SetColumnsWidth(i + 1, 1, 60);
                }

                RestoreWorksheetSelectionToSafeCell(sheet);
                UpdateSelectedCellDisplayText(0, 0);
            }

            // 使用模板系统渲染图表
            RefreshPlotFromLayers();

            // 构建图层树
            BuildLayerTreeFromTemplate(CurrentTemplate);
        }

        /// <summary>
        /// 初始化蛛网图绘图区域
        /// </summary>
        private void InitializeSpiderPlotArea()
        {
            if (WpfPlot1 == null) return;

            var plot = WpfPlot1.Plot;
            plot.Clear();

            // 设置基本样式
            plot.Axes.Title.Label.Text = SpiderDiagramViewModel.Title;
            plot.Axes.Left.Label.Text = SpiderDiagramViewModel.IsNormalizationEnabled
                ? "Sample / " + (SpiderDiagramViewModel.SelectedStandard?.ShortName ?? "Standard")
                : "Concentration (ppm)";
            plot.Axes.Bottom.Label.Text = "Elements";

            // 预设 X 轴元素名称刻度标签
            var elements = SpiderDiagramViewModel.ElementOrder;
            if (elements.Count > 0)
            {
                // 启用标准化时按方案过滤，禁用时使用全部已选元素
                var validElements = SpiderDiagramViewModel.IsNormalizationEnabled
                    ? (SpiderDiagramViewModel.SelectedStandard != null
                        ? elements.Where(e => SpiderDiagramViewModel.SelectedStandard.Values.ContainsKey(e)).ToList()
                        : elements.ToList())
                    : elements.ToList();

                if (validElements.Count > 0)
                {
                    double[] xPositions = Enumerable.Range(1, validElements.Count).Select(i => (double)i).ToArray();
                    var customTicks = new ScottPlot.TickGenerators.NumericManual();
                    for (int i = 0; i < xPositions.Length; i++)
                    {
                        customTicks.AddMajor(xPositions[i], validElements[i]);
                    }
                    plot.Axes.Bottom.TickGenerator = customTicks;

                    // 预设 Y 轴对数刻度格式
                    var tickGen = new ScottPlot.TickGenerators.NumericAutomatic();
                    tickGen.MinorTickGenerator = new ScottPlot.TickGenerators.LogMinorTickGenerator();
                    tickGen.IntegerTicksOnly = true;
                    tickGen.LabelFormatter = y =>
                    {
                        double val = Math.Pow(10, y);
                        return val.ToString("G10");
                    };
                    plot.Axes.Left.TickGenerator = tickGen;

                    // 预设坐标轴范围（Log10 空间：-2 = 0.01, 4 = 10000）
                    plot.Axes.SetLimits(
                        left: 0.5,
                        right: validElements.Count + 0.5,
                        bottom: -2,
                        top: 4
                    );
                }
            }

            // 准备数据表格表头（根据元素列表）
            if (_dataGrid != null)
            {
                var sheet = _dataGrid.CurrentWorksheet;
                sheet.Reset();

                // 设置列数
                sheet.ColumnCount = elements.Count + 1;

                // 使用 ColumnHeaders 设置表头（与 PrepareDataGridForInput 一致）
                sheet.ColumnHeaders[0].Text = "Category";
                for (int i = 0; i < elements.Count; i++)
                {
                    sheet.ColumnHeaders[i + 1].Text = elements[i];
                }

                // 设置列宽
                sheet.SetColumnsWidth(0, 1, 80);
                for (int i = 0; i < elements.Count; i++)
                {
                    sheet.SetColumnsWidth(i + 1, 1, 60);
                }

                RestoreWorksheetSelectionToSafeCell(sheet);
                UpdateSelectedCellDisplayText(0, 0);
            }

            WpfPlot1.Refresh();

            // 构建蛛网图的图层树（显示坐标轴信息）
            BuildSpiderLayerTree();
        }

        /// <summary>
        /// 构建蛛网图模式下的图层树
        /// </summary>
        private void BuildSpiderLayerTree()
        {
            ClearLayerTree();

            // 如果当前有模板，使用模板系统构建图层
            if (CurrentTemplate != null && CurrentTemplate.TemplateType == "Spider")
            {
                // 1. 先构建坐标轴图层
                BuildLayerTreeFromTemplate(CurrentTemplate);

                // 2. 渲染蜘蛛图（这会添加数据图层到 LayerTree）
                RenderSpiderPlot();
                return;
            }

            // 旧代码：为非模板蛛网图构建图层（兼容性）
            // 1. 坐标轴分类
            var axesCategory = new CategoryLayerItemViewModel(LanguageService.Instance["axes"] ?? "Axes", LayerTreeIconKind.Axis);

            var xAxisNode = new CategoryLayerItemViewModel("[X] Elements", LayerTreeIconKind.AxisX);
            if (WpfPlot1 != null)
            {
                xAxisNode.Tag = new SpiderAxisPropertyModel(WpfPlot1.Plot.Axes.Bottom, WpfPlot1);
            }
            axesCategory.Children.Add(xAxisNode);

            var yLabel = SpiderDiagramViewModel.IsNormalizationEnabled
                ? (SpiderDiagramViewModel.SelectedStandard != null
                    ? $"[Y] Sample / {SpiderDiagramViewModel.SelectedStandard.ShortName}"
                    : "[Y] Sample / Standard")
                : "[Y] Concentration (ppm)";
            var yAxisNode = new CategoryLayerItemViewModel(yLabel, LayerTreeIconKind.AxisY);
            if (WpfPlot1 != null)
            {
                yAxisNode.Tag = new SpiderAxisPropertyModel(WpfPlot1.Plot.Axes.Left, WpfPlot1);
            }
            axesCategory.Children.Add(yAxisNode);

            LayerTree.Add(axesCategory);

            // 2. 数据图层分类（仅当有样品数据时）
            if (SpiderDiagramViewModel.Samples.Count > 0 && WpfPlot1 != null)
            {
                var dataCategory = new CategoryLayerItemViewModel(LanguageService.Instance["Data"] ?? "Data", LayerTreeIconKind.Line);

                // 获取绘图中的所有 Scatter plottable
                var scatterPlottables = WpfPlot1.Plot.GetPlottables()
                    .OfType<ScottPlot.Plottables.Scatter>()
                    .ToList();

                // 分离主 scatter（LegendText 为空，有实际数据）和图例代理（LegendText 非空）
                var mainScatters = scatterPlottables.Where(s => string.IsNullOrEmpty(s.LegendText)).ToList();

                for (int i = 0; i < SpiderDiagramViewModel.Samples.Count && i < mainScatters.Count; i++)
                {
                    var sample = SpiderDiagramViewModel.Samples[i];
                    var matchingScatter = mainScatters[i];

                    // 通过 LegendText 匹配图例代理
                    var legendProxy = scatterPlottables
                        .FirstOrDefault(s => s.LegendText == sample.Name);

                    // 创建支持高亮的图层项
                    var sampleLayer = new SpiderSampleLayerItemViewModel(sample, matchingScatter, legendProxy ?? matchingScatter, WpfPlot1);
                    // 设置 Tag 为属性模型，用于属性面板显示
                    sampleLayer.Tag = sampleLayer.PropertyModel;
                    sampleLayer.SampleNameChanged += (layer, sampleName) => SyncSpiderSampleNameToDataGrid(layer, sampleName);

                    // 可见性切换事件
                    var scatter = matchingScatter;
                    var proxy = legendProxy;
                    sampleLayer.RequestRefresh += (s, e) =>
                    {
                        if (s is LayerItemViewModel node)
                        {
                            scatter.IsVisible = node.IsVisible;
                            if (proxy != null) proxy.IsVisible = node.IsVisible;
                            WpfPlot1?.Refresh();
                        }
                    };

                    dataCategory.Children.Add(sampleLayer);
                }

                LayerTree.Add(dataCategory);
            }
        }

        /// <summary>
        /// 保存模板库的当前选中状态
        /// </summary>
        private void SaveTemplateLibraryState()
        {
            // 根据当前展开的分类，保存状态
            if (IsSpiderDiagramMode)
            {
                _lastSelectedCategory = SpiderDiagramViewModel.DiagramType == "REE" ? "SpiderREE" : "SpiderTraceElement";
            }
            else if (IsHarkerDiagramMode)
            {
                _lastSelectedCategory = "Harker";
            }
            else if (IsPersonalExpanded)
            {
                _lastSelectedCategory = "Personal";
            }
            else if (IsFavoriteExpanded)
            {
                _lastSelectedCategory = "Favorite";
            }
            else if (IsOfficialExpanded)
            {
                _lastSelectedCategory = "Official";
            }
            else if (IsRecentsExpanded)
            {
                _lastSelectedCategory = "Recents";
            }

            var isSidebarCategoryActive = IsPersonalExpanded || IsFavoriteExpanded || IsOfficialExpanded || IsRecentsExpanded;

            // 保存面包屑导航位置，便于从绘图模式返回时还原
            var lastNavNode = Breadcrumbs.LastOrDefault(b => b.Node != null)?.Node;
            _lastSavedNavigationTemplateId = lastNavNode?.TemplateId;
            _lastSavedNavigationGraphMapPath = lastNavNode?.GraphMapPath;
            _lastSavedNavigationNodeNames = Breadcrumbs
                .Where(b => b.Node != null)
                .Select(b => b.Node.Name)
                .ToList();

            // 侧栏已选中某大类但面包屑尚未同步（如刷新过程中被重置）时，用大类根节点补全导航
            if (isSidebarCategoryActive && _lastSavedNavigationNodeNames.Count == 0)
            {
                var categoryNode = GetCategoryNodeForLastSelection();
                if (categoryNode != null)
                {
                    _lastSavedNavigationNodeNames = new List<string> { categoryNode.Name };
                }
            }

            _lastWasAllTemplatesView = !isSidebarCategoryActive &&
                (Breadcrumbs.Count <= 1 || Breadcrumbs.All(b => b.Node == null));

            // 仅在模板浏览模式下更新滚动偏移；绘图模式下保留进入前保存的值
            if (IsTemplateMode && !IsPlotMode)
            {
                _lastSavedTemplateCardsScrollOffset = GetTemplateCardsScrollOffset?.Invoke() ?? 0;
            }
        }

        private GraphMapTemplateNode? GetCategoryNodeForLastSelection()
        {
            return _lastSelectedCategory switch
            {
                "Personal" => PersonalTemplatesNode,
                "Official" => OfficialTemplatesNode,
                "Favorite" => FavoriteTemplatesNode,
                "Recents" => RecentsTemplatesNode,
                _ => null
            };
        }

        private static bool IsTopLevelCategoryNode(GraphMapTemplateNode? node, GraphMapTemplateNode? personalNode, GraphMapTemplateNode? officialNode, GraphMapTemplateNode? favoriteNode, GraphMapTemplateNode? recentsNode)
        {
            return node == personalNode || node == officialNode || node == favoriteNode || node == recentsNode;
        }

        /// <summary>
        /// 恢复模板库的上次选中状态
        /// </summary>
        private async Task RestoreTemplateLibraryState()
        {
            // 根据保存的状态，恢复对应的分类展开和卡片显示
            switch (_lastSelectedCategory)
            {
                case "SpiderREE":
                case "SpiderTraceElement":
                    IsPersonalExpanded = false;
                    IsFavoriteExpanded = false;
                    IsOfficialExpanded = false;
                    IsRecentsExpanded = false;
                    IsHarkerDiagramMode = false;

                    // 重置图表：清除所有绘图对象
                    WpfPlot1?.Plot.Clear();
                    WpfPlot1?.Refresh();

                    // 重置选中状态
                    CancelSelected();
                    ClearLayerTree();

                    // 恢复蛛网图模式
                    var spiderType = _lastSelectedCategory == "SpiderREE" ? "REE" : "TraceElement";
                    IsSpiderDiagramMode = true;
                    SpiderDiagramViewModel.ElementOrderChanged -= OnSpiderElementOrderChanged;
                    SpiderDiagramViewModel.PlotSettingsChanged -= OnSpiderPlotSettingsChanged;
                    SpiderDiagramViewModel.Initialize(spiderType);
                    SpiderDiagramViewModel.ElementOrderChanged += OnSpiderElementOrderChanged;
                    SpiderDiagramViewModel.PlotSettingsChanged += OnSpiderPlotSettingsChanged;
                    CurrentTemplate = SpiderTemplateFactory.GetSpiderTemplate(spiderType);
                    CurrentTemplate.DefaultLanguage = ResolveInitialSpiderDiagramLanguage(CurrentTemplate);
                    _isCurrentTemplateCustom = true;
                    CurrentCategoryName = spiderType == "REE"
                        ? (LanguageService.Instance["ree_spider_diagram"] ?? "REE Spider Diagram")
                        : (LanguageService.Instance["trace_element_spider_diagram"] ?? "Multi-Element Spider Diagram");
                    break;
                case "Harker":
                    IsPersonalExpanded = false;
                    IsFavoriteExpanded = false;
                    IsOfficialExpanded = false;
                    IsRecentsExpanded = false;
                    IsSpiderDiagramMode = false;
                    IsHarkerDiagramMode = true;
                    CurrentCategoryName = LanguageService.Instance["harker_diagram"] ?? "哈克图解";
                    break;
                case "Personal":
                    IsHarkerDiagramMode = false;
                    IsPersonalExpanded = true;
                    IsFavoriteExpanded = false;
                    IsOfficialExpanded = false;
                    IsRecentsExpanded = false;
                    await RestoreTemplateLibraryNavigationAsync(PersonalTemplatesNode);
                    break;
                case "Favorite":
                    IsHarkerDiagramMode = false;
                    IsPersonalExpanded = false;
                    IsFavoriteExpanded = true;
                    IsOfficialExpanded = false;
                    IsRecentsExpanded = false;
                    LoadFavoriteTemplates();
                    await RestoreTemplateLibraryNavigationAsync(FavoriteTemplatesNode);
                    break;
                case "Recents":
                    IsHarkerDiagramMode = false;
                    IsPersonalExpanded = false;
                    IsFavoriteExpanded = false;
                    IsOfficialExpanded = false;
                    IsRecentsExpanded = true;
                    LoadRecentsTemplates();
                    await RestoreTemplateLibraryNavigationAsync(RecentsTemplatesNode);
                    break;
                case "Official":
                default:
                    // 默认显示官方图解
                    IsHarkerDiagramMode = false;
                    IsPersonalExpanded = false;
                    IsFavoriteExpanded = false;
                    IsOfficialExpanded = true;
                    IsRecentsExpanded = false;
                    await RestoreTemplateLibraryNavigationAsync(OfficialTemplatesNode);
                    break;
            }
        }

        /// <summary>
        /// 恢复模板库面包屑导航位置
        /// </summary>
        private async Task RestoreTemplateLibraryNavigationAsync(GraphMapTemplateNode fallbackNode)
        {
            if (_lastWasAllTemplatesView &&
                !IsTopLevelCategoryNode(fallbackNode, PersonalTemplatesNode, OfficialTemplatesNode, FavoriteTemplatesNode, RecentsTemplatesNode))
            {
                await LoadAllTemplateCardsAsync(default);
                InitializeBreadcrumbs();
                CurrentCategoryName = string.Empty;
                if (GraphMapTemplateNode != null)
                {
                    ClearTreeViewSelection(GraphMapTemplateNode);
                }
                return;
            }

            var nodeToRestore = FindTemplateLibraryNode(
                _lastSavedNavigationTemplateId,
                _lastSavedNavigationGraphMapPath,
                _lastSavedNavigationNodeNames) ?? fallbackNode;

            if (!NodeBelongsToSelectedCategory(nodeToRestore))
            {
                nodeToRestore = fallbackNode;
            }

            if (!string.IsNullOrEmpty(nodeToRestore.GraphMapPath))
            {
                await ShowSingleTemplateCard(nodeToRestore);
            }
            else
            {
                await ShowCategoryTemplateCards(nodeToRestore);
            }

            SyncTreeViewSelection(nodeToRestore);
        }

        /// <summary>
        /// 判断节点是否属于当前选中的模板库大类
        /// </summary>
        private bool NodeBelongsToSelectedCategory(GraphMapTemplateNode? node)
        {
            if (node == null)
            {
                return false;
            }

            return _lastSelectedCategory switch
            {
                "Personal" => IsUnderPersonalTemplates(node),
                "Official" => IsUnderOfficialTemplates(node),
                "Favorite" => node == FavoriteTemplatesNode || IsDescendantOf(FavoriteTemplatesNode, node),
                "Recents" => node == RecentsTemplatesNode || IsDescendantOf(RecentsTemplatesNode, node),
                _ => true
            };
        }

        private bool IsUnderPersonalTemplates(GraphMapTemplateNode node)
        {
            if (node == PersonalTemplatesNode)
            {
                return true;
            }

            var current = node;
            while (current != null)
            {
                if (current == PersonalTemplatesNode)
                {
                    return true;
                }

                current = current.Parent;
            }

            return false;
        }

        private bool IsUnderOfficialTemplates(GraphMapTemplateNode node)
        {
            if (node == OfficialTemplatesNode)
            {
                return true;
            }

            if (OfficialTemplatesNode?.Children == null)
            {
                return false;
            }

            foreach (var child in OfficialTemplatesNode.Children)
            {
                if (node == child || IsDescendantOf(child, node))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsDescendantOf(GraphMapTemplateNode? ancestor, GraphMapTemplateNode target)
        {
            if (ancestor == null)
            {
                return false;
            }

            if (target == ancestor)
            {
                return true;
            }

            foreach (var child in ancestor.Children)
            {
                if (IsDescendantOf(child, target))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 在模板树中查找之前保存的导航节点（支持树重建后通过 ID/路径/名称定位）
        /// </summary>
        private GraphMapTemplateNode? FindTemplateLibraryNode(
            Guid? templateId,
            string? graphMapPath,
            IReadOnlyList<string>? namePath)
        {
            var searchRoots = new[]
            {
                GraphMapTemplateNode,
                OfficialTemplatesNode,
                PersonalTemplatesNode,
                FavoriteTemplatesNode,
                RecentsTemplatesNode
            };

            if (templateId.HasValue)
            {
                foreach (var root in searchRoots)
                {
                    var found = FindNodeByTemplateId(root, templateId.Value);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }

            if (!string.IsNullOrEmpty(graphMapPath))
            {
                foreach (var root in searchRoots)
                {
                    var found = FindNodeByGraphMapPath(root, graphMapPath);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }

            return FindNodeByNamePath(namePath);
        }

        private static GraphMapTemplateNode? FindNodeByTemplateId(GraphMapTemplateNode? node, Guid templateId)
        {
            if (node == null)
            {
                return null;
            }

            if (node.TemplateId == templateId)
            {
                return node;
            }

            foreach (var child in node.Children)
            {
                var found = FindNodeByTemplateId(child, templateId);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static GraphMapTemplateNode? FindNodeByGraphMapPath(GraphMapTemplateNode? node, string graphMapPath)
        {
            if (node == null)
            {
                return null;
            }

            if (string.Equals(node.GraphMapPath, graphMapPath, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            foreach (var child in node.Children)
            {
                var found = FindNodeByGraphMapPath(child, graphMapPath);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private GraphMapTemplateNode? FindNodeByNamePath(IReadOnlyList<string>? namePath)
        {
            if (namePath == null || namePath.Count == 0)
            {
                return null;
            }

            var current = ResolveTopLevelCategoryNode(namePath[0]);
            if (current == null)
            {
                return null;
            }

            if (namePath.Count == 1)
            {
                return current;
            }

            for (int i = 1; i < namePath.Count; i++)
            {
                var child = current.Children?.FirstOrDefault(c => c.Name == namePath[i]);
                if (child == null)
                {
                    return null;
                }

                current = child;
            }

            return current;
        }

        private GraphMapTemplateNode? ResolveTopLevelCategoryNode(string name)
        {
            if (OfficialTemplatesNode?.Name == name)
            {
                return OfficialTemplatesNode;
            }

            if (PersonalTemplatesNode?.Name == name)
            {
                return PersonalTemplatesNode;
            }

            if (FavoriteTemplatesNode?.Name == name)
            {
                return FavoriteTemplatesNode;
            }

            if (RecentsTemplatesNode?.Name == name)
            {
                return RecentsTemplatesNode;
            }

            return GraphMapTemplateNode?.Children?.FirstOrDefault(c => c.Name == name);
        }

        /// <summary>
        /// 递归更新节点的服务器清单元数据（哈希、版本）
        /// </summary>
        private void UpdateNodeMetadataFromServer(
            GraphMapTemplateNode node,
            Dictionary<Guid, string> serverHashes,
            Dictionary<Guid, string> serverVersions)
        {
            if (node == null) return;

            if (node.TemplateId.HasValue && !node.IsCustomTemplate)
            {
                if (serverHashes.TryGetValue(node.TemplateId.Value, out string hash))
                    node.FileHash = hash;

                if (serverVersions.TryGetValue(node.TemplateId.Value, out string version))
                    node.ServerVersion = version;
            }

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                    UpdateNodeMetadataFromServer(child, serverHashes, serverVersions);
            }
        }

        /// <summary>
        /// 当鼠标进入绘图区域时调用
        /// </summary>
        private void WpfPlot1_MouseEnter(object? sender, MouseEventArgs e)
        {
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
            var visibleLayers = FlattenTree(LayerTree).Where(l => l.IsVisible);
            List<(Coordinates pt, bool isHighlighted)> pointsToShow = new List<(Coordinates, bool)>();

            foreach (var layer in visibleLayers)
            {
                if (layer is LineLayerItemViewModel lineLayer)
                {
                    // 从 LineDefinition 获取坐标，而不是 Plottable，以确保获取最新的坐标值
                    if (lineLayer.LineDefinition?.Start != null)
                    {
                        var pt = PlotTransformHelper.ToRenderCoordinates(WpfPlot1.Plot, lineLayer.LineDefinition.Start.X, lineLayer.LineDefinition.Start.Y);
                        pointsToShow.Add((pt, lineLayer.LineDefinition.Start.IsHighlighted));
                    }
                    if (lineLayer.LineDefinition?.End != null)
                    {
                        var pt = PlotTransformHelper.ToRenderCoordinates(WpfPlot1.Plot, lineLayer.LineDefinition.End.X, lineLayer.LineDefinition.End.Y);
                        pointsToShow.Add((pt, lineLayer.LineDefinition.End.IsHighlighted));
                    }
                }
                else if (layer is ArrowLayerItemViewModel arrowLayer)
                {
                    // 从 ArrowDefinition 获取坐标，而不是 Plottable
                    if (arrowLayer.ArrowDefinition?.Start != null)
                    {
                        var pt = PlotTransformHelper.ToRenderCoordinates(WpfPlot1.Plot, arrowLayer.ArrowDefinition.Start.X, arrowLayer.ArrowDefinition.Start.Y);
                        pointsToShow.Add((pt, arrowLayer.ArrowDefinition.Start.IsHighlighted));
                    }
                    if (arrowLayer.ArrowDefinition?.End != null)
                    {
                        var pt = PlotTransformHelper.ToRenderCoordinates(WpfPlot1.Plot, arrowLayer.ArrowDefinition.End.X, arrowLayer.ArrowDefinition.End.Y);
                        pointsToShow.Add((pt, arrowLayer.ArrowDefinition.End.IsHighlighted));
                    }
                }
                else if (layer is PolygonLayerItemViewModel polygonLayer)
                {
                    if (polygonLayer.PolygonDefinition?.Vertices != null)
                    {
                        foreach (var v in polygonLayer.PolygonDefinition.Vertices)
                        {
                            var pt = PlotTransformHelper.ToRenderCoordinates(WpfPlot1.Plot, v.X, v.Y);
                            pointsToShow.Add((pt, v.IsHighlighted));
                        }
                    }
                }
                else if (layer is TextLayerItemViewModel textLayer)
                {
                    if (textLayer.TextDefinition?.StartAndEnd != null)
                    {
                        var pt = PlotTransformHelper.ToRenderCoordinates(WpfPlot1.Plot, textLayer.TextDefinition.StartAndEnd.X, textLayer.TextDefinition.StartAndEnd.Y);
                        pointsToShow.Add((pt, textLayer.TextDefinition.StartAndEnd.IsHighlighted));
                    }
                }
            }

            // 为每个端点添加圆圈标记，高亮端点显示红色，非高亮端点显示灰色
            foreach (var (pt, isHighlighted) in pointsToShow)
            {
                var marker = WpfPlot1.Plot.Add.Marker(pt);
                marker.Shape = MarkerShape.OpenCircle;
                
                if (isHighlighted)
                {
                    // 高亮端点：红色
                    marker.Color = ScottPlot.Colors.Red;
                    marker.Size = 10;
                    marker.LineWidth = 2;
                }
                else
                {
                    // 普通端点：灰色
                    marker.Color = ScottPlot.Colors.Gray.WithAlpha(150);
                    marker.Size = 6;
                    marker.LineWidth = 1;
                }

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
        /// 注意：所有的计算都基于 Render Coordinates (绘图坐标)，即线性化后的坐标。
        /// 对于对数轴，这意味着是 Log(Value)。
        /// </summary>
        /// <param name="mousePixel">鼠标像素位置</param>
        /// <param name="snapDistancePixels">吸附距离阈值</param>
        /// <returns>吸附点的坐标 (Render Coordinates)，如果没有吸附则返回 null</returns>
        private Coordinates? GetSnapPoint(Pixel mousePixel, double snapDistancePixels = 10)
        {
            if (WpfPlot1?.Plot == null)
            {
                return null;
            }

            Coordinates? bestSnap = null;
            double minDistanceSq = snapDistancePixels * snapDistancePixels;

            foreach (var dataPoint in GetSnapDataPoints())
            {
                var pt = PlotTransformHelper.ToRenderCoordinates(WpfPlot1.Plot, dataPoint);
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

            return bestSnap;
        }

        [ObservableProperty]
        private string _diagramLanguageStatus = "";

        [ObservableProperty]
        private string _currentDiagramLanguage = "";

        public ContentLanguageContext DiagramLanguage { get; } = new();

        public bool IsDiagramLanguageStatusVisible => CurrentTemplate?.TemplateType != "Spider";

        private void SyncDiagramLanguageContext(string? language)
        {
            DiagramLanguage.ContentLanguage = language;
            ContentLanguageScope.Instance.Active = string.IsNullOrEmpty(language) ? null : DiagramLanguage;
        }

        private void ClearDiagramLanguageContext()
        {
            SyncDiagramLanguageContext(null);
        }
        public bool IsScriptSettingButtonVisible => !IsSpiderDiagramMode && !IsHarkerDiagramMode;

        partial void OnIsSpiderDiagramModeChanged(bool value)
        {
            OnPropertyChanged(nameof(IsScriptSettingButtonVisible));
            UpdateTemplateCardsEmptyState();
        }

        partial void OnIsHarkerDiagramModeChanged(bool value)
        {
            OnPropertyChanged(nameof(IsScriptSettingButtonVisible));
        }

        private void AutoDetectFonts()
        {
            if (CurrentTemplate?.Info == null) return;

            // Title
            if (CurrentTemplate.Info.Title != null)
            {
                CurrentTemplate.Info.Title.Family = ScottPlot.Fonts.Detect(CurrentTemplate.Info.Title.Label.Get(DiagramLanguage));
            }

            // Legend
            if (CurrentTemplate.Info.Legend != null)
            {
                string sampleText = CurrentTemplate.Info.Title?.Label.Get(DiagramLanguage) ?? "Legend";
                CurrentTemplate.Info.Legend.Font = ScottPlot.Fonts.Detect(sampleText);
            }

            // Axes
            if (CurrentTemplate.Info.Axes != null)
            {
                foreach (var axis in CurrentTemplate.Info.Axes)
                {
                    axis.Family = ScottPlot.Fonts.Detect(axis.Label.Get(DiagramLanguage));
                }
            }

            // Texts
            if (CurrentTemplate.Info.Texts != null)
            {
                foreach (var text in CurrentTemplate.Info.Texts)
                {
                    text.Family = ScottPlot.Fonts.Detect(text.Content.Get(DiagramLanguage));
                }
            }
        }

        private void AutoDetectSpiderFonts()
        {
            if (CurrentTemplate?.TemplateType != "Spider" || CurrentTemplate.Info == null) return;

            if (CurrentTemplate.Info.Title != null)
            {
                CurrentTemplate.Info.Title.Family = ScottPlot.Fonts.Detect(CurrentTemplate.Info.Title.Label.Get(DiagramLanguage));
            }

            if (CurrentTemplate.Info.Axes != null)
            {
                foreach (var axis in CurrentTemplate.Info.Axes.OfType<SpiderAxisDefinition>())
                {
                    axis.Family = ScottPlot.Fonts.Detect(axis.Label.Get(DiagramLanguage));
                }
            }
        }



        partial void OnCurrentDiagramLanguageChanged(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                ClearDiagramLanguageContext();
                return;
            }

            DiagramLanguageStatus = LanguageService.GetLanguageDisplayName(value);
            SyncDiagramLanguageContext(value);

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
            OnPropertyChanged(nameof(IsDiagramLanguageStatusVisible));

            // 确保先关闭提示
            IsDataStateReminderVisible = false;
            ClearDataGridPlotRefreshPending();
            // 然后再重置状态，确保新模板开始时是false
            _hasViewedHelpForCurrentDiagram = false;

            if (value != null)
            {
                // 设置网格属性按钮是否可用
                IsGridSettingEnabled = true;

                // 优先使用当前 App 语言；模板不支持时回退到模板默认语言
                string initialLanguage = ResolveInitialDiagramLanguage(value);
                if (!string.Equals(CurrentDiagramLanguage, initialLanguage, StringComparison.OrdinalIgnoreCase))
                {
                    CurrentDiagramLanguage = initialLanguage;
                }
                else
                {
                    SyncDiagramLanguageContext(initialLanguage);
                    DiagramLanguageStatus = LanguageService.GetLanguageDisplayName(initialLanguage);
                }

                // 统一在模板切换时执行一次字体自动检测，
                // 让蛛网图标题和坐标轴标题与普通模板保持一致。
                AutoDetectFonts();
                
                // 重置脚本为只读状态（需要二次确认进入编辑模式）
                if (value.Script != null)
                {
                    value.Script.IsReadOnly = true;
                }
            }
            else
            {
                DiagramLanguageStatus = "";
                CurrentDiagramLanguage = "";
                ClearDiagramLanguageContext();
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
                        var text = bottomAxis.Label.Get(DiagramLanguage);
                        if (!string.IsNullOrEmpty(text)) labelA = text;
                    }
                    if (leftAxis != null && leftAxis.Label != null)
                    {
                        var text = leftAxis.Label.Get(DiagramLanguage);
                        if (!string.IsNullOrEmpty(text)) labelB = text;
                    }
                    if (rightAxis != null && rightAxis.Label != null)
                    {
                        var text = rightAxis.Label.Get(DiagramLanguage);
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
            // 获取WPF的鼠标位置
            Point mousePosition = e.GetPosition(WpfPlot1);
            
            // 获取DPI缩放系数
            var source = PresentationSource.FromVisual(WpfPlot1);
            double dpiScale = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            
            // 将WPF逻辑单位转换为ScottPlot像素单位（应用DPI缩放）
            Pixel mousePixel = new(mousePosition.X * dpiScale, mousePosition.Y * dpiScale);

            // 将像素位置转换为图表的坐标单位
            Coordinates mouseCoordinates = WpfPlot1.Plot.GetCoordinates(mousePixel);

            // 预先计算真实坐标 (用于状态栏)
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

            long currentMs = Environment.TickCount64;

            if (isDrawingOrPicking)
            {
                Coordinates? snapPoint = null;
                if (currentMs - _lastSnapCheckTimeMs >= SnapCheckIntervalMs)
                {
                    _lastSnapCheckTimeMs = currentMs;
                    snapPoint = GetSnapPoint(mousePixel);
                    _lastSnapSearchHasResult = snapPoint.HasValue;
                    _lastSnapSearchResult = snapPoint;
                }
                else if (_lastSnapSearchHasResult)
                {
                    snapPoint = _lastSnapSearchResult;
                }

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

            // 坐标更新节流
            if (currentMs - _lastCoordinateUpdateMs > CoordinateUpdateIntervalMs)
            {
                _lastCoordinateUpdateMs = currentMs;
                UpdateCoordinateStatus(mouseCoordinates, realCoordinates);
            }

            // 只有当开启吸附，且距离上次检测超过设定间隔时，才执行命中测试
            if (IsSnapSelectionEnabled && !(IsPickingPointMode || IsAddingLine || IsAddingArrow || IsAddingPolygon || IsAddingText) && (currentMs - _lastHitTestTimeMs > _hitTestIntervalMs))
            {
                // 更新最后检测时间
                _lastHitTestTimeMs = currentMs;

                var currentHoveredPlottable = GetPlottableAtPixel(mousePixel, 10);

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
                            bool isSelectedSpiderSubItem = currentLayer == _selectedLayer &&
                                                           currentLayer is SpiderSampleLayerItemViewModel selectedSpiderLayer &&
                                                           selectedSpiderLayer.ContainsPlottable(currentHoveredPlottable);

                            if (currentLayer != _selectedLayer || isSelectedSpiderSubItem)
                            {
                                HighlightLayer(currentLayer);
                                _lastHoveredLayer = currentLayer;
                            }
                        }
                    }

                    // 3. 记录当前悬浮对象并刷新
                    _lastHoveredPlottable = currentHoveredPlottable;
                    needRefresh = true;
                }

                // 鼠标指针应跟随当前命中状态，而不依赖于悬浮对象是否发生切换
                WpfPlot1.Cursor = currentHoveredPlottable != null ? Cursors.Hand : Cursors.Arrow;
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
                    _tempLinePlot.Color = ScottPlot.Colors.Red; // 设置预览线为红色虚线
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
                    _tempArrowPlot.ArrowFillColor = ScottPlot.Colors.Red;
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
                    _tempRubberBandLine.Color = ScottPlot.Colors.Red;
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
                bool isPreviewOnlyRefresh = (IsAddingLine && _lineStartPoint.HasValue)
                    || (IsAddingArrow && _arrowStartPoint.HasValue)
                    || (IsAddingPolygon && _polygonVertices.Any());

                if (!isPreviewOnlyRefresh
                    || snapChanged
                    || currentMs - _lastPreviewRefreshMs >= PreviewRefreshIntervalMs)
                {
                    _lastPreviewRefreshMs = currentMs;
                    WpfPlot1.Refresh();
                }
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
        private readonly Dictionary<string, List<TemplateCardViewModel>> _templateCardsCache = new(StringComparer.OrdinalIgnoreCase);
        private const int MaxThumbnailBytesCacheEntries = 64;
        private readonly Dictionary<Guid, LinkedListNode<(Guid Id, byte[] Bytes)>> _thumbnailBytesCacheMap = new();
        private readonly LinkedList<(Guid Id, byte[] Bytes)> _thumbnailBytesCacheOrder = new();
        private readonly object _thumbnailCacheLock = new();
        private readonly HashSet<Guid> _thumbnailLoadInFlight = new();
        private bool _suppressTemplateCardsEmptyStateUpdates;

        /// <summary>
        /// 模板卡片列表呈现更新后通知 View 刷新可见区缩略图
        /// </summary>
        public event Action? TemplateCardsPresentationChanged;

        /// <summary>
        /// 清空模板卡片缓存（数据变更后调用）
        /// </summary>
        private void InvalidateTemplateCardsCache()
        {
            _templateCardsCache.Clear();
            lock (_thumbnailCacheLock)
            {
                _thumbnailBytesCacheMap.Clear();
                _thumbnailBytesCacheOrder.Clear();
            }
            _thumbnailLoadInFlight.Clear();
            _displayedTemplateCardsCacheKey = null;
        }

        private bool TryGetCachedThumbnailBytes(Guid templateId, out byte[]? bytes)
        {
            lock (_thumbnailCacheLock)
            {
                if (_thumbnailBytesCacheMap.TryGetValue(templateId, out var node))
                {
                    _thumbnailBytesCacheOrder.Remove(node);
                    _thumbnailBytesCacheOrder.AddFirst(node);
                    bytes = node.Value.Bytes;
                    return true;
                }

                bytes = null;
                return false;
            }
        }

        private void PutCachedThumbnailBytes(Guid templateId, byte[] bytes)
        {
            lock (_thumbnailCacheLock)
            {
                if (_thumbnailBytesCacheMap.TryGetValue(templateId, out var existing))
                {
                    _thumbnailBytesCacheOrder.Remove(existing);
                    existing.Value = (templateId, bytes);
                    _thumbnailBytesCacheOrder.AddFirst(existing);
                    return;
                }

                var node = _thumbnailBytesCacheOrder.AddFirst((templateId, bytes));
                _thumbnailBytesCacheMap[templateId] = node;

                while (_thumbnailBytesCacheMap.Count > MaxThumbnailBytesCacheEntries)
                {
                    var last = _thumbnailBytesCacheOrder.Last;
                    if (last == null)
                        break;

                    _thumbnailBytesCacheOrder.RemoveLast();
                    _thumbnailBytesCacheMap.Remove(last.Value.Id);
                }
            }
        }

        private static string GetTemplateCardsCacheKey(GraphMapTemplateNode? node)
        {
            if (node == null)
                return "null";

            if (node.TemplateId is Guid templateId)
                return $"id:{templateId:D}";

            if (!string.IsNullOrEmpty(node.GraphMapPath))
                return $"path:{node.GraphMapPath}";

            var segments = new List<string>();
            var current = node;
            while (current != null)
            {
                if (!string.IsNullOrEmpty(current.Name))
                    segments.Add(current.Name);
                current = current.Parent;
            }

            segments.Reverse();
            return $"cat:{string.Join("/", segments)}";
        }

        private void StoreTemplateCardsCache(string cacheKey, List<TemplateCardViewModel> cards)
        {
            _templateCardsCache[cacheKey] = cards;
        }

        private bool TryGetCachedTemplateCards(string cacheKey, out List<TemplateCardViewModel> cards)
        {
            return _templateCardsCache.TryGetValue(cacheKey, out cards!);
        }

        private async Task ApplyCachedTemplateCardsAsync(
            IReadOnlyList<TemplateCardViewModel> cards,
            CancellationToken token)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
                return;

            await dispatcher.InvokeAsync(() =>
            {
                if (token.IsCancellationRequested)
                    return;

                _suppressTemplateCardsEmptyStateUpdates = true;
                try
                {
                    TemplateCards.Clear();
                    foreach (var card in cards)
                        TemplateCards.Add(card);
                    TemplateCardsView?.Refresh();
                }
                finally
                {
                    _suppressTemplateCardsEmptyStateUpdates = false;
                    UpdateTemplateCardsEmptyState();
                }
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private void ScheduleTemplateCardDetailsLoad(
            CancellationToken token,
            Dictionary<Guid, GraphMapTemplateEntity>? summaryLookup,
            IReadOnlyList<TemplateCardViewModel>? cardsOverride = null,
            IReadOnlyList<TemplateCardViewModel>? initialThumbnailCards = null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await LoadTemplateCardStatesAsync(token, summaryLookup, cardsOverride);
                    if (token.IsCancellationRequested)
                        return;

                    var thumbTargets = initialThumbnailCards;
                    if (thumbTargets == null || thumbTargets.Count == 0)
                    {
                        var snapshot = cardsOverride ?? TemplateCards.ToList();
                        thumbTargets = snapshot.Take(36).ToList();
                    }

                    await LoadTemplateCardThumbnailsAsync(token, thumbTargets);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ScheduleTemplateCardDetailsLoad error: {ex.Message}");
                }
            }, token);
        }

        /// <summary>
        /// 为可见区域的模板卡片按需加载缩略图（由 View 在滚动或列表更新后调用）
        /// </summary>
        public void RequestTemplateCardThumbnails(IEnumerable<TemplateCardViewModel> cards)
        {
            if (_loadTemplatesCts == null)
                return;

            var token = _loadTemplatesCts.Token;
            var targets = cards
                .Where(card => card.TemplateId.HasValue && card.ThumbnailImage == null)
                .Distinct()
                .ToList();

            if (targets.Count == 0)
                return;

            _ = LoadTemplateCardThumbnailsAsync(token, targets);
        }

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

            var summaryLookup = await Task.Run(BuildTemplateSummaryLookup, token);
            var cards = await Task.Run(() => CollectTemplateCardsFromNode(GraphMapTemplateNode, summaryLookup), token);

            var cacheKey = GetTemplateCardsCacheKey(GraphMapTemplateNode);
            StoreTemplateCardsCache(cacheKey, cards);

            await ReplaceTemplateCardsInBatchesAsync(cards, token);
            _displayedTemplateCardsCacheKey = cacheKey;
            RaiseTemplateCardsPresentationChanged();

            ScheduleTemplateCardDetailsLoad(token, summaryLookup, cards, cards.Take(36).ToList());
        }

        /// <summary>
        /// 后台加载模板状态（不加载缩略图，浏览切换时不写回数据库）
        /// </summary>
        private async Task LoadTemplateCardStatesAsync(
            CancellationToken token,
            Dictionary<Guid, GraphMapTemplateEntity>? summaryLookup = null,
            IReadOnlyList<TemplateCardViewModel>? cardsOverride = null)
        {
            var cardsSnapshot = cardsOverride?.ToList() ?? TemplateCards.ToList();
            if (cardsSnapshot.Count == 0)
                return;

            summaryLookup ??= BuildTemplateSummaryLookup();

            await Task.Run(() =>
            {
                var batchUpdateList = new List<(TemplateCardViewModel Card, TemplateState State)>();
                const int batchSize = 40;

                foreach (var card in cardsSnapshot)
                {
                    if (token.IsCancellationRequested)
                        return;

                    TemplateState newState = card.State;

                    if (card.TemplateId.HasValue)
                    {
                        if (!card.IsCustomTemplate)
                        {
                            if (summaryLookup.TryGetValue(card.TemplateId.Value, out var summary))
                            {
                                newState = ResolveOfficialTemplateUpdateState(
                                    summary,
                                    card.ServerHash,
                                    card.ServerVersion,
                                    dbService: null);
                            }
                            else
                            {
                                newState = TemplateState.NotDownloaded;
                            }
                        }
                        else if (card.State == TemplateState.Loading)
                        {
                            newState = TemplateState.Ready;
                        }
                    }
                    else
                    {
                        newState = TemplateState.NotDownloaded;
                    }

                    if (card.State != newState)
                        batchUpdateList.Add((card, newState));

                    if (batchUpdateList.Count >= batchSize)
                    {
                        UpdateTemplateCardStatesOnUi(batchUpdateList, token);
                        batchUpdateList.Clear();
                    }
                }

                if (batchUpdateList.Count > 0)
                    UpdateTemplateCardStatesOnUi(batchUpdateList, token);
            }, token);
        }

        /// <summary>
        /// 按需加载模板缩略图（支持内存缓存与并发去重）
        /// </summary>
        private Task LoadTemplateCardThumbnailsAsync(
            CancellationToken token,
            IReadOnlyList<TemplateCardViewModel> cards)
        {
            if (cards.Count == 0)
                return Task.CompletedTask;

            return Task.Run(() =>
            {
                var batchUpdateList = new List<(TemplateCardViewModel Card, byte[] Bytes)>();
                const int batchSize = 12;

                foreach (var card in cards)
                {
                    if (token.IsCancellationRequested)
                        return;

                    if (!card.TemplateId.HasValue || card.ThumbnailImage != null)
                        continue;

                    var templateId = card.TemplateId.Value;
                    if (_thumbnailLoadInFlight.Contains(templateId))
                        continue;

                    byte[]? thumbBytes = null;
                    if (TryGetCachedThumbnailBytes(templateId, out var cachedBytes))
                    {
                        thumbBytes = cachedBytes;
                    }
                    else
                    {
                        _thumbnailLoadInFlight.Add(templateId);
                        try
                        {
                            using var thumbStream = GraphMapDatabaseService.Instance.GetThumbnail(templateId);
                            if (thumbStream != null)
                            {
                                using var ms = new MemoryStream();
                                thumbStream.CopyTo(ms);
                                thumbBytes = ms.ToArray();
                                PutCachedThumbnailBytes(templateId, thumbBytes);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Failed to load thumbnail from DB: {ex.Message}");
                        }
                        finally
                        {
                            _thumbnailLoadInFlight.Remove(templateId);
                        }
                    }

                    if (thumbBytes != null)
                        batchUpdateList.Add((card, thumbBytes));

                    if (batchUpdateList.Count >= batchSize)
                    {
                        UpdateTemplateCardThumbnailsOnUi(batchUpdateList, token);
                        batchUpdateList.Clear();
                    }
                }

                if (batchUpdateList.Count > 0)
                    UpdateTemplateCardThumbnailsOnUi(batchUpdateList, token);
            }, token);
        }

        private void UpdateTemplateCardStatesOnUi(
            List<(TemplateCardViewModel Card, TemplateState State)> batch,
            CancellationToken token)
        {
            if (token.IsCancellationRequested || batch.Count == 0)
                return;

            var updates = batch.ToList();
            Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                if (token.IsCancellationRequested)
                    return;

                foreach (var item in updates)
                    item.Card.State = item.State;
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private void UpdateTemplateCardThumbnailsOnUi(
            List<(TemplateCardViewModel Card, byte[] Bytes)> batch,
            CancellationToken token)
        {
            if (token.IsCancellationRequested || batch.Count == 0)
                return;

            var updates = batch.ToList();
            Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                if (token.IsCancellationRequested)
                    return;

                foreach (var item in updates)
                {
                    if (item.Card.ThumbnailImage != null)
                        continue;

                    try
                    {
                        item.Card.ThumbnailImage = CreateTemplateCardThumbnailImage(item.Bytes);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error creating bitmap: {ex.Message}");
                    }
                }
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private void RaiseTemplateCardsPresentationChanged()
        {
            Application.Current?.Dispatcher.InvokeAsync(
                () => TemplateCardsPresentationChanged?.Invoke(),
                System.Windows.Threading.DispatcherPriority.Background);
        }
        private Dictionary<Guid, GraphMapTemplateEntity> BuildTemplateSummaryLookup()
        {
            try
            {
                return GraphMapDatabaseService.Instance.GetSummaries()
                    .GroupBy(entity => entity.Id)
                    .ToDictionary(group => group.Key, group => group.First());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Build template summary lookup error: {ex.Message}");
                return new Dictionary<Guid, GraphMapTemplateEntity>();
            }
        }

        private static TemplateState ResolveTemplateState(string? status)
        {
            return status switch
            {
                "UP_TO_DATE" => TemplateState.Ready,
                "OUTDATED" => TemplateState.UpdateAvailable,
                "NOT_INSTALLED" => TemplateState.NotDownloaded,
                _ => TemplateState.Loading
            };
        }

        /// <summary>
        /// 按数据库中缩略图原始分辨率解码（默认 680×480 灰度 JPEG）。虚拟化下同时呈现的卡片数量有限，内存可接受。
        /// </summary>
        private static BitmapImage CreateTemplateCardThumbnailImage(byte[] bytes)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = new MemoryStream(bytes);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        private TemplateCardViewModel CreateTemplateCardViewModel(
            GraphMapTemplateNode templateNode,
            Dictionary<Guid, GraphMapTemplateEntity>? summaryLookup = null)
        {
            GraphMapTemplateEntity? summary = null;
            if (templateNode.TemplateId.HasValue && summaryLookup != null)
            {
                summaryLookup.TryGetValue(templateNode.TemplateId.Value, out summary);
            }

            bool isFavorite = summary?.IsFavorite ?? ReferenceEquals(templateNode.Parent, FavoriteTemplatesNode);
            string? status = summary?.Status ?? templateNode.Status;

            return new TemplateCardViewModel
            {
                Name = templateNode.Name,
                TemplateId = templateNode.TemplateId,
                TemplatePath = templateNode.GraphMapPath,
                Category = GetNodePath(templateNode),
                ServerHash = templateNode.FileHash,
                ServerVersion = templateNode.ServerVersion,
                IsCustomTemplate = templateNode.IsCustomTemplate,
                IsNewTemplate = summary?.IsNewTemplate ?? false,
                IsFavorite = isFavorite,
                State = ResolveTemplateState(status),
                ThumbnailImage = null,
                OpenHandler = (vm) => SelectTemplateCardCommand.ExecuteAsync(vm),
                DownloadHandler = (vm) => DownloadSingleTemplate(vm, showNotification: true),
                CheckUpdateHandler = CheckSingleTemplateUpdate,
                ToggleFavoriteHandler = ToggleFavorite,
                DeleteHandler = PerformDeleteTemplate,
                EditHandler = EditTemplate
            };
        }

        /// <summary>
        /// 官方模板推送成功后，同步当前可见卡片的 IsNewTemplate 状态
        /// </summary>
        private void SyncTemplateCardPublishFlags()
        {
            var summaries = GraphMapDatabaseService.Instance.GetSummaries().ToDictionary(x => x.Id);
            foreach (var card in TemplateCards)
            {
                if (card.TemplateId.HasValue
                    && summaries.TryGetValue(card.TemplateId.Value, out var summary))
                {
                    card.IsNewTemplate = summary.IsNewTemplate;
                }
            }
        }

        /// <summary>
        /// 递归收集节点下的所有模板卡片（纯内存，不直接触碰 UI 集合）
        /// </summary>
        private List<TemplateCardViewModel> CollectTemplateCardsFromNode(
            GraphMapTemplateNode node,
            Dictionary<Guid, GraphMapTemplateEntity>? summaryLookup = null)
        {
            var cards = new List<TemplateCardViewModel>();
            if (node?.Children == null) return cards;

            foreach (var child in node.Children)
            {
                if (!string.IsNullOrEmpty(child.GraphMapPath) || child.IsCustomTemplate)
                {
                    cards.Add(CreateTemplateCardViewModel(child, summaryLookup));
                }
                else
                {
                    cards.AddRange(CollectTemplateCardsFromNode(child, summaryLookup));
                }
            }

            return cards;
        }

        private async Task ReplaceTemplateCardsInBatchesAsync(
            IReadOnlyList<TemplateCardViewModel> cards,
            System.Threading.CancellationToken token)
        {
            const int batchSize = 48;
            var dispatcher = Application.Current?.Dispatcher;

            if (dispatcher == null)
            {
                _suppressTemplateCardsEmptyStateUpdates = true;
                try
                {
                    TemplateCards.Clear();
                    foreach (var card in cards)
                        TemplateCards.Add(card);
                }
                finally
                {
                    _suppressTemplateCardsEmptyStateUpdates = false;
                    UpdateTemplateCardsEmptyState();
                }
                return;
            }

            await dispatcher.InvokeAsync(() =>
            {
                if (token.IsCancellationRequested) return;
                _suppressTemplateCardsEmptyStateUpdates = true;
                TemplateCards.Clear();
            }, System.Windows.Threading.DispatcherPriority.Background);

            for (int i = 0; i < cards.Count; i += batchSize)
            {
                token.ThrowIfCancellationRequested();

                var batch = cards.Skip(i).Take(batchSize).ToList();
                await dispatcher.InvokeAsync(() =>
                {
                    if (token.IsCancellationRequested) return;

                    foreach (var card in batch)
                        TemplateCards.Add(card);
                }, System.Windows.Threading.DispatcherPriority.Background);

                if (i + batchSize < cards.Count)
                    await Task.Yield();
            }

            await dispatcher.InvokeAsync(() =>
            {
                if (token.IsCancellationRequested) return;

                _suppressTemplateCardsEmptyStateUpdates = false;
                TemplateCardsView?.Refresh();
                UpdateTemplateCardsEmptyState();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// 删除模板
        /// </summary>
        [RelayCommand]
        private async Task DeleteTemplate(TemplateCardViewModel card)
        {
            await PerformDeleteTemplate(card, false);
        }

        /// <summary>
        /// 执行删除模板操作
        /// </summary>
        /// <param name="card">模板卡片</param>
        /// <param name="skipConfirmation">是否跳过二次确认（Ctrl快捷删除时为true）</param>
        private async Task PerformDeleteTemplate(TemplateCardViewModel card, bool skipConfirmation)
        {
            if (card == null || (!card.IsCustomTemplate && !IsDeveloperMode) || !card.TemplateId.HasValue) return;

            if (!skipConfirmation)
            {
                // 确定要删除该模板吗？
                bool confirm = await MessageHelper.ShowAsyncDialog(
                    LanguageService.Instance["confirm_delete_custom_diagram_template"],
                    LanguageService.Instance["Cancel"],
                    LanguageService.Instance["Confirm"]);

                if (!confirm) return;
            }

            try
            {
                // 使用数据库服务删除
                GraphMapDatabaseService.Instance.DeleteTemplate(card.TemplateId.Value);

                // 如果删除的是官方模板（开发者模式），同步删除本地 GraphMapList.json
                if (!card.IsCustomTemplate)
                {
                    string localListPath = FileHelper.GetDataPath("PlotData", "GraphMapList.json");
                    if (File.Exists(localListPath))
                    {
                        File.Delete(localListPath);
                    }
                }

                await RefreshTemplateLibraryAfterDataChangeAsync();

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
                    var vars = PlotDataGridHelper.ParseScriptDataColumns(template.Script.RequiredDataSeries);

                    foreach (var v in vars)
                    {
                        if (!Regex.IsMatch(v, @"^[a-zA-Z_$][a-zA-Z0-9_$]*$"))
                        {
                            // • 脚本变量名格式错误：'{v}' 不是有效的变量名。
                            warnings.Add(LanguageService.Instance["invalid_script_variable_name"] +
                                v + LanguageService.Instance["is_not_a_valid_variable_name"]);
                        }
                    }

                    // Check execution. Coordinate scripts are treated as function bodies and must explicitly return an array.
                    try
                    {
                        var engine = new Jint.Engine();
                        var tempLogs = new List<string>();
                        JintHelper.InjectTraceFunction(engine, tempLogs); // 注入trace函数避免验证失败
                        foreach (var v in vars)
                        {
                            engine.SetValue(v, 1.0); // 测试数据
                        }

                        if (!JintHelper.TryPrepareCoordinateScript(engine, template.Script.ScriptBody))
                        {
                            throw new InvalidOperationException(
                                Lang("plot_script_must_return_array_explicitly", "Scripts must explicitly return an array, for example: return [x, y];"));
                        }

                        var result = JintHelper.InvokePreparedCoordinateScript(engine);

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
                        var tempLogs = new List<string>();
                        JintHelper.InjectTraceFunction(engine, tempLogs); // 注入trace函数避免验证失败
                        if (!JintHelper.TryPrepareCoordinateScript(engine, template.Script.ScriptBody))
                        {
                            throw new InvalidOperationException(
                                Lang("plot_script_must_return_array_explicitly", "Scripts must explicitly return an array, for example: return [x, y];"));
                        }

                        var result = JintHelper.InvokePreparedCoordinateScript(engine);
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
                        else
                        {
                            warnings.Add(LanguageService.Instance["script_return_value_error_must_be_array"]);
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

                // 2. 选择保存位置（用户导出使用 .gndiag，内容仍为 zip）
                var dialog = new VistaSaveFileDialog();
                dialog.Filter = FileDialogFilterHelper.DiagramPackageFiles;
                dialog.DefaultExt = TemplatePackageFileExtensions.DiagramPrimary.TrimStart('.');

                string templateFileName = entity.Name;
                if (string.IsNullOrEmpty(templateFileName))
                {
                    templateFileName = Path.GetFileName(entity.GraphMapPath);
                }

                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    templateFileName = templateFileName.Replace(c, '_');
                }

                dialog.FileName = $"{templateFileName}{TemplatePackageFileExtensions.DiagramPrimary}";

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
        /// 编辑图解模板元数据（分类、语言）
        /// </summary>
        [RelayCommand]
        private async Task EditTemplate(TemplateCardViewModel card)
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

                var editWindow = new DiagramPlotEditorWindow();
                TrySetDialogOwner(editWindow);
                editWindow.InitializeForEdit(entity);

                editWindow.ConfirmCommand = new RelayCommand<DiagramPlotEditorViewModel>(async (editor) =>
                {
                    if (editor == null) return;

                    if (!editor.Validate(out string validationError))
                    {
                        editWindow.ShowWarningMessage(validationError);
                        return;
                    }

                    var updatedTemplate = editor.BuildTemplateForSubmit();
                    var newNodeList = updatedTemplate.NodeList;
                    var languages = editor.LanguageParts.Select(l => l.Text).ToList();

                    var categoryParts = editor.CategoryParts.ToList();
                    string lastPart = categoryParts[categoryParts.Count - 1].DisplayName;
                    string secondLastPart = categoryParts[categoryParts.Count - 2].DisplayName;
                    string newGraphMapPath = $"{secondLastPart}_{lastPart}";

                    // 根据新的 GraphMapPath 生成新的确定性 ID
                    Guid oldId = entity.Id;
                    Guid newId = GraphMapDatabaseService.GenerateId(newGraphMapPath, entity.IsCustom);

                    // 检查新 ID 对应的模板是否已存在（排除当前模板自身）
                    if (newId != oldId)
                    {
                        var existingTemplate = await Task.Run(() => GraphMapDatabaseService.Instance.GetTemplate(newId));
                        if (existingTemplate != null)
                        {
                            editWindow.ShowWarningMessage(LanguageService.Instance["BasemapExisted"]);
                            return;
                        }
                    }

                    entity.Content = updatedTemplate;
                    entity.NodeList = newNodeList;
                    entity.Content.NodeList = newNodeList;
                    entity.Content.DefaultLanguage = languages.First();
                    entity.Version = updatedTemplate.Version;
                    entity.TemplateType = updatedTemplate.TemplateType;
                    entity.GraphMapPath = newGraphMapPath;
                    entity.Name = newGraphMapPath;
                    entity.LastModified = DateTime.Now;
                    entity.HelpDocuments = editor.GetHelpDocumentsForSubmit();

                    entity.FileHash = GraphMapTemplateService.ComputeTemplateContentHash(entity.Content);

                    if (!entity.IsCustom)
                    {
                        entity.PendingPublish = true;
                        bool isHashSame = string.Equals(entity.FileHash, card.ServerHash, StringComparison.OrdinalIgnoreCase);
                        entity.Status = isHashSame ? "UP_TO_DATE" : "OUTDATED";
                    }

                    if (newId != oldId)
                    {
                        // ID 变更：需要迁移数据库记录和缩略图

                        // 1. 备份缩略图
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

                        // 2. 删除旧记录
                        GraphMapDatabaseService.Instance.DeleteTemplate(oldId);

                        // 3. 使用新 ID 保存
                        entity.Id = newId;
                        await Task.Run(() => GraphMapDatabaseService.Instance.UpsertTemplate(entity));

                        // 4. 迁移缩略图
                        if (thumbnailBytes != null)
                        {
                            using (var ms = new MemoryStream(thumbnailBytes))
                            {
                                GraphMapDatabaseService.Instance.UploadThumbnail(newId, ms);
                            }
                        }

                        // 5. 同步当前绘图编辑器的模板 ID
                        if (_currentTemplateId == oldId)
                        {
                            _currentTemplateId = newId;
                        }
                    }
                    else
                    {
                        // ID 未变更：直接更新
                        await Task.Run(() => GraphMapDatabaseService.Instance.UpsertTemplate(entity));
                    }

                    TemplateState refreshedState = !entity.IsCustom
                        ? ResolveOfficialTemplateUpdateState(entity, card.ServerHash, card.ServerVersion)
                        : TemplateState.Ready;

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (newId != oldId)
                        {
                            card.TemplateId = newId;
                            card.TemplatePath = newGraphMapPath;
                            card.Name = newGraphMapPath;
                        }

                        if (!entity.IsCustom)
                            card.State = refreshedState;
                    });

                    await RefreshTemplateLibraryAfterDataChangeAsync();

                    editWindow.ShowSuccessMessage(LanguageService.Instance["ModifedSuccess"]);
                    editWindow.Close();
                });

                editWindow.CancelCommand = new RelayCommand<DiagramPlotEditorViewModel>(_ => editWindow.Close());

                editWindow.ShowDialog();

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                MessageHelper.Error(LanguageService.Instance["edit_failed"] + ex.Message);
            }
        }

        /// <summary>
        /// 点击模板分类节点
        /// </summary>
        [RelayCommand]
        private async Task SelectTreeViewItem(GraphMapTemplateNode graphMapTemplateNode)
        {
            if (_suppressTreeViewSelectionCommand) return;
            if (graphMapTemplateNode == null) return;

            // 返回模板库后 TreeView 容器延迟生成会再次触发 SelectedItemChanged；
            // 若已在显示同一分类，跳过重建，否则 TemplateCards.Clear 会把刚恢复的滚动冲回顶部。
            var cacheKey = GetTemplateCardsCacheKey(graphMapTemplateNode);
            if (TemplateCards.Count > 0 &&
                string.Equals(cacheKey, _displayedTemplateCardsCacheKey, StringComparison.Ordinal))
            {
                return;
            }

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

            SaveTemplateLibraryState();
        }

        /// <summary>
        /// 显示单个模板卡片
        /// </summary>
        private async Task ShowSingleTemplateCard(GraphMapTemplateNode templateNode)
        {
            _loadTemplatesCts?.Cancel();
            _loadTemplatesCts = new System.Threading.CancellationTokenSource();
            var token = _loadTemplatesCts.Token;

            var cacheKey = GetTemplateCardsCacheKey(templateNode);
            if (TryGetCachedTemplateCards(cacheKey, out var cachedCards))
            {
                await ApplyCachedTemplateCardsAsync(cachedCards, token);
                UpdateBreadcrumbs(templateNode);
                _displayedTemplateCardsCacheKey = cacheKey;
                RaiseTemplateCardsPresentationChanged();
                ScheduleTemplateCardDetailsLoad(token, summaryLookup: null, cachedCards, cachedCards);
                return;
            }

            var summaryLookup = await Task.Run(BuildTemplateSummaryLookup, token);
            var cards = new List<TemplateCardViewModel>
            {
                CreateTemplateCardViewModel(templateNode, summaryLookup)
            };
            StoreTemplateCardsCache(cacheKey, cards);
            await ReplaceTemplateCardsInBatchesAsync(cards, token);
            UpdateBreadcrumbs(templateNode);
            _displayedTemplateCardsCacheKey = cacheKey;
            RaiseTemplateCardsPresentationChanged();
            ScheduleTemplateCardDetailsLoad(token, summaryLookup, cards, cards);
        }

        /// <summary>
        /// 显示分类下的模板卡片
        /// </summary>
        private async Task ShowCategoryTemplateCards(GraphMapTemplateNode categoryNode)
        {
            _loadTemplatesCts?.Cancel();
            _loadTemplatesCts = new System.Threading.CancellationTokenSource();
            var token = _loadTemplatesCts.Token;

            var cacheKey = GetTemplateCardsCacheKey(categoryNode);

            if (TryGetCachedTemplateCards(cacheKey, out var cachedCards))
            {
                await ApplyCachedTemplateCardsAsync(cachedCards, token);
                UpdateBreadcrumbs(categoryNode);
                UpdateCategoryTitleForNode(categoryNode);
                _displayedTemplateCardsCacheKey = cacheKey;
                RaiseTemplateCardsPresentationChanged();
                ScheduleTemplateCardDetailsLoad(token, summaryLookup: null, cachedCards, cachedCards.Take(36).ToList());
                return;
            }

            var summaryLookup = await Task.Run(BuildTemplateSummaryLookup, token);
            var cards = await Task.Run(
                () => CollectTemplateCardsFromNode(categoryNode, summaryLookup),
                token);

            StoreTemplateCardsCache(cacheKey, cards);
            await ReplaceTemplateCardsInBatchesAsync(cards, token);
            UpdateBreadcrumbs(categoryNode);
            UpdateCategoryTitleForNode(categoryNode);
            _displayedTemplateCardsCacheKey = cacheKey;
            RaiseTemplateCardsPresentationChanged();
            ScheduleTemplateCardDetailsLoad(token, summaryLookup, cards, cards.Take(36).ToList());
        }

        private void UpdateCategoryTitleForNode(GraphMapTemplateNode categoryNode)
        {
            bool isTopLevelCategory = categoryNode == PersonalTemplatesNode ||
                                      categoryNode == FavoriteTemplatesNode ||
                                      categoryNode == OfficialTemplatesNode ||
                                      categoryNode == RecentsTemplatesNode;
            if (isTopLevelCategory)
            {
                CurrentCategoryName = categoryNode?.Name ?? string.Empty;
            }
            else
            {
                CurrentCategoryName = string.Empty;
            }
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

            // 清空并重新构建面包屑集合
            Breadcrumbs.Clear();
            Breadcrumbs.Add(new BreadcrumbItem { Name = LanguageService.Instance["all_templates"] });

            // 检查是否为大类节点（个人、收藏、官方、最近使用）
            bool isTopLevelCategory = currentNode == PersonalTemplatesNode || 
                                      currentNode == FavoriteTemplatesNode || 
                                      currentNode == OfficialTemplatesNode || 
                                      currentNode == RecentsTemplatesNode;

            if (isTopLevelCategory)
            {
                // 如果是大类节点，只显示：全部模板 / 大类名称
                Breadcrumbs.Add(new BreadcrumbItem { Name = currentNode.Name, Node = currentNode });
            }
            else
            {
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

                // 判断当前节点是否属于官方或个人图解模板
                // 通过检查路径中的根节点是否在官方或个人模板节点的子节点中
                GraphMapTemplateNode topCategoryNode = null;
                if (path.Count > 0)
                {
                    var rootNode = path[0];
                    if (OfficialTemplatesNode?.Children?.Contains(rootNode) == true)
                    {
                        topCategoryNode = OfficialTemplatesNode;
                    }
                    else if (PersonalTemplatesNode?.Children?.Contains(rootNode) == true)
                    {
                        topCategoryNode = PersonalTemplatesNode;
                    }
                }

                // 如果找到所属的大分类，先添加大分类节点
                if (topCategoryNode != null)
                {
                    Breadcrumbs.Add(new BreadcrumbItem { Name = topCategoryNode.Name, Node = topCategoryNode });
                }

                // 将路径上的所有节点添加到面包屑集合中
                foreach (var node in path)
                {
                    Breadcrumbs.Add(new BreadcrumbItem { Name = node.Name, Node = node });
                }
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
            if (card == null || IsTransitionLoading) return;

            // 在切换模式/显示过渡遮罩之前保存滚动位置，确保读到真实偏移
            SaveTemplateLibraryState();

            await BeginTransitionLoadingAsync(enteringPlot: true);
            try
            {
                // 记录模板使用
                if (card.TemplateId.HasValue)
                {
                    RecordTemplateUsage(card.TemplateId.Value);
                }

                // 确保清除语言覆盖，使用软件默认配置
                ClearDiagramLanguageContext();
                IsHarkerDiagramMode = false;

                // 设置是否为自定义模板 (官方模板不显示未保存提醒)
                _isCurrentTemplateCustom = card.IsCustomTemplate;
                UpdateHelpDocReadOnlyState();

                // 切换到绘图模式
                IsTemplateMode = false;
                IsPlotMode = true;

                if (!card.TemplateId.HasValue)
                {
                    MessageHelper.Error(LanguageService.Instance["diagram_template_source_missing"]);
                    await BackToTemplateMode();
                    return;
                }

                _currentTemplateId = card.TemplateId.Value;
                _currentTemplateFilePath = null;
                if (!await LoadAndBuildLayersFromDb(card.TemplateId.Value))
                {
                    await BackToTemplateMode();
                    return;
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
            finally
            {
                await EndTransitionLoadingAsync();
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
            // 统一使用 HasUnsavedChanges 判断，确保 * 号和弹窗同步
            // 强制刷新一次未保存状态
            CheckUnsavedChanges();

            // 检查是否有未保存的更改 (仅针对自定义模板)
            if (_isCurrentTemplateCustom && HasUnsavedChanges)
            {
                // 当前模板有未保存的内容，是否保存？
                var result = await NotificationManager.Instance.ShowThreeButtonDialogAsync(
                    LanguageService.Instance["tips"] ?? "提示",
                    LanguageService.Instance["unsaved_template_changes_confirm"],
                    LanguageService.Instance["Save"],
                    LanguageService.Instance["DontSave"],
                    LanguageService.Instance["Cancel"]);

                if (result == 2) // Cancel
                {
                    return;
                }
                else if (result == 0) // Save
                {
                    await PerformSave();
                }
                else if (result == 1) // Don't Save
                {
                    RestoreCurrentTemplateFromOriginal();
                }
            }

            await BeginTransitionLoadingAsync(enteringPlot: false);
            try
            {
            CancelPendingPlotRefreshes();

            // 1. 优先切换 UI 状态，提升响应速度
            IsTemplateMode = true;
            IsPlotMode = false;
            RibbonTabIndex = 0;
            IsShowTemplateInfo = false;
            IsSpiderDiagramMode = false;

            // 如果是蛛网图模式，重置蛛网图状态
            if (SpiderDiagramViewModel.IsSpiderPlotMode)
            {
                SpiderDiagramViewModel.IsSpiderPlotMode = false;
                // 清理蛛网图模式下的属性定义缓存
                _spiderTitleDef = null;
                _spiderLegendDef = null;
                _spiderGridDef = null;
            }

            // 2. 在返回模板库之前，完全重置绘图状态
            ResetPlotStateToDefault();

            // 3. 仅在必要时重新加载所有模板（包括更新后的自定义列表），
            //    并在遮罩关闭前完成滚动恢复，避免先闪顶部再跳回。
            await RefreshTemplateLibraryIfNeededAsync();

            // 再等一帧 Render，确保恢复后的滚动已进入合成，再揭开遮罩
            var renderDispatcher = Application.Current?.Dispatcher;
            if (renderDispatcher != null)
            {
                await renderDispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            }
            }
            finally
            {
                await EndTransitionLoadingAsync();
            }

            // 滚动已在遮罩下恢复；延后解除 TreeView 抑制，覆盖容器延迟生成的 SelectedItemChanged。
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null)
            {
                _ = UnsuppressTreeViewSelectionAfterIdleAsync(dispatcher);
            }
            else
            {
                _suppressTreeViewSelectionCommand = false;
            }
        }

        private async Task BeginTransitionLoadingAsync(bool enteringPlot)
        {
            if (IsTransitionLoading)
            {
                IsEnteringPlotTransition = enteringPlot;
                IsReturningTemplateLibraryTransition = !enteringPlot;
                await Task.Yield();
                return;
            }

            IsEnteringPlotTransition = enteringPlot;
            IsReturningTemplateLibraryTransition = !enteringPlot;
            IsTransitionLoading = true;
            _transitionLoadingStopwatch.Restart();

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null)
            {
                await dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            }
            else
            {
                await Task.Yield();
            }

            // 给遮罩一个很短的时间完成首帧绘制，避免重逻辑先占满 UI 线程。
            await Task.Delay(TransitionLoadingRenderLeadMs);
        }

        private async Task EndTransitionLoadingAsync()
        {
            if (!IsTransitionLoading)
            {
                return;
            }

            var remainingMs = TransitionLoadingMinimumVisibleMs - _transitionLoadingStopwatch.ElapsedMilliseconds;
            if (remainingMs > 0)
            {
                await Task.Delay((int)remainingMs);
            }

            _transitionLoadingStopwatch.Reset();
            IsTransitionLoading = false;
            IsEnteringPlotTransition = false;
            IsReturningTemplateLibraryTransition = false;
        }

        /// <summary>
        /// 在 ApplicationIdle 后再延迟解除 TreeView 选中抑制，避免容器延迟生成触发误选。
        /// </summary>
        private async Task UnsuppressTreeViewSelectionAfterIdleAsync(System.Windows.Threading.Dispatcher dispatcher)
        {
            await dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            await Task.Delay(300);
            _suppressTreeViewSelectionCommand = false;
        }

        private bool CanUpdateData()
        {
            return !IsTransitionLoading && !IsDataPlotLoading;
        }

        partial void OnIsTransitionLoadingChanged(bool value)
        {
            UpdateDataCommand.NotifyCanExecuteChanged();
        }

        partial void OnIsDataPlotLoadingChanged(bool value)
        {
            UpdateDataCommand.NotifyCanExecuteChanged();
        }

        private async Task BeginDataPlotLoadingAsync()
        {
            if (IsDataPlotLoading)
            {
                await Task.Yield();
                return;
            }

            IsDataPlotLoading = true;
            _dataPlotLoadingStopwatch.Restart();

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null)
            {
                await dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            }
            else
            {
                await Task.Yield();
            }

            // 给遮罩一个很短的时间完成首帧绘制，避免投图逻辑先占满 UI 线程。
            await Task.Delay(DataPlotLoadingRenderLeadMs);
        }

        private async Task EndDataPlotLoadingAsync()
        {
            if (!IsDataPlotLoading)
            {
                return;
            }

            var remainingMs = DataPlotLoadingMinimumVisibleMs - _dataPlotLoadingStopwatch.ElapsedMilliseconds;
            if (remainingMs > 0)
            {
                await Task.Delay((int)remainingMs);
            }

            _dataPlotLoadingStopwatch.Reset();
            IsDataPlotLoading = false;
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
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
                // 允许序列化 NaN、Infinity 等特殊浮点值
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
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
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
                    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
                });

                if (clonedTemplate == null) return false;

                // 2. 将当前 LayerTree 的状态同步到克隆对象中
                UpdateTemplateInfoFromLayers(clonedTemplate);

                // 3. 序列化克隆对象，得到“当前完整状态”的 JSON
                string finalJson = SerializeTemplate(clonedTemplate);

                // 4. 与原始 JSON 对比 (忽略字体相关属性的变化)
                bool templateJsonChanged = NormalizeJsonForComparison(finalJson) != NormalizeJsonForComparison(_originalTemplateJson);

                // 5. 检查图解帮助内容是否改变
                bool helpDocChanged = IsHelpDocumentModified();

                return templateJsonChanged || helpDocChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IsTemplateModified check failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查图解帮助文档内容是否改变
        /// 使用加载时存储的原始RTF与当前RTF比较，避免因 RichTextBox 规范化 RTF 导致误判
        /// </summary>
        private bool IsHelpDocumentModified()
        {
            try
            {
                // 如果没有加载模板，或没有当前语言，则不检查
                if (_currentTemplateId == null || string.IsNullOrEmpty(CurrentDiagramLanguage)) return false;

                // 获取当前RichTextBox的RTF内容
                string currentRtf = RtfHelper.GetRtfString(_richTextBox);

                // 使用加载时存储的原始RTF进行比较
                // 需要注意的是：
                // 不直接从数据库读取原始RTF来比较，因为 RichTextBox 在加载/保存 RTF 时
                // 会进行规范化处理（如字体表重排、空白字符标准化等），导致保存后的RTF与原始RTF不一致
                return _originalHelpDocumentRtf != currentRtf;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IsHelpDocumentModified check failed: {ex.Message}");
                return false;
            }
        }

        private string NormalizeJsonForComparison(string json)
        {
            if (string.IsNullOrEmpty(json)) return json;
            // 忽略与字体相关的属性变更,替换为占位符，以确保一致性
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
                
                // 清空当前大类名称，显示默认的“模板库”标题
                CurrentCategoryName = string.Empty;
                
                // 清除 TreeView 选中项
                ClearTreeViewSelection(GraphMapTemplateNode);
            }
            else
            {
                // 获取对应的节点
                var targetNode = item.Node;

                await ShowCategoryTemplateCards(targetNode);
                
                // 同步 TreeView 选中状态
                SyncTreeViewSelection(targetNode);
            }

            SaveTemplateLibraryState();
        }

        /// <summary>
        /// 清除 TreeView 所有节点的选中状态
        /// </summary>
        private void ClearTreeViewSelection(GraphMapTemplateNode node)
        {
            if (node == null) return;

            // 取消当前节点的选中状态
            node.IsSelected = false;

            // 递归清除所有子节点的选中状态
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    ClearTreeViewSelection(child);
                }
            }
        }

        /// <summary>
        /// 同步 TreeView 选中状态到指定节点
        /// </summary>
        private void SyncTreeViewSelection(GraphMapTemplateNode targetNode)
        {
            if (targetNode == null) return;

            // 先清除所有节点的选中状态
            ClearTreeViewSelection(GraphMapTemplateNode);

            // 设置目标节点为选中状态
            targetNode.IsSelected = true;

            // 确保父节点展开，以便目标节点可见
            var parent = targetNode.Parent;
            while (parent != null)
            {
                parent.IsExpanded = true;
                parent = parent.Parent;
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
            // 拦截 null 值：用 EmptyPropertyModel 替代，确保 ContentControl 始终通过
            // DataTemplate 渲染而非切换 ControlTemplate（避免 WPF Template Trigger 问题）
            if (newValue == null)
            {
                // 在替换为 nullObject 之前，先取消订阅旧对象的事件
                // 因为下面的赋值会递归触发本方法，届时 oldValue 将为 null 而无法正确取消订阅
                if (oldValue is INotifyPropertyChanged oldNotify)
                {
                    oldNotify.PropertyChanged -= PropertyGridModel_PropertyChanged;
                }
                PropertyGridModel = nullObject;
                return; // 重新赋值会再次触发此方法，此处直接返回
            }

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
                    // 选中项：蜘蛛图数据图层保持原样式，其余恢复正常
                    if (layer is SpiderSampleLayerItemViewModel spiderLayer)
                    {
                        spiderLayer.Restore();
                    }
                    else
                    {
                        layer.Restore();
                    }
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
            if (sender == null || string.IsNullOrWhiteSpace(e.PropertyName)) return;

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

            // 2. 蛛网图模式：样品样式保留即时更新，其余属性走模板重绘
            if (SpiderDiagramViewModel.IsSpiderPlotMode)
            {
                if (sender is SpiderSamplePropertyModel or SpiderAxisPropertyModel)
                {
                    SchedulePropertyEditRefresh(PropertyEditRefreshMode.StyleOnly);
                }
                else if (IsTemplateAppearanceModel(sender))
                {
                    SchedulePropertyEditRefresh(PropertyEditRefreshMode.TemplateAppearanceOnly);
                }
                else
                {
                    bool preserveSpiderLimits = !(sender is Models.CartesianAxisDefinition &&
                        (e.PropertyName == "Minimum" || e.PropertyName == "Maximum" || e.PropertyName == "IsAutoRange"));
                    SchedulePropertyEditRefresh(
                        preserveSpiderLimits ? PropertyEditRefreshMode.FullPreserveLimits : PropertyEditRefreshMode.FullResetLimits);
                }
                return;
            }

            // 3. 决定是否保留当前视图范围
            // 如果修改的是坐标轴范围，则不保留（使用新设置的范围），否则保留当前平移缩放状态
            bool preserveLimits = true;
            if (sender is Models.CartesianAxisDefinition && 
                (e.PropertyName == "Minimum" || e.PropertyName == "Maximum" || e.PropertyName == "IsAutoRange"))
            {
                preserveLimits = false;
            }

            // 4. 合并连续属性编辑导致的频繁重绘
            if (IsTemplateAppearanceModel(sender))
            {
                SchedulePropertyEditRefresh(PropertyEditRefreshMode.TemplateAppearanceOnly);
            }
            else
            {
                SchedulePropertyEditRefresh(
                    preserveLimits ? PropertyEditRefreshMode.FullPreserveLimits : PropertyEditRefreshMode.FullResetLimits);
            }
        }

        /// <summary>
        /// 创建属性编辑刷新计时器，将连续属性变更合并为一次绘图刷新
        /// </summary>
        private System.Windows.Threading.DispatcherTimer CreatePropertyEditRefreshTimer()
        {
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(PropertyEditRefreshDelayMs)
            };
            timer.Tick += PropertyEditRefreshTimer_Tick;
            return timer;
        }

        private void SchedulePropertyEditRefresh(PropertyEditRefreshMode refreshMode, bool checkUnsavedChanges = true)
        {
            if (refreshMode > _pendingPropertyEditRefreshMode)
            {
                _pendingPropertyEditRefreshMode = refreshMode;
            }

            _pendingPropertyEditUnsavedCheck |= checkUnsavedChanges;

            _propertyEditRefreshTimer.Stop();
            _propertyEditRefreshTimer.Start();
        }

        private void PropertyEditRefreshTimer_Tick(object? sender, EventArgs e)
        {
            _propertyEditRefreshTimer.Stop();

            var refreshMode = _pendingPropertyEditRefreshMode;
            var shouldCheckUnsavedChanges = _pendingPropertyEditUnsavedCheck;

            _pendingPropertyEditRefreshMode = PropertyEditRefreshMode.None;
            _pendingPropertyEditUnsavedCheck = false;

            if (WpfPlot1 == null)
            {
                return;
            }

            switch (refreshMode)
            {
                case PropertyEditRefreshMode.FullResetLimits:
                    RefreshPlotFromLayers(false);
                    break;
                case PropertyEditRefreshMode.FullPreserveLimits:
                    RefreshPlotFromLayers(true);
                    break;
                case PropertyEditRefreshMode.TemplateAppearanceOnly:
                    RefreshCurrentTemplateAppearance();
                    break;
                case PropertyEditRefreshMode.StyleOnly:
                    WpfPlot1.Refresh();
                    break;
                case PropertyEditRefreshMode.None:
                default:
                    break;
            }

            if (shouldCheckUnsavedChanges)
            {
                CheckUnsavedChanges();
            }
        }

        private System.Windows.Threading.DispatcherTimer CreateLayerRefreshTimer()
        {
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(LayerRefreshDelayMs)
            };
            timer.Tick += LayerRefreshTimer_Tick;
            return timer;
        }

        private void OnLayerRequestRefreshPreserveLimits(object? sender, EventArgs e) => ScheduleLayerRefresh(true);

        private void OnLayerRequestRefreshResetLimits(object? sender, EventArgs e) => ScheduleLayerRefresh(false);

        private void OnLayerRequestStyleUpdate(object? sender, EventArgs e) => WpfPlot1?.Refresh();

        private void EnsureLayerRefreshHandler(LayerItemViewModel layer, bool preserveAxisLimits = true)
        {
            layer.RequestRefresh -= OnLayerRequestRefreshPreserveLimits;
            layer.RequestRefresh -= OnLayerRequestRefreshResetLimits;
            layer.RequestRefresh += preserveAxisLimits
                ? OnLayerRequestRefreshPreserveLimits
                : OnLayerRequestRefreshResetLimits;
        }

        private void EnsureLayerStyleUpdateHandler(LayerItemViewModel layer)
        {
            layer.RequestStyleUpdate -= OnLayerRequestStyleUpdate;
            layer.RequestStyleUpdate += OnLayerRequestStyleUpdate;
        }

        private void AttachLayerPlotHandlers(LayerItemViewModel layer, bool preserveAxisLimits)
        {
            EnsureLayerRefreshHandler(layer, preserveAxisLimits);
            EnsureLayerStyleUpdateHandler(layer);
        }

        /// <summary>
        /// 清空图层树前先退订事件，避免悬挂回调累积。
        /// </summary>
        private void ClearLayerTree()
        {
            foreach (var root in LayerTree)
                root.ClearEventSubscriptions();
            LayerTree.Clear();
        }

        private static void ClearLayerChildren(LayerItemViewModel parent)
        {
            foreach (var child in parent.Children)
                child.ClearEventSubscriptions();
            parent.Children.Clear();
        }

        private void RemoveLayerSubtree(LayerItemViewModel node)
        {
            node.ClearEventSubscriptions();
            LayerTree.Remove(node);
        }

        private void ScheduleLayerRefresh(bool preserveAxisLimits = true)
        {
            if (!preserveAxisLimits)
            {
                _pendingLayerRefreshPreserveLimits = false;
            }

            _layerRefreshTimer.Stop();
            _layerRefreshTimer.Start();
        }

        private void LayerRefreshTimer_Tick(object? sender, EventArgs e)
        {
            _layerRefreshTimer.Stop();

            var preserveAxisLimits = _pendingLayerRefreshPreserveLimits;
            _pendingLayerRefreshPreserveLimits = true;

            RefreshPlotFromLayers(preserveAxisLimits);
        }

        private void InvalidateSnapPointsCache()
        {
            _isSnapDataPointsCacheValid = false;
            _cachedSnapDataPoints = null;
            _lastSnapSearchHasResult = false;
            _lastSnapSearchResult = null;
        }

        private IReadOnlyList<Coordinates> GetSnapDataPoints()
        {
            if (!_isSnapDataPointsCacheValid || _cachedSnapDataPoints == null)
            {
                _cachedSnapDataPoints = CollectSnapDataPoints();
                _isSnapDataPointsCacheValid = true;
            }

            return _cachedSnapDataPoints;
        }

        private List<Coordinates> CollectSnapDataPoints()
        {
            var points = new List<Coordinates>();

            foreach (var layer in FlattenTree(LayerTree).Where(l => l.IsVisible))
            {
                switch (layer)
                {
                    case LineLayerItemViewModel lineLayer:
                        if (lineLayer.LineDefinition?.Start != null)
                        {
                            points.Add(new Coordinates(lineLayer.LineDefinition.Start.X, lineLayer.LineDefinition.Start.Y));
                        }

                        if (lineLayer.LineDefinition?.End != null)
                        {
                            points.Add(new Coordinates(lineLayer.LineDefinition.End.X, lineLayer.LineDefinition.End.Y));
                        }
                        break;

                    case ArrowLayerItemViewModel arrowLayer:
                        if (arrowLayer.ArrowDefinition?.Start != null)
                        {
                            points.Add(new Coordinates(arrowLayer.ArrowDefinition.Start.X, arrowLayer.ArrowDefinition.Start.Y));
                        }

                        if (arrowLayer.ArrowDefinition?.End != null)
                        {
                            points.Add(new Coordinates(arrowLayer.ArrowDefinition.End.X, arrowLayer.ArrowDefinition.End.Y));
                        }
                        break;

                    case PolygonLayerItemViewModel polygonLayer when polygonLayer.PolygonDefinition?.Vertices != null:
                        foreach (var vertex in polygonLayer.PolygonDefinition.Vertices)
                        {
                            points.Add(new Coordinates(vertex.X, vertex.Y));
                        }
                        break;

                    case ScatterLayerItemViewModel scatterLayer when scatterLayer.DataPoints != null:
                        points.AddRange(scatterLayer.DataPoints);
                        break;

                    case TextLayerItemViewModel textLayer when textLayer.TextDefinition?.StartAndEnd != null:
                        points.Add(new Coordinates(
                            textLayer.TextDefinition.StartAndEnd.X,
                            textLayer.TextDefinition.StartAndEnd.Y));
                        break;
                }
            }

            return points;
        }

        private void RebuildPlottableLayerLookup()
        {
            _plottableLayerLookup.Clear();

            foreach (var layer in FlattenTree(LayerTree))
            {
                if (layer.Plottable != null)
                {
                    _plottableLayerLookup[layer.Plottable] = layer;
                }

                switch (layer)
                {
                    case ScatterLayerItemViewModel scatterLayer:
                        scatterLayer.RegisterPlottablesForLookup(_plottableLayerLookup);
                        break;
                    case SpiderSampleLayerItemViewModel spiderLayer:
                        spiderLayer.RegisterPlottablesForLookup(_plottableLayerLookup);
                        break;
                }
            }
        }

        private static bool IsTemplateAppearanceModel(object sender)
        {
            return sender is Models.TitleDefinition
                or Models.LegendDefinition
                or Models.GridDefinition;
        }

        private void RefreshCurrentTemplateAppearance()
        {
            if (WpfPlot1 == null || CurrentTemplate == null)
            {
                return;
            }

            BaseMapType = CurrentTemplate.TemplateType;
            ConfigureGridDefinitionCapabilities(CurrentTemplate);

            if (CurrentTemplate.TemplateType == "Spider")
            {
                AutoDetectSpiderFonts();
            }

            ApplyCurrentTemplateAppearanceToPlot();
            WpfPlot1.Refresh();
        }

        /// <summary>
        /// 使用JavaScript脚本计算坐标
        /// </summary>
        /// <param name="dataRow">数据行</param>
        /// <param name="dataColumns">参与计算的数据列名</param>
        /// <returns>返回计算结果的double数组,如果计算失败或返回类型不正确则返回null</returns>
        private double[]? CalculateCoordinatesUsingScript(Jint.Engine engine, DataRow dataRow, List<string> dataColumns, out string? invalidReason)
        {
            invalidReason = null;
            try
            {
                if (!TrySetCoordinateScriptInputs(engine, dataRow, dataColumns, out invalidReason))
                {
                    return null;
                }
        
                if (!_usePreparedCoordinateScript)
                {
                    invalidReason = Lang("plot_script_must_return_array_explicitly", "Scripts must explicitly return an array, for example: return [x, y];");
                    return null;
                }

                // 执行脚本并获取结果
                var result = JintHelper.InvokePreparedCoordinateScript(engine);
        
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
                            // 如果数组中有任何一个元素不是数字,则认为结果无效
                            invalidReason = Lang("plot_script_result_non_numeric", "Script result contains non-numeric values.");
                            return null;
                        }
                    }

                    if (!TryValidateFiniteValues(values, out invalidReason))
                    {
                        return null;
                    }

                    return values; // 返回包含所有数值的数组
                }
        
                invalidReason = Lang("plot_script_result_not_array", "Script result is not an array.");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(LanguageService.Instance["script_execution_failed"] + ex.Message);
                invalidReason = $"{Lang("plot_script_execution_failed_with_reason", "Script execution failed")}: {ex.Message}";
                return null;
            }
        }

        private static bool TrySetCoordinateScriptInputs(Jint.Engine engine, DataRow dataRow, IEnumerable<string> dataColumns, out string? invalidReason)
        {
            invalidReason = null;

            foreach (string columnName in dataColumns)
            {
                string? rawValue = dataRow[columnName]?.ToString();
                if (!TryParseCoordinateScriptInput(rawValue, out double value))
                {
                    invalidReason = string.Format(
                        Lang("plot_script_input_value_invalid", "Input value for column \"{0}\" is missing or not numeric."),
                        columnName);
                    return false;
                }

                engine.SetValue(columnName, value);
            }

            return true;
        }

        private static int FindWorksheetColumnIndex(Worksheet worksheet, string columnName)
        {
            for (int c = 0; c < worksheet.ColumnCount; c++)
            {
                var headerText = worksheet.ColumnHeaders[c].Text;
                if (string.Equals(headerText, columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return c;
                }
            }

            return -1;
        }

        private static bool RowHasAnyParseableScriptInput(Worksheet worksheet, int rowIndex, IReadOnlyList<string> dataColumns)
        {
            foreach (string columnName in dataColumns)
            {
                int colIndex = FindWorksheetColumnIndex(worksheet, columnName);
                if (colIndex < 0)
                {
                    continue;
                }

                string? rawValue = worksheet[rowIndex, colIndex]?.ToString();
                if (TryParseCoordinateScriptInput(rawValue, out _))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TrySetCoordinateScriptInputs(Jint.Engine engine, Worksheet worksheet, int rowIndex, IEnumerable<string> dataColumns, out string? invalidReason)
        {
            invalidReason = null;

            foreach (string columnName in dataColumns)
            {
                int colIndex = FindWorksheetColumnIndex(worksheet, columnName);

                if (colIndex < 0)
                {
                    invalidReason = string.Format(
                        Lang("plot_script_input_column_missing", "Required column \"{0}\" was not found."),
                        columnName);
                    return false;
                }

                string? rawValue = worksheet[rowIndex, colIndex]?.ToString();
                if (!TryParseCoordinateScriptInput(rawValue, out double value))
                {
                    invalidReason = string.Format(
                        Lang("plot_script_input_value_invalid", "Input value for column \"{0}\" is missing or not numeric."),
                        columnName);
                    return false;
                }

                engine.SetValue(columnName, value);
            }

            return true;
        }

        private static bool TryParseCoordinateScriptInput(string? rawValue, out double value)
        {
            value = default;
            return !string.IsNullOrWhiteSpace(rawValue) && double.TryParse(rawValue, out value) && IsFiniteValue(value);
        }

        private static bool IsFiniteValue(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool TryValidateFiniteValues(IReadOnlyList<double> values, out string? invalidReason)
        {
            invalidReason = null;

            for (int i = 0; i < values.Count; i++)
            {
                double value = values[i];
                if (!IsFiniteValue(value))
                {
                    invalidReason = string.Format(
                        Lang("plot_script_result_contains_invalid_number", "Script result contains {0}; a valid coordinate cannot be generated."),
                        FormatInvalidNumber(value));
                    return false;
                }
            }

            return true;
        }

        private static string FormatInvalidNumber(double value)
        {
            if (double.IsNaN(value)) return "NaN";
            if (double.IsPositiveInfinity(value)) return "Infinity";
            if (double.IsNegativeInfinity(value)) return "-Infinity";
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string BuildInvalidCoordinateHint(string? detail = null)
        {
            var message = Lang("plot_invalid_coordinate_hint", "This row's script result is invalid, so a valid coordinate cannot be generated. Check for division by zero, empty values, logarithms of negative numbers, or square roots of negative numbers.");
            return string.IsNullOrWhiteSpace(detail) ? message : $"{message} {detail}";
        }

        private static bool TryReadFiniteArray(Jint.Native.JsValue result, int requiredLength, out double[] values, out string? invalidReason)
        {
            values = Array.Empty<double>();
            invalidReason = null;

            if (!result.IsArray())
            {
                invalidReason = Lang("plot_script_result_not_array", "Script result is not an array.");
                return false;
            }

            var array = result.AsArray();
            if (array.Length < requiredLength)
            {
                invalidReason = requiredLength == 3
                    ? Lang("ternary_script_return_value_requirement", "Ternary diagram scripts must return an array with at least 3 values [A, B, C].")
                    : Lang("script_return_two_values_requirement", "Scripts must return an array with at least 2 values [X, Y].");
                return false;
            }

            values = new double[requiredLength];
            for (int i = 0; i < requiredLength; i++)
            {
                if (!array[i].IsNumber())
                {
                    invalidReason = Lang("plot_script_result_non_numeric", "Script result contains non-numeric values.");
                    return false;
                }

                values[i] = array[i].AsNumber();
            }

            return TryValidateFiniteValues(values, out invalidReason);
        }

        private static string FormatSkippedRowsWarning(IReadOnlyList<string> validationErrors, int skippedCount)
        {
            var builder = new StringBuilder();
            builder.Append(string.Format(
                Lang("plot_invalid_rows_summary", "{0} rows were not plotted because their script results are not valid coordinates. They have been marked in the data table; please check their input values."),
                skippedCount));

            if (validationErrors.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
                builder.AppendLine(Lang("plot_invalid_rows_examples", "Examples:"));
                foreach (var error in validationErrors.Take(5))
                {
                    builder.AppendLine(error);
                }

                if (skippedCount > 5)
                {
                    builder.AppendLine(string.Format(
                        Lang("plot_invalid_rows_more", "{0} additional rows have similar issues."),
                        skippedCount - 5));
                }
            }

            return builder.ToString().TrimEnd();
        }

        private void MarkScriptInvalidRow(Worksheet worksheet, int rowIndex)
        {
            if (worksheet == null || rowIndex < 0 || rowIndex >= worksheet.RowCount)
            {
                return;
            }

            if (!_scriptInvalidRowVisualSnapshots.ContainsKey(rowIndex))
            {
                var snapshots = new List<ScriptInvalidCellVisualSnapshot>();
                for (int col = 0; col < worksheet.ColumnCount; col++)
                {
                    var style = worksheet.GetCellStyles(rowIndex, col);
                    var hasBackColor = style != null && style.HasStyle(PlainStyleFlag.BackColor);
                    var borders = worksheet.GetRangeBorders(new RangePosition(rowIndex, col, 1, 1), BorderPositions.All, true);
                    snapshots.Add(new ScriptInvalidCellVisualSnapshot
                    {
                        Column = col,
                        HasBackColor = hasBackColor,
                        BackColor = hasBackColor ? ToMediaColor(style.BackColor) : System.Windows.Media.Colors.Transparent,
                        TopBorder = borders.Top,
                        RightBorder = borders.Right,
                        BottomBorder = borders.Bottom,
                        LeftBorder = borders.Left
                    });
                }

                _scriptInvalidRowVisualSnapshots[rowIndex] = snapshots;
            }

            _scriptInvalidRows.Add(rowIndex);
            for (int col = 0; col < worksheet.ColumnCount; col++)
            {
                ApplyScriptInvalidRowBackColor(worksheet, rowIndex, col, System.Windows.Media.Color.FromArgb(36, 255, 0, 0));
                ApplyScriptInvalidRowGridLines(worksheet, rowIndex, col);
            }
        }

        private void ClearScriptInvalidRowMarks(Worksheet worksheet)
        {
            if (worksheet == null || _scriptInvalidRows.Count == 0)
            {
                _scriptInvalidRows.Clear();
                _scriptInvalidRowVisualSnapshots.Clear();
                return;
            }

            foreach (var rowIndex in _scriptInvalidRows.ToList())
            {
                if (rowIndex >= 0 && rowIndex < worksheet.RowCount)
                {
                    if (_scriptInvalidRowVisualSnapshots.TryGetValue(rowIndex, out var snapshots))
                    {
                        foreach (var snapshot in snapshots)
                        {
                            if (snapshot.Column >= 0 && snapshot.Column < worksheet.ColumnCount)
                            {
                                var range = new RangePosition(rowIndex, snapshot.Column, 1, 1);
                                if (snapshot.HasBackColor)
                                {
                                    ApplyScriptInvalidRowBackColor(worksheet, rowIndex, snapshot.Column, snapshot.BackColor);
                                }
                                else
                                {
                                    worksheet.RemoveRangeStyles(range, PlainStyleFlag.BackColor);
                                }

                                RestoreScriptInvalidRowBorder(worksheet, range, BorderPositions.Top, snapshot.TopBorder);
                                RestoreScriptInvalidRowBorder(worksheet, range, BorderPositions.Right, snapshot.RightBorder);
                                RestoreScriptInvalidRowBorder(worksheet, range, BorderPositions.Bottom, snapshot.BottomBorder);
                                RestoreScriptInvalidRowBorder(worksheet, range, BorderPositions.Left, snapshot.LeftBorder);
                            }
                        }
                    }
                }
            }

            _scriptInvalidRows.Clear();
            _scriptInvalidRowVisualSnapshots.Clear();
        }

        private static void ApplyScriptInvalidRowBackColor(Worksheet worksheet, int rowIndex, int columnIndex, System.Windows.Media.Color color)
        {
            worksheet.SetRangeStyles(new RangePosition(rowIndex, columnIndex, 1, 1), new WorksheetRangeStyle
            {
                Flag = PlainStyleFlag.BackColor,
                BackColor = ToReoGridColor(color)
            });
        }

        private static void ApplyScriptInvalidRowGridLines(Worksheet worksheet, int rowIndex, int columnIndex)
        {
            worksheet.SetRangeBorders(
                new RangePosition(rowIndex, columnIndex, 1, 1),
                BorderPositions.All,
                RangeBorderStyle.SilverSolid);
        }

        private static void RestoreScriptInvalidRowBorder(Worksheet worksheet, RangePosition range, BorderPositions position, RangeBorderStyle borderStyle)
        {
            if (borderStyle.IsEmpty)
            {
                worksheet.RemoveRangeBorders(range, position);
                return;
            }

            worksheet.SetRangeBorders(range, position, borderStyle);
        }

        private static System.Windows.Media.Color ToMediaColor(unvell.ReoGrid.Graphics.SolidColor color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        private static unvell.ReoGrid.Graphics.SolidColor ToReoGridColor(System.Windows.Media.Color color)
        {
            return unvell.ReoGrid.Graphics.SolidColor.FromArgb(color.A, color.R, color.G, color.B);
        }
        
        /// <summary>
        /// 计算并显示当前选中行的计算结果
        /// </summary>
        /// <param name="rowIndex">选中的行号</param>
        private void CalculateAndDisplayResult(int rowIndex)
        {
            // 重置状态
            CalculationResultSummary = string.Empty;
            CalculationLogs.Clear();
            IsCalculationVerificationVisible = false;
        
            // 检查是否为支持的坐标系类型（二维坐标系或三元图）
            if (CurrentTemplate == null || 
                (CurrentTemplate.TemplateType != "Cartesian" && CurrentTemplate.TemplateType != "Ternary"))
            {
                return;
            }
        
            // 检查是否有脚本定义
            if (CurrentTemplate.Script == null || string.IsNullOrWhiteSpace(CurrentTemplate.Script.ScriptBody))
            {
                return;
            }
        
            // 获取数据表
            if (_dataGrid == null || _dataGrid.Worksheets.Count == 0)
            {
                return;
            }
        
            var sheet = _dataGrid.Worksheets[0];
            if (rowIndex < 0 || rowIndex >= sheet.RowCount)
            {
                return;
            }

            if (_scriptInvalidRows.Contains(rowIndex))
            {
                IsCalculationVerificationVisible = true;
                CalculationResultSummary = Lang("plot_row_script_result_invalid", "This row's script result is invalid.");
                CalculationLogs.Add(BuildInvalidCoordinateHint());
                return;
            }
            
            try
            {
                // 获取需要的数据列名（排除 Category 等分组/metadata 列）
                var requiredSeriesStr = CurrentTemplate.Script.RequiredDataSeries;
                if (string.IsNullOrWhiteSpace(requiredSeriesStr))
                {
                    return;
                }

                var dataColumns = PlotDataGridHelper.ParseScriptDataColumns(requiredSeriesStr);
                if (dataColumns.Count == 0)
                {
                    CalculationResultSummary = Lang("plot_undefined_data_columns", "Undefined data columns");
                    IsCalculationVerificationVisible = true;
                    return;
                }

                // 仅当脚本数值列存在可解析输入时才显示验证；Category 等非数值分组内容不触发报错
                if (!RowHasAnyParseableScriptInput(sheet, rowIndex, dataColumns))
                {
                    return;
                }

                // 显示计算验证区域
                IsCalculationVerificationVisible = true;
        
                // 准备日志收集列表
                var logs = new List<string>();
        
                // 创建Jint引擎并注入trace函数
                var engine = new Jint.Engine();
                JintHelper.InjectTraceFunction(engine, logs);

                if (!TrySetCoordinateScriptInputs(engine, sheet, rowIndex, dataColumns, out var inputInvalidReason))
                {
                    CalculationResultSummary = Lang("plot_row_script_result_invalid", "This row's script result is invalid.");
                    CalculationLogs.Add(BuildInvalidCoordinateHint(inputInvalidReason));
                    return;
                }

                // 执行脚本并获取结果
                if (!JintHelper.TryPrepareCoordinateScript(engine, CurrentTemplate.Script.ScriptBody))
                {
                    CalculationResultSummary = Lang("plot_row_script_result_invalid", "This row's script result is invalid.");
                    CalculationLogs.Add(BuildInvalidCoordinateHint(
                        Lang("plot_script_must_return_array_explicitly", "Scripts must explicitly return an array, for example: return [x, y];")));
                    return;
                }

                var result = JintHelper.InvokePreparedCoordinateScript(engine);

                int requiredLength = CurrentTemplate.TemplateType == "Ternary" ? 3 : 2;
                if (!TryReadFiniteArray(result, requiredLength, out var finiteValues, out var invalidReason))
                {
                    CalculationResultSummary = Lang("plot_row_script_result_invalid", "This row's script result is invalid.");
                    CalculationLogs.Add(BuildInvalidCoordinateHint(invalidReason));
                    return;
                }

                // 检查返回结果是否为数组
                if (result.IsArray())
                {
                    var array = result.AsArray();
                    
                    // 根据坐标系类型显示不同格式的结果
                    if (CurrentTemplate.TemplateType == "Ternary")
                    {
                        // 三元图需要3个值：A, B, C
                        if (array.Length >= 3)
                        {
                            double a = finiteValues[0];
                            double b = finiteValues[1];
                            double c = finiteValues[2];

                            // 格式化结果摘要（三元图使用 A, B, C）
                            CalculationResultSummary = LanguageService.Instance["calculation_results"] +
                                $": A = {a:F4}, B = {b:F4}, C = {c:F4}";
        
                            // 更新日志
                            foreach (var log in logs)
                            {
                                CalculationLogs.Add(log);
                            }
        
                            // 如果没有日志,显示默认提示
                            if (CalculationLogs.Count == 0)
                            {
                                // 该脚本未提供详细计算过程说明
                                CalculationLogs.Add(LanguageService.Instance["script_no_detailed_calculation_description"]);
                                // 可在脚本中使用 trace(...) 函数记录计算过程
                                CalculationLogs.Add(LanguageService.Instance["trace_function_usage_tip"]);
                            }
                        }
                        else
                        {
                            // 脚本返回值不足
                            CalculationResultSummary = LanguageService.Instance["insufficient_script_return_values"];
                            // 三元图脚本必须返回至少 3 个值的数组 [A, B, C]
                            CalculationLogs.Add(LanguageService.Instance["ternary_script_return_value_requirement"]);
                        }
                    }
                    else // Cartesian
                    {
                        // 二维坐标系需要2个值：X, Y
                        if (array.Length >= 2)
                        {
                            double x = finiteValues[0];
                            double y = finiteValues[1];

                            // 格式化结果摘要（二维坐标系使用 X, Y）
                            CalculationResultSummary = LanguageService.Instance["calculation_results"] + 
                                $": X = {x:F4}, Y = {y:F4}";
        
                            // 更新日志
                            foreach (var log in logs)
                            {
                                CalculationLogs.Add(log);
                            }
        
                            // 如果没有日志,显示默认提示
                            if (CalculationLogs.Count == 0)
                            {
                                // 该脚本未提供详细计算过程说明
                                CalculationLogs.Add(LanguageService.Instance["script_no_detailed_calculation_description"]);
                                // 可在脚本中使用 trace(...) 函数记录计算过程
                                CalculationLogs.Add(LanguageService.Instance["trace_function_usage_tip"]);
                            }
                        }
                        else
                        {
                            // 脚本返回值不足
                            CalculationResultSummary = LanguageService.Instance["insufficient_script_return_values"];
                            // 脚本必须返回至少 2 个值的数组 [X, Y]
                            CalculationLogs.Add(LanguageService.Instance["script_return_two_values_requirement"]);
                        }
                    }
                }
                else
                {
                    // 脚本返回类型错误
                    CalculationResultSummary = LanguageService.Instance["script_return_type_error"];
                    if (CurrentTemplate.TemplateType == "Ternary")
                    {
                        // 三元图脚本必须返回至少 3 个值的数组 [A, B, C]
                        CalculationLogs.Add(LanguageService.Instance["ternary_script_return_value_requirement"]);
                    }
                    else
                    {
                        // 脚本必须返回至少 2 个值的数组 [X, Y]
                        CalculationLogs.Add(LanguageService.Instance["script_return_two_values_requirement"]);
                    }
                }
            }
            catch (Exception ex)
            {
                // 计算错误
                CalculationResultSummary = LanguageService.Instance["calculation_error"];
                CalculationLogs.Add(LanguageService.Instance["error"] + $"{ex.Message}");
            }
        }
        
        /// <summary>
        /// 切换计算过程详情展开/收缩状态
        /// </summary>
        [RelayCommand]
        private void ToggleCalculationDetail()
        {
            IsCalculationDetailExpanded = !IsCalculationDetailExpanded;
        }

        /// <summary>
        /// 从数据库加载模板、构建图层树并刷新绘图
        /// </summary>
        private async Task<bool> LoadAndBuildLayersFromDb(Guid templateId)
        {
            CancelPendingPlotRefreshes();

            // 重置编辑确认状态和标签页索引
            SetHasConfirmedEditMode(false);
            RibbonTabIndex = 0;

            var entity = await Task.Run(() => GraphMapDatabaseService.Instance.GetTemplate(templateId));

            if (entity == null || entity.Content == null)
            {
                MessageHelper.Error(LanguageService.Instance["diagram_template_corrupted_or_invalid"]);
                return false;
            }

            if (!GraphMapTemplateService.IsValidDiagramTemplateContent(entity.Content))
            {
                MessageHelper.Error(LanguageService.Instance["diagram_template_corrupted_or_invalid"]);
                return false;
            }

            if (!GraphMapTemplateService.IsVersionCompatible(entity.Content))
            {
                MessageHelper.Error(LanguageService.Instance["template_version_too_high"]);
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

            // 初始化网格支持属性
            ConfigureGridDefinitionCapabilities(CurrentTemplate);

            // 自动根据内容检测并设置字体
            AutoDetectFonts();

            // 根据加载的模板数据，构建【图层树】
            BuildLayerTreeFromTemplate(CurrentTemplate);

            // 根据新建的【图层树】来渲染前端
            RefreshPlotFromLayers();
            
            // 在所有初始化完成后，再记录原始状态
            _originalTemplateJson = SerializeTemplate(CurrentTemplate);
            HasUnsavedChanges = false;
            
            return true;
        }

        /// <summary>
        /// 加载模板、构建图层树并刷新绘图
        /// </summary>
        /// <param name="templatePath">模板文件的路径</param>
        private async Task<bool> LoadAndBuildLayers(string templatePath)
        {
            CancelPendingPlotRefreshes();

            // 重置编辑确认状态和标签页索引
            SetHasConfirmedEditMode(false);
            RibbonTabIndex = 0;

            if (!File.Exists(templatePath))
            {
                // 文件不存在
                return false;
            }

            // 读取并反序列化模板文件
            var templateJsonContent = await File.ReadAllTextAsync(templatePath);
            if (!GraphMapTemplateService.TryParseDiagramTemplate(templateJsonContent, out var template))
            {
                MessageHelper.Error(LanguageService.Instance["diagram_template_corrupted_or_invalid"]);
                return false;
            }

            if (!GraphMapTemplateService.IsVersionCompatible(template))
            {
                MessageHelper.Error(LanguageService.Instance["template_version_too_high"]);
                return false;
            }

            CurrentTemplate = template;

            // 初始化网格支持属性
            ConfigureGridDefinitionCapabilities(CurrentTemplate);

            // 自动根据内容检测并设置字体 (覆盖 JSON 中的设置)
            AutoDetectFonts();

            // 根据加载的模板数据，构建【图层树】
            BuildLayerTreeFromTemplate(CurrentTemplate);

            // 根据新建的【图层树】来渲染前端
            RefreshPlotFromLayers();

            // 在所有初始化完成后，再记录原始状态
            // 注意：必须在 AutoDetectFonts / BuildLayerTreeFromTemplate 等修改之后记录，
            // 否则这些修改会导致 IsTemplateModified 误判
            _originalTemplateJson = SerializeTemplate(CurrentTemplate);
            HasUnsavedChanges = false;

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
            var axesCategory = new CategoryLayerItemViewModel(LanguageService.Instance["axes"], LayerTreeIconKind.Axis);
            if (attachEvents) EnsureLayerRefreshHandler(axesCategory);
            foreach (var axis in info.Axes)
            {
                var axisLayer = new AxisLayerItemViewModel(axis, DiagramLanguage);
                if (attachEvents)
                    AttachLayerPlotHandlers(axisLayer, preserveAxisLimits: true);
                axesCategory.Children.Add(axisLayer);
            }
            if (axesCategory.Children.Any()) list.Add(axesCategory);

            // 2. 多边形图层
            var polygonsCategory = new CategoryLayerItemViewModel(LanguageService.Instance["polygon"], LayerTreeIconKind.Polygon);
            if (attachEvents) EnsureLayerRefreshHandler(polygonsCategory);
            for (int i = 0; i < info.Polygons.Count; i++)
            {
                var polygonLayer = new PolygonLayerItemViewModel(info.Polygons[i], i);
                if (attachEvents)
                    AttachLayerPlotHandlers(polygonLayer, preserveAxisLimits: false);
                polygonsCategory.Children.Add(polygonLayer);
            }
            if (polygonsCategory.Children.Any()) list.Add(polygonsCategory);

            // 3. 线图层
            var linesCategory = new CategoryLayerItemViewModel(LanguageService.Instance["line"], LayerTreeIconKind.Line);
            if (attachEvents) EnsureLayerRefreshHandler(linesCategory);
            for (int i = 0; i < info.Lines.Count; i++)
            {
                var lineLayer = new LineLayerItemViewModel(info.Lines[i], i);
                if (attachEvents)
                    AttachLayerPlotHandlers(lineLayer, preserveAxisLimits: false);
                linesCategory.Children.Add(lineLayer);
            }
            if (linesCategory.Children.Any()) list.Add(linesCategory);

            // 4. 函数图层
            var functionCategory = new CategoryLayerItemViewModel("Function", LayerTreeIconKind.Function); // Consider localization
            if (attachEvents) EnsureLayerRefreshHandler(functionCategory);
            for (int i = 0; i < info.Functions.Count; i++)
            {
                var funcLayer = new FunctionLayerItemViewModel(info.Functions[i], i);
                if (attachEvents)
                {
                    EnsureLayerRefreshHandler(funcLayer);
                };
                functionCategory.Children.Add(funcLayer);
            }
            if (functionCategory.Children.Any()) list.Add(functionCategory);

            // 5. 点图层 (位于线条之上，在文本之下)
            var pointsCategory = new CategoryLayerItemViewModel(LanguageService.Instance["point"], LayerTreeIconKind.Point);
            if (attachEvents) EnsureLayerRefreshHandler(pointsCategory);
            for (int i = 0; i < info.Points.Count; i++)
            {
                var pointLayer = new PointLayerItemViewModel(info.Points[i], i);
                if (attachEvents)
                    AttachLayerPlotHandlers(pointLayer, preserveAxisLimits: false);
                pointsCategory.Children.Add(pointLayer);
            }
            if (pointsCategory.Children.Any()) list.Add(pointsCategory);

            // 6. 箭头图层
            var arrowsCategory = new CategoryLayerItemViewModel(LanguageService.Instance["arrow"], LayerTreeIconKind.Arrow);
            if (attachEvents) EnsureLayerRefreshHandler(arrowsCategory);
            for (int i = 0; i < template.Info.Arrows.Count; i++)
            {
                var arrowLayer = new ArrowLayerItemViewModel(template.Info.Arrows[i], i);
                if (attachEvents)
                    AttachLayerPlotHandlers(arrowLayer, preserveAxisLimits: false);
                arrowsCategory.Children.Add(arrowLayer);
            }
            if (arrowsCategory.Children.Any()) list.Add(arrowsCategory);

            // 7. 注释图层
            var annotationCategory = new CategoryLayerItemViewModel(LanguageService.Instance["annotation"], LayerTreeIconKind.Text);
            if (attachEvents) EnsureLayerRefreshHandler(annotationCategory);
            for (int i = 0; i < info.Annotations.Count; i++)
            {
                var annotationLayer = new AnnotationLayerItemViewModel(info.Annotations[i], i);
                if (attachEvents)
                    AttachLayerPlotHandlers(annotationLayer, preserveAxisLimits: false);
                annotationCategory.Children.Add(annotationLayer);
            }
            if (annotationCategory.Children.Any()) list.Add(annotationCategory);

            // 8. 文本图层 (最顶层)
            var textCategory = new CategoryLayerItemViewModel(LanguageService.Instance["text"], LayerTreeIconKind.Text);
            if (attachEvents) EnsureLayerRefreshHandler(textCategory);
            for (int i = 0; i < info.Texts.Count; i++)
            {
                var textLayer = new TextLayerItemViewModel(info.Texts[i], i, DiagramLanguage);
                if (attachEvents)
                    AttachLayerPlotHandlers(textLayer, preserveAxisLimits: false);
                textCategory.Children.Add(textLayer);
            }
            if (textCategory.Children.Any()) list.Add(textCategory);

            return list;
        }

        private void BuildLayerTreeFromTemplate(GraphMapTemplate template)
        {
            ClearLayerTree();
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

            BaseMapType = CurrentTemplate.TemplateType;

            Clockwise = CurrentTemplate.Clockwise;
            ConfigureGridDefinitionCapabilities(CurrentTemplate);

            if (CurrentTemplate.TemplateType == "Spider")
            {
                // 蛛网图标题/轴标题在属性编辑后需要按当前显示文本重新检测字体。
                AutoDetectSpiderFonts();
            }

            // 根据模板类型选择渲染路径
            if (CurrentTemplate.TemplateType == "Ternary")
            {
                RenderTernaryPlot();
            }
            else if (CurrentTemplate.TemplateType == "Spider")
            {
                // 蜘蛛图的数据图层已在 BuildSpiderLayerTree 中通过 RenderSpiderPlot 添加
                // 这里只需要确保坐标轴图层存在
                if (!LayerTree.Any(l => l is CategoryLayerItemViewModel c && c.Children.Any()))
                {
                    BuildLayerTreeFromTemplate(CurrentTemplate);
                }
                // 渲染蜘蛛图
                RenderSpiderPlot();
            }
            else // 默认处理笛卡尔坐标系
            {
                RenderCartesianPlot();
            }

            // 如果处于吸附/绘图模式，重新添加潜在吸附点标记（在渲染之后）
            if (IsPickingPointMode || IsAddingLine || IsAddingArrow || IsAddingPolygon || IsAddingText)
            {
                // 清空旧列表
                _potentialSnapMarkers.Clear();
                // 重新调用 UpdatePotentialSnapPoints(true) 来生成并添加
                UpdatePotentialSnapPoints(true);
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

            RebuildPlottableLayerLookup();
            InvalidateSnapPointsCache();

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
            foreach (var layer in allNodes.OfType<IPlotLayer>().Where(ShouldRenderLayer))
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
                WpfPlot1.Plot.Axes.Title.Label.Text = CurrentTemplate.Info.Title.Label.Get(DiagramLanguage);
                WpfPlot1.Plot.Axes.Title.Label.FontName = CurrentTemplate.Info.Title.Family;
                WpfPlot1.Plot.Axes.Title.Label.FontSize = CurrentTemplate.Info.Title.Size;
                WpfPlot1.Plot.Axes.Title.Label.ForeColor = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(CurrentTemplate.Info.Title.Color));
                WpfPlot1.Plot.Axes.Title.Label.Bold = CurrentTemplate.Info.Title.IsBold;
                WpfPlot1.Plot.Axes.Title.Label.Italic = CurrentTemplate.Info.Title.IsItalic;
            }

            // 设置网格样式
            if (CurrentTemplate?.Info?.Grid != null)
            {
                ApplyGridDefinitionToPlot(WpfPlot1.Plot, CurrentTemplate.Info.Grid);
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
                _triangularAxis.GridLineStyle.AntiAlias = true;

                // 应用背景填充样式
                if (gridDef.GridAlternateFillingIsEnable)
                {
                    // 使用FillColor1作为其填充色
                    _triangularAxis.FillStyle.Color = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(gridDef.GridFillColor1));
                }
                else
                {
                    _triangularAxis.FillStyle.Color = ScottPlot.Colors.Transparent;
                }
            }

            // 全局设置——处理标题
            WpfPlot1.Plot.Axes.Title.IsVisible = true;
            if (CurrentTemplate.Info.Title.Label.Translations.Any())
            {
                WpfPlot1.Plot.Axes.Title.Label.Text = CurrentTemplate.Info.Title.Label.Get(DiagramLanguage);
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
            foreach (var layer in allNodes.OfType<IPlotLayer>().Where(ShouldRenderLayer))
            {
                layer.Render(WpfPlot1.Plot);
            }

            // 三角图需要方形坐标轴以避免变形（先清除旧规则，避免重复叠加）
            WpfPlot1.Plot.Axes.SquareUnits(false);
            WpfPlot1.Plot.Axes.SquareUnits();

            // 显式关闭倒置轴，避免继承上一次绘图的 InvertedY 状态
            WpfPlot1.Plot.Axes.AutoScale(invertX: false, invertY: false);
        }

        /// <summary>
        /// 渲染蜘蛛图（REE/微量元素）
        /// </summary>
        private void RenderSpiderPlot()
        {
            if (WpfPlot1 == null || CurrentTemplate == null) return;

            // 从模板获取蜘蛛图配置
            var spiderAxis = CurrentTemplate.Info.Axes
                .OfType<SpiderAxisDefinition>()
                .FirstOrDefault(a => a.Type == "Bottom")
                ?? CurrentTemplate.Info.Axes.OfType<SpiderAxisDefinition>().FirstOrDefault();
            if (spiderAxis == null) return;

            // 设置标题
            WpfPlot1.Plot.Axes.Title.IsVisible = true;
            if (CurrentTemplate.Info.Title.Label.Translations.Any())
            {
                WpfPlot1.Plot.Axes.Title.Label.Text = CurrentTemplate.Info.Title.Label.Get(DiagramLanguage);
                WpfPlot1.Plot.Axes.Title.Label.FontName = CurrentTemplate.Info.Title.Family;
                WpfPlot1.Plot.Axes.Title.Label.FontSize = CurrentTemplate.Info.Title.Size;
                WpfPlot1.Plot.Axes.Title.Label.ForeColor = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(CurrentTemplate.Info.Title.Color));
                WpfPlot1.Plot.Axes.Title.Label.Bold = CurrentTemplate.Info.Title.IsBold;
                WpfPlot1.Plot.Axes.Title.Label.Italic = CurrentTemplate.Info.Title.IsItalic;
            }

            // 设置图例
            WpfPlot1.Plot.Legend.Alignment = CurrentTemplate.Info.Legend.Alignment;
            WpfPlot1.Plot.Legend.FontName = CurrentTemplate.Info.Legend.Font;
            WpfPlot1.Plot.Legend.Orientation = CurrentTemplate.Info.Legend.Orientation;
            WpfPlot1.Plot.Legend.IsVisible = CurrentTemplate.Info.Legend.IsVisible;

            // 获取元素列表
            var configuredElements = spiderAxis.ElementOrder.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            if (configuredElements.Count == 0) return;

            // 获取标准化方案
            NormalizationStandard? selectedStandard = null;
            if (spiderAxis.IsNormalizationEnabled && !string.IsNullOrEmpty(spiderAxis.NormalizationStandard))
            {
                var allStandards = spiderAxis.SpiderType == "REE"
                    ? NormalizationData.GetReeStandards()
                    : NormalizationData.GetTraceElementStandards();
                selectedStandard = allStandards.FirstOrDefault(s => s.Name == spiderAxis.NormalizationStandard);
            }

            var displayElements = spiderAxis.IsNormalizationEnabled && selectedStandard != null
                ? configuredElements.Where(e => selectedStandard.Values.ContainsKey(e)).ToList()
                : configuredElements.ToList();

            if (displayElements.Count == 0)
            {
                displayElements = configuredElements.ToList();
            }

            // 遍历所有图层节点
            var allNodes = FlattenTree(LayerTree);

            // 刷新渲染绘图对象（样品散点单独处理，坐标轴复用模板图层渲染）
            foreach (var layer in allNodes.OfType<IPlotLayer>().Where(ShouldRenderLayer))
            {
                if (layer is ScatterLayerItemViewModel || layer is SpiderSampleLayerItemViewModel)
                    continue;
                layer.Render(WpfPlot1.Plot);
            }

            if (CurrentTemplate.Info.Grid != null)
            {
                ApplyGridDefinitionToPlot(WpfPlot1.Plot, CurrentTemplate.Info.Grid);
            }

            // 绘制样品数据并同步收集 Y 轴范围，避免后续再次全量扫描
            var spiderLayers = RenderSpiderData(
                displayElements,
                spiderAxis.IsNormalizationEnabled,
                selectedStandard,
                out double? renderedYMin,
                out double? renderedYMax);

            // 将数据图层添加到LayerTree
            var existingDataCategory = LayerTree.FirstOrDefault(l => l is CategoryLayerItemViewModel c && c.Name == (LanguageService.Instance["Data"] ?? "Data"));
            if (spiderLayers.Count > 0)
            {
                // 先清除旧的数据分类中的所有子图层（避免重复）
                if (existingDataCategory != null)
                {
                    ClearLayerChildren(existingDataCategory);
                }

                // 如果不存在数据分类，创建新的
                var dataCategory = existingDataCategory ?? new CategoryLayerItemViewModel(LanguageService.Instance["Data"] ?? "Data", LayerTreeIconKind.Line);
                if (existingDataCategory == null)
                {
                    LayerTree.Add(dataCategory);
                }

                // 添加所有样品图层
                foreach (var spiderLayer in spiderLayers)
                {
                    dataCategory.Children.Add(spiderLayer);
                }
            }
            else if (existingDataCategory != null)
            {
                RemoveLayerSubtree(existingDataCategory);
            }

            ApplySpiderPlotBestView(displayElements, spiderAxis.IsNormalizationEnabled, selectedStandard, renderedYMin, renderedYMax);
        }

        /// <summary>
        /// 渲染蜘蛛图数据
        /// </summary>
        private List<SpiderSampleLayerItemViewModel> RenderSpiderData(
            IReadOnlyList<string> elements,
            bool isNormalizationEnabled,
            NormalizationStandard? standard,
            out double? renderedYMin,
            out double? renderedYMax)
        {
            var spiderLayers = new List<SpiderSampleLayerItemViewModel>();
            renderedYMin = null;
            renderedYMax = null;

            if (SpiderDiagramViewModel == null || SpiderDiagramViewModel.Samples.Count == 0 || elements.Count == 0)
                return spiderLayers;

            double[] xPositions = Enumerable.Range(1, elements.Count).Select(i => (double)i).ToArray();
            var standardValues = standard?.Values;

            var plotColors = new[]
            {
                "#1f77b4", "#ff7f0e", "#2ca02c", "#d62728", "#9467bd",
                "#8c564b", "#e377c2", "#7f7f7f", "#bcbd22", "#17becf",
                "#aec7e8", "#ffbb78", "#98df8a", "#ff9896", "#c5b0d5"
            };

            var sampleGroups = new List<(string DisplayName, List<SpiderSampleData> Samples)>();
            var groupedLookup = new Dictionary<string, List<SpiderSampleData>>(StringComparer.OrdinalIgnoreCase);
            for (int sampleIndex = 0; sampleIndex < SpiderDiagramViewModel.Samples.Count; sampleIndex++)
            {
                var sample = SpiderDiagramViewModel.Samples[sampleIndex];
                string displayName = string.IsNullOrWhiteSpace(sample.Name)
                    ? $"Sample {sampleIndex + 1}"
                    : sample.Name.Trim();

                if (!groupedLookup.TryGetValue(displayName, out var groupedSamples))
                {
                    groupedSamples = new List<SpiderSampleData>();
                    groupedLookup[displayName] = groupedSamples;
                    sampleGroups.Add((displayName, groupedSamples));
                }

                groupedSamples.Add(sample);
            }

            for (int groupIdx = 0; groupIdx < sampleGroups.Count; groupIdx++)
            {
                var sampleGroup = sampleGroups[groupIdx];
                var color = ScottPlot.Color.FromHex(plotColors[groupIdx % plotColors.Length]);
                var groupedSeries = new List<(SpiderSampleData Sample, ScottPlot.Plottables.Scatter Scatter)>();

                foreach (var sample in sampleGroup.Samples)
                {
                    var normalizedValues = new double[elements.Count];
                    var xValues = new double[elements.Count];
                    int pointCount = 0;

                    for (int i = 0; i < elements.Count; i++)
                    {
                        string element = elements[i];
                        if (!TryGetSpiderPlotValue(sample, element, isNormalizationEnabled, standardValues, out double plotValue))
                        {
                            continue;
                        }

                        normalizedValues[pointCount] = plotValue;
                        xValues[pointCount] = xPositions[i];
                        pointCount++;

                        renderedYMin = !renderedYMin.HasValue || plotValue < renderedYMin.Value ? plotValue : renderedYMin;
                        renderedYMax = !renderedYMax.HasValue || plotValue > renderedYMax.Value ? plotValue : renderedYMax;
                    }

                    if (pointCount == 0)
                    {
                        continue;
                    }

                    double[] finalXValues;
                    double[] finalYValues;
                    if (pointCount == elements.Count)
                    {
                        finalXValues = xValues;
                        finalYValues = normalizedValues;
                    }
                    else
                    {
                        finalXValues = new double[pointCount];
                        finalYValues = new double[pointCount];
                        Array.Copy(xValues, finalXValues, pointCount);
                        Array.Copy(normalizedValues, finalYValues, pointCount);
                    }

                    var scatter = WpfPlot1.Plot.Add.Scatter(finalXValues, finalYValues);
                    scatter.LineWidth = 1.5f;
                    scatter.Color = color;
                    scatter.MarkerSize = 5;
                    scatter.LegendText = string.Empty;

                    groupedSeries.Add((sample, scatter));
                }

                if (groupedSeries.Count == 0)
                {
                    continue;
                }

                var legendProxy = WpfPlot1.Plot.Add.ScatterPoints(Array.Empty<Coordinates>());
                legendProxy.LegendText = sampleGroup.DisplayName;
                legendProxy.Color = color;
                legendProxy.MarkerSize = 8;
                legendProxy.LineWidth = 1.5f;
                legendProxy.MarkerShape = groupedSeries[0].Scatter.MarkerShape;

                var spiderLayer = new SpiderSampleLayerItemViewModel(
                    sampleGroup.DisplayName,
                    groupedSeries.Select(entry => (entry.Sample, entry.Scatter)),
                    legendProxy,
                    WpfPlot1);

                spiderLayer.Tag = spiderLayer.PropertyModel;
                spiderLayer.SampleNameChanged += (layer, sampleName) => SyncSpiderSampleNameToDataGrid(layer, sampleName);
                spiderLayers.Add(spiderLayer);
            }

            return spiderLayers;
        }

        /// <summary>
        /// 蛛网图专用最佳视图：固定X轴元素完整可见，并根据数据自适应Y轴范围。
        /// </summary>
        private void ApplySpiderPlotBestView(
            IReadOnlyList<string> displayElements,
            bool isNormalizationEnabled,
            NormalizationStandard? standard,
            double? precomputedYMin = null,
            double? precomputedYMax = null)
        {
            if (WpfPlot1 == null || displayElements == null || displayElements.Count == 0)
                return;

            double yMin = -2;
            double yMax = 4;

            if ((!precomputedYMin.HasValue || !precomputedYMax.HasValue)
                && TryCalculateSpiderYAxisRange(displayElements, isNormalizationEnabled, standard, out double calculatedYMin, out double calculatedYMax))
            {
                precomputedYMin = calculatedYMin;
                precomputedYMax = calculatedYMax;
            }

            if (precomputedYMin.HasValue && precomputedYMax.HasValue)
            {
                yMin = precomputedYMin.Value;
                yMax = precomputedYMax.Value;

                double padding = yMax > yMin
                    ? Math.Max((yMax - yMin) * 0.1, 0.2)
                    : Math.Max(Math.Abs(yMin) * 0.1, 0.5);

                yMin -= padding;
                yMax += padding;
            }

            WpfPlot1.Plot.Axes.SetLimits(
                left: 0.5,
                right: displayElements.Count + 0.5,
                bottom: yMin,
                top: yMax
            );
        }

        private bool TryCalculateSpiderYAxisRange(
            IReadOnlyList<string> displayElements,
            bool isNormalizationEnabled,
            NormalizationStandard? standard,
            out double yMin,
            out double yMax)
        {
            yMin = 0;
            yMax = 0;

            if (SpiderDiagramViewModel == null || displayElements == null || displayElements.Count == 0)
            {
                return false;
            }

            bool hasValue = false;
            var standardValues = standard?.Values;
            foreach (var sample in SpiderDiagramViewModel.Samples)
            {
                foreach (var element in displayElements)
                {
                    if (!TryGetSpiderPlotValue(sample, element, isNormalizationEnabled, standardValues, out double plotValue))
                    {
                        continue;
                    }

                    if (!hasValue)
                    {
                        yMin = plotValue;
                        yMax = plotValue;
                        hasValue = true;
                        continue;
                    }

                    if (plotValue < yMin) yMin = plotValue;
                    if (plotValue > yMax) yMax = plotValue;
                }
            }

            return hasValue;
        }

        private static bool TryGetSpiderPlotValue(
            SpiderSampleData sample,
            string element,
            bool isNormalizationEnabled,
            IReadOnlyDictionary<string, double>? standardValues,
            out double plotValue)
        {
            plotValue = 0;

            if (sample == null || string.IsNullOrWhiteSpace(element))
            {
                return false;
            }

            if (!sample.ElementValues.TryGetValue(element, out double rawValue) || rawValue <= 0)
            {
                return false;
            }

            if (isNormalizationEnabled)
            {
                if (standardValues == null
                    || !standardValues.TryGetValue(element, out double refValue)
                    || refValue <= 0)
                {
                    return false;
                }

                plotValue = Math.Log10(rawValue / refValue);
                return true;
            }

            plotValue = Math.Log10(rawValue);
            return true;
        }

        /// <summary>
        /// 判断当前图解是否已有投图产生的数据图层。
        /// </summary>
        private bool HasPlottedDataLayers()
        {
            var dataRootNode = LayerTree.FirstOrDefault(node =>
                node is CategoryLayerItemViewModel vm && vm.Name == LanguageService.Instance["data_point"]);
            if (dataRootNode?.Children.Any() == true)
            {
                return true;
            }

            var spiderDataCategory = LayerTree.FirstOrDefault(layer =>
                layer is CategoryLayerItemViewModel category
                && category.Name == (LanguageService.Instance["Data"] ?? "Data"));
            if (spiderDataCategory?.Children.Any() == true)
            {
                return true;
            }

            return SpiderDiagramViewModel.IsSpiderPlotMode && SpiderDiagramViewModel.Samples.Count > 0;
        }

        private void MarkDataGridPlotRefreshPending()
        {
            if (!HasPlottedDataLayers())
            {
                return;
            }

            IsDataGridPlotRefreshPending = true;
        }

        private void ClearDataGridPlotRefreshPending()
        {
            IsDataGridPlotRefreshPending = false;
        }

        /// <summary>
        /// 辅助方法，用于清除当前绘图中的所有由数据导入的点。
        /// 这个方法不会清空数据表格，也不会显示确认弹窗。
        /// </summary>
        private void ClearExistingPlottedData()
        {
            if (SpiderDiagramViewModel.IsSpiderPlotMode && CurrentTemplate?.TemplateType == "Spider")
            {
                SpiderDiagramViewModel.Samples.Clear();

                if (_selectedLayer is SpiderSampleLayerItemViewModel)
                {
                    PropertyGridModel = null;
                    _selectedLayer = null;
                }

                ClearLayerTree();
                BuildLayerTreeFromTemplate(CurrentTemplate);
                RefreshPlotFromLayers();
                return;
            }

            // 在图层树中找到名为 "数据点" 的根分类图层
            var dataRootNode = LayerTree.FirstOrDefault(node => node is CategoryLayerItemViewModel vm && vm.Name == LanguageService.Instance["data_point"]);

            // 如果找到了该节点
            if (dataRootNode != null)
            {
                var allDataLayers = FlattenTree(dataRootNode.Children).ToList();

                // 从图层树的根集合中移除 "数据点" 这个顶级分类节点
                RemoveLayerSubtree(dataRootNode);

                // 如果属性面板当前显示的是某个被删除的图层，则清空属性面板
                if (_selectedLayer != null && (allDataLayers.Contains(_selectedLayer) || _selectedLayer == dataRootNode))
                {
                    PropertyGridModel = null;
                    _selectedLayer = null;
                }
            }

            // 重绘底图，确保数据图例（含替身）与空数据图层列表保持一致
            RefreshPlotFromLayers(true);
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

        private void NotifySelectionDependentCommandStates()
        {
            DeleteSelectedObjectCommand.NotifyCanExecuteChanged();
            HandlePageDeleteKeyCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 页面级 Delete 快捷键：数据页交给 ReoGrid 处理单元格清除，其余页面删除绘图对象。
        /// </summary>
        private bool CanHandlePageDeleteKey() => RibbonTabIndex != 1 && CanDeleteSelectedObject();

        [RelayCommand(CanExecute = nameof(CanHandlePageDeleteKey))]
        private Task HandlePageDeleteKey() => DeleteSelectedObject();

        /// <summary>
        /// 点击图层对象, 在图上高亮显示, 并在属性面板显示其属性
        /// 支持 Ctrl/Shift 多选
        /// </summary>
        /// <param name="selectedItem">当前选中的图层对象</param>
        [RelayCommand]
        private void SelectLayer(LayerItemViewModel selectedItem)
        {
            // 如果正在阻止选择（例如脚本验证清除数据后），忽略此次选择请求
            if (_isBlockingTreeViewSelection)
            {
                return;
            }

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
                NotifySelectionDependentCommandStates();
                return;
            }

            // 获取当前选中项的父级分类
            var newParent = LayerTree.FirstOrDefault(c => c.Children.Contains(selectedItem));

            // --- 处理多选逻辑 ---
            bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            if (selectedItem is SpiderSampleLayerItemViewModel spiderSelectedItem)
            {
                bool isPlotHitSelection = _lastHoveredPlottable != null &&
                                          ReferenceEquals(_lastHoveredLayer, selectedItem) &&
                                          spiderSelectedItem.ContainsPlottable(_lastHoveredPlottable);

                if (!isPlotHitSelection)
                {
                    spiderSelectedItem.ResetActivePlottable();
                }
            }

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

            if (!_isSyncingSelection && _selectedLayer is SpiderSampleLayerItemViewModel spiderLayer)
            {
                SyncSpiderSelectionToDataGrid(spiderLayer);
            }

            // 应用选中样式：选中对象保持原样，其他的变暗
            foreach (var layer in allPlottableLayers)
            {
                if (layer is IPlotLayer plotLayer)
                {
                    if (SelectedLayers.Contains(layer))
                    {
                        // 选中图层：蛛网图保持原样，其余恢复原样
                        if (layer is SpiderSampleLayerItemViewModel selectedSpiderLayer)
                        {
                            selectedSpiderLayer.Restore();
                        }
                        else
                        {
                            plotLayer.Restore();
                        }
                    }
                    else
                    {
                        plotLayer.Dim();
                    }
                }
            }

            WpfPlot1.Refresh();
            NotifySelectionDependentCommandStates();
        }



        private object? GetLayerDefinition(LayerItemViewModel layer)
        {
            // 蛛网图模式下通过 Tag 存储属性模型
            if (layer.Tag is SpiderSamplePropertyModel spiderModel)
            {
                return spiderModel;
            }
            if (layer.Tag is SpiderAxisPropertyModel spiderAxisModel)
            {
                return spiderAxisModel;
            }

            return layer switch
            {
                PointLayerItemViewModel pointLayer => pointLayer.PointDefinition,
                LineLayerItemViewModel lineLayer => lineLayer.LineDefinition,
                TextLayerItemViewModel textLayer => textLayer.TextDefinition,
                ArrowLayerItemViewModel arrowLayer => arrowLayer.ArrowDefinition,
                PolygonLayerItemViewModel polygonLayer => polygonLayer.PolygonDefinition,
                AxisLayerItemViewModel axisLayer => axisLayer.AxisDefinition is SpiderAxisDefinition spiderAxis
                    ? CreateSpiderAxisPropertyModel(spiderAxis)
                    : axisLayer.AxisDefinition,
                ScatterLayerItemViewModel scatterLayer => scatterLayer.ScatterDefinition,
                FunctionLayerItemViewModel funcLayer => funcLayer.FunctionDefinition,
                _ => nullObject
            };
        }

        private object CreateSpiderAxisPropertyModel(SpiderAxisDefinition spiderAxis)
        {
            if (WpfPlot1 == null)
                return spiderAxis;

            ScottPlot.IAxis? axis = spiderAxis.Type switch
            {
                "Bottom" => WpfPlot1.Plot.Axes.Bottom,
                "Left" => WpfPlot1.Plot.Axes.Left,
                _ => null
            };

            return axis != null
                ? new SpiderAxisPropertyModel(axis, WpfPlot1)
                : spiderAxis;
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

                // 5. 清除待刷新提示
                ClearDataGridPlotRefreshPending();

                // 6. 成功提示
                MessageHelper.Success(LanguageService.Instance["all_data_cleared"]);
            }
        }

        /// <summary>
        /// 更新数据
        /// 先清除旧的数据点，然后根据当前表格重新投点
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanUpdateData))]
        private async Task UpdateData()
        {
            if (IsTransitionLoading || IsDataPlotLoading)
            {
                return;
            }

            await BeginDataPlotLoadingAsync();
            try
            {
                // 先触发取消选择
                CancelSelected();

                // 清除当前绘图中的所有由数据导入的点
                ClearExistingPlottedData();

                // 根据当前数据表格中的数据重新进行投点
                PlotDataFromGrid();
            }
            finally
            {
                await EndDataPlotLoadingAsync();
            }
        }

        /// <summary>
        /// 视图复位
        /// </summary>
        [RelayCommand]
        private void CenterPlot()
        {
            if (WpfPlot1 == null)
                return;

            if (CurrentTemplate?.TemplateType == "Spider" && CurrentTemplate.Info?.Axes != null)
            {
                var spiderAxis = CurrentTemplate.Info.Axes
                    .OfType<SpiderAxisDefinition>()
                    .FirstOrDefault(axis => axis.Type == "Bottom")
                    ?? CurrentTemplate.Info.Axes.OfType<SpiderAxisDefinition>().FirstOrDefault();

                if (spiderAxis != null)
                {
                    NormalizationStandard? selectedStandard = null;
                    if (spiderAxis.IsNormalizationEnabled && !string.IsNullOrEmpty(spiderAxis.NormalizationStandard))
                    {
                        var allStandards = spiderAxis.SpiderType == "REE"
                            ? NormalizationData.GetReeStandards()
                            : NormalizationData.GetTraceElementStandards();
                        selectedStandard = allStandards.FirstOrDefault(s => s.Name == spiderAxis.NormalizationStandard);
                    }

                    var configuredElements = spiderAxis.ElementOrder
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .ToList();

                    var displayElements = spiderAxis.IsNormalizationEnabled && selectedStandard != null
                        ? configuredElements.Where(e => selectedStandard.Values.ContainsKey(e)).ToList()
                        : configuredElements;

                    if (displayElements.Count == 0)
                    {
                        displayElements = configuredElements;
                    }

                    if (displayElements.Count > 0)
                    {
                        ApplySpiderPlotBestView(displayElements, spiderAxis.IsNormalizationEnabled, selectedStandard);
                        WpfPlot1.Refresh();
                        return;
                    }
                }
            }

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
                            // 支持倒置范围：需要考虑 xMin 可能大于 xMax 的情况
                            double actualXMin = Math.Min(xMin, xMax);
                            double actualXMax = Math.Max(xMin, xMax);
                            if (dataXMin < actualXMin - tolerance || dataXMax > actualXMax + tolerance)
                            {
                                outOfRange = true;
                            }
                        }

                        if (isYRangeSet && !outOfRange)
                        {
                            // 支持倒置范围：需要考虑 yMin 可能大于 yMax 的情况
                            double actualYMin = Math.Min(yMin, yMax);
                            double actualYMax = Math.Max(yMin, yMax);
                            if (dataYMin < actualYMin - tolerance || dataYMax > actualYMax + tolerance)
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
                            // 支持倒置范围：直接设置 Range.Min 和 Range.Max，不使用 SetLimitsX
                            WpfPlot1.Plot.Axes.Bottom.Range.Min = xMin;
                            WpfPlot1.Plot.Axes.Bottom.Range.Max = xMax;
                        }
                        else
                        {
                            WpfPlot1.Plot.Axes.AutoScaleX();
                        }

                        if (isYRangeSet)
                        {
                            // 支持倒置范围：直接设置 Range.Min 和 Range.Max，不使用 SetLimitsY
                            WpfPlot1.Plot.Axes.Left.Range.Min = yMin;
                            WpfPlot1.Plot.Axes.Left.Range.Max = yMax;
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
                prop.PropertyChanged -= PropertyGridModel_PropertyChanged;
            }

            PropertyGridModel = null;   // 取消属性编辑器
            ScriptsPropertyGrid = false;

            IsShowTemplateInfo = false;     // 取消绘图模板指南显示

            // 清除数据点高亮标记
            if (_selectedDataPointMarker != null && _selectedDataPointMarker.IsVisible)
            {
                _selectedDataPointMarker.IsVisible = false;
                if (_selectedDataPointLabel != null)
                {
                    _selectedDataPointLabel.IsVisible = false;
                }
                WpfPlot1.Refresh();
            }

            NotifySelectionDependentCommandStates();
        }

        /// <summary>
        /// 图例设置
        /// </summary>
        [RelayCommand]
        private void LegendSetting()
        {
            // 蛛网图模式：直接编辑模板中的图例定义，确保修改能参与重绘
            if (SpiderDiagramViewModel.IsSpiderPlotMode)
            {
                if (_selectedLayer != null) CancelSelected();
                PropertyGridModel = GetOrCreateSpiderLegendDefinition();
                return;
            }

            if (CurrentTemplate?.Info?.Legend == null) return;
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
            // 蛛网图模式：直接编辑模板中的网格定义，确保修改能参与重绘
            if (SpiderDiagramViewModel.IsSpiderPlotMode)
            {
                if (_selectedLayer != null) CancelSelected();
                PropertyGridModel = GetOrCreateSpiderGridDefinition();
                return;
            }

            if (CurrentTemplate?.Info?.Grid == null) return;
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
            // 蛛网图模式：直接编辑模板中的标题定义，确保修改能参与重绘
            if (SpiderDiagramViewModel.IsSpiderPlotMode)
            {
                if (_selectedLayer != null) CancelSelected();
                PropertyGridModel = GetOrCreateSpiderTitleDefinition();
                return;
            }

            if (CurrentTemplate?.Info?.Title == null) return;
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

            // 设置加载标志，防止触发未保存检测
            _isLoadingHelpDocument = true;

            try
            {
                _richTextBox.Document.Blocks.Clear();

                // 1. 优先尝试从数据库加载 (CurrentTemplate 对应的 Entity)
                string rtfContent = null;

                try
                {
                    // 使用在 SelectTemplateCard 时记录的 _currentTemplateId
                    Guid? templateId = _currentTemplateId;

                    if (templateId.HasValue)
                    {
                        rtfContent = GraphMapDatabaseService.Instance.GetHelpDocument(
                            templateId.Value,
                            languageCode);
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
                // 3. 如果数据库没有内容，尝试从本地文件系统加载（用于外部临时文件模式）
                if (string.IsNullOrEmpty(rtfContent))
                {
                    if (!string.IsNullOrEmpty(_currentTemplateFilePath))
                    {
                        string directory = Path.GetDirectoryName(_currentTemplateFilePath);
                        
                        // 尝试加载对应语言的 RTF 文件
                        string fileRtfPath = FileHelper.FindFileOrGetFirstWithExtension(directory, languageCode, ".rtf");
                        
                        if (!string.IsNullOrEmpty(fileRtfPath))
                        {
                            RtfHelper.LoadRtfToRichTextBox(fileRtfPath, _richTextBox);
                            _currentRtfFilePath = fileRtfPath;
                            return; // 成功加载，直接返回
                        }
                    }
                }

                // 4. 如果以上都失败，加载默认模板文件
                if (string.IsNullOrEmpty(rtfContent))
                {
                     string defaultTemplatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Documents", "template.rtf");
                     if (File.Exists(defaultTemplatePath))
                     {
                         RtfHelper.LoadRtfToRichTextBox(defaultTemplatePath, _richTextBox);
                         _currentRtfFilePath = defaultTemplatePath;
                     }
                }
            }
            finally
            {
                // 恢复加载标志
                _isLoadingHelpDocument = false;

                // 记录加载完成后的 RTF 内容作为基准，用于 IsHelpDocumentModified 比较
                // 必须在 _isLoadingHelpDocument = false 之后获取，因为 RichTextBox 可能
                // 在加载过程中规范化 RTF 内容，获取当前规范化后的内容确保基准一致
                _originalHelpDocumentRtf = RtfHelper.GetRtfString(_richTextBox);
            }
        }

        /// <summary>
        /// 使用 Word 打开说明文档（复制模板到临时目录）
        /// </summary>
        [RelayCommand]
        private void OpenHelpDocInWord()
        {
            if (_richTextBox == null) return;

            string sourceRtfPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Documents", "template.rtf");

            // 检查源模板文件是否存在
            if (!File.Exists(sourceRtfPath))
            {
                MessageHelper.Error(LanguageService.Instance["help_file_not_found"]);
                return;
            }

            try
            {
                // 创建临时文件路径
                string tempFolder = Path.GetTempPath();
                string tempFileName = $"template_{DateTime.Now:yyyyMMddHHmmss}.rtf";
                string tempRtfPath = Path.Combine(tempFolder, tempFileName);

                // 复制模板文件到临时目录
                File.Copy(sourceRtfPath, tempRtfPath, true);

                // 使用默认关联程序（Word）打开临时文件
                Process.Start(new ProcessStartInfo(tempRtfPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                // 无法打开文件
                MessageHelper.Error(LanguageService.Instance["failed_to_open_file"] + ex.Message);
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
        private async Task<bool> TryCreateNewTemplate(
            DiagramPlotEditorViewModel editor,
            Action<string>? showWarningMessage = null,
            Func<string, string, string, string, Task<bool>>? showConfirmDialogAsync = null)
        {
            if (editor == null) return false;

            if (!editor.Validate(out string validationError))
            {
                (showWarningMessage ?? MessageHelper.Warning)(validationError);
                return false;
            }

            var allLanguages = editor.LanguageParts.Select(l => l.Text).ToList();
            var categoryParts = editor.CategoryParts.ToList();
            string plotType = editor.SelectedPlotType;
            var localizedCategory = editor.BuildCategoryNodeList();

            string lastPart = categoryParts[categoryParts.Count - 1].DisplayName;
            string secondLastPart = categoryParts[categoryParts.Count - 2].DisplayName;
            string folderName = $"{secondLastPart}_{lastPart}";

            // 1. 检查数据库中是否存在同名模板
            var existingId = GraphMapDatabaseService.GenerateId(folderName, true);
            var existingTemplate = await Task.Run(() => GraphMapDatabaseService.Instance.GetTemplate(existingId));

            if (existingTemplate != null)
            {
                bool shouldContinue;
                if (showConfirmDialogAsync != null)
                {
                    shouldContinue = await showConfirmDialogAsync(
                        LanguageService.Instance["tips"] ?? "Tips",
                        LanguageService.Instance["BasemapExisted"],
                        LanguageService.Instance["Confirm"] ?? "Confirm",
                        LanguageService.Instance["Cancel"] ?? "Cancel");
                }
                else
                {
                    var result = HandyControl.Controls.MessageBox.Show(
                        LanguageService.Instance["BasemapExisted"],
                        LanguageService.Instance["tips"] ?? "Tips",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    shouldContinue = result == MessageBoxResult.Yes;
                }

                if (!shouldContinue)
                {
                    return false;
                }
            }

            // 确保清除语言覆盖
            ClearDiagramLanguageContext();

            CurrentTemplate = editor.BuildTemplateForSubmit();

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
                HelpDocuments = editor.GetHelpDocumentsForSubmit()
            };

            // 为尚未写入帮助文档的语言填充默认 template.rtf
            if (newEntity.HelpDocuments.Count < allLanguages.Count)
            {
                string sourceRtfPath = Path.Combine(FileHelper.GetAppPath(), "Data", "Documents", "template.rtf");
                if (!File.Exists(sourceRtfPath))
                {
                    try
                    {
                        string devPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Data", "Documents", "template.rtf"));
                        if (File.Exists(devPath))
                            sourceRtfPath = devPath;
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
                            if (!newEntity.HelpDocuments.ContainsKey(lang))
                                newEntity.HelpDocuments[lang] = defaultRtfContent;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to load template.rtf: {ex.Message}");
                    }
                }
            }

            // 5. 保存到数据库
            await Task.Run(() => GraphMapDatabaseService.Instance.UpsertTemplate(newEntity));

            // 更新当前状态
            _currentTemplateId = newEntity.Id;
            _currentTemplateFilePath = null;
            _isCurrentTemplateCustom = true;
            IsHarkerDiagramMode = false;
            UpdateHelpDocReadOnlyState();

            IsTemplateMode = false;
            IsPlotMode = true;


            CurrentDiagramLanguage = ResolveInitialDiagramLanguage(CurrentTemplate);

            BuildLayerTreeFromTemplate(CurrentTemplate);
            RefreshPlotFromLayers();

            // 初始化原始模板 JSON 基准（修复漏洞1：新建模板后首轮编辑未被检测）
            _originalTemplateJson = SerializeTemplate(CurrentTemplate);
            _originalHelpDocumentRtf = RtfHelper.GetRtfString(_richTextBox);
            HasUnsavedChanges = false;

            // 生成并保存缩略图（680x480 灰度图）
            try
            {
                var colorImageBytes = WpfPlot1.Plot.GetImageBytes(680, 480);

                // 转换为灰度图
                using (var ms = new MemoryStream(colorImageBytes))
                {
                    var colorBitmap = new BitmapImage();
                    colorBitmap.BeginInit();
                    colorBitmap.CacheOption = BitmapCacheOption.OnLoad;
                    colorBitmap.StreamSource = ms;
                    colorBitmap.EndInit();
                    
                    // 转换为 8 位灰度图
                    var grayBitmap = new FormatConvertedBitmap();
                    grayBitmap.BeginInit();
                    grayBitmap.Source = colorBitmap;
                    grayBitmap.DestinationFormat = PixelFormats.Gray8;
                    grayBitmap.EndInit();
                    
                    // 编码为 JPEG 并上传到数据库
                    var encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(grayBitmap));
                    using (var uploadStream = new MemoryStream())
                    {
                        encoder.Save(uploadStream);
                        uploadStream.Position = 0;
                        
                        await Task.Run(() => GraphMapDatabaseService.Instance.UploadThumbnail(newEntity.Id, uploadStream));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to generate thumbnail: {ex.Message}");
            }

            // 加载说明文档 (从数据库)
            ReloadHelpDocument(CurrentDiagramLanguage);

            // 新建后进入绘图模式，返回模板库时再刷新
            RequestTemplateLibraryRefresh();

            return true;
        }

        private static void TrySetDialogOwner(Window window)
        {
            try
            {
                if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
                    window.Owner = Application.Current.MainWindow;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置 Owner 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 新建底图——弹窗新建
        /// </summary>
        [RelayCommand]
        private void NewTemplate()
        {
            try
            {
                var window = new DiagramPlotEditorWindow();
                window.InitializeForCreate();

                window.ConfirmCommand = new AsyncRelayCommand<DiagramPlotEditorViewModel>(async (editor) =>
                {
                    if (await TryCreateNewTemplate(
                        editor,
                        window.ShowWarningMessage,
                        window.ShowConfirmDialogAsync))
                    {
                        window.Close();
                    }
                });

                window.CancelCommand = new RelayCommand<DiagramPlotEditorViewModel>(_ => window.Close());

                TrySetDialogOwner(window);
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
        /// 保存当前底图模板
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanAddDrawingObject))]
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
                Filter = FileDialogFilterHelper.ImportTemplates,
                DefaultExt = TemplatePackageFileExtensions.DiagramPrimary,
                CheckFileExists = true,
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != true) return;

            if (await ImportCustomTemplatesBatchAsync(openFileDialog.FileNames))
            {
                await RefreshTemplateLibraryAfterDataChangeAsync();
            }
        }

        /// <summary>
        /// 批量导入自定义图解模板
        /// </summary>
        private async Task<bool> ImportCustomTemplatesBatchAsync(IEnumerable<string> filePaths)
        {
            var paths = filePaths
                .Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p))
                .ToList();

            if (paths.Count == 0)
                return false;

            if (paths.Count > 1 && paths.Any(f => TemplatePackageFileExtensions.FromPath(f) == TemplatePackageFileExtensions.Json))
            {
                MessageHelper.Warning(Lang("plot_json_multiselect_not_supported", "JSON files cannot be added in a multi-select batch. Only package files support multi-select batch import."));
                return false;
            }

            bool imported = false;

            foreach (var selectedPath in paths)
            {
                string ext = TemplatePackageFileExtensions.FromPath(selectedPath);

                try
                {
                    if (TemplatePackageFileExtensions.IsDiagramPackage(ext))
                    {
                        if (await ImportZipTemplate(selectedPath))
                            imported = true;
                    }
                    else if (ext == TemplatePackageFileExtensions.Json)
                    {
                        if (await ImportJsonTemplate(selectedPath))
                            imported = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageHelper.Error($"{Path.GetFileName(selectedPath)}: {LanguageService.Instance["import_failed"]} {ex.Message}");
                }
            }

            return imported;
        }

        /// <summary>
        /// 从指定路径导入自定义图解模板（支持拖入左侧导入区域，可多文件）
        /// </summary>
        [RelayCommand]
        private async Task ImportCustomTemplateFromPath(object? parameter)
        {
            if (IsPlotMode)
                return;

            var filePaths = ExtractImportFilePaths(parameter);
            if (filePaths.Count == 0)
                return;

            try
            {
                if (await ImportCustomTemplatesBatchAsync(filePaths))
                    await NavigateToPersonalTemplatesAfterImportAsync();
            }
            catch (Exception ex)
            {
                var displayName = filePaths.Count == 1
                    ? Path.GetFileName(filePaths[0])
                    : string.Join(", ", filePaths.Select(Path.GetFileName));
                MessageHelper.Error($"{displayName}: {LanguageService.Instance["import_failed"]} {ex.Message}");
            }
        }

        private static IReadOnlyList<string> ExtractImportFilePaths(object? parameter)
        {
            return parameter switch
            {
                string singlePath when !string.IsNullOrWhiteSpace(singlePath) => new[] { singlePath },
                string[] paths => paths.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray(),
                IEnumerable<string> paths => paths.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray(),
                _ => Array.Empty<string>()
            };
        }

        /// <summary>
        /// 导入成功后切换到个人图解模板分类
        /// </summary>
        private async Task NavigateToPersonalTemplatesAfterImportAsync()
        {
            _isTemplateLibraryDirty = true;
            IsSpiderDiagramMode = false;
            IsHarkerDiagramMode = false;
            IsPersonalExpanded = true;
            IsFavoriteExpanded = false;
            IsOfficialExpanded = false;
            IsRecentsExpanded = false;

            if (IsPlotMode)
            {
                await BackToTemplateMode();
                return;
            }

            ClearTreeViewSelection(OfficialTemplatesNode);
            ClearTreeViewSelection(PersonalTemplatesNode);
            CurrentCategoryName = PersonalTemplatesNode?.Name ?? LanguageService.Instance["personal_templates"];
            await RefreshTemplateLibraryAfterDataChangeAsync();
        }

        private async Task<bool> ImportZipTemplate(string zipPath)
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
                                        if (!GraphMapTemplateService.TryParseDiagramTemplate(jsonContent, out var tempTemplate))
                                            continue;

                                        if (!GraphMapTemplateService.IsVersionCompatible(tempTemplate))
                                        {
                                            errorMessage = LanguageService.Instance["template_version_too_high"];
                                            return;
                                        }

                                        template = tempTemplate;
                                        validTemplateName = Path.GetFileNameWithoutExtension(entry.Name);
                                        break;
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
                return false;
            }

            if (string.IsNullOrEmpty(validTemplateName) || template == null)
            {
                // 模板文件已损坏或未包含有效的模板 JSON 文件。
                MessageHelper.Error(LanguageService.Instance["diagram_template_corrupted_or_invalid"]);
                return false;
            }

            return await FinalizeImport(validTemplateName, template, async (customDir) =>
            {
                await Task.Run(() =>
                {
                    ZipFile.ExtractToDirectory(zipPath, customDir);
                });
            });
        }

        private async Task<bool> ImportJsonTemplate(string jsonPath)
        {
            string jsonContent = await File.ReadAllTextAsync(jsonPath);
            if (!GraphMapTemplateService.TryParseDiagramTemplate(jsonContent, out var template))
            {
                MessageHelper.Error(LanguageService.Instance["diagram_template_corrupted_or_invalid"]);
                return false;
            }

            if (!GraphMapTemplateService.IsVersionCompatible(template))
            {
                MessageHelper.Error(LanguageService.Instance["template_version_too_high"]);
                return false;
            }

            string validTemplateName = Path.GetFileNameWithoutExtension(jsonPath);
            string sourceDir = Path.GetDirectoryName(jsonPath);

            return await FinalizeImport(validTemplateName, template, async (customDir) =>
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

        private async Task<bool> FinalizeImport(string templateName, GraphMapTemplate template, Func<string, Task> copyAction)
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

                if (!confirm) return false;
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
                            Application.Current.Dispatcher.Invoke(() => HandyControl.Controls.MessageBox.Show(
                                string.Format(Lang("plot_upload_thumbnail_failed", "Failed to upload thumbnail: {0}"), ex.Message)));
                        }
                    }
                });

                MessageHelper.Success(LanguageService.Instance["template_saved_successfully"]);
                CheckUnsavedChanges();
                return true;
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
                        ConfigureGridDefinitionCapabilities(template);
                        ApplyGridDefinitionToPlot(plot, template.Info.Grid);
                    }
                }

                // Title
                if (template.Info.Title.Label.Translations.Any())
                {
                    plot.Axes.Title.Label.Text = template.Info.Title.Label.Get(DiagramLanguage);
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
                foreach (var layer in allNodes.OfType<IPlotLayer>().Where(ShouldRenderLayer))
                {
                    layer.Render(plot);
                }

                if (template.TemplateType != "Ternary")
                    plot.Axes.AutoScale();

                // 生成 680x480 灰度缩略图
                var colorImageBytes = plot.GetImageBytes(680, 480, ScottPlot.ImageFormat.Jpeg);
                
                // 转换为灰度图
                using (var ms = new MemoryStream(colorImageBytes))
                {
                    var colorBitmap = new BitmapImage();
                    colorBitmap.BeginInit();
                    colorBitmap.CacheOption = BitmapCacheOption.OnLoad;
                    colorBitmap.StreamSource = ms;
                    colorBitmap.EndInit();
                    
                    // 转换为 8 位灰度图
                    var grayBitmap = new FormatConvertedBitmap();
                    grayBitmap.BeginInit();
                    grayBitmap.Source = colorBitmap;
                    grayBitmap.DestinationFormat = PixelFormats.Gray8;
                    grayBitmap.EndInit();
                    
                    // 编码为 JPEG 并保存
                    var encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(grayBitmap));
                    using (var fileStream = new FileStream(outputPath, FileMode.Create))
                    {
                        encoder.Save(fileStream);
                    }
                }
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
            var openFileDialog = new VistaOpenFileDialog
            {
                Title = LanguageService.Instance["open_template"],
                Filter = FileDialogFilterHelper.OpenTemplate,
                DefaultExt = TemplatePackageFileExtensions.DiagramPrimary,
                CheckFileExists = true,
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
                await OpenTemplateFromPath(openFileDialog.FileName);
        }

        /// <summary>
        /// 从指定路径打开图解模板（支持拖入文件）
        /// </summary>
        [RelayCommand]
        private async Task OpenTemplateFromPath(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return;

            string extension = TemplatePackageFileExtensions.FromPath(filePath);
            if (!TemplatePackageFileExtensions.IsDiagramOpenable(extension))
                return;

            try
            {
                ClearDiagramLanguageContext();
                _currentTemplateId = null;

                if (TemplatePackageFileExtensions.IsDiagramPackage(extension))
                {
                    string tempDir = Path.Combine(Path.GetTempPath(), "GeoChemistryNexus", "TempTemplates", Guid.NewGuid().ToString());
                    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                    Directory.CreateDirectory(tempDir);

                    await Task.Run(() => System.IO.Compression.ZipFile.ExtractToDirectory(filePath, tempDir));

                    var jsonFiles = Directory.GetFiles(tempDir, "*.json", SearchOption.AllDirectories);
                    if (jsonFiles.Length == 0)
                    {
                        MessageHelper.Error(LanguageService.Instance["invalid_diagram_template_no_json"]);
                        return;
                    }

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

                _currentTemplateFilePath = filePath;
                IsHarkerDiagramMode = false;
                _isCurrentTemplateCustom = true;
                UpdateHelpDocReadOnlyState();

                IsTemplateMode = false;
                IsPlotMode = true;

                if (!await LoadAndBuildLayers(filePath))
                {
                    await BackToTemplateMode();
                    return;
                }

                string directoryPath = Path.GetDirectoryName(filePath);
                var tempRTFfile = FileHelper.FindFileOrGetFirstWithExtension(
                                      directoryPath,
                                      CurrentDiagramLanguage,
                                      ".rtf");

                if (string.IsNullOrEmpty(tempRTFfile))
                {
                    string defaultTemplatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Documents", "template.rtf");
                    if (File.Exists(defaultTemplatePath))
                        tempRTFfile = defaultTemplatePath;
                }

                _currentRtfFilePath = tempRTFfile;
                RtfHelper.LoadRtfToRichTextBox(tempRTFfile, _richTextBox);
                PrepareDataGridForInput();
                MessageHelper.Success(LanguageService.Instance["template_loaded_successfully"]);
            }
            catch (Exception ex)
            {
                MessageHelper.Error($"{LanguageService.Instance["template_load_failed"]}: {ex.Message}");
                await BackToTemplateMode();
            }
        }

        /// <summary>
        /// 从数据表格中读取数据并进行投点
        /// 新的数据导入逻辑
        /// </summary>
        [RelayCommand]
        private void PlotDataFromGrid()
        {
            // 蛛网图模式下使用专用逻辑
            if (SpiderDiagramViewModel.IsSpiderPlotMode)
            {
                PlotSpiderDataFromGrid();
                return;
            }

            // 关闭数据状态栏提示
            IsDataStateReminderVisible = false;
            ClearDataGridPlotRefreshPending();

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
            ClearScriptInvalidRowMarks(worksheet);

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

            // 2. 确定数据列（排除 Category 等 metadata 列，它们仅用于分组）
            var requiredSeries = PlotDataGridHelper.ParseScriptDataColumns(scriptDefinition.RequiredDataSeries);
            if (requiredSeries.Count == 0)
            {
                MessageHelper.Error(LanguageService.Instance["script_not_defined_in_template"]);
                return;
            }

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

            // 先对数据进行分组（Category 允许任意非空文本，不要求数值）
            var groupedData = dataTable.AsEnumerable()
                .Select(row => new { Row = row, OriginalRowIndex = row.Field<int>("OriginalRowIndex") })
                .Select(x => new
                {
                    x.Row,
                    x.OriginalRowIndex,
                    CategoryValue = PlotDataGridHelper.GetRowColumnText(x.Row, categoryColumn)
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.CategoryValue))
                .GroupBy(x => x.CategoryValue);

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
            var invalidCoordinateMessages = new List<string>();
            var invalidCoordinateRows = new HashSet<int>();

            void RegisterInvalidCoordinateRow(int rowIndex, string? reason)
            {
                if (invalidCoordinateRows.Add(rowIndex))
                {
                    MarkScriptInvalidRow(worksheet, rowIndex);
                    invalidCoordinateMessages.Add(string.Format(
                        Lang("plot_invalid_row_example", "Row {0}: {1}"),
                        rowIndex + 1,
                        reason ?? Lang("plot_script_result_invalid_coordinate", "Script result is not a valid coordinate.")));
                }
            }

            // 创建Jint引擎并注入trace函数
            var engine = new Jint.Engine();
            var engineLogs = new List<string>(); // 收集日志
            JintHelper.InjectTraceFunction(engine, engineLogs);

            _usePreparedCoordinateScript = JintHelper.TryPrepareCoordinateScript(engine, scriptDefinition.ScriptBody);
            if (!_usePreparedCoordinateScript)
            {
                MessageHelper.Error((LanguageService.Instance["script_execution_failed"] ?? "Script execution failed.") +
                    Lang("plot_script_must_return_array_explicitly", "Scripts must explicitly return an array, for example: return [x, y];"));
                return;
            }

            try
            {
            // ===================================
            //  根据图表类型选择不同的投点逻辑
            // ===================================
            if (BaseMapType == "Ternary")
            {
                // --- 三元图投点逻辑 ---
                var triangularAxis = WpfPlot1.Plot.GetPlottables().OfType<ScottPlot.Plottables.TriangularAxis>().FirstOrDefault();
                if (triangularAxis == null)
                {
                    MessageHelper.Error(Lang("plot_ternary_axis_not_found", "Error: triangular axis object not found in the plot."));
                    return;
                }

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
                            var ternaryValues = CalculateCoordinatesUsingScript(engine, row, dataColumns, out var invalidReason);
                            // 脚本必须为三元图返回三个值
                            if (ternaryValues != null && ternaryValues.Length == 3)
                            {
                                double bottomVal = ternaryValues[0];
                                double leftVal = ternaryValues[1];
                                double rightVal = ternaryValues[2];

                                if (bottomVal < 0 || leftVal < 0 || rightVal < 0)
                                {
                                    RegisterInvalidCoordinateRow(rowIndex, Lang("plot_ternary_negative_component", "Ternary coordinate components cannot be negative."));
                                    continue;
                                }

                                // 计算三个分量的和
                                double sum = bottomVal + leftVal + rightVal;

                                // 如果和接近0，视为无效数据，跳过
                                if (Math.Abs(sum) < 1e-9)
                                {
                                    RegisterInvalidCoordinateRow(rowIndex, Lang("plot_ternary_sum_zero", "The ternary coordinate components sum to 0 and cannot be normalized."));
                                    continue;
                                }

                                // 三元图只关心三个分量的相对比例，统一归一化避免未归一化数据产生偏移。
                                bottomVal /= sum;
                                leftVal /= sum;
                                rightVal /= sum;

                                // 将三元坐标转换为笛卡尔坐标
                                var cartesianCoord = triangularAxis.GetCoordinates(bottomVal, leftVal, rightVal);
                                if (!IsFiniteValue(cartesianCoord.X) || !IsFiniteValue(cartesianCoord.Y))
                                {
                                    RegisterInvalidCoordinateRow(rowIndex, Lang("plot_ternary_conversion_invalid", "The ternary coordinate conversion did not produce a valid coordinate."));
                                    continue;
                                }

                                cartesianCoords.Add(cartesianCoord);
                                rowIndices.Add(rowIndex);
                            }
                            else
                            {
                                RegisterInvalidCoordinateRow(rowIndex, invalidReason ?? Lang("plot_ternary_script_return_three_finite_values", "Ternary diagram scripts must return 3 valid finite numbers."));
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"计算三元坐标时出错: {ex.Message}");
                            RegisterInvalidCoordinateRow(rowIndex, $"{Lang("plot_script_execution_failed_with_reason", "Script execution failed")}: {ex.Message}");
                        }
                    }

                    if (!cartesianCoords.Any()) continue;

                    var scatterDefForCategory = new ScatterDefinition
                    {
                        Name = categoryName,
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

                    categoryViewModel.ScatterNameChanged += (layer, scatterName) => SyncScatterNameToDataGrid(layer, scatterName);
                    AttachLayerPlotHandlers(categoryViewModel, preserveAxisLimits: false);
                    rootDataNode.Children.Add(categoryViewModel);
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
                            var coordinates = CalculateCoordinatesUsingScript(engine, row, dataColumns, out var invalidReason);
                            // 脚本必须为笛卡尔坐标图返回两个值
                            if (coordinates != null && coordinates.Length == 2)
                            {
                                xs.Add(coordinates[0]);
                                ys.Add(coordinates[1]);
                                rowIndices.Add(rowIndex);
                            }
                            else
                            {
                                RegisterInvalidCoordinateRow(rowIndex, invalidReason ?? Lang("plot_cartesian_script_return_two_finite_values", "Cartesian diagram scripts must return 2 valid finite numbers."));
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"计算坐标时出错: {ex.Message}");
                            RegisterInvalidCoordinateRow(rowIndex, $"{Lang("plot_script_execution_failed_with_reason", "Script execution failed")}: {ex.Message}");
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
                        Name = categoryName,
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

                    categoryViewModel.ScatterNameChanged += (layer, scatterName) => SyncScatterNameToDataGrid(layer, scatterName);
                    AttachLayerPlotHandlers(categoryViewModel, preserveAxisLimits: false);
                    rootDataNode.Children.Add(categoryViewModel);
                }
            }

            if (invalidCoordinateRows.Any())
            {
                MessageHelper.Warning(FormatSkippedRowsWarning(invalidCoordinateMessages, invalidCoordinateRows.Count));
            }

            if (!rootDataNode.Children.Any())
            {
                RemoveLayerSubtree(rootDataNode);
            }

            // 刷新图表和图例
            WpfPlot1.Plot.Legend.IsVisible = true;
            //WpfPlot1.Refresh();
            RefreshPlotFromLayers();
            CenterPlot();
            }
            finally
            {
                _usePreparedCoordinateScript = false;
            }
        }

        /// <summary>
        /// 蛛网图模式：从数据表格读取数据并渲染蛛网图
        /// </summary>
        private void PlotSpiderDataFromGrid()
        {
            if (_dataGrid == null || WpfPlot1 == null)
                return;

            ClearDataGridPlotRefreshPending();

            // 启用标准化时必须有方案；禁用标准化时则不依赖方案
            if (SpiderDiagramViewModel.IsNormalizationEnabled && SpiderDiagramViewModel.SelectedStandard == null)
                return;

            var worksheet = _dataGrid.Worksheets[0];
            var elements = SpiderDiagramViewModel.ElementOrder.ToList();

            // 读取表头，建立列名到列索引的映射
            var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int col = 0; col < worksheet.ColumnCount; col++)
            {
                var headerText = worksheet.ColumnHeaders[col].Text;
                if (!string.IsNullOrEmpty(headerText))
                {
                    columnMap[headerText] = col;
                }
            }

            // 找到样品/分类名称列索引，优先使用 Category，兼容旧表头 Sample
            int sampleColIdx = columnMap.ContainsKey("Category")
                ? columnMap["Category"]
                : (columnMap.ContainsKey("Sample") ? columnMap["Sample"] : -1);

            // 预编译元素列索引，避免逐行反复按名称查找
            var elementColumns = new List<(string Element, int ColumnIndex)>(elements.Count);
            foreach (var element in elements)
            {
                if (columnMap.TryGetValue(element, out int columnIndex))
                {
                    elementColumns.Add((element, columnIndex));
                }
            }

            // 直接构建样品对象，避免字符串字典到样品对象的二次转换
            var parsedSamples = new List<SpiderSampleData>();
            var sourceRowIndices = new List<int>();

            for (int row = 0; row <= worksheet.MaxContentRow; row++)
            {
                // 检查是否有任何有效数据
                bool hasData = false;
                var elementValues = new Dictionary<string, double>(elementColumns.Count, StringComparer.OrdinalIgnoreCase);

                foreach (var (element, columnIndex) in elementColumns)
                {
                    var cellValue = worksheet[row, columnIndex]?.ToString();
                    if (!string.IsNullOrWhiteSpace(cellValue)
                        && double.TryParse(cellValue, out double numericValue))
                    {
                        elementValues[element] = numericValue;
                        hasData = true;
                    }
                }

                if (hasData)
                {
                    string sampleName = sampleColIdx >= 0
                        ? (worksheet[row, sampleColIdx]?.ToString() ?? $"Sample {parsedSamples.Count + 1}")
                        : $"Sample {parsedSamples.Count + 1}";

                    parsedSamples.Add(new SpiderSampleData
                    {
                        Name = sampleName,
                        ElementValues = elementValues,
                        SourceRowIndices = new List<int> { row }
                    });
                    sourceRowIndices.Add(row);
                }
            }

            if (parsedSamples.Count == 0)
            {
                MessageHelper.Warning(LanguageService.Instance["no_valid_data_found"] ?? "No valid data found in the table.");
                return;
            }

            // 加载数据到 SpiderDiagramViewModel
            SpiderDiagramViewModel.LoadSamples(parsedSamples);

            // 模板化蛛网图模式下，LoadSamplesFromData() 会通过 PlotSettingsChanged
            // 触发统一重绘，这里不要再手动构建/渲染一次，否则会造成重复图层和重复图例。
            if (!(SpiderDiagramViewModel.IsSpiderPlotMode && CurrentTemplate?.TemplateType == "Spider"))
            {
                // 兼容旧模式：更新图层树（显示数据图层）
                BuildSpiderLayerTree();
            }
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
                string localListPath = FileHelper.GetDataPath("PlotData", "GraphMapList.json");

                // 计算本地文件的哈希值
                string localHash = UpdateHelper.ComputeFileMd5(localListPath);

                // 获取本地 PlotTemplateCategories.json
                string localCategoryPath = FileHelper.GetDataPath("PlotData", "PlotTemplateCategories.json");

                // 从服务器获取 server_info.json
                string jsonContent = await UpdateHelper.GetUrlContentAsync();

                // 反序列化 JSON
                var serverInfo = JsonSerializer.Deserialize<ServerInfo>(jsonContent);

                if (serverInfo == null) return;

                // 检查数据库文件是否存在
                string dbPath = FileHelper.GetDataPath("PlotData", "Templates.db");
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
                    // 列表文件已是最新：仍用本地 GraphMapList 与数据库对账（清理下架项、同步 Hash）
                    if (File.Exists(localListPath))
                    {
                        bool dbChanged = await Task.Run(() =>
                        {
                            string listContent = File.ReadAllText(localListPath);
                            var templateList = JsonSerializer.Deserialize<List<GraphMapTemplateService.JsonTemplateItem>>(listContent);
                            return templateList != null
                                && GraphMapTemplateService.SyncOfficialTemplatesFromServerList(templateList);
                        });

                        if (dbChanged)
                        {
                            await RefreshTemplateLibraryAfterDataChangeAsync();
                        }
                    }

                    // 检查分类文件是否缺失 (如果不缺失，即使Hash不一致也忽略，等待下次列表更新)
                    if (!File.Exists(localCategoryPath))
                    {
                        // 本地缺失分类文件，静默补全
                        await PerformCategoryListUpdate(serverInfo.ListPlotCategoriesHash, showMessages: false);
                    }

                    // 仅手动检查时提示"当前模板列表已是最新版本"
                    if (!_isAutoCheckingTemplateUpdate)
                    {
                        // 当前模板列表已是最新版本。
                        MessageHelper.Success(LanguageService.Instance["current_template_list_latest_version"]);
                    }
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
            string localListPath = FileHelper.GetDataPath("PlotData", "GraphMapList.json");

            try
            {
                // 服务器端的 GraphMapList.json 下载地址
                string listDownloadUrl = OfficialContentEndpoints.GraphMapListUrl;

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

                // 同步本地数据库与新的模板列表（含下架删除与 FileHash 更新）
                await Task.Run(() =>
                {
                    string newListContent = File.ReadAllText(localListPath);
                    var newTemplateList = JsonSerializer.Deserialize<List<GraphMapTemplateService.JsonTemplateItem>>(newListContent);
                    if (newTemplateList != null)
                        GraphMapTemplateService.SyncOfficialTemplatesFromServerList(newTemplateList);
                });

                // 刷新 UI (重新加载卡片)；绘图模式下仅标记 dirty，不强制返回模板库
                await RefreshTemplateLibraryAfterDataChangeAsync();
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
            string localListPath = FileHelper.GetDataPath("PlotData", "PlotTemplateCategories.json");

            try
            {
                // 服务器端的 PlotTemplateCategories.json 下载地址
                string listDownloadUrl = OfficialContentEndpoints.PlotTemplateCategoriesUrl;

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
        /// "添加箭头"按钮的命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanAddDrawingObject))]
        private void AddArrow()
        {
            IsAddingArrow = true;
        }
        
        /// <summary>
        /// "添加函数"按钮的命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanAddDrawingObject))]
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
            EnsureLayerRefreshHandler(funcLayer);
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

        private static bool ShouldRenderLayer(IPlotLayer layer)
        {
            // 坐标轴并不是普通 plottable，隐藏时也必须执行 Render() 才能把可见性同步回 Plot.Axes
            return layer is AxisLayerItemViewModel || ((LayerItemViewModel)layer).IsVisible;
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

        private bool CanDeleteSelectedObject() => GetSelectedLayersForDeletion().Count > 0;

        private List<LayerItemViewModel> GetSelectedLayersForDeletion()
        {
            var layersToDelete = SelectedLayers
                .Where(layer => layer != null && layer.CanDelete)
                .Distinct()
                .ToList();

            if (layersToDelete.Count == 0 && _selectedLayer != null && _selectedLayer.CanDelete)
            {
                layersToDelete.Add(_selectedLayer);
            }

            return layersToDelete;
        }

        private static bool HasBoundData(LayerItemViewModel layer) =>
            layer is ScatterLayerItemViewModel or SpiderSampleLayerItemViewModel;

        private List<int> CollectSourceRowIndices(IEnumerable<LayerItemViewModel> layers)
        {
            var rowIndices = new HashSet<int>();

            foreach (var layer in layers)
            {
                switch (layer)
                {
                    case ScatterLayerItemViewModel scatterLayer:
                        foreach (var rowIndex in scatterLayer.OriginalRowIndices.Where(index => index >= 0))
                        {
                            rowIndices.Add(rowIndex);
                        }
                        break;

                    case SpiderSampleLayerItemViewModel spiderLayer:
                        foreach (var rowIndex in spiderLayer.Samples
                                     .SelectMany(sample => sample.SourceRowIndices)
                                     .Where(index => index >= 0))
                        {
                            rowIndices.Add(rowIndex);
                        }
                        break;
                }
            }

            return rowIndices.OrderBy(index => index).ToList();
        }

        private List<(SpiderSampleData Sample, int Index)> CaptureSpiderSamplesForDeletion(IEnumerable<LayerItemViewModel> layers)
        {
            if (SpiderDiagramViewModel == null)
            {
                return new List<(SpiderSampleData Sample, int Index)>();
            }

            var targetSamples = layers
                .OfType<SpiderSampleLayerItemViewModel>()
                .SelectMany(layer => layer.Samples)
                .ToHashSet();

            var capturedSamples = new List<(SpiderSampleData Sample, int Index)>();
            for (int i = 0; i < SpiderDiagramViewModel.Samples.Count; i++)
            {
                var sample = SpiderDiagramViewModel.Samples[i];
                if (targetSamples.Contains(sample))
                {
                    capturedSamples.Add((sample, i));
                }
            }

            return capturedSamples;
        }

        private void RemoveSpiderSamplesFromCurrentPlot(IReadOnlyList<(SpiderSampleData Sample, int Index)> capturedSamples)
        {
            if (SpiderDiagramViewModel == null || capturedSamples.Count == 0)
            {
                return;
            }

            foreach (var capturedSample in capturedSamples.OrderByDescending(item => item.Index))
            {
                if (capturedSample.Index >= 0 &&
                    capturedSample.Index < SpiderDiagramViewModel.Samples.Count &&
                    ReferenceEquals(SpiderDiagramViewModel.Samples[capturedSample.Index], capturedSample.Sample))
                {
                    SpiderDiagramViewModel.Samples.RemoveAt(capturedSample.Index);
                    continue;
                }

                SpiderDiagramViewModel.Samples.Remove(capturedSample.Sample);
            }
        }

        private void RestoreSpiderSamplesToCurrentPlot(IReadOnlyList<(SpiderSampleData Sample, int Index)> capturedSamples)
        {
            if (SpiderDiagramViewModel == null || capturedSamples.Count == 0)
            {
                return;
            }

            foreach (var capturedSample in capturedSamples.OrderBy(item => item.Index))
            {
                if (SpiderDiagramViewModel.Samples.Contains(capturedSample.Sample))
                {
                    continue;
                }

                if (capturedSample.Index >= 0 && capturedSample.Index <= SpiderDiagramViewModel.Samples.Count)
                {
                    SpiderDiagramViewModel.Samples.Insert(capturedSample.Index, capturedSample.Sample);
                }
                else
                {
                    SpiderDiagramViewModel.Samples.Add(capturedSample.Sample);
                }
            }
        }

        private void DeleteLayersFromTreeWithUndo(IReadOnlyList<LayerItemViewModel> layersToDelete, bool removeSpiderSamples = false)
        {
            if (layersToDelete == null || layersToDelete.Count == 0)
            {
                return;
            }

            var deletedInfos = new List<(LayerItemViewModel Layer, ScottPlot.IPlottable? Plottable, CategoryLayerItemViewModel? Parent, int Index, int ParentIndex)>();
            foreach (var layer in layersToDelete)
            {
                var parent = FindParentLayer(LayerTree, layer);
                int index = parent != null ? parent.Children.IndexOf(layer) : LayerTree.IndexOf(layer);
                int parentIndex = parent != null ? LayerTree.IndexOf(parent) : -1;
                deletedInfos.Add((layer, layer.Plottable, parent, index, parentIndex));
            }

            var capturedSpiderSamples = removeSpiderSamples
                ? CaptureSpiderSamplesForDeletion(layersToDelete)
                : new List<(SpiderSampleData Sample, int Index)>();

            Action redo = () =>
            {
                if (removeSpiderSamples)
                {
                    RemoveSpiderSamplesFromCurrentPlot(capturedSpiderSamples);
                }

                foreach (var info in deletedInfos)
                {
                    if (info.Parent != null)
                    {
                        if (info.Parent.Children.Contains(info.Layer))
                        {
                            info.Parent.Children.Remove(info.Layer);
                        }

                        if (info.Parent.Children.Count == 0 && LayerTree.Contains(info.Parent))
                        {
                            LayerTree.Remove(info.Parent);
                        }
                    }
                    else if (LayerTree.Contains(info.Layer))
                    {
                        LayerTree.Remove(info.Layer);
                    }
                }

                CancelSelected();
                RefreshPlotFromLayers(true);
            };

            Action undo = () =>
            {
                if (removeSpiderSamples)
                {
                    RestoreSpiderSamplesToCurrentPlot(capturedSpiderSamples);
                }

                for (int i = deletedInfos.Count - 1; i >= 0; i--)
                {
                    var info = deletedInfos[i];
                    if (info.Parent != null)
                    {
                        if (!LayerTree.Contains(info.Parent))
                        {
                            if (info.ParentIndex >= 0 && info.ParentIndex <= LayerTree.Count)
                            {
                                LayerTree.Insert(info.ParentIndex, info.Parent);
                            }
                            else
                            {
                                LayerTree.Add(info.Parent);
                            }
                        }

                        if (!info.Parent.Children.Contains(info.Layer))
                        {
                            if (info.Index >= 0 && info.Index <= info.Parent.Children.Count)
                            {
                                info.Parent.Children.Insert(info.Index, info.Layer);
                            }
                            else
                            {
                                info.Parent.Children.Add(info.Layer);
                            }
                        }
                    }
                    else if (!LayerTree.Contains(info.Layer))
                    {
                        if (info.Index >= 0 && info.Index <= LayerTree.Count)
                        {
                            LayerTree.Insert(info.Index, info.Layer);
                        }
                        else
                        {
                            LayerTree.Add(info.Layer);
                        }
                    }
                }

                RefreshPlotFromLayers(true);
            };

            AddUndoState(undo, redo);
            redo();
        }

        private bool DeleteWorksheetRows(IReadOnlyList<int> rowIndices)
        {
            if (_dataGrid == null || _dataGrid.Worksheets.Count == 0 || rowIndices == null || rowIndices.Count == 0)
            {
                return false;
            }

            var worksheet = _dataGrid.Worksheets[0];
            var validRows = rowIndices
                .Where(row => row >= 0 && row < worksheet.RowCount)
                .Distinct()
                .OrderBy(row => row)
                .ToList();

            if (validRows.Count == 0)
            {
                return false;
            }

            bool deleteAllRows = validRows.Count >= worksheet.RowCount;
            var rowsToDelete = deleteAllRows
                ? validRows.Where(row => row != 0).ToList()
                : validRows;

            if (rowsToDelete.Count > 0)
            {
                int groupStart = rowsToDelete[^1];
                int groupCount = 1;

                for (int i = rowsToDelete.Count - 2; i >= 0; i--)
                {
                    int currentRow = rowsToDelete[i];
                    if (currentRow == groupStart - 1)
                    {
                        groupStart = currentRow;
                        groupCount++;
                    }
                    else
                    {
                        worksheet.DeleteRows(groupStart, groupCount);
                        groupStart = currentRow;
                        groupCount = 1;
                    }
                }

                worksheet.DeleteRows(groupStart, groupCount);
            }

            if (deleteAllRows && worksheet.RowCount > 0)
            {
                for (int col = 0; col < worksheet.ColumnCount; col++)
                {
                    worksheet[0, col] = string.Empty;
                }
            }

            RestoreWorksheetSelectionToSafeCell(worksheet);
            UpdateSelectedCellDisplayText(0, 0);
            return true;
        }

        /// <summary>
        /// 删除当前选中的绘图对象。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanDeleteSelectedObject))]
        private async Task DeleteSelectedObject()
        {
            var layersToDelete = GetSelectedLayersForDeletion();
            if (layersToDelete.Count == 0)
            {
                MessageHelper.Warning(LanguageService.Instance["please_select_an_object_to_delete_first"]);
                return;
            }

            bool containsDataLayers = layersToDelete.Any(HasBoundData);
            if (containsDataLayers)
            {
                int result = await NotificationManager.Instance.ShowThreeButtonDialogAsync(
                    LanguageService.Instance["tips"] ?? "提示",
                    LanguageService.Instance["delete_selected_data_and_drawing_objects"] ?? "是否删除选中的数据及绘图对象？",
                    LanguageService.Instance["delete_all"] ?? "全部删除",
                    LanguageService.Instance["keep_data"] ?? "保留数据",
                    LanguageService.Instance["cancel_delete"] ?? "取消删除");

                if (result == 2)
                {
                    return;
                }

                if (result == 0)
                {
                    var rowIndices = CollectSourceRowIndices(layersToDelete);
                    if (rowIndices.Count == 0)
                    {
                        DeleteLayersFromTreeWithUndo(layersToDelete, removeSpiderSamples: layersToDelete.Any(layer => layer is SpiderSampleLayerItemViewModel));
                        MessageHelper.Warning(LanguageService.Instance["no_source_data_found_only_drawing_objects_deleted"] ?? "未找到可删除的源数据，已仅删除绘图对象。");
                        return;
                    }

                    try
                    {
                        CancelSelected();
                        if (!DeleteWorksheetRows(rowIndices))
                        {
                            MessageHelper.Warning(LanguageService.Instance["no_source_data_found"] ?? "未找到可删除的源数据。");
                            return;
                        }

                        ClearExistingPlottedData();
                        PlotDataFromGrid();
                        MessageHelper.Success(LanguageService.Instance["selected_data_and_drawing_objects_deleted"] ?? "已删除选中的数据及绘图对象。");
                    }
                    catch (Exception ex)
                    {
                        MessageHelper.Warning(LanguageService.Instance["delete_data_failed"] ?? "删除数据失败: " + ex.Message);
                    }

                    NotifySelectionDependentCommandStates();
                    return;
                }

                DeleteLayersFromTreeWithUndo(layersToDelete, removeSpiderSamples: layersToDelete.Any(layer => layer is SpiderSampleLayerItemViewModel));
                MessageHelper.Success(LanguageService.Instance["selected_drawing_objects_deleted_data_kept"] ?? "已删除选中的绘图对象，已保留数据。");
                NotifySelectionDependentCommandStates();
                return;
            }

            bool confirmed = await NotificationManager.Instance.ShowDialogAsync(
                LanguageService.Instance["tips"] ?? "提示",
                LanguageService.Instance["delete_selected_drawing_objects"] ?? "是否删除选中的绘图对象？",
                LanguageService.Instance["Confirm"] ?? "确认",
                LanguageService.Instance["Cancel"] ?? "取消");

            if (!confirmed)
            {
                return;
            }

            DeleteLayersFromTreeWithUndo(layersToDelete);
            MessageHelper.Success(LanguageService.Instance["object_deleted_successfully"]);
            NotifySelectionDependentCommandStates();
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

        private void CancelPendingPlotRefreshes()
        {
            _layerRefreshTimer.Stop();
            _pendingLayerRefreshPreserveLimits = true;

            _propertyEditRefreshTimer.Stop();
            _pendingPropertyEditRefreshMode = PropertyEditRefreshMode.None;
            _pendingPropertyEditUnsavedCheck = false;
        }

        private void ResetScottPlotAxisState()
        {
            if (WpfPlot1?.Plot == null)
            {
                return;
            }

            var axes = WpfPlot1.Plot.Axes;
            axes.Rules.Clear();
            axes.SquareUnits(false);
            axes.AutoScaler = new ScottPlot.AutoScalers.FractionalAutoScaler();
            axes.AutoScale(invertX: false, invertY: false);
        }

        private void RestoreCurrentTemplateFromOriginal()
        {
            if (string.IsNullOrEmpty(_originalTemplateJson))
            {
                return;
            }

            if (!GraphMapTemplateService.TryParseDiagramTemplate(_originalTemplateJson, out var template) || template == null)
            {
                return;
            }

            CurrentTemplate = template;
            HasUnsavedChanges = false;
        }

        private void ResetPlotStateToDefault()
        {
            CancelPendingPlotRefreshes();

            // 重置编辑模式
            ResetEditModes();

            // 清除语言覆盖
            ClearDiagramLanguageContext();

            // 重置编辑确认状态
            SetHasConfirmedEditMode(false);

            // 清空撤销/重做栈
            _undoStack.Clear();
            _redoStack.Clear();
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();

            // 清除所有绘图对象
            WpfPlot1.Plot.Clear();

            // 重置 ScottPlot 坐标轴与自动缩放状态（含 InvertedY）
            ResetScottPlotAxisState();

            // 重置坐标轴布局为默认值
            WpfPlot1.Plot.Layout.Default();

            // 移除所有与轴相关的自定义规则
            WpfPlot1.Plot.Axes.UnlinkAll();

            _triangularAxis = null;

            // 确保将全局变量重置为默认状态
            MainPlotViewModel.BaseMapType = String.Empty;
            MainPlotViewModel.Clockwise = true;

            CurrentTemplate = null;

            // 清除图层树和属性面板的绑定
            ClearLayerTree();
            PropertyGridModel = null;
            _selectedLayer = null;

            // 清除数据表格
            var worksheet = _dataGrid.Worksheets[0];
            worksheet.Reset();
            RestoreWorksheetSelectionToSafeCell(worksheet);
            UpdateSelectedCellDisplayText(0, 0);

            // 重置原始模板记录
            _originalTemplateJson = string.Empty;
            _originalHelpDocumentRtf = string.Empty;
            HasUnsavedChanges = false;

            // 刷新一次以应用所有重置
            WpfPlot1.Refresh();
        }

        /// <summary>
        /// 执行核心的保存操作，将CurrentTemplate保存到数据库
        /// </summary>
        private async Task PerformSave()
        {
            // 版本校验：若程序格式 x.y 高于模板，提示保存后将升级格式基线
            string appFormatVersion = ContentVersionHelper.GetDiagramFormatVersion();
            if (ContentVersionHelper.CompareFormat(appFormatVersion, CurrentTemplate.Version) > 0)
            {
                bool confirmUpgrade = await MessageHelper.ShowAsyncDialog(
                    LanguageService.Instance["template_upgrade_warning"],
                    LanguageService.Instance["Cancel"],
                    LanguageService.Instance["Confirm"]);

                if (!confirmUpgrade) return;
            }

            CurrentTemplate.Version = ContentVersionHelper.ResolveVersionOnSave(
                CurrentTemplate.Version,
                appFormatVersion);

            // 清空模板中原有的动态绘图元素列表并从 LayerTree 更新
            UpdateTemplateInfoFromLayers(CurrentTemplate);

            // 如果 _currentTemplateId 为空，说明是外部文件模式
            if (_currentTemplateId == null)
            {
                if (!string.IsNullOrEmpty(_currentTemplateFilePath))
                {
                    try
                    {
                        // 1. 保存 JSON
                        string jsonString = SerializeTemplate(CurrentTemplate);
                        await File.WriteAllTextAsync(_currentTemplateFilePath, jsonString);

                        // 2. 保存缩略图（680x480 灰度图）
                        try
                        {
                            string thumbnailPath = Path.ChangeExtension(_currentTemplateFilePath, ".jpg");
                            var colorImageBytes = WpfPlot1.Plot.GetImageBytes(680, 480, ScottPlot.ImageFormat.Jpeg);
                            
                            // 转换为灰度图
                            using (var ms = new MemoryStream(colorImageBytes))
                            {
                                var colorBitmap = new BitmapImage();
                                colorBitmap.BeginInit();
                                colorBitmap.CacheOption = BitmapCacheOption.OnLoad;
                                colorBitmap.StreamSource = ms;
                                colorBitmap.EndInit();
                                
                                // 转换为 8 位灰度图
                                var grayBitmap = new FormatConvertedBitmap();
                                grayBitmap.BeginInit();
                                grayBitmap.Source = colorBitmap;
                                grayBitmap.DestinationFormat = PixelFormats.Gray8;
                                grayBitmap.EndInit();
                                
                                // 编码为 JPEG 并保存
                                var encoder = new JpegBitmapEncoder();
                                encoder.Frames.Add(BitmapFrame.Create(grayBitmap));
                                using (var fileStream = new FileStream(thumbnailPath, FileMode.Create))
                                {
                                    encoder.Save(fileStream);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to save thumbnail: {ex.Message}");
                        }

                        // 3. 保存 RTF
                        try
                        {
                            string directory = Path.GetDirectoryName(_currentTemplateFilePath);
                            string lang = !string.IsNullOrEmpty(CurrentDiagramLanguage) ? CurrentDiagramLanguage : "en-US";
                            string rtfPath = Path.Combine(directory, $"{lang}.rtf");
                            
                            RtfHelper.SaveRichTextBoxToRtf(_richTextBox, rtfPath);
                        }
                        catch (Exception ex)
                        {
                             System.Diagnostics.Debug.WriteLine($"Failed to save RTF: {ex.Message}");
                        }

                        _originalTemplateJson = jsonString;
                        _originalHelpDocumentRtf = RtfHelper.GetRtfString(_richTextBox);
                        HasUnsavedChanges = false;

                        MessageHelper.Success(LanguageService.Instance["template_saved_successfully"]);
                    }
                    catch (Exception ex)
                    {
                        MessageHelper.Error(LanguageService.Instance["save_template_failed"] + $": {ex.Message}");
                    }
                }
                return;
            }

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
                    entity.PendingPublish = true;

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

                RequestTemplateLibraryRefresh();

                // 更新原始状态记录
                _originalTemplateJson = SerializeTemplate(CurrentTemplate);

                // 同步更新帮助文档基准 RTF
                _originalHelpDocumentRtf = RtfHelper.GetRtfString(_richTextBox);

                // 重置未保存状态
                HasUnsavedChanges = false;

                // 更新或生成新的缩略图（680x480 灰度图）
                try
                {
                    var colorImageBytes = WpfPlot1.Plot.GetImageBytes(680, 480, ScottPlot.ImageFormat.Jpeg);
                    
                    // 转换为灰度图
                    using (var ms = new MemoryStream(colorImageBytes))
                    {
                        var colorBitmap = new BitmapImage();
                        colorBitmap.BeginInit();
                        colorBitmap.CacheOption = BitmapCacheOption.OnLoad;
                        colorBitmap.StreamSource = ms;
                        colorBitmap.EndInit();
                        
                        // 转换为 8 位灰度图
                        var grayBitmap = new FormatConvertedBitmap();
                        grayBitmap.BeginInit();
                        grayBitmap.Source = colorBitmap;
                        grayBitmap.DestinationFormat = PixelFormats.Gray8;
                        grayBitmap.EndInit();
                        
                        // 编码为 JPEG 并上传到数据库
                        var encoder = new JpegBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(grayBitmap));
                        using (var uploadStream = new MemoryStream())
                        {
                            encoder.Save(uploadStream);
                            uploadStream.Position = 0;
                            
                            await Task.Run(() =>
                            {
                                GraphMapDatabaseService.Instance.UploadThumbnail(entity.Id, uploadStream);
                            });
                        }
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
            string tempFilePath = FileHelper.GetSaveFilePath2(
                title: LanguageService.Instance["save_as_csv_file"],
                filter: FileDialogFilterHelper.CsvFiles,
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
        /// 根据本地 FileHash 与服务器哈希解析官方模板更新状态，必要时同步写回 Status。
        /// </summary>
        private static TemplateState ResolveOfficialTemplateUpdateState(
            GraphMapTemplateEntity entity,
            string? serverHash,
            string? serverVersion = null,
            GraphMapDatabaseService? dbService = null)
        {
            if (entity == null)
                return TemplateState.NotDownloaded;

            if (string.Equals(entity.Status, "NOT_INSTALLED", StringComparison.Ordinal))
                return TemplateState.NotDownloaded;

            bool isHashSame = !string.IsNullOrEmpty(serverHash)
                && string.Equals(entity.FileHash, serverHash, StringComparison.OrdinalIgnoreCase);
            bool hasVersionUpdate = !string.IsNullOrWhiteSpace(serverVersion)
                && ContentVersionHelper.HasContentUpdate(entity.Version, serverVersion);

            string resolvedStatus = isHashSame && !hasVersionUpdate ? "UP_TO_DATE" : "OUTDATED";
            if (dbService != null && !string.Equals(entity.Status, resolvedStatus, StringComparison.Ordinal))
            {
                dbService.UpdateTemplateStatus(entity.Id, resolvedStatus);
                entity.Status = resolvedStatus;
            }

            return isHashSame && !hasVersionUpdate ? TemplateState.Ready : TemplateState.UpdateAvailable;
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
                    nextState = ResolveOfficialTemplateUpdateState(entity, card.ServerHash, card.ServerVersion, dbService);
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
        /// 开发者模式：强制从服务器重新下载官方图解模板
        /// </summary>
        [RelayCommand]
        private async Task ForceUpdateTemplate(TemplateCardViewModel card)
        {
            if (card == null || card.IsCustomTemplate || !IsDeveloperMode || !card.TemplateId.HasValue)
                return;

            if (card.State == TemplateState.Downloading)
                return;

            try
            {
                if (!await TryRefreshOfficialTemplateMetadataFromServer(card))
                {
                    MessageHelper.Error(LanguageService.Instance["force_update_template_not_found"]);
                    return;
                }

                await DownloadSingleTemplate(card, showNotification: true);
            }
            catch (Exception ex)
            {
                MessageHelper.Error(LanguageService.Instance["download_template_failed"] + $" {ex.Message}");
            }
        }

        /// <summary>
        /// 从服务器 GraphMapList 刷新指定官方模板的元数据（哈希、路径）
        /// </summary>
        private static async Task<bool> TryRefreshOfficialTemplateMetadataFromServer(TemplateCardViewModel card)
        {
            string json = await UpdateHelper.GetUrlContentAsync(OfficialContentEndpoints.GraphMapListUrl);
            var list = JsonSerializer.Deserialize<List<GraphMapTemplateService.JsonTemplateItem>>(json);
            if (list == null || list.Count == 0)
                return false;

            var item = list.FirstOrDefault(x =>
                Guid.TryParse(x.ID, out Guid id) && id == card.TemplateId!.Value);

            if (item == null)
                return false;

            card.ServerHash = item.FileHash;
            if (!string.IsNullOrWhiteSpace(item.Version))
                card.ServerVersion = ContentVersionHelper.Normalize(item.Version);
            if (!string.IsNullOrEmpty(item.GraphMapPath))
                card.TemplatePath = item.GraphMapPath;

            return true;
        }

        /// <summary>
        /// 下载单个模板的具体实现 (Updated for LiteDB)
        /// </summary>
        private async Task DownloadSingleTemplate(TemplateCardViewModel card, bool showNotification = true)
        {
            try
            {
                if (!card.TemplateId.HasValue) throw new Exception("Template ID is missing");

                // 切换状态
                card.State = TemplateState.Downloading;
                card.DownloadProgress = 0;

                // 准备路径: {BaseUrl}/{GraphMapPath}.zip
                string baseUrl = OfficialContentEndpoints.CosBaseUrl;
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

                        if (!GraphMapTemplateService.TryParseDiagramTemplate(jsonContent, out var template))
                            throw new Exception(LanguageService.Instance["diagram_template_corrupted_or_invalid"]);

                        if (!GraphMapTemplateService.IsVersionCompatible(template))
                            throw new Exception(LanguageService.Instance["template_version_too_high"]);

                        if (!string.IsNullOrEmpty(card.ServerHash))
                        {
                            string computedHash = GraphMapTemplateService.ComputeTemplateContentHash(template);
                            if (!string.Equals(computedHash, card.ServerHash, StringComparison.OrdinalIgnoreCase))
                                throw new Exception(LanguageService.Instance["downloaded_file_hash_mismatch"]);
                        }

                        // 准备实体
                        var entity = new GraphMapTemplateEntity
                        {
                            Id = card.TemplateId.Value,
                            GraphMapPath = card.TemplatePath,
                            Name = card.TemplatePath,
                            FileHash = card.ServerHash,
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
                using (var thumbStream = GraphMapDatabaseService.Instance.GetThumbnail(card.TemplateId.Value))
                {
                    if (thumbStream != null)
                    {
                        using var ms = new MemoryStream();
                        thumbStream.CopyTo(ms);
                        card.ThumbnailImage = CreateTemplateCardThumbnailImage(ms.ToArray());
                    }
                }

                // 仅在显示通知为true时显示成功通知
                if (showNotification)
                {
                    MessageHelper.Success($"{card.Name} " + LanguageService.Instance["template_ready"]);
                }
            }
            catch (Exception ex)
            {
                card.State = TemplateState.Error;
                // 仅在显示通知为true时显示错误通知
                if (showNotification)
                {
                    MessageHelper.Error(LanguageService.Instance["download_template_failed"] + $" {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 更新批量下载/批量更新按钮的显示状态
        /// </summary>
        private void UpdateBatchActionButtonsVisibility()
        {
            CheckForNewTemplatesDownload();
            CheckForOutdatedTemplatesUpdate();
        }

        /// <summary>
        /// 检查是否存在未安装的模板（仅本地不存在的，不包括云端有更新的）
        /// </summary>
        private void CheckForNewTemplatesDownload()
        {
            try
            {
                // 获取所有模板摘要
                var allTemplates = GraphMapDatabaseService.Instance.GetSummaries();
                
                // 筛选出 Status 为 NOT_INSTALLED 且为官方模板的模板
                var notInstalledTemplates = allTemplates.Where(t => 
                    !t.IsCustom && 
                    t.Status == "NOT_INSTALLED"
                ).ToList();

                // 更新按钮显示状态
                IsBatchDownloadButtonVisible = notInstalledTemplates.Count > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Check for new templates failed: {ex.Message}");
                IsBatchDownloadButtonVisible = false;
            }
        }

        /// <summary>
        /// 检查是否存在待更新的官方模板（已安装但云端有新版）
        /// </summary>
        private void CheckForOutdatedTemplatesUpdate()
        {
            try
            {
                var outdatedTemplates = GraphMapDatabaseService.Instance.GetSummaries()
                    .Where(t => !t.IsCustom && t.Status == "OUTDATED")
                    .ToList();

                IsBatchUpdateButtonVisible = outdatedTemplates.Count > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Check for outdated templates failed: {ex.Message}");
                IsBatchUpdateButtonVisible = false;
            }
        }

        /// <summary>
        /// 从本地 GraphMapList.json 加载服务器端模板哈希与版本
        /// </summary>
        private static Dictionary<Guid, (string Hash, string? Version)> LoadServerTemplateMetadata()
        {
            var result = new Dictionary<Guid, (string, string?)>();

            try
            {
                string localListPath = FileHelper.GetDataPath("PlotData", "GraphMapList.json");
                if (!File.Exists(localListPath))
                    return result;

                var serverTemplates = JsonSerializer.Deserialize<List<GraphMapTemplateService.JsonTemplateItem>>(
                    File.ReadAllText(localListPath));

                if (serverTemplates == null)
                    return result;

                foreach (var item in serverTemplates)
                {
                    if (!Guid.TryParse(item.ID, out Guid itemId))
                        continue;

                    string? version = string.IsNullOrWhiteSpace(item.Version)
                        ? null
                        : ContentVersionHelper.Normalize(item.Version);
                    result[itemId] = (item.FileHash, version);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Load server template metadata failed: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 批量下载所有未安装的模板
        /// </summary>
        [RelayCommand]
        private async Task BatchDownloadNewTemplates()
        {
            try
            {
                // 获取所有未安装的模板
                var allTemplates = GraphMapDatabaseService.Instance.GetSummaries();
                var notInstalledTemplates = allTemplates.Where(t => 
                    !t.IsCustom && 
                    t.Status == "NOT_INSTALLED"
                ).ToList();

                if (notInstalledTemplates.Count == 0)
                {
                    MessageHelper.Info(LanguageService.Instance["no_new_templates_to_download"] ?? "没有需要下载的新模板");
                    return;
                }

                // 弹窗确认
                string confirmMessage = string.Format(
                    LanguageService.Instance["batch_download_confirm_message"] ?? "检测到 {0} 个未下载的新模板，是否批量下载？",
                    notInstalledTemplates.Count);

                bool confirmed = await NotificationManager.Instance.ShowDialogAsync(
                    LanguageService.Instance["batch_download_title"] ?? "批量下载",
                    confirmMessage,
                    LanguageService.Instance["Confirm"] ?? "确认",
                    LanguageService.Instance["Cancel"] ?? "取消");

                if (!confirmed) return;

                // 创建取消令牌
                _batchDownloadCts?.Cancel();
                _batchDownloadCts = new CancellationTokenSource();
                var token = _batchDownloadCts.Token;

                // 显示遮罩层
                BatchOverlayTitle = LanguageService.Instance["batch_downloading"] ?? "批量下载中";
                IsBatchDownloadOverlayVisible = true;

                // 开始批量下载
                int successCount = 0;
                int failCount = 0;
                List<string> failedTemplates = new List<string>();
                int totalCount = notInstalledTemplates.Count;

                for (int i = 0; i < notInstalledTemplates.Count; i++)
                {
                    // 检查是否取消
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    var template = notInstalledTemplates[i];

                    // 更新进度信息
                    BatchDownloadProgressText = string.Format(
                        LanguageService.Instance["batch_download_progress"] ?? "正在下载：{0}/{1} - {2}",
                        i + 1,
                        totalCount,
                        template.Name ?? template.GraphMapPath);

                    try
                    {
                        // 创建临时卡片对象用于下载
                        var tempCard = new TemplateCardViewModel
                        {
                            TemplateId = template.Id,
                            Name = template.Name,
                            TemplatePath = template.GraphMapPath,
                            ServerHash = template.FileHash,
                            State = TemplateState.Loading
                        };

                        // 执行下载（不显示单个通知）
                        await DownloadSingleTemplate(tempCard, showNotification: false);
                        
                        if (tempCard.State == TemplateState.Ready)
                        {
                            successCount++;
                        }
                        else
                        {
                            failCount++;
                            failedTemplates.Add(template.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        failedTemplates.Add(template.Name);
                        Debug.WriteLine($"Download template {template.Name} failed: {ex.Message}");
                    }
                }

                // 隐藏遮罩层
                IsBatchDownloadOverlayVisible = false;

                // 检查是否被取消
                if (token.IsCancellationRequested)
                {
                    MessageHelper.Info(LanguageService.Instance["batch_download_cancelled"] ?? "批量下载已取消");
                }
                else
                {
                    // 显示结果
                    StringBuilder resultMessage = new StringBuilder();
                    resultMessage.AppendLine(string.Format(
                        LanguageService.Instance["batch_download_success_count"] ?? "成功下载：{0} 个",
                        successCount));
                    
                    if (failCount > 0)
                    {
                        resultMessage.AppendLine(string.Format(
                            LanguageService.Instance["batch_download_fail_count"] ?? "失败：{0} 个",
                            failCount));
                        if (failedTemplates.Count > 0)
                        {
                            resultMessage.AppendLine(LanguageService.Instance["failed_templates"] ?? "失败模板：");
                            resultMessage.AppendLine(string.Join(", ", failedTemplates.Take(5)));
                            if (failedTemplates.Count > 5)
                            {
                                resultMessage.AppendLine("...");
                            }
                        }
                    }

                    NotificationManager.Instance.Show(
                        LanguageService.Instance["batch_download_complete"] ?? "批量下载完成",
                        resultMessage.ToString(),
                        successCount == notInstalledTemplates.Count ? NotificationType.Success : NotificationType.Warning,
                        0);
                }

                await RefreshTemplateLibraryAfterDataChangeAsync();
            }
            catch (Exception ex)
            {
                // 隐藏遮罩层
                IsBatchDownloadOverlayVisible = false;
                MessageHelper.Error(LanguageService.Instance["batch_download_failed"] ?? "批量下载失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 批量更新所有待更新的官方模板
        /// </summary>
        [RelayCommand]
        private async Task BatchUpdateOutdatedTemplates()
        {
            try
            {
                var allTemplates = GraphMapDatabaseService.Instance.GetSummaries();
                var outdatedTemplates = allTemplates
                    .Where(t => !t.IsCustom && t.Status == "OUTDATED")
                    .ToList();

                if (outdatedTemplates.Count == 0)
                {
                    MessageHelper.Info(LanguageService.Instance["no_outdated_templates_to_update"] ?? "没有需要更新的模板");
                    return;
                }

                string confirmMessage = string.Format(
                    LanguageService.Instance["batch_update_confirm_message"] ?? "检测到 {0} 个模板有新版本，是否批量更新？",
                    outdatedTemplates.Count);

                bool confirmed = await NotificationManager.Instance.ShowDialogAsync(
                    LanguageService.Instance["batch_update_title"] ?? "批量更新",
                    confirmMessage,
                    LanguageService.Instance["Confirm"] ?? "确认",
                    LanguageService.Instance["Cancel"] ?? "取消");

                if (!confirmed) return;

                var serverMetadata = LoadServerTemplateMetadata();
                if (serverMetadata.Count == 0)
                {
                    MessageHelper.Error(LanguageService.Instance["batch_update_failed"] ?? "批量更新失败：无法读取模板清单");
                    return;
                }

                _batchDownloadCts?.Cancel();
                _batchDownloadCts = new CancellationTokenSource();
                var token = _batchDownloadCts.Token;

                BatchOverlayTitle = LanguageService.Instance["batch_updating"] ?? "批量更新中";
                IsBatchDownloadOverlayVisible = true;

                int successCount = 0;
                int failCount = 0;
                List<string> failedTemplates = new List<string>();
                int totalCount = outdatedTemplates.Count;

                for (int i = 0; i < outdatedTemplates.Count; i++)
                {
                    if (token.IsCancellationRequested)
                        break;

                    var template = outdatedTemplates[i];

                    BatchDownloadProgressText = string.Format(
                        LanguageService.Instance["batch_update_progress"] ?? "正在更新：{0}/{1} - {2}",
                        i + 1,
                        totalCount,
                        template.Name ?? template.GraphMapPath);

                    if (!serverMetadata.TryGetValue(template.Id, out var metadata))
                    {
                        failCount++;
                        failedTemplates.Add(template.Name ?? template.GraphMapPath);
                        continue;
                    }

                    try
                    {
                        var tempCard = new TemplateCardViewModel
                        {
                            TemplateId = template.Id,
                            Name = template.Name ?? template.GraphMapPath,
                            TemplatePath = template.GraphMapPath,
                            ServerHash = metadata.Hash,
                            ServerVersion = metadata.Version,
                            State = TemplateState.UpdateAvailable
                        };

                        await DownloadSingleTemplate(tempCard, showNotification: false);

                        if (tempCard.State == TemplateState.Ready)
                        {
                            successCount++;

                            var existingCard = TemplateCards.FirstOrDefault(c => c.TemplateId == template.Id);
                            if (existingCard != null)
                            {
                                existingCard.State = TemplateState.Ready;
                                if (tempCard.ThumbnailImage != null)
                                    existingCard.ThumbnailImage = tempCard.ThumbnailImage;
                            }
                        }
                        else
                        {
                            failCount++;
                            failedTemplates.Add(template.Name ?? template.GraphMapPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        failedTemplates.Add(template.Name ?? template.GraphMapPath);
                        Debug.WriteLine($"Update template {template.Name} failed: {ex.Message}");
                    }
                }

                IsBatchDownloadOverlayVisible = false;

                if (token.IsCancellationRequested)
                {
                    MessageHelper.Info(LanguageService.Instance["batch_update_cancelled"] ?? "批量更新已取消");
                }
                else
                {
                    StringBuilder resultMessage = new StringBuilder();
                    resultMessage.AppendLine(string.Format(
                        LanguageService.Instance["batch_update_success_count"] ?? "成功更新：{0} 个",
                        successCount));

                    if (failCount > 0)
                    {
                        resultMessage.AppendLine(string.Format(
                            LanguageService.Instance["batch_update_fail_count"] ?? "失败：{0} 个",
                            failCount));
                        if (failedTemplates.Count > 0)
                        {
                            resultMessage.AppendLine(LanguageService.Instance["failed_templates"] ?? "失败模板：");
                            resultMessage.AppendLine(string.Join(", ", failedTemplates.Take(5)));
                            if (failedTemplates.Count > 5)
                            {
                                resultMessage.AppendLine("...");
                            }
                        }
                    }

                    NotificationManager.Instance.Show(
                        LanguageService.Instance["batch_update_complete"] ?? "批量更新完成",
                        resultMessage.ToString(),
                        successCount == outdatedTemplates.Count ? NotificationType.Success : NotificationType.Warning,
                        0);
                }

                UpdateBatchActionButtonsVisibility();
                await RefreshTemplateLibraryAfterDataChangeAsync();
            }
            catch (Exception ex)
            {
                IsBatchDownloadOverlayVisible = false;
                MessageHelper.Error((LanguageService.Instance["batch_update_failed"] ?? "批量更新失败：") + ex.Message);
            }
        }

        /// <summary>
        /// 取消批量下载/更新
        /// </summary>
        [RelayCommand]
        private void CancelBatchDownload()
        {
            _batchDownloadCts?.Cancel();
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

        private LayerTreeIconKind GetCategoryIconKind(string name)
        {
            if (name == LanguageService.Instance["axes"]) return LayerTreeIconKind.Axis;
            if (name == LanguageService.Instance["polygon"]) return LayerTreeIconKind.Polygon;
            if (name == LanguageService.Instance["line"]) return LayerTreeIconKind.Line;
            if (name == LanguageService.Instance["function"] || name == "Function") return LayerTreeIconKind.Function;
            if (name == LanguageService.Instance["point"] || name == LanguageService.Instance["data_point"]) return LayerTreeIconKind.Point;
            if (name == LanguageService.Instance["arrow"]) return LayerTreeIconKind.Arrow;
            if (name == LanguageService.Instance["annotation"] || name == LanguageService.Instance["text"]) return LayerTreeIconKind.Text;
            if (name == LanguageService.Instance["Data"] || string.Equals(name, "Data", StringComparison.OrdinalIgnoreCase)) return LayerTreeIconKind.Line;
            return LayerTreeIconKind.Group;
        }

        /// <summary>
        /// 获取或创建指定名称的分类图层，并确保其处于正确的渲染顺序位置
        /// </summary>
        private CategoryLayerItemViewModel GetOrCreateCategory(string name)
        {
            var category = LayerTree.FirstOrDefault(c => c is CategoryLayerItemViewModel vm && vm.Name == name) as CategoryLayerItemViewModel;
            if (category != null) return category;

            category = new CategoryLayerItemViewModel(name, GetCategoryIconKind(name));
            EnsureLayerRefreshHandler(category);
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
