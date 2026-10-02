using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers.PlotMarkers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System.Collections.Generic;
using System.Linq;

namespace GeoChemistryNexus.Models.SpiderDiagram
{
    /// <summary>
    /// 蛛网图样品线属性模型，用于属性面板编辑
    /// </summary>
    public partial class SpiderSamplePropertyModel : ObservableObject
    {
        private readonly List<Scatter> _scatters = new();
        private Scatter? _legendProxy;

        /// <summary>
        /// 样品名称
        /// </summary>
        [ObservableProperty]
        private string _sampleName = string.Empty;

        /// <summary>
        /// 线条颜色（Hex）
        /// </summary>
        [ObservableProperty]
        private string _color = "#1f77b4";

        /// <summary>
        /// 线条宽度
        /// </summary>
        [ObservableProperty]
        private float _lineWidth = 1.5f;

        /// <summary>
        /// 标记点大小
        /// </summary>
        [ObservableProperty]
        private float _markerSize = 5f;

        /// <summary>
        /// 标记点形状（与图解模板数据点相同的 PlotMarkerShape）
        /// </summary>
        [ObservableProperty]
        private PlotMarkerShape _markerShape = PlotMarkerShape.FilledCircle;

        public SpiderSamplePropertyModel()
        {
        }

        public SpiderSamplePropertyModel(Scatter scatter, Scatter? legendProxy, WpfPlot? wpfPlot)
            : this(new[] { scatter }, legendProxy, wpfPlot)
        {
        }

        public SpiderSamplePropertyModel(IEnumerable<Scatter> scatters, Scatter? legendProxy, WpfPlot? wpfPlot)
        {
            _scatters = scatters?.Where(s => s != null).Distinct().ToList() ?? new List<Scatter>();
            _legendProxy = legendProxy;

            var firstScatter = _scatters.FirstOrDefault();
            if (firstScatter != null)
            {
                // 从第一条 scatter 读取初始值，分组内其他曲线同步使用同一套样式
                _sampleName = legendProxy?.LegendText ?? firstScatter.LegendText ?? string.Empty;
                _color = firstScatter.Color.ToHex();
                _lineWidth = firstScatter.LineWidth;
                _markerSize = firstScatter.MarkerSize;
                _markerShape = PlotMarkerStyleApplier.FromScottPlotShape(firstScatter.MarkerShape);
            }
        }

        /// <summary>
        /// 当前形状是否具有填充（与散点属性面板一致，便于扩展）
        /// </summary>
        public bool HasFill => PlotMarkerStyleApplier.IsFilled(MarkerShape);

        partial void OnColorChanged(string value)
        {
            ApplyLineAndMarkerStyles();
        }

        partial void OnLineWidthChanged(float value)
        {
            if (_scatters.Count == 0) return;
            foreach (var scatter in _scatters)
            {
                scatter.LineWidth = value;
            }
            // 不更新 _legendProxy.LineWidth — 图例保持固定大小
        }

        partial void OnMarkerSizeChanged(float value)
        {
            if (_scatters.Count == 0) return;
            foreach (var scatter in _scatters)
            {
                scatter.MarkerSize = value;
            }
            // 不更新 _legendProxy.MarkerSize — 图例保持固定大小
        }

        partial void OnMarkerShapeChanged(PlotMarkerShape value)
        {
            OnPropertyChanged(nameof(HasFill));
            ApplyMarkerStyles();
        }

        partial void OnSampleNameChanged(string value)
        {
            if (_scatters.Count == 0) return;
            // 名称更新到图例代理（主 scatter 无 LegendText）
            if (_legendProxy != null)
            {
                _legendProxy.LegendText = value;
            }
            else
            {
                foreach (var scatter in _scatters)
                {
                    scatter.LegendText = value;
                }
            }
        }

        private void ApplyLineAndMarkerStyles()
        {
            if (_scatters.Count == 0) return;
            try
            {
                var color = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(Color));
                foreach (var scatter in _scatters)
                {
                    scatter.Color = color;
                }

                if (_legendProxy != null)
                {
                    _legendProxy.Color = color;
                }

                ApplyMarkerStyles(color);
            }
            catch
            {
                // 忽略非法颜色
            }
        }

        private void ApplyMarkerStyles(ScottPlot.Color? overrideColor = null)
        {
            if (_scatters.Count == 0) return;

            ScottPlot.Color color;
            try
            {
                color = overrideColor
                    ?? ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(Color));
            }
            catch
            {
                return;
            }

            float strokeWidth = PlotMarkerStyleApplier.IsFilled(MarkerShape) ? 0f : 1.5f;
            foreach (var scatter in _scatters)
            {
                PlotMarkerStyleApplier.Apply(
                    scatter.MarkerStyle,
                    MarkerShape,
                    color,
                    strokeWidth,
                    color);
                scatter.MarkerSize = MarkerSize;
            }

            if (_legendProxy != null)
            {
                PlotMarkerStyleApplier.Apply(
                    _legendProxy.MarkerStyle,
                    MarkerShape,
                    color,
                    strokeWidth,
                    color);
            }
        }
    }
}
