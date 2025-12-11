using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Controls;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Interfaces;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Models;
using HandyControl.Controls;
using Jint;
using Ookii.Dialogs.Wpf;
using ScottPlot;
using ScottPlot.Colormaps;
using ScottPlot.Interactivity.UserActionResponses;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using unvell.ReoGrid;

namespace GeoChemistryNexus.ViewModels
{
    public partial class MainPlotViewModel : ObservableObject, IRecipient<PickPointRequestMessage>
    {
        // 拾取点模式
        [ObservableProperty]
        private bool _isPickingPointMode = false;

        // 待更新的 PointDefinition 对象
        private PointDefinition? _targetPointDefinition;

        // 用于鼠标命中测试的节流控制
        private long _lastHitTestTimeTicks = 0;
        // 定义检测间隔，40ms 大概是 25FPS
        private const long HitTestIntervalTicks = 40 * 10000;

        // 属性编辑器属性对象
        [ObservableProperty]
        private object _propertyGridModel;

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
        private bool _isShowTemplateInfo = false;   // 显示绘图模板的说明帮助

        [ObservableProperty]
        private bool _isNewTemplateMode = false; // 新建绘图遮罩

        // 卡片展示用的模板集合
        [ObservableProperty]
        private ObservableCollection<TemplateCardViewModel> _templateCards = new();

        [ObservableProperty]
        private string _templateSearchText = string.Empty;

        public ICollectionView TemplateCardsView { get; private set; }

        [ObservableProperty]
        private bool _isTemplateSearchVisible = false;

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

        // 十字轴显示
        [ObservableProperty]
        private bool _isCrosshairVisible = true;

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
                    // 如果开启，且鼠标在控件内（通过 IsMouseOver 判断），则显示
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
        private object nullObject = new object();

        // 当前点击的模板列表节点
        private GraphMapTemplateNode currentgraphMapTemplateNode;

        // 标记是否已经确认过进入编辑模式
        private bool _hasConfirmedEditMode = false;

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
                    var result = HandyControl.Controls.MessageBox.Show("确定要进入编辑模式吗？这将允许您修改底图。", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes)
                    {
                        // 强制通知 UI 属性值未改变，以恢复 RadioButton 的选中状态
                        OnPropertyChanged(nameof(RibbonTabIndex));
                        return;
                    }
                    _hasConfirmedEditMode = true;
                }

                SetProperty(ref _ribbonTabIndex, value);
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

        // 测试
        [ObservableProperty]
        private bool _isAddingArrow = false; // 标记是否处于添加箭头模式
        private Coordinates? _arrowStartPoint = null; // 存储箭头的起点
        private ScottPlot.Plottables.Arrow? _tempArrowPlot; // 用于实时预览箭头

        // 吸附标记
        private ScottPlot.Plottables.Marker? _snapMarker;

        // 存储所有潜在吸附点的标记（提示用）
        private List<ScottPlot.Plottables.Marker> _potentialSnapMarkers = new();

        // 初始化
        public MainPlotViewModel(WpfPlot wpfPlot, System.Windows.Controls.RichTextBox richTextBox, unvell.ReoGrid.ReoGridControl dataGrid)
        {
            // 获取模板列表
            InitTemplate();

            WpfPlot1 = wpfPlot;      // 获取绘图控件
            _richTextBox = richTextBox;      // 富文本框
            _dataGrid = dataGrid;        // 获取数据表格控件
            IsSnapSelectionEnabled = true;  // 吸附选择开启
            IsShowTemplateInfo = false;

            TemplateCardsView = CollectionViewSource.GetDefaultView(TemplateCards);
            TemplateCardsView.Filter = FilterTemplateCards;

            InitializeBreadcrumbs();
            LoadAllTemplateCards();
            TemplateCardsView.Refresh();

            // 初始化十字轴并设置样式
            CrosshairPlot = WpfPlot1.Plot.Add.Crosshair(0, 0);
            CrosshairPlot.IsVisible = true;
            CrosshairPlot.TextColor = ScottPlot.Colors.White;
            CrosshairPlot.TextBackgroundColor = CrosshairPlot.HorizontalLine.Color;

            // 初始化吸附标记
            _snapMarker = WpfPlot1.Plot.Add.Marker(0, 0);
            _snapMarker.IsVisible = false;
            _snapMarker.Color = Colors.Orange; // 使用醒目的橙色
            _snapMarker.Size = 15; // 稍微大一点
            _snapMarker.Shape = MarkerShape.OpenCircle;
            _snapMarker.LineWidth = 3; // 加粗

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
            }

            WpfPlot1.Menu.Clear();      // 禁用原生右键菜单

            // 禁用双击帧率显示
            WpfPlot1.UserInputProcessor.DoubleLeftClickBenchmark(false);

            // 注册消息接收
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        [RelayCommand]
        private void ShowTemplateSearch()
        {
            IsTemplateSearchVisible = true;
        }

        [RelayCommand]
        private void HideTemplateSearch()
        {
            IsTemplateSearchVisible = false;
        }

        partial void OnTemplateSearchTextChanged(string value)
        {
            TemplateCardsView?.Refresh();
        }

