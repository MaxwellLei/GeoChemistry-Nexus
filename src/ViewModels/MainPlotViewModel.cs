using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Controls;
using GeoChemistryNexus.Helpers;
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
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using unvell.ReoGrid;

namespace GeoChemistryNexus.ViewModels
{
    public partial class MainPlotViewModel : ObservableObject
    {

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
        private bool _isNewTemplateMode = false; // 新建绘图遮罩

        // 卡片展示用的模板集合
        [ObservableProperty]
        private ObservableCollection<TemplateCardViewModel> _templateCards = new();

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

        // 追踪当前在TreeView中被选中的图层ViewModel
        private LayerItemViewModel _selectedLayer;

        // 空属性编辑对象占位
        private object nullObject = new object();

        // 当前点击的模板列表节点
        private GraphMapTemplateNode currentgraphMapTemplateNode;

        // 选项卡index
        [ObservableProperty]
        private int tabIndex = 0;

        // Ribbon 选项卡 Index
        [ObservableProperty]
        private int ribbonTabIndex = 0;

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
        private ScottPlot.Plottables.LinePlot? _tempRubberBandLine; // 用于预览下一段连线的"橡皮筋"

        // 用于存储当前正在编辑的模板的完整文件路径
        private string _currentTemplateFilePath;



        // 测试
        [ObservableProperty]
        private bool _isAddingArrow = false; // 标记是否处于添加箭头模式
        private Coordinates? _arrowStartPoint = null; // 存储箭头的起点
        private ScottPlot.Plottables.Arrow? _tempArrowPlot; // 用于实时预览箭头


        // 初始化
        public MainPlotViewModel(WpfPlot wpfPlot, System.Windows.Controls.RichTextBox richTextBox, unvell.ReoGrid.ReoGridControl dataGrid)
        {
            // 获取模板列表
            GraphMapTemplateNode = GraphMapTemplateParser.Parse(
                JsonHelper.ReadJsonFile(
                Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "GraphMapList.json")));

            WpfPlot1 = wpfPlot;      // 获取绘图控件
            _richTextBox = richTextBox;      // 富文本框
            _dataGrid = dataGrid;        // 获取数据表格控件
            IsSnapSelectionEnabled = true;  // 吸附选择开启

            InitializeBreadcrumbs(); // 初始化面包屑
            LoadAllTemplateCards();  // 加载所有模板卡片

            // 初始化十字轴并设置样式
            CrosshairPlot = WpfPlot1.Plot.Add.Crosshair(0, 0);
            CrosshairPlot.IsVisible = true;
            CrosshairPlot.TextColor = ScottPlot.Colors.White;
            CrosshairPlot.TextBackgroundColor = CrosshairPlot.HorizontalLine.Color;

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

            // 1. 从剪贴板获取将要粘贴的文本数据
            if (!Clipboard.ContainsText()) return;
            string pasteText = Clipboard.GetText();
            if (string.IsNullOrEmpty(pasteText)) return;

            // 2. 计算粘贴文本包含的行数
            var lines = pasteText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int pastedRowCount = lines.Length;

            // 如果粘贴的文本以换行符结尾，Split会产生一个额外的空数组元素，需要排除掉
            if (string.IsNullOrEmpty(lines.Last()))
            {
                pastedRowCount--;
            }

            if (pastedRowCount <= 0) return;

            // 3. 获取粘贴操作的目标起始行
            int startRow = worksheet.SelectionRange.Row;

            // 4. 计算粘贴完成后所需要的总行数
            int requiredTotalRows = startRow + pastedRowCount;

            // 5. 如果需要的总行数大于当前表格的总行数，则扩展表格
            if (requiredTotalRows > worksheet.RowCount)
            {
                // 动态设置工作表的总行数
                worksheet.RowCount = requiredTotalRows;
            }
        }

        /// <summary>
        /// 根据 CartesianAxisDefinition 对象中的设置，更新实际坐标轴的范围
        /// </summary>
        private void UpdateAxisLimitsFromModel(CartesianAxisDefinition axisDef, IAxis targetAxis)
        {
            // 如果用户在属性面板清空了最大值和最小值（变为NaN），则执行自动缩放
            if (double.IsNaN(axisDef.Minimum) && double.IsNaN(axisDef.Maximum))
            {
                WpfPlot1.Plot.Axes.AutoScale();
                return;
            }

            var currentLimits = targetAxis.Range;

            // 优先使用模型中的值，如果模型值为NaN，则保持坐标轴当前的值
            var newMin = !double.IsNaN(axisDef.Minimum) ? axisDef.Minimum : currentLimits.Min;
            var newMax = !double.IsNaN(axisDef.Maximum) ? axisDef.Maximum : currentLimits.Max;

            // 如果是对数坐标轴，对范围值取对数
            if (axisDef.ScaleType == AxisScaleType.Logarithmic)
            {
                if (!double.IsNaN(axisDef.Minimum))
                {
                    // 对数轴的最小值必须大于0
                    newMin = axisDef.Minimum > 0 ? Math.Log10(axisDef.Minimum) : currentLimits.Min;
                }
                if (!double.IsNaN(axisDef.Maximum))
                {
                    // 对数轴的最大值必须大于0
                    newMax = axisDef.Maximum > 0 ? Math.Log10(axisDef.Maximum) : currentLimits.Max;
                }
            }

            // 设置最终的坐标轴范围
            targetAxis.Range.Set(newMin, newMax);
        }

        /// <summary>
        /// 将图层恢复到其在当前选择状态下应有的样式
        /// 先恢复原始样式，再根据情况应用遮罩
        /// </summary>
        private void RestoreLayerToCorrectState(LayerItemViewModel layerToRestore)
        {
            if (layerToRestore?.Plottable == null)
                return;

            // 先将图层恢复到其最原始的定义样式
            RevertLayerStyle(layerToRestore);

            // 判断是否需要应用遮罩
            if (_selectedLayer != null && layerToRestore != _selectedLayer)
            {
                // 应用遮罩
                DimLayer(layerToRestore);
            }
        }

