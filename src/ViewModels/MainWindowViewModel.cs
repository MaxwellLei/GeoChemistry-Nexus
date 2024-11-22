using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using MahApps.Metro.Controls;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static System.Net.Mime.MediaTypeNames;

namespace GeoChemistryNexus.ViewModels
{
    public partial class MainWindowViewModel: ObservableObject
    {
        // 图层选中列表
        private IList<PlotItemModel> _previousSelectedItems;

        // 添加一个标志来防止递归更新属性
        private bool _isUpdatingLineWidth = false;  

        // 绘图控件
        private WpfPlot WpfPlot1;

        // 测试
        [ObservableProperty]
        private string? _newtitle = "nihao";

        // 图层列表
        [ObservableProperty]
        private ObservableCollection<PlotItemModel> _basePlotItems;

        /// <summary>
        /// 公共属性
        /// </summary>

        // 线是否可见
        [ObservableProperty]
        private bool _plotVisible;

        // 线点 宽度-大小
        [ObservableProperty]
        private float _plotWidth;

        // 线条/字体类型
        [ObservableProperty]
        private int _plotType;

        // 线-文本绘制颜色
        [ObservableProperty]
        private ScottPlot.Color _plotColor;

        

        /// <summary>
        /// 文本属性
        /// </summary>

        // 文本字体列表
        [ObservableProperty]
        private List<string> _plotTextFontNames;

        // 当前文本字体
        [ObservableProperty]
        private int _plotTextFontName;

        // 当前文本内容
        [ObservableProperty]
        private string _plotTextContent;


        /// <summary>
        /// 属性面板显示
        /// </summary>

        // 是否显示边界属性
        [ObservableProperty]
        private bool _plotLineShow = false;

        // 是否显示文本属性
        [ObservableProperty]
        private bool _plotTextShow = false;


        // 初始化
        public MainWindowViewModel(WpfPlot wpfPlot) {
            WpfPlot1 = wpfPlot;     // 获取绘图控件
            BasePlotItems = new ObservableCollection<PlotItemModel>();      // 初始化底图列表
            _previousSelectedItems = new List<PlotItemModel>();     // 初始化图层选中对象
            _plotTextFontNames = new List<string>();        // 字体集合
        }

        // 设置显示属性
        private void SetTrue(string key)
        {
            if (key == null)
            {
                PlotLineShow = false;
                PlotTextShow = false;
                return;
            }

            if (key == "PlotLine")
            {
                PlotLineShow = true;
                PlotTextShow = false;
            }
            else
            {
                PlotLineShow = false;
                PlotTextShow = true;
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

        // 刷新图层列表
        private void PopulatePlotItems(List<IPlottable> plottables)
        {
            if (plottables == null)
                throw new ArgumentNullException(nameof(plottables));

            BasePlotItems.Clear();
            foreach (IPlottable plottable in plottables)
            {
                // 获取绘图名称
                string plottableType = plottable.GetType().Name;
                string displayName = "Error Type";

                if (plottableType == "LinePlot")
                {
                    displayName = "边界";
                }
                if (plottableType == "Text")
                {
                    displayName = "文本";
                }

                BasePlotItems.Add(new PlotItemModel
                {
                    Plottable = plottable,
                    DisplayName = displayName,
                    TypeName = plottableType
                });
            }
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
                    if (item.TypeName == "Text")
                    {
                        var text = (ScottPlot.Plottables.Text)item.Plottable;
                        text.LabelText = value;
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
                    if (item.TypeName == "Text")
                    {
                        var text = (ScottPlot.Plottables.Text)item.Plottable;
                        text.LabelFontName = PlotTextFontNames[value];
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
                    if (item.TypeName == "LinePlot")
                    {
                        var linePlot = (LinePlot)item.Plottable;
                        linePlot.IsVisible = value;
                    }
                    if (item.TypeName == "Text")
                    {
                        var text = (ScottPlot.Plottables.Text)item.Plottable;
                        text.IsVisible = value;
                    }
                }
                // 刷新图形
                WpfPlot1.Refresh();
            }
        }

        // 改变 LinePlot 线类型
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
                    if (item.TypeName == "LinePlot")
                    {
                        var linePlot = (LinePlot)item.Plottable;
                        if (PlotType == 0){linePlot.LineStyle.Pattern = LinePattern.Solid;}
                        if (PlotType == 1) { linePlot.LineStyle.Pattern = LinePattern.Dashed; }
                        if (PlotType == 2) { linePlot.LineStyle.Pattern = LinePattern.DenselyDashed; }
                        if (PlotType == 3) { linePlot.LineStyle.Pattern = LinePattern.Dotted; }
                    }

                    if (item.TypeName == "Text")
                    {
                        var text = (ScottPlot.Plottables.Text)item.Plottable;
                        if (PlotType == 0) { text.LabelStyle.Bold = false; text.LabelStyle.Italic = false;}
                        if (PlotType == 1) { text.LabelStyle.Bold = true; }
                        if (PlotType == 2) { text.LabelStyle.Italic = true; }
                    }
                }
                // 刷新图形
                WpfPlot1.Refresh();
            }
        }


