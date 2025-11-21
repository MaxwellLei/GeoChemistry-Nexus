using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Interfaces;
using GeoChemistryNexus.Models;
using ScottPlot;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// 代表单个线元素的图层项
    /// </summary>
    public partial class LineLayerItemViewModel : LayerItemViewModel, IPlotLayer
    {
        public LineDefinition LineDefinition { get; }

        public LineLayerItemViewModel(LineDefinition lineDefinition, int index)
            : base(LanguageService.Instance["line"] + $" {index + 1}")
        {
            LineDefinition = lineDefinition;
        }

        // 渲染自己
        public void Render(Plot plot)
        {
            // 数据校验：如果起点或终点为空，则不绘制
            if (LineDefinition?.Start == null || LineDefinition?.End == null) return;

            // 在传入的 plot 对象上添加线条
            var linePlot = plot.Add.Line(
                LineDefinition.Start.X,
                LineDefinition.Start.Y,
                LineDefinition.End.X,
                LineDefinition.End.Y
            );

            // --- 应用样式 ---

            // 设置线宽
            linePlot.LineWidth = LineDefinition.Width;

            // 设置颜色 (使用辅助类转换 Hex 字符串)
            linePlot.Color = ScottPlot.Color.FromHex(
                GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(LineDefinition.Color)
            );

            // 设置线型 (实线、虚线等)
            linePlot.LinePattern = GraphMapTemplateParser.GetLinePattern(
                LineDefinition.Style.ToString()
            );

            // 将生成的 ScottPlot 对象赋值给基类的 Plottable 属性
            this.Plottable = linePlot;
        }

        public void Highlight()
        {
            if (Plottable is ScottPlot.Plottables.LinePlot linePlot)
            {
                linePlot.Color = ScottPlot.Colors.Red;
                linePlot.LineWidth = LineDefinition.Width + 2; // 宽度增加
            }
        }

        public void Dim()
        {
            if (Plottable is ScottPlot.Plottables.LinePlot linePlot)
            {
                linePlot.Color = linePlot.Color.WithAlpha(60); // 变暗
            }
        }

        public void Restore()
        {
            if (Plottable is ScottPlot.Plottables.LinePlot linePlot)
            {
                // 恢复原始定义中的属性
                linePlot.LineWidth = LineDefinition.Width;
                linePlot.Color = ScottPlot.Color.FromHex(
                    GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(LineDefinition.Color));
            }
        }
    }
}
