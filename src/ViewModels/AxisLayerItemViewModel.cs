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

                // 对数刻度与常规刻度处理
                if (cartesianAxisDef.ScaleType == AxisScaleType.Logarithmic)
                {
                    var minorTickGen = new ScottPlot.TickGenerators.LogMinorTickGenerator();
                    var tickGen = new ScottPlot.TickGenerators.NumericAutomatic();
                    tickGen.MinorTickGenerator = minorTickGen;
                    tickGen.IntegerTicksOnly = true;
                    tickGen.LabelFormatter = y => $"{Math.Pow(10, y)}"; // 简单的对数格式化
                    targetAxis.TickGenerator = tickGen;
                }
                else
                {
                    // 强制恢复默认刻度生成器
                    targetAxis.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic();
                }

                // 应用轴范围限制 (逻辑封装在私有方法中)
                UpdateAxisLimits(plot, cartesianAxisDef, targetAxis);

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

                // 对于坐标轴，我们不设置 this.Plottable，因为 Axis 不是 IPlottable
                // this.Plottable = null; 
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
