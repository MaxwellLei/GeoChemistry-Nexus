using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Interfaces;
using GeoChemistryNexus.Models;
using ScottPlot;
using System;
using System.Collections.Generic;
using Jint;

namespace GeoChemistryNexus.ViewModels
{
    public partial class FunctionLayerItemViewModel : LayerItemViewModel, IPlotLayer
    {
        public FunctionDefinition FunctionDefinition { get; }

        private ScottPlot.Plottables.Scatter? _scatterPlot;

        // 原始采样缓存（未做坐标轴变换）。y 为 NaN 的项表示断点分隔。
        private (double[] Xs, double[] Ys)? _samples;
        private (string? Formula, double MinX, double MaxX, double MinY, double MaxY, int Count) _sampleKey;

        /// <summary>
        /// 对公式 f(x) 在 [minX, maxX] 上均匀采样；公式语法错误时返回 null。
        /// </summary>
        private static (double[] Xs, double[] Ys)? SampleFunction(
            string formula, double minX, double maxX, double minY, double maxY, int count)
        {
            bool hasMinY = !double.IsNaN(minY);
            bool hasMaxY = !double.IsNaN(maxY);
            double step = (maxX - minX) / (count - 1);

            var engine = JintHelper.CreateEngine();
            // Pre-add common math functions aliases
            engine.Execute("var sin = Math.sin; var cos = Math.cos; var tan = Math.tan; var abs = Math.abs; var sqrt = Math.sqrt; var pow = Math.pow; var log = Math.log; var log10 = Math.log10; var exp = Math.exp; var PI = Math.PI;");

            // 1. 尝试解析并定义为函数，如果解析失败则直接退出
            try
            {
                // 将用户输入的公式包装成 JS 函数 f(x)
                engine.Execute($"function f(x) {{ return {formula}; }}");
            }
            catch
            {
                return null;
            }

            var xs = new List<double>(count);
            var ys = new List<double>(count);
            bool previousPointSkipped = false;

            for (int i = 0; i < count; i++)
            {
                double x = minX + i * step;
                try
                {
                    // 调用已定义的函数 f(x)
                    var result = engine.Invoke("f", x);
                    if (result.IsNumber())
                    {
                        double y = result.AsNumber();
                        bool isInvalidNumber = double.IsNaN(y) || double.IsInfinity(y);
                        bool isOutOfYRange = (hasMinY && y < minY) || (hasMaxY && y > maxY);

                        if (isInvalidNumber || isOutOfYRange)
                        {
                            previousPointSkipped = xs.Count > 0;
                            continue;
                        }

                        if (previousPointSkipped)
                        {
                            xs.Add(double.NaN);
                            ys.Add(double.NaN);
                            previousPointSkipped = false;
                        }

                        xs.Add(x);
                        ys.Add(y);
                    }
                    else
                    {
                        previousPointSkipped = xs.Count > 0;
                    }
                }
                catch
                {
                    // 运行时错误
                    previousPointSkipped = xs.Count > 0;
                }
            }

            return (xs.ToArray(), ys.ToArray());
        }

        public FunctionLayerItemViewModel(FunctionDefinition functionDefinition, int index)
            : base(LanguageService.Instance["function"] + $" {index + 1}")
        {
            FunctionDefinition = functionDefinition;
            FunctionDefinition.PropertyChanged += (s, e) => OnRefreshRequired();
        }

        public void Render(Plot plot)
        {
            if (string.IsNullOrWhiteSpace(FunctionDefinition.Formula)) return;
            if (!JintHelper.IsValidFunctionExpression(FunctionDefinition.Formula)) return;

            double minX = FunctionDefinition.MinX;
            double maxX = FunctionDefinition.MaxX;
            double minY = FunctionDefinition.MinY;
            double maxY = FunctionDefinition.MaxY;
            int count = FunctionDefinition.PointCount;
            if (count < FunctionDefinition.MinPointCount) count = FunctionDefinition.MinPointCount;
            if (count > FunctionDefinition.MaxPointCount) count = FunctionDefinition.MaxPointCount;

            // 采样结果只取决于公式与取值范围（与坐标轴是否为对数轴无关），
            // 图层整体重绘时直接复用，避免重复创建 Jint 引擎并逐点求值。
            (string? Formula, double MinX, double MaxX, double MinY, double MaxY, int Count) sampleKey =
                (FunctionDefinition.Formula, minX, maxX, minY, maxY, count);
            if (_samples == null || !_sampleKey.Equals(sampleKey))
            {
                _samples = SampleFunction(FunctionDefinition.Formula, minX, maxX, minY, maxY, count);
                _sampleKey = sampleKey;
            }

            // 公式语法错误，不绘图（不缓存失败结果）
            if (_samples == null)
            {
                _sampleKey = default;
                return;
            }

            var (rawXs, rawYs) = _samples.Value;
            if (rawXs.Length == 0) return;

            var xs = new double[rawXs.Length];
            var ys = new double[rawXs.Length];
            for (int i = 0; i < rawXs.Length; i++)
            {
                // y 为 NaN 的项是断点分隔符
                if (double.IsNaN(rawYs[i]))
                {
                    xs[i] = double.NaN;
                    ys[i] = double.NaN;
                    continue;
                }

                var renderCoord = PlotTransformHelper.ToRenderCoordinates(plot, rawXs[i], rawYs[i]);
                xs[i] = renderCoord.X;
                ys[i] = renderCoord.Y;
            }

            var scatter = plot.Add.ScatterLine(xs, ys);
            
            scatter.LineWidth = FunctionDefinition.Width;
            scatter.Color = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(FunctionDefinition.Color));
            scatter.LinePattern = GraphMapTemplateService.GetLinePattern(FunctionDefinition.Style.ToString());

            this.Plottable = scatter;
            _scatterPlot = scatter;
        }

        public void Highlight()
        {
            if (_scatterPlot != null)
            {
                _scatterPlot.Color = ScottPlot.Colors.Red;
                _scatterPlot.LineWidth = FunctionDefinition.Width + 2;
            }
        }

        public void Dim()
        {
            if (_scatterPlot != null)
            {
                 _scatterPlot.Color = _scatterPlot.Color.WithAlpha(60);
            }
        }

        public void Restore()
        {
             if (_scatterPlot != null)
             {
                 _scatterPlot.Color = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(FunctionDefinition.Color));
                 _scatterPlot.LineWidth = FunctionDefinition.Width;
             }
        }
    }
}
