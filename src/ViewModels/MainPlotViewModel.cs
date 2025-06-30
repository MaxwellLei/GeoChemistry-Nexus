using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Controls;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using HandyControl.Controls;
using Jint;
using OfficeOpenXml;
using Ookii.Dialogs.Wpf;
using ScottPlot;
using ScottPlot.Colormaps;
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
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

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

        // 绘图控件
        private WpfPlot WpfPlot1;

        // 说明控件
        private System.Windows.Controls.RichTextBox _richTextBox;
        
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

        [ObservableProperty]
        private bool _isAddingText = false; // 标记是否正处于添加文本的模式

        [ObservableProperty]
        private bool _isAddingLine = false; // 标记是否正处于添加线条的模式

        private Coordinates? _lineStartPoint = null; // 用于存储线条的起点
        private ScottPlot.Plottables.LinePlot? _tempLinePlot; // 用于实时预览的临时线条


        //  ==============================
        //              测试区域
        //  ==============================



        // 用于存储当前正在编辑的模板的完整文件路径
        private string _currentTemplateFilePath;


        // 初始化
        public MainPlotViewModel(WpfPlot wpfPlot, System.Windows.Controls.RichTextBox richTextBox)
        {
            // 获取模板列表
            GraphMapTemplateNode = GraphMapTemplateParser.Parse(
                JsonHelper.ReadJsonFile(
                Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "GraphMapList.json")));

            WpfPlot1 = wpfPlot;      // 获取绘图控件
            _richTextBox = richTextBox;      // 富文本框

            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;      // 初始化 excel 相关协议

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
        }

        /// <summary>
        /// 鼠标左键抬起事件，用于确定线条的起点和终点
        /// </summary>
        private void WpfPlot1_MouseUp(object? sender, MouseButtonEventArgs e)
        {
            // 如果不是画线模式或添加文本模式，或不是鼠标左键点击，则忽略
            if ((!IsAddingLine && !IsAddingText) || e.ChangedButton != MouseButton.Left)
                return;

            var mousePos = e.GetPosition(WpfPlot1);
            Coordinates mouseCoordinates = WpfPlot1.Plot.GetCoordinates(new Pixel(mousePos.X * WpfPlot1.DisplayScale, mousePos.Y * WpfPlot1.DisplayScale));

            // 添加线条
            if (IsAddingLine)
            {
                if (!_lineStartPoint.HasValue)
                {
                    // 第一次点击：设置起点
                    _lineStartPoint = mouseCoordinates;
                    MessageHelper.Info("起点已确认，请点击设置终点");
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
        }

        /// <summary>
        /// 鼠标右键抬起事件，用于取消画线操作
        /// </summary>
        private void WpfPlot1_MouseRightButtonUp(object? sender, MouseButtonEventArgs e)
        {
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
                MessageHelper.Info("添加线条操作已取消");
            }

            // 取消添加文本
            if (IsAddingText)
            {
                IsAddingText = false;
                MessageHelper.Info("添加文本操作已取消");
            }
        }

        /// <summary>
        /// “添加线条”按钮的命令
        /// </summary>
        [RelayCommand]
        private void AddLine()
        {
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

            // 如果追踪模式未开启，则不执行任何操作
            if (!IsCrosshairVisible) return;

            // 更新十字轴的位置
            CrosshairPlot.Position = mouseCoordinates;

            // 更新十字轴上的文本标签以显示实时坐标 (保留3位小数的数字)
            CrosshairPlot.VerticalLine.Text = $"{mouseCoordinates.X:N3}";
            CrosshairPlot.HorizontalLine.Text = $"{mouseCoordinates.Y:N3}";

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

            var tempRTFfile = FileHelper.FindFileOrGetFirstWithExtension(
                    Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "Default",
                    card.TemplatePath), LanguageService.CurrentLanguage,".rtf");
            RtfHelper.LoadRtfToRichTextBox(tempRTFfile, _richTextBox);
        }

        /// <summary>
        /// 返回模板浏览模式
        /// </summary>
        [RelayCommand]
        private void BackToTemplateMode()
        {
            IsTemplateMode = true;
            IsPlotMode = false;

            // 清空绘图
            WpfPlot1?.Plot.Clear();
            WpfPlot1?.Refresh();

            // 清空图层树
            LayerTree.Clear();
            PropertyGridModel = nullObject;

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
        /// 当属性面板中的值改变时，此方法被调用实现更新
        /// </summary>
        private void PropertyGridModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender == null) return;

            bool needsRefresh = true;

            // 坐标轴单独处理
            if (sender is AxisDefinition axisDef)
            {
                needsRefresh = AxisPropertyChanged(axisDef, e);
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
        /// 坐标轴对象属性更新，触发
        /// </summary>
        /// <param name="axisDef"></param>
        private bool AxisPropertyChanged(AxisDefinition axisDef, PropertyChangedEventArgs e)
        {
            // 根据 AxisDefinition 的 Type 获取对应的坐标轴对象
            ScottPlot.IAxis? targetAxis = axisDef.Type switch
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
                // =================================================
                //                           坐标轴标题
                // =================================================
                // 标签文本内容
                case nameof(AxisDefinition.Label):
                    targetAxis.Label.Text = axisDef.Label.Get();
                    break;

                // 字体族
                case nameof(AxisDefinition.Family):
                    targetAxis.Label.FontName = axisDef.Family;
                    break;

                // 字体大小
                case nameof(AxisDefinition.Size):
                    targetAxis.Label.FontSize = axisDef.Size;
                    break;

                // 字体颜色
                case nameof(AxisDefinition.Color):
                    targetAxis.Label.ForeColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(axisDef.Color));
                    break;

                // 粗体
                case nameof(AxisDefinition.IsBold):
                    targetAxis.Label.Bold = axisDef.IsBold;
                    break;

                // 斜体
                case nameof(AxisDefinition.IsItalic):
                    targetAxis.Label.Italic = axisDef.IsItalic;
                    break;

                // 刻度间隔
                case nameof(AxisDefinition.TickInterval):
                    if (axisDef.TickInterval.HasValue && axisDef.TickInterval > 0)
                    {
                        targetAxis.TickGenerator = new ScottPlot.TickGenerators.NumericFixedInterval(axisDef.TickInterval.Value);
                    }
                    else
                    {
                        targetAxis.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic();
                    }
                    break;

                // =================================================
                //                               主刻度
                // =================================================
                // 主刻度长度
                case nameof(AxisDefinition.MajorTickLength):
                    targetAxis.MajorTickStyle.Length = axisDef.MajorTickLength;
                    break;

                // 主刻度宽度
                case nameof(AxisDefinition.MajorTickWidth):
                    targetAxis.MajorTickStyle.Width = axisDef.MajorTickWidth;
                    break;

                // 主刻度颜色
                case nameof(AxisDefinition.MajorTickWidthColor):
                    targetAxis.MajorTickStyle.Color = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(axisDef.MajorTickWidthColor));
                    break;

                // 主刻度抗锯齿
                case nameof(AxisDefinition.MajorTickAntiAlias):
                    targetAxis.MajorTickStyle.AntiAlias = axisDef.MajorTickAntiAlias;
                    break;

                // =================================================
                //                               次刻度
                // =================================================
                // 次刻度长度
                case nameof(AxisDefinition.MinorTickLength):
                    targetAxis.MinorTickStyle.Length = axisDef.MinorTickLength;
                    break;

                // 次刻度宽度
                case nameof(AxisDefinition.MinorTickWidth):
                    targetAxis.MinorTickStyle.Width = axisDef.MinorTickWidth;
                    break;

                // 次刻度颜色
                case nameof(AxisDefinition.MinorTickColor):
                    targetAxis.MinorTickStyle.Color = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(axisDef.MinorTickColor));
                    break;

                // 次刻度抗锯齿
                case nameof(AxisDefinition.MinorTickAntiAlias):
                    targetAxis.MinorTickStyle.AntiAlias = axisDef.MinorTickAntiAlias;
                    break;
                default:
                    return false;

                // =================================================
                //                               刻度标签
                // =================================================

                // 刻度标签字体
                case nameof(AxisDefinition.TickLableFamily):
                    targetAxis.TickLabelStyle.FontName = axisDef.TickLableFamily;
                    break;

                // 刻度标签字体大小
                case nameof(AxisDefinition.TickLablesize):
                    targetAxis.TickLabelStyle.FontSize = axisDef.TickLablesize;
                    break;

                // 刻度标签字体颜色
                case nameof(AxisDefinition.TickLablecolor):
                    targetAxis.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(axisDef.TickLablecolor));
                    break;

                // 刻度标签粗体
                case nameof(AxisDefinition.TickLableisBold):
                    targetAxis.TickLabelStyle.Bold = axisDef.TickLableisBold;
                    break;

                // 刻度标签斜体
                case nameof(AxisDefinition.TickLableisItalic):
                    targetAxis.TickLabelStyle.Italic = axisDef.TickLableisItalic;
                    break;
            }

            return true;

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
        /// 导入数据，根据类别分组，并为每个数据点创建独立图层
        /// </summary>
        [RelayCommand]
        private void ImportDataPlot()
        {
            // 检查当前模板是否有效
            if (CurrentTemplate?.Script == null)
            {
                MessageHelper.Error(LanguageService.Instance["load_valid_template_with_script"]);
                return;
            }

            var scriptDefinition = CurrentTemplate.Script;

            // 验证脚本配置
            if (string.IsNullOrEmpty(scriptDefinition.RequiredDataSeries) ||
                string.IsNullOrEmpty(scriptDefinition.ScriptBody))
            {
                MessageHelper.Error(LanguageService.Instance["incomplete_script_config"]);
                return;
            }

            // 弹窗让用户选择文件
            var openFileDialog = new VistaOpenFileDialog
            {
                Title = LanguageService.Instance["select_data_file"],
                Filter = LanguageService.Instance["data_file_filter"],
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            string filePath = openFileDialog.FileName;
            DataTable dataTable;

            // 使用 EPPlus 从 Excel 文件读取数据到 DataTable
            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null || worksheet.Dimension == null)
                    {
                        MessageHelper.Error(LanguageService.Instance["no_valid_workbook_or_data_found"]);
                        return;
                    }

                    dataTable = new DataTable();
                    foreach (var firstRowCell in worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column])
                    {
                        dataTable.Columns.Add(firstRowCell.Text);
                    }
                    for (var rowNumber = 2; rowNumber <= worksheet.Dimension.End.Row; rowNumber++)
                    {
                        var row = worksheet.Cells[rowNumber, 1, rowNumber, worksheet.Dimension.End.Column];
                        var newRow = dataTable.NewRow();
                        foreach (var cell in row)
                        {
                            newRow[cell.Start.Column - 1] = cell.Text;
                        }
                        dataTable.Rows.Add(newRow);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageHelper.Error(LanguageService.Instance["read_file_failed"] + ":" + ex.Message);
                return;
            }

            // 解析脚本中需要的数据列
            var requiredColumns = scriptDefinition.RequiredDataSeries
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();

            if (requiredColumns.Count < 2)
            {
                MessageHelper.Error(LanguageService.Instance["script_config_error_required_data_series"]);
                return;
            }

            // 验证Excel文件是否包含所需的列
            var missingColumns = requiredColumns.Where(col => !dataTable.Columns.Contains(col)).ToList();
            if (missingColumns.Any())
            {
                MessageHelper.Error(LanguageService.Instance["excel_missing_required_columns"] + ":" + string.Join(", ", missingColumns));
                return;
            }

            // 提取类别列（第一个列）和其他数据列
            string categoryColumn = requiredColumns[0];
            var dataColumns = requiredColumns.Skip(1).ToList();

            // 准备图层树的根节点和绘图样式
            var rootDataNode = LayerTree.FirstOrDefault(c => c.Name == LanguageService.Instance["data_point"]) as CategoryLayerItemViewModel;
            if (rootDataNode == null)
            {
                rootDataNode = new CategoryLayerItemViewModel(LanguageService.Instance["data_point"]);
                LayerTree.Add(rootDataNode);
            }
            rootDataNode.IsExpanded = true;

            var palette = new ScottPlot.Palettes.Category10();
            int colorIndex = 0;

            // 按类别列对数据进行分组
            var groupedData = dataTable.AsEnumerable()
                .Where(row => row[categoryColumn] != null && !string.IsNullOrEmpty(row[categoryColumn].ToString()))
                .GroupBy(row => row.Field<string>(categoryColumn));

            if (!groupedData.Any())
            {
                // 未能从文件中解析出有效的类别分组。请检查类别列数据
                MessageHelper.Warning(LanguageService.Instance["failed_to_parse_category_group"]);
                return;
            }

            // 遍历每个类别分组，为每个数据点创建图层和绘图对象
            foreach (var group in groupedData)
            {
                string categoryName = group.Key;
                if (string.IsNullOrWhiteSpace(categoryName)) continue;

                // 在图层树中为该类别创建一个父节点
                var categoryViewModel = new CategoryLayerItemViewModel(categoryName)
                {
                    IsExpanded = false
                };
                rootDataNode.Children.Add(categoryViewModel);

                // 为这个类别的所有点确定一个统一的颜色
                var groupColor = palette.GetColor(colorIndex++);

                // 循环处理该类别下的每一个数据点
                bool isFirstPointInGroup = true;
                foreach (DataRow row in group)
                {
                    try
                    {
                        // 使用脚本计算坐标
                        var coordinates = CalculateCoordinatesUsingScript(row, dataColumns, scriptDefinition.ScriptBody);
                        if (coordinates == null || coordinates.Length != 2)
                        {
                            continue; // 跳过计算失败的数据点
                        }

                        double x = coordinates[0];
                        double y = coordinates[1];

                        // 创建 ScatterDefinition 来定义此数据点的属性
                        var scatterDefinition = new ScatterDefinition
                        {
                            Color = groupColor.ToHex(),
                        };
                        scatterDefinition.StartAndEnd.X = x;
                        scatterDefinition.StartAndEnd.Y = y;

                        // 在ScottPlot图表上为这一个点添加散点图对象
                        var scatterPlot = WpfPlot1.Plot.Add.Scatter(new[] { x }, new[] { y });

                        WpfPlot1.Plot.MoveToBottom(scatterPlot);

                        scatterPlot.Color = groupColor;
                        scatterPlot.MarkerSize = scatterDefinition.Size;
                        scatterPlot.MarkerShape = scatterDefinition.MarkerShape;

                        // 只为每组的第一个点添加标签
                        if (isFirstPointInGroup)
                        {
                            scatterPlot.LegendText = categoryName;
                            isFirstPointInGroup = false;
                        }
                        else
                        {
                            scatterPlot.LegendText = null;
                        }

                        // 创建 ScatterLayerItemViewModel 来代表图层列表中的这一个点
                        var scatterLayerItem = new ScatterLayerItemViewModel(scatterDefinition)
                        {
                            Name = $"点 ({x:F2}, {y:F2})",
                            Plottable = scatterPlot,
                            IsVisible = true,
                        };

                        scatterLayerItem.PropertyChanged += (s, e) =>
                        {
                            // 订阅视图可见
                            if (e.PropertyName == nameof(ScatterLayerItemViewModel.IsVisible))
                            {
                                var layer = s as ScatterLayerItemViewModel;
                                if (layer?.Plottable != null)
                                {
                                    // 直接控制图表上对应元素的可见性
                                    layer.Plottable.IsVisible = layer.IsVisible;
                                    // 刷新
                                    WpfPlot1.Refresh();
                                }
                            }
                        };

                        // 将单个点的图层添加到对应类别的子节点下
                        categoryViewModel.Children.Add(scatterLayerItem);
                    }
                    catch (Exception ex)
                    {
                        // 记录但不中断处理过程
                        System.Diagnostics.Debug.WriteLine(LanguageService.Instance["error_calculating_data_point_coordinates"] + ex.Message);
                    }
                }
            }

            // 刷新图表和图例
            WpfPlot1.Plot.Legend.IsVisible = true;
            WpfPlot1.Plot.Axes.AutoScale();
            WpfPlot1.Refresh();
            MessageHelper.Success(LanguageService.Instance["data_import_successful"]);
        }

        /// <summary>
        /// 使用JavaScript脚本计算坐标
        /// </summary>
        /// <param name="dataRow">数据行</param>
        /// <param name="dataColumns">参与计算的数据列名</param>
        /// <param name="scriptBody">脚本内容</param>
        /// <returns>返回[x, y]坐标数组，失败时返回null</returns>
        private double[] CalculateCoordinatesUsingScript(DataRow dataRow, List<string> dataColumns, string scriptBody)
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

                // 判断返回结果类型
                if (result.IsArray())
                {
                    var array = result.AsArray();
                    if (array.Length >= 2)
                    {
                        var x = Convert.ToDouble(array[0].AsNumber());
                        var y = Convert.ToDouble(array[1].AsNumber());
                        return new double[] { x, y };
                    }
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

                        // TODO: 添加其他图层类型（如点、多边形）变暗逻辑
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

            // TODO:网格对象加载
            // TODO:绘图设置加载
            // TODO:图例设置加载
            // TODO:脚本设置加载


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

            // todo: 添加对其他绘图对象的处理
        }

        /// <summary>
        /// 根据当前的 LayerTree 状态，完全重绘 ScottPlot 图表
        /// 负责根据图层对象绘制图像，第一次加载对象
        /// </summary>
        public void RefreshPlotFromLayers()
        {
            if (WpfPlot1 == null) return;

            WpfPlot1.Plot.Clear();

            WpfPlot1.Plot.Add.Plottable(CrosshairPlot);

            var allNodes = FlattenTree(LayerTree); // 使用辅助方法获取所有节点

            // 单独处理坐标轴
            foreach (var item in allNodes)
            {
                if (item is AxisLayerItemViewModel axisLayer)
                {
                    ApplyAxisSettings(axisLayer);
                    continue;
                }

                item.Plottable = null; // 清空旧引用

                if (!item.IsVisible) continue;      // 如果图层不可见就跳过处理


                if (item is LineLayerItemViewModel lineLayer)
                {
                    CreateLinePlottable(lineLayer);
                }
                else if (item is TextLayerItemViewModel textLayer)
                {
                    CreateTextPlottable(textLayer);
                }
                else if (item is ScatterLayerItemViewModel scatterLayer)
                {

                    return;
                }
                // todo: 添加对其他绘图对象的处理
            }


            // 处理图例
            WpfPlot1.Plot.Legend.Alignment = CurrentTemplate.Info.Legend.Alignment;
            WpfPlot1.Plot.Legend.FontName = (LanguageService.CurrentLanguage == "zh-CN") ?
                "微软雅黑" : CurrentTemplate.Info.Legend.Font;
            WpfPlot1.Plot.Legend.Orientation = CurrentTemplate.Info.Legend.Orientation;
            WpfPlot1.Plot.Legend.IsVisible = CurrentTemplate.Info.Legend.IsVisible;

            if(CurrentTemplate.Info.Title.Label.Translations.Count != 0)
            {
                // 处理标题
                WpfPlot1.Plot.Axes.Title.Label.Text = CurrentTemplate.Info.Title.Label.Get();
                // 图表标题字体
                WpfPlot1.Plot.Axes.Title.Label.FontName = (LanguageService.CurrentLanguage == "zh-CN") ?
                    "微软雅黑" : CurrentTemplate.Info.Title.Family;
                // 图表标题字体大小
                WpfPlot1.Plot.Axes.Title.Label.FontSize = CurrentTemplate.Info.Title.Size;
                // 图表标题字体颜色
                WpfPlot1.Plot.Axes.Title.Label.ForeColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(CurrentTemplate.Info.Title.Color));
                // 图表标题粗体
                WpfPlot1.Plot.Axes.Title.Label.Bold = CurrentTemplate.Info.Title.IsBold;
                // 图表标题斜体
                WpfPlot1.Plot.Axes.Title.Label.Italic = CurrentTemplate.Info.Title.IsItalic;
            }
            

            // 获取脚本对象
            CurrentScript = CurrentTemplate.Script;

            WpfPlot1.Plot.Axes.AutoScale();
            WpfPlot1.Refresh();
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

            // 如果坐标轴可见，才应用其他详细样式
            if (axisLayer.IsVisible)
            {
                if (LanguageService.CurrentLanguage == "zh-CN")
                {
                    targetAxis.Label.FontName = "微软雅黑";
                }
                else
                {
                    targetAxis.Label.FontName = axisDef.Family;
                }
                targetAxis.Label.Text = axisDef.Label.Get();
                targetAxis.Label.FontSize = axisDef.Size;
                targetAxis.Label.ForeColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(axisDef.Color));
                targetAxis.Label.Bold = axisDef.IsBold;
                targetAxis.Label.Italic = axisDef.IsItalic;

                if (axisDef.TickInterval.HasValue && axisDef.TickInterval > 0)
                {
                    targetAxis.TickGenerator = new ScottPlot.TickGenerators.NumericFixedInterval(axisDef.TickInterval.Value);
                }
                else
                {
                    targetAxis.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic();
                }
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
            textPlot.LabelFontName = (LanguageService.CurrentLanguage == "zh-CN") ? "微软雅黑" : textDef.Family;
            textPlot.LabelText = textDef.Content.Get();
            textPlot.LabelFontSize = textDef.Size;
            textPlot.LabelRotation = textDef.Rotation;
            textPlot.LabelFontColor = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex((textDef.Color)));
            textPlot.LabelBold = textDef.IsBold;
            textPlot.LabelItalic = textDef.IsItalic;

            // 存储引用
            textLayer.Plottable = textPlot;
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
        /// 标题属性
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
        /// 清除所有导入的数据点
        /// </summary>
        [RelayCommand]
        private async Task ClearImportedDataAsync()
        {
            bool isConfirmed = await MessageHelper.ShowAsyncDialog(
                LanguageService.Instance["confirm_clear_data_points"],
                LanguageService.Instance["Cancel"],
                LanguageService.Instance["Confirm"]);

            if(isConfirmed )
            {
                // 在图层列表中查找名为 "数据点" 的根分类图层
                var dataRootNode = LayerTree.FirstOrDefault(node => node is CategoryLayerItemViewModel vm && vm.Name == "数据点");

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
        /// 保存当前底图模板
        /// </summary>
        [RelayCommand]
        private void SaveBaseMap()
        {
            // 确保有模板和路径可供保存
            if (CurrentTemplate == null || string.IsNullOrWhiteSpace(_currentTemplateFilePath))
            {
                // 没有可保存的模板或未指定保存路径。请先新建或打开一个模板
                MessageHelper.Error(LanguageService.Instance["no_template_or_path_specified"]);
                return;
            }

            // 清空模板中原有的动态绘图元素列表
            //    (坐标轴、标题等对象是直接修改属性，不需要清空)
            CurrentTemplate.Info.Lines.Clear();
            CurrentTemplate.Info.Texts.Clear();
            CurrentTemplate.Info.Annotations.Clear();
            CurrentTemplate.Info.Points.Clear();

            // 遍历当前的图层列表，收集所有图元信息
            var allLayers = FlattenTree(LayerTree); // 使用您已有的辅助方法获取所有图层
            foreach (var layer in allLayers)
            {
                // 使用 switch 匹配不同类型的图层
                switch (layer)
                {
                    // 如果是线条图层
                    case LineLayerItemViewModel lineLayer:
                        CurrentTemplate.Info.Lines.Add(lineLayer.LineDefinition);
                        break;

                    // 如果是文本图层
                    case TextLayerItemViewModel textLayer:
                        CurrentTemplate.Info.Texts.Add(textLayer.TextDefinition);
                        break;

                    // 如果是注释图层
                    case AnnotationLayerItemViewModel annotationLayer:
                        CurrentTemplate.Info.Annotations.Add(annotationLayer.AnnotationDefinition);
                        break;

                        // TODO:其他图元处理
                }
            }

            // 将更新后的 CurrentTemplate 对象序列化为 JSON 字符串
            var options = new JsonSerializerOptions
            {
                // 美化输出，使其易于阅读
                WriteIndented = true,
                // 解决中文字符被编码为 \uXXXX 的问题
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                // 将枚举类型转换为字符串而不是数字
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };

            try
            {
                string jsonString = JsonSerializer.Serialize(CurrentTemplate, options);

                // 将 JSON 字符串写入文件
                // 确保目录存在
                string directoryPath = Path.GetDirectoryName(_currentTemplateFilePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                File.WriteAllText(_currentTemplateFilePath, jsonString);

                // 更新或生成新的缩略图
                string thumbnailPath = Path.Combine(directoryPath, "thumbnail.jpg");
                WpfPlot1.Plot.Save(thumbnailPath, 400, 300); // 保存一个400x300的缩略图

                // 模板已成功保存！
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
    }
}