using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScottPlot;
using ScottPlot.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels
{
    public partial class MainWindowViewModel: ObservableObject
    {
        [ObservableProperty]
        public string? _newtitle = "nihao";

        private WpfPlot WpfPlot1;

        public MainWindowViewModel(WpfPlot wpfPlot) {
            WpfPlot1 = wpfPlot;
        }

        [RelayCommand]
        public async void Test()
        {
            // 异步绘图
            await Task.Run(() =>
            {
                // 使用你的 WPF Plot 控件
                var plot = WpfPlot1.Plot;

                // 清除现有的绘图（如果有的话）
                plot.Clear();

                // 定义中心点和其他点
                var centerPoint = new ScottPlot.Coordinates(-12.23, -1.37);
                var point1 = new ScottPlot.Coordinates(-12.0, 4.0);  // IAB-OIB
                var point2 = new ScottPlot.Coordinates(-8.0, -6.45); // OIB-MORB
                var point3 = new ScottPlot.Coordinates(-18.0, -6.6); // MORB-IAB

                // 绘制从中心点到每个其他点的线
                plot.Add.Line(centerPoint, point1);
                plot.Add.Line(centerPoint, point2);
                plot.Add.Line(centerPoint, point3);

                // 添加区域标注
                plot.Add.Text("A", -15, 2);   // 区域 A 的大致位置
                plot.Add.Text("B", -10, -5);  // 区域 B 的大致位置
                plot.Add.Text("C", -15, -8);  // 区域 C 的大致位置

                // 手动设置坐标轴范围
                double xMin = -20;  // X轴的最小值 例如
                double xMax = 0;    // X轴的最大值 例如
                double yMin = -10;  // Y轴的最小值 例如
                double yMax = 10;   // Y轴的最大值 例如

                // 设置视图的轴范围，使中心点位于屏幕中心
                plot.Axes.SetLimits(xMin, xMax, yMin, yMax);

            });

            // 刷新图形界面
            WpfPlot1.Refresh();
        }


    }
}