        /// <summary>
        /// 高亮显示指定的图层
        /// </summary>
        private void HighlightLayer(LayerItemViewModel layer)
        {
            if (layer?.Plottable == null) return;

            // 定义高亮样式
            var highlightColor = ScottPlot.Colors.Red;
            float highlightWidthIncrease = 2;

            switch (layer.Plottable)
            {
                case ScottPlot.Plottables.LinePlot linePlot:
                    linePlot.Color = highlightColor;
                    linePlot.LineWidth += highlightWidthIncrease;
                    break;

                case ScottPlot.Plottables.Text textPlot:
                    textPlot.LabelBorderColor = highlightColor;
                    textPlot.LabelBorderWidth = 2;
                    break;

                case ScottPlot.Plottables.Scatter scatterPlot:
                    scatterPlot.MarkerStyle.OutlineColor = highlightColor;
                    scatterPlot.MarkerStyle.OutlineWidth = highlightWidthIncrease;
                    break;

                case ScottPlot.Plottables.Arrow arrowPlot:
                    arrowPlot.ArrowFillColor = highlightColor;
                    arrowPlot.ArrowWidth += highlightWidthIncrease;
                    break;

                case ScottPlot.Plottables.Polygon polygonPlot:
                    polygonPlot.LineStyle.Color = highlightColor;
                    polygonPlot.LineStyle.Width += highlightWidthIncrease;
                    break;
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
                worksheet.AutoFitColumnWidth(i, true);
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

            // 处理吸附选择的点击逻辑
            if (IsSnapSelectionEnabled && _lastHoveredLayer != null)
            {
                // 如果当前有悬浮高亮的图层，则执行选中操作
                if (SelectLayerCommand.CanExecute(_lastHoveredLayer))
                {
                    SelectLayerCommand.Execute(_lastHoveredLayer);
                }
                // 选中后，直接返回，不再执行后续的添加点/线/多边形等操作
                return;
            }

            var mousePos = e.GetPosition(WpfPlot1);
            Coordinates mouseCoordinates = WpfPlot1.Plot.GetCoordinates(new Pixel(mousePos.X * WpfPlot1.DisplayScale, mousePos.Y * WpfPlot1.DisplayScale));

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
                    //MessageHelper.Info("起点已确认，请点击设置终点");
                }
                else
                {
                    // 第二次点击：设置终点，正式创建线条
                    var startPoint = _lineStartPoint.Value;
                    var endPoint = mouseCoordinates;

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
                        Start = new PointDefinition { X = startPoint.X, Y = startPoint.Y },
                        End = new PointDefinition { X = endPoint.X, Y = endPoint.Y },
                        // 设置默认样式
                        Color = "#0078D4", // 默认蓝色
                        Width = 2,
                        Style = LineDefinition.LineType.Solid
                    };

                    // 创建新的 LineLayerItemViewModel
                    var lineLayer = new LineLayerItemViewModel(newLineDef, linesCategory.Children.Count);
                    // 订阅可见性变化事件，以便在图层列表中勾选/取消勾选时刷新视图
                    lineLayer.PropertyChanged += (s, ev) => { if (ev.PropertyName == nameof(LineLayerItemViewModel.IsVisible)) RefreshPlotFromLayers(); };

                    // 将新图层添加到图层树
                    linesCategory.Children.Add(lineLayer);

                    // 刷新整个绘图（这将根据图层树重新绘制所有内容，包括新添加的线条）
                    RefreshPlotFromLayers();

                    // 重置画线状态
                    if (_tempLinePlot != null)
                    {
                        WpfPlot1.Plot.Remove(_tempLinePlot);
                        _tempLinePlot = null;
                    }
                    _lineStartPoint = null;
                    IsAddingLine = false;
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


                // 创建新的 TextDefinition 对象来存储文本的属性
                var newTextDef = new TextDefinition
                {
                    Content = contentString, // 使用动态生成的对象
                    StartAndEnd = new PointDefinition { X = mouseCoordinates.X, Y = mouseCoordinates.Y },
                    // 设置默认样式
                    Color = "#FF000000",
                    Size = 12,
                    Family = "Microsoft YaHei",
                    BackgroundColor = "#00FFFFFF",
                    BorderColor = "#00FFFFFF"
                };

                // 创建新的 TextLayerItemViewModel
                var textLayer = new TextLayerItemViewModel(newTextDef, textCategory.Children.Count);
                textLayer.PropertyChanged += (s, ev) => { if (ev.PropertyName == nameof(TextLayerItemViewModel.IsVisible)) RefreshPlotFromLayers(); };
                textCategory.Children.Add(textLayer);

                RefreshPlotFromLayers();
                IsAddingText = false;

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
                    var startPoint = _arrowStartPoint.Value;
                    var endPoint = mouseCoordinates;

                    // 如果是三元图，需要将笛卡尔坐标转换为三元坐标进行存储
                    if (BaseMapType == "Ternary")
                    {
                        // Clockwise 参数需要根据您的三元图设置来确定
                        var ternaryStart = ToTernary(startPoint.X, startPoint.Y, Clockwise);
                        var ternaryEnd = ToTernary(endPoint.X, endPoint.Y, Clockwise);

                        // 将转换后的三元坐标（底轴和左轴的分量）存入PointDefinition
                        startPoint = new Coordinates(ternaryStart.Item1, ternaryStart.Item2);
                        endPoint = new Coordinates(ternaryEnd.Item1, ternaryEnd.Item2);
                    }

                    var arrowsCategory = LayerTree.FirstOrDefault(c => c is CategoryLayerItemViewModel vm && vm.Name == LanguageService.Instance["arrow"]) as CategoryLayerItemViewModel;
                    if (arrowsCategory == null)
                    {
                        arrowsCategory = new CategoryLayerItemViewModel(LanguageService.Instance["arrow"]);
                        LayerTree.Add(arrowsCategory);
                    }

                    var newArrowDef = new ArrowDefinition
                    {
                        Start = new PointDefinition { X = startPoint.X, Y = startPoint.Y },
                        End = new PointDefinition { X = endPoint.X, Y = endPoint.Y },
                        Color = "#000000",
                        ArrowWidth = 1.5f,
                        ArrowheadWidth = 18f,
                        ArrowheadLength = 18f
                    };

                    var arrowLayer = new ArrowLayerItemViewModel(newArrowDef, arrowsCategory.Children.Count);
                    arrowLayer.PropertyChanged += (s, ev) => { if (ev.PropertyName == nameof(ArrowLayerItemViewModel.IsVisible)) RefreshPlotFromLayers(); };
                    arrowsCategory.Children.Add(arrowLayer);

                    RefreshPlotFromLayers();

                    // Reset arrow drawing state
                    if (_tempArrowPlot != null)
                    {
                        WpfPlot1.Plot.Remove(_tempArrowPlot);
                        _tempArrowPlot = null;
                    }
                    _arrowStartPoint = null;
                    IsAddingArrow = false;
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
                    IsAddingPolygon = false;
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

                // 创建 PolygonDefinition 用于存储属性
                var newPolygonDef = new PolygonDefinition
                {
                    Vertices = new ObservableCollection<PointDefinition>(_polygonVertices.Select(c => new PointDefinition { X = c.X, Y = c.Y })),
                    // 使用默认样式
                };

                // 创建 PolygonLayerItemViewModel
                var polygonLayer = new PolygonLayerItemViewModel(newPolygonDef, polygonsCategory.Children.Count);
                polygonLayer.PropertyChanged += (s, ev) => { if (ev.PropertyName == nameof(PolygonLayerItemViewModel.IsVisible)) RefreshPlotFromLayers(); };
                polygonsCategory.Children.Add(polygonLayer);

                // 刷新整个绘图
                RefreshPlotFromLayers();

                // 重置状态
                IsAddingPolygon = false;
                _polygonVertices.Clear();
                WpfPlot1.Refresh();
                //MessageHelper.Success("多边形添加成功！");
                return;
            }

            if (IsAddingLine)
            {
                // 如果正在添加线条，右键点击则取消操作
                if (_tempLinePlot != null)
                {
                    WpfPlot1.Plot.Remove(_tempLinePlot);
                    _tempLinePlot = null;
                }
                _lineStartPoint = null;
                IsAddingLine = false;
                WpfPlot1.Refresh();
                MessageHelper.Info(LanguageService.Instance["add_line_operation_canceled"]);
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
                if (_tempArrowPlot != null)
                {
                    WpfPlot1.Plot.Remove(_tempArrowPlot);
                    _tempArrowPlot = null;
                }
                _arrowStartPoint = null;
                IsAddingArrow = false;
                WpfPlot1.Refresh();
                MessageHelper.Info(LanguageService.Instance["add_arrow_operation_canceled"]);
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

            //MessageHelper.Info("请在绘图区连续左键点击设置顶点，右键完成绘制。");
        }

