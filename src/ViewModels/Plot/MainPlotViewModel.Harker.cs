using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Helpers.PlotMarkers;
using GeoChemistryNexus.Interfaces;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Models.HarkerDiagram;
using GeoChemistryNexus.Services;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// Harker 多联图：设置页 → 进入绘图模式 → 多面板绘图。
    /// 绘图态复用现有 MainPlotPage 的绘图模式：每个面板是图层树里的一个分组，面板各自持有一个 ScottPlot 子图
    /// （第 1 个面板就是 WpfPlot1.Plot）。
    /// 对原有代码的侵入点（均为单个分支）：
    ///   RefreshPlotFromLayers / CenterPlot / ExportImg / CopyToClipboard / ResetPlotStateToDefault /
    ///   BackToTemplateMode / PrepareDataGridForInput / PlotDataFromGrid。
    /// 画布鼠标交互：原有的 WpfPlot1_MouseMove/Up/RightButtonUp 等全部基于“只有一个 Plot”，
    /// Harker 绘图态下改用本文件里的多面板版本（悬停高亮坐标轴、点击选中、右键取消选中、样点悬停联动）。
    /// </summary>
    public partial class MainPlotViewModel
    {
        private const int HarkerPanelUnit = 8;
        private const double HarkerClickTolerance = 5;
        private const double HarkerSampleHoverRadius = 12;

        private sealed class HarkerPanel
        {
            public required HarkerPanelSpec Spec { get; init; }
            public required GraphMapTemplate Template { get; init; }
            public required CategoryLayerItemViewModel Node { get; init; }
            public required List<CategoryLayerItemViewModel> Layers { get; init; }
            public required Plot Plot { get; set; }

            /// <summary>该面板是否绘制 X 轴刻度与标签（共享 X 轴时只有每列最底部的面板绘制）。</summary>
            public required bool ShowXAxis { get; init; }

            public int Index => Spec.Index;
            public HarkerVariable Variable => Spec.Variable;

            /// <summary>该面板里的“数据点”分类（每个 Category 一个散点图层）。</summary>
            public CategoryLayerItemViewModel? DataCategory { get; set; }

            public double[] Xs { get; set; } = [];
            public double[] Ys { get; set; } = [];
            public ScottPlot.Plottables.Marker? HoverMarker { get; set; }

            /// <summary>数据模式联动：数据表选中行对应的样点闪烁标记。</summary>
            public ScottPlot.Plottables.Marker? FlashMarker { get; set; }
        }

        /// <summary>进入绘图时对设置页的快照（绘图期间设置页不可改，要改请“返回设置”）。</summary>
        private sealed class HarkerPlotLayout
        {
            public required HarkerVariable XVariable { get; init; }
            public required int Rows { get; init; }
            public required int Columns { get; init; }
            public required bool ShareXAxis { get; init; }
            public required HarkerLegendMode Legend { get; init; }
            public required (int Row, int Column)? LegendCell { get; init; }
            public required IReadOnlyList<HarkerPanelSpec> Specs { get; init; }
        }

        /// <summary>从数据表读取的样品数据：所有面板共享同一组样品，缺失的 Y 值为 NaN（样品索引在各面板间一致，悬停联动靠它）。</summary>
        private sealed class HarkerDataSet
        {
            public required double[] Xs { get; init; }

            /// <summary>[面板序号][样品序号]。</summary>
            public required double[][] Ys { get; init; }

            /// <summary>样品所属分组（Category）序号，对应 <see cref="Groups"/>。</summary>
            public required int[] GroupIndex { get; init; }

            /// <summary>样品在数据表里的行号。</summary>
            public required int[] SourceRows { get; init; }

            /// <summary>
            /// 每个 Category 一个样式定义。所有面板里同一个 Category 的数据图层共用同一个定义，
            /// 所以在任意一个面板里改颜色 / 大小 / 形状 / 名称，其余面板和图例都会同步。
            /// </summary>
            public required List<ScatterDefinition> Groups { get; init; }
        }

        /// <summary>图例条目：来自某个 Category 的样式定义；没有数据时 Definition 为空（占位）。</summary>
        private sealed record HarkerLegendEntry(string Name, ScatterDefinition? Definition);

        private HarkerDataSet? _harkerData;

        private readonly List<HarkerPanel> _harkerPanels = new();
        private HarkerPlotLayout? _harkerLayout;
        private Plot? _harkerTitleHost;
        private Plot? _harkerLegendHost;

        // 标题 / 图例的遮罩需要持有它们的绘图对象
        private ScottPlot.Plottables.Annotation? _harkerTitleAnnotation;
        private ScottPlot.Color _harkerTitleColor = Colors.Black;
        private readonly List<(ScottPlot.Plottables.Marker Marker, HarkerLegendEntry Entry)> _harkerLegendMarkers = new();
        private Plot? _harkerLegendOwner;

        // 悬停状态
        private AxisLayerItemViewModel? _harkerHoveredAxis;
        private int _harkerHoverSampleIndex = -1;
        private int _harkerHoverPanelIndex = -1;
        // 数据模式联动（数据表选中行 ↔ 图上样点）
        private int _harkerFlashSample = -1;
        private bool _harkerFlashIsRed = true;

        private Point _harkerMouseDownPoint;
        private bool _harkerMouseDownValid;

        /// <summary>设置页 ViewModel。</summary>
        public HarkerDiagramViewModel HarkerDiagramViewModel { get; } = new();

        /// <summary>处于 Harker 工具（设置页或绘图态）。侧栏高亮、工具面板显示都依赖它。</summary>
        [ObservableProperty]
        private bool _isHarkerDiagramMode;

        /// <summary>处于 Harker 绘图态（已进入绘图模式）。</summary>
        [ObservableProperty]
        private bool _isHarkerPlotMode;

        /// <summary>X 轴联动缩放/平移（所有面板共享 X 轴范围）。</summary>
        [ObservableProperty]
        private bool _harkerLinkX = true;

        /// <summary>悬停某个面板的样点时，其余面板同步高亮同一样点。</summary>
        [ObservableProperty]
        private bool _harkerLinkHover = true;

        /// <summary>右侧内容区显示“工具设置页”（蛛网图 / Harker），此时隐藏模板卡片、搜索框、面包屑。</summary>
        public bool IsToolPanelMode => IsSpiderDiagramMode || IsHarkerDiagramMode;

        /// <summary>蛛网图、Harker 没有模板翻译数据，不显示“切换图解语言”。</summary>
        public bool IsDiagramLanguageButtonVisible => !IsSpiderDiagramMode && !IsHarkerPlotMode;

        partial void OnIsHarkerDiagramModeChanged(bool value)
        {
            OnPropertyChanged(nameof(IsToolPanelMode));
            UpdateTemplateCardsEmptyState();
        }

        partial void OnIsHarkerPlotModeChanged(bool value)
        {
            OnPropertyChanged(nameof(IsScriptSettingButtonVisible));
            OnPropertyChanged(nameof(IsDiagramLanguageButtonVisible));
            OnPropertyChanged(nameof(IsEditModeAvailable));
        }

        partial void OnHarkerLinkXChanged(bool value)
        {
            if (!IsHarkerPlotMode || _harkerLayout == null || _harkerPanels.Count == 0)
                return;

            // SharedAxes 没有公开的“取消联动”接口，切换时重建子图集合最稳妥（同时重置视图）
            Plot[] plots = CreateHarkerMultiplot();
            foreach (var panel in _harkerPanels)
                panel.Plot = plots[panel.Index];

            RefreshHarkerPlots(preserveAxisLimits: false);
        }

        partial void OnHarkerLinkHoverChanged(bool value)
        {
            if (!IsHarkerPlotMode)
                return;

            ApplyHarkerSampleHover(_harkerHoverPanelIndex, _harkerHoverSampleIndex, force: true);
        }

        #region 入口：设置页 / 进入绘图 / 返回设置

        /// <summary>
        /// 打开 Harker 多联图工具：右侧内容区显示设置页（与蛛网图相同的交互）。
        /// </summary>
        [RelayCommand]
        private void OpenHarkerDiagram()
        {
            // 收起所有现有分类
            IsPersonalExpanded = false;
            IsFavoriteExpanded = false;
            IsOfficialExpanded = false;
            IsRecentsExpanded = false;
            IsScatterDiagramMode = false;
            IsSpiderDiagramMode = false;

            // 清除 TreeView 选中项
            ClearTreeViewSelection(OfficialTemplatesNode);
            ClearTreeViewSelection(PersonalTemplatesNode);

            // 重置图表：清除所有绘图对象
            WpfPlot1?.Plot.Clear();
            WpfPlot1?.Refresh();

            // 重置选中状态
            CancelSelected();
            ClearLayerTree();

            IsHarkerDiagramMode = true;
            _lastSelectedCategory = "Harker";
            CurrentCategoryName = GetHarkerDiagramName();
        }

        /// <summary>
        /// 进入 Harker 绘图模式：按设置页的配置构建多联图。
        /// </summary>
        [RelayCommand]
        private async Task EnterHarkerPlotMode()
        {
            if (IsTransitionLoading || !HarkerDiagramViewModel.CanEnter)
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
                IsTernaryScatterDiagramMode = false;
                IsScatterDiagramMode = false;
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

                // 宿主模板：只用来保持界面外壳（属性面板、数据表、帮助文档）稳定，真正的图层在各面板分组里
                CurrentTemplate = ToolDiagramTemplateFactory.CreateScatter2DTemplate();
                CurrentTemplate.DefaultLanguage = ResolveBuiltInDiagramLanguage();
                SetLocalizedText(CurrentTemplate.Info.Title.Label, GetHarkerDiagramName());
                CurrentTemplate.Info.Title.Family = DiagramFontHelper.Detect(
                    GetHarkerDiagramName(), CurrentTemplate.Info.Title.Family);
                CurrentCategoryName = GetHarkerDiagramName();

                IsTemplateMode = false;
                IsPlotMode = true;
                RibbonTabIndex = 0;

                // 先置位：之后语言切换触发的 RefreshPlotFromLayers 等都会走 Harker 分支
                IsHarkerPlotMode = true;
                HarkerLinkX = true;

                CurrentDiagramLanguage = ResolveBuiltInDiagramLanguage();

                BuildHarkerPanels();
                RefreshPlotFromLayers();
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

        /// <summary>
        /// 返回设置页（设置保留在 HarkerDiagramViewModel 中）。
        /// </summary>
        private async Task BackToHarkerSettings()
        {
            await BeginTransitionLoadingAsync(enteringPlot: false);
            try
            {
                CancelPendingPlotRefreshes();

                IsTemplateMode = true;
                IsPlotMode = false;
                RibbonTabIndex = 0;
                IsShowTemplateInfo = false;

                // 清理绘图状态（其中会调用 LeaveHarkerPlot 恢复单图布局）
                ResetPlotStateToDefault();

                IsHarkerDiagramMode = true;
                CurrentCategoryName = GetHarkerDiagramName();
            }
            finally
            {
                await EndTransitionLoadingAsync();
            }
        }

        /// <summary>
        /// 退出 Harker 绘图态（由 ResetPlotStateToDefault 调用，可重复调用）。
        /// </summary>
        private void LeaveHarkerPlot()
        {
            if (!IsHarkerPlotMode)
                return;

            IsHarkerPlotMode = false;
            _harkerPanels.Clear();
            _harkerData = null;
            _harkerLayout = null;
            _harkerTitleHost = null;
            _harkerLegendHost = null;
            _harkerTitleAnnotation = null;
            _harkerLegendMarkers.Clear();
            _harkerLegendOwner = null;
            _harkerHoveredAxis = null;
            _harkerHoverSampleIndex = -1;
            _harkerHoverPanelIndex = -1;

            if (WpfPlot1 == null)
                return;

            // 仅保留 WpfPlot1.Plot，布局恢复为默认
            var multiplot = WpfPlot1.Multiplot;
            multiplot.Reset(WpfPlot1.Plot);
            multiplot.Layout = new ScottPlot.MultiplotLayouts.Rows();
            WpfPlot1.Plot.Layout.Default();
            WpfPlot1.Plot.Axes.Bottom.TickLabelStyle.IsVisible = true;
            WpfPlot1.Plot.Axes.Title.Label.Text = string.Empty;
            if (CurrentTemplate?.Info?.Legend is { } hostLegend)
                hostLegend.IsLayoutEditable = true;

            DetachHarkerMouseHandlers();
            AttachLegacyMouseHandlers();
        }

        #endregion

        #region 构建多联图

        /// <summary>
        /// 重建多联图的子图集合：[0..N-1] 数据面板（第 0 个必须就是 WpfPlot1.Plot），
        /// [N] 标题宿主，[N+1] 图例宿主（图例不是“嵌入面板”时）。
        /// </summary>
        private Plot[] CreateHarkerMultiplot()
        {
            var info = _harkerLayout ?? throw new InvalidOperationException("Harker 布局尚未初始化");
            int n = info.Specs.Count;
            bool hasLegendHost = info.Legend != HarkerLegendMode.Panel;

            // 以现有的 WpfPlot1.Plot 作为第 1 个面板，避免控件的 Plot 与多联图脱钩
            var multiplot = WpfPlot1.Multiplot;
            multiplot.Reset(WpfPlot1.Plot);

            // AddPlots(n) 的语义是“补齐/裁剪到总数为 n”，不是“再追加 n 个”
            multiplot.AddPlots(n + 1 + (hasLegendHost ? 1 : 0));
            Plot[] plots = multiplot.GetPlots().ToArray();

            _harkerTitleHost = plots[n];
            _harkerLegendHost = hasLegendHost ? plots[n + 1] : null;

            // 用细分网格实现「标题行 + (图例行) + 行×列 面板 + (图例列)」。
            // 注意：GridCell 构造函数实际参数顺序为 (rowIndex, colIndex, rowCount, colCount, colSpan, rowSpan)，
            // 第 5 个参数决定宽度，第 6 个参数决定高度。
            int panelRowUnits = info.Rows * HarkerPanelUnit;
            int panelColumnUnits = info.Columns * HarkerPanelUnit;
            int barUnits = Math.Max(1, (int)Math.Round(panelRowUnits * 0.07));
            int sideUnits = Math.Max(2, (int)Math.Round(panelColumnUnits * 0.14));

            bool above = info.Legend == HarkerLegendMode.Above;
            bool below = info.Legend == HarkerLegendMode.Below;
            bool right = info.Legend == HarkerLegendMode.Right;
            bool left = info.Legend == HarkerLegendMode.Left;

            int rowCount = barUnits + (above || below ? barUnits : 0) + panelRowUnits;
            int colCount = panelColumnUnits + (right || left ? sideUnits : 0);
            int panelColumnOffset = left ? sideUnits : 0;
            int panelRowOffset = barUnits + (above ? barUnits : 0);

            var layout = new ScottPlot.MultiplotLayouts.CustomGrid();
            layout.Set(plots[n], new GridCell(0, 0, rowCount, colCount, colCount, barUnits));

            foreach (var spec in info.Specs)
            {
                layout.Set(plots[spec.Index], new GridCell(
                    panelRowOffset + spec.Row * HarkerPanelUnit,
                    panelColumnOffset + spec.Column * HarkerPanelUnit,
                    rowCount, colCount, HarkerPanelUnit, HarkerPanelUnit));
            }

            if (_harkerLegendHost != null)
            {
                GridCell cell = info.Legend switch
                {
                    HarkerLegendMode.Above => new GridCell(barUnits, 0, rowCount, colCount, colCount, barUnits),
                    HarkerLegendMode.Below => new GridCell(rowCount - barUnits, 0, rowCount, colCount, colCount, barUnits),
                    HarkerLegendMode.Right => new GridCell(panelRowOffset, panelColumnUnits, rowCount, colCount, sideUnits, panelRowUnits),
                    HarkerLegendMode.Left => new GridCell(panelRowOffset, 0, rowCount, colCount, sideUnits, panelRowUnits),
                    _ => new GridCell(
                        panelRowOffset + (info.LegendCell?.Row ?? 0) * HarkerPanelUnit,
                        panelColumnOffset + (info.LegendCell?.Column ?? 0) * HarkerPanelUnit,
                        rowCount, colCount, HarkerPanelUnit, HarkerPanelUnit)
                };
                layout.Set(_harkerLegendHost, cell);
            }

            multiplot.Layout = layout;

            if (HarkerLinkX)
                multiplot.SharedAxes.ShareX(plots.Take(n));

            return plots;
        }

        /// <summary>
        /// 图例位置由设置页决定；宿主模板的图例定义（属性面板里编辑的对象）在这里按设置页初始化：
        /// 对齐方式 = 图例所在区域内的位置，排列方式 = 条目横排/竖排，之后用户在属性面板里的修改会被保留。
        /// </summary>
        private void ConfigureHarkerLegendDefinition()
        {
            var legend = CurrentTemplate?.Info?.Legend;
            if (legend == null || _harkerLayout == null)
                return;

            legend.IsVisible = true;
            legend.IsLayoutEditable = false;
            legend.Alignment = _harkerLayout.Legend == HarkerLegendMode.Panel ? Alignment.UpperRight : Alignment.MiddleCenter;
            legend.Orientation = _harkerLayout.Legend is HarkerLegendMode.Above or HarkerLegendMode.Below
                ? Orientation.Horizontal
                : Orientation.Vertical;
        }

        /// <summary>按设置页配置创建面板、图层树分组。</summary>
        private void BuildHarkerPanels()
        {
            if (WpfPlot1 == null)
                return;

            // CurrentTemplate / 语言切换会按宿主模板重建一遍图层树（根节点下的“坐标轴 X/Y”），
            // 这些图层对多联图没有意义，先清掉
            ClearLayerTree();
            _harkerPanels.Clear();
            _harkerData = null;

            var settings = HarkerDiagramViewModel;
            var specs = settings.BuildPanelSpecs();
            _harkerLayout = new HarkerPlotLayout
            {
                XVariable = settings.XVariable,
                Rows = settings.Rows,
                Columns = settings.Columns,
                ShareXAxis = settings.ShareXAxis,
                Legend = settings.EffectiveLegendMode,
                LegendCell = settings.GetLegendCell(),
                Specs = specs
            };

            ConfigureHarkerLegendDefinition();
            ConfigureGridDefinitionCapabilities(CurrentTemplate);

            Plot[] plots = CreateHarkerMultiplot();

            if (!ReferenceEquals(WpfPlot1.Plot, plots[0]))
            {
                MessageHelper.Warning(LanguageService.GetString(
                    "harker_plot_mismatch",
                    "Harker：WpfPlot1.Plot 与第 1 个面板不是同一个对象，导出/鼠标等行为可能不一致。"));
            }

            // 旧的画布鼠标交互全部基于“只有一个 Plot”的假设，绘图态换成多面板版本，退出时恢复
            DetachLegacyMouseHandlers();
            AttachHarkerMouseHandlers();

            var x = _harkerLayout.XVariable;
            foreach (var spec in specs)
            {
                var y = spec.Variable;

                // 共享 X 轴：每列只有最底部的面板绘制 X 轴刻度与标签
                bool isBottomOfColumn = !specs.Any(s => s.Column == spec.Column && s.Row > spec.Row);
                bool showXAxis = !_harkerLayout.ShareXAxis || isBottomOfColumn;

                var template = ToolDiagramTemplateFactory.CreateScatter2DTemplate();
                foreach (var axis in template.Info.Axes.OfType<CartesianAxisDefinition>())
                {
                    // Harker 图解的坐标轴固定为线性，属性面板里不提供“缩放类型”
                    axis.IsScaleTypeSupported = false;

                    if (axis.Type == "Bottom")
                    {
                        // 不显示 X 轴的面板：标题文本直接在模板里置空。
                        // 图层的 Restore()（取消选中、导出面板等都会触发）会按模板重新写回轴标题，
                        // 只在渲染后临时清空的话会被它恢复出来。
                        SetLocalizedText(axis.Label, showXAxis ? x.AxisText : string.Empty);
                        axis.Minimum = x.DefaultMin;
                        axis.Maximum = x.DefaultMax;
                    }
                    else if (axis.Type == "Left")
                    {
                        SetLocalizedText(axis.Label, y.AxisText);
                        axis.Minimum = y.DefaultMin;
                        axis.Maximum = y.DefaultMax;
                    }

                    // 轴标题含化学式（上下标等）时，按内容自动设定字体
                    axis.Family = DiagramFontHelper.DetectAxisFamily(axis, DiagramLanguage);
                }

                var layers = CreateLayersFromTemplate(template, attachEvents: true);

                var node = new CategoryLayerItemViewModel($"{y.Key} - {x.Key}", LayerTreeIconKind.Group);
                EnsureLayerRefreshHandler(node);
                foreach (var category in layers)
                    node.Children.Add(category);

                LayerTree.Add(node);

                _harkerPanels.Add(new HarkerPanel
                {
                    Spec = spec,
                    Template = template,
                    Node = node,
                    Layers = layers,
                    Plot = plots[spec.Index],
                    ShowXAxis = showXAxis
                });
            }
        }

        #endregion

        #region 渲染

        /// <summary>
        /// 每个面板各自渲染自己分组下的图层（图层本身持有 Plot 无关的 Render(Plot)）。
        /// </summary>
        private void RefreshHarkerPlots(bool preserveAxisLimits)
        {
            if (WpfPlot1 == null || _harkerLayout == null || _harkerPanels.Count == 0)
                return;

            // 重绘后旧的高亮/悬停标记都失效了
            _harkerHoveredAxis = null;
            _harkerHoverSampleIndex = -1;
            _harkerHoverPanelIndex = -1;

            var legendEntries = GetHarkerLegendEntries();
            _harkerLegendMarkers.Clear();
            _harkerLegendOwner = null;

            foreach (var panel in _harkerPanels)
            {
                var plot = panel.Plot;

                AxisLimits? limits = preserveAxisLimits ? plot.Axes.GetLimits() : null;

                plot.Clear();

                // 每个面板不带单独的标题（图形级标题由标题宿主子图负责）
                plot.Axes.Title.Label.Text = string.Empty;

                foreach (var layer in FlattenTree(panel.Layers).OfType<IPlotLayer>().Where(ShouldRenderLayer))
                    layer.Render(plot);

                // 固定边距：所有面板使用完全相同的四边距，保证每个面板的绘图区宽高一致、行列对齐。
                // （共享 X 轴时，不显示 X 轴的面板也保留底部边距，否则最底行的绘图区会比其他行矮。）
                plot.Layout.Fixed(new PixelPadding(64, 14, 50, 10));

                // 绘图对象会在多次进入 / 重建之间复用（第 1 个面板就是 WpfPlot1.Plot），
                // 刻度标签的可见性必须每次显式设置，不能只在隐藏时设置
                plot.Axes.Bottom.TickLabelStyle.IsVisible = panel.ShowXAxis;
                if (!panel.ShowXAxis)
                    plot.Axes.Bottom.Label.Text = string.Empty;

                // 网格：所有面板共用宿主模板里的网格定义（工具栏“网格”按钮编辑的就是它）
                if (CurrentTemplate?.Info?.Grid is { } gridDefinition)
                    ApplyGridDefinitionToPlot(plot, gridDefinition);

                AddHarkerHoverMarker(plot, panel);
                AddHarkerFlashMarker(plot, panel);

                if (_harkerLayout.Legend == HarkerLegendMode.Panel && panel.Index == 0)
                    AddHarkerEmbeddedLegend(plot, legendEntries);
                else
                    plot.HideLegend();

                if (limits.HasValue)
                    plot.Axes.SetLimits(limits.Value);
                else
                    FitHarkerAxesToData(plot, panel);
            }

            RenderHarkerTitleHost();
            RenderHarkerLegendHost(legendEntries);

            ApplyHarkerFlashMarkers();
            ReapplySelectionVisualState();
            RebuildPlottableLayerLookup();
            WpfPlot1.Refresh();
        }

        /// <summary>图例条目：数据表里每个 Category 一项；还没有数据时没有图例。</summary>
        private IReadOnlyList<HarkerLegendEntry> GetHarkerLegendEntries()
        {
            if (_harkerData is { Groups.Count: > 0 })
                return _harkerData.Groups.Select(g => new HarkerLegendEntry(g.Name, g)).ToList();

            return [];
        }

        /// <summary>在图例宿主 / 嵌入图例用的子图里放一个带图例文本的标记，样式与数据图层一致，并登记以便遮罩时变暗。</summary>
        private void AddHarkerLegendMarker(Plot plot, double x, double y, HarkerLegendEntry entry, float size)
        {
            var marker = plot.Add.Marker(x, y, MarkerShape.FilledCircle, size, ScottPlot.Color.FromHex("#3C4A98"));
            marker.LegendText = entry.Name;
            ApplyHarkerLegendMarkerStyle(marker, entry);
            _harkerLegendMarkers.Add((marker, entry));
        }

        private static void ApplyHarkerLegendMarkerStyle(ScottPlot.Plottables.Marker marker, HarkerLegendEntry entry)
        {
            var definition = entry.Definition;
            if (definition == null)
                return;

            var fill = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(definition.Color));
            var stroke = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(definition.StrokeColor));
            PlotMarkerStyleApplier.Apply(marker.MarkerStyle, definition.MarkerShape, fill, definition.StrokeWidth, stroke);
        }
        /// <summary>
        /// 重建每个面板里的“数据点”分类：每个 Category 一个 <see cref="ScatterLayerItemViewModel"/>，
        /// 这样选中、属性面板、显示/隐藏、改名同步数据表等都直接复用已有的散点图层能力。
        /// </summary>
        private void RebuildHarkerDataLayers()
        {
            var data = _harkerData;
            string categoryName = LanguageService.Instance["data_point"];
            var legendSubscribed = new HashSet<ScatterDefinition>();

            foreach (var panel in _harkerPanels)
            {
                if (panel.DataCategory != null)
                {
                    panel.DataCategory.ClearEventSubscriptions();
                    panel.Node.Children.Remove(panel.DataCategory);
                    panel.Layers.Remove(panel.DataCategory);
                    panel.DataCategory = null;
                }

                if (data == null)
                    continue;

                var category = new CategoryLayerItemViewModel(categoryName, LayerTreeIconKind.Point);
                EnsureLayerRefreshHandler(category);
                category.IsExpanded = true;

                for (int g = 0; g < data.Groups.Count; g++)
                {
                    var points = new List<Coordinates>();
                    var rows = new List<int>();
                    for (int i = 0; i < data.Xs.Length; i++)
                    {
                        if (data.GroupIndex[i] != g)
                            continue;

                        double x = data.Xs[i];
                        double y = data.Ys[panel.Index][i];
                        if (double.IsNaN(x) || double.IsNaN(y))
                            continue;

                        points.Add(new Coordinates(x, y));
                        rows.Add(data.SourceRows[i]);
                    }

                    if (points.Count == 0)
                        continue;

                    var definition = data.Groups[g];
                    var layer = new ScatterLayerItemViewModel(definition)
                    {
                        Name = definition.Name,
                        DataPoints = points,
                        OriginalRowIndices = rows,
                        IsVisible = true
                    };

                    layer.ScatterNameChanged += (l, name) => SyncScatterNameToDataGrid(l, name);
                    AttachLayerPlotHandlers(layer, preserveAxisLimits: true);

                    // 同一个定义被多个面板共用：只在第一个图层上监听样式变化来更新整图图例，避免重复刷新
                    if (legendSubscribed.Add(definition))
                        layer.RequestStyleUpdate += OnHarkerDataLayerStyleUpdated;

                    category.Children.Add(layer);
                }

                if (category.Children.Count == 0)
                    continue;

                panel.Node.Children.Add(category);
                panel.Layers.Add(category);
                panel.DataCategory = category;
            }
        }

        /// <summary>数据图层的颜色 / 形状 / 名称变化后，同步整图图例。</summary>
        private void OnHarkerDataLayerStyleUpdated(object? sender, EventArgs e)
        {
            if (!IsHarkerPlotMode || _harkerLayout == null || WpfPlot1 == null)
                return;

            // 嵌入面板的图例是画在第 1 个面板里的，需要整体重绘；其余位置只需重绘图例宿主
            if (_harkerLayout.Legend == HarkerLegendMode.Panel)
            {
                ScheduleLayerRefresh(true);
                return;
            }

            RenderHarkerLegendHost(GetHarkerLegendEntries());
            WpfPlot1.Refresh();
        }
        /// <summary>
        /// 视图复位（非保留视野的重绘）时：在模板默认范围的基础上，必要时向外扩展以容纳全部样点。
        /// 样点都在默认范围内时保持默认范围不变。
        /// </summary>
        private static void FitHarkerAxesToData(Plot plot, HarkerPanel panel)
        {
            if (panel.Xs.Length == 0)
                return;

            if (!TryGetFiniteRange(panel.Xs, out double dataMinX, out double dataMaxX) ||
                !TryGetFiniteRange(panel.Ys, out double dataMinY, out double dataMaxY))
                return;

            double padX = Math.Max((dataMaxX - dataMinX) * 0.05, 1e-9);
            double padY = Math.Max((dataMaxY - dataMinY) * 0.05, 1e-9);

            double left = dataMinX - padX, right = dataMaxX + padX;
            double bottom = dataMinY - padY, top = dataMaxY + padY;

            foreach (var axis in panel.Template.Info.Axes.OfType<CartesianAxisDefinition>())
            {
                if (axis.Type == "Bottom")
                {
                    if (!double.IsNaN(axis.Minimum)) left = Math.Min(left, axis.Minimum);
                    if (!double.IsNaN(axis.Maximum)) right = Math.Max(right, axis.Maximum);
                }
                else if (axis.Type == "Left")
                {
                    if (!double.IsNaN(axis.Minimum)) bottom = Math.Min(bottom, axis.Minimum);
                    if (!double.IsNaN(axis.Maximum)) top = Math.Max(top, axis.Maximum);
                }
            }

            plot.Axes.SetLimits(left, right, bottom, top);
        }

        private static bool TryGetFiniteRange(double[] values, out double min, out double max)
        {
            min = double.PositiveInfinity;
            max = double.NegativeInfinity;
            foreach (double v in values)
            {
                if (double.IsNaN(v) || double.IsInfinity(v))
                    continue;

                if (v < min) min = v;
                if (v > max) max = v;
            }

            return min <= max;
        }

        /// <summary>图形级标题：无边框宿主子图 + 居中的 Annotation，样式取自宿主模板的标题定义。</summary>
        private void RenderHarkerTitleHost()
        {
            if (_harkerTitleHost == null || CurrentTemplate?.Info?.Title == null)
                return;

            var definition = CurrentTemplate.Info.Title;
            SetLocalizedText(definition.Label, GetHarkerDiagramName());
            var plot = _harkerTitleHost;

            plot.Clear();
            plot.Layout.Frameless(true);
            plot.HideGrid();
            plot.HideLegend();

            var title = plot.Add.Annotation(definition.Label.Get(DiagramLanguage), Alignment.MiddleCenter);
            title.LabelFontName = definition.Family;
            title.LabelFontSize = (float)definition.Size;
            title.LabelBold = definition.IsBold;
            title.LabelItalic = definition.IsItalic;
            title.LabelFontColor = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(definition.Color));
            title.LabelBackgroundColor = Colors.Transparent;
            title.LabelBorderColor = Colors.Transparent;
            title.LabelBorderWidth = 0;
            title.LabelShadowColor = Colors.Transparent;

            _harkerTitleAnnotation = title;
            _harkerTitleColor = title.LabelFontColor;
        }

        /// <summary>整图图例（图下方 / 图上方 / 右侧 / 占用空格）：无边框宿主子图，标记放在视野外，只借用它的图例。</summary>
        private void RenderHarkerLegendHost(IReadOnlyList<HarkerLegendEntry> entries)
        {
            if (_harkerLegendHost == null || _harkerLayout == null)
                return;

            var plot = _harkerLegendHost;
            plot.Clear();
            plot.Layout.Frameless(true);
            plot.HideGrid();
            _harkerLegendMarkers.Clear();
            _harkerLegendOwner = null;

            var definition = CurrentTemplate?.Info?.Legend;
            if (entries.Count == 0 || definition is { IsVisible: false })
            {
                plot.HideLegend();
                return;
            }

            foreach (var entry in entries)
                AddHarkerLegendMarker(plot, 0, 0, entry, 9);

            plot.Axes.SetLimits(10, 11, 10, 11);

            bool horizontal = _harkerLayout.Legend is HarkerLegendMode.Above or HarkerLegendMode.Below;
            var legend = plot.ShowLegend(
                definition?.Alignment ?? Alignment.MiddleCenter,
                definition?.Orientation ?? (horizontal ? Orientation.Horizontal : Orientation.Vertical));
            legend.FontSize = 13;
            if (definition != null)
                ApplyLegendAppearance(legend, definition);
            _harkerLegendOwner = plot;
        }

        /// <summary>嵌入面板的图例：在目标面板里放视野外的标记，借用它的图例条目。</summary>
        private void AddHarkerEmbeddedLegend(Plot plot, IReadOnlyList<HarkerLegendEntry> entries)
        {
            var definition = CurrentTemplate?.Info?.Legend;
            if (entries.Count == 0 || definition is { IsVisible: false })
            {
                plot.HideLegend();
                return;
            }

            // 数据图层自带的图例替身也带着图例文本，不清掉的话嵌入图例的条目会重复一份
            foreach (var scatter in plot.GetPlottables().OfType<ScottPlot.Plottables.Scatter>())
                scatter.LegendText = string.Empty;

            foreach (var entry in entries)
                AddHarkerLegendMarker(plot, -1e9, -1e9, entry, 9);

            var legend = plot.ShowLegend(
                definition?.Alignment ?? Alignment.UpperRight,
                definition?.Orientation ?? Orientation.Vertical);
            legend.FontSize = 12;
            if (definition != null)
                ApplyLegendAppearance(legend, definition);
            _harkerLegendOwner = plot;
        }

        #region 选择遮罩（标题 / 图例 / 右上边框）

        // 原有的标题、图例、右/上边框遮罩逻辑都直接操作 WpfPlot1.Plot（即第 1 个面板），
        // 多联图的标题、图例在独立的宿主子图里，边框要对每个面板处理，所以这里提供对应的多面板版本。

        /// <summary>标题 / 图例定义被编辑后（属性面板）：只重绘标题和图例，并保持当前选中 / 遮罩状态。</summary>
        private void RefreshHarkerChrome()
        {
            if (WpfPlot1 == null || _harkerLayout == null)
                return;

            // 嵌入面板的图例画在第 1 个面板里，需要整体重绘
            if (_harkerLayout.Legend == HarkerLegendMode.Panel)
            {
                RefreshHarkerPlots(preserveAxisLimits: true);
                return;
            }

            // 网格定义被编辑：所有面板同步套用
            if (CurrentTemplate?.Info?.Grid is { } gridDefinition)
            {
                foreach (var panel in _harkerPanels)
                    ApplyGridDefinitionToPlot(panel.Plot, gridDefinition);
            }

            RenderHarkerTitleHost();
            RenderHarkerLegendHost(GetHarkerLegendEntries());
            ReapplySelectionVisualState();
            WpfPlot1.Refresh();
        }

        private void RestoreHarkerTitle()
        {
            if (_harkerTitleAnnotation != null)
                _harkerTitleAnnotation.LabelFontColor = _harkerTitleColor;
        }

        /// <summary>悬停预览：标题变红（与原有图解的悬浮样式一致）。</summary>
        private void HighlightHarkerTitle()
        {
            if (_harkerTitleAnnotation != null)
                _harkerTitleAnnotation.LabelFontColor = Colors.Red;
        }

        /// <summary>悬停预览：图例边框与文字变红。</summary>
        private void HighlightHarkerLegend()
        {
            if (_harkerLegendOwner == null)
                return;

            var legend = _harkerLegendOwner.Legend;
            legend.OutlineColor = Colors.Red;
            legend.FontColor = Colors.Red;
        }

        private void DimHarkerTitle()
        {
            if (_harkerTitleAnnotation != null)
                _harkerTitleAnnotation.LabelFontColor = _harkerTitleColor.WithAlpha(UnselectedMaskAlpha);
        }

        private void RestoreHarkerLegend()
        {
            foreach (var (marker, entry) in _harkerLegendMarkers)
                ApplyHarkerLegendMarkerStyle(marker, entry);

            if (_harkerLegendOwner != null)
            {
                var legend = _harkerLegendOwner.Legend;
                if (CurrentTemplate?.Info?.Legend is { } definition)
                {
                    ApplyLegendAppearance(legend, definition);
                }
                else
                {
                    legend.OutlineColor = Colors.Black;
                    legend.BackgroundColor = Colors.White;
                    legend.FontColor = null;
                    DisableLegendShadow(_harkerLegendOwner);
                }
            }
        }

        private void DimHarkerLegend()
        {
            foreach (var (marker, _) in _harkerLegendMarkers)
            {
                marker.MarkerStyle.FillColor = marker.MarkerStyle.FillColor.WithAlpha(UnselectedMaskAlpha);
                marker.MarkerStyle.LineColor = marker.MarkerStyle.LineColor.WithAlpha(UnselectedMaskAlpha);
                marker.MarkerStyle.OutlineColor = marker.MarkerStyle.OutlineColor.WithAlpha(UnselectedMaskAlpha);
            }

            if (_harkerLegendOwner != null)
            {
                DimLegendColors(_harkerLegendOwner.Legend);
            }
        }

        /// <summary>每个面板未图层化的右/上边框恢复默认。</summary>
        private void RestoreHarkerFrames()
        {
            foreach (var panel in _harkerPanels)
            {
                RestoreUnlayeredCartesianAxisChrome(panel.Plot.Axes.Right, "Right");
                RestoreUnlayeredCartesianAxisChrome(panel.Plot.Axes.Top, "Top");
            }
        }

        /// <summary>每个面板未图层化的右/上边框按当前选择状态施加遮罩。</summary>
        private void ApplyHarkerFrameMask(string? selectedAxisType)
        {
            foreach (var panel in _harkerPanels)
            {
                RestoreUnlayeredAxisToCorrectState(panel.Plot.Axes.Right, "Right", selectedAxisType);
                RestoreUnlayeredAxisToCorrectState(panel.Plot.Axes.Top, "Top", selectedAxisType);
            }
        }

        #endregion
        /// <summary>悬停联动用的高亮标记（默认隐藏）。样点数据由数据层写入 panel.Xs / panel.Ys。</summary>
        private static void AddHarkerHoverMarker(Plot plot, HarkerPanel panel)
        {
            var marker = plot.Add.Marker(0, 0, MarkerShape.OpenCircle, 14, Colors.Red);
            marker.LineWidth = 2;
            marker.IsVisible = false;
            panel.HoverMarker = marker;
        }

        private static string GetHarkerDiagramName() =>
            LanguageService.GetString("harker_diagram", "Harker Diagrams");

        private static void SetLocalizedText(LocalizedString? target, string text)
        {
            if (target == null)
                return;

            foreach (var key in target.Translations.Keys.ToList())
                target.Translations[key] = text;
        }

        #endregion

        #region 数据表

        /// <summary>按设置页配置准备 Harker 数据表：Category + Sample + X + Y（按绘图顺序），列头带单位。</summary>
        private void PrepareHarkerDataGrid()
        {
            if (_dataGrid == null)
                return;

            var settings = HarkerDiagramViewModel;
            var columns = new List<HarkerVariable> { settings.XVariable };
            columns.AddRange(settings.PlotOrderItems.Select(i => i.Variable));

            var sheet = _dataGrid.CurrentWorksheet;
            _rowStatus.Reset();
            SetDataColumnUnits(null);
            sheet.Reset();
            sheet.RowCount = WorksheetDefaultsHelper.GetDefaultRowCount(WorksheetDefaultsHelper.DiagramConfigKey);

            sheet.ColumnCount = columns.Count + DataGridLeadingColumnCount;
            ReoGridTableAppearance.StyleColumnHeaders(_dataGrid);

            sheet.ColumnHeaders[0].Text = CategoryColumnName;
            sheet.ColumnHeaders[1].Text = SampleColumnName;
            for (int i = 0; i < columns.Count; i++)
            {
                sheet.ColumnHeaders[i + DataGridLeadingColumnCount].Text = columns[i].Key;
            }

            sheet.SetColumnsWidth(0, 1, GetDataGridHeaderWidth(0, CategoryColumnName, 80));
            sheet.SetColumnsWidth(1, 1, GetDataGridHeaderWidth(1, SampleColumnName, 80));
            for (int i = 0; i < columns.Count; i++)
            {
                sheet.SetColumnsWidth(i + DataGridLeadingColumnCount, 1, 70);
            }

            SetDataColumnUnits(columns.Select(c => string.IsNullOrEmpty(c.Unit) ? null : c.Unit));

            RestoreWorksheetSelectionToSafeCell(sheet);
            FreezeDataGridFirstColumn(sheet);
            UpdateSelectedCellDisplayText(0, 0);
            RequestDataGridScrollReset();
        }

        /// <summary>
        /// 从数据表读取 Category / X / 各 Y 列，按 Category 分组着色，投到所有面板。
        /// 一行里 X 缺失则整行跳过；某个 Y 缺失则只在对应面板里缺这个点。
        /// </summary>
        private void PlotHarkerDataFromGrid()
        {
            if (_dataGrid == null || WpfPlot1 == null || _harkerLayout == null || _harkerPanels.Count == 0)
                return;

            StopDataLinkFlash();

            var worksheet = _dataGrid.CurrentWorksheet;

            var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int col = 0; col < worksheet.ColumnCount; col++)
            {
                string? header = worksheet.ColumnHeaders[col].Text;
                if (!string.IsNullOrWhiteSpace(header))
                    columnMap[header.Trim()] = col;
            }

            if (!columnMap.TryGetValue(_harkerLayout.XVariable.Key, out int xColumn))
            {
                MessageHelper.Warning(string.Format(
                    LanguageService.GetString("harker_missing_x_column", "数据表缺少 X 变量列：{0}"),
                    _harkerLayout.XVariable.Key));
                return;
            }

            int categoryColumn = columnMap.TryGetValue(CategoryColumnName, out int c) ? c : -1;
            int panelCount = _harkerPanels.Count;
            int[] yColumns = _harkerPanels
                .Select(p => columnMap.TryGetValue(p.Variable.Key, out int yc) ? yc : -1)
                .ToArray();

            var xs = new List<double>();
            var ys = Enumerable.Range(0, panelCount).Select(_ => new List<double>()).ToArray();
            var groupIndices = new List<int>();
            var groupLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var groups = new List<ScatterDefinition>();
            var sourceRows = new List<int>();
            var palette = new ScottPlot.Palettes.Category10();

            for (int row = 0; row <= worksheet.MaxContentRow; row++)
            {
                if (!InvariantNumberParser.TryParseCell(worksheet[row, xColumn], out double x))
                {
                    _rowStatus.MarkSkippedIfHasContent(row, DiagramRowSkipReason.NoValidData);
                    continue;
                }

                var rowYs = new double[panelCount];
                bool hasAnyY = false;
                for (int p = 0; p < panelCount; p++)
                {
                    rowYs[p] = double.NaN;
                    if (yColumns[p] >= 0 && InvariantNumberParser.TryParseCell(worksheet[row, yColumns[p]], out double y))
                    {
                        rowYs[p] = y;
                        hasAnyY = true;
                    }
                }

                if (!hasAnyY)
                {
                    _rowStatus.MarkSkippedIfHasContent(row, DiagramRowSkipReason.NoValidData);
                    continue;
                }

                string category = categoryColumn >= 0 ? (worksheet[row, categoryColumn]?.ToString() ?? string.Empty).Trim() : string.Empty;
                if (category.Length == 0)
                    category = "Sample";

                if (!groupLookup.TryGetValue(category, out int groupIndex))
                {
                    groupIndex = groups.Count;
                    groupLookup[category] = groupIndex;
                    string hex = palette.GetColor(groupIndex).ToHex();
                    groups.Add(new ScatterDefinition
                    {
                        Name = category,
                        Color = hex,
                        StrokeColor = hex,
                        MarkerShape = PlotMarkerShape.FilledCircle,
                        Size = 8
                    });
                }

                xs.Add(x);
                for (int p = 0; p < panelCount; p++)
                    ys[p].Add(rowYs[p]);
                groupIndices.Add(groupIndex);
                sourceRows.Add(row);
                _rowStatus.MarkPlotted(row);
            }

            if (xs.Count == 0)
            {
                MessageHelper.Warning(LanguageService.Instance["no_valid_data_found"] ?? "No valid data found in the table.");
                return;
            }

            _harkerData = new HarkerDataSet
            {
                Xs = xs.ToArray(),
                Ys = ys.Select(list => list.ToArray()).ToArray(),
                GroupIndex = groupIndices.ToArray(),
                SourceRows = sourceRows.ToArray(),
                Groups = groups
            };

            foreach (var panel in _harkerPanels)
            {
                panel.Xs = _harkerData.Xs;
                panel.Ys = _harkerData.Ys[panel.Index];
            }

            // 每个 Category 在每个面板里是一个散点图层（出现在图层列表里，可选中、改样式）
            RebuildHarkerDataLayers();

            // 重新投图时视野适应新数据
            RefreshHarkerPlots(preserveAxisLimits: false);
        }

        #endregion

        #region 导出 / 复制

        private byte[] GetHarkerImageBytes(ImageFormat format = ImageFormat.Png)
        {
            // 导出/复制时不要带上悬停标记
            ApplyHarkerSampleHover(-1, -1, force: true, refresh: false);

            int width = Math.Max(800, (int)(WpfPlot1.ActualWidth * 2));
            int height = Math.Max(600, (int)(WpfPlot1.ActualHeight * 2));

            using var image = WpfPlot1.Multiplot.Render(width, height);
            return image.GetImageBytes(format, 100);
        }

        private void ExportHarkerImage(string path)
        {
            try
            {
                ImageFormat? format = Path.GetExtension(path).ToLowerInvariant() switch
                {
                    ".png" => ImageFormat.Png,
                    ".jpg" or ".jpeg" => ImageFormat.Jpeg,
                    ".bmp" => ImageFormat.Bmp,
                    ".webp" => ImageFormat.Webp,
                    _ => null
                };

                if (format == null)
                {
                    MessageHelper.Warning(LanguageService.GetString(
                        "harker_export_format",
                        "Harker 多联图暂不支持该导出格式（目前支持 png/jpg/bmp/webp）。"));
                    return;
                }

                File.WriteAllBytes(path, GetHarkerImageBytes(format.Value));
                MessageHelper.ExportSuccess(LanguageService.Instance["export_successful"], path);
            }
            catch (Exception ex)
            {
                MessageHelper.Error(LanguageService.Instance["export_failed"] + ex.Message);
            }
        }

        #endregion

        #region 鼠标事件切换

        // 注意：WpfPlot1_MouseDown 只处理中键切换模式，与 Plot 无关，始终保留

        private void DetachLegacyMouseHandlers()
        {
            WpfPlot1.MouseEnter -= WpfPlot1_MouseEnter;
            WpfPlot1.MouseLeave -= WpfPlot1_MouseLeave;
            WpfPlot1.MouseMove -= WpfPlot1_MouseMove;
            WpfPlot1.MouseUp -= WpfPlot1_MouseUp;
            WpfPlot1.MouseRightButtonUp -= WpfPlot1_MouseRightButtonUp;
            WpfPlot1.MouseDoubleClick -= WpfPlot1_MouseDoubleClick;
        }

        private void AttachLegacyMouseHandlers()
        {
            // 先退订再订阅，保证不会重复订阅
            DetachLegacyMouseHandlers();
            WpfPlot1.MouseEnter += WpfPlot1_MouseEnter;
            WpfPlot1.MouseLeave += WpfPlot1_MouseLeave;
            WpfPlot1.MouseMove += WpfPlot1_MouseMove;
            WpfPlot1.MouseUp += WpfPlot1_MouseUp;
            WpfPlot1.MouseRightButtonUp += WpfPlot1_MouseRightButtonUp;
            WpfPlot1.MouseDoubleClick += WpfPlot1_MouseDoubleClick;
        }

        private void AttachHarkerMouseHandlers()
        {
            DetachHarkerMouseHandlers();
            WpfPlot1.MouseMove += Harker_MouseMove;
            WpfPlot1.MouseLeave += Harker_MouseLeave;
            WpfPlot1.MouseDown += Harker_MouseDown;
            WpfPlot1.MouseUp += Harker_MouseUp;
            WpfPlot1.MouseRightButtonUp += Harker_MouseRightButtonUp;
        }

        private void DetachHarkerMouseHandlers()
        {
            WpfPlot1.MouseMove -= Harker_MouseMove;
            WpfPlot1.MouseLeave -= Harker_MouseLeave;
            WpfPlot1.MouseDown -= Harker_MouseDown;
            WpfPlot1.MouseUp -= Harker_MouseUp;
            WpfPlot1.MouseRightButtonUp -= Harker_MouseRightButtonUp;
        }

        #endregion

        #region 多面板鼠标交互

        private Pixel GetHarkerPixel(MouseEventArgs e)
        {
            Point position = e.GetPosition(WpfPlot1);
            var source = PresentationSource.FromVisual(WpfPlot1);
            double dpiScale = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            return new Pixel(position.X * dpiScale, position.Y * dpiScale);
        }

        private HarkerPanel? GetHarkerPanelAtPixel(Pixel pixel)
        {
            Plot? plot = WpfPlot1.Multiplot.GetPlotAtPixel(pixel);
            return plot == null ? null : _harkerPanels.FirstOrDefault(p => ReferenceEquals(p.Plot, plot));
        }

        /// <summary>鼠标是否落在某面板的 X/Y 轴区域内，是则返回该面板对应的坐标轴图层。</summary>
        private AxisLayerItemViewModel? HitTestHarkerAxis(HarkerPanel panel, Pixel pixel)
        {
            var rect = panel.Plot.RenderManager.LastRender.DataRect;
            if (rect.Width <= 0 || rect.Height <= 0)
                return null;

            string? axisType = null;
            if (pixel.X < rect.Left && pixel.Y >= rect.Top && pixel.Y <= rect.Bottom)
                axisType = "Left";
            else if (panel.ShowXAxis && pixel.Y > rect.Bottom && pixel.X >= rect.Left && pixel.X <= rect.Right)
                axisType = "Bottom";

            if (axisType == null)
                return null;

            return FlattenTree(panel.Layers)
                .OfType<AxisLayerItemViewModel>()
                .FirstOrDefault(l => l.AxisDefinition != null && l.AxisDefinition.Type == axisType);
        }

        private void Harker_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!IsHarkerPlotMode)
                return;

            Pixel pixel = GetHarkerPixel(e);
            var panel = GetHarkerPanelAtPixel(pixel);

            bool needRefresh = false;

            // 1. 坐标轴悬停高亮（只作用于鼠标所在面板的坐标轴）
            var hoveredAxis = panel == null ? null : HitTestHarkerAxis(panel, pixel);
            if (!ReferenceEquals(hoveredAxis, _harkerHoveredAxis))
            {
                if (_harkerHoveredAxis != null)
                    RestoreLayerToCorrectState(_harkerHoveredAxis);

                _harkerHoveredAxis = hoveredAxis;

                // 已选中的对象不再叠加悬停高亮（与原有行为一致）
                if (hoveredAxis != null && !ReferenceEquals(hoveredAxis, _selectedLayer))
                    HighlightLayer(hoveredAxis);

                needRefresh = true;
            }

            // 2. 样点悬停（可联动其余面板）
            int sampleIndex = panel == null || hoveredAxis != null
                ? -1
                : FindHarkerNearestSample(panel, pixel, HarkerSampleHoverRadius);
            int panelIndex = sampleIndex < 0 ? -1 : panel!.Index;
            if (ApplyHarkerSampleHover(panelIndex, sampleIndex, force: false, refresh: false))
                needRefresh = true;

            // 3. 状态栏坐标
            if (panel != null && _harkerLayout != null)
            {
                Coordinates c = panel.Plot.GetCoordinates(pixel);
                CoordinateStatus = $"{panel.Variable.Key}  {_harkerLayout.XVariable.Key}={c.X:F2}, {panel.Variable.Key}={c.Y:F2}";
            }
            else
            {
                CoordinateStatus = string.Empty;
            }

            // 标题行、图例区域也是可点击选中的对象
            Plot? hostPlot = panel == null ? WpfPlot1.Multiplot.GetPlotAtPixel(pixel) : null;
            var chromeKind = PlotChromeHoverKind.None;
            if (hostPlot != null)
            {
                if (ReferenceEquals(hostPlot, _harkerTitleHost))
                    chromeKind = PlotChromeHoverKind.Title;
                else if (ReferenceEquals(hostPlot, _harkerLegendHost) && _harkerLegendMarkers.Count > 0)
                    chromeKind = PlotChromeHoverKind.Legend;
            }
            bool overChrome = chromeKind != PlotChromeHoverKind.None;

            // 标题 / 图例悬浮高亮（复用原有图解的 Chrome 悬浮逻辑）
            if (chromeKind != _lastHoveredChrome)
            {
                ClearChromeHoverVisual();
                if (chromeKind != PlotChromeHoverKind.None)
                    ApplyChromeHoverVisual(chromeKind);
                needRefresh = true;
            }

            WpfPlot1.Cursor = (hoveredAxis != null || sampleIndex >= 0 || overChrome) ? Cursors.Hand : Cursors.Arrow;

            if (needRefresh)
                WpfPlot1.Refresh();
        }

        private void Harker_MouseLeave(object? sender, MouseEventArgs e)
        {
            if (!IsHarkerPlotMode)
                return;

            CoordinateStatus = string.Empty;

            bool needRefresh = false;
            if (_harkerHoveredAxis != null)
            {
                RestoreLayerToCorrectState(_harkerHoveredAxis);
                _harkerHoveredAxis = null;
                needRefresh = true;
            }

            if (_lastHoveredChrome != PlotChromeHoverKind.None)
            {
                ClearChromeHoverVisual();
                needRefresh = true;
            }

            if (ApplyHarkerSampleHover(-1, -1, force: false, refresh: false))
                needRefresh = true;

            WpfPlot1.Cursor = Cursors.Arrow;

            if (needRefresh)
                WpfPlot1.Refresh();
        }

        private void Harker_MouseDown(object? sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            _harkerMouseDownPoint = e.GetPosition(WpfPlot1);
            _harkerMouseDownValid = true;
        }

        private void Harker_MouseUp(object? sender, MouseButtonEventArgs e)
        {
            if (!IsHarkerPlotMode || e.ChangedButton != MouseButton.Left)
                return;

            // 左键拖动是平移，不当作点击
            Point up = e.GetPosition(WpfPlot1);
            bool isClick = _harkerMouseDownValid
                && Math.Abs(up.X - _harkerMouseDownPoint.X) <= HarkerClickTolerance
                && Math.Abs(up.Y - _harkerMouseDownPoint.Y) <= HarkerClickTolerance;
            _harkerMouseDownValid = false;
            if (!isClick)
                return;

            Pixel pixel = GetHarkerPixel(e);

            // 点击标题行：选中图形标题（属性面板里编辑文字 / 字体 / 颜色）
            if (ReferenceEquals(WpfPlot1.Multiplot.GetPlotAtPixel(pixel), _harkerTitleHost)
                && CurrentTemplate?.Info?.Title != null)
            {
                SelectChrome(CurrentTemplate.Info.Title);
                return;
            }

            // 点击整图图例所在区域：选中图例（属性面板里编辑对齐、排列、字体、是否显示）
            if (_harkerLegendHost != null
                && ReferenceEquals(WpfPlot1.Multiplot.GetPlotAtPixel(pixel), _harkerLegendHost)
                && CurrentTemplate?.Info?.Legend != null)
            {
                SelectChrome(CurrentTemplate.Info.Legend);
                return;
            }

            var panel = GetHarkerPanelAtPixel(pixel);
            if (panel == null)
                return;

            var axisLayer = HitTestHarkerAxis(panel, pixel);
            if (axisLayer != null && SelectLayerCommand.CanExecute(axisLayer))
            {
                SelectLayerCommand.Execute(axisLayer);
                return;
            }

            // 点击样点：同步选中数据表对应行；图解模式选中图层，数据模式让该样点在所有面板里闪烁
            var data = _harkerData;
            int sample = data == null ? -1 : FindHarkerNearestSample(panel, pixel, HarkerSampleHoverRadius);
            if (data == null || sample < 0 || sample >= data.GroupIndex.Length)
                return;

            var scatterLayer = FindHarkerScatterLayer(panel, sample);

            try
            {
                _isSyncingSelection = true;
                SelectReoGridRow(data.SourceRows[sample]);
            }
            finally
            {
                _isSyncingSelection = false;
            }

            if (RibbonTabIndex == 1)
            {
                // 与原有数据模式一致：开启“选择联动”时才同时选中图层；先选图层再闪烁（选中会停止旧的闪烁）
                if (IsDataSelectionLinkEnabled && scatterLayer != null && SelectLayerCommand.CanExecute(scatterLayer))
                    SelectLayerCommand.Execute(scatterLayer);

                StartHarkerFlash(sample);
            }
            else
            {
                StopDataLinkFlash();
                if (scatterLayer != null && SelectLayerCommand.CanExecute(scatterLayer))
                    SelectLayerCommand.Execute(scatterLayer);
            }
        }

        /// <summary>某个样点在指定面板里所属 Category 的散点图层。</summary>
        private ScatterLayerItemViewModel? FindHarkerScatterLayer(HarkerPanel panel, int sample)
        {
            var data = _harkerData;
            if (data == null || panel.DataCategory == null || sample < 0 || sample >= data.GroupIndex.Length)
                return null;

            var definition = data.Groups[data.GroupIndex[sample]];
            return panel.DataCategory.Children
                .OfType<ScatterLayerItemViewModel>()
                .FirstOrDefault(l => ReferenceEquals(l.ScatterDefinition, definition) && l.IsVisible);
        }

        #region 数据模式联动

        private static void AddHarkerFlashMarker(Plot plot, HarkerPanel panel)
        {
            var marker = plot.Add.Marker(0, 0, MarkerShape.OpenCircle, 12, Colors.Red);
            marker.IsVisible = false;
            panel.FlashMarker = marker;
        }

        /// <summary>数据表选中某行：在所有面板里闪烁对应样点，并选中它所属的数据图层。</summary>
        private void HighlightHarkerSampleByRow(int row)
        {
            var data = _harkerData;
            int sample = data == null ? -1 : Array.IndexOf(data.SourceRows, row);
            if (data == null || sample < 0)
            {
                StopDataLinkFlash();
                return;
            }

            var panel = _harkerPanels.FirstOrDefault(p => !double.IsNaN(p.Ys[sample]));
            var layer = panel == null ? null : FindHarkerScatterLayer(panel, sample);
            if (layer != null && SelectLayerCommand.CanExecute(layer))
                SelectLayerCommand.Execute(layer);

            StartHarkerFlash(sample);
        }

        /// <summary>进入数据模式时，按数据表当前选中行（或选中的数据图层）恢复闪烁。</summary>
        private void ApplyHarkerDataModeSelection()
        {
            var worksheet = _dataGrid?.CurrentWorksheet
                ?? (_dataGrid != null && _dataGrid.Worksheets.Count > 0 ? _dataGrid.Worksheets[0] : null);

            if (worksheet != null)
            {
                var range = worksheet.SelectionRange;
                if (!range.IsEmpty
                    && range.Row >= 0
                    && range.Row < worksheet.RowCount
                    && worksheet.SelectionStyle != unvell.ReoGrid.WorksheetSelectionStyle.None
                    && _harkerData != null
                    && Array.IndexOf(_harkerData.SourceRows, range.Row) >= 0)
                {
                    HighlightHarkerSampleByRow(range.Row);
                    return;
                }
            }

            StopDataLinkFlash();
        }

        private void StartHarkerFlash(int sample)
        {
            if (WpfPlot1 == null)
                return;

            _harkerFlashSample = sample;
            _harkerFlashIsRed = true;
            ApplyHarkerFlashMarkers();
            WpfPlot1.Refresh();

            if (!_dataLinkFlashTimer.IsEnabled)
                _dataLinkFlashTimer.Start();
        }

        private void TickHarkerFlash()
        {
            _harkerFlashIsRed = !_harkerFlashIsRed;
            ApplyHarkerFlashMarkers();
            WpfPlot1?.Refresh();
        }

        /// <summary>清掉闪烁状态（由 StopDataLinkFlash 调用；调用方负责刷新和停止计时器）。</summary>
        private void ClearHarkerFlashState()
        {
            if (_harkerFlashSample < 0)
                return;

            _harkerFlashSample = -1;
            ApplyHarkerFlashMarkers();
        }

        /// <summary>按当前闪烁样点和红/蓝颜色，更新每个面板的闪烁标记（该面板里这个样点缺失则隐藏）。</summary>
        private void ApplyHarkerFlashMarkers()
        {
            var data = _harkerData;
            var color = _harkerFlashIsRed ? DataLinkFlashRed : DataLinkFlashBlue;

            foreach (var panel in _harkerPanels)
            {
                var marker = panel.FlashMarker;
                if (marker == null)
                    continue;

                int i = _harkerFlashSample;
                if (data == null || i < 0 || i >= panel.Xs.Length || double.IsNaN(panel.Xs[i]) || double.IsNaN(panel.Ys[i]))
                {
                    marker.IsVisible = false;
                    continue;
                }

                var definition = data.Groups[data.GroupIndex[i]];
                // 与数据点相同的图案，只切换闪烁颜色
                PlotMarkerStyleApplier.Apply(marker.MarkerStyle, definition.MarkerShape, color, definition.StrokeWidth, color);
                marker.Size = definition.Size > 0 ? definition.Size : 10;
                marker.Location = PlotTransformHelper.ToRenderCoordinates(panel.Plot, new Coordinates(panel.Xs[i], panel.Ys[i]));
                marker.IsVisible = true;
            }
        }

        #endregion

        private void Harker_MouseRightButtonUp(object? sender, MouseButtonEventArgs e)
        {
            WpfPlot1.Focus();

            // 右键：取消选中
            CancelSelected();
        }

        /// <summary>在像素空间里找离鼠标最近且在半径内的样点，避免不同面板量纲差异。</summary>
        private static int FindHarkerNearestSample(HarkerPanel panel, Pixel mouse, double radius)
        {
            int best = -1;
            double bestDistance = radius * radius;

            for (int i = 0; i < panel.Xs.Length; i++)
            {
                Pixel p = panel.Plot.GetPixel(new Coordinates(panel.Xs[i], panel.Ys[i]));
                double dx = p.X - mouse.X;
                double dy = p.Y - mouse.Y;
                double distance = dx * dx + dy * dy;

                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }

        /// <summary>设置样点悬停状态。返回状态是否发生变化。</summary>
        private bool ApplyHarkerSampleHover(int panelIndex, int sampleIndex, bool force, bool refresh = true)
        {
            if (!force && sampleIndex == _harkerHoverSampleIndex && panelIndex == _harkerHoverPanelIndex)
                return false;

            _harkerHoverSampleIndex = sampleIndex;
            _harkerHoverPanelIndex = panelIndex;

            foreach (var panel in _harkerPanels)
            {
                var marker = panel.HoverMarker;
                if (marker == null)
                    continue;

                if (sampleIndex < 0 || sampleIndex >= panel.Xs.Length || (!HarkerLinkHover && panel.Index != panelIndex)
                    || double.IsNaN(panel.Xs[sampleIndex]) || double.IsNaN(panel.Ys[sampleIndex]))
                {
                    marker.IsVisible = false;
                }
                else
                {
                    marker.Location = new Coordinates(panel.Xs[sampleIndex], panel.Ys[sampleIndex]);
                    marker.IsVisible = true;
                }
            }

            if (refresh)
                WpfPlot1?.Refresh();

            return true;
        }

        #endregion
    }
}
