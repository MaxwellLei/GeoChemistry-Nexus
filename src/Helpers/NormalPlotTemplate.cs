using ScottPlot;
using ScottPlot.AxisPanels;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OfficeOpenXml.ExcelErrorValue;

namespace GeoChemistryNexus.Helpers
{
    // 常规投图模板
    public static class NormalPlotTemplate
    {
        // Vermeesch (2006) 【构造环境判别】
        // Vermeesch, P., 2006. Tectonic discrimination diagrams revisited.  Geochemistry, Geophysics, Geosystems 7, Q06017, doi:  10.1029/2005GC001092.
        public static void Vermessch_2006(ScottPlot.Plot plot)
        {
            plot.Axes.Title.Label.Text = "Vermeesch (2006) Tectonic Setting Discrimination Diagram";
            plot.Axes.Title.Label.FontSize = 18;

            // 定义中心点和其他点
            var centerPoint = new ScottPlot.Coordinates(-12.23, -1.37);
            var point1 = new ScottPlot.Coordinates(-12.0, 4.0);  // IAB-OIB
            var point2 = new ScottPlot.Coordinates(-8.0, -6.45); // OIB-MORB
            var point3 = new ScottPlot.Coordinates(-18.0, -6.6); // MORB-IAB


            // 绘制从中心点到每个其他点的线
            var newline1 = plot.Add.Line(centerPoint, point1);
            var newline2 = plot.Add.Line(centerPoint, point2);
            var newline3 = plot.Add.Line(centerPoint, point3);

            // 设置线宽
            newline1.LineWidth = 3;
            newline2.LineWidth = 3;
            newline3.LineWidth = 3;

            // 添加区域标注
            var newtext1 = plot.Add.Text("IAB", -15, 2);   // 区域 A 的大致位置
            var newtext2 = plot.Add.Text("OIB", -9, 0);  // 区域 B 的大致位置
            var newtext3 = plot.Add.Text("MORB", -12, -5);  // 区域 C 的大致位置

            newtext1.LabelFontSize = 20;
            newtext2.LabelFontSize = 20;
            newtext3.LabelFontSize = 20;

            plot.XLabel("DF1");
            plot.YLabel("DF2");

            // 手动设置坐标轴范围
            double xMin = -19;  // X轴的最小值
            double xMax = -7;    // X轴的最大值
            double yMin = -8;  // Y轴的最小值
            double yMax = 5;   // Y轴的最大值

            // 设置视图的轴范围，使中心点位于屏幕中心
            plot.Axes.SetLimits(xMin, xMax, yMin, yMax);


        }

        // Butler_and_Woronow_1986 【构造环境判别】
        // Vermeesch, P., 2006. Tectonic discrimination diagrams revisited.  Geochemistry, Geophysics, Geosystems 7, Q06017, doi:  10.1029/2005GC001092.
        public static void Butler_and_Woronow_1986(ScottPlot.Plot plot)
        {
            plot.Axes.Title.Label.Text = "Butler and Woronow (1986) Tectonic Setting Discrimination Diagram";
            plot.Axes.Title.Label.FontSize = 18;

            // 定义中心点和其他点
            var centerPoint = new ScottPlot.Coordinates(12.17, -12.23);
            var point1 = new ScottPlot.Coordinates(15.9, -10.93);  // IAB-OIB
            var point2 = new ScottPlot.Coordinates(11.85, -16); // OIB-MORB
            var point3 = new ScottPlot.Coordinates(5.02, -6.28); // MORB-IAB


            // 绘制从中心点到每个其他点的线
            var newline1 = plot.Add.Line(centerPoint, point1);
            var newline2 = plot.Add.Line(centerPoint, point2);
            var newline3 = plot.Add.Line(centerPoint, point3);

            // 设置线宽
            newline1.LineWidth = 3;
            newline2.LineWidth = 3;
            newline3.LineWidth = 3;

            // 添加区域标注
            var newtext1 = plot.Add.Text("IAB", 13, -8);   // 区域 A 的大致位置
            var newtext2 = plot.Add.Text("OIB", 14.5, -14);  // 区域 B 的大致位置
            var newtext3 = plot.Add.Text("MORB", 8, -13);  // 区域 C 的大致位置

            newtext1.LabelFontSize = 20;
            newtext2.LabelFontSize = 20;
            newtext3.LabelFontSize = 20;

            plot.XLabel("DF1");
            plot.YLabel("DF2");

            plot.Axes.AutoScale();

        }
    }
}
