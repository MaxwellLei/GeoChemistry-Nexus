using ScottPlot;
using ScottPlot.AxisPanels;
using ScottPlot.Plottables;
using ScottPlot.TickGenerators;
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
        // 重置坐标轴
        private static void ResetPlot(ScottPlot.Plot plot)
        {
            // 重置刻度轴限制
            plot.Axes.SetLimits(double.NaN, double.NaN, double.NaN, double.NaN);    
            // 重置刻度轴
            plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic();     
            plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic();
            // 重置坐标轴标题字体大小
            plot.Axes.Left.Label.FontSize = 12;     
            plot.Axes.Bottom.Label.FontSize = 12;
            // 重置坐标轴刻度字体大小
            plot.Axes.Left.TickLabelStyle.FontSize  = 12;
            plot.Axes.Bottom.TickLabelStyle.FontSize = 12;
        }

        // Vermeesch (2006) 【构造环境判别】
        public static void Vermessch_2006(ScottPlot.Plot plot)
        {
            ResetPlot(plot);
            plot.Axes.Title.Label.Text = "Vermeesch (2006) Tectonic Setting Discrimination Diagram";
            plot.Axes.Title.Label.FontSize = 18;

            // 定义中心点和其他点
            var centerPoint = new ScottPlot.Coordinates(-12.23, -1.37);
            var point1 = new ScottPlot.Coordinates(-12.0, 4.0);  // IAB-OIB
            var point2 = new ScottPlot.Coordinates(-8.0, -6.45); // OIB-MORB
            var point3 = new ScottPlot.Coordinates(-18.0, -6.6); // MORB-IAB


            // 绘制从中心点到每个其他点的线
            var newline1 = plot.Add.Line(centerPoint, point1).LineWidth =3;
            var newline2 = plot.Add.Line(centerPoint, point2).LineWidth = 3;
            var newline3 = plot.Add.Line(centerPoint, point3).LineWidth = 3;

            // 添加区域标注
            var newtext1 = plot.Add.Text("IAB", -15, 2).LabelFontSize = 20;   // 区域 A 的大致位置
            var newtext2 = plot.Add.Text("OIB", -9, 0).LabelFontSize = 20; ;  // 区域 B 的大致位置
            var newtext3 = plot.Add.Text("MORB", -12, -5).LabelFontSize = 20; ;  // 区域 C 的大致位置

            // 设置标题属性
            plot.XLabel("DF1");
            plot.Axes.Bottom.Label.FontSize = 20;
            plot.YLabel("DF2");
            plot.Axes.Left.Label.FontSize = 20;

            plot.Axes.AutoScale();
        }

        //  Vermeesch (2006) b 【构造环境判别】
        public static void Vermessch_2006_b(ScottPlot.Plot plot)
        {
            ResetPlot(plot);
            plot.Axes.Title.Label.Text = "Vermessch (2006) Tectonic Setting Discrimination Diagram";
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

        //  Saccani (2015) 【构造环境判别】
        public static void Saccani_2015(ScottPlot.Plot plot)
        {
            ResetPlot(plot);
            plot.Axes.Title.Label.Text = "Saccani (2005) Tectonic Setting Discrimination Diagram";
            plot.Axes.Title.Label.FontSize = 18;

            // 定义中心点和其他点
            var centerPoint1 = new ScottPlot.Coordinates(Math.Log10(2.2), Math.Log10(8.0));
            var centerPoint2 = new ScottPlot.Coordinates(Math.Log10(0.306), Math.Log10(0.708));
            var point1 = new ScottPlot.Coordinates(Math.Log10(0.01), Math.Log10(0.1));
            var point2 = new ScottPlot.Coordinates(Math.Log10(0.01), Math.Log10(20));
            var point3 = new ScottPlot.Coordinates(Math.Log10(0.5), Math.Log10(0.01));
            var point4 = new ScottPlot.Coordinates(Math.Log10(100), Math.Log10(1000));


            // 绘制从中心点到每个其他点的线
            var newline1 = plot.Add.Line(centerPoint1, point2).LineWidth = 3;
            var newline2 = plot.Add.Line(centerPoint1, point4).LineWidth = 3;
            var newline3 = plot.Add.Line(centerPoint1, centerPoint2).LineWidth = 3;
            var newline4 = plot.Add.Line(centerPoint2, point1).LineWidth = 3;
            var newline5 = plot.Add.Line(centerPoint2, point3).LineWidth = 3;

            // 添加区域标注
            var newtext1 = plot.Add.Text("MTB", Math.Log10(0.1), Math.Log10(0.1));
            var newtext2 = plot.Add.Text("IAT", Math.Log10(0.1), Math.Log10(4));
            var newtext3 = plot.Add.Text("CAB", Math.Log10(2), Math.Log10(100));
            var newtext4 = plot.Add.Text("MORB-OIB", Math.Log10(5), Math.Log10(1));

            newtext1.LabelFontSize = 20;
            newtext2.LabelFontSize = 20;
            newtext3.LabelFontSize = 20;
            newtext4.LabelFontSize = 20;

            plot.XLabel("Nb_n");
            plot.YLabel("Th_n");


            // 创建一个次刻度生成器，用于放置对数分布的次刻度
            ScottPlot.TickGenerators.LogMinorTickGenerator minorTickGen = new();

            // 创建一个数字刻度生成器，使用我们自定义的次刻度生成器
            ScottPlot.TickGenerators.NumericAutomatic tickGen = new();
            tickGen.MinorTickGenerator = minorTickGen;

            // 自定义刻度格式化器以显示我们想要的刻度
            static string LogTickLabelFormatter(double y)
            {
                if (y == -1) return "0.1";
                else if (y == 0) return "1";
                else if (y == 1) return "10";
                else if (y == 2) return "100";
                return string.Empty; // 对于其他值返回空值
            }

            //// 告诉我们的主刻度生成器仅显示整数的主刻度
            //tickGen.IntegerTicksOnly = true;

            // 告诉我们的自定义刻度生成器使用我们的新标签格式化器
            tickGen.LabelFormatter = LogTickLabelFormatter;

            // 告诉左侧坐标轴使用我们的自定义刻度生成器
            plot.Axes.Left.TickGenerator = tickGen;
            plot.Axes.Bottom.TickGenerator = tickGen;

            plot.Axes.SetLimits(0.01, 100, 0.01, 1000);

            plot.Axes.AutoScale();

        }

        //  Saccani (2015) b【构造环境判别】
        public static void Saccani_2015_b(ScottPlot.Plot plot)
        {
            ResetPlot(plot);
            plot.Axes.Title.Label.Text = "Saccani (2005) Tectonic Setting Discrimination Diagram";
            plot.Axes.Title.Label.FontSize = 18;

            // 定义中心点和其他点
            var point1 = new ScottPlot.Coordinates(9.5, 0);  // IAB-OIB
            var point2 = new ScottPlot.Coordinates(7, 25); // OIB-MORB


            // 绘制从中心点到每个其他点的线
            var newline1 = plot.Add.Line(point1, point2).LineWidth = 3;

            // 添加区域标注
            var newtext1 = plot.Add.Text("BON", 4, 15).LabelFontSize = 20;   // 区域 A 的大致位置
            var newtext2 = plot.Add.Text("IAT", 15, 15).LabelFontSize = 20; ;  // 区域 B 的大致位置

            // 设置标题属性
            plot.XLabel("Dy");
            plot.Axes.Bottom.Label.FontSize = 20;
            plot.YLabel("Yb");
            plot.Axes.Left.Label.FontSize = 20;

            plot.Axes.SetLimits(0, 20, 0, 25);
        }

        //  Saccani (2015) b【构造环境判别】
        public static void Saccani_2015_c(ScottPlot.Plot plot)
        {
            ResetPlot(plot);
            plot.Axes.Title.Label.Text = "Saccani (2005) Tectonic Setting Discrimination Diagram";
            plot.Axes.Title.Label.FontSize = 18;

            // 定义中心点和其他点
            var point1 = new ScottPlot.Coordinates(0.4, 1.5);  // IAB-OIB
            var point2 = new ScottPlot.Coordinates(2, 0.9); // OIB-MORB


            // 绘制从中心点到每个其他点的线
            var newline1 = plot.Add.Line(point1, point2).LineWidth = 3;

            // 添加区域标注
            var newtext1 = plot.Add.Text("G-MORB", 1.5, 1.5).LabelFontSize = 20;   // 区域 A 的大致位置
            var newtext2 = plot.Add.Text("N-MORB", 0.6, 1).LabelFontSize = 20; ;  // 区域 B 的大致位置

            // 设置标题属性
            plot.XLabel("Ce/Yb");
            plot.Axes.Bottom.Label.FontSize = 20;
            plot.YLabel("Dy/Yb");
            plot.Axes.Left.Label.FontSize = 20;

            plot.Axes.SetLimits(0, 2.5, 0.8, 1.7);
        }
    }
}
