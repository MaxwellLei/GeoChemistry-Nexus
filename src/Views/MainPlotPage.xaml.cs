using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.ViewModels;
using HandyControl.Controls;
using ScottPlot;
using ScottPlot.Interactivity;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        // 添加状态 -1 无状态; 0 添加点； 1 添加线条； 2 添加多边形； 3 添加注释；
        private int status = -1;

        // 语言变量 0-简体中文 1-英文（美国）
        private string lg;

        private bool PonintEnd = false;

        // 线段起始点
        private Coordinates pointStart;
        // 临时点
        private IPlottable temp;
        // 临时数组存储点
        private List<double> pointsX;
        private List<double> pointsY;

        public MainPlotPage()
        {
            InitializeComponent();
            // 链接 ViewModel
            viewModel = new MainPlotViewModel(this.WpfPlot1,this.Drichtextbox);
            this.DataContext = viewModel;
            // 在原始坐标添加坐标线
            MainPlotViewModel.crosshair = WpfPlot1.Plot.Add.Crosshair(0, 0);
            MainPlotViewModel.crosshair.IsVisible = false;

            MainPlotViewModel.myHighlightMarker = WpfPlot1.Plot.Add.Marker(0, 0); // 添加高亮标记
            MainPlotViewModel.myHighlightMarker.IsVisible = false;
            MainPlotViewModel.myHighlightMarker.Shape = MarkerShape.OpenCircle; // 设置标记形状
            MainPlotViewModel.myHighlightMarker.Size = 17; // 设置标记大小
            MainPlotViewModel.myHighlightMarker.LineWidth = 2; // 设置标记线宽
        }

        public static MainPlotPage GetPage()
        {
            if (homePage == null)
            {
                homePage = new MainPlotPage();
                //homePage.lg = ConfigHelper.GetConfig("language");
            }
            homePage.viewModel.RegisterPlotTemplates();
            return homePage;
        }

        // 语言自适应
        private void AutoChangeLg()
        {
            var tempvalue = ConfigHelper.GetConfig("language");
            if (homePage.lg == "1")
            {
                viewModel.RegisterPlotTemplates();
                homePage.lg = tempvalue;
            }
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

        public (float x, float y) GetLocation(MouseEventArgs e)
        {
            // 从 WpfPlot 和鼠标事件中获取鼠标的像素位置
            double mousePixelX = e.GetPosition(WpfPlot1).X * WpfPlot1.DisplayScale;
            double mousePixelY = e.GetPosition(WpfPlot1).Y * WpfPlot1.DisplayScale;
            ScottPlot.Pixel mousePixel = new(mousePixelX, mousePixelY);

            // 将像素转换为坐标（Plot 的坐标）
            var coordinates = WpfPlot1.Plot.GetCoordinates(mousePixel);

            // 返回 (X, Y)，使用 float 类型
            return ((float)coordinates.X, (float)coordinates.Y);
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

        // 自定义坐标轴-切换状态添加点
        private void AddPointStatus(object sender, RoutedEventArgs e)
        {
            status = 0;
        }



        // 添加点-鼠标点击
        private void AddPoint(object sender, MouseButtonEventArgs e)
        {
            if (status == 0) { return; }

            if (status == 0)
            {
                var (x, y) = GetLocation(e);
                double[] x1 = new double[] { x };
                double[] y1 = new double[] { y };
                WpfPlot1.Plot.Add.ScatterPoints(x1, y1);
                WpfPlot1.Refresh();
                status = -1;
            }
            if (status == 1)
            {
                if(PonintEnd)
                {
                    AddEnd(e);
                    PonintEnd = false;
                    status = -1;
                }
                else
                {
                    AddStart(e);
                    PonintEnd = true;
                }
            }
            if (status == 2)
            {
                AddPolygonR(e);
            }
            if (status == 3)
            {
                AddTextR(e);
                status = -1;
            }
        }

        // 添加线段
        private void AddBorder(object sender, RoutedEventArgs e)
        {
            status = 1;

            //viewModel._basePlotConfig.Lines.Add(new LineConfig() { start = new PointConfig() { x = 0, y = 0 }, end = new PointConfig() { x = 0, y = 0 } });
            
        }

        // 添加注释
        private void AddTextR(MouseButtonEventArgs e)
        {
            var (x, y) = GetLocation(e);
            pointStart = new Coordinates(x, y);
            temp = WpfPlot1.Plot.Add.Text("Text", pointStart);
            viewModel.Refresh();
        }

        // 添加多边形-实际
        private void AddPolygonR(MouseButtonEventArgs e)
        {
            var (x, y) = GetLocation(e);
            pointsX.Add(x);
            pointsY.Add(y);
        }

        // 添加起始点
        private void AddStart(MouseButtonEventArgs e)
        {
            var (x, y) = GetLocation(e);
            pointStart = new Coordinates(x, y);
            Coordinates[] coordinates = new Coordinates[] { pointStart };
            temp = WpfPlot1.Plot.Add.ScatterPoints(coordinates);
                //var lines = viewModel._basePlotConfig.Lines;
                //var lastIndex = lines.Count - 1;
                //lines[lastIndex].start.x = x;
                //lines[lastIndex].start.y = y;
                //viewModel.X1 = x;
                //viewModel.Y1 = y;
                //// 加载底图
                //PlotLoader.LoadBasePlot(WpfPlot1.Plot, viewModel._basePlotConfig);
            WpfPlot1.Refresh();
        }

        // 添加终止点
        private void AddEnd(MouseButtonEventArgs e)
        {
            // 获取指针位置
            var (x, y) = GetLocation(e);
            // 创建线段
            var templine = WpfPlot1.Plot.Add.Line(pointStart.X, pointStart.Y, x, y);
            templine.LineWidth = 2;
            var tempPlotItem = new PlotItemModel()
            {
                Plottable = templine,
                Name = "LinePlot",
                ObjectType = "LinePlot",
            };
            // 选中列表
            viewModel._previousSelectedItems.Clear();
            // 清空选中列表
            viewModel._previousSelectedItems.Add(tempPlotItem);
            WpfPlot1.Plot.Remove(temp);
            //var lines = viewModel._basePlotConfig.Lines;
            //var lastIndex = lines.Count - 1;
            //lines[lastIndex].end.x = x;
            //lines[lastIndex].end.y = y;
            //viewModel.X2 = x;
            //viewModel.Y2 = y;
            //// 加载底图
            //PlotLoader.LoadBasePlot(WpfPlot1.Plot, viewModel._basePlotConfig);
            viewModel.PopulatePlotItems((List<IPlottable>)WpfPlot1.Plot.GetPlottables());
            WpfPlot1.Refresh();
        }

        // 删除添加对象（点，线段，多边形，注释）
        private void DeleteLine(object sender, RoutedEventArgs e)
        {

            //if (viewModel._basePlotConfig.Lines.Count > 0)
            //{
            //    int lastIndex = viewModel._basePlotConfig.Lines.Count - 1;
            //    viewModel._basePlotConfig.Lines.RemoveAt(lastIndex);
            //}
            //// 加载底图
            //PlotLoader.LoadBasePlot(WpfPlot1.Plot, viewModel._basePlotConfig);
            // 删除选中项
            foreach(PlotItemModel temp in viewModel._previousSelectedItems)
            {
                WpfPlot1.Plot.Remove((IPlottable)temp.Plottable);
            }
            viewModel.PopulatePlotItems((List<IPlottable>)WpfPlot1.Plot.GetPlottables());
            WpfPlot1.Refresh();
        }

        // 添加注释
        private void AddText(object sender, RoutedEventArgs e)
        {
            status = 3;
        }

        // 添加多边形
        private void AddPolygon(object sender, RoutedEventArgs e)
        {
            status = 2;
            pointsX = new List<double>();
            pointsY = new List<double>();
        }

        // 右键确认  创建多边形
        private void ConfirmCreate(object sender, MouseButtonEventArgs e)
        {
            if(pointsX!= null && pointsX.Count != 0)
            {
                WpfPlot1.Plot.Add.Polygon(pointsX, pointsY);
                WpfPlot1.Refresh();
                pointsX = null;
                pointsY = null;
            }
            status = -1;
        }
    }
}
