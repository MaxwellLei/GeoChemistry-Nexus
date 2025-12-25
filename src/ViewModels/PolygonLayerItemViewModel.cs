using GeoChemistryNexus.Models;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Interfaces;
using ScottPlot;
using System.Linq;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// 代表单个多边形元素的图层项
    /// </summary>
    public partial class PolygonLayerItemViewModel : LayerItemViewModel, IPlotLayer
    {
        public PolygonDefinition PolygonDefinition { get; }

        public PolygonLayerItemViewModel(PolygonDefinition polygonDefinition, int index)
            : base(LanguageService.Instance["polygon"] + $" {index + 1}")
        {
            PolygonDefinition = polygonDefinition;
            // 监听 Model 变化
            PolygonDefinition.PropertyChanged += (s, e) => OnRefreshRequired();
        }

        public void Render(Plot plot)
        {
            // 数据校验
            if (PolygonDefinition?.Vertices == null || !PolygonDefinition.Vertices.Any()) return;

            // 转换坐标点 (从 ObservableCollection<PointDefinition> 转为 Coordinates 数组)
            var plotCoordinates = PolygonDefinition.Vertices
                    .Select(v => PlotTransformHelper.ToRenderCoordinates(plot, v.X, v.Y))
                    .ToArray();

            // 添加多边形到 Plot
            var polygonPlot = plot.Add.Polygon(plotCoordinates);

            // --- 应用样式 ---

            // 填充颜色
            polygonPlot.FillStyle.Color = ScottPlot.Color.FromHex(
                GraphMapTemplateService.ConvertWpfHexToScottPlotHex(PolygonDefinition.FillColor));

            // 边框颜色
            polygonPlot.LineStyle.Color = ScottPlot.Color.FromHex(
                GraphMapTemplateService.ConvertWpfHexToScottPlotHex(PolygonDefinition.BorderColor));

            // 边框宽度
            polygonPlot.LineStyle.Width = PolygonDefinition.BorderWidth;

            // 赋值给基类的 Plottable 属性
            this.Plottable = polygonPlot;

            // --- 绘制高亮顶点 ---
            foreach (var vertex in PolygonDefinition.Vertices)
            {
                if (vertex.IsHighlighted)
                {
                    var p = PlotTransformHelper.ToRenderCoordinates(plot, vertex.X, vertex.Y);
                    var marker = plot.Add.Marker(p.X, p.Y);
                    marker.Color = ScottPlot.Colors.Red;
                    marker.Size = 10;
                    marker.Shape = MarkerShape.OpenCircle;
                    marker.LineWidth = 2;
                }
            }
        }

        public void Highlight()
        {
            if (Plottable is ScottPlot.Plottables.Polygon polygonPlot)
            {
                polygonPlot.LineStyle.Color = ScottPlot.Colors.Red;
                polygonPlot.LineStyle.Width = PolygonDefinition.BorderWidth + 2;
            }
        }

        public void Dim()
        {
            if (Plottable is ScottPlot.Plottables.Polygon polygonPlot)
            {
                byte dimAlpha = 60;
                polygonPlot.FillStyle.Color = polygonPlot.FillStyle.Color.WithAlpha(dimAlpha);
                polygonPlot.LineStyle.Color = polygonPlot.LineStyle.Color.WithAlpha(dimAlpha);
            }
        }

        public void Restore()
        {
            if (Plottable is ScottPlot.Plottables.Polygon polygonPlot)
            {
                polygonPlot.FillStyle.Color = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(PolygonDefinition.FillColor));
                polygonPlot.LineStyle.Color = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(PolygonDefinition.BorderColor));
                polygonPlot.LineStyle.Width = PolygonDefinition.BorderWidth;
            }
        }
    }
}
