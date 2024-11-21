using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Helpers
{
    // 常规投图模板
    public static class NormalPlotTemplate
    {
        // Vermeesch (2006) 【构造环境判别】
        // Vermeesch, P., 2006. Tectonic discrimination diagrams revisited.  Geochemistry, Geophysics, Geosystems 7, Q06017, doi:  10.1029/2005GC001092.
        public static void Vermessch_2006(ScottPlot.Plot plot)
        {
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
            plot.Add.Text("B", -9, 0);  // 区域 B 的大致位置
            plot.Add.Text("C", -12, -5);  // 区域 C 的大致位置

            // 手动设置坐标轴范围
            double xMin = -19;  // X轴的最小值
            double xMax = -7;    // X轴的最大值
            double yMin = -8;  // Y轴的最小值
            double yMax = 5;   // Y轴的最大值

            // 设置视图的轴范围，使中心点位于屏幕中心
            plot.Axes.SetLimits(xMin, xMax, yMin, yMax);
        }
    }
}
