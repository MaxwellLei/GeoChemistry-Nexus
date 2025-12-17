using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Interfaces;
using GeoChemistryNexus.Models;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// 代表单个坐标轴的图层项
    /// </summary>
    public partial class AxisLayerItemViewModel : LayerItemViewModel, IPlotLayer
    {
        public BaseAxisDefinition AxisDefinition { get; }

        public AxisLayerItemViewModel(BaseAxisDefinition axisDefinition)
            : base(axisDefinition.Label.Get()) // 根据坐标轴类型设置名称
        {
            AxisDefinition = axisDefinition;
        }

        public void Render(Plot plot)
        {
            // 1. 处理笛卡尔坐标轴 (Cartesian)
            if (AxisDefinition is CartesianAxisDefinition cartesianAxisDef)
            {
                // 根据类型找到对应的轴对象
                ScottPlot.IAxis? targetAxis = cartesianAxisDef.Type switch
                {
                    "Left" => plot.Axes.Left,
                    "Right" => plot.Axes.Right,
                    "Bottom" => plot.Axes.Bottom,
                    "Top" => plot.Axes.Top,
                    _ => null
                };

                if (targetAxis == null) return;
                var viewLimits = plot.Axes.GetLimits();

                // --- 应用样式 ---
                targetAxis.IsVisible = IsVisible; // 使用 ViewModel 自身的 IsVisible

                // 标题样式
                targetAxis.Label.Text = cartesianAxisDef.Label.Get();
                targetAxis.Label.FontName = Fonts.Detect(targetAxis.Label.Text);
                targetAxis.Label.FontSize = cartesianAxisDef.Size;
                targetAxis.Label.ForeColor = ScottPlot.Color.FromHex(
                    GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(cartesianAxisDef.Color));
                targetAxis.Label.Bold = cartesianAxisDef.IsBold;
                targetAxis.Label.Italic = cartesianAxisDef.IsItalic;

                // 应用轴范围限制
                UpdateAxisLimits(plot, cartesianAxisDef, targetAxis);

                // 对数刻度与常规刻度处理
                if (cartesianAxisDef.ScaleType == AxisScaleType.Logarithmic)
                {
                    var tickGen = new ScottPlot.TickGenerators.NumericAutomatic();
                    var hideMinor = cartesianAxisDef.MinorTickWidth <= 0 || cartesianAxisDef.MinorTickLength <= 0;
                    tickGen.MinorTickGenerator = hideMinor
                        ? new ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator(0)
                        : new ScottPlot.TickGenerators.LogMinorTickGenerator();
                    tickGen.IntegerTicksOnly = true;
                    tickGen.LabelFormatter = y => $"{Math.Pow(10, y)}";
                    targetAxis.TickGenerator = tickGen;
                }
                else
                {
                    var minorCount = Math.Max(0, cartesianAxisDef.MinorTicksPerMajorTick);
                    if (cartesianAxisDef.MinorTickWidth <= 0 || cartesianAxisDef.MinorTickLength <= 0)
                        minorCount = 0;
                    else
                        minorCount += 1;

                    if (cartesianAxisDef.MajorTickInterval > 0)
                    {
                        double interval = cartesianAxisDef.MajorTickInterval;
                        // 使用更新后的轴范围
                        double min = targetAxis.Range.Min;
                        double max = targetAxis.Range.Max;

                        if (double.IsFinite(min) && double.IsFinite(max) && max > min)
                        {
                            var manual = new ScottPlot.TickGenerators.NumericManual();
                            // 确保从刻度网格对齐的位置开始
                            double start = Math.Floor(min / interval) * interval;
                            // 稍微扩展一点范围以确保覆盖边界，或者严格控制
                            // 这里保持原有的逻辑，但起始点计算稍微优化一下，用 Floor 确保包含下边界
                            
                            // 注意：原代码是 Math.Ceiling(min / interval) * interval
                            // 如果 min=0.5, interval=1, Ceiling -> 1. start=1. 0.5~1之间就没有刻度了。
                            // 如果 min=1.5, interval=1, Ceiling -> 2. start=2.
                            // 通常我们希望刻度包含 min，或者从 min 之后的第一个刻度开始。
                            // 让我们保持原有的 Ceiling 逻辑，或者根据用户习惯调整。
                            // 如果用户希望 0, 1, 2... 而范围是 0.5~9.5，那么第一个刻度应该是 1。Ceiling 是对的。
                            // 但如果范围是 0~10，min=0, Ceiling(0)=0. start=0. 正确。
                            
                            start = Math.Ceiling(min / interval) * interval;
                            
                            // 为了防止浮点误差导致少绘制最后一个刻度，可以给 max 加一个微小的 epsilon
                            for (double pos = start; pos <= max + 1e-10; pos += interval)
                            {
                                manual.AddMajor(pos, pos.ToString());
                                if (minorCount > 0)
                                {
                                    double step = interval / (minorCount + 1);
                                    for (int j = 1; j <= minorCount; j++)
                                    {
                                        double minorPos = pos + j * step;
                                        if (minorPos <= max + 1e-10) 
                                            manual.AddMinor(minorPos);
                                    }
                                }
                            }
                            targetAxis.TickGenerator = manual;
                        }
                    }
                    else
                    {
                        var tickGen = new ScottPlot.TickGenerators.NumericAutomatic();
                        tickGen.MinorTickGenerator = new ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator(minorCount);
                        targetAxis.TickGenerator = tickGen;
                    }
                }

                // 主刻度样式
                targetAxis.MajorTickStyle.Length = cartesianAxisDef.MajorTickLength;
                targetAxis.MajorTickStyle.Width = cartesianAxisDef.MajorTickWidth;
                targetAxis.MajorTickStyle.Color = ScottPlot.Color.FromHex(
                    GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(cartesianAxisDef.MajorTickWidthColor));
                targetAxis.MajorTickStyle.AntiAlias = cartesianAxisDef.MajorTickAntiAlias;

                // 次刻度样式
                targetAxis.MinorTickStyle.Length = cartesianAxisDef.MinorTickLength;
                targetAxis.MinorTickStyle.Width = cartesianAxisDef.MinorTickWidth;
                targetAxis.MinorTickStyle.Color = ScottPlot.Color.FromHex(
                    GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(cartesianAxisDef.MinorTickColor));
                targetAxis.MinorTickStyle.AntiAlias = cartesianAxisDef.MinorTickAntiAlias;

                // 刻度标签样式
                targetAxis.TickLabelStyle.FontName = cartesianAxisDef.TickLableFamily;
                targetAxis.TickLabelStyle.FontSize = cartesianAxisDef.TickLablesize;
                targetAxis.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex(
                    GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(cartesianAxisDef.TickLablecolor));
                targetAxis.TickLabelStyle.Bold = cartesianAxisDef.TickLableisBold;
                targetAxis.TickLabelStyle.Italic = cartesianAxisDef.TickLableisItalic;
            }

            // 2. 处理三元坐标轴 (Ternary)
            else if (AxisDefinition is TernaryAxisDefinition ternaryAxisDef)
            {
                // 获取图表中已存在的 TriangularAxis 对象
                var triangularAxis = plot.GetPlottables().OfType<ScottPlot.Plottables.TriangularAxis>().FirstOrDefault();
                if (triangularAxis == null) return;

                ScottPlot.TriangularAxisEdge? targetEdge = ternaryAxisDef.Type switch
                {
                    "Bottom" => triangularAxis.Bottom,
                    "Left" => triangularAxis.Left,
                    "Right" => triangularAxis.Right,
                    _ => null
                };

                if (targetEdge != null)
                {
                    // 标题样式
                    targetEdge.LabelText = ternaryAxisDef.Label.Get();
                    targetEdge.LabelStyle.FontName = Fonts.Detect(targetEdge.LabelText);
                    targetEdge.LabelStyle.FontSize = ternaryAxisDef.Size;
                    targetEdge.LabelStyle.ForeColor = ScottPlot.Color.FromHex(
                        GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(ternaryAxisDef.Color));
                    targetEdge.LabelStyle.Bold = ternaryAxisDef.IsBold;
                    targetEdge.LabelStyle.Italic = ternaryAxisDef.IsItalic;
                    targetEdge.LabelStyle.OffsetX = (float)ternaryAxisDef.LabelOffsetX;
                    targetEdge.LabelStyle.OffsetY = (float)ternaryAxisDef.LabelOffsetY;

                    // 刻度样式
                    targetEdge.TickMarkStyle.Width = ternaryAxisDef.MajorTickWidth;
                    targetEdge.TickMarkStyle.Color = ScottPlot.Color.FromHex(
                        GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(ternaryAxisDef.MajorTickWidthColor));

                    // 刻度标签样式
                    targetEdge.TickLabelStyle.FontName = ternaryAxisDef.TickLableFamily;
                    targetEdge.TickLabelStyle.FontSize = ternaryAxisDef.TickLablesize;
                    targetEdge.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex(
                        GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(ternaryAxisDef.TickLablecolor));
                    targetEdge.TickLabelStyle.Bold = ternaryAxisDef.TickLableisBold;
                    targetEdge.TickLabelStyle.Italic = ternaryAxisDef.TickLableisItalic;

                    // 修改刻度标签为 0-1
                    targetEdge.Ticks.Clear();
                    int ticksPerEdge = 10;
                    for (int i = 0; i <= ticksPerEdge; i++)
                    {
                        double fraction = i / (double)ticksPerEdge;
                        double tickX = targetEdge.Start.X + fraction * (targetEdge.End.X - targetEdge.Start.X);
                        double tickY = targetEdge.Start.Y + fraction * (targetEdge.End.Y - targetEdge.Start.Y);
                        ScottPlot.Coordinates tickPoint = new(tickX, tickY);
                        string tickLabel = $"{fraction:0.#}";
                        targetEdge.Ticks.Add((tickPoint, tickLabel));
                    }
                }
            }
        }

        /// <summary>
        /// 私有辅助方法：更新坐标轴范围
        /// </summary>
        private void UpdateAxisLimits(Plot plot, CartesianAxisDefinition axisDef, IAxis targetAxis)
        {
            // 如果最大值最小值都是 NaN，则自动缩放
            if (double.IsNaN(axisDef.Minimum) && double.IsNaN(axisDef.Maximum))
            {
                // 注意：这里可能需要在全部渲染完成后再调用 AutoScale，
                // 但单独设置 Axis Range 为 Auto 也是一种方式，ScottPlot 默认就是 Auto
                // 这里我们只处理设定了具体数值的情况
                return;
            }

            var currentLimits = targetAxis.Range;
            var newMin = !double.IsNaN(axisDef.Minimum) ? axisDef.Minimum : currentLimits.Min;
            var newMax = !double.IsNaN(axisDef.Maximum) ? axisDef.Maximum : currentLimits.Max;

            // 对数处理
            if (axisDef.ScaleType == AxisScaleType.Logarithmic)
            {
                if (!double.IsNaN(axisDef.Minimum)) newMin = axisDef.Minimum > 0 ? Math.Log10(axisDef.Minimum) : currentLimits.Min;
                if (!double.IsNaN(axisDef.Maximum)) newMax = axisDef.Maximum > 0 ? Math.Log10(axisDef.Maximum) : currentLimits.Max;
            }

            targetAxis.Range.Set(newMin, newMax);
        }

        public void Highlight() { /* 坐标轴不参与高亮 */ }
        public void Dim() { /* 坐标轴不参与遮罩 */ }
        public void Restore() { }
    }
}