        private bool FilterTemplateCards(object obj)
        {
            if (obj is not TemplateCardViewModel card) return false;
            if (string.IsNullOrWhiteSpace(TemplateSearchText)) return true;

            var query = TemplateSearchText.Trim();
            return (card.Name?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                   || (card.Category?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                   || (card.TemplatePath?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
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

            // 提示用户
            MessageHelper.Info("请在绘图区域点击以拾取坐标");

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

            worksheet.ColumnCount = requiredColumns.Count;

            // 将参数名设置为表格的表头
            for (int i = 0; i < requiredColumns.Count; i++)
            {
                worksheet.ColumnHeaders[i].Text = requiredColumns[i];
                // 自适应列宽，包含表头
                worksheet.AutoFitColumnWidth(i, true);

                // 稍微增加一点内边距，避免太紧凑
                var currentWidth = worksheet.GetColumnWidth(i);
                worksheet.SetColumnsWidth(i, 1, (ushort)(currentWidth + 10));
            }
        }

        /// <summary>
        /// 鼠标左键抬起事件，用于确定绘图对象的起点和终点
        /// </summary>
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
                    MessageHelper.Success("坐标拾取成功！");

                    // 刷新绘图
                    RefreshPlotFromLayers(true);
                    ReapplySelectionVisualState();
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
            if (!isDrawingOrAdding && IsSnapSelectionEnabled && _lastHoveredLayer != null)
            {
                if (SelectLayerCommand.CanExecute(_lastHoveredLayer))
                {
                    SelectLayerCommand.Execute(_lastHoveredLayer);
                }
                return;
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
                    var linesCategory = LayerTree.FirstOrDefault(c => c is CategoryLayerItemViewModel vm && vm.Name == LanguageService.Instance["line"]) as CategoryLayerItemViewModel;
                    if (linesCategory == null)
                    {
                        // 如果 "线" 分类不存在，则创建一个
                        linesCategory = new CategoryLayerItemViewModel(LanguageService.Instance["line"]);
                        LayerTree.Add(linesCategory);
                    }

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
                    // 订阅可见性变化事件，以便在图层列表中勾选/取消勾选时刷新视图
                    lineLayer.PropertyChanged += (s, ev) => { if (ev.PropertyName == nameof(LineLayerItemViewModel.IsVisible)) RefreshPlotFromLayers(true); };

                    // 将新图层添加到图层树
                    linesCategory.Children.Add(lineLayer);

                    // 刷新整个绘图（这将根据图层树重新绘制所有内容，包括新添加的线条）
                    RefreshPlotFromLayers(true);

                    // 选中新添加的图层
                    SelectLayer(lineLayer);

                    // 记录 Undo/Redo
                    Action undoLine = () => {
                        if (linesCategory.Children.Contains(lineLayer)) linesCategory.Children.Remove(lineLayer);
                        if (linesCategory.Children.Count == 0 && LayerTree.Contains(linesCategory)) LayerTree.Remove(linesCategory);
                        RefreshPlotFromLayers(true);
                    };
                    Action redoLine = () => {
                        if (!LayerTree.Contains(linesCategory)) LayerTree.Add(linesCategory);
                        if (!linesCategory.Children.Contains(lineLayer)) linesCategory.Children.Add(lineLayer);
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
                var textCategory = LayerTree.FirstOrDefault(c => c is CategoryLayerItemViewModel vm && vm.Name == LanguageService.Instance["text"]) as CategoryLayerItemViewModel;
                if (textCategory == null)
                {
                    textCategory = new CategoryLayerItemViewModel(LanguageService.Instance["text"]);
                    LayerTree.Add(textCategory);
                }

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

                // 根据默认语言选择合适的占位符
                string placeholder = defaultLang == "zh-CN" ? "请输入文本" : "Enter Text";

                // 动态创建 LocalizedString 对象
                var contentString = new LocalizedString
                {
                    Default = defaultLang,
                    Translations = allLangs.ToDictionary(lang => lang, lang => placeholder)
                };


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
                textLayer.PropertyChanged += (s, ev) => { if (ev.PropertyName == nameof(TextLayerItemViewModel.IsVisible)) RefreshPlotFromLayers(true); };
                textCategory.Children.Add(textLayer);

                RefreshPlotFromLayers(true);

                // 选中新添加的图层
                SelectLayer(textLayer);

                // 记录 Undo/Redo
                Action undoText = () => {
                    if (textCategory.Children.Contains(textLayer)) textCategory.Children.Remove(textLayer);
                    if (textCategory.Children.Count == 0 && LayerTree.Contains(textCategory)) LayerTree.Remove(textCategory);
                    RefreshPlotFromLayers(true);
                };
                Action redoText = () => {
                    if (!LayerTree.Contains(textCategory)) LayerTree.Add(textCategory);
                    if (!textCategory.Children.Contains(textLayer)) textCategory.Children.Add(textLayer);
                    RefreshPlotFromLayers(true);
                };
                AddUndoState(undoText, redoText);

                // IsAddingText = false; // 保持添加模式

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

                    var arrowsCategory = LayerTree.FirstOrDefault(c => c is CategoryLayerItemViewModel vm && vm.Name == LanguageService.Instance["arrow"]) as CategoryLayerItemViewModel;
                    if (arrowsCategory == null)
                    {
                        arrowsCategory = new CategoryLayerItemViewModel(LanguageService.Instance["arrow"]);
                        LayerTree.Add(arrowsCategory);
                    }

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
                    arrowLayer.PropertyChanged += (s, ev) => { if (ev.PropertyName == nameof(ArrowLayerItemViewModel.IsVisible)) RefreshPlotFromLayers(true); };
                    arrowsCategory.Children.Add(arrowLayer);

                    RefreshPlotFromLayers(true);

                    // 选中新添加的图层
                    SelectLayer(arrowLayer);

                    // 记录 Undo/Redo
                    Action undoArrow = () => {
                        if (arrowsCategory.Children.Contains(arrowLayer)) arrowsCategory.Children.Remove(arrowLayer);
                        if (arrowsCategory.Children.Count == 0 && LayerTree.Contains(arrowsCategory)) LayerTree.Remove(arrowsCategory);
                        RefreshPlotFromLayers(true);
                    };
                    Action redoArrow = () => {
                        if (!LayerTree.Contains(arrowsCategory)) LayerTree.Add(arrowsCategory);
                        if (!arrowsCategory.Children.Contains(arrowLayer)) arrowsCategory.Children.Add(arrowLayer);
                        RefreshPlotFromLayers(true);
                    };
                    AddUndoState(undoArrow, redoArrow);

                    // Reset arrow drawing state
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
                IsPickingPointMode = false;
                _targetPointDefinition = null;
                WpfPlot1.Cursor = Cursors.Arrow;
                MessageHelper.Info("拾取操作已取消");
                return;
            }

            // 如果当前是高亮状态，鼠标右键单击就是取消选择
            if (_selectedLayer != null)
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
                var polygonsCategory = LayerTree.FirstOrDefault(c => c is CategoryLayerItemViewModel vm && vm.Name == LanguageService.Instance["polygon"]) as CategoryLayerItemViewModel;
                if (polygonsCategory == null)
                {
                    polygonsCategory = new CategoryLayerItemViewModel(LanguageService.Instance["polygon"]);
                    LayerTree.Add(polygonsCategory);
                }

                // 使用 PlotTransformHelper 将临时绘图坐标转换为真实数据坐标
                var realVertices = _polygonVertices.Select(c => PlotTransformHelper.ToRealDataCoordinates(WpfPlot1.Plot, c));

                // 创建 PolygonDefinition 用于存储属性
                var newPolygonDef = new PolygonDefinition
                {
                    // 现在存入的是真实数据（比如 0.001, 10000 等）
                    Vertices = new ObservableCollection<PointDefinition>(realVertices.Select(c => new PointDefinition { X = c.X, Y = c.Y })),
                    // 使用默认样式
                };

                // 创建 PolygonLayerItemViewModel
                var polygonLayer = new PolygonLayerItemViewModel(newPolygonDef, polygonsCategory.Children.Count);
                polygonLayer.PropertyChanged += (s, ev) => { if (ev.PropertyName == nameof(PolygonLayerItemViewModel.IsVisible)) RefreshPlotFromLayers(true); };
                polygonsCategory.Children.Add(polygonLayer);

                // 刷新整个绘图
                RefreshPlotFromLayers(true);

                // 选中新添加的图层
                SelectLayer(polygonLayer);

                // 记录 Undo/Redo
                Action undoPolygon = () => {
                    if (polygonsCategory.Children.Contains(polygonLayer)) polygonsCategory.Children.Remove(polygonLayer);
                    if (polygonsCategory.Children.Count == 0 && LayerTree.Contains(polygonsCategory)) LayerTree.Remove(polygonsCategory);
                    RefreshPlotFromLayers(true);
                };
                Action redoPolygon = () => {
                    if (!LayerTree.Contains(polygonsCategory)) LayerTree.Add(polygonsCategory);
                    if (!polygonsCategory.Children.Contains(polygonLayer)) polygonsCategory.Children.Add(polygonLayer);
                    RefreshPlotFromLayers(true);
                };
                AddUndoState(undoPolygon, redoPolygon);

                // 重置状态
                // IsAddingPolygon = false; // 保持添加模式
                _polygonVertices.Clear();
                WpfPlot1.Refresh();
                //MessageHelper.Success("多边形添加成功！");
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
                    // MessageHelper.Info("本次线条添加已取消");
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
        /// “添加多边形”按钮的命令
        /// </summary>
        [RelayCommand]
        private void AddPolygon()
        {
            // 取消高亮选择
            CancelSelected();

            // 进入添加多边形模式
            IsAddingPolygon = true;
            _polygonVertices.Clear(); // 清空旧的顶点

            // 关闭其他可能开启的模式
            IsAddingLine = false;
            IsAddingText = false;
        }

        /// <summary>
        /// “添加线条”按钮的命令
        /// </summary>
        [RelayCommand]
        private void AddLine()
        {
            // 取消高亮选择
            CancelSelected();

            // 进入添加线条模式
            IsAddingLine = true;
            _lineStartPoint = null; // 重置起点
        }

        /// <summary>
        /// “添加文本”按钮的命令
        /// </summary>
        [RelayCommand]
        private void AddText()
        {
            // 取消高亮选择
            CancelSelected();

            // 进入添加文本模式
            IsAddingText = true;
            // 关闭其他可能开启的模式，以避免冲突
            IsAddingLine = false;
            _lineStartPoint = null;
        }

        // 切换语言——刷新
        public void InitTemplate()
        {
            // 刷新模板列表
            // 读取默认模板列表
            string defaultListPath = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "GraphMapList.json");
            string defaultJson = JsonHelper.ReadJsonFile(defaultListPath);

            // 读取自定义模板列表
            string customListPath = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "GraphMapCustomList.json");
            string customJson = File.Exists(customListPath) ? JsonHelper.ReadJsonFile(customListPath) : "[]";

            // 解析并合并
            // 解析默认列表
            GraphMapTemplateNode = GraphMapTemplateParser.Parse(defaultJson);

            // 创建“自定义模板”根节点
            string customNodeName = "Custom Templates";
            if (LanguageService.Instance != null && LanguageService.Instance["custom_templates"] != null)
            {
                customNodeName = LanguageService.Instance["custom_templates"];
            }

            var customRoot = new GraphMapTemplateNode
            {
                Name = customNodeName,
                Parent = GraphMapTemplateNode
            };

            // 将自定义列表解析并合并到“自定义模板”节点下
            GraphMapTemplateParser.ParseAndMerge(customRoot, customJson, true);

            // 只有当自定义列表不为空时，才将 customRoot 添加到主树中
            if (customRoot.Children.Count > 0)
            {
                GraphMapTemplateNode.Children.Add(customRoot);
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
            UpdatePotentialSnapPoints(value || IsAddingLine || IsAddingArrow || IsAddingPolygon);
        }

        partial void OnIsAddingLineChanged(bool value)
        {
            UpdatePotentialSnapPoints(value || IsPickingPointMode || IsAddingArrow || IsAddingPolygon);
        }

        partial void OnIsAddingArrowChanged(bool value)
        {
            UpdatePotentialSnapPoints(value || IsPickingPointMode || IsAddingLine || IsAddingPolygon);
        }

        partial void OnIsAddingPolygonChanged(bool value)
        {
            UpdatePotentialSnapPoints(value || IsPickingPointMode || IsAddingLine || IsAddingArrow);
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

        /// <summary>
        /// 当鼠标在绘图区域移动时调用
        /// </summary>
        private void WpfPlot1_MouseMove(object? sender, MouseEventArgs e)
        {
            // 将WPF的鼠标位置转换为ScottPlot的像素单位
            Pixel mousePixel = new(e.GetPosition(WpfPlot1).X * WpfPlot1.DisplayScale,
                                  e.GetPosition(WpfPlot1).Y * WpfPlot1.DisplayScale);

            // 将像素位置转换为图表的坐标单位
            Coordinates mouseCoordinates = WpfPlot1.Plot.GetCoordinates(mousePixel);

            // 吸附逻辑
            bool isDrawingOrPicking = IsPickingPointMode || IsAddingLine || IsAddingArrow || IsAddingPolygon;
            bool snapChanged = false;

            if (isDrawingOrPicking)
            {
                var snapPoint = GetSnapPoint(mousePixel);
                if (snapPoint.HasValue)
                {
                    mouseCoordinates = snapPoint.Value;
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

            if (snapChanged) WpfPlot1.Refresh();

            // 获取当前时间 Tick
            long currentTicks = DateTime.Now.Ticks;

            // 只有当开启吸附，且距离上次检测超过 40ms 时，才执行命中测试
            if (IsSnapSelectionEnabled && !(IsPickingPointMode || IsAddingLine || IsAddingArrow || IsAddingPolygon || IsAddingText) && (currentTicks - _lastHitTestTimeTicks > HitTestIntervalTicks))
            {
                // 更新最后检测时间
                _lastHitTestTimeTicks = currentTicks;

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
                    WpfPlot1.Refresh();
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
                WpfPlot1.Refresh();
                return;
            }

            // 实时预览箭头
            if (IsAddingArrow && _arrowStartPoint.HasValue)
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
                WpfPlot1.Refresh();
                return;
            }

            // 处理添加多边形时的鼠标移动预览
            if (IsAddingPolygon && _polygonVertices.Any())
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
                WpfPlot1.Refresh();
            }

            // 如果追踪模式未开启，则不执行任何操作
            if (!IsCrosshairVisible) return;

            // 确保十字轴可见
            if (!CrosshairPlot.IsVisible) CrosshairPlot.IsVisible = true;

            // 更新十字轴的位置
            CrosshairPlot.Position = mouseCoordinates;

            // 默认情况下，显示的值就是鼠标的坐标值
            double xValueToDisplay = mouseCoordinates.X;
            double yValueToDisplay = mouseCoordinates.Y;

            // 查找当前主X轴（Bottom）和主Y轴（Left）
            // 使用缓存的坐标轴定义
            var xAxisDef = _cachedXAxisDef;
            var yAxisDef = _cachedYAxisDef;

            // 如果X轴是对数坐标，则进行反对数转换 (10^x)
            if (xAxisDef?.ScaleType == AxisScaleType.Logarithmic)
            {
                xValueToDisplay = Math.Pow(10, mouseCoordinates.X);
            }

            // 如果Y轴是对数坐标，则进行反对数转换 (10^y)
            if (yAxisDef?.ScaleType == AxisScaleType.Logarithmic)
            {
                yValueToDisplay = Math.Pow(10, mouseCoordinates.Y);
            }

            // 更新十字轴上的文本标签以显示实时坐标 (保留4位小数的数字)
            CrosshairPlot.VerticalLine.Text = $"{xValueToDisplay:N4}";
            CrosshairPlot.HorizontalLine.Text = $"{yValueToDisplay:N4}";

            // 刷新图表以应用更改
            WpfPlot1.Refresh();
        }

        /// <summary>
        /// 初始化面包屑导航
        /// </summary>
        private void InitializeBreadcrumbs()
        {
            Breadcrumbs.Clear();
            Breadcrumbs.Add(new BreadcrumbItem { Name = LanguageService.Instance["all_templates"] });
        }

        /// <summary>
        /// 加载所有模板卡片
        /// </summary>
        private void LoadAllTemplateCards()
        {
            TemplateCards.Clear();
            CollectTemplatesFromNode(GraphMapTemplateNode);
        }

        /// <summary>
        /// 获取模板所在的目录（优先查找 Custom 目录，如果不存在则返回 Default 目录）
        /// </summary>
        private string GetTemplateDirectory(string templatePath)
        {
            var customDir = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "Custom", templatePath);
            if (Directory.Exists(customDir)) return customDir;

            return Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "Default", templatePath);
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
                    string localJsonPath;
                    string localThumbPath = null;

                    if (child.IsCustomTemplate)
                    {
                        // 自定义模板路径构建逻辑：Custom/{GraphMapPath}/{GraphMapPath}.json
                        if (!string.IsNullOrEmpty(child.GraphMapPath))
                        {
                            var customRoot = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "Custom");
                            var templateDir = Path.Combine(customRoot, child.GraphMapPath);
                            
                            localJsonPath = Path.Combine(templateDir, $"{child.GraphMapPath}.json");
                            localThumbPath = Path.Combine(templateDir, "thumbnail.jpg");
                        }
                        else
                        {
                            // 异常情况，保持原样或跳过
                            localJsonPath = string.Empty;
                        }
                    }
                    else
                    {
                        // 1. 构建本地路径
                        var localDir = GetTemplateDirectory(child.GraphMapPath);
                        localJsonPath = Path.Combine(localDir, $"{child.GraphMapPath}.json");
                        localThumbPath = Path.Combine(localDir, "thumbnail.jpg");
                    }

                    // 2. 初始化 ViewModel
                    var cardVm = new TemplateCardViewModel
                    {
                        Name = child.Name,
                        TemplatePath = child.GraphMapPath, // 使用相对路径
                        Category = GetNodePath(child),
                        ServerHash = child.FileHash,    // 注入服务器哈希
                        IsCustomTemplate = child.IsCustomTemplate,

                        // 注入回调
                        OpenHandler = (vm) => SelectTemplateCardCommand.ExecuteAsync(vm),
                        DownloadHandler = DownloadSingleTemplate
                    };

                    // 3. 判断初始状态
                    if (!File.Exists(localJsonPath))
                    {
                        cardVm.State = TemplateState.NotDownloaded;
                        // 占位图
                        cardVm.ThumbnailImage = null;
                    }
                    else
                    {
                        // 是自定义模板，不检查更新
                        if (child.IsCustomTemplate)
                        {
                            cardVm.State = TemplateState.Ready;
                        }
                        else
                        {
                            // 文件存在，校验哈希
                            string localHash = UpdateHelper.ComputeFileMd5(localJsonPath);

                            // 如果 ServerHash 为空(旧列表可能没有)，则默认为 Ready
                            if (string.IsNullOrEmpty(cardVm.ServerHash) ||
                                string.Equals(localHash, cardVm.ServerHash, StringComparison.OrdinalIgnoreCase))
                            {
                                cardVm.State = TemplateState.Ready;
                            }
                            else
                            {
                                cardVm.State = TemplateState.UpdateAvailable;
                            }
                        }

                        // 文件存在，使用真实的缩略图
                        cardVm.ThumbnailImage = FileHelper.LoadBitmapNoLock(localThumbPath);
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
        /// 删除自定义模板
        /// </summary>
        [RelayCommand]
        private async Task DeleteCustomTemplate(TemplateCardViewModel card)
        {
            if (card == null || !card.IsCustomTemplate) return;

            bool confirm = await MessageHelper.ShowAsyncDialog(
                "确定要删除该自定义模板吗？",
                LanguageService.Instance["Cancel"],
                LanguageService.Instance["Confirm"]);

            if (!confirm) return;

            try
            {
                // 1. 确定要删除的路径
                // card.TemplatePath 是相对路径
                string customRoot = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "Custom");
                string folderToDelete = Path.Combine(customRoot, card.TemplatePath);
                string graphMapPathToRemove = card.TemplatePath;

                // 2. 删除文件目录
                if (Directory.Exists(folderToDelete))
                {
                    Directory.Delete(folderToDelete, true);
                }

                // 3. 更新列表 JSON
                string customListPath = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "GraphMapCustomList.json");
                GraphMapTemplateParser.RemoveCustomTemplateEntry(customListPath, graphMapPathToRemove);

                // 4. 刷新界面
                // 重新加载所有模板（包括更新后的自定义列表）
                InitTemplate();

                InitializeBreadcrumbs(); // 重置面包屑到初始状态
                LoadAllTemplateCards();  // 重新加载全部模板卡片

                MessageHelper.Success("模板删除成功");
            }
            catch (Exception ex)
            {
                MessageHelper.Error($"删除失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 导出自定义模板
        /// </summary>
        [RelayCommand]
        private void ExportCustomTemplate(TemplateCardViewModel card)
        {
            if (card == null || !card.IsCustomTemplate) return;

            try
            {
                // 1. 确定源目录
                string customRoot = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "Custom");
                string sourceDir = Path.Combine(customRoot, card.TemplatePath);

                if (!Directory.Exists(sourceDir))
                {
                    MessageHelper.Error("模板源文件不存在");
                    return;
                }

                // 2. 选择保存位置
                var dialog = new VistaSaveFileDialog();
                dialog.Filter = "Zip Files (*.zip)|*.zip";
                dialog.DefaultExt = "zip";
                dialog.FileName = $"{card.Name}.zip";

                if (dialog.ShowDialog() == true)
                {
                    string destinationPath = dialog.FileName;

                    // 如果目标文件已存在，删除它（ZipFile.CreateFromDirectory 不会覆盖）
                    if (File.Exists(destinationPath))
                    {
                        File.Delete(destinationPath);
                    }

                    // 3. 打包
                    System.IO.Compression.ZipFile.CreateFromDirectory(sourceDir, destinationPath);

                    MessageHelper.Success("模板导出成功");
                }
            }
            catch (Exception ex)
            {
                MessageHelper.Error($"导出失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 打开模板所在位置
        /// </summary>
        [RelayCommand]
        private void OpenTemplateLocation(TemplateCardViewModel card)
        {
            if (card == null) return;

            string path = GetTemplateDirectory(card.TemplatePath);
            if (Directory.Exists(path))
            {
                System.Diagnostics.Process.Start("explorer.exe", path);
            }
            else
            {
                MessageHelper.Warning("目录不存在");
            }
        }

        /// <summary>
        /// 点击模板分类节点
        /// </summary>
        [RelayCommand]
        private void SelectTreeViewItem(GraphMapTemplateNode graphMapTemplateNode)
        {
            if (graphMapTemplateNode == null) return;
            currentgraphMapTemplateNode = graphMapTemplateNode;

            // 如果是模板文件（叶子节点）
            if (!string.IsNullOrEmpty(graphMapTemplateNode.GraphMapPath))
            {
                // 显示单个模板卡片
                ShowSingleTemplateCard(graphMapTemplateNode);
            }
            else
            {
                // 显示分类下的所有模板卡片
                ShowCategoryTemplateCards(graphMapTemplateNode);
            }
        }

        /// <summary>
        /// 显示单个模板卡片
        /// </summary>
        private void ShowSingleTemplateCard(GraphMapTemplateNode templateNode)
        {
            TemplateCards.Clear();

            string jsonPath;
            string thumbnailPath = null;

            if (templateNode.IsCustomTemplate)
            {
                // 自定义模板路径构建逻辑：Custom/{GraphMapPath}/{GraphMapPath}.json
                if (!string.IsNullOrEmpty(templateNode.GraphMapPath))
                {
                    var customRoot = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "Custom");
                    var templateDir = Path.Combine(customRoot, templateNode.GraphMapPath);

                    jsonPath = Path.Combine(templateDir, $"{templateNode.GraphMapPath}.json");
                    thumbnailPath = Path.Combine(templateDir, "thumbnail.jpg");
                }
                else
                {
                    jsonPath = string.Empty;
                }
            }
            else
            {
                // 构建基础路径
                var baseDir = GetTemplateDirectory(templateNode.GraphMapPath);
                thumbnailPath = Path.Combine(baseDir, "thumbnail.jpg");
                jsonPath = Path.Combine(baseDir, $"{templateNode.GraphMapPath}.json");
            }

            // 初始化 ViewModel
            var cardVm = new TemplateCardViewModel
            {
                Name = templateNode.Name,
                TemplatePath = templateNode.GraphMapPath,
                Category = GetNodePath(templateNode),
                ServerHash = templateNode.FileHash,
                IsCustomTemplate = templateNode.IsCustomTemplate,

                // 注入回调
                OpenHandler = (vm) => SelectTemplateCardCommand.ExecuteAsync(vm),
                DownloadHandler = DownloadSingleTemplate
            };

            // 判断文件状态并加载图片
            if (File.Exists(jsonPath))
            {
                // 是自定义模板，不检查更新
                if (templateNode.IsCustomTemplate)
                {
                    cardVm.State = TemplateState.Ready;
                }
                else
                {
                    // 计算哈希以确定是否需要更新（逻辑保持与 LoadAllTemplateCards 一致）
                    string localHash = UpdateHelper.ComputeFileMd5(jsonPath);

                    if (string.IsNullOrEmpty(cardVm.ServerHash) ||
                        string.Equals(localHash, cardVm.ServerHash, StringComparison.OrdinalIgnoreCase))
                    {
                        cardVm.State = TemplateState.Ready;
                    }
                    else
                    {
                        cardVm.State = TemplateState.UpdateAvailable;
                    }
                }

                // 加载图片
                if (File.Exists(thumbnailPath))
                {
                    // 使用无锁方式加载图片，避免文件占用
                    cardVm.ThumbnailImage = FileHelper.LoadBitmapNoLock(thumbnailPath);
                }
            }
            else
            {
                // 如果 JSON 文件不存在，说明未下载
                cardVm.State = TemplateState.NotDownloaded;
                cardVm.ThumbnailImage = null;
            }

            TemplateCards.Add(cardVm);
            UpdateBreadcrumbs(templateNode);
        }

        /// <summary>
        /// 显示分类下的模板卡片
        /// </summary>
        private void ShowCategoryTemplateCards(GraphMapTemplateNode categoryNode)
        {
            TemplateCards.Clear();
            // 递归收集该节点下的所有模板文件
            CollectTemplatesFromNode(categoryNode);
            UpdateBreadcrumbs(categoryNode);
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

            // 切换到绘图模式
            IsTemplateMode = false;
            IsPlotMode = true;

            // 加载模板文件
            var baseDir = GetTemplateDirectory(card.TemplatePath);
            var templateFilePath = Path.Combine(baseDir, $"{card.TemplatePath}.json");
            _currentTemplateFilePath = templateFilePath;
            await LoadAndBuildLayers(templateFilePath);

            CenterPlot();   // 视图复位

            // 加载底图模板的说明文件
            var tempRTFfile = FileHelper.FindFileOrGetFirstWithExtension(
                    baseDir, LanguageService.CurrentLanguage, ".rtf");
            RtfHelper.LoadRtfToRichTextBox(tempRTFfile, _richTextBox);

            // 加载数据表格控件
            PrepareDataGridForInput();
        }

        /// <summary>
        /// 显示绘图模板的帮助说明
        /// </summary>
        [RelayCommand]
        private void ShowTemplateInfo()
        {
            IsShowTemplateInfo = !IsShowTemplateInfo;
        }

        /// <summary>
        /// 返回模板库-浏览模式
        /// </summary>
        [RelayCommand]
        private void BackToTemplateMode()
        {
            // 在返回模板库之前，完全重置绘图状态
            ResetPlotStateToDefault();

            IsTemplateMode = true;
            IsPlotMode = false;
            RibbonTabIndex = 0;

            // 重新加载所有模板（包括更新后的自定义列表）
            InitTemplate();

            InitializeBreadcrumbs(); // 重置面包屑到初始状态
            LoadAllTemplateCards();  // 重新加载全部模板卡片
        }

        /// <summary>
        /// 面包屑导航点击
        /// </summary>
        [RelayCommand]
        private void NavigateToBreadcrumb(BreadcrumbItem item)
        {
            if (item == null) return;

            if (item.Node == null)
            {
                // 返回全部模板
                LoadAllTemplateCards();
                InitializeBreadcrumbs();
            }
            else
            {
                // 获取对应的节点
                var targetNode = item.Node;

                ShowCategoryTemplateCards(targetNode);
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
        partial void OnPropertyGridModelChanged(object oldValue, object newValue)
        {
            // 取消显示脚本面板
            ScriptsPropertyGrid = false;

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

            bool isAxisChange = sender is BaseAxisDefinition;

            // 全量重绘
            RefreshPlotFromLayers(!isAxisChange);

            // 恢复遮罩
            ReapplySelectionVisualState();
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
        /// 降低指定图层透明度，实现遮罩效果
        /// </summary>
        private void DimLayer(LayerItemViewModel layer)
        {
            if (layer is IPlotLayer plotLayer)
            {
                plotLayer.Dim();
            }
        }

        /// <summary>
        /// 加载模板、构建图层树并刷新绘图
        /// </summary>
        /// <param name="templatePath">模板文件的路径</param>
        private async Task LoadAndBuildLayers(string templatePath)
        {
            // 重置编辑确认状态和标签页索引
            _hasConfirmedEditMode = false;
            RibbonTabIndex = 0;

            if (!File.Exists(templatePath))
            {
                // 文件不存在
                return;
            }

            // 读取并反序列化模板文件
            var templateJsonContent = File.ReadAllText(templatePath);
            var options = new JsonSerializerOptions { 
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            CurrentTemplate = JsonSerializer.Deserialize<GraphMapTemplate>(templateJsonContent, options);

            if (CurrentTemplate == null) return;

            // 根据加载的模板数据，构建【图层树】
            BuildLayerTreeFromTemplate(CurrentTemplate);

            // 根据新建的【图层树】来渲染前端
            RefreshPlotFromLayers();
        }

        /// <summary>
        /// 使用当前加载的模板数据填充 LayerTree 集合
        /// 负责添加图层对象
        /// </summary>
        private void BuildLayerTreeFromTemplate(GraphMapTemplate template)
        {
            LayerTree.Clear();
            UpdateAxisCache(); // 更新坐标轴缓存
            var info = template.Info;

            // 坐标轴图层
            var axesCategory = new CategoryLayerItemViewModel(LanguageService.Instance["axes"]);

            // 遍历添加坐标轴对象
            foreach (var axis in info.Axes)
            {
                var axisLayer = new AxisLayerItemViewModel(axis);

                // 监听 IsVisible 属性的变化，当它改变时，刷新整个图表
                axisLayer.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(AxisLayerItemViewModel.IsVisible))
                    {
                        RefreshPlotFromLayers(true);    // 切换显隐不重置缩放
                    }
                };
                axesCategory.Children.Add(axisLayer);
            }

            // 空对象不添加图层
            if (axesCategory.Children.Any()) LayerTree.Add(axesCategory);

            // 点图层
            var pointsCategory = new CategoryLayerItemViewModel(LanguageService.Instance["point"]);
            for (int i = 0; i < info.Points.Count; i++)
            {
                var pointLayer = new PointLayerItemViewModel(info.Points[i], i);
                // 监听每个图层的 IsVisible 变化，自动刷新
                pointLayer.PropertyChanged += (s, e) => { 
                    if (e.PropertyName == nameof(PointLayerItemViewModel.IsVisible)) RefreshPlotFromLayers(); };
                pointsCategory.Children.Add(pointLayer);
            }
            if (pointsCategory.Children.Any()) LayerTree.Add(pointsCategory);

            // 线图层
            var linesCategory = new CategoryLayerItemViewModel(LanguageService.Instance["line"]);
            for (int i = 0; i < info.Lines.Count; i++)
            {
                var lineLayer = new LineLayerItemViewModel(info.Lines[i], i);
                lineLayer.PropertyChanged += (s, e) => { 
                    if (e.PropertyName == nameof(LineLayerItemViewModel.IsVisible)) 
                        RefreshPlotFromLayers(); };
                linesCategory.Children.Add(lineLayer);
            }
            if (linesCategory.Children.Any()) LayerTree.Add(linesCategory);


            // 文本图层
            var textCategory = new CategoryLayerItemViewModel(LanguageService.Instance["text"]);
            for (int i = 0; i < info.Texts.Count; i++)
            {
                var textLayer = new TextLayerItemViewModel(info.Texts[i], i);
                // 监听每个图层的 IsVisible 变化，自动刷新
                textLayer.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(TextLayerItemViewModel.IsVisible)) RefreshPlotFromLayers(); };
                textCategory.Children.Add(textLayer);
            }
            if (textCategory.Children.Any()) LayerTree.Add(textCategory);

            // 箭头图层
            var arrowsCategory = new CategoryLayerItemViewModel(LanguageService.Instance["arrow"]);
            for (int i = 0; i < template.Info.Arrows.Count; i++)
            {
                var arrowLayer = new ArrowLayerItemViewModel(template.Info.Arrows[i], i);
                arrowLayer.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(ArrowLayerItemViewModel.IsVisible)) RefreshPlotFromLayers(); };
                arrowsCategory.Children.Add(arrowLayer);
            }
            if (arrowsCategory.Children.Any()) LayerTree.Add(arrowsCategory);

            // 注释图层
            var annotationCategory = new CategoryLayerItemViewModel(LanguageService.Instance["annotation"]);
            for (int i = 0; i < info.Annotations.Count; i++)
            {
                var annotationLayer = new AnnotationLayerItemViewModel(info.Annotations[i], i);
                // 监听每个图层的 IsVisible 变化，自动刷新
                annotationLayer.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(AnnotationLayerItemViewModel.IsVisible)) RefreshPlotFromLayers(); };
                annotationCategory.Children.Add(annotationLayer);
            }
            if (annotationCategory.Children.Any()) LayerTree.Add(annotationCategory);

            // 多边形图层
            var polygonsCategory = new CategoryLayerItemViewModel(LanguageService.Instance["polygon"]);
            for (int i = 0; i < info.Polygons.Count; i++) // 假设 template.Info 中有 Polygons 列表
            {
                var polygonLayer = new PolygonLayerItemViewModel(info.Polygons[i], i);
                polygonLayer.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(PolygonLayerItemViewModel.IsVisible)) RefreshPlotFromLayers(); };
                polygonsCategory.Children.Add(polygonLayer);
            }
            if (polygonsCategory.Children.Any()) LayerTree.Add(polygonsCategory);
            // todo: 添加对其他绘图对象的处理
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
                // 此时 _potentialSnapMarkers 可能还是旧的引用，已经被 Clear 了
                // 我们需要重新生成它们并添加到新的 Plot 中
                // 先清空旧列表，因为 Plot.Clear() 已经移除了它们
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
                //WpfPlot1.Plot.Axes.AutoScale();
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
            WpfPlot1.Plot.Legend.FontName = Fonts.Detect(CurrentTemplate.Info.Title.Label.Get());   // 临时使用 Title 的字体
            WpfPlot1.Plot.Legend.Orientation = CurrentTemplate.Info.Legend.Orientation;
            WpfPlot1.Plot.Legend.IsVisible = CurrentTemplate.Info.Legend.IsVisible;

