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
        public BaseAxisDefinition AxisDefinition { get; }

        public AxisLayerItemViewModel(BaseAxisDefinition axisDefinition)
            : base(GetAxisDisplayName(axisDefinition)) // 根据坐标轴类型设置名称
        {
            AxisDefinition = axisDefinition;
            SetCustomIconKind(GetAxisIconKind(axisDefinition));
            // 监听 Label 属性的变化，及时更新图层列表名称
            AxisDefinition.PropertyChanged += OnAxisDefinitionPropertyChanged;
        }

        /// <summary>
        /// 监听坐标轴定义属性变化，更新显示名称
        /// </summary>
        private void OnAxisDefinitionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BaseAxisDefinition.Label))
            {
                UpdateDisplayName();
            }
        }

        /// <summary>
        /// 更新图层列表中的显示名称
        /// </summary>
        private void UpdateDisplayName()
        {
            Name = GetAxisDisplayName(AxisDefinition);
        }

        /// <summary>
        /// 获取坐标轴的显示名称
        /// </summary>
        private static string GetAxisDisplayName(BaseAxisDefinition axisDefinition)
        {
            var baseName = axisDefinition.Label.Get();

            // 图层树标题直接使用轴标签本身,移除旧的字符拼接
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
            // 1. 处理蜘蛛图坐标轴 (Spider)
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
                    var tickGen = new ScottPlot.TickGenerators.NumericAutomatic();
                    var hideMinor = spiderAxisDef.MinorTickWidth <= 0 || spiderAxisDef.MinorTickLength <= 0;
                    tickGen.MinorTickGenerator = hideMinor
                        ? new ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator(0)
                        : new ScottPlot.TickGenerators.LogMinorTickGenerator();
                    tickGen.IntegerTicksOnly = true;
                    tickGen.LabelFormatter = y =>
                    {
                        double val = Math.Pow(10, y);
                        return val.ToString("G10");
                    };
                    targetAxis.TickGenerator = tickGen;

                    string axisLabel = spiderAxisDef.Label.Get();
                    targetAxis.Label.Text = string.IsNullOrWhiteSpace(axisLabel)
                        ? GetDefaultSpiderYAxisLabel(spiderAxisDef)
                        : axisLabel;
                }
            }

            // 2. 处理笛卡尔坐标轴 (Cartesian)
            else if (AxisDefinition is CartesianAxisDefinition cartesianAxisDef)
            {
                // 根据类型找到对应的轴对象
                ScottPlot.IAxis? targetAxis = cartesianAxisDef.Type switch
                {
                    "Left" => plot.Axes.Left,
                    "Right" => plot.Axes.Right,
                    "Bottom" => plot.Axes.Bottom,
                    "Top" => plot.Axes.Top,
                    _ => null
                };

                if (targetAxis == null) return;

                // 使用自定义坐标轴以支持子标题
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

                var viewLimits = plot.Axes.GetLimits();

                // --- 应用样式 ---
                targetAxis.IsVisible = IsVisible; // 使用 ViewModel 自身的 IsVisible

                // 标题样式
                targetAxis.Label.Text = cartesianAxisDef.Label.Get();
                targetAxis.Label.FontName = cartesianAxisDef.Family;
                targetAxis.Label.FontSize = cartesianAxisDef.Size;
                targetAxis.Label.ForeColor = ScottPlot.Color.FromHex(
                    GraphMapTemplateService.ConvertWpfHexToScottPlotHex(cartesianAxisDef.Color));
                targetAxis.Label.Bold = cartesianAxisDef.IsBold;
                targetAxis.Label.Italic = cartesianAxisDef.IsItalic;

                // 对数刻度与常规刻度处理
                if (cartesianAxisDef.ScaleType == AxisScaleType.Logarithmic)
                {
                    var tickGen = new ScottPlot.TickGenerators.NumericAutomatic();
                    var hideMinor = cartesianAxisDef.MinorTickWidth <= 0 || cartesianAxisDef.MinorTickLength <= 0;
                    tickGen.MinorTickGenerator = hideMinor
                        ? new ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator(0)
                        : new ScottPlot.TickGenerators.LogMinorTickGenerator();
                    tickGen.IntegerTicksOnly = true;
                    tickGen.LabelFormatter = y => 
                    {
                        double val = Math.Pow(10, y);
                        // 使用 G10 格式化并移除可能出现的微小误差后缀
                        return val.ToString("G10"); 
                    };
                    targetAxis.TickGenerator = tickGen;
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
                        // 自动刻度生成器下，ScottPlot 的 EvenlySpacedMinorTickGenerator 参数是“分段数”而不是“刻度数”
                        // 这里需要 +1
                        int divisions = minorCount > 0 ? minorCount + 1 : 0;
                        tickGen.MinorTickGenerator = new ScottPlot.TickGenerators.EvenlySpacedMinorTickGenerator(divisions);
                        targetAxis.TickGenerator = tickGen;
                    }
                }

                // 主刻度样式
                targetAxis.MajorTickStyle.Length = cartesianAxisDef.MajorTickLength;
                targetAxis.MajorTickStyle.Width = cartesianAxisDef.MajorTickWidth;
                targetAxis.MajorTickStyle.Color = ScottPlot.Color.FromHex(
                    GraphMapTemplateService.ConvertWpfHexToScottPlotHex(cartesianAxisDef.MajorTickWidthColor));
                targetAxis.MajorTickStyle.AntiAlias = true;

                // 次刻度样式
                targetAxis.MinorTickStyle.Length = cartesianAxisDef.MinorTickLength;
                targetAxis.MinorTickStyle.Width = cartesianAxisDef.MinorTickWidth;
                targetAxis.MinorTickStyle.Color = ScottPlot.Color.FromHex(
                    GraphMapTemplateService.ConvertWpfHexToScottPlotHex(cartesianAxisDef.MinorTickColor));
                targetAxis.MinorTickStyle.AntiAlias = true;

                // 刻度标签样式
                targetAxis.TickLabelStyle.FontName = cartesianAxisDef.TickLableFamily;
                targetAxis.TickLabelStyle.FontSize = cartesianAxisDef.TickLablesize;
                targetAxis.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex(
                    GraphMapTemplateService.ConvertWpfHexToScottPlotHex(cartesianAxisDef.TickLablecolor));
                targetAxis.TickLabelStyle.Bold = cartesianAxisDef.TickLableisBold;
                targetAxis.TickLabelStyle.Italic = cartesianAxisDef.TickLableisItalic;

                // 子标题样式
                if (targetAxis is LeftAxisWithSubtitle leftSub)
                {
                    ConfigureSubtitle(leftSub.SubLabelStyle, cartesianAxisDef);
                    leftSub.SubLabelText = cartesianAxisDef.SubLabel.Get()?.Replace("\r\n", "\n").Replace("\r", "\n") ?? "";
                }
                else if (targetAxis is RightAxisWithSubtitle rightSub)
                {
                    ConfigureSubtitle(rightSub.SubLabelStyle, cartesianAxisDef);
                    rightSub.SubLabelText = cartesianAxisDef.SubLabel.Get()?.Replace("\r\n", "\n").Replace("\r", "\n") ?? "";
                }
                else if (targetAxis is BottomAxisWithSubtitle bottomSub)
                {
                    ConfigureSubtitle(bottomSub.SubLabelStyle, cartesianAxisDef);
                    bottomSub.SubLabelText = cartesianAxisDef.SubLabel.Get()?.Replace("\r\n", "\n").Replace("\r", "\n") ?? "";
                }
                else if (targetAxis is TopAxisWithSubtitle topSub)
                {
                    ConfigureSubtitle(topSub.SubLabelStyle, cartesianAxisDef);
                    topSub.SubLabelText = cartesianAxisDef.SubLabel.Get()?.Replace("\r\n", "\n").Replace("\r", "\n") ?? "";
                }

                // 应用轴范围限制 (最后执行以确保覆盖 TickGenerator 可能带来的副作用)
                UpdateAxisLimits(plot, cartesianAxisDef, targetAxis);
            }

            // 3. 处理三元坐标轴 (Ternary)
            else if (AxisDefinition is TernaryAxisDefinition ternaryAxisDef)
            {
                // 获取图表中已存在的 TriangularAxis 对象
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

                    // 标题样式
                    targetEdge.LabelText = edgeVisible
                        ? ternaryAxisDef.Label.Get()?.Replace("\r\n", "\n").Replace("\r", "\n") ?? ""
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

                    // 刻度样式
                    targetEdge.TickMarkStyle.Width = edgeVisible && ternaryAxisDef.IsShowMajorTicks ? 1 : 0;
                    targetEdge.TickMarkStyle.Color = edgeVisible && ternaryAxisDef.IsShowMajorTicks
                        ? ScottPlot.Color.FromHex(GraphMapTemplateService.ConvertWpfHexToScottPlotHex(ternaryAxisDef.MajorTickWidthColor))
                        : ScottPlot.Color.FromHex("#00000000");

                    // 刻度标签样式
                    targetEdge.TickLabelStyle.FontName = ternaryAxisDef.TickLableFamily;
                    targetEdge.TickLabelStyle.FontSize = edgeVisible && ternaryAxisDef.IsShowTickLabels ? ternaryAxisDef.TickLablesize : 0;
                    targetEdge.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex(
                        edgeVisible
                            ? GraphMapTemplateService.ConvertWpfHexToScottPlotHex(ternaryAxisDef.TickLablecolor)
                            : "#00000000");
                    targetEdge.TickLabelStyle.Bold = ternaryAxisDef.TickLableisBold;
                    targetEdge.TickLabelStyle.Italic = ternaryAxisDef.TickLableisItalic;

                    // 修改刻度标签为 0-1
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
            }
            else if (axis is ScottPlot.IYAxis yAxis && axis.Edge == ScottPlot.Edge.Left)
            {
                plot.Axes.DefaultGrid.YAxis = yAxis;
            }
        }

        /// <summary>
        /// 私有辅助方法：更新坐标轴范围
        /// </summary>
        private void UpdateAxisLimits(Plot plot, CartesianAxisDefinition axisDef, IAxis targetAxis)
        {
            var currentLimits = targetAxis.Range;
            var newMin = !double.IsNaN(axisDef.Minimum) ? axisDef.Minimum : currentLimits.Min;
            var newMax = !double.IsNaN(axisDef.Maximum) ? axisDef.Maximum : currentLimits.Max;

            // 检查用户是否明确设定了范围
            bool isMinSet = !double.IsNaN(axisDef.Minimum);
            bool isMaxSet = !double.IsNaN(axisDef.Maximum);

            // 对数处理
            if (axisDef.ScaleType == AxisScaleType.Logarithmic)
            {
                if (isMinSet) newMin = axisDef.Minimum > 0 ? Math.Log10(axisDef.Minimum) : currentLimits.Min;
                if (isMaxSet) newMax = axisDef.Maximum > 0 ? Math.Log10(axisDef.Maximum) : currentLimits.Max;
            }

            // 如果设定了范围，添加小的边距以确保边界标签完整显示
            // padding比例为范围的2.5%，确保边界标签不被裁切
            if (isMinSet && isMaxSet)
            {
                double rangeSpan = Math.Abs(newMax - newMin);
                if (rangeSpan > 0)
                {
                    double padding = rangeSpan * 0.025; // 2.5%的padding
                    
                    // 判断是否为倒置范围（newMin > newMax）
                    if (newMin > newMax)
                    {
                        // 倒置范围：Min增加padding，Max减少padding
                        newMin += padding;
                        newMax -= padding;
                    }
                    else
                    {
                        // 正常范围：Min减少padding，Max增加padding
                        newMin -= padding;
                        newMax += padding;
                    }
                }
            }

            // 支持倒置范围
            targetAxis.Range.Min = newMin;
            targetAxis.Range.Max = newMax;
        }

        public void Highlight() { /* 坐标轴不参与高亮 */ }
        public void Dim() { /* 坐标轴不参与遮罩 */ }
        public void Restore() { }

        private void ApplyAxisLabelStyle(ScottPlot.IAxis targetAxis, BaseAxisDefinition axisDef)
        {
            string labelText = axisDef.Label.Get();
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
            string subtitleText = axisDef.SubLabel.Get()?.Replace("\r\n", "\n").Replace("\r", "\n") ?? "";

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
