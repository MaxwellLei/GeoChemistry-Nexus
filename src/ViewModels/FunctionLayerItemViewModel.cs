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

        private ScottPlot.Plottables.Scatter _scatterPlot;

        public FunctionLayerItemViewModel(FunctionDefinition functionDefinition, int index)
            : base("Function " + (index + 1))
        {
            FunctionDefinition = functionDefinition;
            FunctionDefinition.PropertyChanged += (s, e) => OnRefreshRequired();
        }

        public void Render(Plot plot)
        {
            if (string.IsNullOrWhiteSpace(FunctionDefinition.Formula)) return;
            if (!JintHelper.IsValidFunctionExpression(FunctionDefinition.Formula)) return;

            var xs = new List<double>();
            var ys = new List<double>();
            
            double minX = FunctionDefinition.MinX;
            double maxX = FunctionDefinition.MaxX;
            double minY = FunctionDefinition.MinY;
            double maxY = FunctionDefinition.MaxY;
            bool hasMinY = !double.IsNaN(minY);
            bool hasMaxY = !double.IsNaN(maxY);
            int count = FunctionDefinition.PointCount;
            if (count < 2) count = 2;
            
            double step = (maxX - minX) / (count - 1);
            bool previousPointSkipped = false;
            
            var engine = new Engine();
            // Pre-add common math functions aliases
            engine.Execute("var sin = Math.sin; var cos = Math.cos; var tan = Math.tan; var abs = Math.abs; var sqrt = Math.sqrt; var pow = Math.pow; var log = Math.log; var log10 = Math.log10; var exp = Math.exp; var PI = Math.PI;");

            // 1. 尝试解析并定义为函数，如果解析失败则直接退出
            try
            {
                // 将用户输入的公式包装成 JS 函数 f(x)
                engine.Execute($"function f(x) {{ return {FunctionDefinition.Formula}; }}");
            }
            catch
            {
                // 公式语法错误，直接返回不绘图
                return;
            }
            
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
                        
                        var renderCoord = PlotTransformHelper.ToRenderCoordinates(plot, x, y);
                        xs.Add(renderCoord.X);
                        ys.Add(renderCoord.Y);
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

            if (xs.Count == 0) return;

            var scatter = plot.Add.ScatterLine(xs.ToArray(), ys.ToArray());
            
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
