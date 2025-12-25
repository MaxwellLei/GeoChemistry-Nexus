using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Interfaces;
using GeoChemistryNexus.Models;
using ScottPlot;
using System;

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
            // 监听 Model 变化
            LineDefinition.PropertyChanged += (s, e) => OnRefreshRequired();
            // 监听子对象
            if (LineDefinition.Start != null) LineDefinition.Start.PropertyChanged += (s, e) => OnRefreshRequired();
            if (LineDefinition.End != null) LineDefinition.End.PropertyChanged += (s, e) => OnRefreshRequired();
            
            // 监听 Start/End 对象本身的替换
            LineDefinition.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(LineDefinition.Start) && LineDefinition.Start != null)
                    LineDefinition.Start.PropertyChanged += (sender, args) => OnRefreshRequired();
                if (e.PropertyName == nameof(LineDefinition.End) && LineDefinition.End != null)
                    LineDefinition.End.PropertyChanged += (sender, args) => OnRefreshRequired();
            };
        }

        // 渲染自己
        public void Render(Plot plot)
        {
            // 数据校验：如果起点或终点为空，则不绘制
            if (LineDefinition?.Start == null || LineDefinition?.End == null) return;

            // 如果当前是对数轴，会自动取 Log10
            var startNode = PlotTransformHelper.ToRenderCoordinates(
                plot,
                LineDefinition.Start.X,
                LineDefinition.Start.Y
            );

            var endNode = PlotTransformHelper.ToRenderCoordinates(
                plot,
                LineDefinition.End.X,
                LineDefinition.End.Y
            );

            // 在传入的 plot 对象上添加线条 (使用转换后的坐标)
            var linePlot = plot.Add.Line(
                startNode.X,
                startNode.Y,
                endNode.X,
                endNode.Y
            );

            // --- 应用样式 ---

            // 设置线宽
            linePlot.LineWidth = LineDefinition.Width;

            // 设置颜色 (使用辅助类转换 Hex 字符串)
            linePlot.Color = ScottPlot.Color.FromHex(
                GraphMapTemplateService.ConvertWpfHexToScottPlotHex(LineDefinition.Color)
            );

            // 设置线型 (实线、虚线等)
            linePlot.LinePattern = GraphMapTemplateService.GetLinePattern(
                LineDefinition.Style.ToString()
            );

            // 将生成的 ScottPlot 对象赋值给基类的 Plottable 属性
            this.Plottable = linePlot;

            // --- 绘制高亮顶点 ---
            if (LineDefinition.Start.IsHighlighted)
            {
                var marker = plot.Add.Marker(startNode.X, startNode.Y);
                marker.Color = ScottPlot.Colors.Red;
                marker.Size = 10;
                marker.Shape = MarkerShape.OpenCircle;
                marker.LineWidth = 2;
            }
            if (LineDefinition.End.IsHighlighted)
            {
                var marker = plot.Add.Marker(endNode.X, endNode.Y);
                marker.Color = ScottPlot.Colors.Red;
                marker.Size = 10;
                marker.Shape = MarkerShape.OpenCircle;
                marker.LineWidth = 2;
            }
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
                    GraphMapTemplateService.ConvertWpfHexToScottPlotHex(LineDefinition.Color));
            }
        }
    }
}
