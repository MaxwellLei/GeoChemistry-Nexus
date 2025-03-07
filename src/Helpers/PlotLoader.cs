using GeoChemistryNexus.Models;
using ScottPlot;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using ScottPlot.Plottables;
using System.Windows.Controls;

namespace GeoChemistryNexus.Helpers
{
    public static class PlotLoader
    {
        /// <summary>
        /// 加载底图到传入的plot对象中，支持从JSON文件加载或直接使用配置对象
        /// </summary>
        /// <param name="plot">要加载底图的Plot对象</param>
        /// <param name="source">JSON文件路径或BasePlotConfig对象</param>
        /// <returns>底图的基础信息(BaseInfo)，加载失败时返回null</returns>
        public static BaseInfo LoadBasePlot(Plot plot, object source, RichTextBox richTextBox)
        {
            BasePlotConfig config = null;

            // 根据source类型确定处理方式
            if (source is string jsonFilePath)
            {
                if (!File.Exists(jsonFilePath))
                {
                    Console.WriteLine($"文件不存在：{jsonFilePath}");
                    return null;
                }
                // 读取JSON文件并反序列化
                string json = File.ReadAllText(jsonFilePath);
                config = JsonHelper.Deserialize<BasePlotConfig>(json);
                if (config == null)
                {
                    Console.WriteLine("反序列化失败");
                    return null;
                }
            }
            else if (source is BasePlotConfig configObj)
            {
                config = configObj;
            }
            else
            {
                Console.WriteLine("不支持的源类型");
                return null;
            }

            // 重置 plot（例如清空已有内容）
            plot.Clear();

            // 设置图表标题
            plot.Title(config.Title, size: 18);

            // 设置 X、Y 轴标签
            plot.XLabel(config.PlotConfig?.x ?? "");
            plot.YLabel(config.PlotConfig?.y ?? "");

            // 设置轴范围
            if (config.PlotAxes.XAxes != null)
            {
                var xAxes = config.PlotAxes.XAxes;
                plot.Axes.Bottom.TickLabelStyle.FontSize = xAxes.axesTickFontSize;    // 匹配字体大小
                plot.Axes.Bottom.TickLabelStyle.ForeColor = 
                    new ScottPlot.Color(System.Drawing.ColorTranslator.FromHtml(xAxes.axesColor));   // 匹配字体大小
                //plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericFixedInterval(xAxes.axesTickSpacing);
                // 不为空且有起点和终点
                if (xAxes.Limit != null && xAxes.Limit.Length >= 2)
                {
                    plot.Axes.Bottom.Min = xAxes.Limit[0];
                    plot.Axes.Bottom.Min = xAxes.Limit[1];
                }
            }

            if (config.PlotAxes.YAxes != null)
            {
                var xAxes = config.PlotAxes.YAxes;
                plot.Axes.Left.TickLabelStyle.FontSize = xAxes.axesTickFontSize;    // 匹配字体大小
                plot.Axes.Left.TickLabelStyle.ForeColor =
                    new ScottPlot.Color(System.Drawing.ColorTranslator.FromHtml(xAxes.axesColor));   // 匹配字体大小
                //plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericFixedInterval(xAxes.axesTickSpacing);
                // 不为空且有起点和终点
                if (xAxes.Limit != null && xAxes.Limit.Length >= 2)
                {
                    plot.Axes.Left.Min = xAxes.Limit[0];
                    plot.Axes.Left.Min = xAxes.Limit[1];
                }
            }

            // 添加点
            if (config.Points != null && config.Points.Count > 0)
            {
                // 将所有点的坐标汇总
                double[] xs = config.Points.Select(pt => pt.x).ToArray();
                double[] ys = config.Points.Select(pt => pt.y).ToArray();
                // 添加点
                plot.Add.ScatterPoints(xs, ys, color: ScottPlot.Colors.Black);
            }

            // 添加多边形
            if (config.Polygons != null)
            {
                foreach (var poly in config.Polygons)
                {
                    if (poly.Points != null && poly.Points.Count > 0)
                    {
                        var coords = new Coordinates[poly.Points.Count];
                        for (int i = 0; i < poly.Points.Count; i++)
                        {
                            coords[i] = new Coordinates(poly.Points[i].x, poly.Points[i].y);
                        }
                        // ScottPlot.Add.Polygon 返回 PolygonPlot 对象
                        var polygon = plot.Add.Polygon(coords);
                        // 利用 System.Drawing.ColorTranslator 根据 HTML 格式转换颜色
                        polygon.FillColor = new ScottPlot.Color(System.Drawing.ColorTranslator.FromHtml(poly.fillColor));
                        polygon.LineStyle.Width = 0;  // 如若不需要边框则设置宽度为 0
                    }
                }
            }

            // 添加线段
            if (config.Lines != null)
            {
                foreach (var ln in config.Lines)
                {
                    var start = new Coordinates(ln.start.x, ln.start.y);
                    var end = new Coordinates(ln.end.x, ln.end.y);
                    var line = plot.Add.Line(start, end);   // 线条位置
                    line.Color = new ScottPlot.Color(System.Drawing.ColorTranslator.FromHtml(ln.color));
                    line.LineWidth = ln.linewidth;  // 线宽
                    if (ln.lineType == 0) { line.LineStyle.Pattern = LinePattern.Solid; }
                    if (ln.lineType == 1) { line.LineStyle.Pattern = LinePattern.Dashed; }
                    if (ln.lineType == 2) { line.LineStyle.Pattern = LinePattern.DenselyDashed; }
                    if (ln.lineType == 3) { line.LineStyle.Pattern = LinePattern.Dotted; }
                }
            }

            // 添加文本标注
            if (config.Texts != null)
            {
                foreach (var txt in config.Texts)
                {
                    var text = plot.Add.Text(txt.text, txt.x, txt.y);
                    text.FontSize = txt.fontSize;
                    text.LabelRotation = (float)txt.rotation;
                }
            }

            // 设置坐标轴刻度
            if (config.Ticks != null)
            {

            }

            DocumentHelper.DecompressRichTextBoxContent(richTextBox,config.Description);

            return config.baseInfo;
        }

        // 加载底图列表
        public static PlotListConfig LoadBasePlotList(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
            {
                Console.WriteLine($"文件不存在：{jsonFilePath}");
                return null;
            }

            // 读取 JSON 字符串
            string json = File.ReadAllText(jsonFilePath);

            // 反序列化
            PlotListConfig config = JsonHelper.Deserialize<PlotListConfig>(json);
            if (config == null)
            {
                Console.WriteLine("反序列化失败");
                return null;
            }

            return config;
        }
    }
}
