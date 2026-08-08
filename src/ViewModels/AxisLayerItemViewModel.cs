using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Interfaces;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Models.SpiderDiagram;
using GeoChemistryNexus.Extensions.ScottPlotExtensions;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// 代表单个坐标轴的图层项
    /// </summary>
    public partial class AxisLayerItemViewModel : LayerItemViewModel, IPlotLayer
    {
        private const byte UnselectedMaskAlpha = 60;

        public BaseAxisDefinition AxisDefinition { get; }
        private readonly ContentLanguageContext? _contentLanguage;
        private Plot? _plot;

        public AxisLayerItemViewModel(BaseAxisDefinition axisDefinition, ContentLanguageContext? contentLanguage = null)
            : base(GetAxisDisplayName(axisDefinition, contentLanguage))
        {
            AxisDefinition = axisDefinition;
            _contentLanguage = contentLanguage;
            SetCustomIconKind(GetAxisIconKind(axisDefinition));
            AxisDefinition.PropertyChanged += OnAxisDefinitionPropertyChanged;
        }

        private void OnAxisDefinitionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BaseAxisDefinition.Label))
            {
                UpdateDisplayName();
            }
        }

        private void UpdateDisplayName()
        {
            Name = GetAxisDisplayName(AxisDefinition, _contentLanguage);
        }

        private static string GetAxisDisplayName(BaseAxisDefinition axisDefinition, ContentLanguageContext? contentLanguage)
        {
            var baseName = axisDefinition.Label.Get(contentLanguage);
            return TruncateName(baseName);
        }

        private static LayerTreeIconKind GetAxisIconKind(BaseAxisDefinition axisDefinition)
        {
            if (axisDefinition is TernaryAxisDefinition)
            {
                return axisDefinition.Type switch
                {
                    "Bottom" => LayerTreeIconKind.AxisA,
                    "Left" => LayerTreeIconKind.AxisB,
                    "Right" => LayerTreeIconKind.AxisC,
                    _ => LayerTreeIconKind.Axis
                };
            }

            return axisDefinition.Type switch
            {
                "Bottom" => LayerTreeIconKind.AxisX,
                "Left" => LayerTreeIconKind.AxisY,
                _ => LayerTreeIconKind.Axis
            };
        }

        public void Render(Plot plot)
        {
            _plot = plot;

            if (AxisDefinition is SpiderAxisDefinition spiderAxisDef)
            {
                ScottPlot.IAxis? targetAxis = spiderAxisDef.Type switch
                {
                    "Bottom" => plot.Axes.Bottom,
                    "Left" => plot.Axes.Left,
                    _ => null
                };

                if (targetAxis == null) return;

                targetAxis = EnsureSubtitleAxis(plot, spiderAxisDef.Type, targetAxis);
                targetAxis.IsVisible = IsVisible;

                ApplyAxisLabelStyle(targetAxis, spiderAxisDef);
                ApplyTickStyles(targetAxis, spiderAxisDef);
                ApplySubtitle(targetAxis, spiderAxisDef);

                // 与笛卡尔一致：显式恢复边框，避免遮罩透明度残留
                targetAxis.FrameLineStyle.Color = ScottPlot.Colors.Black;

                var displayElements = GetSpiderDisplayElements(spiderAxisDef);

                if (spiderAxisDef.Type == "Bottom")
                {
                    if (displayElements.Count > 0)
                    {
                        var customTicks = new ScottPlot.TickGenerators.NumericManual();
                        for (int i = 0; i < displayElements.Count; i++)
                        {
                            customTicks.AddMajor(i + 1, displayElements[i]);
                        }

                        int minorTickCount = spiderAxisDef.MinorTickWidth <= 0 || spiderAxisDef.MinorTickLength <= 0
                            ? 0
                            : Math.Max(0, spiderAxisDef.MinorTicksPerMajorTick);

                        if (minorTickCount > 0)
                        {
                            for (int i = 0; i < displayElements.Count - 1; i++)
                            {
                                double left = i + 1;
                                double step = 1d / (minorTickCount + 1);
                                for (int j = 1; j <= minorTickCount; j++)
                                {
                                    customTicks.AddMinor(left + step * j);
                                }
                            }
                        }

                        targetAxis.TickGenerator = customTicks;
                        plot.Axes.SetLimits(
                            left: 0.5,
                            right: displayElements.Count + 0.5,
                            bottom: -2,
                            top: 4
                        );
                    }
                }
                else if (spiderAxisDef.Type == "Left")
                {
                    var hideMinor = spiderAxisDef.MinorTickWidth <= 0 || spiderAxisDef.MinorTickLength <= 0;
                    targetAxis.TickGenerator = new LogDecadeTickGenerator
                    {
                        ShowMinorTicks = !hideMinor
                    };

                    string axisLabel = spiderAxisDef.Label.Get(_contentLanguage);
                    targetAxis.Label.Text = string.IsNullOrWhiteSpace(axisLabel)
                        ? GetDefaultSpiderYAxisLabel(spiderAxisDef)
                        : axisLabel;
                }
            }
            else if (AxisDefinition is CartesianAxisDefinition cartesianAxisDef)
            {
                ScottPlot.IAxis? targetAxis = cartesianAxisDef.Type switch
                {
                    "Left" => plot.Axes.Left,
                    "Right" => plot.Axes.Right,
                    "Bottom" => plot.Axes.Bottom,
                    "Top" => plot.Axes.Top,
                    _ => null
                };

                if (targetAxis == null) return;

                if (cartesianAxisDef.Type == "Left" && targetAxis is not LeftAxisWithSubtitle)
                {
                    plot.Axes.Remove(targetAxis);
                    var newAxis = new LeftAxisWithSubtitle();
                    plot.Axes.AddLeftAxis(newAxis);
                    RebindDefaultGridAxis(plot, newAxis);
                    targetAxis = newAxis;
                }
                else if (cartesianAxisDef.Type == "Right" && targetAxis is not RightAxisWithSubtitle)
                {
                    plot.Axes.Remove(targetAxis);
                    var newAxis = new RightAxisWithSubtitle();
                    plot.Axes.AddRightAxis(newAxis);
                    targetAxis = newAxis;
                }
                else if (cartesianAxisDef.Type == "Bottom" && targetAxis is not BottomAxisWithSubtitle)
                {
                    plot.Axes.Remove(targetAxis);
                    var newAxis = new BottomAxisWithSubtitle();
                    plot.Axes.AddBottomAxis(newAxis);
                    RebindDefaultGridAxis(plot, newAxis);
                    targetAxis = newAxis;
                }
                else if (cartesianAxisDef.Type == "Top" && targetAxis is not TopAxisWithSubtitle)
                {
                    plot.Axes.Remove(targetAxis);
                    var newAxis = new TopAxisWithSubtitle();
                    plot.Axes.AddTopAxis(newAxis);
                    targetAxis = newAxis;
                }

                targetAxis.IsVisible = IsVisible;

                targetAxis.Label.Text = cartesianAxisDef.Label.Get(_contentLanguage);
                targetAxis.Label.FontName = cartesianAxisDef.Family;
                targetAxis.Label.FontSize = cartesianAxisDef.Size;
                targetAxis.Label.ForeColor = ScottPlot.Color.FromHex(
                    GraphMapTemplateService.ConvertWpfHexToScottPlotHex(cartesianAxisDef.Color));
                targetAxis.Label.Bold = cartesianAxisDef.IsBold;
                targetAxis.Label.Italic = cartesianAxisDef.IsItalic;

                if (cartesianAxisDef.ScaleType == AxisScaleType.Logarithmic)
                {
                    var hideMinor = cartesianAxisDef.MinorTickWidth <= 0 || cartesianAxisDef.MinorTickLength <= 0;
                    targetAxis.TickGenerator = new LogDecadeTickGenerator
                    {
                        ShowMinorTicks = !hideMinor
                    };
                }
                else
                {
                    var minorCount = Math.Max(0, cartesianAxisDef.MinorTicksPerMajorTick);
                    if (cartesianAxisDef.MinorTickWidth <= 0 || cartesianAxisDef.MinorTickLength <= 0)
                        minorCount = 0;

                    if (cartesianAxisDef.MajorTickInterval > 0)
                    {
                        targetAxis.TickGenerator = new FixedIntervalTickGenerator(
                            cartesianAxisDef.MajorTickInterval,
                            minorCount);
                    }
                    else
                    {
                        var tickGen = new ScottPlot.TickGenerators.NumericAutomatic();
                        int divisions = minorCount > 0 ? minorCount + 1 : 0;
                        tickGen.MinorTickGenerator = new ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator(divisions);
                        targetAxis.TickGenerator = tickGen;
                    }
                }

                targetAxis.MajorTickStyle.Length = cartesianAxisDef.MajorTickLength;
                targetAxis.MajorTickStyle.Width = cartesianAxisDef.MajorTickWidth;
                targetAxis.MajorTickStyle.Color = ScottPlot.Color.FromHex(
                    GraphMapTemplateService.ConvertWpfHexToScottPlotHex(cartesianAxisDef.MajorTickWidthColor));
                targetAxis.MajorTickStyle.AntiAlias = true;

                targetAxis.MinorTickStyle.Length = cartesianAxisDef.MinorTickLength;
                targetAxis.MinorTickStyle.Width = cartesianAxisDef.MinorTickWidth;
                targetAxis.MinorTickStyle.Color = ScottPlot.Color.FromHex(
                    GraphMapTemplateService.ConvertWpfHexToScottPlotHex(cartesianAxisDef.MinorTickColor));
                targetAxis.MinorTickStyle.AntiAlias = true;

                targetAxis.TickLabelStyle.FontName = cartesianAxisDef.TickLableFamily;
                targetAxis.TickLabelStyle.FontSize = cartesianAxisDef.TickLablesize;
                targetAxis.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex(
                    GraphMapTemplateService.ConvertWpfHexToScottPlotHex(cartesianAxisDef.TickLablecolor));
                targetAxis.TickLabelStyle.Bold = cartesianAxisDef.TickLableisBold;
                targetAxis.TickLabelStyle.Italic = cartesianAxisDef.TickLableisItalic;

                // Render 需显式恢复边框色：选中遮罩会降低 FrameLineStyle 透明度，且复用轴对象时不会自动重置
                targetAxis.FrameLineStyle.Color = ScottPlot.Colors.Black;

                if (targetAxis is LeftAxisWithSubtitle leftSub)
                {
                    ConfigureSubtitle(leftSub.SubLabelStyle, cartesianAxisDef);
                    leftSub.SubLabelText = cartesianAxisDef.SubLabel.Get(_contentLanguage)?.Replace("\r\n", "\n").Replace("\r", "\n") ?? "";
                }
                else if (targetAxis is RightAxisWithSubtitle rightSub)
                {
                    ConfigureSubtitle(rightSub.SubLabelStyle, cartesianAxisDef);
                    rightSub.SubLabelText = cartesianAxisDef.SubLabel.Get(_contentLanguage)?.Replace("\r\n", "\n").Replace("\r", "\n") ?? "";
                }
                else if (targetAxis is BottomAxisWithSubtitle bottomSub)
                {
                    ConfigureSubtitle(bottomSub.SubLabelStyle, cartesianAxisDef);
                    bottomSub.SubLabelText = cartesianAxisDef.SubLabel.Get(_contentLanguage)?.Replace("\r\n", "\n").Replace("\r", "\n") ?? "";
                }
                else if (targetAxis is TopAxisWithSubtitle topSub)
                {
                    ConfigureSubtitle(topSub.SubLabelStyle, cartesianAxisDef);
                    topSub.SubLabelText = cartesianAxisDef.SubLabel.Get(_contentLanguage)?.Replace("\r\n", "\n").Replace("\r", "\n") ?? "";
                }

                UpdateAxisLimits(plot, cartesianAxisDef, targetAxis);
            }
            else if (AxisDefinition is TernaryAxisDefinition ternaryAxisDef)
            {
                var triangularAxis = plot.GetPlottables().OfType<ScottPlot.Plottables.TriangularAxis>().FirstOrDefault();
                if (triangularAxis == null) return;

                ScottPlot.TriangularAxisEdge? targetEdge = ternaryAxisDef.Type switch
                {
                    "Bottom" => triangularAxis.Bottom,
                    "Left" => triangularAxis.Left,
                    "Right" => triangularAxis.Right,
                    _ => null
                };

                if (targetEdge != null)
                {
                    bool edgeVisible = IsVisible;

                    targetEdge.LabelText = edgeVisible
                        ? ternaryAxisDef.Label.Get(_contentLanguage)?.Replace("\r\n", "\n").Replace("\r", "\n") ?? ""
                        : string.Empty;
                    targetEdge.LabelStyle.FontName = ternaryAxisDef.Family;
                    targetEdge.LabelStyle.FontSize = edgeVisible ? ternaryAxisDef.Size : 0;
                    targetEdge.LabelStyle.ForeColor = ScottPlot.Color.FromHex(
                        edgeVisible
                            ? GraphMapTemplateService.ConvertWpfHexToScottPlotHex(ternaryAxisDef.Color)
                            : "#00000000");
                    targetEdge.LabelStyle.Bold = ternaryAxisDef.IsBold;
                    targetEdge.LabelStyle.Italic = ternaryAxisDef.IsItalic;
                    targetEdge.LabelStyle.OffsetX = (float)ternaryAxisDef.LabelOffsetX;
                    targetEdge.LabelStyle.OffsetY = (float)ternaryAxisDef.LabelOffsetY;

                    targetEdge.TickMarkStyle.Width = edgeVisible && ternaryAxisDef.IsShowMajorTicks ? 1 : 0;
                    targetEdge.TickMarkStyle.Color = edgeVisible && ternaryAxisDef.IsShowMajorTicks
                        ? ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(ternaryAxisDef.MajorTickWidthColor))
                        : ScottPlot.Color.FromHex("#00000000");

                    targetEdge.TickLabelStyle.FontName = ternaryAxisDef.TickLableFamily;
                    targetEdge.TickLabelStyle.FontSize = edgeVisible && ternaryAxisDef.IsShowTickLabels ? ternaryAxisDef.TickLablesize : 0;
                    targetEdge.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex(
                        edgeVisible
                            ? GraphMapTemplateService.ConvertWpfHexToScottPlotHex(ternaryAxisDef.TickLablecolor)
                            : "#00000000");
                    targetEdge.TickLabelStyle.Bold = ternaryAxisDef.TickLableisBold;
                    targetEdge.TickLabelStyle.Italic = ternaryAxisDef.TickLableisItalic;

                    targetEdge.Ticks.Clear();
                    if (!edgeVisible)
                    {
                        return;
                    }

                    int ticksPerEdge = 10;
                    for (int i = 0; i <= ticksPerEdge; i++)
                    {
                        double fraction = i / (double)ticksPerEdge;
                        double tickX = targetEdge.Start.X + fraction * (targetEdge.End.X - targetEdge.Start.X);
                        double tickY = targetEdge.Start.Y + fraction * (targetEdge.End.Y - targetEdge.Start.Y);
                        ScottPlot.Coordinates tickPoint = new(tickX, tickY);
                        string tickLabel = $"{fraction:0.#}";
                        targetEdge.Ticks.Add((tickPoint, tickLabel));
                    }
                }
            }
        }

        private ScottPlot.IAxis EnsureSubtitleAxis(Plot plot, string axisType, ScottPlot.IAxis targetAxis)
        {
            if (axisType == "Left" && targetAxis is not LeftAxisWithSubtitle)
            {
                plot.Axes.Remove(targetAxis);
                var newAxis = new LeftAxisWithSubtitle();
                plot.Axes.AddLeftAxis(newAxis);
                RebindDefaultGridAxis(plot, newAxis);
                return newAxis;
            }

            if (axisType == "Right" && targetAxis is not RightAxisWithSubtitle)
            {
                plot.Axes.Remove(targetAxis);
                var newAxis = new RightAxisWithSubtitle();
                plot.Axes.AddRightAxis(newAxis);
                return newAxis;
            }

            if (axisType == "Bottom" && targetAxis is not BottomAxisWithSubtitle)
            {
                plot.Axes.Remove(targetAxis);
                var newAxis = new BottomAxisWithSubtitle();
                plot.Axes.AddBottomAxis(newAxis);
                RebindDefaultGridAxis(plot, newAxis);
                return newAxis;
            }

            if (axisType == "Top" && targetAxis is not TopAxisWithSubtitle)
            {
                plot.Axes.Remove(targetAxis);
                var newAxis = new TopAxisWithSubtitle();
                plot.Axes.AddTopAxis(newAxis);
                return newAxis;
            }

            return targetAxis;
        }

        private static void RebindDefaultGridAxis(Plot plot, ScottPlot.IAxis axis)
        {
            if (axis is ScottPlot.IXAxis xAxis && axis.Edge == ScottPlot.Edge.Bottom)
            {
                plot.Axes.DefaultGrid.XAxis = xAxis;
                foreach (var plottable in plot.GetPlottables())
                {
                    if (plottable.Axes.XAxis is not null)
                        plottable.Axes.XAxis = xAxis;
                }
            }
            else if (axis is ScottPlot.IYAxis yAxis && axis.Edge == ScottPlot.Edge.Left)
            {
                plot.Axes.DefaultGrid.YAxis = yAxis;
                foreach (var plottable in plot.GetPlottables())
                {
                    if (plottable.Axes.YAxis is not null)
                        plottable.Axes.YAxis = yAxis;
                }
            }
        }

        private void UpdateAxisLimits(Plot plot, CartesianAxisDefinition axisDef, IAxis targetAxis)
        {
            if (PlotTransformHelper.TryGetPlotAxisRange(
                    axisDef.Minimum,
                    axisDef.Maximum,
                    axisDef.ScaleType,
                    out double plotMin,
                    out double plotMax))
            {
                targetAxis.Range.Min = plotMin;
                targetAxis.Range.Max = plotMax;
                return;
            }

            bool isMinSet = !double.IsNaN(axisDef.Minimum);
            bool isMaxSet = !double.IsNaN(axisDef.Maximum);

            // 两端都是自动：不要沿用当前轴限（否则清回 Auto 后会卡在半固定范围，例如 1~10）
            // 完整自动范围由 CenterPlot / AutoScale 在全部图层渲染后重算
            if (!isMinSet && !isMaxSet)
                return;

            var currentLimits = targetAxis.Range;
            var newMin = isMinSet ? axisDef.Minimum : currentLimits.Min;
            var newMax = isMaxSet ? axisDef.Maximum : currentLimits.Max;

            if (axisDef.ScaleType == AxisScaleType.Logarithmic)
            {
                if (isMinSet) newMin = axisDef.Minimum > 0 ? Math.Log10(axisDef.Minimum) : currentLimits.Min;
                if (isMaxSet) newMax = axisDef.Maximum > 0 ? Math.Log10(axisDef.Maximum) : currentLimits.Max;
            }

            // 仅一端设定时不做端点边距，保持原有半自动范围行为
            targetAxis.Range.Min = newMin;
            targetAxis.Range.Max = newMax;
        }

        public void Highlight()
        {
            if (_plot == null || !IsVisible)
                return;

            if (AxisDefinition is TernaryAxisDefinition ternaryAxisDef)
            {
                var edge = TryGetTernaryEdge(_plot, ternaryAxisDef.Type);
                if (edge == null)
                    return;

                edge.LabelStyle.ForeColor = ScottPlot.Colors.Red;
                edge.TickMarkStyle.Color = ScottPlot.Colors.Red;
                edge.TickLabelStyle.ForeColor = ScottPlot.Colors.Red;
                return;
            }

            var targetAxis = TryGetCartesianAxis(_plot, AxisDefinition.Type);
            if (targetAxis == null)
                return;

            targetAxis.Label.ForeColor = ScottPlot.Colors.Red;
            targetAxis.TickLabelStyle.ForeColor = ScottPlot.Colors.Red;
            targetAxis.MajorTickStyle.Color = ScottPlot.Colors.Red;
            targetAxis.MinorTickStyle.Color = ScottPlot.Colors.Red;
            targetAxis.FrameLineStyle.Color = ScottPlot.Colors.Red;
            HighlightSubtitle(targetAxis);
        }

        public void Dim()
        {
            if (_plot == null || !IsVisible)
                return;

            if (AxisDefinition is TernaryAxisDefinition ternaryAxisDef)
            {
                var edge = TryGetTernaryEdge(_plot, ternaryAxisDef.Type);
                if (edge == null)
                    return;

                edge.LabelStyle.ForeColor = edge.LabelStyle.ForeColor.WithAlpha(UnselectedMaskAlpha);
                edge.TickMarkStyle.Color = edge.TickMarkStyle.Color.WithAlpha(UnselectedMaskAlpha);
                edge.TickLabelStyle.ForeColor = edge.TickLabelStyle.ForeColor.WithAlpha(UnselectedMaskAlpha);
                return;
            }

            var targetAxis = TryGetCartesianAxis(_plot, AxisDefinition.Type);
            if (targetAxis == null)
                return;

            targetAxis.Label.ForeColor = targetAxis.Label.ForeColor.WithAlpha(UnselectedMaskAlpha);
            targetAxis.TickLabelStyle.ForeColor = targetAxis.TickLabelStyle.ForeColor.WithAlpha(UnselectedMaskAlpha);
            targetAxis.MajorTickStyle.Color = targetAxis.MajorTickStyle.Color.WithAlpha(UnselectedMaskAlpha);
            targetAxis.MinorTickStyle.Color = targetAxis.MinorTickStyle.Color.WithAlpha(UnselectedMaskAlpha);
            targetAxis.FrameLineStyle.Color = targetAxis.FrameLineStyle.Color.WithAlpha(UnselectedMaskAlpha);
            DimSubtitle(targetAxis);
        }

        public void Restore()
        {
            if (_plot == null)
                return;

            ApplyColorsFromDefinition(_plot);
        }

        private void ApplyColorsFromDefinition(Plot plot)
        {
            if (AxisDefinition is TernaryAxisDefinition ternaryAxisDef)
            {
                var edge = TryGetTernaryEdge(plot, ternaryAxisDef.Type);
                if (edge == null)
                    return;

                bool edgeVisible = IsVisible;
                edge.LabelStyle.ForeColor = ScottPlot.Color.FromHex(
                    edgeVisible
                        ? GraphMapTemplateService.ConvertWpfHexToScottPlotHex(ternaryAxisDef.Color)
                        : "#00000000");
                edge.TickMarkStyle.Color = edgeVisible && ternaryAxisDef.IsShowMajorTicks
                    ? ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(ternaryAxisDef.MajorTickWidthColor))
                    : ScottPlot.Color.FromHex("#00000000");
                edge.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex(
                    edgeVisible
                        ? GraphMapTemplateService.ConvertWpfHexToScottPlotHex(ternaryAxisDef.TickLablecolor)
                        : "#00000000");
                return;
            }

            var targetAxis = TryGetCartesianAxis(plot, AxisDefinition.Type);
            if (targetAxis == null)
                return;

            if (AxisDefinition is CartesianAxisDefinition cartesianAxisDef)
            {
                ApplyAxisLabelStyle(targetAxis, cartesianAxisDef);
                ApplyTickStyles(targetAxis, cartesianAxisDef);
                ApplySubtitle(targetAxis, cartesianAxisDef);
                targetAxis.FrameLineStyle.Color = ScottPlot.Colors.Black;
            }
            else
            {
                ApplyAxisLabelStyle(targetAxis, AxisDefinition);
                targetAxis.FrameLineStyle.Color = ScottPlot.Colors.Black;
            }
        }

        private static ScottPlot.IAxis? TryGetCartesianAxis(Plot plot, string axisType)
        {
            return axisType switch
            {
                "Left" => plot.Axes.Left,
                "Right" => plot.Axes.Right,
                "Bottom" => plot.Axes.Bottom,
                "Top" => plot.Axes.Top,
                _ => null
            };
        }

        private static ScottPlot.TriangularAxisEdge? TryGetTernaryEdge(Plot plot, string axisType)
        {
            var triangularAxis = plot.GetPlottables().OfType<ScottPlot.Plottables.TriangularAxis>().FirstOrDefault();
            if (triangularAxis == null)
                return null;

            return axisType switch
            {
                "Bottom" => triangularAxis.Bottom,
                "Left" => triangularAxis.Left,
                "Right" => triangularAxis.Right,
                _ => null
            };
        }

        private static void DimSubtitle(ScottPlot.IAxis targetAxis)
        {
            if (targetAxis is LeftAxisWithSubtitle leftSub)
                leftSub.SubLabelStyle.ForeColor = leftSub.SubLabelStyle.ForeColor.WithAlpha(UnselectedMaskAlpha);
            else if (targetAxis is RightAxisWithSubtitle rightSub)
                rightSub.SubLabelStyle.ForeColor = rightSub.SubLabelStyle.ForeColor.WithAlpha(UnselectedMaskAlpha);
            else if (targetAxis is BottomAxisWithSubtitle bottomSub)
                bottomSub.SubLabelStyle.ForeColor = bottomSub.SubLabelStyle.ForeColor.WithAlpha(UnselectedMaskAlpha);
            else if (targetAxis is TopAxisWithSubtitle topSub)
                topSub.SubLabelStyle.ForeColor = topSub.SubLabelStyle.ForeColor.WithAlpha(UnselectedMaskAlpha);
        }

        private static void HighlightSubtitle(ScottPlot.IAxis targetAxis)
        {
            if (targetAxis is LeftAxisWithSubtitle leftSub)
                leftSub.SubLabelStyle.ForeColor = ScottPlot.Colors.Red;
            else if (targetAxis is RightAxisWithSubtitle rightSub)
                rightSub.SubLabelStyle.ForeColor = ScottPlot.Colors.Red;
            else if (targetAxis is BottomAxisWithSubtitle bottomSub)
                bottomSub.SubLabelStyle.ForeColor = ScottPlot.Colors.Red;
            else if (targetAxis is TopAxisWithSubtitle topSub)
                topSub.SubLabelStyle.ForeColor = ScottPlot.Colors.Red;
        }

        private void ApplyAxisLabelStyle(ScottPlot.IAxis targetAxis, BaseAxisDefinition axisDef)
        {
            string labelText = axisDef.Label.Get(_contentLanguage);
            targetAxis.Label.Text = labelText;

            if (axisDef is SpiderAxisDefinition spiderAxisDef)
            {
                spiderAxisDef.Family = ScottPlot.Fonts.Detect(labelText);
                targetAxis.Label.FontName = spiderAxisDef.Family;
            }
            else
            {
                targetAxis.Label.FontName = axisDef.Family;
            }

            targetAxis.Label.FontSize = axisDef.Size;
            targetAxis.Label.ForeColor = ScottPlot.Color.FromHex(
                GraphMapTemplateService.ConvertWpfHexToScottPlotHex(axisDef.Color));
            targetAxis.Label.Bold = axisDef.IsBold;
            targetAxis.Label.Italic = axisDef.IsItalic;
        }

        private void ApplyTickStyles(ScottPlot.IAxis targetAxis, CartesianAxisDefinition axisDef)
        {
            targetAxis.MajorTickStyle.Length = axisDef.MajorTickLength;
            targetAxis.MajorTickStyle.Width = axisDef.MajorTickWidth;
            targetAxis.MajorTickStyle.Color = ScottPlot.Color.FromHex(
                GraphMapTemplateService.ConvertWpfHexToScottPlotHex(axisDef.MajorTickWidthColor));
            targetAxis.MajorTickStyle.AntiAlias = true;

            targetAxis.MinorTickStyle.Length = axisDef.MinorTickLength;
            targetAxis.MinorTickStyle.Width = axisDef.MinorTickWidth;
            targetAxis.MinorTickStyle.Color = ScottPlot.Color.FromHex(
                GraphMapTemplateService.ConvertWpfHexToScottPlotHex(axisDef.MinorTickColor));
            targetAxis.MinorTickStyle.AntiAlias = true;

            targetAxis.TickLabelStyle.FontName = axisDef.TickLableFamily;
            targetAxis.TickLabelStyle.FontSize = axisDef.TickLablesize;
            targetAxis.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex(
                GraphMapTemplateService.ConvertWpfHexToScottPlotHex(axisDef.TickLablecolor));
            targetAxis.TickLabelStyle.Bold = axisDef.TickLableisBold;
            targetAxis.TickLabelStyle.Italic = axisDef.TickLableisItalic;
        }

        private void ApplySubtitle(ScottPlot.IAxis targetAxis, CartesianAxisDefinition axisDef)
        {
            string subtitleText = axisDef.SubLabel.Get(_contentLanguage)?.Replace("\r\n", "\n").Replace("\r", "\n") ?? "";

            if (targetAxis is LeftAxisWithSubtitle leftSub)
            {
                ConfigureSubtitle(leftSub.SubLabelStyle, axisDef);
                leftSub.SubLabelText = subtitleText;
            }
            else if (targetAxis is RightAxisWithSubtitle rightSub)
            {
                ConfigureSubtitle(rightSub.SubLabelStyle, axisDef);
                rightSub.SubLabelText = subtitleText;
            }
            else if (targetAxis is BottomAxisWithSubtitle bottomSub)
            {
                ConfigureSubtitle(bottomSub.SubLabelStyle, axisDef);
                bottomSub.SubLabelText = subtitleText;
            }
            else if (targetAxis is TopAxisWithSubtitle topSub)
            {
                ConfigureSubtitle(topSub.SubLabelStyle, axisDef);
                topSub.SubLabelText = subtitleText;
            }
        }

        private List<string> GetSpiderDisplayElements(SpiderAxisDefinition spiderAxisDef)
        {
            var configuredElements = spiderAxisDef.ElementOrder
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (!spiderAxisDef.IsNormalizationEnabled || string.IsNullOrWhiteSpace(spiderAxisDef.NormalizationStandard))
            {
                return configuredElements;
            }

            var allStandards = spiderAxisDef.SpiderType == "REE"
                ? NormalizationData.GetReeStandards()
                : NormalizationData.GetTraceElementStandards();
            var selectedStandard = allStandards.FirstOrDefault(s => s.Name == spiderAxisDef.NormalizationStandard);
            if (selectedStandard == null)
            {
                return configuredElements;
            }

            var displayElements = configuredElements
                .Where(e => selectedStandard.Values.ContainsKey(e))
                .ToList();

            return displayElements.Count > 0 ? displayElements : configuredElements;
        }

        private string GetDefaultSpiderYAxisLabel(SpiderAxisDefinition spiderAxisDef)
        {
            if (!spiderAxisDef.IsNormalizationEnabled)
            {
                return "Concentration (ppm)";
            }

            var allStandards = spiderAxisDef.SpiderType == "REE"
                ? NormalizationData.GetReeStandards()
                : NormalizationData.GetTraceElementStandards();
            var selectedStandard = allStandards.FirstOrDefault(s => s.Name == spiderAxisDef.NormalizationStandard);

            return selectedStandard == null
                ? "Sample / Standard"
                : $"Sample / {selectedStandard.ShortName}";
        }

        private void ConfigureSubtitle(ScottPlot.LabelStyle style, CartesianAxisDefinition def)
        {
            style.FontName = def.Family;
            style.FontSize = def.SubLabelSize;
            style.ForeColor = ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(def.SubLabelColor));
            style.Bold = def.SubLabelBold;
            style.Italic = def.SubLabelItalic;
        }
    }
}
