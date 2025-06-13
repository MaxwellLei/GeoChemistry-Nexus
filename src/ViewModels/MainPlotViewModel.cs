using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using Jint;
using Jint.Runtime;
using Microsoft.Win32;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using ScottPlot;
using ScottPlot.AxisPanels;
using ScottPlot.Colormaps;
using ScottPlot.Grids;
using ScottPlot.Plottables;
using ScottPlot.TickGenerators;
using ScottPlot.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using System.Windows.Shapes;
using static OfficeOpenXml.ExcelErrorValue;
using static SkiaSharp.HarfBuzz.SKShaper;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace GeoChemistryNexus.ViewModels
{
    public partial class MainPlotViewModel : ObservableObject
    {
        // 注册绘图模板列表
        private PlotTemplateRegistry _registry;

        // 图层选中列表
        public IList<PlotItemModel> _previousSelectedItems;

        // 当前选中模板
        public static TreeNode _previousSelectedNode;

        // 添加一个标志来防止递归更新属性
        private bool _isUpdatingLineWidth = false;

        // 映射字典
        Dictionary<string, string> translations;

        // 绘图控件
        private WpfPlot WpfPlot1;

        // 描述控件
        private RichTextBox _richTextBox;

        // 定位轴显示
        public static Crosshair crosshair;

        // 选中对象提示
        public static ScottPlot.Plottables.Marker myHighlightMarker; // 高亮标记

        // 选中对象信息
        public static BaseInfo _selectedBaseInfo;

        // 绘图模板列表
        [ObservableProperty]
        private TreeNode _rootTreeNode;

        // 边界列表
        [ObservableProperty]
        public ObservableCollection<PlotItemModel> _basePlotItems;

        // 注释列表
        [ObservableProperty]
        private ObservableCollection<PlotItemModel> _baseTextItems;

        // 坐标轴列表
        [ObservableProperty]
        private ObservableCollection<PlotItemModel> _axesList;

        //数据列表
        [ObservableProperty]
        private ObservableCollection<PlotItemModel> _baseDataItems;

        // 切换属性对象
        [ObservableProperty]
        private int _switchLayer;

        // 自定义底图
        [ObservableProperty]
        public BasePlotConfig _basePlotConfigObject;

        // 是否是编辑模式
        [ObservableProperty]
        private bool isEditMode = false;

        // 是否是绘图模式
        [ObservableProperty]
        private bool isPlotMode = true;

        // 需要参数内容
        [ObservableProperty]
        private string _needParamContent;

        // Js 脚本内容
        [ObservableProperty]
        private string _jsScriptContent;

        // 新建底图名称
        [ObservableProperty]
        private string _newBasePlotName;

        // 底图类别
        [ObservableProperty]
        private string _basePlotType;

        // 底图对应的语言
        [ObservableProperty]
        private string _basePlotLanguage;


        /// <summary>
        /// ========================================公共属性
        /// </summary>

        // 绘图对象是否可见
        [ObservableProperty]
        private bool _plotVisible;

        // 绘图对象 宽度-大小-轴标题大小
        [ObservableProperty]
        private float _plotWidth;

        // 线条/字体类型
        [ObservableProperty]
        private int _plotType;

        // 绘图对象绘制颜色-坐标轴标题颜色
        [ObservableProperty]
        private ScottPlot.Color _plotColor;

        // 当前文本-标题字体
        [ObservableProperty]
        private int _plotTextFontName;

        // 文本字体-轴标题列表
        [ObservableProperty]
        private List<string> _plotTextFontNames;

        // 当前文本-轴标题内容
        [ObservableProperty]
        private string _plotTextContent;


        /// <summary>
        /// ========================================坐标轴属性
        /// </summary>

        // 显示刻度
        [ObservableProperty]
        private bool _reverseAxis;

        // 显示次刻度
        [ObservableProperty]
        private bool _SecondTickShow;

        // 刻度字体
        [ObservableProperty]
        private int _axisTickFontName;

        // 刻度字体大小
        [ObservableProperty]
        private float _axisTickFontSize;

        // 主刻度间距
        [ObservableProperty]
        private double _axisTickSpan;

        // 刻度上限
        [ObservableProperty]
        private double _axisTickUpLimit;

        // 刻度下限
        [ObservableProperty]
        private double _axisTickDownLimit;

        // 刻度颜色
        [ObservableProperty]
        private ScottPlot.Color _axisPlotColor;

        /// <summary>
        /// ========================================图例设置
        /// </summary>

        // 是否显示图例
        [ObservableProperty]
        private bool _showLegends;

        // 图例位置
        [ObservableProperty]
        private int _legendsLocation;

        // 图例方向
        [ObservableProperty]
        private int _legendsO;

        // 图例字体
        [ObservableProperty]
        private int _legendsFonts;

        // 图例字体大小
        [ObservableProperty]
        private float _legendsFontSize;

        // 图例字体颜色
        [ObservableProperty]
        private ScottPlot.Color _legendsFontColor;

        /// <summary>
        /// ========================================绘图设置
        /// </summary>

        // 绘图标题
        [ObservableProperty]
        private string _axisTitle;

        // 标题字体大小
        [ObservableProperty]
        private float _axisTitleFontSize;

        // 标题字体
        [ObservableProperty]
        private int _axisTitleFontName;

        // 轴标题颜色
        [ObservableProperty]
        private ScottPlot.Color _axisTitleColor;

        // 坐标x轴标题
        [ObservableProperty]
        private string _xAxisTitle;

        // 坐标y轴标题
        [ObservableProperty]
        private string _yAxisTitle;

        // 轴标题字体大小
        [ObservableProperty]
        private float _axisXYTitleFontSize;

        // 轴标题字体
        [ObservableProperty]
        private int _axisXYTitleFontName;

        // 轴标题颜色
        [ObservableProperty]
        private ScottPlot.Color _axisXYTitleColor;

        /// <summary>
        /// ========================================背景设置
        /// </summary>

        // 显示主网格
        [ObservableProperty]
        private bool _firstGridShow;

        // 主网格颜色
        [ObservableProperty]
        private ScottPlot.Color _firstGridColor;

        // 主网格宽度
        [ObservableProperty]
        private float _firstGridWidth;

        // 显示次网格
        [ObservableProperty]
        private bool _secondGridShow;

        // 次网格颜色
        [ObservableProperty]
        private ScottPlot.Color _secondGridColor;

        // 次网格宽度
        [ObservableProperty]
        private float _secondGridWidth;

        // 反转填充颜色
        [ObservableProperty]
        private bool _swichtFillColor = false;

        // 填充区域颜色1
        [ObservableProperty]
        private ScottPlot.Color _gridFillColor1;

        // 填充区域颜色2
        [ObservableProperty]
        private ScottPlot.Color _gridFillColor2;

        /// <summary>
        /// =========================================这是分隔符
        /// </summary>
        /// 

        // 是否显示边界属性
        [ObservableProperty]
        private bool _plotLineShow = false;

        // 是否显示文本属性
        [ObservableProperty]
        private bool _plotTextShow = false;

        // 是否显示数据点属性
        [ObservableProperty]
        private bool _plotDataShow = false;

        // 是否显示坐标轴属性
        [ObservableProperty]
        private bool _plotAxisShow = false;

        // 是否显示绘图设置属性
        [ObservableProperty]
        private bool _plotMainShow = false;

        // 是否显示图例设置属性
        [ObservableProperty]
        private bool _plotLegendShow = false;

        // 是否显示脚本设置属性
        [ObservableProperty]
        private bool _plotScriptShow = false;

        // 是否显示弹窗
        [ObservableProperty]
        private bool _plotPopupShow = false;

        // 自定义底图
        /// <summary>
        /// =========================================自定义底图
        /// </summary>
        /// 

        // 起始点1
        [ObservableProperty]
        private double _x1;

        // 起始点1
        [ObservableProperty]
        private double _y1;

        // 起始点2
        [ObservableProperty]
        private double _x2;

        // 起始点2
        [ObservableProperty]
        private double _y2;


        // 初始化
        public MainPlotViewModel(WpfPlot wpfPlot, RichTextBox richTextBox)
        {
            WpfPlot1 = wpfPlot;     // 获取绘图控件
            _richTextBox = richTextBox;     // 获取内容控件
            BasePlotItems = new ObservableCollection<PlotItemModel>();      // 初始化边界列表
            BaseTextItems = new ObservableCollection<PlotItemModel>();      // 初始化注释列表
            BaseDataItems = new ObservableCollection<PlotItemModel>();      // 初始化数据列表
            _axesList = new();  // 初始化坐标轴列表
            _previousSelectedItems = new List<PlotItemModel>();     // 初始化图层选中对象
            _plotTextFontNames = new List<string>();        // 字体集合

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;     // 初始化 excel 相关协议

            _registry = new PlotTemplateRegistry();
            RegisterPlotTemplates();    // 注册绘图模板
            


            // 获取系统所有字体
            PlotTextFontNames = System.Drawing.FontFamily.Families
                .Select(f => f.Name)
                .OrderBy(name => name)
                .ToList();
        }

        // 注册绘图模板
        public void RegisterPlotTemplates(bool editList = false)
        {
            // 加载底图列表
            var listPath = System.IO.Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "PlotList.json");
            if (editList)
            {
                listPath = System.IO.Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "PlotListCustom.json");
            }
            

            // todo: 这里应该判断一下底图列表是否存在，不存在就提示有问题，然后检查是否存在底图文件如果存在可以重建，不存在就警告

            // 反序列化底图列表
            PlotListConfig plotListConfigs = PlotLoader.LoadBasePlotList(listPath);

            // 如果不是空列表
            if(plotListConfigs.listNodeConfigs.Count != 0)
            {
                RootTreeNode = _registry.GenerateListFromConfig(plotListConfigs);
            }
            else
            {
                RootTreeNode = new TreeNode();
            }
            //var tempTitle1 = I18n.GetString("IgneousRock");      // 岩浆岩
            //var tempTitle12 = I18n.GetString("TectonicSetting");      // 构造环境
            //var tempTitle13 = I18n.GetString("Basalt");      // 玄武岩
            //var tempTitle2 = I18n.GetString("RockClassification");      // 岩石分类
            //var tempTitle31 = I18n.GetString("GTMP");      // 温度绘图

            //// 岩浆岩-构造环境-玄武岩
            //_registry.RegisterTemplate(new[] { tempTitle1, tempTitle12, tempTitle13, "Pearce and Gale (1977)" }, "Ti-Zr-Y",
            //    NormalPlotTemplate.Pearce_and_Gale_1977, NormalPlotMethod.Vermessch_2006_PlotAsync,
            //    "Vermessch_2006.rtf", new string[] { "Group", "Ti", "Zr", "Y" });
            //_registry.RegisterTemplate(new[] { tempTitle1, tempTitle12, tempTitle13, "Vermeesch (2006)" }, "Major Elements (-Fe)",
            //    NormalPlotTemplate.Vermessch_2006, NormalPlotMethod.Vermessch_2006_PlotAsync,
            //    "Vermessch_2006.rtf", new string[] { "Group", "SiO2", "Al2O3", "TiO2", "CaO", "MgO", "MnO", "K2O", "Na2O" });
            //_registry.RegisterTemplate(new[] { tempTitle1, tempTitle12, tempTitle13, "Vermeesch (2006)" }, "TiO2-Zr-Y-Sr",
            //    NormalPlotTemplate.Vermessch_2006_b, NormalPlotMethod.Vermessch_2006_b_PlotAsync,
            //    "Vermessch_2006_b.rtf", new string[] { "Group", "TiO2", "Zr", "Y", "Sr", });

            ////_registry.RegisterTemplate(new[] { "岩浆岩", "构造环境", "玄武岩", "Vermeesch (2006)" }, "Ti-Y",
            ////    NormalPlotTemplate.Vermessch_2006_c, NormalPlotMethod.Vermessch_2006_b_PlotAsync,
            ////    "Vermessch_2006_c.rtf", new string[] { "Group", "TiO2", "Zr", "Y", "Sr", });

            //_registry.RegisterTemplate(new[] { tempTitle1, tempTitle12, tempTitle13, "Saccani (2015)" }, "Th-Nb",
            //    NormalPlotTemplate.Saccani_2015, NormalPlotMethod.Saccani_2015_PlotAsync,
            //    "Saccani_2015.rtf", new string[] { "Group", "Th", "Nb" });
            //_registry.RegisterTemplate(new[] { tempTitle1, tempTitle12, tempTitle13, "Saccani (2015)" }, "Yb-Dy",
            //    NormalPlotTemplate.Saccani_2015_b, NormalPlotMethod.Saccani_2015_b_PlotAsync,
            //    "Saccani_2015.rtf", new string[] { "Group", "Yb", "Dy" });
            //_registry.RegisterTemplate(new[] { tempTitle1, tempTitle12, tempTitle13, "Saccani (2015)" }, "Ce-Dy-Yb",
            //    NormalPlotTemplate.Saccani_2015_c, NormalPlotMethod.Saccani_2015_c_PlotAsync,
            //    "Saccani_2015.rtf", new string[] { "Group", "Ce", "Yb", "Dy" });

            //// 岩浆岩 - 岩石分类 - TAS
            //_registry.RegisterTemplate(new[] { tempTitle1, tempTitle2 }, "TAS",
            //    NormalPlotTemplate.TAS, NormalPlotMethod.TAS_PlotAsync,
            //    "Saccani_2015.rtf", new string[] { "Group", "K2O", "Na2O", "SiO2" });

            //// 其他 - 温度绘图 - 毒砂
            //_registry.RegisterTemplate(new[] { I18n.GetString("Other"), tempTitle31 }, I18n.GetString("Arsenopyrite"),
            //    NormalPlotTemplate.ArsenicT, NormalPlotMethod.TAS_PlotAsync,
            //    "Saccani_2015.rtf", new string[] { "Group", "K2O", "Na2O", "SiO2" });
            //RootTreeNode = _registry.GenerateTreeStructure();       // 注册模板列表


            // 映射字典
            translations = new Dictionary<string, string>
            {
                { "Left", LanguageService.Instance["PlotLeftAxies"]},
                { "Bottom", LanguageService.Instance["PlotBottomAxies"]}
            };

            // 刷新图层列表
            PopulatePlotItems((List<IPlottable>)WpfPlot1.Plot.GetPlottables());
        }

        // 设置显示属性
        private void SetTrue(string key)
        {
            // 先全部重置为false
            PlotLineShow = false;
            PlotTextShow = false;
            PlotDataShow = false;
            PlotAxisShow = false;
            PlotMainShow = false;
            PlotLegendShow = false;
            PlotScriptShow = false;
            PlotPopupShow = false;

            if (key == null) return;

            // 根据key设置对应属性为true
            switch (key)
            {
                case "PlotLine": PlotLineShow = true; break;
                case "Scatter": PlotDataShow = true; break;
                case "Axis": PlotAxisShow = true; break;
                case "Legend": PlotLegendShow = true; break;
                case "Main": PlotMainShow = true; break;
                case "Script": PlotScriptShow = true; break;
                case "Popup": PlotPopupShow = true; break;
                default: PlotTextShow = true; break;
            }
        }

        // 查找字体
        private int FindFontNameIndex(string currentFontName)
        {
            // 如果当前字体名为空，返回0或其他默认值
            if (string.IsNullOrEmpty(currentFontName))
                return 0;

            // 在字体列表中查找当前字体的索引
            int index = PlotTextFontNames.FindIndex(name =>
                name.Equals(currentFontName, StringComparison.OrdinalIgnoreCase));

            // 如果找不到，返回0或其他默认值
            return index >= 0 ? index : 0;
        }

        // 去掉转义字符
        private string RemoveNewlines(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str), "输入字符串不能为 null。");
            }

            return str.Replace("\n", string.Empty);
        }

        // 字典映射
        private string Translate(string input, Dictionary<string, string> translations)
        {

            if (translations.TryGetValue(input, out string translation))
            {
                return translation;
            }
            else
            {
                return "映射Key未找到"; // "Translation not found"
            }
        }

        // 刷新坐标轴列表
        private void GetAxisList()
        {
            AxesList.Clear();
            var testdata = WpfPlot1.Plot.Axes.GetAxes();


            foreach (var axis in WpfPlot1.Plot.Axes.GetAxes())
            {
                var test = axis.Edge.ToString();
                var test2 = Equals(axis.Edge.ToString(), "Top");
                if (axis.Edge.ToString() != "Right" && axis.Edge.ToString() != "Top")
                {
                    AxesList.Add(new PlotItemModel()
                    {
                        Name = Translate(axis.Edge.ToString(), translations),
                        ObjectType = "Axis",
                        Plottable = axis,
                    });
                }
            }
        }

        // 刷新图层列表
        public void PopulatePlotItems(List<IPlottable> plottables)
        {
            if (plottables == null)
                throw new ArgumentNullException(nameof(plottables));

            BasePlotItems.Clear();
            BaseTextItems.Clear();
            BaseDataItems.Clear();
            foreach (IPlottable plottable in plottables)
            {
                // 获取绘图名称
                string plottableType = plottable.GetType().Name;
                string displayName = "Error Type";

                if (plottableType == "LinePlot")
                {
                    displayName = LanguageService.Instance["PlotBoder"];
                    BasePlotItems.Add(new PlotItemModel
                    {
                        Plottable = plottable,
                        Name = displayName,
                        ObjectType = plottableType
                    });
                }
                if (plottableType == "Text")
                {
                    // 添加注释对象
                    displayName = RemoveNewlines(((ScottPlot.Plottables.Text)plottable).LabelText);
                    BaseTextItems.Add(new PlotItemModel
                    {
                        Plottable = plottable,
                        Name = displayName,
                        ObjectType = plottableType
                    });
                }
                if (plottableType == "Scatter")
                {
                    // 添加数据对象
                    displayName = ((Scatter)plottable).LegendText;
                    BaseDataItems.Add(new PlotItemModel
                    {
                        Plottable = plottable,
                        Name = displayName,
                        ObjectType = plottableType
                    });
                }



            }
        }

        // 检查字符完全匹配
        private int ContainsAllStrings(DataTable dataTable, string[] stringsToCheck)
        {
            if (dataTable == null || stringsToCheck == null || stringsToCheck.Length == 0)
            {
                return -1;      // 输入数据参数错误
            }
            // 检查 DataTable 是否有列
            if (dataTable.Columns.Count == 0 || dataTable.Rows.Count == 0)
            {
                return 0;       // DataTable 内容为空
            }
            // 遍历要检查的字符串
            foreach (var str in stringsToCheck)
            {
                bool found = false;
                // 遍历所有列名
                foreach (DataColumn column in dataTable.Columns)
                {
                    // 检查当前列名是否完全匹配目标字符串（区分大小写）
                    if (column.ColumnName == str)
                    {
                        found = true;
                        break;
                    }
                }
                // 如果当前字符串没有在列名中找到，返回 false
                if (!found)
                {
                    return -2;      // 匹配失败
                }
            }
            // 所有字符串都完全匹配
            return 1;
        }

        // 当进入编辑模式
        partial void OnIsEditModeChanged(bool value)
        {
            Refresh(true,true);
        }

        // 改变 选择图层 内容
        partial void OnSwitchLayerChanged(int value)
        {
            // 取消属性显示
            LayersSelection(null);
            // 取消属性面板展示
            SetTrue(null);
            // 刷新图层列表
            PopulatePlotItems((List<IPlottable>)WpfPlot1.Plot.GetPlottables());
            // 刷新坐标轴列表
            GetAxisList();
        }

        // 改变 Text 文本内容
        partial void OnPlotTextContentChanged(string value)
        {
            // 如果是在更新属性值，则不执行更新_plotLineType
            if (_isUpdatingLineWidth)
                return;

            // 更新当前选中图层的线宽
            if (_previousSelectedItems != null && _previousSelectedItems.Any())
            {
                foreach (var item in _previousSelectedItems)
                {
                    if (item.ObjectType == "Text")
                    {
                        var text = (ScottPlot.Plottables.Text)item.Plottable;
                        text.LabelText = value;
                    }
                    else if (item.ObjectType == "Axis")
                    {
                        var tempAxis = (IAxis)item.Plottable;
                        tempAxis.Label.Text = value;
                    }
                }
                // 刷新图形
                WpfPlot1.Refresh();
            }
        }

        // 改变 Text 文本字体
        partial void OnPlotTextFontNameChanged(int value)
        {
            // 如果是在更新属性值，则不执行更新_plotLineType
            if (_isUpdatingLineWidth)
                return;

            // 更新当前选中图层的线宽
            if (_previousSelectedItems != null && _previousSelectedItems.Any())
            {
                foreach (var item in _previousSelectedItems)
                {
                    if (item.ObjectType == "Text")
                    {
                        var text = (ScottPlot.Plottables.Text)item.Plottable;
                        text.LabelFontName = PlotTextFontNames[value];
                    }
                    else if (item.ObjectType == "Axis")
                    {
                        var tempAxis = (IAxis)item.Plottable;
                        tempAxis.Label.FontName = PlotTextFontNames[value];
                    }
                }
                // 刷新图形
                WpfPlot1.Refresh();
            }
        }

        // 改变 绘图对象 可见性
        partial void OnPlotVisibleChanged(bool value)
        {
            // 如果是在更新属性值，则不执行更新_plotLineType
            if (_isUpdatingLineWidth)
                return;

            // 更新当前选中图层的线宽
            if (_previousSelectedItems != null && _previousSelectedItems.Any())
            {
                foreach (var item in _previousSelectedItems)
                {
                    if (item.ObjectType == "LinePlot")
                    {
                        var linePlot = (LinePlot)item.Plottable;
                        linePlot.IsVisible = value;
                    }
                    else if (item.ObjectType == "Text")
                    {
                        var text = (ScottPlot.Plottables.Text)item.Plottable;
                        text.IsVisible = value;
                    }
                    else if (item.ObjectType == "Scatter")
                    {
                        var scatter = (ScottPlot.Plottables.Scatter)item.Plottable;
                        scatter.IsVisible = value;
                    }
                    else if (item.ObjectType == "Axis")
                    {
                        var tempAxis = (IAxis)item.Plottable;
                        tempAxis.IsVisible = value;
                    }
                }
                // 刷新图形
                WpfPlot1.Refresh();
            }

        }

        // 改变 绘图对象 类型
        partial void OnPlotTypeChanged(int value)
        {
            // 如果是在更新属性值，则不执行更新_plotLineType
            if (_isUpdatingLineWidth)
                return;

            // 更新当前选中图层的线宽
            if (_previousSelectedItems != null && _previousSelectedItems.Any())
            {
                foreach (var item in _previousSelectedItems)
                {
                    if (item.ObjectType == "LinePlot")
                    {
                        var linePlot = (LinePlot)item.Plottable;
                        if (PlotType == 0) { linePlot.LineStyle.Pattern = LinePattern.Solid; }
                        if (PlotType == 1) { linePlot.LineStyle.Pattern = LinePattern.Dashed; }
                        if (PlotType == 2) { linePlot.LineStyle.Pattern = LinePattern.DenselyDashed; }
                        if (PlotType == 3) { linePlot.LineStyle.Pattern = LinePattern.Dotted; }
                    }

                    if (item.ObjectType == "Text")
                    {
                        var text = (ScottPlot.Plottables.Text)item.Plottable;
                        if (PlotType == 0) { text.LabelStyle.Bold = false; text.LabelStyle.Italic = false; }
                        if (PlotType == 1) { text.LabelStyle.Bold = true; }
                        if (PlotType == 2) { text.LabelStyle.Italic = true; }
                    }

                    if (item.ObjectType == "Scatter")
                    {
                        var scatter = (ScottPlot.Plottables.Scatter)item.Plottable;
                        if (PlotType == 0) { scatter.MarkerShape = MarkerShape.FilledCircle; }
                        if (PlotType == 1) { scatter.MarkerShape = MarkerShape.OpenCircle; }
                        if (PlotType == 2) { scatter.MarkerShape = MarkerShape.FilledSquare; }
                        if (PlotType == 3) { scatter.MarkerShape = MarkerShape.FilledTriangleUp; }
                        if (PlotType == 4) { scatter.MarkerShape = MarkerShape.FilledTriangleDown; }
                        if (PlotType == 5) { scatter.MarkerShape = MarkerShape.FilledDiamond; }
                        if (PlotType == 6) { scatter.MarkerShape = MarkerShape.Eks; }
                        if (PlotType == 7) { scatter.MarkerShape = MarkerShape.Cross; }
                        if (PlotType == 8) { scatter.MarkerShape = MarkerShape.Asterisk; }
                    }
                }
                // 刷新图形
                WpfPlot1.Refresh();
            }
        }

        // 改变 绘图对象 宽度-大小
        partial void OnPlotWidthChanged(float value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            // 更新当前选中图层的线宽
            if (_previousSelectedItems != null && _previousSelectedItems.Any())
            {
                foreach (var item in _previousSelectedItems)
                {
                    if (item.ObjectType == "LinePlot")
                    {
                        var linePlot = (LinePlot)item.Plottable;
                        linePlot.LineWidth = value;
                    }
                    if (item.ObjectType == "Text")
                    {
                        var text = (ScottPlot.Plottables.Text)item.Plottable;
                        text.LabelFontSize = value;
                    }
                    if (item.ObjectType == "Scatter")
                    {
                        var scatter = (ScottPlot.Plottables.Scatter)item.Plottable;
                        scatter.MarkerSize = value;
                    }
                    else if (item.ObjectType == "Axis")
                    {
                        var tempAxis = (IAxis)item.Plottable;
                        tempAxis.Label.FontSize = value;
                    }
                }
                // 刷新图形
                WpfPlot1.Refresh();
            }
        }

        // 改变 绘图对象 颜色
        partial void OnPlotColorChanged(ScottPlot.Color value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            // 更新当前选中图层的颜色
            if (_previousSelectedItems != null && _previousSelectedItems.Any())
            {
                foreach (var item in _previousSelectedItems)
                {
                    if (item.ObjectType == "LinePlot")
                    {
                        var linePlot = (LinePlot)item.Plottable;
                        linePlot.LineColor = value;
                    }
                    if (item.ObjectType == "Text")
                    {
                        var text = (ScottPlot.Plottables.Text)item.Plottable;
                        text.LabelFontColor = value;
                    }
                    if (item.ObjectType == "Scatter")
                    {
                        var scatter = (ScottPlot.Plottables.Scatter)item.Plottable;
                        scatter.MarkerColor = value;
                    }
                    else if (item.ObjectType == "Axis")
                    {
                        var tempAxis = (IAxis)item.Plottable;
                        tempAxis.Label.ForeColor = value;
                    }
                }
                // 刷新图形
                WpfPlot1.Refresh();
            }
        }

        // 主刻度是否显示
        partial void OnReverseAxisChanged(bool value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            // 更新当前选中图层的颜色
            if (_previousSelectedItems != null && _previousSelectedItems.Any())
            {
                foreach (var item in _previousSelectedItems)
                {
                    var tempAxis = (IAxis)item.Plottable;
                    if (value == true)
                    {
                        tempAxis.MajorTickStyle = new TickMarkStyle()
                        {
                            Length = 0,
                            Width = 0,
                        };
                    }
                    else
                    {
                        tempAxis.MajorTickStyle = new TickMarkStyle()
                        {
                            Length = 4,
                            Width = 1,
                            Color = ScottPlot.Colors.Black,
                        };
                    }

                }
                // 刷新图形
                WpfPlot1.Refresh();
            }
        }

        // 次刻度是否显示
        partial void OnSecondTickShowChanged(bool value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            // 更新当前选中图层的颜色
            if (_previousSelectedItems != null && _previousSelectedItems.Any())
            {
                foreach (var item in _previousSelectedItems)
                {
                    var tempAxis = (IAxis)item.Plottable;
                    if (value == true)
                    {
                        List<Tick> wwee = tempAxis.TickGenerator.Ticks.ToList();
                        wwee.Add(new Tick(-9, "-9", false));
                        var tewew = tempAxis.TickGenerator;
                        if (tempAxis.TickGenerator is ScottPlot.TickGenerators.NumericFixedInterval)
                        {
                            ((ScottPlot.TickGenerators.NumericFixedInterval)tempAxis.TickGenerator).Ticks = wwee.ToArray();
                        }

                        //tempAxis.SetTicks(wwee.Select(tick => tick.Position).ToArray(), wwee.Select(tick => tick.Label).ToArray()) ;
                        //tempAxis.MinorTickStyle = new TickMarkStyle()
                        //{
                        //    Length = 0,
                        //    Width = 0,
                        //};
                    }
                    else
                    {
                        tempAxis.MinorTickStyle = new TickMarkStyle()
                        {
                            Length = 2,
                            Width = 1,
                            Color = ScottPlot.Colors.Black,
                        };
                    }

                }
                // 刷新图形
                WpfPlot1.Refresh();
            }
        }

        // 改变 刻度轴字体
        partial void OnAxisTickFontNameChanged(int value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            // 更新当前选中图层的颜色
            if (_previousSelectedItems != null && _previousSelectedItems.Any())
            {
                foreach (var item in _previousSelectedItems)
                {
                    var tempAxis = (IAxis)item.Plottable;
                    tempAxis.TickLabelStyle.FontName = PlotTextFontNames[value];
                }
                // 刷新图形
                WpfPlot1.Refresh();
            }
        }

        // 改变 刻度轴字体大小
        partial void OnAxisTickFontSizeChanged(float value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            // 更新当前选中图层的颜色
            if (_previousSelectedItems != null && _previousSelectedItems.Any())
            {
                foreach (var item in _previousSelectedItems)
                {
                    var tempAxis = (IAxis)item.Plottable;
                    tempAxis.TickLabelStyle.FontSize = value;
                }
                // 刷新图形
                WpfPlot1.Refresh();
            }
        }

        // 改变 主刻度间距
        partial void OnAxisTickSpanChanged(double value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            // 更新当前选中图层的颜色
            if (_previousSelectedItems != null && _previousSelectedItems.Any())
            {
                foreach (var item in _previousSelectedItems)
                {
                    var tempAxis = (IAxis)item.Plottable;
                    var majortick = new ScottPlot.TickGenerators.NumericFixedInterval(value);
                    tempAxis.TickGenerator = majortick;
                }
                // 刷新图形
                WpfPlot1.Refresh();
            }

        }

        // 改变 刻度轴上限
        partial void OnAxisTickUpLimitChanged(double value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            // 更新当前选中图层的颜色
            if (_previousSelectedItems != null && _previousSelectedItems.Any())
            {
                foreach (var item in _previousSelectedItems)
                {
                    var tempAxis = (IAxis)item.Plottable;
                    tempAxis.Max = value;
                }
                // 刷新图形
                WpfPlot1.Refresh();
            }
        }

        // 改变 刻度轴下限
        partial void OnAxisTickDownLimitChanged(double value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            // 更新当前选中图层的颜色
            if (_previousSelectedItems != null && _previousSelectedItems.Any())
            {
                foreach (var item in _previousSelectedItems)
                {
                    var tempAxis = (IAxis)item.Plottable;
                    tempAxis.Min = value;
                }
                // 刷新图形
                WpfPlot1.Refresh();
            }
        }

        // 改变 刻度轴的颜色
        partial void OnAxisPlotColorChanged(ScottPlot.Color value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            // 更新当前选中图层的颜色
            if (_previousSelectedItems != null && _previousSelectedItems.Any())
            {
                foreach (var item in _previousSelectedItems)
                {
                    var tempAxis = (IAxis)item.Plottable;
                    tempAxis.TickLabelStyle.ForeColor = value;
                }
                // 刷新图形
                WpfPlot1.Refresh();
            }
        }

        /// <summary>
        /// ========================================LinePlot 位置
        /// </summary>
        /// 

        // 起始 X1 改变
        partial void OnX1Changed(double value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            if (_previousSelectedItems != null && _previousSelectedItems.Any())
            {
                foreach (var item in _previousSelectedItems)
                {
                    if (item.ObjectType == "LinePlot")
                    {
                        var linePlot = (LinePlot)item.Plottable;
                        linePlot.Start = new Coordinates(value, Y1);
                    }
                    if (item.ObjectType == "Text")
                    {
                        var text = (ScottPlot.Plottables.Text)item.Plottable;
                        text.Location = new Coordinates(value, Y1);
                    }
                    if (item.ObjectType == "Scatter")
                    {
                        //var scatter = (ScottPlot.Plottables.Scatter)item.Plottable;
                        //scatter.MarkerColor = value;
                    }
                    else if (item.ObjectType == "Axis")
                    {
                        //var tempAxis = (IAxis)item.Plottable;
                        //tempAxis.Label.ForeColor = value;
                    }
                }
                // 刷新图形
                WpfPlot1.Refresh();
            }
        }

        // 起始 Y1 改变
        partial void OnY1Changed(double value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            if (_previousSelectedItems != null && _previousSelectedItems.Any())
            {
                foreach (var item in _previousSelectedItems)
                {
                    if (item.ObjectType == "LinePlot")
                    {
                        ((LinePlot)item.Plottable).Start = new Coordinates(X1, value);
                    }
                    if (item.ObjectType == "Text")
                    {
                        var text = (ScottPlot.Plottables.Text)item.Plottable;
                        text.Location = new Coordinates(X1, value);
                    }
                    if (item.ObjectType == "Scatter")
                    {
                        //var scatter = (ScottPlot.Plottables.Scatter)item.Plottable;
                        //scatter.MarkerColor = value;
                    }
                    else if (item.ObjectType == "Axis")
                    {
                        //var tempAxis = (IAxis)item.Plottable;
                        //tempAxis.Label.ForeColor = value;
                    }
                }
                // 刷新图形
                WpfPlot1.Refresh();
            }
        }

        // 起始 X2 改变
        partial void OnX2Changed(double value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            if (_previousSelectedItems != null && _previousSelectedItems.Any())
            {
                foreach (var item in _previousSelectedItems)
                {
                    if (item.ObjectType == "LinePlot")
                    {
                        var linePlot = (LinePlot)item.Plottable;
                        linePlot.End = new Coordinates(value, Y2);
                    }
                }
                // 刷新图形
                WpfPlot1.Refresh();
            }
        }

        // 起始 Y2 改变
        partial void OnY2Changed(double value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            if (_previousSelectedItems != null && _previousSelectedItems.Any())
            {
                foreach (var item in _previousSelectedItems)
                {
                    if (item.ObjectType == "LinePlot")
                    {
                        var linePlot = (LinePlot)item.Plottable;
                        linePlot.End = new Coordinates(X2, value);
                    }
                }
                // 刷新图形
                WpfPlot1.Refresh();
            }
        }


        /// <summary>
        /// ========================================图例设置
        /// </summary>

        // 改变 图例对象 可见性
        partial void OnShowLegendsChanged(bool value)
        {
            if (value)
            {
                WpfPlot1.Plot.ShowLegend();
            }
            else
            {
                WpfPlot1.Plot.HideLegend();
            }
            WpfPlot1.Refresh();

        }

        // 改变 图例 的位置
        partial void OnLegendsLocationChanged(int value)
        {
            // 右上角
            if (value == 0)
            {
                WpfPlot1.Plot.Legend.Alignment = Alignment.UpperRight;
            }

            // 右下角
            if (value == 1)
            {
                WpfPlot1.Plot.Legend.Alignment = Alignment.LowerRight;
            }

            // 左上角
            if (value == 2)
            {
                WpfPlot1.Plot.Legend.Alignment = Alignment.UpperLeft;
            }

            if (value == 3)
            {
                WpfPlot1.Plot.Legend.Alignment = Alignment.LowerLeft;

            }

            // 刷新绘图
            WpfPlot1.Refresh();
        }

        // 改变 图例 的排序展示方向
        partial void OnLegendsOChanged(int value)
        {
            // 默认纵向展示
            if (value == 0)
            {
                WpfPlot1.Plot.Legend.Orientation = ScottPlot.Orientation.Vertical;
            }
            else
            {
                WpfPlot1.Plot.Legend.Orientation = ScottPlot.Orientation.Horizontal;
            }
            // 刷新图形
            WpfPlot1.Refresh();
        }

        // 改变 图例 字体
        partial void OnLegendsFontsChanged(int value)
        {

            // 匹配字体
            foreach(var temp in WpfPlot1.Plot.Legend.LegendItems)
            {
                temp.LabelFontName = PlotTextFontNames[value];
            }
            // 刷新图形
            WpfPlot1.Refresh();
        }

        // 改变 图例 字体大小
        partial void OnLegendsFontSizeChanged(float value)
        {
            // 匹配字体大小
            foreach (var temp in WpfPlot1.Plot.Legend.LegendItems)
            {
                temp.LabelFontSize = value;
            }

            // 刷新图形
            WpfPlot1.Refresh();
        }

        // 改变 图例 颜色
        partial void OnLegendsFontColorChanged(ScottPlot.Color value)
        {
            // 匹配字体
            foreach (var temp in WpfPlot1.Plot.Legend.LegendItems)
            {
                temp.LabelFontColor = value;
            }

            // 刷新图形
            WpfPlot1.Refresh();
        }


        /// <summary>
        /// ========================================绘图设置
        /// </summary>

        // 改变 绘图设置 绘图标题
        partial void OnAxisTitleChanged(string value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            // 更新标题
            WpfPlot1.Plot.Axes.Title.Label.Text = value;

            // 刷新图形
            WpfPlot1.Refresh();
        }

        // 改变 绘图设置 绘图标题字体大小
        partial void OnAxisTitleFontSizeChanged(float value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            // 更新标题
            WpfPlot1.Plot.Axes.Title.Label.FontSize = value;

            // 刷新图形
            WpfPlot1.Refresh();
        }

        // 改变 绘图设置 绘图标题字体
        partial void OnAxisTitleFontNameChanged(int value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            // 更新标题
            WpfPlot1.Plot.Axes.Title.Label.FontName = PlotTextFontNames[value];

            // 刷新图形
            WpfPlot1.Refresh();
        }

        // 改变 绘图设置 绘图标题颜色
        partial void OnAxisTitleColorChanged(ScottPlot.Color value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            // 更新标题
            WpfPlot1.Plot.Axes.Title.Label.ForeColor = value;

            // 刷新图形
            WpfPlot1.Refresh();
        }

        // 改变 绘图设置 X轴标题
        partial void OnXAxisTitleChanged(string value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            // 更新标题
            WpfPlot1.Plot.Axes.Bottom.Label.Text = value;

            // 刷新图形
            WpfPlot1.Refresh();
        }

        // 改变 绘图设置 Y轴标题
        partial void OnYAxisTitleChanged(string value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            // 更新标题
            WpfPlot1.Plot.Axes.Left.Label.Text = value;

            // 刷新图形
            WpfPlot1.Refresh();
        }

        // 改变 绘图设置 轴标题字体大小
        partial void OnAxisXYTitleFontSizeChanged(float value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            // 更新标题
            WpfPlot1.Plot.Axes.Left.Label.FontSize = value;
            WpfPlot1.Plot.Axes.Bottom.Label.FontSize = value;

            // 刷新图形
            WpfPlot1.Refresh();
        }

        // 改变 绘图设置 轴标题字体
        partial void OnAxisXYTitleFontNameChanged(int value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            // 更新标题
            WpfPlot1.Plot.Axes.Left.Label.FontName = PlotTextFontNames[value];
            WpfPlot1.Plot.Axes.Bottom.Label.FontName = PlotTextFontNames[value];

            // 刷新图形
            WpfPlot1.Refresh();
        }

        // 改变 绘图设置 轴标题颜色
        partial void OnAxisXYTitleColorChanged(ScottPlot.Color value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            // 更新标题
            WpfPlot1.Plot.Axes.Left.Label.ForeColor = value;
            WpfPlot1.Plot.Axes.Bottom.Label.ForeColor = value;

            // 刷新图形
            WpfPlot1.Refresh();
        }

        // 改变 背景设置 显示主网格
        partial void OnFirstGridShowChanged(bool value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            if (value)
            {
                WpfPlot1.Plot.ShowGrid();
            }
            else
            {
                WpfPlot1.Plot.HideGrid();
            }

            // 刷新图形
            WpfPlot1.Refresh();
        }

        // 改变 背景设置 主网格颜色
        partial void OnFirstGridColorChanged(ScottPlot.Color value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            WpfPlot1.Plot.Grid.MajorLineColor = value;
            //WpfPlot1.Plot.Grid.YAxisStyle.MajorLineStyle.Color = value;

            // 刷新图形
            WpfPlot1.Refresh();
        }

        // 改变 背景设置 主网格宽度
        partial void OnFirstGridWidthChanged(float value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            WpfPlot1.Plot.Grid.MajorLineWidth = value;

            // 刷新图形
            WpfPlot1.Refresh();
        }

        // 改变 背景设置 显示次网格
        partial void OnSecondGridShowChanged(bool value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            if (value)
            {
                WpfPlot1.Plot.Grid.MinorLineWidth = 1;
            }
            else
            {
                WpfPlot1.Plot.Grid.MinorLineWidth = 0;
            }

            // 刷新图形
            WpfPlot1.Refresh();
        }

        // 改变 背景设置 次网格颜色
        partial void OnSecondGridColorChanged(ScottPlot.Color value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            WpfPlot1.Plot.Grid.MinorLineColor = value;

            // 刷新图形
            WpfPlot1.Refresh();
        }

        // 改变 背景设置 主网格宽度
        partial void OnSecondGridWidthChanged(float value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            WpfPlot1.Plot.Grid.MinorLineWidth = value;

            // 刷新图形
            WpfPlot1.Refresh();
        }

        // 改变 背景设置 反转填充颜色
        partial void OnSwichtFillColorChanged(bool value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            if (value)
            {
                WpfPlot1.Plot.Grid.YAxisStyle.FillColor1 = GridFillColor1;
                WpfPlot1.Plot.Grid.YAxisStyle.FillColor2 = GridFillColor2;
                WpfPlot1.Plot.Grid.XAxisStyle.FillColor1 = ScottPlot.Colors.Transparent;
                WpfPlot1.Plot.Grid.XAxisStyle.FillColor2 = ScottPlot.Colors.Transparent;
            }
            else
            {
                WpfPlot1.Plot.Grid.YAxisStyle.FillColor1 = ScottPlot.Colors.Transparent;
                WpfPlot1.Plot.Grid.YAxisStyle.FillColor2 = ScottPlot.Colors.Transparent;
                WpfPlot1.Plot.Grid.XAxisStyle.FillColor1 = GridFillColor1;
                WpfPlot1.Plot.Grid.XAxisStyle.FillColor2 = GridFillColor2;
            }

            // 刷新图形
            WpfPlot1.Refresh();
        }

        // 改变 背景设置 填充颜色1
        partial void OnGridFillColor1Changed(ScottPlot.Color value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            WpfPlot1.Plot.Grid.XAxisStyle.FillColor1 = value;
            //WpfPlot1.Plot.Grid.YAxisStyle.MajorLineStyle.Color = value;

            // 刷新图形
            WpfPlot1.Refresh();
        }

        // 改变 背景设置 填充颜色2
        partial void OnGridFillColor2Changed(ScottPlot.Color value)
        {
            // 如果是在更新属性值，则不执行更新
            if (_isUpdatingLineWidth)
                return;

            WpfPlot1.Plot.Grid.XAxisStyle.FillColor2 = value;
            //WpfPlot1.Plot.Grid.YAxisStyle.MajorLineStyle.Color = value;

            // 刷新图形
            WpfPlot1.Refresh();
        }

        // LinePlot 属性匹配
        private void GetLinePlotAttributeMapping(LinePlot linePlot)
        {
            _isUpdatingLineWidth = true;  // 设置标志，防止触发更新
            try
            {
                PlotWidth = linePlot.LineWidth;     // 匹配线宽
                PlotColor = linePlot.LineColor;     // 匹配线的颜色
                // 匹配线的样式
                if (linePlot.LinePattern.Name == LinePattern.Solid.Name) { PlotType = 0; }
                if (linePlot.LinePattern.Name == LinePattern.Dashed.Name) { PlotType = 1; }
                if (linePlot.LinePattern.Name == LinePattern.DenselyDashed.Name) { PlotType = 2; }
                if (linePlot.LinePattern.Name == LinePattern.Dotted.Name) { PlotType = 3; }
                X1 = linePlot.Start.X;
                Y1 = linePlot.Start.Y;
                X2 = linePlot.End.X;
                Y2 = linePlot.End.Y;
                PlotVisible = linePlot.IsVisible;       // 匹配可见性
                //OnPropertyChanged(nameof(PlotLineWidth));  // 手动触发属性更新
            }
            finally
            {
                _isUpdatingLineWidth = false;  // 确保标志被重置
            }
        }

        // Text 属性匹配
        private void GetTextAttributeMapping(ScottPlot.Plottables.Text text)
        {
            _isUpdatingLineWidth = true;  // 设置标志，防止触发更新
            try
            {
                PlotVisible = text.IsVisible;       // 匹配可见性

                PlotWidth = text.LabelFontSize;     // 匹配文本大小

                // 当前字体
                PlotTextFontName = FindFontNameIndex(text.LabelFontName);

                // 匹配字体的样式
                if (text.LabelStyle.Italic == true) { PlotType = 2; }
                if (text.LabelStyle.Bold == true) { PlotType = 1; }
                else
                {
                    PlotType = 0;
                }

                PlotColor = text.LabelFontColor;        // 匹配字体颜色
                PlotTextContent = text.LabelText;       // 匹配文本内容

                X1 = text.Location.X;
                Y1 = text.Location.Y;

            }
            finally
            {
                _isUpdatingLineWidth = false;  // 确保标志被重置
            }
        }

        // 数据点 属性匹配
        private void GetDataAttributeMapping(ScottPlot.Plottables.Scatter scatter)
        {
            _isUpdatingLineWidth = true;  // 设置标志，防止触发更新
            try
            {

                PlotWidth = scatter.MarkerSize;     // 匹配线宽
                PlotColor = scatter.MarkerColor;     // 匹配线的颜色
                //// 匹配线的样式
                if (scatter.MarkerShape.ToString() == MarkerShape.FilledCircle.ToString()) { PlotType = 0; }
                if (scatter.MarkerShape.ToString() == MarkerShape.OpenCircle.ToString()) { PlotType = 1; }
                if (scatter.MarkerShape.ToString() == MarkerShape.FilledSquare.ToString()) { PlotType = 2; }
                if (scatter.MarkerShape.ToString() == MarkerShape.FilledTriangleUp.ToString()) { PlotType = 3; }
                if (scatter.MarkerShape.ToString() == MarkerShape.FilledTriangleDown.ToString()) { PlotType = 4; }
                if (scatter.MarkerShape.ToString() == MarkerShape.FilledDiamond.ToString()) { PlotType = 5; }
                if (scatter.MarkerShape.ToString() == MarkerShape.Eks.ToString()) { PlotType = 6; }
                if (scatter.MarkerShape.ToString() == MarkerShape.Cross.ToString()) { PlotType = 7; }
                if (scatter.MarkerShape.ToString() == MarkerShape.Asterisk.ToString()) { PlotType = 8; }
                PlotVisible = scatter.IsVisible;       // 匹配可见性
            }
            finally
            {
                _isUpdatingLineWidth = false;  // 确保标志被重置
            }
        }

        // 坐标轴属性匹配
        private void GetAxisAttributeMapping(IAxis axes)
        {
            _isUpdatingLineWidth = true;  // 设置标志，防止触发更新
            try
            {
                PlotVisible = axes.IsVisible;       // 匹配可见性
                ReverseAxis = axes.IsInverted();    // 匹配轴反转
                AxisTitle = axes.Label.Text;        // 匹配轴标题
                PlotTextFontName = FindFontNameIndex(axes.Label.FontName);      // 匹配当前字体
                PlotWidth = axes.Label.FontSize;        //匹配轴标题字体大小
                PlotColor = axes.Label.ForeColor;     // 匹配标题的颜色
                AxisTickFontName = FindFontNameIndex(axes.TickLabelStyle.FontName);        // 匹配刻度轴字体
                AxisTickFontSize = axes.TickLabelStyle.FontSize;       // 匹配刻度轴字体大小
                if (axes.TickGenerator is ScottPlot.TickGenerators.NumericFixedInterval)
                {
                    // 匹配刻度间隔
                    AxisTickSpan = ((ScottPlot.TickGenerators.NumericFixedInterval)axes.TickGenerator).Interval;
                }
                else
                {
                    AxisTickSpan = 1;
                }
                AxisTickUpLimit = axes.GetRange().Max;      // 匹配刻度上限
                AxisTickDownLimit = axes.GetRange().Min;        // 匹配刻度下限
                AxisPlotColor = axes.TickLabelStyle.ForeColor;     // 匹配刻度的颜色
            }
            finally
            {
                _isUpdatingLineWidth = false;  // 确保标志被重置
            }
        }

        // 绘图设置匹配
        private void GetPlotAttributeMapping()
        {
            _isUpdatingLineWidth = true;  // 设置标志，防止触发更新
            try
            {
                // 这是绘图部分
                AxisTitle = WpfPlot1.Plot.Axes.Title.Label.Text;        // 匹配绘图标题
                AxisTitleFontSize = WpfPlot1.Plot.Axes.Title.Label.FontSize;        // 匹配绘图标题大小
                AxisTitleFontName = FindFontNameIndex(WpfPlot1.Plot.Axes.Title.Label.FontName); // 匹配绘图标题字体
                AxisTitleColor = WpfPlot1.Plot.Axes.Title.Label.ForeColor; // 匹配绘图标题字体

                YAxisTitle = WpfPlot1.Plot.Axes.Left.Label.Text;        // 匹配左侧轴标题
                XAxisTitle = WpfPlot1.Plot.Axes.Bottom.Label.Text;        // 匹配左侧轴标题
                AxisXYTitleColor = WpfPlot1.Plot.Axes.Left.Label.ForeColor;     // 匹配轴标题的颜色
                AxisXYTitleFontName = FindFontNameIndex(WpfPlot1.Plot.Axes.Left.Label.FontName);      // 匹配轴标题字体
                AxisXYTitleFontSize = WpfPlot1.Plot.Axes.Left.Label.FontSize;        //匹配轴标题字体大小


                // 这是背景部分
                if (WpfPlot1.Plot.Grid.XAxisStyle.MajorLineStyle.Width > 0)
                {
                    FirstGridShow = true;       // 匹配显示主网格
                }
                FirstGridColor = WpfPlot1.Plot.Grid.XAxisStyle.MajorLineStyle.Color;    // 匹配主网格颜色
                FirstGridWidth = WpfPlot1.Plot.Grid.XAxisStyle.MajorLineStyle.Width;    // 匹配主网格宽度
                if (WpfPlot1.Plot.Grid.XAxisStyle.MinorLineStyle.Width > 0)
                {
                    SecondGridShow = true;       // 匹配显示次网格
                }
                SecondGridColor = WpfPlot1.Plot.Grid.XAxisStyle.MinorLineStyle.Color;    // 匹配主网格颜色
                SecondGridWidth = WpfPlot1.Plot.Grid.XAxisStyle.MinorLineStyle.Width;    // 匹配主网格宽度
                GridFillColor1 = WpfPlot1.Plot.Grid.XAxisStyle.FillColor1;      // 匹配填充颜色1
                GridFillColor2 = WpfPlot1.Plot.Grid.XAxisStyle.FillColor2;      // 匹配填充颜色1
            }
            finally
            {
                _isUpdatingLineWidth = false;  // 确保标志被重置
            }
        }

        // 图例设置匹配
        private void GetLegendAttributeMapping()
        {
            ShowLegends = WpfPlot1.Plot.Legend.IsVisible;      // 是否显示

            // 右上角
            if (WpfPlot1.Plot.Legend.Alignment == Alignment.UpperRight)
            {
                _legendsLocation = 0;
            }

            // 右下角
            if (WpfPlot1.Plot.Legend.Alignment == Alignment.LowerRight)
            {
                _legendsLocation = 1;
            }

            // 左上角
            if (WpfPlot1.Plot.Legend.Alignment == Alignment.UpperLeft)
            {
                _legendsLocation = 2;
            }
            else
            {
                _legendsLocation = 3;
            }

            // 默认纵向展示
            if (WpfPlot1.Plot.Legend.Orientation == ScottPlot.Orientation.Vertical)
            {
                _legendsO = 0;
            }
            else
            {
                _legendsO = 1;
            }
            //// 匹配字体
            //_legendsFonts = FindFontNameIndex(WpfPlot1.Plot.Legend.LegendItems.First().LabelFontName);

            //// 匹配字体大小
            //_legendsFontSize = (float)WpfPlot1.Plot.Legend.LegendItems.First().LabelFontSize;

            //// 匹配字体颜色
            //_legendsFontColor = (ScottPlot.Color)WpfPlot1.Plot.Legend.LegendItems.First().LabelFontColor;
        }

        // 解压缩
        private string ReadRtfFromZip(string zipFilePath, string rtfFileName)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
            {
                ZipArchiveEntry entry = archive.GetEntry(rtfFileName);
                if (entry != null)
                {
                    using (Stream stream = entry.Open())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            return null;
        }

        // 加载绘图说明内容
        private void LoadRtfContent(string zipFilePath, string rtfFileName)
        {
            string rtfContent = ReadRtfFromZip(zipFilePath, rtfFileName);
            if (rtfContent != null)
            {
                TextRange textRange = new TextRange(_richTextBox.Document.ContentStart, _richTextBox.Document.ContentEnd);
                using (MemoryStream stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(rtfContent)))
                {
                    // 使用 Dispatcher 在 UI 线程上更新 UI 元素
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        textRange.Load(stream, DataFormats.Rtf);
                    });

                }
            }
        }

        // ===============================基础功能

        // 刷新操作-刷新底图，刷新底图列表
        public void Refresh(bool isClearPlot = false, bool isRestPlot = false, bool isOnlyRefreshPlot = false)
        {
            // 清空底图
            if (isClearPlot) { WpfPlot1.Plot.Clear(); }
            // 清空画布
            if (isRestPlot) { WpfPlot1.Reset(); }
            // 刷新关闭属性面板
            SetTrue(null);
            if (!isOnlyRefreshPlot)
            {
                // 刷新坐标轴列表
                GetAxisList();
                // 刷新图层列表
                PopulatePlotItems((List<IPlottable>)WpfPlot1.Plot.GetPlottables());
            }
            // 刷新底图
            WpfPlot1.Refresh();
        }

        // 检查参数格式是否正确
        private (string,bool) CheckParameter(string parameter)
        {
            if (string.IsNullOrEmpty(parameter) || parameter == "")
            {
                return ("参数为空",false);
            }

            // 正则表达式，确保只包含字母、数字和英文逗号，并且没有多余的空格
            string pattern = @"^([a-zA-Z0-9]+(,[a-zA-Z0-9]+)*)?$";
            if (!Regex.IsMatch(parameter, pattern))
            {
                return ("参数格式不正确",false);
            }

            return ("参数格式正确",true);

        }

        // 检查 Js 脚本是否正确
        private string CheckJsScript(string script)
        {
            try
            {
                var engine = new Engine();
                var (x,y) = CheckParameter(NeedParamContent);
                if (y)
                {
                    // 按逗号分隔字符串并去除多余的空格
                    string[] variables = NeedParamContent.Split(',').Select(v => v.Trim()).ToArray();
                    // 将每个变量添加到 Jint 中
                    foreach (string variable in variables)
                    {
                        engine.SetValue(variable, 1);
                    }
                }
                engine.Execute(script);
                // 脚本执行成功,语法正确
                return "脚本正确";
            }
            catch (JavaScriptException ex)
            {
                // 捕获到异常,说明脚本存在语法错误
                return ($"脚本错误: {ex.Message}");
            }
        }

        // 新建底图
        private void NewPlot()
        {
            // 刷新绘图
            Refresh(false, true);
            // 进入底图编辑模式
            IsEditMode = true;
            IsPlotMode = false;
        }

        // 使用正则表达式检查字符串是否为空且只包含正常语言字符
        public bool IsValidLanguageString(string input)
        {
            // 检查字符串是否为空
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            // 使用正则表达式验证是否只包含Unicode字母、数字和下划线
            string pattern = @"^[\p{L}0-9_]*$";
            return Regex.IsMatch(input, pattern);
        }

        // 是否符合 xxx,xxx,xxxx,xxx 格式
        public bool IsValidCommaSeparatedString(string input)
        {
            // 检查字符串是否为 null 或空
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            // 检查是否包含空格
            if (input.Contains(" "))
            {
                return false;
            }

            // 按逗号分隔字符串
            string[] parts = input.Split(',');

            // 检查每部分是否都不为空
            foreach (string part in parts)
            {
                if (string.IsNullOrEmpty(part))
                {
                    return false; // 如果有空部分，说明有连续逗号或开头/结尾有逗号
                }
            }

            return true;
        }


        /// <summary>
        /// 比较两个rootNode数组是否相同
        /// </summary>
        private static bool CompareRootNode(string[] nodeA, string[] nodeB)
        {
            if (nodeA == null || nodeB == null)
                return false;

            if (nodeA.Length != nodeB.Length)
                return false;

            for (int i = 0; i < nodeA.Length; i++)
            {
                if (nodeA[i] != nodeB[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 根据ListNodeConfig查找对应的TreeNode
        /// </summary>
        /// <param name="rootNode">开始搜索的根节点</param>
        /// <param name="config">要查找的节点配置</param>
        /// <returns>找到的TreeNode，未找到则返回null</returns>
        public static TreeNode FindTreeNodeByConfig(TreeNode rootNode, ListNodeConfig config)
        {
            if (rootNode == null || config == null || config.rootNode == null)
                return null;

            // 检查当前节点是否匹配
            if (CompareRootNode(rootNode.rootNode, config.rootNode))
            {
                return rootNode;
            }

            // 递归搜索子节点
            foreach (var child in rootNode.Children)
            {
                var result = FindTreeNodeByConfig(child, config);
                if (result != null)
                    return result;
            }

            return null;
        }

        // 删除节点
        private void RemoveBaseMapNode()
        {
            // 删除Json列表
            string basePath = FileHelper.GetAppPath();
            string tempNewPath;
            if (IsEditMode)
            {
                tempNewPath = System.IO.Path.Combine(basePath, "Data", "PlotData", "PlotListCustom.json");
            }
            else
            {
                tempNewPath = System.IO.Path.Combine(basePath, "Data", "PlotData", "PlotList.json");
            }
            JsonHelper.RemoveNodeFromJson(tempNewPath, _previousSelectedNode.rootNode);
        }


        // ==================================绑定命令

        // 文本更新改变
        [RelayCommand]
        public void RefreshText()
        {
            OnPlotTextContentChanged(PlotTextContent);
        }

        //// 图层对象选择
        //[RelayCommand]
        //public void LayersSelection(IList selectedItems)
        //{
        //    // 如果没有选中项，恢复所有对象的正常显示
        //    if (selectedItems == null || selectedItems.Count == 0)
        //    {
        //        foreach (var item in BasePlotItems)
        //        {
        //            SetTrue(null);
        //            if (item.ObjectType == "LinePlot")
        //            {
        //                var linePlot = (LinePlot)item.Plottable;
        //                linePlot.LineColor = linePlot.LineColor.WithAlpha(1f); // 恢复完全不透明
        //            }
        //            else if (item.ObjectType == "Text")
        //            {
        //                var text = (ScottPlot.Plottables.Text)item.Plottable;
        //                text.Color = text.Color.WithAlpha(1f); // 恢复完全不透明
        //            }
        //        }
        //        _previousSelectedItems.Clear();
        //        WpfPlot1.Refresh();
        //        return;
        //    }

        //    // 先将所有对象设置为暗淡效果
        //    foreach (var item in BasePlotItems)
        //    {
        //        if (item.ObjectType == "LinePlot")
        //        {
        //            var linePlot = (LinePlot)item.Plottable;
        //            linePlot.LineColor = linePlot.LineColor.WithAlpha(0.5f); // 降低透明度
        //        }
        //        else if (item.ObjectType == "Text")
        //        {
        //            var text = (ScottPlot.Plottables.Text)item.Plottable;
        //            text.Color = text.Color.WithAlpha(0.3f); // 降低透明度
        //        }
        //    }

        //    // 恢复选中项的完全不透明效果
        //    foreach (var item in selectedItems)
        //    {
        //        if (item is PlotItemModel plotItem)
        //        {
        //            if (plotItem.ObjectType == "LinePlot")
        //            {
        //                var linePlot = (LinePlot)plotItem.Plottable;
        //                linePlot.LineColor = linePlot.LineColor.WithAlpha(1f); // 完全不透明
        //                SetTrue("PlotLine");
        //                GetLinePlotAttributeMapping(linePlot);
        //            }
        //            else if (plotItem.ObjectType == "Text")
        //            {
        //                var text = (ScottPlot.Plottables.Text)plotItem.Plottable;
        //                text.Color = text.Color.WithAlpha(1f); // 完全不透明
        //                SetTrue("PlotText");
        //                GetTextAttributeMapping(text);
        //            }
        //            else if (plotItem.ObjectType == "Scatter")
        //            {
        //                var markers = (ScottPlot.Plottables.Scatter)plotItem.Plottable;
        //                markers.Color = markers.Color.WithAlpha(1f); // 完全不透明
        //                SetTrue("Scatter");
        //                GetDataAttributeMapping(markers);
        //            }
        //        }
        //    }

        //    // 更新选中状态
        //    _previousSelectedItems = selectedItems.Cast<PlotItemModel>().ToList();
        //    // 刷新绘图
        //    WpfPlot1.Refresh();
        //}

        
        // 除了坐标轴之外的图层对象选择
        [RelayCommand]
        public void LayersSelection(IList selectedItems)
        {
            // 如果没有选中项，恢复所有对象的正常显示
            if (selectedItems == null || selectedItems.Count == 0)
            {
                ResetAllItemsOpacity();
                _previousSelectedItems.Clear();
                WpfPlot1.Refresh();
                return;
            }

            // 将所有对象设置为暗淡效果
            SetItemsOpacity(0.2f);

            // 恢复所有选中项的完全不透明效果
            foreach (var selectedItem in selectedItems)
            {
                if (selectedItem is PlotItemModel plotItem)
                {
                    RestoreItemOpacity(plotItem);
                }
            }


            // 更新选中状态
            _previousSelectedItems = selectedItems.Cast<PlotItemModel>().ToList();

            // 刷新绘图
            WpfPlot1.Refresh();
        }

        // 将所有图层的透明度设置为指定值
        private void SetItemsOpacity(float alpha)
        {
            foreach (var item in BasePlotItems)
            {
                SetItemOpacity(item, alpha);
            }

            foreach (var item in BaseTextItems)
            {
                SetItemOpacity(item, alpha);
            }

            foreach (var item in BaseDataItems)
            {
                SetItemOpacity(item, alpha);
            }
        }

        // 设置单个图层的透明度
        private void SetItemOpacity(PlotItemModel item, float alpha)
        {
            if (item.ObjectType == "LinePlot")
            {
                var linePlot = (LinePlot)item.Plottable;
                linePlot.LineColor = linePlot.LineColor.WithAlpha(alpha);
            }
            else if (item.ObjectType == "Text")
            {
                var text = (ScottPlot.Plottables.Text)item.Plottable;
                text.LabelFontColor = text.LabelFontColor.WithAlpha(alpha);
            }
            else if (item.ObjectType == "Scatter")
            {
                var scatter = (ScottPlot.Plottables.Scatter)item.Plottable;
                scatter.Color = scatter.Color.WithAlpha(alpha);
            }
        }

        // 恢复选中项的透明度
        private void RestoreItemOpacity(PlotItemModel selectedItem)
        {
            if (selectedItem.ObjectType == "LinePlot")
            {
                var linePlot = (LinePlot)selectedItem.Plottable;
                linePlot.LineColor = linePlot.LineColor.WithAlpha(1f);
                SetTrue("PlotLine");
                GetLinePlotAttributeMapping(linePlot);
            }
            else if (selectedItem.ObjectType == "Text")
            {
                var text = (ScottPlot.Plottables.Text)selectedItem.Plottable;
                text.LabelFontColor = text.LabelFontColor.WithAlpha(1f);
                SetTrue("PlotText");
                GetTextAttributeMapping(text);
            }
            else if (selectedItem.ObjectType == "Scatter")
            {
                var markers = (ScottPlot.Plottables.Scatter)selectedItem.Plottable;
                markers.Color = markers.Color.WithAlpha(1f);
                SetTrue("Scatter");
                GetDataAttributeMapping(markers);
            }
        }

        // 重置所有对象的透明度
        private void ResetAllItemsOpacity()
        {
            SetItemsOpacity(1f); // 恢复为不透明
        }

        // 添加底图到底图列表
        public static void AddOrFindPath(TreeNode rootNode, BaseInfo baseInfo, string path)
        {
            TreeNode currentNode = rootNode;
            foreach (string category in baseInfo.rootNode)
            {
                TreeNode childNode = currentNode.Children.FirstOrDefault(child => child.Name == category);

                if (childNode == null)
                {
                    childNode = new TreeNode { Name = category };
                    childNode.PlotTemplate = new PlotTemplate();

                    childNode.PlotTemplate.PlotMethod = null;   // 标识是 Json 格式
                    childNode.PlotTemplate.Description = path;      // 标识 Json文件路径
                    childNode.PlotTemplate.RequiredElements = baseInfo.requiredElements;    // 获取判别元素

                    currentNode.Children.Add(childNode);
                }

                currentNode = childNode;
            }
        }

        // 删除节点
        public bool RemoveNode(TreeNode root, TreeNode nodeToRemove)
        {
            if (root == null)
            {
                return false;
            }

            for (int i = 0; i < root.Children.Count; i++)
            {
                if (root.Children[i] == nodeToRemove)
                {
                    root.Children.RemoveAt(i);
                    return (root.Children.Count == 0);
                }
            }

            foreach (var child in root.Children)
            {
                bool shouldRemoveThisChild = RemoveNode(child, nodeToRemove);
                if (shouldRemoveThisChild)
                {
                    root.Children.Remove(child);
                    return (root.Children.Count == 0);
                }
            }

            return false;
        }

        // 坐标轴图层对象选择
        [RelayCommand]
        public void AxisSelection(IList selectedItems)
        {
            // 如果存在对象
            if (selectedItems != null || selectedItems.Count != 0)
            {
                foreach (var item in selectedItems)
                {
                    SetTrue("Axis");
                    var tempAxis = ((PlotItemModel)item).Plottable;
                    GetAxisAttributeMapping((IAxis)tempAxis);
                }
                WpfPlot1.Refresh();
                // 更新选中状态
                _previousSelectedItems = selectedItems.Cast<PlotItemModel>().ToList();
                return;
            }
            else
            {
                _previousSelectedItems.Clear();
                SetTrue("null");
            }
        }

        // 点击选择绘图模板
        [RelayCommand]
        private void SelectTreeViewItem(object parameter)
        {
            if (parameter is TreeNode node && node.PlotTemplate != null && node.PlotTemplate.PlotMethod != null)
            {

                WpfPlot1.Plot.Clear();
                SetTrue(null);      // 显示属性
                _previousSelectedNode = (TreeNode)parameter;        // 获取当前选中模板对象
                node.PlotTemplate.DrawMethod(WpfPlot1.Plot);        // 获取选择对象
                LoadRtfContent(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "PlotData", "PlotData.zip"),
                    ((TreeNode)parameter).PlotTemplate.Description);

                // 刷新图层列表
                PopulatePlotItems((List<IPlottable>)WpfPlot1.Plot.GetPlottables());
                // 获取坐标轴对象
                GetAxisList();
                // 刷新图形界面
                WpfPlot1.Refresh();
            }

            // 如果是 null 则表示是 json 导入进来的
            if (parameter is TreeNode node1 && node1.PlotTemplate == null && node1.Children.Count == 0)
            {

                _previousSelectedNode = (TreeNode)parameter;        // 获取当前选中模板对象

                string lgFolder = "zh-Hans";
                if (ConfigHelper.GetConfig("language") == "1")
                {
                    lgFolder = "en-US";
                }

                var basemapFilePath = System.IO.Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "Default", lgFolder, node1.BaseMapPath);


                // 排除创建底图对象但是没有保存底图配置文件
                if (FileHelper.IsFileExist(basemapFilePath))
                {
                    // 清空画布
                    Refresh(true, true);

                    // 根据语言-加载底图
                    _selectedBaseInfo = PlotLoader.LoadBasePlot(WpfPlot1.Plot, basemapFilePath, _richTextBox);

                    //_previousSelectedNode = (TreeNode)parameter;        // 获取当前选中模板对象
                    NewBasePlotName = _previousSelectedNode.Name;       // 获取当前对象
                    _richTextBox.Document.Blocks.Clear();       // 清除当前的内容

                    // 获取脚本对象
                    if( _selectedBaseInfo.requiredElements != null )
                    {
                        NeedParamContent = string.Join(",", _selectedBaseInfo.requiredElements);
                    }

                    // 获取脚本内容
                    if (_selectedBaseInfo.script != null)
                    {
                        JsScriptContent = _selectedBaseInfo.script;
                    }

                    //LoadRtfContent(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "PlotData", "PlotData.zip"),
                    //    ((TreeNode)parameter).PlotTemplate.Description);

                    // 刷新底图
                    Refresh();
                }
                else
                {
                    // 底图文件丢失询问是否删除
                    Growl.Ask("当前语言底图文件丢失,是否删除该底图？", isConfirmed =>
                    {
                        if (isConfirmed)
                        {
                            RemoveBaseMapNode();
                            RegisterPlotTemplates(true);
                        }
                        else
                        {
                            // 通知：已取消导入
                            MessageHelper.Info("取消加载");
                        }

                        return true;
                    });
                }
            }
        }

        // 点击选择导入数据投图
        [RelayCommand]
        private async void ImportDataPlot()
        {
            if (_previousSelectedNode != null)
            {
                var fileContent = FileHelper.ReadFile();
                if (fileContent == null)
                {
                    //await _dialogCoordinator.ShowMessageAsync(this, "说明", "未选择文件\n文件存在问题");
                }
                else
                {
                    // 判别参数是否符合需求
                    var tempNum = ContainsAllStrings(fileContent, _selectedBaseInfo.requiredElements);
                    // 匹配成功
                    if (tempNum == 1)
                    {
                        // 如果是 Json 文件投点
                        if(_previousSelectedNode.PlotTemplate == null)
                        {
                            // 计算好的数据
                            var groupedData = new Dictionary<string, List<(double, double)>>();
                            // 错误读取的数据
                            int skippedRows = 0;
                            // 获取投点计算结果
                            foreach (DataRow row in fileContent.Rows)
                            {
                                // 提取数据
                                var (success, values) = NormalPlotMethod.ExtractValues(row);
                                if (!success)
                                {
                                    skippedRows++;
                                    continue;
                                }

                                // 确保 "Group" 键存在且是字符串
                                if (!values.TryGetValue("Group", out var groupObj) || !(groupObj is string group))
                                {
                                    skippedRows++;
                                    continue;
                                }

                                // 调用计算脚本
                                var engine = new Engine();
                                // 将字典中的值设置到 JavaScript 环境中
                                foreach (var kvp in values)
                                {
                                    engine.SetValue(kvp.Key, kvp.Value);
                                }
                                // 执行脚本  "var result1 = K2O * Na2O; var result2 = SiO2; [result1, result2]"
                                var script = _selectedBaseInfo.script;
                                var results = engine.Evaluate(script).ToObject() as object[];

                                //var (df1, df2) = calculatePoints(values);       // 计算数据

                                if (!groupedData.ContainsKey(group))
                                {
                                    groupedData[group] = new List<(double, double)>();
                                }
                                // 添加数据
                                groupedData[group].Add(((double)results[0], (double)results[1]));
                            }

                            NormalPlotMethod.PlotData(WpfPlot1.Plot, groupedData);

                        }
                        else
                        {
                            // 计算投点
                            await _previousSelectedNode.PlotTemplate.PlotMethod(WpfPlot1.Plot, fileContent);
                        }
                        
                        // 添加数据
                        foreach (var kvp in NormalPlotMethod.pointObject)
                        {
                            BaseDataItems.Add(new PlotItemModel
                            {
                                Name = kvp.Key, // 组别的名称
                                Plottable = (IPlottable)kvp.Value, // 对应的散点图对象
                                ObjectType = "Scatter"
                            });
                        }

                        // 刷新绘图
                        WpfPlot1.Refresh();
                    }
                    else
                    {
                        if (tempNum == -2)
                        {
                            //await _dialogCoordinator.ShowMessageAsync(this, "说明", "匹配失败，请检查表头");
                        }
                        else if (tempNum == 0)
                        {
                            //await _dialogCoordinator.ShowMessageAsync(this, "说明", "读取表格数据为空");
                        }
                        else if (tempNum == -1)
                        {
                            //await _dialogCoordinator.ShowMessageAsync(this, "说明", "输入数据参数错误");
                        }
                    }
                    return;
                }

                //await Task.Run(() =>
                //{
                //    DataTable dataTable = new DataTable();
                //    _previousSelectedNode.PlotTemplate.PlotMethod(WpfPlot1.Plot, dataTable);
                //});
                // 使用 Dispatcher 在 UI 线程上更新 UI 元素
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // 刷新图层列表
                    PopulatePlotItems((List<IPlottable>)WpfPlot1.Plot.GetPlottables());
                    // 刷新图形界面
                    WpfPlot1.Refresh();
                });
            }
        }

        // 恢复默认绘图
        [RelayCommand]
        public void ReSetDefault()
        {
            if (_previousSelectedNode != null)
            {
                // 如果是 Json 格式的文件
                if(_previousSelectedNode.PlotTemplate == null)
                {
                    SelectTreeViewItem(_previousSelectedNode);
                }
                else
                {
                    _previousSelectedNode.PlotTemplate.DrawMethod(WpfPlot1.Plot);
                }
                    
                //// 使用 Dispatcher 在 UI 线程上更新 UI 元素
                //System.Windows.Application.Current.Dispatcher.Invoke(() =>
                //{
                Refresh(true);
                //});
            }
        }

        // 视图复位
        [RelayCommand]
        public void CenterPlot()
        {
            WpfPlot1.Plot.Axes.AutoScale();
            WpfPlot1.Refresh();
        }

        // 取消选中
        [RelayCommand]
        public void CancelSelected()
        {
            // 取消选择图层对象
            LayersSelection(null);
            // 刷新属性
            SetTrue(null);
            // 获取坐标轴对象
            GetAxisList();
            // 刷新图层列表
            PopulatePlotItems((List<IPlottable>)WpfPlot1.Plot.GetPlottables());
        }

        // 图例设置
        [RelayCommand]
        public void LegendSetting()
        {
            SetTrue("Legend");
            GetLegendAttributeMapping();
        }

        // 绘图设置
        [RelayCommand]
        public void PlotSetting()
        {
            SetTrue("Main");
            GetPlotAttributeMapping();
        }

        // 展示定位轴
        [RelayCommand]
        public void LocationAxis()
        {
            if(crosshair == null)
            {
                crosshair = WpfPlot1.Plot.Add.Crosshair(0, 0);
            }
            else
            {
                if(crosshair.IsVisible == false)
                {
                    crosshair.IsVisible = true;
                }
                else
                {
                    WpfPlot1.Plot.Remove(crosshair);
                    crosshair = null;
                }

            }
            WpfPlot1.Refresh();
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
            WpfPlot1.Plot.Save(temp, (int)(tempWidth*1.25), (int)(tempHeight*1.25));
        }

        // ========================================================
        // ======================自定义底图========================
        // ========================================================

        // 检查脚本
        [RelayCommand]
        public void CheckContent()
        {
            string output = "";
            // 检查参数
            if (NeedParamContent == "" || NeedParamContent == null)
            {
                // 通知：参数为空
                output += LanguageService.Instance["ParameterNull"];
            }
            else
            {
                output += CheckParameter(NeedParamContent);
            }
            // 检查脚本
            if (JsScriptContent == "" || JsScriptContent == null)
            {
                // 脚本为空
                output += "\n" + LanguageService.Instance["JsNull"];
            }
            else
            {
                output += "\n" + CheckJsScript(JsScriptContent);
            }
            MessageHelper.Info(output);
        }

        // 新建底图
        [RelayCommand]
        public void NewBasemap()
        {
            // 显示弹窗
            SetTrue("Popup");
        }

        // 确认新建底图
        [RelayCommand]
        public void ConfirmNewBasemap()
        {
            // todo: 参数检查
            //if (!IsValidLanguageString(NewBasePlotName))
            //{
            //    MessageHelper.Info("名称：格式错误");
            //    return;
            //}

            if (!IsValidCommaSeparatedString(BasePlotType))
            {
                // 通知：类别格式错误
                MessageHelper.Info(LanguageService.Instance["CategoryFormatError"]);
                return;
            }

            // 获取对象
            ListNodeConfig listNodeConfig = new ListNodeConfig();
            listNodeConfig.rootNode = BasePlotType.Split(',', StringSplitOptions.RemoveEmptyEntries);
            listNodeConfig.baseMapPath = BasePlotType.Replace(",","_") + ".json";
            listNodeConfig.baseMapLg = BasePlotLanguage;    // 获取语言

            // 获取文件路径
            string tempNewPath = System.IO.Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "PlotListCustom.json");
            // Json文件添加内容保存
            JsonHelper.AddToJsonFile(listNodeConfig, tempNewPath);

            RegisterPlotTemplates(true);

            // 新建底图
            NewPlot();

            _previousSelectedNode = FindTreeNodeByConfig(RootTreeNode, listNodeConfig);

            // 通知:新建底图成功
            MessageHelper.Info(LanguageService.Instance["NewBasemapSuccess"]);

            // 关闭弹窗
            SetTrue(null);
        }

        // 取消新建底图
        [RelayCommand]
        public void CancelNewBasemap()
        {
            // 关闭弹窗
            SetTrue(null);
        }

        // 导入底图
        [RelayCommand]
        public void LoadBasemap()
        {
            // 获取底图路径
            string filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
            var filePath = FileHelper.GetFilePath(filter);

            if(filePath != null)
            {
                BasePlotConfig config = null;

                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"文件不存在：{filePath}");
                    return;
                }
                // 读取JSON文件并反序列化
                string json = File.ReadAllText(filePath);
                config = JsonHelper.Deserialize<BasePlotConfig>(json);
                if (config == null)
                {
                    Console.WriteLine("反序列化失败");
                    return;
                }

                // 判断底图版本
                double mapVersion = Convert.ToDouble(System.Diagnostics.FileVersionInfo.GetVersionInfo(
                    System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion);
                if (Convert.ToDouble(config.BaseMapVersion) >= mapVersion)
                {
                    ListNodeConfig listNodeConfig = new ListNodeConfig();

                    string tempFolder = "Default";

                    // 如果是编辑模式导入的底图
                    if (IsEditMode)
                    {
                        tempFolder = "Custom";
                    }

                    string lgFolder = "zh-Hans";
                    if (ConfigHelper.GetConfig("language") == "1")
                    {
                        lgFolder = "en-US";
                    }

                    string tempPath = System.IO.Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", tempFolder, lgFolder,
                        FileHelper.GetFileName(filePath));

                    // 判断导入的底图是否已存在
                    if (FileHelper.IsFileExist(tempPath))
                    {
                        Growl.Ask(LanguageService.Instance["BasemapExisted"], isConfirmed =>
                        {
                            if (isConfirmed)
                            {
                                // 加载底图
                                BaseInfo baseInfo = PlotLoader.LoadBasePlot(WpfPlot1.Plot, filePath, _richTextBox);

                                // 复制文件到指定路径
                                var targetPath = System.IO.Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", tempFolder, lgFolder);
                                FileHelper.CopyFile(filePath, targetPath);

                                // 加载侧边列表
                                RegisterPlotTemplates();

                                // 设置当前选中底图为当前
                                _previousSelectedNode = FindTreeNodeByConfig(RootTreeNode, listNodeConfig);

                                // 视角居中
                                CenterPlot();

                                // 刷新底图
                                //Refresh();
                            }
                            else
                            {
                                // 通知：已取消导入
                                MessageHelper.Info(LanguageService.Instance["CancelImport"]);
                            }

                            return true;
                        });
                    }
                    else
                    {
                        // 加载底图
                        BaseInfo baseInfo = PlotLoader.LoadBasePlot(WpfPlot1.Plot, filePath, _richTextBox);

                        // 复制文件到指定路径
                        var targetPath = System.IO.Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", tempFolder, lgFolder);
                        FileHelper.CopyFile(filePath, targetPath);

                        
                        listNodeConfig.rootNode = baseInfo.rootNode;
                        listNodeConfig.baseMapPath = System.IO.Path.GetFileName(filePath);

                        

                        // 如果是编辑模式导入的底图
                        if (IsEditMode)
                        {
                            targetPath = System.IO.Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "PlotListCustom.json");
                        }
                        else
                        {
                            targetPath = System.IO.Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "PlotList.json");
                        }

                        // 写入列表
                        JsonHelper.AddToJsonFile(listNodeConfig, targetPath);

                        // 加载侧边列表
                        //AddOrFindPath(RootTreeNode, baseInfo, filePath);
                        RegisterPlotTemplates();

                        // 设置当前选中底图为当前
                        _previousSelectedNode = FindTreeNodeByConfig(RootTreeNode, listNodeConfig);

                        // 视角居中
                        CenterPlot();

                        // 刷新底图
                        //Refresh();
                    }



                }
                else
                {
                    // 通知：导入的底图版本过低，请使用版本大于" + mapVersion + "的底图
                    MessageHelper.Warning(LanguageService.Instance["BasemapVersionToLow1"] + mapVersion +
                        LanguageService.Instance["BasemapVersionToLow2"]);
                }
            }
        }

        // 退出编辑
        [RelayCommand]
        public void ExitEditModel()
        {
            // 判断是否修改了底图属性，如果修改了提示保存
            
            // 清除底图文件内存
            BasePlotConfigObject = null;

            //退出底图编辑模式
            IsEditMode = false;
            IsPlotMode = true;

            // 加载正常的底图列表
            RegisterPlotTemplates();

            // 刷新绘图
            Refresh(false, true);

            // 恢复右键菜单
            WpfPlot1.UserInputProcessor.IsEnabled = true;// 禁用右键菜单

            // 刷新绘图
            Refresh(true);
        }

        // 保存底图
        [RelayCommand]
        public void SaveBaseMap()
        {
            BasePlotConfigObject = new BasePlotConfig();
            BasePlotConfigObject.Title = WpfPlot1.Plot.Axes.Title.Label.Text;    // 绘图标题

            BasePlotConfigObject.BaseMapVersion =
                System.Diagnostics.FileVersionInfo.GetVersionInfo(
                    System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;    // 图表Json版本号

            // 保存类别名称
            BasePlotConfigObject.baseInfo.rootNode = _previousSelectedNode.rootNode;

            // 保存需要的参数和脚本
            //BasePlotConfigObject.baseInfo.requiredElements = new string[] { "Group", "K2O", "Na2O", "SiO2" };
            if (NeedParamContent == null || NeedParamContent =="")
            {
                // 通知:投图参数未填写
                MessageHelper.Info(LanguageService.Instance["ParameterNotFilledIn"]);
            }
            else
            {
                BasePlotConfigObject.baseInfo.requiredElements = NeedParamContent.Split(",");
            };

            if (JsScriptContent == null || JsScriptContent == "")
            {
                // 通知:投图脚本未填写
                MessageHelper.Info(LanguageService.Instance["ScNotFilledIn"]);
            }
            else
            {
                BasePlotConfigObject.baseInfo.script = JsScriptContent;
            };

            //坐标轴标题
            BasePlotConfigObject.PlotConfig = new PlotConfig()
            {
                title = WpfPlot1.Plot.Axes.Title.Label.Text,                        // 图表标题
                titleFontSize = WpfPlot1.Plot.Axes.Title.Label.FontSize,            // 图表标题字体大小
                titleColor = WpfPlot1.Plot.Axes.Title.Label.ForeColor.ToHex(),      // 图表标题颜色
                x = WpfPlot1.Plot.Axes.Bottom.Label.Text,                           // X 轴标题
                xFontSize = WpfPlot1.Plot.Axes.Bottom.Label.FontSize,               // X 标题大小   
                y = WpfPlot1.Plot.Axes.Left.Label.Text,                             // Y 轴标题
                yFontSize = WpfPlot1.Plot.Axes.Left.Label.FontSize,                 // Y 标题大小
                axisTitleColor = WpfPlot1.Plot.Axes.Left.Label.ForeColor.ToHex(),    // 轴标题颜色 

                // 背景
                isShowMainGrid = WpfPlot1.Plot.Grid.IsVisible,                          // 是否显示网格
                mainGridColor = WpfPlot1.Plot.Grid.MajorLineColor.ToHex(),              // 主网格颜色
                mainGridSize = WpfPlot1.Plot.Grid.XAxisStyle.MajorLineStyle.Width,      // 主网格宽度
                isShowMinorGrid = (WpfPlot1.Plot.Grid.XAxisStyle.MinorLineStyle.Width > 0),       // 是否显示网格
                minorGridColor = WpfPlot1.Plot.Grid.XAxisStyle.MinorLineStyle.Color.ToHex(),          // 是否显示网格
                minorGridSize = WpfPlot1.Plot.Grid.XAxisStyle.MinorLineStyle.Width,               // 是否显示网格
            };

            // X 坐标轴
            BasePlotConfigObject.PlotAxes.XAxes = new AxesConfig()
            {
                isShowMinorGrid = WpfPlot1.Plot.Axes.Bottom.IsVisible,
                axesTickFontSize = WpfPlot1.Plot.Axes.Bottom.Label.FontSize,
                //axesTickSpacing = ((ScottPlot.TickGenerators.NumericAutomatic)WpfPlot1.Plot.Axes.Bottom.TickGenerator).Interval,
                Limit = new double[] { WpfPlot1.Plot.Axes.Bottom.GetRange().Min, WpfPlot1.Plot.Axes.Bottom.GetRange().Max },
                axesColor = WpfPlot1.Plot.Axes.Bottom.TickLabelStyle.ForeColor.ToHex(),
                //yLimit = new double[] { WpfPlot1.Plot.Axes.Left.GetRange().Min, WpfPlot1.Plot.Axes.Left.GetRange().Max, }
            };

            // Y 坐标轴
            BasePlotConfigObject.PlotAxes.XAxes = new AxesConfig()
            {
                isShowMinorGrid = WpfPlot1.Plot.Axes.Left.IsVisible,
                axesTickFontSize = WpfPlot1.Plot.Axes.Left.Label.FontSize,
                //axesTickSpacing = ((ScottPlot.TickGenerators.NumericFixedInterval)WpfPlot1.Plot.Axes.Left.TickGenerator).Interval,
                Limit = new double[] { WpfPlot1.Plot.Axes.Left.GetRange().Min, WpfPlot1.Plot.Axes.Left.GetRange().Max },
                axesColor = WpfPlot1.Plot.Axes.Left.TickLabelStyle.ForeColor.ToHex(),
                //yLimit = new double[] { WpfPlot1.Plot.Axes.Left.GetRange().Min, WpfPlot1.Plot.Axes.Left.GetRange().Max, }
            };

            // 图表 线段 多边形 注释 存储
            foreach (var tempItem in WpfPlot1.Plot.GetPlottables())
            {
                // 查找到一个多边形就存储一个
                if (tempItem.GetType().Name == "Polygon")
                {
                    // 创建多边形对象
                    var tempPolygon = new PolygonConfig()
                    {
                        Points = new List<PointConfig>(),
                        fillColor = ((ScottPlot.Plottables.Polygon)tempItem).FillColor.ToHex(),
                    };
                    // 记录点位
                    foreach(var point in ((ScottPlot.Plottables.Polygon)tempItem).Coordinates)
                    {
                        tempPolygon.Points.Add(new PointConfig() { x = point.X, y = point.Y});
                    }
                    // 存储多边形
                    BasePlotConfigObject.Polygons.Add(tempPolygon);
                }

                // 存储线段对象
                if (tempItem.GetType().Name == "LinePlot")
                {
                    LinePlot linePlot = ((LinePlot)tempItem);
                    // 创建线段对象
                    var tempLine = new LineConfig()
                    {
                        isShow = linePlot.IsVisible,
                        start = new PointConfig() { x = linePlot.Start.X, y = linePlot.Start.Y },
                        end = new PointConfig() { x = linePlot.End.X, y = linePlot.End.Y },
                        color = linePlot.LineColor.ToHex(),
                        linewidth = linePlot.LineWidth,
                    };
                    // 匹配线的样式
                    if (linePlot.LinePattern.Name == LinePattern.Solid.Name) { tempLine.lineType = 0; }
                    if (linePlot.LinePattern.Name == LinePattern.Dashed.Name) { tempLine.lineType = 1; }
                    if (linePlot.LinePattern.Name == LinePattern.DenselyDashed.Name) { tempLine.lineType = 2; }
                    if (linePlot.LinePattern.Name == LinePattern.Dotted.Name) { tempLine.lineType = 3; }
                    // 存储线段
                    BasePlotConfigObject.Lines.Add(tempLine);
                }

                // 存储文本对象
                if (tempItem.GetType().Name == "Text")
                {
                    ScottPlot.Plottables.Text text = ((ScottPlot.Plottables.Text)tempItem);
                    // 创建线段对象
                    var tempLine = new TextConfig()
                    {
                        isShow = text.IsVisible,
                        text = text.LabelText,                  // 存储文本内容
                        x = text.Location.X,                    // 存储文本 X 坐标
                        y = text.Location.Y,                    // 存储文本 Y 坐标
                        rotation = text.LabelRotation,          // 存储文本旋转
                        fontSize = text.LabelFontSize,          // 存储文本大小
                        color = text.LabelFontColor.ToHex(),             // 存储文本颜色
                    };
                    // 存储线段
                    BasePlotConfigObject.Texts.Add(tempLine);
                }
            }

            // 保存指南
            if(_richTextBox.Document != null)
            {
                BasePlotConfigObject.Description = DocumentHelper.CompressRichTextBoxContent(_richTextBox);
            }

            // 获取对象文件路径
            string tempNewPath = System.IO.Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "Custom", _previousSelectedNode.BaseMapPath.ToString());

            // 序列化对象
            JsonHelper.SerializeToJsonFile(BasePlotConfigObject, tempNewPath);

            // 通知前端
            MessageHelper.Success(LanguageService.Instance["SaveSuccess"]);
        }

        // 脚本设置
        [RelayCommand]
        public void ScriptSetting()
        {
            SetTrue("Script");
        }

        // 确认保存脚本
        [RelayCommand]
        public void ConfirmSaveScript()
        {
            // 关闭脚本设置
            SetTrue(null);
            // 通知前端
            MessageHelper.Success(LanguageService.Instance["SaveSuccess"]);
        }

        // 取消脚本
        [RelayCommand]
        public void CancelSaveScript()
        {
            JsScriptContent = null;
            NeedParamContent = null;
            // 关闭脚本设置
            SetTrue(null);
        }

        // 编辑模式
        [RelayCommand]
        public void EditMode()
        {
            IsPlotMode = false;
            IsEditMode = true;
            RegisterPlotTemplates(true);
            // 禁用右键菜单
            //WpfPlot1.UserInputProcessor.IsEnabled = false;// 禁用右键菜单
            _richTextBox.Document.Blocks.Clear();       // 清除当前的内容
            //NormalPlotTemplate.TAS(WpfPlot1.Plot);    // 调试视图
        }

        // 移动位置
        [RelayCommand]
        public void MovePosition()
        {

        }

        // 删除底图
        [RelayCommand]
        public void DeleteBasemap()
        {
            // 用户选中了节点
            if(_previousSelectedNode != null)
            {
                // 通知：正在删除底图  xxx ，是否确认删除
                Growl.Ask(LanguageService.Instance["DeleteingBasemapConfirm1_Message"] + 
                    _previousSelectedNode.rootNode[_previousSelectedNode.rootNode.Length-1]+
                    LanguageService.Instance["DeleteingBasemapConfirm2_Message"], 
                isConfirmed =>
                {
                    if (isConfirmed)
                    {
                        // 删除节点
                        RemoveNode(RootTreeNode, _previousSelectedNode);

                        // 删除Json列表
                        string basePath = FileHelper.GetAppPath();
                        string tempNewPath = System.IO.Path.Combine(basePath, "Data", "PlotData", "PlotListCustom.json");
                        if (IsEditMode == false)
                        {
                            tempNewPath = System.IO.Path.Combine(basePath, "Data", "PlotData", "PlotList.json");
                        }
                        JsonHelper.RemoveNodeFromJson(tempNewPath, _previousSelectedNode.rootNode);

                        // 获取文件路径
                        tempNewPath = System.IO.Path.Combine(basePath, "Data", "PlotData", "Custom", _previousSelectedNode.BaseMapPath);
                        if (IsEditMode == false)
                        {
                            tempNewPath = System.IO.Path.Combine(basePath, "Data", "PlotData", "Default", _previousSelectedNode.BaseMapPath);
                        }
                        // 删除底图 Json 文件
                        if (FileHelper.IsFileExist(tempNewPath))
                        {
                            FileHelper.DeleteFile(tempNewPath);
                        }

                        // 刷新列表
                        Refresh(true, true);
                    }
                    else
                    {
                        // 通知：取消删除
                        MessageHelper.Info(LanguageService.Instance["Undelete_Message"]);
                    }
                    return true;
                });
                
            }
        }
    }
}
