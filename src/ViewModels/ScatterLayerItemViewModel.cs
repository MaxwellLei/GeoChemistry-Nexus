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

        private List<Coordinates> _dataPoints = new List<Coordinates>();
        // 用来存储实际的数据点列表（无论是笛卡尔还是三元转换后的）
        public List<Coordinates> DataPoints 
        {
            get => _dataPoints;
            set
            {
                if (_dataPoints != value)
                {
                    _dataPoints = value;
                    _cachedRenderPoints = null; // 数据源改变，缓存失效
                }
            }
        }

        // 存储原始数据行号
        public List<int> OriginalRowIndices { get; set; } = new List<int>();

        // --- 缓存字段 ---
        private Coordinates[]? _cachedRenderPoints;
        private bool _lastXIsLog;
        private bool _lastYIsLog;
        
        // --- 图例替身引用 ---
        private ScottPlot.Plottables.Scatter? _legendProxy;

        public ScatterLayerItemViewModel(ScatterDefinition scatterDefinition)
            : base(LanguageService.Instance["data_point"])
        {
            ScatterDefinition = scatterDefinition;
            // 监听 Model 变化
            ScatterDefinition.PropertyChanged += (s, e) => 
            {
                // 区分样式更新和全量刷新
                if (e.PropertyName == nameof(ScatterDefinition.Color) ||
                    e.PropertyName == nameof(ScatterDefinition.Size) ||
                    e.PropertyName == nameof(ScatterDefinition.MarkerShape) ||
                    e.PropertyName == nameof(ScatterDefinition.StrokeWidth) ||
                    e.PropertyName == nameof(ScatterDefinition.StrokeColor))
                {
                    OnStyleUpdateRequired();
                }
                else if (e.PropertyName == nameof(ScatterDefinition.StartAndEnd))
                {
                     OnRefreshRequired();
                }
                else
                {
                    OnRefreshRequired();
                }
            };

            if (ScatterDefinition.StartAndEnd != null) 
            {
                ScatterDefinition.StartAndEnd.PropertyChanged += (s, e) => OnRefreshRequired();
            }
            
            // 额外处理 StartAndEnd 属性被替换的情况
            ScatterDefinition.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(ScatterDefinition.StartAndEnd) && ScatterDefinition.StartAndEnd != null)
                {
                    ScatterDefinition.StartAndEnd.PropertyChanged -= (sender, args) => OnRefreshRequired(); // 移除旧的
                    ScatterDefinition.StartAndEnd.PropertyChanged += (sender, args) => OnRefreshRequired();
                }
            };
        }

        public void Render(Plot plot)
        {
            // 如果没有数据点，则不绘制
            if (DataPoints == null || !DataPoints.Any()) return;

            // --- 缓存逻辑 ---
            // 检查坐标轴状态
            bool currentXIsLog = PlotTransformHelper.IsLogAxis(plot.Axes.Bottom);
            bool currentYIsLog = PlotTransformHelper.IsLogAxis(plot.Axes.Left);

            // 如果缓存不存在，或者坐标轴状态发生了变化，则重新计算
            if (_cachedRenderPoints == null || currentXIsLog != _lastXIsLog || currentYIsLog != _lastYIsLog)
            {
                _cachedRenderPoints = DataPoints
                    .Select(p => PlotTransformHelper.ToRenderCoordinates(plot, p))
                    .ToArray();

                _lastXIsLog = currentXIsLog;
                _lastYIsLog = currentYIsLog;
            }

            // 使用缓存的数据点
            var scatterPlot = plot.Add.ScatterPoints(_cachedRenderPoints);

            // 设置图例 - 主绘图对象不显示图例，使用替身显示固定大小的图例
            scatterPlot.LegendText = string.Empty;

            // --- 创建图例替身 ---
            // 创建一个没有数据的散点图，仅用于显示图例
            _legendProxy = plot.Add.ScatterPoints(new Coordinates[] { });
            _legendProxy.LegendText = this.Name;
            
            // 应用初始样式
            UpdatePlottableStyle(scatterPlot, _legendProxy);

            // 赋值给基类
            this.Plottable = scatterPlot;
        }

        public override void UpdateStyle()
        {
            if (Plottable is ScottPlot.Plottables.Scatter scatterPlot && _legendProxy != null)
            {
                UpdatePlottableStyle(scatterPlot, _legendProxy);
            }
        }

        private void UpdatePlottableStyle(ScottPlot.Plottables.Scatter scatterPlot, ScottPlot.Plottables.Scatter legendProxy)
        {
            // --- 更新主对象样式 ---
            scatterPlot.Color = ScottPlot.Color.FromHex(
                GraphMapTemplateService.ConvertWpfHexToScottPlotHex(ScatterDefinition.Color));
            scatterPlot.MarkerSize = ScatterDefinition.Size;
            scatterPlot.MarkerShape = ScatterDefinition.MarkerShape;
            scatterPlot.MarkerStyle.OutlineWidth = ScatterDefinition.StrokeWidth;
            scatterPlot.MarkerStyle.OutlineColor = ScottPlot.Color.FromHex(
                GraphMapTemplateService.ConvertWpfHexToScottPlotHex(ScatterDefinition.StrokeColor));

            // --- 更新图例替身样式 ---
            legendProxy.Color = scatterPlot.Color;
            legendProxy.MarkerSize = 8; // 固定图例大小
            legendProxy.MarkerShape = ScatterDefinition.MarkerShape;
            legendProxy.MarkerStyle.OutlineWidth = ScatterDefinition.StrokeWidth;
            legendProxy.MarkerStyle.OutlineColor = scatterPlot.MarkerStyle.OutlineColor;
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
                
                // 恢复描边样式
                scatterPlot.MarkerStyle.OutlineWidth = ScatterDefinition.StrokeWidth;
                scatterPlot.MarkerStyle.OutlineColor = ScottPlot.Color.FromHex(
                    GraphMapTemplateService.ConvertWpfHexToScottPlotHex(ScatterDefinition.StrokeColor));
            }
        }
    }
}
