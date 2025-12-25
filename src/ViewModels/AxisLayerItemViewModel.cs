using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Interfaces;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Extensions.ScottPlotExtensions;
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

                // 使用自定义坐标轴以支持子标题
                if (cartesianAxisDef.Type == "Left" && targetAxis is not LeftAxisWithSubtitle)
                {
                    plot.Axes.Remove(targetAxis);
                    var newAxis = new LeftAxisWithSubtitle();
                    plot.Axes.AddLeftAxis(newAxis);
                    targetAxis = newAxis;
                }
                else if (cartesianAxisDef.Type == "Right" && targetAxis is not RightAxisWithSubtitle)
                {
                    plot.Axes.Remove(targetAxis);
                    var newAxis = new RightAxisWithSubtitle();
                    plot.Axes.AddRightAxis(newAxis);
                    targetAxis = newAxis;
                }
                else if (cartesianAxisDef.Type == "Bottom" && targetAxis is not BottomAxisWithSubtitle)
                {
                    plot.Axes.Remove(targetAxis);
                    var newAxis = new BottomAxisWithSubtitle();
                    plot.Axes.AddBottomAxis(newAxis);
                    targetAxis = newAxis;
                }
                else if (cartesianAxisDef.Type == "Top" && targetAxis is not TopAxisWithSubtitle)
                {
                    plot.Axes.Remove(targetAxis);
                    var newAxis = new TopAxisWithSubtitle();
                    plot.Axes.AddTopAxis(newAxis);
                    targetAxis = newAxis;
                }

                var viewLimits = plot.Axes.GetLimits();

                // --- 应用样式 ---
                targetAxis.IsVisible = IsVisible; // 使用 ViewModel 自身的 IsVisible

                // 标题样式
                targetAxis.Label.Text = cartesianAxisDef.Label.Get();
                targetAxis.Label.FontName = Fonts.Detect(targetAxis.Label.Text);
                targetAxis.Label.FontSize = cartesianAxisDef.Size;
                targetAxis.Label.ForeColor = ScottPlot.Color.FromHex(
                    GraphMapTemplateService.ConvertWpfHexToScottPlotHex(cartesianAxisDef.Color));
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

                    if (cartesianAxisDef.MajorTickInterval > 0)
                    {
                        targetAxis.TickGenerator = new FixedIntervalTickGenerator(
                            cartesianAxisDef.MajorTickInterval, 
                            minorCount);
                    }
                    else
                    {
                        var tickGen = new ScottPlot.TickGenerators.NumericAutomatic();
                        // 自动刻度生成器下，ScottPlot 的 EvenlySpacedMinorTickGenerator 参数是“分段数”而不是“刻度数”
                        // 这里需要 +1
                        int divisions = minorCount > 0 ? minorCount + 1 : 0;
                        tickGen.MinorTickGenerator = new ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator(divisions);
                        targetAxis.TickGenerator = tickGen;
                    }
                }

                // 主刻度样式
                targetAxis.MajorTickStyle.Length = cartesianAxisDef.MajorTickLength;
                targetAxis.MajorTickStyle.Width = cartesianAxisDef.MajorTickWidth;
                targetAxis.MajorTickStyle.Color = ScottPlot.Color.FromHex(
                    GraphMapTemplateService.ConvertWpfHexToScottPlotHex(cartesianAxisDef.MajorTickWidthColor));
                targetAxis.MajorTickStyle.AntiAlias = cartesianAxisDef.MajorTickAntiAlias;

                // 次刻度样式
                targetAxis.MinorTickStyle.Length = cartesianAxisDef.MinorTickLength;
                targetAxis.MinorTickStyle.Width = cartesianAxisDef.MinorTickWidth;
                targetAxis.MinorTickStyle.Color = ScottPlot.Color.FromHex(
                    GraphMapTemplateService.ConvertWpfHexToScottPlotHex(cartesianAxisDef.MinorTickColor));
                targetAxis.MinorTickStyle.AntiAlias = cartesianAxisDef.MinorTickAntiAlias;

                // 刻度标签样式
                targetAxis.TickLabelStyle.FontName = cartesianAxisDef.TickLableFamily;
                targetAxis.TickLabelStyle.FontSize = cartesianAxisDef.TickLablesize;
                targetAxis.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex(
                    GraphMapTemplateService.ConvertWpfHexToScottPlotHex(cartesianAxisDef.TickLablecolor));
                targetAxis.TickLabelStyle.Bold = cartesianAxisDef.TickLableisBold;
                targetAxis.TickLabelStyle.Italic = cartesianAxisDef.TickLableisItalic;

                // 子标题样式
                if (targetAxis is LeftAxisWithSubtitle leftSub)
                {
                    ConfigureSubtitle(leftSub.SubLabelStyle, cartesianAxisDef);
                    leftSub.SubLabelText = cartesianAxisDef.SubLabel.Get();
                }
                else if (targetAxis is RightAxisWithSubtitle rightSub)
                {
                    ConfigureSubtitle(rightSub.SubLabelStyle, cartesianAxisDef);
                    rightSub.SubLabelText = cartesianAxisDef.SubLabel.Get();
                }
                else if (targetAxis is BottomAxisWithSubtitle bottomSub)
                {
                    ConfigureSubtitle(bottomSub.SubLabelStyle, cartesianAxisDef);
                    bottomSub.SubLabelText = cartesianAxisDef.SubLabel.Get();
                }
                else if (targetAxis is TopAxisWithSubtitle topSub)
                {
                    ConfigureSubtitle(topSub.SubLabelStyle, cartesianAxisDef);
                    topSub.SubLabelText = cartesianAxisDef.SubLabel.Get();
                }
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
                        GraphMapTemplateService.ConvertWpfHexToScottPlotHex(ternaryAxisDef.Color));
                    targetEdge.LabelStyle.Bold = ternaryAxisDef.IsBold;
                    targetEdge.LabelStyle.Italic = ternaryAxisDef.IsItalic;
                    targetEdge.LabelStyle.OffsetX = (float)ternaryAxisDef.LabelOffsetX;
                    targetEdge.LabelStyle.OffsetY = (float)ternaryAxisDef.LabelOffsetY;

                    // 刻度样式
                    targetEdge.TickMarkStyle.Width = ternaryAxisDef.MajorTickWidth;
                    targetEdge.TickMarkStyle.Color = ScottPlot.Color.FromHex(
                        GraphMapTemplateService.ConvertWpfHexToScottPlotHex(ternaryAxisDef.MajorTickWidthColor));

                    // 刻度标签样式
                    targetEdge.TickLabelStyle.FontName = ternaryAxisDef.TickLableFamily;
                    targetEdge.TickLabelStyle.FontSize = ternaryAxisDef.TickLablesize;
                    targetEdge.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex(
                        GraphMapTemplateService.ConvertWpfHexToScottPlotHex(ternaryAxisDef.TickLablecolor));
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

        private void ConfigureSubtitle(ScottPlot.LabelStyle style, CartesianAxisDefinition def)
        {
            style.FontName = Fonts.Detect(def.SubLabel.Get());
            style.FontSize = def.SubLabelSize;
            style.ForeColor = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(def.SubLabelColor));
            style.Bold = def.SubLabelBold;
            style.Italic = def.SubLabelItalic;
        }
    }
}
