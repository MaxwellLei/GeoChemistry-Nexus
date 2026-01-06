using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Interfaces;
using GeoChemistryNexus.Models;
using ScottPlot.Plottables;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels
{
    public class ArrowLayerItemViewModel : LayerItemViewModel, IPlotLayer
    {
        public ArrowDefinition ArrowDefinition { get; }

        public ArrowLayerItemViewModel(ArrowDefinition arrowDefinition, int index)
            : base(LanguageService.Instance["arrow"] + $" {index + 1}")
        {
            ArrowDefinition = arrowDefinition;
            // 监听 Model 变化
            ArrowDefinition.PropertyChanged += (s, e) => OnRefreshRequired();
            // 监听子对象
            if (ArrowDefinition.Start != null) ArrowDefinition.Start.PropertyChanged += (s, e) => OnRefreshRequired();
            if (ArrowDefinition.End != null) ArrowDefinition.End.PropertyChanged += (s, e) => OnRefreshRequired();

            // 监听 Start/End 对象本身的替换
            ArrowDefinition.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(ArrowDefinition.Start) && ArrowDefinition.Start != null)
                    ArrowDefinition.Start.PropertyChanged += (sender, args) => OnRefreshRequired();
                if (e.PropertyName == nameof(ArrowDefinition.End) && ArrowDefinition.End != null)
                    ArrowDefinition.End.PropertyChanged += (sender, args) => OnRefreshRequired();
            };
        }

        public void Render(Plot plot)
        {
            // 基础校验
            if (ArrowDefinition?.Start == null || ArrowDefinition?.End == null) return;

            // 内部存储的X/Y已经是笛卡尔坐标，直接使用PlotTransformHelper转换（处理对数轴）
            var startPixel = PlotTransformHelper.ToRenderCoordinates(plot, ArrowDefinition.Start.X, ArrowDefinition.Start.Y);
            var endPixel = PlotTransformHelper.ToRenderCoordinates(plot, ArrowDefinition.End.X, ArrowDefinition.End.Y);

            var arrowPlot = plot.Add.Arrow(startPixel, endPixel);

            ApplyCommonStyle(arrowPlot);

            // 绑定引用
            this.Plottable = arrowPlot;

            // --- 绘制高亮顶点 ---
            if (ArrowDefinition.Start.IsHighlighted)
            {
                var marker = plot.Add.Marker(startPixel.X, startPixel.Y);
                marker.Color = ScottPlot.Colors.Red;
                marker.Size = 10;
                marker.Shape = MarkerShape.OpenCircle;
                marker.LineWidth = 2;
            }
            if (ArrowDefinition.End.IsHighlighted)
            {
                var marker = plot.Add.Marker(endPixel.X, endPixel.Y);
                marker.Color = ScottPlot.Colors.Red;
                marker.Size = 10;
                marker.Shape = MarkerShape.OpenCircle;
                marker.LineWidth = 2;
            }
        }

        /// <summary>
        /// 应用通用的样式 (颜色、宽度等)
        /// </summary>
        private void ApplyCommonStyle(ScottPlot.Plottables.Arrow arrowPlot)
        {
            arrowPlot.ArrowFillColor = ScottPlot.Color.FromHex(
                GraphMapTemplateService.ConvertWpfHexToScottPlotHex(ArrowDefinition.Color));

            arrowPlot.ArrowWidth = ArrowDefinition.ArrowWidth;
            arrowPlot.ArrowheadWidth = ArrowDefinition.ArrowheadWidth;
            arrowPlot.ArrowheadLength = ArrowDefinition.ArrowheadLength;

            // 隐藏描边
            arrowPlot.ArrowLineColor = Colors.Transparent;
        }

        public void Highlight()
        {
            if (Plottable is ScottPlot.Plottables.Arrow arrowPlot)
            {
                arrowPlot.ArrowFillColor = ScottPlot.Colors.Red;
                arrowPlot.ArrowWidth = ArrowDefinition.ArrowWidth + 2;
            }
        }

        public void Dim()
        {
            if (Plottable is ScottPlot.Plottables.Arrow arrowPlot)
            {
                arrowPlot.ArrowFillColor = arrowPlot.ArrowFillColor.WithAlpha(60);
            }
        }

        public void Restore()
        {
            if (Plottable is ScottPlot.Plottables.Arrow arrowPlot)
            {
                arrowPlot.ArrowFillColor = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(ArrowDefinition.Color));
                arrowPlot.ArrowWidth = ArrowDefinition.ArrowWidth;
                arrowPlot.ArrowheadWidth = ArrowDefinition.ArrowheadWidth;
                arrowPlot.ArrowheadLength = ArrowDefinition.ArrowheadLength;
            }
        }
    }
}