        // 创建多边形绘图对象
        private void CreatePolygonPlottable(PolygonLayerItemViewModel polygonLayer)
        {
            var polygonDef = polygonLayer.PolygonDefinition;
            if (polygonDef.Vertices == null || !polygonDef.Vertices.Any()) return;

            var coordinates = polygonDef.Vertices.Select(p => new Coordinates(p.X, p.Y)).ToArray();
            var polygonPlot = WpfPlot1.Plot.Add.Polygon(coordinates);

            // 应用样式
            polygonPlot.FillStyle.Color = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(polygonDef.FillColor));
            polygonPlot.LineStyle.Color = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(polygonDef.BorderColor));
            polygonPlot.LineStyle.Width = polygonDef.BorderWidth;

            // 存储引用
            polygonLayer.Plottable = polygonPlot;
        }

        // 多边形属性变更的处理器
        private bool PolygonPropertyChanged(ScottPlot.Plottables.Polygon polygonPlot, object sender, PropertyChangedEventArgs e)
        {
            var polygonDef = (PolygonDefinition)sender;
            switch (e.PropertyName)
            {
                case nameof(PolygonDefinition.Vertices):
                    var newCoordinates = polygonDef.Vertices.Select(p => new Coordinates(p.X, p.Y)).ToArray();
                    polygonPlot.UpdateCoordinates(newCoordinates);
                    break;
                case nameof(PolygonDefinition.FillColor):
                    polygonPlot.FillStyle.Color = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(polygonDef.FillColor));
                    break;
                case nameof(PolygonDefinition.BorderColor):
                    polygonPlot.LineStyle.Color = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(polygonDef.BorderColor));
                    break;
                case nameof(PolygonDefinition.BorderWidth):
                    polygonPlot.LineStyle.Width = polygonDef.BorderWidth;
                    break;
                default:
                    return false;
            }
            return true;
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
            //MessageHelper.Info("请在绘图区点击以设置线条的起点");
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
            //MessageHelper.Info("请在绘图区点击以设置文本的位置");
        }

        // 切换语言——刷新
        public void InitTemplate()
        {
            // 刷新模板列表
            GraphMapTemplateNode = GraphMapTemplateParser.Parse(
                JsonHelper.ReadJsonFile(
                Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "GraphMapList.json")));
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

            // 处理吸附选择
            if (IsSnapSelectionEnabled)
            {
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
                WpfPlot1.Cursor = Cursors.Arrow;
            }

            //  如果正在添加线条且起点已确定，则实时预览线条
            if (IsAddingLine && _lineStartPoint.HasValue)
            {
                // 如果已存在临时预览线，先将其移除
                if (_tempLinePlot != null)
                {
                    WpfPlot1.Plot.Remove(_tempLinePlot);
                }
                // 添加新的临时预览线
                _tempLinePlot = WpfPlot1.Plot.Add.Line(_lineStartPoint.Value.X, _lineStartPoint.Value.Y, mouseCoordinates.X, mouseCoordinates.Y);
                _tempLinePlot.Color = Colors.Red; // 设置预览线为红色虚线
                _tempLinePlot.LinePattern = LinePattern.Dashed;
                WpfPlot1.Refresh();
                return;
            }

            // 实时预览箭头
            if (IsAddingArrow && _arrowStartPoint.HasValue)
            {
                if (_tempArrowPlot != null)
                {
                    WpfPlot1.Plot.Remove(_tempArrowPlot);
                }
                _tempArrowPlot = WpfPlot1.Plot.Add.Arrow(_arrowStartPoint.Value, mouseCoordinates);
                _tempArrowPlot.ArrowFillColor = Colors.Red;
                WpfPlot1.Refresh();
                return;
            }

            // 处理添加多边形时的鼠标移动预览
            if (IsAddingPolygon && _polygonVertices.Any())
            {
                // 如果已存在临时预览线，先将其移除
                if (_tempRubberBandLine != null)
                {
                    WpfPlot1.Plot.Remove(_tempRubberBandLine);
                }
                // 从最后一个顶点到当前鼠标位置画一根"橡皮筋"线
                var lastVertex = _polygonVertices.Last();
                _tempRubberBandLine = WpfPlot1.Plot.Add.Line(lastVertex, mouseCoordinates);
                _tempRubberBandLine.Color = Colors.Red;
                _tempRubberBandLine.LinePattern = LinePattern.Dashed;
                WpfPlot1.Refresh();
            }

            // 如果追踪模式未开启，则不执行任何操作
            if (!IsCrosshairVisible) return;

            // 更新十字轴的位置
            CrosshairPlot.Position = mouseCoordinates;

            // 默认情况下，显示的值就是鼠标的坐标值
            double xValueToDisplay = mouseCoordinates.X;
            double yValueToDisplay = mouseCoordinates.Y;

            // 查找当前主X轴（通常是Bottom）和主Y轴（通常是Left）的定义
            var xAxisDef = CurrentTemplate?.Info?.Axes
                .OfType<CartesianAxisDefinition>()
                .FirstOrDefault(ax => ax.Type == "Bottom");

            var yAxisDef = CurrentTemplate?.Info?.Axes
                .OfType<CartesianAxisDefinition>()
                .FirstOrDefault(ax => ax.Type == "Left");

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

            // 更新十字轴上的文本标签以显示实时坐标 (保留3位小数的数字)
            CrosshairPlot.VerticalLine.Text = $"{xValueToDisplay:N3}";
            CrosshairPlot.HorizontalLine.Text = $"{yValueToDisplay:N3}";

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
        /// 递归收集节点下的所有模板
        /// </summary>
        private void CollectTemplatesFromNode(GraphMapTemplateNode node)
        {
            if (node?.Children == null) return;

            foreach (var child in node.Children)
            {
                if (!string.IsNullOrEmpty(child.GraphMapPath))
                {
                    var thumbnailPath = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "Default"
                                                    , child.GraphMapPath, "thumbnail.jpg");

                    if (File.Exists(thumbnailPath))
                    {
                        TemplateCards.Add(new TemplateCardViewModel
                        {
                            Name = child.Name,
                            TemplatePath = child.GraphMapPath,
                            ThumbnailPath = thumbnailPath,
                            Category = GetNodePath(child)
                        });
                    }
                }
                else
                {
                    // 递归处理子分类
                    CollectTemplatesFromNode(child);
                }
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

            var thumbnailPath = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "Default"
                                            , templateNode.GraphMapPath, "thumbnail.jpg");
            if (File.Exists(thumbnailPath))
            {
                TemplateCards.Add(new TemplateCardViewModel
                {
                    Name = templateNode.Name,
                    TemplatePath = templateNode.GraphMapPath,
                    ThumbnailPath = thumbnailPath,
                    Category = GetNodePath(templateNode)
                });
            }

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
            TabIndex = 0;

            // 切换到绘图模式
            IsTemplateMode = false;
            IsPlotMode = true;

            // 加载模板文件
            var templateFilePath = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "Default"
                                            , card.TemplatePath, $"{card.TemplatePath}.json");
            await LoadAndBuildLayers(templateFilePath);

            // 加载底图模板的说明文件
            var tempRTFfile = FileHelper.FindFileOrGetFirstWithExtension(
                    Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "Default",
                    card.TemplatePath), LanguageService.CurrentLanguage,".rtf");
            RtfHelper.LoadRtfToRichTextBox(tempRTFfile, _richTextBox);

            // 加载数据表格控件
            PrepareDataGridForInput();
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
        /// 箭头对象属性更新，触发
        /// </summary>
        private bool ArrowPropertyChanged(ScottPlot.Plottables.Arrow arrowPlot, object sender, PropertyChangedEventArgs e)
        {
            var arrowDef = (ArrowDefinition)sender;
            switch (e.PropertyName)
            {
                case nameof(ArrowDefinition.Start):
                    arrowPlot.Base = new Coordinates(arrowDef.Start.X, arrowDef.Start.Y);
                    break;
                case nameof(ArrowDefinition.End):
                    arrowPlot.Tip = new Coordinates(arrowDef.End.X, arrowDef.End.Y);
                    break;
                case nameof(ArrowDefinition.Color):
                    arrowPlot.ArrowFillColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(arrowDef.Color));
                    break;
                case nameof(ArrowDefinition.ArrowWidth):
                    arrowPlot.ArrowWidth = arrowDef.ArrowWidth;
                    break;
                case nameof(ArrowDefinition.ArrowheadWidth):
                    arrowPlot.ArrowheadWidth = arrowDef.ArrowheadWidth;
                    break;
                case nameof(ArrowDefinition.ArrowheadLength):
                    arrowPlot.ArrowheadLength = arrowDef.ArrowheadLength;
                    break;
                default:
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 当属性面板中的值改变时，此方法被调用实现更新
        /// </summary>
        private void PropertyGridModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender == null) return;

            bool needsRefresh = true;

            // 坐标轴单独处理
            if (sender is BaseAxisDefinition baseAxisDef)
            {
                needsRefresh = AxisPropertyChanged(baseAxisDef, e);
            }

            // 网格单独处理
            if (sender is GridDefinition gridDef)
            {
                needsRefresh = GridPropertyChanged(gridDef, e);
            }

            // 图例单独处理
            if (sender is LegendDefinition legendDef)
            {
                needsRefresh = LegendPropertyChanged(legendDef, e);
            }

            // 标题单独处理
            if (sender is TitleDefinition titleDef)
            {
                needsRefresh = TitlePropertyChanged(titleDef, e);
            }

            // 刷新 UI
            if (needsRefresh)
            {
                WpfPlot1.Refresh();
            }

            var targetLayer = _selectedLayer;

            // 如果图层当前不可见或未找到，则不进行任何操作
            if (targetLayer?.Plottable == null) return;

            // 处理其他 Plottable 对象的逻辑
            if (targetLayer.Plottable != null)
            {
                switch (targetLayer.Plottable)
                {
                    // 线条
                    case ScottPlot.Plottables.LinePlot linePlot:
                        needsRefresh = LinePropertyChanged(linePlot, sender, e);
                        break;

                    // 文本
                    case ScottPlot.Plottables.Text textPlot:
                        needsRefresh = TextPropertyChanged(textPlot, sender, e);
                        break;

                    // 箭头
                    case ScottPlot.Plottables.Arrow arrowPlot:
                        needsRefresh = ArrowPropertyChanged(arrowPlot, sender, e);
                        break;

                    // 多边形
                    case ScottPlot.Plottables.Polygon polygonPlot:
                        needsRefresh = PolygonPropertyChanged(polygonPlot, sender, e);
                        break;

                    // 数据点
                    case ScottPlot.Plottables.Scatter scatterPlot:
                        needsRefresh = ScatterPropertyChanged(scatterPlot, sender, e);
                        break;
                }
            }

            // 如果有必要，仅刷新一次UI
            if (needsRefresh)
            {
                WpfPlot1.Refresh();
            }
        }

        /// <summary>
        /// 数据点对象属性更新，触发
        /// </summary>
        /// <param name="scatterPlot"></param>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool ScatterPropertyChanged(ScottPlot.Plottables.Scatter scatterPlot, object sender,
            PropertyChangedEventArgs e)
        {
            var scatterDef = (ScatterDefinition)sender;
            switch (e.PropertyName)
            {
                // 更新散点大小
                case nameof(ScatterDefinition.StartAndEnd):
                    MessageHelper.Info(LanguageService.Instance["disallow_modify_data_point_position"]);
                    break;

                // 更新散点大小
                case nameof(ScatterDefinition.Size):
                    scatterPlot.MarkerSize = scatterDef.Size;
                    break;

                // 更新散点颜色
                case nameof(ScatterDefinition.Color):
                    scatterPlot.Color = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(scatterDef.Color));
                    break;

                // 更新散点标记的形状
                case nameof(ScatterDefinition.MarkerShape):
                    scatterPlot.MarkerShape = scatterDef.MarkerShape;
                    break;
                default:
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 标题对象属性
        /// </summary>
        /// <param name="titleDef">标题对象</param>
        /// <param name="e"></param>
        /// <returns>是否刷新视图</returns>
        private bool TitlePropertyChanged(TitleDefinition titleDef, PropertyChangedEventArgs e)
        {
            // 根据变化的属性名更新坐标轴
            switch (e.PropertyName)
            {
                // 图表标题内容
                case nameof(titleDef.Label):
                    WpfPlot1.Plot.Axes.Title.Label.Text = titleDef.Label.Get();
                    break;
                // 图表标题字体
                case nameof(titleDef.Family):
                    WpfPlot1.Plot.Axes.Title.Label.FontName = titleDef.Family;
                    break;
                // 图表标题字体大小
                case nameof(titleDef.Size):
                    WpfPlot1.Plot.Axes.Title.Label.FontSize = titleDef.Size;
                    break;
                // 图表标题字体颜色
                case nameof(titleDef.Color):
                    WpfPlot1.Plot.Axes.Title.Label.ForeColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(titleDef.Color));
                    break;
                // 图表标题粗体
                case nameof(titleDef.IsBold):
                    WpfPlot1.Plot.Axes.Title.Label.Bold = titleDef.IsBold;
                    break;
                // 图表标题斜体
                case nameof(titleDef.IsItalic):
                    WpfPlot1.Plot.Axes.Title.Label.Italic = titleDef.IsItalic;
                    break;

                default:
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 文本对象属性更新，触发
        /// </summary>
        /// <param name="textPlot">文本对象</param>
        /// <param name="sender"></param>
        private bool TextPropertyChanged(ScottPlot.Plottables.Text textPlot, object sender,
            PropertyChangedEventArgs e)
        {
            var textDef = (TextDefinition)sender;
            switch (e.PropertyName)
            {
                // =================================================
                //                           内容与位置
                // =================================================
                // 文本位置
                case nameof(TextDefinition.StartAndEnd):
                    textPlot.Location = new Coordinates(textDef.StartAndEnd.X, textDef.StartAndEnd.Y);
                    break;
                // 文本内容
                case nameof(TextDefinition.Content):
                    textPlot.LabelText = textDef.Content.Get();
                    break;
                // 水平对齐方式
                case nameof(TextDefinition.ContentHorizontalAlignment):
                    switch (textDef.ContentHorizontalAlignment)
                    {
                        case System.Windows.HorizontalAlignment.Left:
                            textPlot.LabelAlignment = ScottPlot.Alignment.LowerRight;
                            break;
                        case System.Windows.HorizontalAlignment.Center:
                            textPlot.LabelAlignment = ScottPlot.Alignment.LowerCenter;
                            break;
                        case System.Windows.HorizontalAlignment.Right:
                            textPlot.LabelAlignment = ScottPlot.Alignment.LowerLeft;
                            break;
                        case System.Windows.HorizontalAlignment.Stretch:
                        default:
                            textPlot.LabelAlignment = ScottPlot.Alignment.MiddleCenter;
                            break;
                    }
                    break;

                // =================================================
                //                           字体样式
                // =================================================
                // 文本字体
                case nameof(TextDefinition.Family):
                    textPlot.LabelFontName = textDef.Family;
                    break;
                // 文本字体大小
                case nameof(TextDefinition.Size):
                    textPlot.LabelFontSize = textDef.Size;
                    break;
                // 文本旋转角度
                case nameof(TextDefinition.Rotation):
                    textPlot.LabelRotation = textDef.Rotation;
                    break;
                // 文本字体颜色
                case nameof(TextDefinition.Color):
                    textPlot.LabelFontColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(textDef.Color));
                    break;
                // 文本斜体
                case nameof(TextDefinition.IsItalic):
                    textPlot.LabelItalic = textDef.IsItalic;
                    break;
                // 文本粗体
                case nameof(TextDefinition.IsBold):
                    textPlot.LabelBold = textDef.IsBold;
                    break;

                // =================================================
                //                           背景与边框
                // =================================================
                // 背景颜色
                case nameof(TextDefinition.BackgroundColor):
                    textPlot.LabelBackgroundColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(textDef.BackgroundColor));
                    break;
                // 边框颜色
                case nameof(TextDefinition.BorderColor):
                    textPlot.LabelBorderColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(textDef.BorderColor));
                    break;
                // 边框宽度
                case nameof(TextDefinition.BorderWidth):
                    textPlot.LabelBorderWidth = textDef.BorderWidth;
                    break;
                // 圆角半径
                case nameof(TextDefinition.FilletRadius):
                    textPlot.LabelBorderRadius = textDef.FilletRadius;
                    break;

                // =================================================
                //                           高级渲染
                // =================================================
                // 抗锯齿
                case nameof(TextDefinition.AntiAliasEnable):
                    textPlot.LabelStyle.AntiAliasText = textDef.AntiAliasEnable;
                    break;
                default:
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 线条对象属性更新，触发
        /// </summary>
        /// <param name="linePlot">线条对象</param>
        /// <param name="sender"></param>
        private bool LinePropertyChanged(ScottPlot.Plottables.LinePlot linePlot, object sender,
            PropertyChangedEventArgs e)
        {
            var lineDef = (LineDefinition)sender;
            switch (e.PropertyName)
            {
                case nameof(LineDefinition.Color):
                    linePlot.Color = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(lineDef.Color));
                    break;
                case nameof(LineDefinition.Width):
                    linePlot.LineWidth = lineDef.Width;
                    break;
                case nameof(LineDefinition.Style):
                    linePlot.LinePattern = GraphMapTemplateParser.GetLinePattern(lineDef.Style.ToString());
                    break;
                case nameof(LineDefinition.Start):
                    linePlot.Line = new CoordinateLine(lineDef.Start.X, lineDef.Start.Y, lineDef.End.X, lineDef.End.Y);
                    break;
                case nameof(LineDefinition.End):
                    linePlot.Line = new CoordinateLine(lineDef.Start.X, lineDef.Start.Y, lineDef.End.X, lineDef.End.Y);
                    break;
                default:
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 坐标轴对象属性更新，触发
        /// </summary>
        /// <param name="axisDef"></param>
        private bool GridPropertyChanged(GridDefinition gridDef, PropertyChangedEventArgs e)
        {
            // 根据变化的属性名更新坐标轴
            switch (e.PropertyName)
            {
                // =================================================
                //                           主网格线
                // =================================================
                case nameof(GridDefinition.MajorGridLineIsVisible):
                    WpfPlot1.Plot.Grid.XAxisStyle.MajorLineStyle.IsVisible = gridDef.MajorGridLineIsVisible;
                    WpfPlot1.Plot.Grid.YAxisStyle.MajorLineStyle.IsVisible = gridDef.MajorGridLineIsVisible;
                    break;

                case nameof(GridDefinition.MajorGridLineColor):
                    WpfPlot1.Plot.Grid.XAxisStyle.MajorLineStyle.Color = ScottPlot.Color.FromHex(
                        GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(gridDef.MajorGridLineColor));
                    WpfPlot1.Plot.Grid.YAxisStyle.MajorLineStyle.Color = ScottPlot.Color.FromHex(
                        GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(gridDef.MajorGridLineColor));
                    break;

                case nameof(GridDefinition.MajorGridLineWidth):
                    WpfPlot1.Plot.Grid.XAxisStyle.MajorLineStyle.Width = gridDef.MajorGridLineWidth;
                    WpfPlot1.Plot.Grid.YAxisStyle.MajorLineStyle.Width = gridDef.MajorGridLineWidth;
                    break;

                case nameof(GridDefinition.MajorGridLinePattern):
                    WpfPlot1.Plot.Grid.XAxisStyle.MajorLineStyle.Pattern = GraphMapTemplateParser.GetLinePattern(gridDef.MajorGridLinePattern.ToString());
                    WpfPlot1.Plot.Grid.YAxisStyle.MajorLineStyle.Pattern = GraphMapTemplateParser.GetLinePattern(gridDef.MajorGridLinePattern.ToString());
                    break;

                case nameof(GridDefinition.MajorGridLineAntiAlias):
                    WpfPlot1.Plot.Grid.XAxisStyle.MajorLineStyle.AntiAlias = gridDef.MajorGridLineAntiAlias;
                    WpfPlot1.Plot.Grid.YAxisStyle.MajorLineStyle.AntiAlias = gridDef.MajorGridLineAntiAlias;
                    break;


                // =================================================
                //                           次网格线
                // =================================================
                case nameof(GridDefinition.MinorGridLineIsVisible):
                    WpfPlot1.Plot.Grid.XAxisStyle.MinorLineStyle.IsVisible = gridDef.MinorGridLineIsVisible;
                    WpfPlot1.Plot.Grid.YAxisStyle.MinorLineStyle.IsVisible = gridDef.MinorGridLineIsVisible;
                    break;

                case nameof(GridDefinition.MinorGridLineColor):
                    WpfPlot1.Plot.Grid.XAxisStyle.MinorLineStyle.Color = ScottPlot.Color.FromHex(
                        GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(gridDef.MinorGridLineColor));
                    WpfPlot1.Plot.Grid.YAxisStyle.MinorLineStyle.Color = ScottPlot.Color.FromHex(
                        GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(gridDef.MinorGridLineColor));
                    break;

                case nameof(GridDefinition.MinorGridLineWidth):
                    WpfPlot1.Plot.Grid.XAxisStyle.MinorLineStyle.Width = gridDef.MinorGridLineWidth;
                    WpfPlot1.Plot.Grid.YAxisStyle.MinorLineStyle.Width = gridDef.MinorGridLineWidth;
                    break;

                case nameof(GridDefinition.MinorGridLinePattern):
                    WpfPlot1.Plot.Grid.XAxisStyle.MinorLineStyle.Pattern = GraphMapTemplateParser.GetLinePattern(gridDef.MinorGridLinePattern.ToString());
                    WpfPlot1.Plot.Grid.YAxisStyle.MinorLineStyle.Pattern = GraphMapTemplateParser.GetLinePattern(gridDef.MinorGridLinePattern.ToString());
                    break;

                case nameof(GridDefinition.MinorGridLineAntiAlias):
                    WpfPlot1.Plot.Grid.XAxisStyle.MinorLineStyle.AntiAlias = gridDef.MinorGridLineAntiAlias;
                    WpfPlot1.Plot.Grid.YAxisStyle.MinorLineStyle.AntiAlias = gridDef.MinorGridLineAntiAlias;
                    break;


                // =================================================
                //                           背景填充
                // =================================================
                case nameof(GridDefinition.GridAlternateFillingIsEnable):
                case nameof(GridDefinition.GridFillColor1):
                case nameof(GridDefinition.GridFillColor2):
                if (gridDef.GridAlternateFillingIsEnable)
                {
                    WpfPlot1.Plot.Grid.XAxisStyle.FillColor1 = ScottPlot.Color.FromHex(
                                GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(gridDef.GridFillColor1));
                    WpfPlot1.Plot.Grid.YAxisStyle.FillColor1 = ScottPlot.Color.FromHex(
                                GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(gridDef.GridFillColor1));
                    WpfPlot1.Plot.Grid.XAxisStyle.FillColor2 = ScottPlot.Color.FromHex(
                                GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(gridDef.GridFillColor2));
                    WpfPlot1.Plot.Grid.YAxisStyle.FillColor2 = ScottPlot.Color.FromHex(
                                GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(gridDef.GridFillColor2));
                }
                else
                {
                    // 设置为透明以禁用填充
                    WpfPlot1.Plot.Grid.XAxisStyle.FillColor1 = ScottPlot.Colors.Transparent;
                    WpfPlot1.Plot.Grid.YAxisStyle.FillColor1 = ScottPlot.Colors.Transparent;
                    WpfPlot1.Plot.Grid.XAxisStyle.FillColor2 = ScottPlot.Colors.Transparent;
                    WpfPlot1.Plot.Grid.YAxisStyle.FillColor2 = ScottPlot.Colors.Transparent;
                }
                break;
            }
            return true;
        }

        /// <summary>
        /// 图例对象属性更新，触发
        /// </summary>
        /// <param name="legendDef">图例对象</param>
        /// <param name="e">点击属性</param>
        /// <returns></returns>
        private bool LegendPropertyChanged(LegendDefinition legendDef, PropertyChangedEventArgs e)
        {
            // 根据变化的属性名更新坐标轴
            switch (e.PropertyName)
            {
                // 图例排列方向，横向或者纵向
                case nameof(legendDef.Orientation):
                    WpfPlot1.Plot.Legend.Orientation = legendDef.Orientation;
                    break;

                // 图例是否显示
                case nameof(legendDef.IsVisible):
                    WpfPlot1.Plot.Legend.IsVisible = legendDef.IsVisible;
                    break;

                // 图例位置
                case nameof(legendDef.Alignment):
                    WpfPlot1.Plot.Legend.Alignment = legendDef.Alignment;
                    break;

                // 字体族
                case nameof(legendDef.Font):
                    WpfPlot1.Plot.Legend.FontName = legendDef.Font;
                    break;

                default:
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 坐标轴对象属性更新，触发。
        /// </summary>
        /// <param name="axisDef">传入的坐标轴定义对象，可以是 TernaryAxisDefinition 或 CartesianAxisDefinition</param>
        /// <param name="e">属性变更事件参数</param>
        /// <returns>如果UI需要刷新，则返回 true</returns>
        private bool AxisPropertyChanged(BaseAxisDefinition axisDef, PropertyChangedEventArgs e)
        {
            // =========================================================================
            //  三元相图 (Ternary Plot) 的处理逻辑
            // =========================================================================
            if (axisDef is TernaryAxisDefinition ternaryAxisDef)
            {
                // 从图表中找到 TriangularAxis 对象
                var triangularAxis = WpfPlot1.Plot.GetPlottables().OfType<ScottPlot.Plottables.TriangularAxis>().FirstOrDefault();
                if (triangularAxis == null) return false;

                // 根据类型获取对应的 TriangularAxisEdge
                ScottPlot.TriangularAxisEdge? targetEdge = ternaryAxisDef.Type switch
                {
                    "Left" => triangularAxis.Left,
                    "Right" => triangularAxis.Right,
                    "Bottom" => triangularAxis.Bottom,
                    _ => null
                };

                if (targetEdge == null) return false;

                // 更新 TriangularAxisEdge 的属性
                switch (e.PropertyName)
                {
                    // --- 坐标轴标题 ---
                    case nameof(TernaryAxisDefinition.Label):
                        targetEdge.LabelText = ternaryAxisDef.Label.Get();
                        break;
                    case nameof(TernaryAxisDefinition.Family):
                        targetEdge.LabelStyle.FontName = ternaryAxisDef.Family;
                        // 同时更新刻度标签字体
                        targetEdge.TickLabelStyle.FontName = ternaryAxisDef.TickLableFamily;
                        break;
                    case nameof(TernaryAxisDefinition.Size):
                        targetEdge.LabelStyle.FontSize = ternaryAxisDef.Size;
                        break;
                    case nameof(TernaryAxisDefinition.Color):
                        targetEdge.LabelStyle.ForeColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(ternaryAxisDef.Color));
                        break;
                    case nameof(TernaryAxisDefinition.IsBold):
                        targetEdge.LabelStyle.Bold = ternaryAxisDef.IsBold;
                        break;
                    case nameof(TernaryAxisDefinition.IsItalic):
                        targetEdge.LabelStyle.Italic = ternaryAxisDef.IsItalic;
                        break;

                    // --- 主刻度样式 (三元图只有主刻度) ---
                    case nameof(TernaryAxisDefinition.MajorTickWidth):
                        targetEdge.TickMarkStyle.Width = ternaryAxisDef.MajorTickWidth;
                        break;
                    case nameof(TernaryAxisDefinition.MajorTickWidthColor):
                        targetEdge.TickMarkStyle.Color = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(ternaryAxisDef.MajorTickWidthColor));
                        break;

                    // --- 刻度标签样式 ---
                    case nameof(TernaryAxisDefinition.TickLableFamily):
                        targetEdge.TickLabelStyle.FontName = ternaryAxisDef.TickLableFamily;
                        break;
                    case nameof(TernaryAxisDefinition.TickLablesize):
                        targetEdge.TickLabelStyle.FontSize = ternaryAxisDef.TickLablesize;
                        break;
                    case nameof(TernaryAxisDefinition.TickLablecolor):
                        targetEdge.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(ternaryAxisDef.TickLablecolor));
                        break;
                    case nameof(TernaryAxisDefinition.TickLableisBold):
                        targetEdge.TickLabelStyle.Bold = ternaryAxisDef.TickLableisBold;
                        break;
                    case nameof(TernaryAxisDefinition.TickLableisItalic):
                        targetEdge.TickLabelStyle.Italic = ternaryAxisDef.TickLableisItalic;
                        break;

                    default:
                        // 如果属性名未匹配，则不刷新
                        return false;
                }
                return true; // 返回true表示需要刷新
            }
            // =========================================================================
            //  笛卡尔坐标系 (Cartesian Plot) 的处理逻辑
            // =========================================================================
            else if (axisDef is CartesianAxisDefinition cartesianAxisDef)
            {
                // 根据 AxisDefinition 的 Type 获取对应的坐标轴对象
                ScottPlot.IAxis? targetAxis = cartesianAxisDef.Type switch
                {
                    "Left" => WpfPlot1.Plot.Axes.Left,
                    "Right" => WpfPlot1.Plot.Axes.Right,
                    "Bottom" => WpfPlot1.Plot.Axes.Bottom,
                    "Top" => WpfPlot1.Plot.Axes.Top,
                    _ => null
                };

                if (targetAxis == null) return false;

                // 根据变化的属性名更新坐标轴
                switch (e.PropertyName)
                {
                    // === 坐标轴标题 (继承自 BaseAxisDefinition) ===
                    case nameof(CartesianAxisDefinition.Label):
                        targetAxis.Label.Text = cartesianAxisDef.Label.Get();
                        break;
                    case nameof(CartesianAxisDefinition.Family):
                        targetAxis.Label.FontName = cartesianAxisDef.Family;
                        break;
                    case nameof(CartesianAxisDefinition.Size):
                        targetAxis.Label.FontSize = cartesianAxisDef.Size;
                        break;
                    case nameof(CartesianAxisDefinition.Color):
                        targetAxis.Label.ForeColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(cartesianAxisDef.Color));
                        break;
                    case nameof(CartesianAxisDefinition.IsBold):
                        targetAxis.Label.Bold = cartesianAxisDef.IsBold;
                        break;
                    case nameof(CartesianAxisDefinition.IsItalic):
                        targetAxis.Label.Italic = cartesianAxisDef.IsItalic;
                        break;

                    // === 刻度生成器与范围 (CartesianAxisDefinition 特有) ===
                    case nameof(CartesianAxisDefinition.ScaleType):
                        if (cartesianAxisDef.ScaleType == AxisScaleType.Logarithmic)
                        {
                            var minorTickGen = new ScottPlot.TickGenerators.LogMinorTickGenerator();
                            var tickGen = new ScottPlot.TickGenerators.NumericAutomatic
                            {
                                MinorTickGenerator = minorTickGen,
                                IntegerTicksOnly = true,
                                LabelFormatter = y => $"{Math.Pow(10, y)}"
                            };
                            targetAxis.TickGenerator = tickGen;
                        }
                        else
                        {
                            var autoTickGen2 = new ScottPlot.TickGenerators.NumericAutomatic();
                            int intervalCount = Math.Max(1, cartesianAxisDef.MinorTicksPerMajorTick + 1);
                            var minorTickGen = new ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator(intervalCount);
                            autoTickGen2.MinorTickGenerator = minorTickGen;
                            targetAxis.TickGenerator = autoTickGen2;
                        }
                        UpdateAxisLimitsFromModel(cartesianAxisDef, targetAxis);
                        break;
                    case nameof(CartesianAxisDefinition.MinorTicksPerMajorTick):
                        if (targetAxis.TickGenerator is ScottPlot.TickGenerators.NumericAutomatic autoTickGen)
                        {
                            int intervalCount = Math.Max(1, cartesianAxisDef.MinorTicksPerMajorTick + 1);
                            var minorTickGen = new ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator(intervalCount);
                            autoTickGen.MinorTickGenerator = minorTickGen;
                        }
                        break;
                    case nameof(CartesianAxisDefinition.Minimum):
                    case nameof(CartesianAxisDefinition.Maximum):
                        UpdateAxisLimitsFromModel(cartesianAxisDef, targetAxis);
                        break;

                    // === 主刻度 (继承自 BaseAxisDefinition) ===
                    case nameof(CartesianAxisDefinition.MajorTickLength):
                        targetAxis.MajorTickStyle.Length = cartesianAxisDef.MajorTickLength;
                        break;
                    case nameof(CartesianAxisDefinition.MajorTickWidth):
                        targetAxis.MajorTickStyle.Width = cartesianAxisDef.MajorTickWidth;
                        break;
                    case nameof(CartesianAxisDefinition.MajorTickWidthColor):
                        targetAxis.MajorTickStyle.Color = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(cartesianAxisDef.MajorTickWidthColor));
                        break;
                    case nameof(CartesianAxisDefinition.MajorTickAntiAlias):
                        targetAxis.MajorTickStyle.AntiAlias = cartesianAxisDef.MajorTickAntiAlias;
                        break;

                    // === 次刻度 (CartesianAxisDefinition 特有) ===
                    case nameof(CartesianAxisDefinition.MinorTickLength):
                        targetAxis.MinorTickStyle.Length = cartesianAxisDef.MinorTickLength;
                        break;
                    case nameof(CartesianAxisDefinition.MinorTickWidth):
                        targetAxis.MinorTickStyle.Width = cartesianAxisDef.MinorTickWidth;
                        break;
                    case nameof(CartesianAxisDefinition.MinorTickColor):
                        targetAxis.MinorTickStyle.Color = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(cartesianAxisDef.MinorTickColor));
                        break;
                    case nameof(CartesianAxisDefinition.MinorTickAntiAlias):
                        targetAxis.MinorTickStyle.AntiAlias = cartesianAxisDef.MinorTickAntiAlias;
                        break;

                    // === 刻度标签 (继承自 BaseAxisDefinition) ===
                    case nameof(CartesianAxisDefinition.TickLableFamily):
                        targetAxis.TickLabelStyle.FontName = cartesianAxisDef.TickLableFamily;
                        break;
                    case nameof(CartesianAxisDefinition.TickLablesize):
                        targetAxis.TickLabelStyle.FontSize = cartesianAxisDef.TickLablesize;
                        break;
                    case nameof(CartesianAxisDefinition.TickLablecolor):
                        targetAxis.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(cartesianAxisDef.TickLablecolor));
                        break;
                    case nameof(CartesianAxisDefinition.TickLableisBold):
                        targetAxis.TickLabelStyle.Bold = cartesianAxisDef.TickLableisBold;
                        break;
                    case nameof(CartesianAxisDefinition.TickLableisItalic):
                        targetAxis.TickLabelStyle.Italic = cartesianAxisDef.TickLableisItalic;
                        break;

                    default:
                        return false;
                }

                return true;
            }

            // 如果对象既不是 TernaryAxisDefinition 也不是 CartesianAxisDefinition，则不处理
            return false;
        }

        /// <summary>
        /// 使用JavaScript脚本计算坐标
        /// </summary>
        /// <param name="dataRow">数据行</param>
        /// <param name="dataColumns">参与计算的数据列名</param>
        /// <param name="scriptBody">脚本内容</param>
        /// <returns>返回计算结果的double数组，如果计算失败或返回类型不正确则返回null</returns>
        private double[]? CalculateCoordinatesUsingScript(DataRow dataRow, List<string> dataColumns, string scriptBody)
        {
            try
            {
                var engine = new Jint.Engine();

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
            if (layer?.Plottable == null) return;

            // 遮罩透明度值
            byte dimAlpha = 60;

            switch (layer.Plottable)
            {
                // 线条变暗
                case ScottPlot.Plottables.LinePlot linePlot:
                    linePlot.Color = linePlot.Color.WithAlpha(dimAlpha);
                    break;

                // 文本变暗
                case ScottPlot.Plottables.Text textPlot:
                    // 如果有背景色，也变暗
                    if (textPlot.LabelBackgroundColor != Colors.Transparent)
                    {
                        textPlot.LabelBackgroundColor = textPlot.LabelBackgroundColor.WithAlpha(dimAlpha);
                    }
                    // 如果有边框，也变暗
                    if (textPlot.LabelBorderColor != Colors.Transparent)
                    {
                        textPlot.LabelBorderColor = textPlot.LabelBorderColor.WithAlpha(dimAlpha);
                    }
                    // 文字颜色变暗
                    textPlot.LabelFontColor = textPlot.LabelFontColor.WithAlpha(dimAlpha);
                    break;

                case ScottPlot.Plottables.Arrow arrowPlot:
                    arrowPlot.ArrowFillColor = arrowPlot.ArrowFillColor.WithAlpha(dimAlpha);
                    break;

                // 数据点变暗
                case ScottPlot.Plottables.Scatter scatterPlot:
                    scatterPlot.Color = scatterPlot.Color.WithAlpha(dimAlpha);
                    break;

                // 多边形变暗
                case ScottPlot.Plottables.Polygon polygonPlot:
                    // 将填充色和边框色都变暗
                    polygonPlot.FillStyle.Color = polygonPlot.FillStyle.Color.WithAlpha(dimAlpha);
                    polygonPlot.LineStyle.Color = polygonPlot.LineStyle.Color.WithAlpha(dimAlpha);
                    break;
                // TODO: 添加其他图层类型变暗逻辑
        }
        }

        /// <summary>
        /// 将图层元素的样式恢复到其原始定义的状态
        /// </summary>
        private void RevertLayerStyle(LayerItemViewModel layer)
        {
            if (layer?.Plottable == null) return;

            // 根据不同的图层类型恢复其原始样式
            switch (layer.Plottable)
            {
                // 恢复线条样式
                case ScottPlot.Plottables.LinePlot linePlot:
                    var lineDef = (layer as LineLayerItemViewModel)?.LineDefinition;
                    if (lineDef == null) break;
                    // 从其定义对象中读取原始属性并应用
                    linePlot.LineWidth = lineDef.Width;
                    linePlot.Color = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(lineDef.Color));
                    break;

                // 恢复文本样式
                case ScottPlot.Plottables.Text textPlot:
                    var textDef = (layer as TextLayerItemViewModel)?.TextDefinition;
                    if (textDef == null) break;

                    // 恢复原始颜色
                    textPlot.LabelFontColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(textDef.Color));
                    textPlot.LabelBackgroundColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(textDef.BackgroundColor));
                    textPlot.LabelBorderColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(textDef.BorderColor));
                    textPlot.LabelBorderWidth = textDef.BorderWidth;
                    break;

                // 恢复数据点样式
                case ScottPlot.Plottables.Scatter scatterPlot:
                    var scatterDef = (layer as ScatterLayerItemViewModel)?.ScatterDefinition;
                    if (scatterDef == null) break;
                    // 从其定义对象中读取原始颜色并应用
                    scatterPlot.Color = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(scatterDef.Color));
                    scatterPlot.MarkerStyle.OutlineWidth = 0;
                    break;

                // 恢复箭头样式
                case ScottPlot.Plottables.Arrow arrowPlot:
                    var arrowDef = (layer as ArrowLayerItemViewModel)?.ArrowDefinition;
                    if (arrowDef == null) break;
                    arrowPlot.ArrowFillColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(arrowDef.Color));
                    arrowPlot.ArrowWidth = arrowDef.ArrowWidth;
                    arrowPlot.ArrowheadWidth = arrowDef.ArrowheadWidth;
                    arrowPlot.ArrowheadLength = arrowDef.ArrowheadLength;
                    break;

                // 多边形恢复
                case ScottPlot.Plottables.Polygon polygonPlot:
                    var polygonDef = (layer as PolygonLayerItemViewModel)?.PolygonDefinition;
                    if (polygonDef == null) break;
                    // 从其定义对象中读取原始属性并应用
                    polygonPlot.FillStyle.Color = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(polygonDef.FillColor));
                    polygonPlot.LineStyle.Color = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(polygonDef.BorderColor));
                    polygonPlot.LineStyle.Width = polygonDef.BorderWidth;
                    break;
                    // TODO: 在此为其他图层类型（如点、多边形）添加恢复逻辑
            }
        }

        /// <summary>
        /// 加载模板、构建图层树并刷新绘图
        /// </summary>
        /// <param name="templatePath">模板文件的路径</param>
        private async Task LoadAndBuildLayers(string templatePath)
        {
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
                        RefreshPlotFromLayers();
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
        public void RefreshPlotFromLayers()
        {
            if (WpfPlot1 == null || CurrentTemplate == null) return;

            WpfPlot1.Plot.Clear();

            BaseMapType = CurrentTemplate.TemplateType;

            Clockwise = CurrentTemplate.Clockwise;

            // 根据模板类型选择渲染路径
            if (CurrentTemplate.TemplateType == "Ternary")
            {
                RenderTernaryPlot();
                WpfPlot1.Plot.Axes.AutoScale();
            }
            else // 默认处理笛卡尔坐标系
            {
                RenderCartesianPlot();
            }

            // 最后添加坐标定位十字轴，确保在最顶层
            WpfPlot1.Plot.Add.Plottable(CrosshairPlot);

            // 获取脚本对象
            CurrentScript = CurrentTemplate.Script;

            //WpfPlot1.Plot.Axes.AutoScale();
            WpfPlot1.Refresh();
        }

        // 创建箭头绘图对象
        private void CreateArrowPlottable(ArrowLayerItemViewModel arrowLayer)
        {
            var arrowDef = arrowLayer.ArrowDefinition;
            if (arrowDef.Start == null || arrowDef.End == null) return;
            var arrowPlot = WpfPlot1.Plot.Add.Arrow(new Coordinates(arrowDef.Start.X, arrowDef.Start.Y), new Coordinates(arrowDef.End.X, arrowDef.End.Y));

            // 应用样式
            arrowPlot.ArrowFillColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(arrowDef.Color));
            arrowPlot.ArrowWidth = arrowDef.ArrowWidth;
            arrowPlot.ArrowheadWidth = arrowDef.ArrowheadWidth;
            arrowPlot.ArrowheadLength = arrowDef.ArrowheadLength;

            // 隐藏属性-箭头描边颜色
            arrowPlot.ArrowLineColor = Colors.Transparent;

            // 存储引用
            arrowLayer.Plottable = arrowPlot;
        }

        /// <summary>
        /// 渲染标准笛卡尔坐标系图表
        /// </summary>
        private void RenderCartesianPlot()
        {
            WpfPlot1.Plot.Axes.Title.IsVisible = true;
            WpfPlot1.Plot.Axes.AddTopAxis();

            var allNodes = FlattenTree(LayerTree);

            // 更新坐标轴样式
            foreach (var item in allNodes)
            {
                if (item is AxisLayerItemViewModel axisLayer)
                {
                    ApplyAxisSettings(axisLayer);
                }
                else
                {
                    item.Plottable = null; // 清空旧的绘图对象引用
                }
            }

            // --- 绘制多边形 ---
            foreach (var item in allNodes.OfType<PolygonLayerItemViewModel>().Where(l => l.IsVisible))
            {
                CreatePolygonPlottable(item);
            }

            // --- 绘制线条 ---
            foreach (var item in allNodes.OfType<LineLayerItemViewModel>().Where(l => l.IsVisible))
            {
                CreateLinePlottable(item);
            }

            // --- 绘制箭头 ---
            foreach (var item in allNodes.OfType<ArrowLayerItemViewModel>().Where(l => l.IsVisible))
            {
                CreateArrowPlottable(item);
            }

            // --- 绘制数据点 ---
            foreach (var item in allNodes.OfType<ScatterLayerItemViewModel>().Where(l => l.IsVisible))
            {
                var scatterDef = item.ScatterDefinition;
                var scatterPlot = WpfPlot1.Plot.Add.Scatter(new[] { scatterDef.StartAndEnd.X }, new[] { scatterDef.StartAndEnd.Y });
                scatterPlot.Color = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(scatterDef.Color));
                scatterPlot.MarkerSize = scatterDef.Size;
                scatterPlot.MarkerShape = scatterDef.MarkerShape;
                item.Plottable = scatterPlot;
            }

            // --- 绘制文本 ---
            foreach (var item in allNodes.OfType<TextLayerItemViewModel>().Where(l => l.IsVisible))
            {
                CreateTextPlottable(item);
            }

            // 全局设置——处理图例
            WpfPlot1.Plot.Legend.Alignment = CurrentTemplate.Info.Legend.Alignment;
            WpfPlot1.Plot.Legend.FontName = (LanguageService.CurrentLanguage == "zh-CN") ? "微软雅黑" : CurrentTemplate.Info.Legend.Font;
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

            // 设置坐标轴标题和样式
            foreach (var item in allNodes.OfType<AxisLayerItemViewModel>().Where(l => l.IsVisible))
            {
                // 确保 axisDef 是 TernaryAxisDefinition 类型
                if (item.AxisDefinition is TernaryAxisDefinition ternaryAxisDef)
                {
                    ScottPlot.TriangularAxisEdge? targetEdge = null;

                    switch (ternaryAxisDef.Type)
                    {
                        case "Bottom":
                            targetEdge = triangularAxis.Bottom;
                            break;
                        case "Left":
                            targetEdge = triangularAxis.Left;
                            break;
                        case "Right":
                            targetEdge = triangularAxis.Right;
                            break;
                    }

                    if (targetEdge != null)
                    {
                        // --- 应用所有标题样式 ---
                        targetEdge.LabelText = ternaryAxisDef.Label.Get();
                        targetEdge.LabelStyle.FontName = Fonts.Detect(ternaryAxisDef.Label.Get());
                        targetEdge.LabelStyle.FontSize = ternaryAxisDef.Size;
                        targetEdge.LabelStyle.ForeColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(ternaryAxisDef.Color));
                        targetEdge.LabelStyle.Bold = ternaryAxisDef.IsBold;
                        targetEdge.LabelStyle.Italic = ternaryAxisDef.IsItalic;

                        // --- 应用所有刻度样式 ---
                        targetEdge.TickMarkStyle.Width = ternaryAxisDef.MajorTickWidth;
                        targetEdge.TickMarkStyle.Color = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(ternaryAxisDef.MajorTickWidthColor));

                        // --- 应用所有刻度标签样式 ---
                        targetEdge.TickLabelStyle.FontName = ternaryAxisDef.TickLableFamily;
                        targetEdge.TickLabelStyle.FontSize = ternaryAxisDef.TickLablesize;
                        targetEdge.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(ternaryAxisDef.TickLablecolor));
                        targetEdge.TickLabelStyle.Bold = ternaryAxisDef.TickLableisBold;
                        targetEdge.TickLabelStyle.Italic = ternaryAxisDef.TickLableisItalic;
                    }
                }
            }

            // --- 绘制数据点 ---
            // 在三元图中，ScatterLayerItemViewModel中的X和Y分别代表底轴和左轴的比例
            foreach (var item in allNodes.OfType<ScatterLayerItemViewModel>().Where(l => l.IsVisible))
            {
                var scatterDef = item.ScatterDefinition;

                // 使用三角轴的GetCoordinates方法将分数坐标转换为笛卡尔坐标
                // X代表底轴比例，Y代表左轴比例。第三个轴的比例将自动计算。
                var cartesianCoords = triangularAxis.GetCoordinates(scatterDef.StartAndEnd.X, scatterDef.StartAndEnd.Y);

                // 在计算出的笛卡尔坐标上添加散点
                var scatterPlot = WpfPlot1.Plot.Add.Scatter(new[] { cartesianCoords });

                // 应用样式
                scatterPlot.Color = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(scatterDef.Color));
                scatterPlot.MarkerSize = scatterDef.Size;
                scatterPlot.MarkerShape = scatterDef.MarkerShape;

                // 将创建的Plottable对象关联回图层项
                item.Plottable = scatterPlot;
            }

            // --- 绘制箭头 (Arrow) ---
            foreach (var item in allNodes.OfType<ArrowLayerItemViewModel>().Where(l => l.IsVisible))
            {
                CreateTernaryArrowPlottable(item, triangularAxis);
            }

            // --- 绘制多边形 (Polygon) ---
            foreach (var item in allNodes.OfType<PolygonLayerItemViewModel>().Where(l => l.IsVisible))
            {
                CreatePolygonPlottable(item);
            }

            // --- 绘制线条 (Line) ---
            foreach (var item in allNodes.OfType<LineLayerItemViewModel>().Where(l => l.IsVisible))
            {
                CreateLinePlottable(item);
            }

            // --- 绘制文本 (Text) ---
            foreach (var item in allNodes.OfType<TextLayerItemViewModel>().Where(l => l.IsVisible))
            {
                CreateTextPlottable(item);
            }

            // 三角图需要方形坐标轴以避免变形
            WpfPlot1.Plot.Axes.SquareUnits();
        }

        /// <summary>
        /// 在三元坐标系下创建箭头绘图对象。
        /// 此方法会处理从存储的三元坐标到绘图所需的笛卡尔坐标的转换。
        /// </summary>
        /// <param name="arrowLayer">包含箭头定义的图层ViewModel</param>
        /// <param name="triangularAxis">用于坐标转换的三元轴对象</param>
        private void CreateTernaryArrowPlottable(ArrowLayerItemViewModel arrowLayer, ScottPlot.Plottables.TriangularAxis triangularAxis)
        {
            var arrowDef = arrowLayer.ArrowDefinition;
            if (arrowDef.Start == null || arrowDef.End == null) return;

            // 从存储的三元坐标分量中获取起点和终点
            // 假设 X 存储 bottomFraction, Y 存储 leftFraction
            double startBottom = arrowDef.Start.X;
            double startLeft = arrowDef.Start.Y;
            double startRight = 1 - startBottom - startLeft; // 计算第三个分量

            double endBottom = arrowDef.End.X;
            double endLeft = arrowDef.End.Y;
            double endRight = 1 - endBottom - endLeft; // 计算第三个分量

            // 使用三角轴对象将三元坐标转换为笛卡尔坐标
            var startCartesian = triangularAxis.GetCoordinates(startBottom, startLeft, startRight);
            var endCartesian = triangularAxis.GetCoordinates(endBottom, endLeft, endRight);

            // 使用转换后的笛卡尔坐标创建箭头
            var arrowPlot = WpfPlot1.Plot.Add.Arrow(startCartesian, endCartesian);

            // 应用样式
            arrowPlot.ArrowFillColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(arrowDef.Color));
            arrowPlot.ArrowWidth = arrowDef.ArrowWidth;
            arrowPlot.ArrowheadWidth = arrowDef.ArrowheadWidth;
            arrowPlot.ArrowheadLength = arrowDef.ArrowheadLength;
            arrowPlot.ArrowLineColor = Colors.Transparent;

            // 存储引用
            arrowLayer.Plottable = arrowPlot;
        }

        // 设置坐标轴
        private void ApplyAxisSettings(AxisLayerItemViewModel axisLayer)
        {
            var axisDef = axisLayer.AxisDefinition;

            // 根据类型获取 ScottPlot 中的坐标轴对象
            ScottPlot.IAxis? targetAxis = axisDef.Type switch
            {
                "Left" => WpfPlot1.Plot.Axes.Left,
                "Right" => WpfPlot1.Plot.Axes.Right,
                "Bottom" => WpfPlot1.Plot.Axes.Bottom,
                "Top" => WpfPlot1.Plot.Axes.Top,
                _ => null
            };

            if (targetAxis == null) return;

            // 将 ViewModel 中的 IsVisible 状态同步到坐标轴
            targetAxis.IsVisible = axisLayer.IsVisible;
            targetAxis.Label.Text = axisDef.Label.Get();
            targetAxis.Label.FontName = Fonts.Detect(targetAxis.Label.Text);
            targetAxis.Label.FontSize = axisDef.Size;
            targetAxis.Label.ForeColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(axisDef.Color));
            targetAxis.Label.Bold = axisDef.IsBold;
            targetAxis.Label.Italic = axisDef.IsItalic;

            // 检查是否为笛卡尔坐标轴
            if (axisDef is CartesianAxisDefinition cartesianAxisDef)
            {
                // 在处理坐标轴样式时增加对数刻度的处理
                if (cartesianAxisDef.ScaleType == AxisScaleType.Logarithmic)
                {
                    var minorTickGen = new ScottPlot.TickGenerators.LogMinorTickGenerator();
                    var tickGen = new ScottPlot.TickGenerators.NumericAutomatic();
                    tickGen.MinorTickGenerator = minorTickGen;
                    tickGen.IntegerTicksOnly = true;
                    tickGen.LabelFormatter = y => $"{Math.Pow(10, y)}";
                    targetAxis.TickGenerator = tickGen;
                }

                // 设置初始的坐标轴范围
                UpdateAxisLimitsFromModel(cartesianAxisDef, targetAxis);
            }
        }

        // 创建线条绘图对象
        private void CreateLinePlottable(LineLayerItemViewModel lineLayer)
        {
            var lineDef = lineLayer.LineDefinition;
            if (lineDef.Start == null || lineDef.End == null) return;
            var linePlot = WpfPlot1.Plot.Add.Line(lineDef.Start.X, lineDef.Start.Y, lineDef.End.X, lineDef.End.Y);

            // 应用样式
            linePlot.LineWidth = lineDef.Width;
            linePlot.Color = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(lineDef.Color));
            linePlot.LinePattern = GraphMapTemplateParser.GetLinePattern(lineDef.Style.ToString());

            // 存储引用
            lineLayer.Plottable = linePlot;
        }

        // 创建文本绘图对象
        private void CreateTextPlottable(TextLayerItemViewModel textLayer)
        {
            var textDef = textLayer.TextDefinition;
            var textPlot = WpfPlot1.Plot.Add.Text(textDef.Content.Get(), new Coordinates(textDef.StartAndEnd.X, textDef.StartAndEnd.Y));

            // 应用样式
            textPlot.LabelText = textDef.Content.Get();
            textPlot.LabelFontName = Fonts.Detect(textPlot.LabelText);      // 自适应字体
            textPlot.LabelFontSize = textDef.Size;
            textPlot.LabelRotation = textDef.Rotation;
            textPlot.LabelFontColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex((textDef.Color)));
            textPlot.LabelBold = textDef.IsBold;
            textPlot.LabelItalic = textDef.IsItalic;

            // 存储引用
            textLayer.Plottable = textPlot;
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

        /// <summary>
        /// 点击图层对象, 在图上高亮显示, 并在属性面板显示其属性
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
                // 恢复所有图层的原始样式
                foreach (var layer in allPlottableLayers)
                {
                    RevertLayerStyle(layer);
                }

                // 如果是分类文件夹，清空属性面板
                PropertyGridModel = nullObject;
                _selectedLayer = selectedItem; // 更新引用
                WpfPlot1.Refresh();
                return;
            }

            // --- 如果选中了一个可绘制的图层 ---

            // 更新当前选中的图层引用
            _selectedLayer = selectedItem;

            // 在属性面板中显示该图层的属性
            object? objectToInspect = selectedItem switch
            {
                PointLayerItemViewModel pointLayer => pointLayer.PointDefinition,
                LineLayerItemViewModel lineLayer => lineLayer.LineDefinition,
                TextLayerItemViewModel textLayer => textLayer.TextDefinition,
                ArrowLayerItemViewModel arrowLayer => arrowLayer.ArrowDefinition,
                PolygonLayerItemViewModel polygonLayer => polygonLayer.PolygonDefinition,
                AxisLayerItemViewModel axisLayer => axisLayer.AxisDefinition,
                LegendLayerItemViewModel legendLayer => legendLayer.LegendDefinition,
                ScatterLayerItemViewModel scatterLayer => scatterLayer.ScatterDefinition,
                _ => nullObject
            };
            PropertyGridModel = objectToInspect;

            // 应用新的高亮样式：选中的恢复原样，其他的变暗
            foreach (var layer in allPlottableLayers)
            {
                if (layer == selectedItem)
                {
                    // 确保选中的图层是其原始样式
                    RevertLayerStyle(layer);
                }
                else
                {
                    // 将其他图层变暗
                    DimLayer(layer);
                }
            }

            WpfPlot1.Refresh();
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
            // 1. 清除当前绘图中的所有由数据导入的点
            ClearExistingPlottedData();

            // 2. 根据当前数据表格中的数据重新进行投点
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
                RevertLayerStyle(layer);
            }

            WpfPlot1.Refresh();
            // 清楚图层列表选中状态
            if (_selectedLayer != null)
            {
                _selectedLayer.IsSelected = false;
                _selectedLayer = null;
            }
            PropertyGridModel = nullObject;   // 取消属性编辑器
            ScriptsPropertyGrid = false;
        }

        /// <summary>
        /// 切换十字定位轴的显示/隐藏状态
        /// </summary>
        [RelayCommand]
        private void LocationAxis()
        {

            // 切换追踪模式的状态
            IsCrosshairVisible = !IsCrosshairVisible;
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
            string path = newTemplateControl.FilePath;
            string plotType = newTemplateControl.PlotType;

            // 检查数据是否有效
            if (string.IsNullOrWhiteSpace(language) || string.IsNullOrWhiteSpace(category)
                 || string.IsNullOrEmpty(plotType) || string.IsNullOrWhiteSpace(path))
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

            var allCategory = category.Split(new[] { " > " }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(lang => lang.Trim())
                                .ToList();

            if(allCategory.Count < 2)
            {
                // 分类结构必须大于等于2！
                MessageHelper.Warning(LanguageService.Instance["category_structure_min_two"]);
                return;
            }

            _currentTemplateFilePath = Path.Combine(path, $"{allCategory[allCategory.Count-2]}_{allCategory[allCategory.Count - 1]}.json");

            // 创建新底图
            CurrentTemplate = GraphMapTemplate.CreateDefault(allLanguages, plotType, category);

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
                    TabIndex = 0; // 确保显示的是绘图选项卡
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
                            var ternaryValues = CalculateCoordinatesUsingScript(row, dataColumns, scriptDefinition.ScriptBody);
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

                    var scatterPlotForCategory = WpfPlot1.Plot.Add.ScatterPoints(cartesianCoords.ToArray());
                    scatterPlotForCategory.Color = groupColor;
                    scatterPlotForCategory.MarkerSize = 10;
                    scatterPlotForCategory.LegendText = categoryName;

                    var scatterDefForCategory = new ScatterDefinition
                    {
                        Color = groupColor.ToHex(),
                        Size = 10
                    };

                    var categoryViewModel = new ScatterLayerItemViewModel(scatterDefForCategory)
                    {
                        Name = categoryName,
                        Plottable = scatterPlotForCategory,
                        IsVisible = true
                    };
                    categoryViewModel.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(ScatterLayerItemViewModel.IsVisible))
                        {
                            if (categoryViewModel.Plottable != null)
                            {
                                categoryViewModel.Plottable.IsVisible = categoryViewModel.IsVisible;
                                WpfPlot1.Refresh();
                            }
                        }
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
                            var coordinates = CalculateCoordinatesUsingScript(row, dataColumns, scriptDefinition.ScriptBody);
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

                    var scatterPlotForCategory = WpfPlot1.Plot.Add.ScatterPoints(xs.ToArray(), ys.ToArray());
                    scatterPlotForCategory.Color = groupColor;
                    scatterPlotForCategory.MarkerSize = 10;
                    scatterPlotForCategory.LegendText = categoryName;

                    var scatterDefForCategory = new ScatterDefinition
                    {
                        Color = groupColor.ToHex(),
                        Size = 10
                    };

                    var categoryViewModel = new ScatterLayerItemViewModel(scatterDefForCategory)
                    {
                        Name = categoryName,
                        Plottable = scatterPlotForCategory,
                        IsVisible = true
                    };
                    categoryViewModel.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(ScatterLayerItemViewModel.IsVisible))
                        {
                            if (categoryViewModel.Plottable != null)
                            {
                                categoryViewModel.Plottable.IsVisible = categoryViewModel.IsVisible;
                                WpfPlot1.Refresh();
                            }
                        }
                    };
                    rootDataNode.Children.Add(categoryViewModel);
                }
            }

            // 刷新图表和图例
            WpfPlot1.Plot.Legend.IsVisible = true;
            WpfPlot1.Plot.Axes.AutoScale();
            WpfPlot1.Refresh();
            MessageHelper.Success(LanguageService.Instance["data_plotting_successful"]);

            // 投点后自动切回绘图选项卡
            RibbonTabIndex = 0;
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

        /// <summary>
        /// 删除当前选中的绘图对象。
        /// </summary>
        [RelayCommand]
        private void DeleteSelectedObject()
        {
            // 检查是否有选中的图层
            if (_selectedLayer == null)
            {
                MessageHelper.Warning(LanguageService.Instance["please_select_an_object_to_delete_first"]);
                return;
            }

            // 禁止删除坐标轴等基础图层
            if (_selectedLayer is AxisLayerItemViewModel)
            {
                MessageHelper.Warning(LanguageService.Instance["cannot_delete_base_layers"]);
                return;
            }

            // 从ScottPlot的绘图对象集合中移除
            if (_selectedLayer.Plottable != null)
            {
                WpfPlot1.Plot.Remove(_selectedLayer.Plottable);
            }

            // 查找父图层
            var parentLayer = FindParentLayer(LayerTree, _selectedLayer);

            // 从数据源中移除选中的图层
            if (parentLayer != null)
            {
                // 如果有父图层，则从父图层的子集和中移除
                parentLayer.Children.Remove(_selectedLayer);

                // 检查父图层是否为空，如果为空则一并移除
                if (parentLayer.Children.Count == 0)
                {
                    // 假设所有分类图层都在根级别，直接从LayerTree移除
                    LayerTree.Remove(parentLayer);
                }
            }
            else
            {
                // 如果没有父图层，说明是顶级图层，直接从根集合移除
                LayerTree.Remove(_selectedLayer);
            }

            // 重置选中状态和属性面板
            _selectedLayer = null;
            PropertyGridModel = nullObject;

            // 取消所有对象的高亮/遮罩效果
            CancelSelected();

            // 刷新绘图控件
            WpfPlot1.Refresh();

            // 显示成功提示
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
            // 清除所有绘图对象
            WpfPlot1.Plot.Clear();

            // 重置坐标轴布局为默认值
            WpfPlot1.Plot.Layout.Default();

            // 移除所有与轴相关的自定义规则
            WpfPlot1.Plot.Axes.UnlinkAll();
            WpfPlot1.Plot.Axes.Rules.Clear();

            // 重新添加十字轴，确保它存在且在最上层
            WpfPlot1.Plot.Add.Plottable(CrosshairPlot);

            // 确保将全局变量重置为默认状态
            MainPlotViewModel.BaseMapType = String.Empty;
            MainPlotViewModel.Clockwise = true;

            // 清除图层树和属性面板的绑定
            LayerTree.Clear();
            PropertyGridModel = null;
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
    }
}