using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Helpers.PlotMarkers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Interfaces;
using GeoChemistryNexus.Models;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoChemistryNexus.ViewModels
{
    public partial class ScatterLayerItemViewModel : LayerItemViewModel, IPlotLayer
    {
        public event Action<ScatterLayerItemViewModel, string>? ScatterNameChanged;

        public override void ClearEventSubscriptions()
        {
            ScatterNameChanged = null;
            base.ClearEventSubscriptions();
        }

        public ScatterDefinition ScatterDefinition { get; }
        public override bool ShowInlineDeleteButton => true;

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
            : base(string.IsNullOrWhiteSpace(scatterDefinition?.Name)
                ? LanguageService.Instance["data_point"]
                : scatterDefinition!.Name)
        {
            ScatterDefinition = scatterDefinition ?? throw new ArgumentNullException(nameof(scatterDefinition));
            PropertyChanged += ScatterLayerItemViewModel_PropertyChanged;

            // 监听 Model 变化
            ScatterDefinition.PropertyChanged += (s, e) => 
            {
                if (e.PropertyName == nameof(ScatterDefinition.Name))
                {
                    var scatterName = ScatterDefinition.Name ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(scatterName))
                    {
                        // 名称不允许为空：回退到图层当前名称，避免触发全量重绘
                        var fallbackName = string.IsNullOrWhiteSpace(Name)
                            ? LanguageService.Instance["data_point"]
                            : Name;
                        if (ScatterDefinition.Name != fallbackName)
                            ScatterDefinition.Name = fallbackName;
                        MessageHelper.Warning(LanguageService.Instance["geo_validate_name_required"]);
                        return;
                    }

                    Name = scatterName;
                    UpdateLegendText(scatterName);
                    ScatterNameChanged?.Invoke(this, scatterName);
                    // 仅刷新图例显示，保留当前视角（投点越界后的 AutoScale 视图）
                    OnStyleUpdateRequired();
                    return;
                }

                // 区分样式更新和全量刷新
                if (e.PropertyName == nameof(ScatterDefinition.Color) ||
                    e.PropertyName == nameof(ScatterDefinition.Size) ||
                    e.PropertyName == nameof(ScatterDefinition.MarkerShape) ||
                    e.PropertyName == nameof(ScatterDefinition.StrokeWidth) ||
                    e.PropertyName == nameof(ScatterDefinition.StrokeColor) ||
                    e.PropertyName == nameof(ScatterDefinition.HasFill))
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

            if (!string.IsNullOrWhiteSpace(ScatterDefinition.Name))
            {
                Name = ScatterDefinition.Name;
            }
            else
            {
                ScatterDefinition.Name = Name;
            }
        }

        internal void RegisterPlottablesForLookup(Dictionary<ScottPlot.IPlottable, LayerItemViewModel> lookup)
        {
            if (Plottable != null)
            {
                lookup[Plottable] = this;
            }

            if (_legendProxy != null)
            {
                lookup[_legendProxy] = this;
            }
        }

        /// <summary>
        /// 从 Plot 卸下本图层的主系列与图例替身（不修改 DataPoints）。
        /// </summary>
        public void DetachPlottablesFromPlot(ScottPlot.Plot plot)
        {
            if (plot == null)
            {
                return;
            }

            if (Plottable != null)
            {
                plot.Remove(Plottable);
                Plottable = null;
            }

            if (_legendProxy != null)
            {
                plot.Remove(_legendProxy);
                _legendProxy = null;
            }

            _cachedRenderPoints = null;
        }

        public void UnregisterPlottablesFromLookup(Dictionary<ScottPlot.IPlottable, LayerItemViewModel> lookup)
        {
            if (lookup == null)
            {
                return;
            }

            if (Plottable != null)
            {
                lookup.Remove(Plottable);
            }

            if (_legendProxy != null)
            {
                lookup.Remove(_legendProxy);
            }
        }

        /// <summary>
        /// 按当前 DataPoints 重新渲染到 Plot（先 Detach 再调用）。
        /// </summary>
        public void AttachPlottablesToPlot(ScottPlot.Plot plot)
        {
            if (plot == null)
            {
                return;
            }

            Render(plot);
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
            UpdateLegendText(Name);
            
            // 应用初始样式
            UpdatePlottableStyle(scatterPlot, _legendProxy);

            // 赋值给基类
            this.Plottable = scatterPlot;
        }

        private void ScatterLayerItemViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Name))
            {
                return;
            }

            var scatterName = Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(scatterName))
            {
                var fallbackName = string.IsNullOrWhiteSpace(ScatterDefinition.Name)
                    ? LanguageService.Instance["data_point"]
                    : ScatterDefinition.Name;
                if (Name != fallbackName)
                    Name = fallbackName;
                MessageHelper.Warning(LanguageService.Instance["geo_validate_name_required"]);
                return;
            }

            if (ScatterDefinition.Name != scatterName)
            {
                ScatterDefinition.Name = scatterName;
            }

            UpdateLegendText(scatterName);
        }

        private void UpdateLegendText(string? scatterName)
        {
            if (_legendProxy == null)
            {
                return;
            }

            _legendProxy.LegendText = scatterName ?? string.Empty;
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
            var fillColor = ScottPlot.Color.FromHex(
                GraphMapTemplateService.ConvertWpfHexToScottPlotHex(ScatterDefinition.Color));
            var strokeColor = ScottPlot.Color.FromHex(
                GraphMapTemplateService.ConvertWpfHexToScottPlotHex(ScatterDefinition.StrokeColor));

            // 系列主色：实心用填充色，空心/线型用描边色（图例颜色）
            var seriesColor = ScatterDefinition.HasFill ? fillColor : strokeColor;

            scatterPlot.Color = seriesColor;
            scatterPlot.MarkerSize = ScatterDefinition.Size;
            PlotMarkerStyleApplier.Apply(
                scatterPlot.MarkerStyle,
                ScatterDefinition.MarkerShape,
                fillColor,
                ScatterDefinition.StrokeWidth,
                strokeColor);

            legendProxy.Color = seriesColor;
            legendProxy.MarkerSize = 8; // 固定图例大小
            PlotMarkerStyleApplier.Apply(
                legendProxy.MarkerStyle,
                ScatterDefinition.MarkerShape,
                fillColor,
                ScatterDefinition.StrokeWidth,
                strokeColor);
        }

        public void Highlight()
        {
            if (Plottable is not ScottPlot.Plottables.Scatter scatterPlot)
            {
                return;
            }

            // 与线条等绘图对象一致：整组标记改为固态红色（非描边高亮）
            var red = ScottPlot.Colors.Red;
            scatterPlot.Color = red;
            float strokeWidth = ScatterDefinition.StrokeWidth > 0 ? ScatterDefinition.StrokeWidth : 1.5f;
            PlotMarkerStyleApplier.Apply(
                scatterPlot.MarkerStyle,
                ScatterDefinition.MarkerShape,
                red,
                strokeWidth,
                red);
        }

        public void Dim()
        {
            if (Plottable is ScottPlot.Plottables.Scatter scatterPlot)
            {
                scatterPlot.Color = scatterPlot.Color.WithAlpha(60);
                scatterPlot.MarkerStyle.FillColor = scatterPlot.MarkerStyle.FillColor.WithAlpha(60);
                scatterPlot.MarkerStyle.LineColor = scatterPlot.MarkerStyle.LineColor.WithAlpha(60);
                scatterPlot.MarkerStyle.OutlineColor = scatterPlot.MarkerStyle.OutlineColor.WithAlpha(60);
            }
        }

        public void Restore()
        {
            if (Plottable is ScottPlot.Plottables.Scatter scatterPlot)
            {
                var fillColor = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(ScatterDefinition.Color));
                var strokeColor = ScottPlot.Color.FromHex(
                    GraphMapTemplateService.ConvertWpfHexToScottPlotHex(ScatterDefinition.StrokeColor));
                var seriesColor = ScatterDefinition.HasFill ? fillColor : strokeColor;
                scatterPlot.Color = seriesColor;
                PlotMarkerStyleApplier.Apply(
                    scatterPlot.MarkerStyle,
                    ScatterDefinition.MarkerShape,
                    fillColor,
                    ScatterDefinition.StrokeWidth,
                    strokeColor);
            }
        }
    }
}
