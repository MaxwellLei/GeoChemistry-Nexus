using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Interfaces;
using GeoChemistryNexus.Models;
using ScottPlot;
using System.Collections.Generic;
using System.Linq;

namespace GeoChemistryNexus.ViewModels
{
    public partial class ScatterLayerItemViewModel : LayerItemViewModel, IPlotLayer
    {
        public ScatterDefinition ScatterDefinition { get; }

        // 用来存储实际的数据点列表（无论是笛卡尔还是三元转换后的）
        public List<Coordinates> DataPoints { get; set; } = new List<Coordinates>();

        // 存储原始数据行号
        public List<int> OriginalRowIndices { get; set; } = new List<int>();

        public ScatterLayerItemViewModel(ScatterDefinition scatterDefinition)
            : base(LanguageService.Instance["data_point"])
        {
            ScatterDefinition = scatterDefinition;
            // 监听 Model 变化
            ScatterDefinition.PropertyChanged += (s, e) => OnRefreshRequired();
            if (ScatterDefinition.StartAndEnd != null) ScatterDefinition.StartAndEnd.PropertyChanged += (s, e) => OnRefreshRequired();
            
            ScatterDefinition.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(ScatterDefinition.StartAndEnd) && ScatterDefinition.StartAndEnd != null)
                    ScatterDefinition.StartAndEnd.PropertyChanged += (sender, args) => OnRefreshRequired();
            };
        }

        public void Render(Plot plot)
        {
            // 如果没有数据点，则不绘制
            if (DataPoints == null || !DataPoints.Any()) return;

            // --- 绘图逻辑 ---
            // 将“真实数据”转换为“渲染坐标”
            var renderPoints = DataPoints
                            .Select(p => PlotTransformHelper.ToRenderCoordinates(plot, p))
                            .ToArray();

            var scatterPlot = plot.Add.ScatterPoints(renderPoints);

            // 设置图例
            scatterPlot.LegendText = this.Name;

            // --- 应用样式 ---
            scatterPlot.Color = ScottPlot.Color.FromHex(
                GraphMapTemplateService.ConvertWpfHexToScottPlotHex(ScatterDefinition.Color));

            scatterPlot.MarkerSize = ScatterDefinition.Size;
            scatterPlot.MarkerShape = ScatterDefinition.MarkerShape;

            // 赋值给基类
            this.Plottable = scatterPlot;
        }

        public void Highlight()
        {
            if (Plottable is ScottPlot.Plottables.Scatter scatterPlot)
            {
                scatterPlot.MarkerStyle.OutlineColor = ScottPlot.Colors.Red;
                scatterPlot.MarkerStyle.OutlineWidth = 2;
            }
        }

        public void Dim()
        {
            if (Plottable is ScottPlot.Plottables.Scatter scatterPlot)
            {
                scatterPlot.Color = scatterPlot.Color.WithAlpha(60);
            }
        }

        public void Restore()
        {
            if (Plottable is ScottPlot.Plottables.Scatter scatterPlot)
            {
                scatterPlot.Color = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(ScatterDefinition.Color));
                // 恢复时去掉高亮轮廓
                scatterPlot.MarkerStyle.OutlineWidth = 0;
            }
        }
    }
}
