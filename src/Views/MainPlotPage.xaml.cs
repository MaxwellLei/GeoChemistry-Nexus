using GeoChemistryNexus.ViewModels;
using HandyControl.Controls;
using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// MainPlot.xaml 的交互逻辑
    /// </summary>
    public partial class MainPlotPage : Page
    {
        // 单例本体
        private static MainPlotPage homePage = null;

        private MainPlotViewModel viewModel;

        public MainPlotPage()
        {
            InitializeComponent();
            // 链接 ViewModel
            viewModel = new MainPlotViewModel(this.WpfPlot1,this.Drichtextbox);
            this.DataContext = viewModel;
            // 在原始坐标添加坐标线
            MainPlotViewModel.crosshair = WpfPlot1.Plot.Add.Crosshair(0, 0);

            MainPlotViewModel.myHighlightMarker = WpfPlot1.Plot.Add.Marker(0, 0); // 添加高亮标记
            MainPlotViewModel.myHighlightMarker.Shape = MarkerShape.OpenCircle; // 设置标记形状
            MainPlotViewModel.myHighlightMarker.Size = 17; // 设置标记大小
            MainPlotViewModel.myHighlightMarker.LineWidth = 2; // 设置标记线宽
        }

        public static MainPlotPage GetPage()
        {
            if (homePage == null)
            {
                homePage = new MainPlotPage();
            }
            return homePage;
        }

        private void Color_Pick(object sender, RoutedEventArgs e)
        {
            // Show the popup
            ColorPicker.IsOpen = true;
        }

        private void Color_Pick0000(object sender, RoutedEventArgs e)
        {
            // Show the popup
            ColorPicker0000.IsOpen = true;
        }

        private void Color_Pick000(object sender, RoutedEventArgs e)
        {
            // Show the popup
            ColorPicker000.IsOpen = true;
        }

        private void Color_Pick00(object sender, RoutedEventArgs e)
        {
            // Show the popup
            ColorPicker00.IsOpen = true;
        }

        private void Color_Pick0(object sender, RoutedEventArgs e)
        {
            // Show the popup
            ColorPicker0.IsOpen = true;
        }

        private void Color_Pick1(object sender, RoutedEventArgs e)
        {
            // Show the popup
            ColorPicker1.IsOpen = true;
        }

        private void Color_Pick2(object sender, RoutedEventArgs e)
        {
            // Show the popup
            ColorPicker2.IsOpen = true;
        }

        private void Color_Pick3(object sender, RoutedEventArgs e)
        {
            // Show the popup
            ColorPicker3.IsOpen = true;
        }

        // 坐标显示
        private void PlotLocationShow(object sender, MouseEventArgs e)
        {
            // 获取鼠标在 Plot 上的像素位置
            ScottPlot.Pixel mousePixel = new(e.GetPosition(WpfPlot1).X * WpfPlot1.DisplayScale, e.GetPosition(WpfPlot1).Y * WpfPlot1.DisplayScale);

            // 将像素位置转换为 Plot 的坐标
            var coordinates = WpfPlot1.Plot.GetCoordinates(mousePixel);

            if (MainPlotViewModel.crosshair != null)
            {
                // 设置样式
                MainPlotViewModel.crosshair.TextColor = ScottPlot.Colors.White;
                MainPlotViewModel.crosshair.TextBackgroundColor = MainPlotViewModel.crosshair.HorizontalLine.Color;
                // 创建坐标标识
                MainPlotViewModel.crosshair.Position = coordinates;
                MainPlotViewModel.crosshair.VerticalLine.Text = $"{coordinates.X:N3}";
                MainPlotViewModel.crosshair.HorizontalLine.Text = $"{coordinates.Y:N3}";

                WpfPlot1.Refresh();
            }
            // 输出相关信息
            loca.Text = $"X={coordinates.X:N3}, Y={coordinates.Y:N3}";

            // 获取每个散点图的最近点
            Dictionary<int, DataPoint> nearestPoints = new();
            for (int i = 0; i < viewModel.BaseDataItems.Count; i++)
            {
                DataPoint nearestPoint = ((Scatter)(viewModel.BaseDataItems[i].Plottable)).Data.GetNearest(coordinates, WpfPlot1.Plot.LastRender); // 获取最近点
                nearestPoints.Add(i, nearestPoint); // 将最近点添加到字典中
            }

            // 确定哪个散点图的最近点离鼠标最近
            bool pointSelected = false; // 选中点的标志
            int scatterIndex = -1; // 散点索引
            double smallestDistance = double.MaxValue; // 最小距离初始化为最大值
            for (int i = 0; i < nearestPoints.Count; i++)
            {
                if (nearestPoints[i].IsReal) // 确保点是有效的
                {
                    // 计算点到鼠标的距离
                    double distance = nearestPoints[i].Coordinates.Distance(coordinates);
                    if (distance < smallestDistance) // 如果距离更小
                    {
                        // 存储索引
                        scatterIndex = i;
                        pointSelected = true; // 标志为已选中
                        // 更新最小距离
                        smallestDistance = distance;
                    }
                }
            }

            // 将十字光标、标记和文本放置到选中的点上
            if (pointSelected)
            {
                ScottPlot.Plottables.Scatter scatter = (Scatter)(viewModel.BaseDataItems[scatterIndex].Plottable); // 获取选中的散点图
                DataPoint point = nearestPoints[scatterIndex]; // 获取选中的点

                MainPlotViewModel.myHighlightMarker.IsVisible = true; // 显示高亮标记
                MainPlotViewModel.myHighlightMarker.Location = point.Coordinates; // 设置标记位置
                MainPlotViewModel.myHighlightMarker.MarkerStyle.LineColor = scatter.MarkerStyle.FillColor; // 设置标记颜色
            }
            else
            {
                // 当没有选中点时隐藏高亮标记
                MainPlotViewModel.myHighlightMarker.IsVisible = false; // 隐藏高亮标记
                
            }
            WpfPlot1.Refresh(); // 刷新图表
        }

    }
}
