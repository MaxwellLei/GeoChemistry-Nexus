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
        //图层选中列表
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
        /// 线属性
        /// </summary>

        // 线条类型
        [ObservableProperty]
        private int _plotLineType;

        // 线宽度
        [ObservableProperty]
        private float _plotLineWidth;

        // 线颜色
        [ObservableProperty]
        private ScottPlot.Color _plotLineColor;



        // 初始化
        public MainWindowViewModel(WpfPlot wpfPlot) {
            WpfPlot1 = wpfPlot;     // 获取绘图控件
            BasePlotItems = new ObservableCollection<PlotItemModel>();      // 初始化底图列表
            _previousSelectedItems = new List<PlotItemModel>();     // 初始化图层选中对象
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

        // 改变 LinePlot 线宽
        partial void OnPlotLineTypeChanged(int value)
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
                        linePlot.LineStyle.Pattern = LinePattern.DenselyDashed;
                    }
                }
                // 刷新图形
                WpfPlot1.Refresh();
            }
        }


        // 改变 LinePlot 线宽
        partial void OnPlotLineWidthChanged(float value)
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
                        item.Tag = value;  // 更新保存的原始线宽
                        linePlot.LineWidth = value;
                    }
                }
                // 刷新图形
                WpfPlot1.Refresh();
            }
        }

        // 改变 LinePlot 颜色
        partial void OnPlotLineColorChanged(ScottPlot.Color value)
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
                }
                // 刷新图形
                WpfPlot1.Refresh();
            }
        }

        // LinePlot 属性匹配
        private void GetLinePlotAttributeMapping(PlotItemModel plotItem)
        {
            _isUpdatingLineWidth = true;  // 设置标志，防止触发更新
            try
            {
                PlotLineWidth = ((LinePlot)plotItem.Plottable).LineWidth;
                PlotLineColor = ((LinePlot)plotItem.Plottable).LineColor;
                OnPropertyChanged(nameof(PlotLineWidth));  // 手动触发属性更新
            }
            finally
            {
                _isUpdatingLineWidth = false;  // 确保标志被重置
            }
        }

        // LinePlot 高亮显示
        private void LinePlotHighlight(PlotItemModel plotItem, bool Highlight)
        {
            var linePlot = (LinePlot)plotItem.Plottable;
            if (Highlight)
            {
                // 保存原始颜色到 Tag
                plotItem.Tag = linePlot.LineColor;
            }
            else
            {
                // 恢复原始颜色
                linePlot.LineColor = (ScottPlot.Color)plotItem.Tag;
            }
        }

        // Text 属性匹配
        private void GetTextAttributeMapping(PlotItemModel plotItem)
        {
            //PlotLineWidth = ((Text)plotItem.Plottable).LineWidth;
            //PlotLineColor = ((Text)plotItem.Plottable).LineColor;
        }

        // Text 高亮显示
        private void TextHighlight(PlotItemModel plotItem, bool Highlight)
        {
            var text = (ScottPlot.Plottables.Text)plotItem.Plottable;

            if (Highlight)
            {
                // 保存原始颜色到 Tag
                plotItem.Tag = text.Color;
            }
            else
            {
                // 恢复原始颜色
                text.Color = (ScottPlot.Color)plotItem.Tag;
            }
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
                    linePlot.LineColor = linePlot.LineColor.WithAlpha(0.3f); // 降低透明度
                }
                else if (item.TypeName == "Text")
                {
                    var text = (ScottPlot.Plottables.Text)item.Plottable;
                    text.Color = text.Color.WithAlpha(0.3f); // 降低透明度
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
                        GetLinePlotAttributeMapping(plotItem);
                    }
                    else if (plotItem.TypeName == "Text")
                    {
                        var text = (ScottPlot.Plottables.Text)plotItem.Plottable;
                        text.Color = text.Color.WithAlpha(1f); // 完全不透明
                        GetTextAttributeMapping(plotItem);
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
