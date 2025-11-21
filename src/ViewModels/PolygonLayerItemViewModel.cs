using GeoChemistryNexus.Models;
using GeoChemistryNexus.Helpers;
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
        }

        public void Render(Plot plot)
        {
            // 数据校验
            if (PolygonDefinition?.Vertices == null || !PolygonDefinition.Vertices.Any()) return;

            // 转换坐标点 (从 ObservableCollection<PointDefinition> 转为 Coordinates 数组)
            var coordinates = PolygonDefinition.Vertices
                .Select(p => new Coordinates(p.X, p.Y))
                .ToArray();

            // 添加多边形到 Plot
            var polygonPlot = plot.Add.Polygon(coordinates);

            // --- 应用样式 ---

            // 填充颜色
            polygonPlot.FillStyle.Color = ScottPlot.Color.FromHex(
                GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(PolygonDefinition.FillColor));

            // 边框颜色
            polygonPlot.LineStyle.Color = ScottPlot.Color.FromHex(
                GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(PolygonDefinition.BorderColor));

            // 边框宽度
            polygonPlot.LineStyle.Width = PolygonDefinition.BorderWidth;

            // 赋值给基类的 Plottable 属性
            this.Plottable = polygonPlot;
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
                polygonPlot.FillStyle.Color = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(PolygonDefinition.FillColor));
                polygonPlot.LineStyle.Color = ScottPlot.Color.FromHex(GraphMapTemplateParser.ConvertWpfHexToScottPlotHex(PolygonDefinition.BorderColor));
                polygonPlot.LineStyle.Width = PolygonDefinition.BorderWidth;
            }
        }
    }
}
