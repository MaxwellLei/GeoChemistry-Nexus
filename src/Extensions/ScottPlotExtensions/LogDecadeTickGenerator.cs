using ScottPlot;
using ScottPlot.TickGenerators;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoChemistryNexus.Extensions.ScottPlotExtensions
{
    /// <summary>
    /// 对数轴主刻度生成器：在 log10 坐标上按整数 decade 生成刻度，
    /// 并保证范围两端落在 decade 上的标签不会因浮点误差被丢掉。
    /// </summary>
    public class LogDecadeTickGenerator : ITickGenerator
    {
        public bool ShowMinorTicks { get; set; } = true;

        /// <summary>
        /// 是否始终在坐标轴两端显示带标签的刻度（适用于用户/模板显式指定范围的轴）。
        /// 自动缩放的轴建议保持 false，否则端点会出现不规整的数值。
        /// </summary>
        public bool ShowRangeEndpoints { get; set; } = false;

        /// <summary>
        /// 需要强制带标签显示的端点位置（log10 坐标）。
        /// 为 null 时使用当前轴范围两端（严格对齐场景）；
        /// 非 null 时仅使用这些位置（自动模式下范围两端含边距，端点应取用户设定的真实边界值）。
        /// 不在当前轴范围内的位置会被忽略。
        /// </summary>
        public double[]? EndpointPositions { get; set; }

        public Tick[] Ticks { get; set; } = Array.Empty<Tick>();

        public int MaxTickCount { get; set; } = 10000;

        public void Regenerate(CoordinateRange range, Edge edge, PixelLength size, SKPaint paint, LabelStyle labelStyle)
        {
            // log10 坐标上的整数步长；避免生成过多 decade
            if (range.Length > MaxTickCount)
            {
                Ticks = Array.Empty<Tick>();
                return;
            }

            // 相对宽松的边界容差，覆盖 Log10 往返与 UI 绑定带来的浮点误差
            const double epsilon = 1e-6;

            long firstIndex = (long)Math.Ceiling(range.Min - epsilon);
            long lastIndex = (long)Math.Floor(range.Max + epsilon);

            // 端点非常接近整数 decade 时强制纳入（例如 Max≈0.9999999 对应 10）
            if (IsNearInteger(range.Min, epsilon))
                firstIndex = Math.Min(firstIndex, (long)Math.Round(range.Min));
            if (IsNearInteger(range.Max, epsilon))
                lastIndex = Math.Max(lastIndex, (long)Math.Round(range.Max));

            if (lastIndex < firstIndex)
            {
                Ticks = Array.Empty<Tick>();
                return;
            }

            List<Tick> ticks = new();
            for (long i = firstIndex; i <= lastIndex; i++)
            {
                double pos = i;
                ticks.Add(new Tick(pos, FormatDecadeLabel(pos), isMajor: true));
            }

            // 范围内整数 decade 太少（<3）时，仅显示 decade 标签会让读者看不出范围边界，
            // 例如 2-100 只有 10、100。此时给 2/5（或 2-9）等中间刻度也加上标签。
            bool labelIntermediate = ticks.Count < 3;
            int[] labeledMantissas = range.Length <= 0.7
                ? new[] { 2, 3, 4, 5, 6, 7, 8, 9 }
                : new[] { 2, 5 };

            if (ShowMinorTicks || labelIntermediate)
            {
                long startDecade = (long)Math.Floor(range.Min) - 1;
                long endDecade = (long)Math.Ceiling(range.Max);

                for (long decade = startDecade; decade <= endDecade; decade++)
                {
                    double decadeValue = Math.Pow(10, decade);
                    for (int m = 2; m <= 9; m++)
                    {
                        double minorPos = decade + Math.Log10(m);
                        if (!range.Contains(minorPos))
                            continue;

                        if (labelIntermediate && labeledMantissas.Contains(m))
                        {
                            // 作为带标签刻度添加（ScottPlot 只对 IsMajor 的刻度绘制文字）
                            ticks.Add(new Tick(minorPos, (m * decadeValue).ToString("G10"), isMajor: true));
                        }
                        else if (ShowMinorTicks)
                        {
                            ticks.Add(new Tick(minorPos, "", isMajor: false));
                        }
                    }
                }
            }

            if (ShowRangeEndpoints)
                AddRangeEndpointTicks(ticks, range, edge, size, epsilon, EndpointPositions);

            Ticks = ticks.OrderBy(t => t.Position).ToArray();
        }

        /// <summary>
        /// 保证坐标轴两端始终有带标签的刻度；
        /// 若附近已有的标签与端点标签挤在一起，则保留端点、丢弃邻近标签。
        /// </summary>
        private static void AddRangeEndpointTicks(List<Tick> ticks, CoordinateRange range, Edge edge, PixelLength size, double epsilon, double[]? endpointPositions)
        {
            if (range.Length <= 0 || double.IsNaN(range.Length) || double.IsInfinity(range.Length))
                return;

            bool isHorizontal = edge == Edge.Bottom || edge == Edge.Top;
            double minLabelGapPx = isHorizontal ? 45 : 20;
            double pxPerLogUnit = size.Length > 0 ? size.Length / range.Length : 0;
            double minGapLog = pxPerLogUnit > 0 ? minLabelGapPx / pxPerLogUnit : 0;

            foreach (double endPos in endpointPositions ?? new[] { range.Min, range.Max })
            {
                if (double.IsNaN(endPos) || double.IsInfinity(endPos))
                    continue;

                // 端点落在当前轴范围之外则忽略
                if (endPos < range.Min - epsilon || endPos > range.Max + epsilon)
                    continue;

                // 端点已经是带标签刻度（如恰好落在 10^n 上），无需重复添加
                bool alreadyLabeled = ticks.Any(t => t.IsMajor && Math.Abs(t.Position - endPos) <= epsilon);
                if (alreadyLabeled)
                    continue;

                // 移除与端点标签过近的其他带标签刻度（例如范围 9.5-100 时的 10）
                ticks.RemoveAll(t => t.IsMajor && Math.Abs(t.Position - endPos) < minGapLog);

                // 移除与端点重合的次刻度，避免重复绘制
                ticks.RemoveAll(t => !t.IsMajor && Math.Abs(t.Position - endPos) <= epsilon);

                ticks.Add(new Tick(endPos, Math.Pow(10, endPos).ToString("G6"), isMajor: true));
            }
        }

        private static bool IsNearInteger(double value, double epsilon)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return false;

            return Math.Abs(value - Math.Round(value)) <= epsilon;
        }

        private static string FormatDecadeLabel(double logPosition)
        {
            double val = Math.Pow(10, logPosition);
            return val.ToString("G10");
        }
    }
}