        // 改变 LinePlot 线宽
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
                    if (item.TypeName == "LinePlot")
                    {
                        var linePlot = (LinePlot)item.Plottable;
                        linePlot.LineWidth = value;
                    }
                    if (item.TypeName == "Text")
                    {
                        var text = (ScottPlot.Plottables.Text)item.Plottable;
                        text.LabelFontSize = value;
                    }
                }
                // 刷新图形
                WpfPlot1.Refresh();
            }
        }

        // 改变 LinePlot 颜色
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
                    if (item.TypeName == "LinePlot")
                    {
                        var linePlot = (LinePlot)item.Plottable;
                        linePlot.LineColor = value;
                    }
                    if (item.TypeName == "Text")
                    {
                        var text = (ScottPlot.Plottables.Text)item.Plottable;
                        text.LabelFontColor = value;
                    }
                }
                // 刷新图形
                WpfPlot1.Refresh();
            }
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

                // 匹配字体
                // 获取系统所有字体
                PlotTextFontNames = System.Drawing.FontFamily.Families
                    .Select(f => f.Name)
                    .OrderBy(name => name)
                    .ToList();

                // 当前字体
                PlotTextFontName = FindFontNameIndex(text.LabelFontName);

                // 匹配字体的样式
                if (text.LabelStyle.Italic == true) { PlotType = 2; }
                if (text.LabelStyle.Bold == true) { PlotType = 1; } else
                {
                    PlotType = 0;
                }

                PlotColor = text.LabelFontColor;        // 匹配字体颜色
                PlotTextContent = text.LabelText;       // 匹配文本内容


            }
            finally
            {
                _isUpdatingLineWidth = false;  // 确保标志被重置
            }
        }

        // 文本更新改变
        [RelayCommand]
        public void RefreshText()
        {
            OnPlotTextContentChanged(PlotTextContent);
        }

        // 图层对象选择
        [RelayCommand]
        public void LayersSelection(IList selectedItems)
        {
            // 如果没有选中项，恢复所有对象的正常显示
            if (selectedItems == null || selectedItems.Count == 0)
            {
                foreach (var item in BasePlotItems)
                {
                    SetTrue(null);
                    if (item.TypeName == "LinePlot")
                    {
                        var linePlot = (LinePlot)item.Plottable;
                        linePlot.LineColor = linePlot.LineColor.WithAlpha(1f); // 恢复完全不透明
                    }
                    else if (item.TypeName == "Text")
                    {
                        var text = (ScottPlot.Plottables.Text)item.Plottable;
                        text.Color = text.Color.WithAlpha(1f); // 恢复完全不透明
                    }
                }
                _previousSelectedItems.Clear();
                WpfPlot1.Refresh();
                return;
            }

            // 先将所有对象设置为暗淡效果
            foreach (var item in BasePlotItems)
            {
                if (item.TypeName == "LinePlot")
                {
                    var linePlot = (LinePlot)item.Plottable;
                    linePlot.LineColor = linePlot.LineColor.WithAlpha(0.5f); // 降低透明度
                }
                else if (item.TypeName == "Text")
                {
                    var text = (ScottPlot.Plottables.Text)item.Plottable;
                    text.Color = text.Color.WithAlpha(0.5f); // 降低透明度
                }
            }

            // 恢复选中项的完全不透明效果
            foreach (var item in selectedItems)
            {
                if (item is PlotItemModel plotItem)
                {
                    if (plotItem.TypeName == "LinePlot")
                    {
                        var linePlot = (LinePlot)plotItem.Plottable;
                        linePlot.LineColor = linePlot.LineColor.WithAlpha(1f); // 完全不透明
                        SetTrue("PlotLine");
                        GetLinePlotAttributeMapping(linePlot);
                    }
                    else if (plotItem.TypeName == "Text")
                    {
                        var text = (ScottPlot.Plottables.Text)plotItem.Plottable;
                        text.Color = text.Color.WithAlpha(1f); // 完全不透明
                        SetTrue("PlotText");
                        GetTextAttributeMapping(text);
                    }
                }
            }

            // 更新选中状态
            _previousSelectedItems = selectedItems.Cast<PlotItemModel>().ToList();
            // 刷新绘图
            WpfPlot1.Refresh();
        }

        // 岩浆岩-构造环境-Vermessch_2006底图加载
        [RelayCommand]
        public async void I_TS_Vermessch_2006()
        {
            // 异步绘图
            await Task.Run(() =>
            {
                // 清除绘图
                WpfPlot1.Plot.Clear();

                // 绘制图形
                NormalPlotTemplate.Vermessch_2006(WpfPlot1.Plot);
            });

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
}