            // 全局设置——处理标题
            if (CurrentTemplate.Info.Title.Label.Translations.Any())
            {
                WpfPlot1.Plot.Axes.Title.Label.Text = CurrentTemplate.Info.Title.Label.Get();
                WpfPlot1.Plot.Axes.Title.Label.FontName = Fonts.Detect(WpfPlot1.Plot.Axes.Title.Label.Text);
                WpfPlot1.Plot.Axes.Title.Label.FontSize = CurrentTemplate.Info.Title.Size;
                WpfPlot1.Plot.Axes.Title.Label.ForeColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(CurrentTemplate.Info.Title.Color));
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
                grid.MajorLineColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(gridDef.MajorGridLineColor));
                grid.MajorLineWidth = gridDef.MajorGridLineWidth;
                grid.MajorLinePattern = GraphMapTemplateParser.GetLinePattern(gridDef.MajorGridLinePattern.ToString());
                grid.XAxisStyle.MajorLineStyle.AntiAlias = gridDef.MajorGridLineAntiAlias;
                grid.YAxisStyle.MajorLineStyle.AntiAlias = gridDef.MajorGridLineAntiAlias;


                // 应用次网格线样式
                grid.XAxisStyle.MinorLineStyle.IsVisible = gridDef.MinorGridLineIsVisible;
                grid.YAxisStyle.MinorLineStyle.IsVisible = gridDef.MinorGridLineIsVisible;
                grid.MinorLineColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(gridDef.MinorGridLineColor));
                grid.MinorLineWidth = gridDef.MinorGridLineWidth;
                grid.XAxisStyle.MinorLineStyle.AntiAlias = gridDef.MinorGridLineAntiAlias;
                grid.YAxisStyle.MinorLineStyle.AntiAlias = gridDef.MinorGridLineAntiAlias;

                // 应用交替填充背景
                if (gridDef.GridAlternateFillingIsEnable)
                {
                    grid.XAxisStyle.FillColor1 = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(gridDef.GridFillColor1));
                    grid.YAxisStyle.FillColor1 = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(gridDef.GridFillColor1));
                    grid.XAxisStyle.FillColor2 = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(gridDef.GridFillColor2));
                    grid.YAxisStyle.FillColor2 = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(gridDef.GridFillColor2));
                }
                else
                {
                    // 如果禁用，则设置为透明
                    grid.XAxisStyle.FillColor1 = Colors.Transparent;
                    grid.YAxisStyle.FillColor1 = Colors.Transparent;
                    grid.XAxisStyle.FillColor2 = Colors.Transparent;
                    grid.YAxisStyle.FillColor2 = Colors.Transparent;
                }

                // 根据API文档，DefaultGrid也有一个总的IsVisible属性，确保它也被设置
                grid.IsVisible = gridDef.MajorGridLineIsVisible || gridDef.MinorGridLineIsVisible;
            }
        }

        /// <summary>
        /// 渲染三元相图
        /// </summary>
        private void RenderTernaryPlot()
        {
            // 添加三角坐标轴到图表，并获取其引用
            var triangularAxis = WpfPlot1.Plot.Add.TriangularAxis(clockwise: CurrentTemplate.Clockwise);

            // 应用模板中的网格和背景样式
            var gridDef = CurrentTemplate.Info.Grid;
            if (gridDef != null)
            {
                // 应用网格线样式
                triangularAxis.GridLineStyle.Color = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(gridDef.MajorGridLineColor));
                triangularAxis.GridLineStyle.Width = gridDef.MajorGridLineWidth;
                triangularAxis.GridLineStyle.Pattern = GraphMapTemplateParser.GetLinePattern(gridDef.MajorGridLinePattern.ToString());

                // 应用背景填充样式 (TriangularAxis只有一个FillStyle)
                if (gridDef.GridAlternateFillingIsEnable)
                {
                    // 使用FillColor1作为其填充色
                    triangularAxis.FillStyle.Color = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(gridDef.GridFillColor1));
                }
                else
                {
                    triangularAxis.FillStyle.Color = Colors.Transparent;
                }
            }

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
                    PropertyGridModel = nullObject;
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
                PropertyGridModel = nullObject;
                
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
                    var toRemove = SelectedLayers.Where(l => {
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
            // 先解绑旧事件
            if (PropertyGridModel is INotifyPropertyChanged oldProp)
            {
                oldProp.PropertyChanged -= OnPropertyGridModelChanged;
            }

            object? objectToInspect = GetLayerDefinition(_selectedLayer);
            PropertyGridModel = objectToInspect;

            // 绑定新事件用于同步修改
            if (PropertyGridModel is INotifyPropertyChanged newProp)
            {
                newProp.PropertyChanged += OnPropertyGridModelChanged;
            }

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

        /// <summary>
        /// 当属性面板绑定的对象属性发生变化时触发，用于同步多选对象的属性
        /// </summary>
        private void OnPropertyGridModelChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender == null) return;

            // 使用反射获取变更的属性值
            var propInfo = sender.GetType().GetProperty(e.PropertyName);
            if (propInfo == null || !propInfo.CanRead || !propInfo.CanWrite) return;

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
                            // 应用修改后，恢复显示状态（这将把新的 def 属性应用到 Plottable）
                            if (layer is IPlotLayer plotLayer)
                            {
                                plotLayer.Restore();
                            }
                        }
                        catch
                        {
                            // 忽略设置失败（例如某些属性不可写或验证失败）
                        }
                    }
                }
            }

            // 决定是否保留当前视图范围
            // 如果修改的是坐标轴范围，则不保留（使用新设置的范围），否则保留当前平移缩放状态
            bool preserveLimits = true;
            if (sender is Models.CartesianAxisDefinition && (e.PropertyName == "Minimum" || e.PropertyName == "Maximum"))
            {
                preserveLimits = false;
            }

            // 对于单选或多选，都需要刷新整个图表
            RefreshPlotFromLayers(preserveLimits);
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
            WpfPlot1.Plot.Axes.AutoScale();
            WpfPlot1.Refresh();
        }

        /// <summary>
        /// 取消选择
        /// </summary>
        [RelayCommand]
        private void CancelSelected()
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

            PropertyGridModel = nullObject;   // 取消属性编辑器
            ScriptsPropertyGrid = false;

            IsShowTemplateInfo = false;     // 取消绘图模板指南显示
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
                MessageHelper.Warning("三元相图暂不支持定位功能");
                return;
            }

            // 切换追踪模式的状态
            // IsCrosshairVisible = !IsCrosshairVisible; // 双向绑定已自动更新，此处无需手动切换
        }

        /// <summary>
        /// 图例设置
        /// </summary>
        [RelayCommand]
        private void LegendSetting()
        {
            PropertyGridModel = CurrentTemplate.Info.Legend;
        }

        /// <summary>
        /// 网格设置
        /// </summary>
        [RelayCommand]
        private void GridSetting()
        {
            PropertyGridModel = CurrentTemplate.Info.Grid;
        }

        /// <summary>
        /// 脚本设置
        /// </summary>
        [RelayCommand]
        private void ScriptSetting()
        {
            // 清空属性面板
            PropertyGridModel = nullObject;
            ScriptsPropertyGrid = !ScriptsPropertyGrid;
        }

        /// <summary>
        /// 绘图设置——标题属性
        /// </summary>
        [RelayCommand]
        private void PlotSetting()
        {
            PropertyGridModel = CurrentTemplate.Info.Title;
        }

        // 导出图片
        [RelayCommand]
        public void ExportImg(string fileType)
        {

            string tempFileName = "OutPut_fig." + fileType;

            // 读取默认文件保存位置
            string temp;
            if (ConfigHelper.GetConfig("database_location_path") == "")
            {
                temp = FileHelper.GetSaveFilePath(tempFileName);
                if (temp == string.Empty) { return; }
            }
            else
            {
                temp = FileHelper.GetSaveFilePath(tempFileName, ConfigHelper.GetConfig("database_location_path"));
                if (temp == string.Empty) { return; }
                //temp = Path.Combine(temp, tempFileName);
            }

            int tempWidth = (int)WpfPlot1.Plot.LastRender.DataRect.Width;
            int tempHeight = (int)WpfPlot1.Plot.LastRender.DataRect.Height;
            WpfPlot1.Plot.Save(temp, (int)(tempWidth * 1.25), (int)(tempHeight * 1.25));
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
        /// 清除绘图中的数据点，但是不会清除数据表格
        /// </summary>
        [RelayCommand]
        private void ClearPlotDataPoints()
        {
            ClearImportedDataAsync();
        }

        /// <summary>
        /// 确认新建图解模板
        /// </summary>
        /// <param name="newTemplateControl">自定义控件</param>
        [RelayCommand]
        private void ConfirmNewTemplate(NewTemplateControl newTemplateControl)
        {
            if (newTemplateControl == null) return;

            // 获取数据
            string language = newTemplateControl.Language;
            string category = newTemplateControl.CategoryHierarchy;
            string plotType = newTemplateControl.PlotType;

            // 检查数据是否有效
            if (string.IsNullOrWhiteSpace(language) || string.IsNullOrWhiteSpace(category)
                 || string.IsNullOrEmpty(plotType))
            {
                // 所有字段均为必填项！
                MessageHelper.Warning(LanguageService.Instance["all_fields_required"]);
                return;
            }

            var allLanguages = language.Split(new[] { " > " }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(lang => lang.Trim())
                                .ToList();

            // 检查是否有重复语言key
            if (allLanguages.Distinct().Count() != allLanguages.Count)
            {
                // 语言设置中存在重复项，请检查！
                MessageHelper.Warning(LanguageService.Instance["language_setting_duplicate_found"]);
                return;
            }

            // 获取富文本形式的分类层级数据
            var categoryParts = newTemplateControl.GetCategoryParts().ToList();

            if (categoryParts.Count < 2)
            {
                // 分类结构必须大于等于2！
                MessageHelper.Warning(LanguageService.Instance["category_structure_min_two"]);
                return;
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

            // 使用自定义模板文件夹路径
            string customDir = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "Custom");
            
            // 构建路径：Custom/A_B/A_B.json
            string directoryPath = Path.Combine(customDir, folderName);

            _currentTemplateFilePath = Path.Combine(directoryPath, $"{folderName}.json");

            // 创建新底图，传入 LocalizedString
            CurrentTemplate = GraphMapTemplate.CreateDefault(allLanguages, plotType, localizedCategory);

            IsTemplateMode = false;
            IsPlotMode = true;
            IsNewTemplateMode = false;

            BuildLayerTreeFromTemplate(CurrentTemplate);
            RefreshPlotFromLayers();

            // 清空数据
            newTemplateControl.EmptyData();
        }

        /// <summary>
        /// 取消新建图解模板
        /// </summary>
        [RelayCommand]
        private void CancelNewTemplate(NewTemplateControl newTemplateControl)
        {
            if (newTemplateControl == null) return;

            // 清空数据
            newTemplateControl.EmptyData();

            IsNewTemplateMode = false;
        }

        /// <summary>
        /// 新建底图——弹窗新建
        /// </summary>
        [RelayCommand]
        private void NewTemplate()
        {
            IsNewTemplateMode = true;
        }

        /// <summary>
        /// 将当前底图模板另存为新文件
        /// </summary>
        [RelayCommand]
        private void SaveBaseMapAs()
        {
            // 确保有模板可以保存
            if (CurrentTemplate == null)
            {
                MessageHelper.Error(LanguageService.Instance["no_template_or_path_specified"]);
                return;
            }

            // 配置并显示文件保存对话框
            var saveFileDialog = new VistaSaveFileDialog
            {
                Title = LanguageService.Instance["save_template_as"], // "模板另存为..."
                Filter = $"{LanguageService.Instance["template_files"]} (*.json)|*.json|{LanguageService.Instance["all_files"]} (*.*)|*.*",
                DefaultExt = ".json",
                FileName = Path.GetFileName(_currentTemplateFilePath) ?? "NewTemplate.json" // 默认文件名
            };

            // 如果用户选择了路径并点击了 "保存"
            if (saveFileDialog.ShowDialog() == true)
            {
                // 获取用户选择的完整文件路径
                string newFilePath = saveFileDialog.FileName;

                // 更新当前ViewModel内部的模板文件路径
                _currentTemplateFilePath = newFilePath;

                // 调用核心保存逻辑
                PerformSave(newFilePath);
            }
        }

        /// <summary>
        /// 保存当前底图模板
        /// </summary>
        [RelayCommand]
        private void SaveBaseMap()
        {
            // 确保有模板和路径可供保存
            if (CurrentTemplate == null || string.IsNullOrWhiteSpace(_currentTemplateFilePath))
            {
                // 没有可保存的模板或未指定保存路径。请使用另存为保存
                MessageHelper.Error(LanguageService.Instance["no_template_or_path_specified"]);
                return;
            }

            // 如果路径存在，直接在原路径上保存
            PerformSave(_currentTemplateFilePath);
        }

        private class GraphMapListItem
        {
            public LocalizedString NodeList { get; set; }
            public string GraphMapPath { get; set; }
            public string FileHash { get; set; }
        }

        /// <summary>
        /// 导入自定义绘图模板
        /// </summary>
        [RelayCommand]
        private async Task ImportCustomTemplate()
        {
            var openFileDialog = new VistaOpenFileDialog
            {
                Title = "导入自定义绘图模板",
                Filter = "Zip Files (*.zip)|*.zip|All Files (*.*)|*.*",
                DefaultExt = ".zip",
                CheckFileExists = true,
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() != true) return;

            try
            {
                string zipPath = openFileDialog.FileName;
                
                // 1. 验证 ZIP 包
                string validTemplateName = null;
                GraphMapTemplate template = null;
                
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
                    }
                });

                if (string.IsNullOrEmpty(validTemplateName) || template == null)
                {
                    MessageHelper.Error("模板文件已损坏或未包含有效的模板 JSON 文件。");
                    return;
                }

                // 2. 准备目标路径
                string fileNameNoExt = validTemplateName;
                string customDir = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "Custom", fileNameNoExt);
                
                if (Directory.Exists(customDir))
                {
                    bool confirm = await MessageHelper.ShowAsyncDialog(
                        $"自定义模板 '{fileNameNoExt}' 已存在，是否覆盖？",
                        "取消",
                        "覆盖");

                    if (!confirm) return;

                    try
                    {
                        Directory.Delete(customDir, true);
                    }
                    catch (Exception ex)
                    {
                        MessageHelper.Error($"无法删除旧目录: {ex.Message}");
                        return;
                    }
                }

                Directory.CreateDirectory(customDir);

                // 3. 解压所有内容
                await Task.Run(() =>
                {
                    ZipFile.ExtractToDirectory(zipPath, customDir);
                });

                // 4. 更新列表记录 (GraphMapCustomList.json)
                // 确保 NodeList 完整性
                if (template.NodeList == null) template.NodeList = new LocalizedString();

                // 确保有中文路径
                if (!template.NodeList.Translations.ContainsKey("zh-CN"))
                {
                    template.NodeList.Translations["zh-CN"] = fileNameNoExt;
                }

                // 确保有英文路径
                if (!template.NodeList.Translations.ContainsKey("en-US"))
                {
                    template.NodeList.Translations["en-US"] = fileNameNoExt;
                }
                template.NodeList.Default = "en-US";

                string customListPath = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "GraphMapCustomList.json");
                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true, 
                    WriteIndented = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                };
                
                string listJson = File.Exists(customListPath) ? await File.ReadAllTextAsync(customListPath) : "[]";
                var list = JsonSerializer.Deserialize<List<GraphMapListItem>>(listJson, options) ?? new List<GraphMapListItem>();

                // 移除旧项（如果存在）
                var existingItem = list.FirstOrDefault(x => x.GraphMapPath.Equals(fileNameNoExt, StringComparison.OrdinalIgnoreCase));
                if (existingItem != null)
                {
                    list.Remove(existingItem);
                }

                // 添加新项
                list.Add(new GraphMapListItem
                {
                    NodeList = template.NodeList,
                    GraphMapPath = fileNameNoExt,
                    FileHash = "" 
                });

                // 保存列表
                string newListJson = JsonSerializer.Serialize(list, options);
                await File.WriteAllTextAsync(customListPath, newListJson);

                // 5. 刷新界面
                InitTemplate();
                LoadAllTemplateCards();
                InitializeBreadcrumbs();

                MessageHelper.Success(LanguageService.Instance["template_saved_successfully"]);
            }
            catch (Exception ex)
            {
                MessageHelper.Error(LanguageService.Instance["save_template_failed"] + $": {ex.Message}");
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
                Filter = $"{LanguageService.Instance["template_files"]} (*.json)|*.json|{LanguageService.Instance["all_files"]} (*.*)|*.*",
                DefaultExt = ".json",
                CheckFileExists = true, // 确保文件存在
                Multiselect = false
            };

            // 如果用户选择了一个文件并点击了 "确定"
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // 获取用户选择的完整文件路径
                    string filePath = openFileDialog.FileName;
                    // 将当前编辑的文件路径更新为用户选择的路径，以便后续保存操作
                    _currentTemplateFilePath = filePath;

                    // 切换到绘图模式
                    IsTemplateMode = false;
                    IsPlotMode = true;

                    // 异步加载模板文件，构建图层并刷新绘图
                    await LoadAndBuildLayers(filePath);

                    // 尝试加载与模板文件位于同一目录下的说明文件 (.rtf)
                    string directoryPath = Path.GetDirectoryName(filePath);
                    var tempRTFfile = FileHelper.FindFileOrGetFirstWithExtension(
                                          directoryPath,
                                          LanguageService.CurrentLanguage,
                                          ".rtf");
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
                    BackToTemplateMode();
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
            // 验证模板和脚本是否有效
            if (CurrentTemplate?.Script == null || string.IsNullOrEmpty(CurrentTemplate.Script.RequiredDataSeries))
            {
                MessageHelper.Error(LanguageService.Instance["script_not_defined_in_template"]);
                return;
            }

            var scriptDefinition = CurrentTemplate.Script;

            // 从 ReoGridControl 读取数据到 DataTable
            var worksheet = _dataGrid.Worksheets[0];
            var dataTable = new DataTable();
            var requiredColumns = new List<string>();

            // 根据表头创建 DataTable 的列
            foreach (var header in worksheet.ColumnHeaders)
            {
                if (string.IsNullOrEmpty(header.Text)) break; // 遇到空表头则停止
                dataTable.Columns.Add(header.Text);
                requiredColumns.Add(header.Text);
            }

            // 如果没有有效的列，则不继续
            if (dataTable.Columns.Count == 0)
            {
                MessageHelper.Warning(LanguageService.Instance["no_data_columns_defined"]);
                return;
            }

            // 遍历行来填充 DataTable
            for (int r = 0; r <= worksheet.MaxContentRow; r++)
            {
                var newRow = dataTable.NewRow();
                bool isRowEmpty = true;
                for (int c = 0; c < dataTable.Columns.Count; c++)
                {
                    var cellValue = worksheet.GetCellData(r, c)?.ToString();
                    newRow[c] = cellValue;
                    if (!string.IsNullOrWhiteSpace(cellValue))
                    {
                        isRowEmpty = false;
                    }
                }
                // 如果整行都是空的，则跳过
                if (!isRowEmpty)
                {
                    dataTable.Rows.Add(newRow);
                }
            }

            if (dataTable.Rows.Count == 0)
            {
                MessageHelper.Info(LanguageService.Instance["no_data_please_add_data"]);
                return;
            }

            // 清除之前导入的数据点
            ClearExistingPlottedData();

            string categoryColumn = requiredColumns[0];
            var dataColumns = requiredColumns.Skip(1).ToList();

            // 先对数据进行分组，并附加行号（从1开始）
            var groupedData = dataTable.AsEnumerable()
                .Select((row, index) => new { Row = row, Index = index + 1 })
                .Where(x => x.Row[categoryColumn] != null && !string.IsNullOrEmpty(x.Row[categoryColumn].ToString()))
                .GroupBy(x => x.Row.Field<string>(categoryColumn));

            // 检查分组是否成功
            if (!groupedData.Any())
            {
                MessageHelper.Warning(LanguageService.Instance["failed_to_parse_category_group"]);
                return;
            }

            // 确认数据有效后，再创建“数据点”的根节点
            var rootDataNode = LayerTree.FirstOrDefault(c => c.Name == LanguageService.Instance["data_point"]) as CategoryLayerItemViewModel;
            if (rootDataNode == null)
            {
                rootDataNode = new CategoryLayerItemViewModel(LanguageService.Instance["data_point"]);
                LayerTree.Add(rootDataNode);
            }
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

                    foreach (var item in group)
                    {
                        DataRow row = item.Row;
                        int rowIndex = item.Index; // 获取原始数据行号（从1开始）
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

                    var categoryViewModel = new ScatterLayerItemViewModel(scatterDefForCategory)
                    {
                        Name = categoryName,
                        //Plottable = scatterPlotForCategory,
                        DataPoints = cartesianCoords,
                        IsVisible = true
                    };

                    rootDataNode.Children.Add(categoryViewModel);
                }

                // 如果存在校验失败的数据，则进行提示
                if (validationErrors.Any())
                {
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

                    foreach (var item in group) // item 包含 Row 和 Index
                    {
                        DataRow row = item.Row;
                        try
                        {
                            var coordinates = CalculateCoordinatesUsingScript(engine, row, dataColumns, scriptDefinition.ScriptBody);
                            // 脚本必须为笛卡尔坐标图返回两个值
                            if (coordinates != null && coordinates.Length == 2)
                            {
                                xs.Add(coordinates[0]);
                                ys.Add(coordinates[1]);
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

                    var categoryViewModel = new ScatterLayerItemViewModel(scatterDefForCategory)
                    {
                        Name = categoryName,
                        //Plottable = scatterPlotForCategory,
                        DataPoints = points,
                        IsVisible = true
                    };
                    rootDataNode.Children.Add(categoryViewModel);
                }
            }

            // 刷新图表和图例
            WpfPlot1.Plot.Legend.IsVisible = true;
            WpfPlot1.Plot.Axes.AutoScale();
            //WpfPlot1.Refresh();
            RefreshPlotFromLayers();
            MessageHelper.Success(LanguageService.Instance["data_plotting_successful"]);

            // 投点后自动切回绘图选项卡
            RibbonTabIndex = 0;
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

                // 从服务器获取 server_info.json
                string jsonContent = await UpdateHelper.GetUrlContentAsync();

                // 反序列化 JSON
                var serverInfo = JsonSerializer.Deserialize<ServerInfo>(jsonContent);

                if (serverInfo == null) return;

                // 对比哈希值 (不区分大小写
                if (!string.Equals(localHash, serverInfo.ListHash, StringComparison.OrdinalIgnoreCase))
                {
                    // 哈希不匹配，提示用户更新
                    // 检测到绘图模板库有新版本，是否立即更新列表？
                    bool confirmUpdate = await MessageHelper.ShowAsyncDialog(
                        LanguageService.Instance["new_drawing_template_library_version_detected"],
                        LanguageService.Instance["Cancel"],
                        LanguageService.Instance["Confirm"]);

                    if (confirmUpdate)
                    {
                        await PerformTemplateListUpdate(serverInfo.ListHash);
                    }
                }
                else
                {
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
        private async Task PerformTemplateListUpdate(string expectedHash = null)
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
                // 模板列表更新成功！正在刷新...
                MessageHelper.Success(LanguageService.Instance["template_list_update_success_refreshing"]);

                // 重新读取刚覆盖的文件
                InitTemplate();

                // 刷新 UI (重新加载卡片，触发新的哈希对比)
                BackToTemplateMode();
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
        /// 检查类别结构更新的命令
        /// </summary>
        [RelayCommand]
        private async Task CheckForCategoryUpdates()
        {
            try
            {
                // 获取本地 PlotTemplateCategories.json
                string localListPath = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "PlotTemplateCategories.json");

                // 计算本地文件的哈希值
                string localHash = UpdateHelper.ComputeFileMd5(localListPath);

                // 从服务器获取 server_info.json
                string jsonContent = await UpdateHelper.GetUrlContentAsync();

                // 反序列化 JSON
                var serverInfo = JsonSerializer.Deserialize<ServerInfo>(jsonContent);

                if (serverInfo == null) return;

                // 对比哈希值 (不区分大小写)
                if (!string.Equals(localHash, serverInfo.ListPlotCategoriesHash, StringComparison.OrdinalIgnoreCase))
                {
                    // 哈希不匹配，提示用户更新
                    bool confirmUpdate = await MessageHelper.ShowAsyncDialog(
                        LanguageService.Instance["new_drawing_template_library_version_detected"], // Reusing the same string or I should have added a new one? User said "same logic", so reusing "new version detected" is probably fine or generic enough.
                        LanguageService.Instance["Cancel"],
                        LanguageService.Instance["Confirm"]);

                    if (confirmUpdate)
                    {
                        await PerformCategoryListUpdate(serverInfo.ListPlotCategoriesHash);
                    }
                }
                else
                {
                    // 当前列表已是最新版本。
                    MessageHelper.Success(LanguageService.Instance["current_template_list_latest_version"]);
                }
            }
            catch (HttpRequestException netEx)
            {
                MessageHelper.Error(LanguageService.Instance["network_connection_failed_cannot_check_for_updates"] + $"{netEx.Message}");
            }
            catch (Exception ex)
            {
                MessageHelper.Error(LanguageService.Instance["error_occurred_while_checking_for_updates"] + $"{ex.Message}");
            }
        }

        /// <summary>
        /// 执行具体的类别列表更新逻辑
        /// </summary>
        /// <param name="expectedHash">从服务器 server_info.json 获取的期望哈希值</param>
        private async Task PerformCategoryListUpdate(string expectedHash = null)
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
                
                MessageHelper.Success(LanguageService.Instance["template_list_update_success_refreshing"]);

                // 发送消息通知 UI 重新加载配置
                WeakReferenceMessenger.Default.Send(new CategoryConfigUpdatedMessage("Updated"));
            }
            catch (Exception ex)
            {
                MessageHelper.Error(LanguageService.Instance["update_list_file_failed"] + $" {ex.Message}");
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
            // 取消高亮选择
            CancelSelected();

            // 进入添加箭头模式
            IsAddingArrow = true;
            _arrowStartPoint = null; // 重置起点

            // 关闭其他模式
            IsAddingLine = false;
            IsAddingPolygon = false;
            IsAddingText = false;

            //MessageHelper.Info("请在绘图区点击设置箭头的起点");
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
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void Undo()
        {
            if (_undoStack.TryPop(out var action))
            {
                action.Undo();
                _redoStack.Push(action);
                TrimStack(_redoStack);
                UndoCommand.NotifyCanExecuteChanged();
                RedoCommand.NotifyCanExecuteChanged();
                WpfPlot1.Refresh();
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
                MessageHelper.Warning(LanguageService.Instance["please_select_an_object_to_delete_first"]);
                return;
            }

            // 禁止删除坐标轴等基础图层
            if (SelectedLayers.Any(l => l is AxisLayerItemViewModel) || (_selectedLayer is AxisLayerItemViewModel))
            {
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
            // 存储每个被删图层的信息：(图层对象, ScottPlot对象, 父图层, 在父图层中的索引, 父图层是否因为空而被删, 父图层在根中的索引)
            var deletedInfos = new List<(LayerItemViewModel Layer, ScottPlot.IPlottable? Plottable, CategoryLayerItemViewModel? Parent, int Index, bool ParentRemoved, int ParentIndex)>();

            foreach (var layer in layersToDelete)
            {
                var parent = FindParentLayer(LayerTree, layer);
                int index = parent != null ? parent.Children.IndexOf(layer) : LayerTree.IndexOf(layer);
                
                bool parentRemoved = false;
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
        /// 清除所有导入的数据点
        /// </summary>
        private async Task ClearImportedDataAsync()
        {
            bool isConfirmed = await MessageHelper.ShowAsyncDialog(
                LanguageService.Instance["confirm_clear_data_points"],
                LanguageService.Instance["Cancel"],
                LanguageService.Instance["Confirm"]);

            if (isConfirmed)
            {
                // 在图层列表中查找名为 "数据点" 的根分类图层
                var dataRootNode = LayerTree.FirstOrDefault(node => node is CategoryLayerItemViewModel vm && vm.Name == LanguageService.Instance["data_point"]);

                // 如果没有找到该节点，说明没有导入数据
                if (dataRootNode == null)
                {
                    MessageHelper.Info(LanguageService.Instance["no_imported_data_to_clear"]);
                    return;
                }

                // 使用辅助方法 `FlattenTree` 获取 "数据点" 分类下的所有子图层
                var allDataLayers = FlattenTree(dataRootNode.Children).ToList();

                // 遍历所有找到的数据点图层，并从 ScottPlot 图表中移除它们对应的 Plottable 对象
                foreach (var layer in allDataLayers)
                {
                    if (layer.Plottable != null)
                    {
                        WpfPlot1.Plot.Remove(layer.Plottable);
                    }
                }

                // 从图层树的根集合中移除 "数据点" 这个顶级分类节点
                LayerTree.Remove(dataRootNode);

                // 检查属性面板当前是否正在显示某个已被删除的图层
                // 如果是，则清空属性面板，避免悬空引用
                if (_selectedLayer != null && (allDataLayers.Contains(_selectedLayer) || _selectedLayer == dataRootNode))
                {
                    PropertyGridModel = nullObject;
                    _selectedLayer = null;
                }

                // 刷新绘图控件以应用所有更改
                WpfPlot1.Refresh();
                MessageHelper.Success(LanguageService.Instance["all_imported_data_cleared"]);
            }
        }

        /// <summary>
        /// 将绘图控件的状态重置为默认值，以确保在加载新模板时有一个干净的环境。
        /// 清除所有绘图对象，并重置坐标轴、布局和所有特殊规则。
        /// </summary>
        private void ResetPlotStateToDefault()
        {
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
            CrosshairPlot.IsVisible = true;
            // 默认功能开关重置为开启
            IsCrosshairVisible = true;

            // 确保将全局变量重置为默认状态
            MainPlotViewModel.BaseMapType = String.Empty;
            MainPlotViewModel.Clockwise = true;

            // 清除图层树和属性面板的绑定
            LayerTree.Clear();
            PropertyGridModel = nullObject;
            _selectedLayer = null;

            // 清除数据表格
            _dataGrid.Worksheets[0].Reset();

            // 刷新一次以应用所有重置
            WpfPlot1.Refresh();
        }

        /// <summary>
        /// 执行核心的保存操作，将CurrentTemplate保存到指定路径
        /// </summary>
        /// <param name="filePath">要保存到的完整文件路径</param>
        private void PerformSave(string filePath)
        {
            // 清空模板中原有的动态绘图元素列表
            CurrentTemplate.Info.Lines.Clear();
            CurrentTemplate.Info.Texts.Clear();
            CurrentTemplate.Info.Annotations.Clear();
            CurrentTemplate.Info.Points.Clear();
            CurrentTemplate.Info.Polygons.Clear();
            CurrentTemplate.Info.Arrows.Clear();

            // 遍历当前的图层列表，收集所有图元信息
            var allLayers = FlattenTree(LayerTree);
            foreach (var layer in allLayers)
            {
                switch (layer)
                {
                    case LineLayerItemViewModel lineLayer:
                        CurrentTemplate.Info.Lines.Add(lineLayer.LineDefinition);
                        break;
                    case TextLayerItemViewModel textLayer:
                        CurrentTemplate.Info.Texts.Add(textLayer.TextDefinition);
                        break;
                    case AnnotationLayerItemViewModel annotationLayer:
                        CurrentTemplate.Info.Annotations.Add(annotationLayer.AnnotationDefinition);
                        break;
                    case ArrowLayerItemViewModel arrowLayer:
                        CurrentTemplate.Info.Arrows.Add(arrowLayer.ArrowDefinition);
                        break;
                    case PolygonLayerItemViewModel polygonLayer:
                        CurrentTemplate.Info.Polygons.Add(polygonLayer.PolygonDefinition);
                        break;
                        // TODO:其他图元处理
                }
            }

            // 将更新后的 CurrentTemplate 对象序列化为 JSON 字符串
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };

            try
            {
                string jsonString = JsonSerializer.Serialize(CurrentTemplate, options);

                // 将 JSON 字符串写入文件
                string directoryPath = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                File.WriteAllText(filePath, jsonString);

                // 更新或生成新的缩略图
                string thumbnailPath = Path.Combine(directoryPath, "thumbnail.jpg");
                WpfPlot1.Plot.Save(thumbnailPath, 400, 300);

                // 同步信息到 GraphMapCustomList.json
                string appPath = FileHelper.GetAppPath();
                string customRoot = Path.Combine(appPath, "Data", "PlotData", "Custom");
                
                // 检查是否在 Custom 目录下
                if (filePath.StartsWith(customRoot, StringComparison.OrdinalIgnoreCase))
                {
                    // 获取模板文件夹名（假设结构为 Custom/TemplateName/TemplateName.json）
                    string relativePath = Path.GetRelativePath(customRoot, filePath);
                    string[] parts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    if (parts.Length >= 2)
                    {
                        // 取第一层目录名作为 GraphMapPath (即 A_B 文件夹)
                        string graphMapPath = parts[0];
                        
                        string listPath = Path.Combine(appPath, "Data", "PlotData", "GraphMapCustomList.json");
                        string fileHash = UpdateHelper.ComputeFileMd5(filePath);
                        
                        GraphMapTemplateParser.AddCustomTemplateEntry(listPath, CurrentTemplate.NodeList, graphMapPath, fileHash);
                    }
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
        public static (double,double) ToTernary(double x, double y, bool clockwise)
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

        /// <summary>
        /// 将三元坐标转换为二维笛卡尔坐标系
        /// </summary>
        /// <param name="bottomFraction">三元图底部坐标轴</param>
        /// <param name="leftFraction">三元图左侧坐标轴</param>
        /// <param name="rightFraction">三元图右侧坐标轴</param>
        /// <returns>转换后的二维笛卡尔坐标系</returns>
        /// <exception cref="ArgumentException"></exception>
        public static (double,double) ToCartesian(double bottomFraction, double leftFraction, double rightFraction)
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

            string tempFilePath = FileHelper.GetSaveFilePath2(title: "保存为csv文件", filter: "CSV文件|*.csv",
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
        /// 下载单个模板的具体实现
        /// </summary>
        private async Task DownloadSingleTemplate(TemplateCardViewModel card)
        {
            try
            {
                // 切换状态
                card.State = TemplateState.Downloading;
                card.DownloadProgress = 0;

                // 准备路径
                string zipUrl = $"https://geochemistrynexus-1303234197.cos.ap-hongkong.myqcloud.com/Templates/{card.TemplatePath}.zip";
                string tempZipPath = Path.Combine(Path.GetTempPath(), $"{card.TemplatePath}_{Guid.NewGuid()}.zip");
                string targetDir = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "Default", card.TemplatePath);

                // 定义进度回调
                var progressIndicator = new Progress<double>(p => card.DownloadProgress = p);

                // 开始下载
                await UpdateHelper.DownloadFileAsync(zipUrl, tempZipPath, progressIndicator);

                // 解压处理
                await Task.Run(() =>
                {
                    // 如果目标文件夹存在，覆盖更新
                    if (Directory.Exists(targetDir))
                    {
                        Directory.Delete(targetDir, true);
                    }
                    Directory.CreateDirectory(targetDir);

                    // 解压
                    System.IO.Compression.ZipFile.ExtractToDirectory(tempZipPath, targetDir);
                });

                // 清理临时文件
                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);

                // 更新卡片状态
                card.State = TemplateState.Ready;

                // 更新缩略图为下载下来的真实图片
                string newThumbPath = Path.Combine(targetDir, "thumbnail.jpg");

                card.ThumbnailImage = FileHelper.LoadBitmapNoLock(newThumbPath);

                // 更新模板成果
                MessageHelper.Success($"{card.Name} "+ LanguageService.Instance["template_ready"]);
            }
            catch (Exception ex)
            {
                card.State = TemplateState.Error;
                // 更新模板失败
                MessageHelper.Error(LanguageService.Instance["download_template_failed"] + $" {ex.Message}");
            }
        }


    }
}
