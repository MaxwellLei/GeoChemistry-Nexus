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

            // 判断当前是否为三元相图
            var triangularAxis = plot.GetPlottables().OfType<TriangularAxis>().FirstOrDefault();

            if (triangularAxis != null)
            {
                // 三元图绘制逻辑
                RenderTernaryArrow(plot, triangularAxis);
            }
            else
            {
                // 普通直角坐标系逻辑
                RenderCartesianArrow(plot);
            }
        }

        /// <summary>
        /// 绘制普通直角坐标系箭头
        /// </summary>
        private void RenderCartesianArrow(Plot plot)
        {
            var startPixel = PlotTransformHelper.ToRenderCoordinates(plot, ArrowDefinition.Start.X, ArrowDefinition.Start.Y);
            var endPixel = PlotTransformHelper.ToRenderCoordinates(plot, ArrowDefinition.End.X, ArrowDefinition.End.Y);

            var arrowPlot = plot.Add.Arrow(startPixel, endPixel);

            ApplyCommonStyle(arrowPlot);

            // 绑定引用
            this.Plottable = arrowPlot;

            // --- 绘制高亮顶点 ---
            // 绘制起点高亮圆圈
            if (ArrowDefinition.Start.IsHighlighted)
            {
                var marker = plot.Add.Marker(startPixel.X, startPixel.Y);
                marker.Color = ScottPlot.Colors.Red;
                marker.Size = 10;
                marker.Shape = MarkerShape.OpenCircle;
                marker.LineWidth = 2;
            }
            // 绘制终点高亮圆圈
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
        /// 绘制三元相图箭头
        /// </summary>
        private void RenderTernaryArrow(Plot plot, TriangularAxis triangularAxis)
        {
            // 从存储的数据中获取三元分量
            double startBottom = ArrowDefinition.Start.X;
            double startLeft = ArrowDefinition.Start.Y;
            double startRight = 1 - startBottom - startLeft; // 计算第三个分量

            double endBottom = ArrowDefinition.End.X;
            double endLeft = ArrowDefinition.End.Y;
            double endRight = 1 - endBottom - endLeft;

            // 使用三角轴工具将三元坐标转换为屏幕上的笛卡尔坐标
            Coordinates startCartesian = triangularAxis.GetCoordinates(startBottom, startLeft, startRight);
            Coordinates endCartesian = triangularAxis.GetCoordinates(endBottom, endLeft, endRight);

            // 添加箭头
            var arrowPlot = plot.Add.Arrow(startCartesian, endCartesian);

            ApplyCommonStyle(arrowPlot);

            // 绑定引用
            this.Plottable = arrowPlot;

            // --- 绘制高亮顶点 ---
            if (ArrowDefinition.Start.IsHighlighted)
            {
                var marker = plot.Add.Marker(startCartesian.X, startCartesian.Y);
                marker.Color = ScottPlot.Colors.Red;
                marker.Size = 10;
                marker.Shape = MarkerShape.OpenCircle;
                marker.LineWidth = 2;
            }
            if (ArrowDefinition.End.IsHighlighted)
            {
                var marker = plot.Add.Marker(endCartesian.X, endCartesian.Y);
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
