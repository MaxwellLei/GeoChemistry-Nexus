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

        // Pearce and Gale (1977) 【构造环境判别】
        public static void Pearce_and_Gale_1977(ScottPlot.Plot plot)
        {
            ResetPlot(plot);
            plot.Axes.Title.Label.Text = "Pearce and Gale (1977) Tectonic Setting Discrimination Diagram";
            plot.Axes.Title.Label.FontSize = 18;

            // 定义中心点和其他点
            var point1 = new ScottPlot.Coordinates(513, 0);  // IAB-OIB
            var point2 = new ScottPlot.Coordinates(313, 7.5); // OIB-MORB


            // 绘制从中心点到每个其他点的线
            var newline1 = plot.Add.Line(point2, point1).LineWidth = 3;

            // 添加区域标注
            var newtext1 = plot.Add.Text("plate margin\n basalts", 280, 4).LabelFontSize = 20;   // 区域 A 的大致位置
            var newtext2 = plot.Add.Text("within-plate\n basalts", 460, 6).LabelFontSize = 20; ;  // 区域 B 的大致位置

            // 设置标题属性
            plot.XLabel("Ti/Y");
            plot.Axes.Bottom.Label.FontSize = 20;
            plot.YLabel("Zr/Y");
            plot.Axes.Left.Label.FontSize = 20;

            plot.Axes.SetLimits(250, 580, -1, 9);
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

        //  Vermeesch (2006) b 【构造环境判别】
        public static void Vermessch_2006_c(ScottPlot.Plot plot)
        {
            ResetPlot(plot);
            plot.Axes.Title.Label.Text = "Vermessch (2006) Tectonic Setting Discrimination Diagram";
            plot.Axes.Title.Label.FontSize = 18;

            //定义数据点
            var dataPoints = new ScottPlot.Coordinates[]
            {
                new ScottPlot.Coordinates(1.8116883624797877, 6.858892553142141),
                new ScottPlot.Coordinates(2.130123877662302, 17.433858422638195),
                new ScottPlot.Coordinates(2.389018580037413, 27.813841190418998),
                new ScottPlot.Coordinates(2.6479983999948518, 39.111391495683165),
                new ScottPlot.Coordinates(2.9070495059274863, 51.177404613589715),
                new ScottPlot.Coordinates(3.10642680283033, 61.6147353524633),
                new ScottPlot.Coordinates(3.285888713408249, 71.24919449603885),
                new ScottPlot.Coordinates(3.485334529964864, 82.4251671025865),
                new ScottPlot.Coordinates(3.66484336161004, 92.56543535119977),
                new ScottPlot.Coordinates(3.8444639100820224, 103.91001099275991),
                new ScottPlot.Coordinates(4.063966620944179, 117.41431122567172),
                new ScottPlot.Coordinates(4.2238631912544955, 130.0193952718497),
                new ScottPlot.Coordinates(4.397949926508473, 142.81946241974293),
                new ScottPlot.Coordinates(4.608928749882467, 156.10125252481492),
                new ScottPlot.Coordinates(4.743167688970133, 167.11206297461558),
                new ScottPlot.Coordinates(4.866011511723793, 177.5035153366149),
                new ScottPlot.Coordinates(5.029733762703109, 190.609438330336),
                new ScottPlot.Coordinates(5.216390236927203, 206.50629591723566),
                new ScottPlot.Coordinates(5.368724464704372, 219.06932478461232),
                new ScottPlot.Coordinates(5.503013410371655, 230.61920616268446),
                new ScottPlot.Coordinates(5.623183477070474, 242.74256725168368),
                new ScottPlot.Coordinates(5.743316304827023, 254.46449254303394),
                new ScottPlot.Coordinates(5.872094951008424, 267.72334345966885),
                new ScottPlot.Coordinates(6.028698800844913, 283.02664954575897),
                new ScottPlot.Coordinates(6.164034426782373, 298.2209944867729),
                new ScottPlot.Coordinates(6.289871196524602, 310.3214163873351),
                new ScottPlot.Coordinates(6.424364424389469, 324.07345985536733),
                new ScottPlot.Coordinates(6.544690894645812, 337.8828512944923),
                new ScottPlot.Coordinates(6.664927991440714, 350.7287968192597),
                new ScottPlot.Coordinates(6.785388521889222, 365.98335712992093),
                new ScottPlot.Coordinates(6.905685200991752, 379.4715999309267),
                new ScottPlot.Coordinates(7.0261382836518065, 394.6458730820582),
                new ScottPlot.Coordinates(7.166359286425405, 409.0333320697977),
                new ScottPlot.Coordinates(7.206997399343622, 419.34220335342354),
                new ScottPlot.Coordinates(7.347204996098003, 433.5851454540094),
                new ScottPlot.Coordinates(7.467628287604245, 448.4382699670217),
                new ScottPlot.Coordinates(7.588155848148836, 464.4154147134512),
                new ScottPlot.Coordinates(7.708534452924355, 478.7868162692847),
                new ScottPlot.Coordinates(7.786161688006437, 490.6234375028204),
                new ScottPlot.Coordinates(7.841808158628434, 503.8364100425812),
                new ScottPlot.Coordinates(7.958812064058651, 518.4945514538782),
                new ScottPlot.Coordinates(8.028325465605427, 534.5290441714004),
                new ScottPlot.Coordinates(8.137316401835392, 548.3613747989625),
            };

            // 绘制每个数据点并连接成线
            for (int i = 0; i < dataPoints.Length - 1; i++)
            {
                LinePlot tempLine = plot.Add.Line(dataPoints[i], dataPoints[i + 1]);
                tempLine.LineColor = Colors.Black;
                tempLine.LineWidth = 3;
            }

            //// 添加区域标注
            //var newtext1 = plot.Add.Text("IAB", 13, -8);   // 区域 A 的大致位置
            //var newtext2 = plot.Add.Text("OIB", 14.5, -14);  // 区域 B 的大致位置
            //var newtext3 = plot.Add.Text("MORB", 8, -13);  // 区域 C 的大致位置

            //newtext1.LabelFontSize = 20;
            //newtext2.LabelFontSize = 20;
            //newtext3.LabelFontSize = 20;

            plot.XLabel("Ti/1000");
            plot.YLabel("V");

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
            plot.Axes.Bottom.Label.FontSize = 20;
            plot.YLabel("Th_n");
            plot.Axes.Left.Label.FontSize = 20;


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
            plot.XLabel("Dy_n");
            plot.Axes.Bottom.Label.FontSize = 20;
            plot.YLabel("Yb_n");
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

        //  TAS【构造环境判别】
        public static void TAS(ScottPlot.Plot plot)
        {
            ResetPlot(plot);
            plot.Axes.Title.Label.Text = "The total alkalis versus silica (TAS) diagram";
            plot.Axes.Title.Label.FontSize = 18;

            // 定义中心点和其他点
            var point1 = new ScottPlot.Coordinates(41, 3);
            var point2 = new ScottPlot.Coordinates(41, 7);
            var point3 = new ScottPlot.Coordinates(45, 3);
            var point4 = new ScottPlot.Coordinates(45, 5);
            var point5 = new ScottPlot.Coordinates(45, 9.4);
            var point6 = new ScottPlot.Coordinates(48.4, 11.5);
            var point7 = new ScottPlot.Coordinates(49.4, 7.3);
            var point8 = new ScottPlot.Coordinates(52, 5);
            var point9 = new ScottPlot.Coordinates(52.5, 14);
            var point10 = new ScottPlot.Coordinates(53, 9.3);
            var point11 = new ScottPlot.Coordinates(57, 5.9);
            var point12 = new ScottPlot.Coordinates(57.6, 11.7);
            var point13 = new ScottPlot.Coordinates(61, 13.5);
            var point14 = new ScottPlot.Coordinates(63, 7);
            var point15 = new ScottPlot.Coordinates(69, 8);

            var point16 = new ScottPlot.Coordinates(41, 0.33);
            var point17 = new ScottPlot.Coordinates(45, 0.33);
            var point18 = new ScottPlot.Coordinates(52, 0.33);
            var point19 = new ScottPlot.Coordinates(57, 0.33);
            var point20 = new ScottPlot.Coordinates(63, 0.33);
            var point21 = new ScottPlot.Coordinates(76.6, 0.33);
            var point22 = new ScottPlot.Coordinates(69, 12.56);
            var point23 = new ScottPlot.Coordinates(63.52, 14.83);
            var point24 = new ScottPlot.Coordinates(50, 15.43);

            

            // 绘制从中心点到每个其他点的线
            var newline1 = plot.Add.Line(point1, point2).LineWidth = 3;
            var newline2 = plot.Add.Line(point1, point3).LineWidth = 3;
            var newline3 = plot.Add.Line(point2, point5).LineWidth = 3;
            var newline4 = plot.Add.Line(point5, point6).LineWidth = 3;
            var newline5 = plot.Add.Line(point6, point9).LineWidth = 3;
            var newline6 = plot.Add.Line(point3, point4).LineWidth = 3;
            var newline7 = plot.Add.Line(point4, point7).LineWidth = 3;
            var newline8 = plot.Add.Line(point4, point8).LineWidth = 3;
            var newline9 = plot.Add.Line(point7, point8).LineWidth = 3;
            var newline10 = plot.Add.Line(point7, point10).LineWidth = 3;
            var newline11 = plot.Add.Line(point10, point12).LineWidth = 3;
            var newline12 = plot.Add.Line(point12, point13).LineWidth = 3;
            var newline13 = plot.Add.Line(point10, point11).LineWidth = 3;
            var newline14 = plot.Add.Line(point12, point14).LineWidth = 3;
            var newline15 = plot.Add.Line(point14, point15).LineWidth = 3;
            var newline16 = plot.Add.Line(point5, point7).LineWidth = 3;
            var newline17 = plot.Add.Line(point6, point10).LineWidth = 3;
            var newline18 = plot.Add.Line(point9, point12).LineWidth = 3;
            var newline19 = plot.Add.Line(point1, point16).LineWidth = 3;
            var newline20 = plot.Add.Line(point3, point17).LineWidth = 3;
            var newline21 = plot.Add.Line(point8, point18).LineWidth = 3;
            var newline22 = plot.Add.Line(point11, point19).LineWidth = 3;
            var newline23 = plot.Add.Line(point15, point22).LineWidth = 3;
            var newline24 = plot.Add.Line(point15, point21).LineWidth = 3;
            var newline25 = plot.Add.Line(point13, point23).LineWidth = 3;
            var newline26 = plot.Add.Line(point9, point24).LineWidth = 3;
            var newline27 = plot.Add.Line(point8, point11).LineWidth = 3;
            var newline28 = plot.Add.Line(point11, point14).LineWidth = 3;
            var newline29 = plot.Add.Line(point14, point20).LineWidth = 3;

            foreach(var temp in plot.GetPlottables())
            {
                if(temp.GetType().Name == "LinePlot")
                {
                    ((LinePlot)temp).Color = ScottPlot.Colors.Black;
                }
            }


            //// 添加区域标注
            var newtext1 = plot.Add.Text("Foidite", 36.5, 2.1);
            var newtext2 = plot.Add.Text("Picro-\nbasalt", 41.8, 2.1);
            var newtext3 = plot.Add.Text("Basalt", 47, 2.5);
            var newtext14 = plot.Add.Text("Basaltic\nandesite", 53, 3.4);
            var newtext10 = plot.Add.Text("Andesite", 58, 3.8);
            //var newtext13 = plot.Add.Text("Boninite", 47, 1.8);
            var newtext11 = plot.Add.Text("Dacite", 66, 4.1);
            var newtext12 = plot.Add.Text("Rhyolite", 75, 5);
            var newtext15 = plot.Add.Text("Trachy\nbasalt", 47.7, 6);
            var newtext9 = plot.Add.Text("Basaltic\n trachy\n  andesite", 51.4, 7.6);
            var newtext8 = plot.Add.Text("Trachy\nandesite", 56, 8.8);
            var newtext16 = plot.Add.Text("Phonotephrite", 46, 9.5);
            var newtext4 = plot.Add.Text("Tephriphonolite", 49.9, 11.8);
            var newtext5 = plot.Add.Text("Phonolite", 55.8, 14);
            var newtext17 = plot.Add.Text("Foidite", 40, 11.3);
            var newtext6 = plot.Add.Text("Trachyte", 60, 10.7);
            var newtext7 = plot.Add.Text("Trachydacite", 62.3, 8.6);
            
            

            // 设置标题属性
            plot.XLabel("SiO2");
            plot.Axes.Bottom.Label.FontSize = 20;
            plot.YLabel("Na2O + K2O");
            plot.Axes.Left.Label.FontSize = 20;

            plot.Axes.SetLimits(35, 80, 0, 16);
        }
    }
}
