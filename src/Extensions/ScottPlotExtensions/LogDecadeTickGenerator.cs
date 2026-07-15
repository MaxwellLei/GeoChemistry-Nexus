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

            if (ShowMinorTicks && ticks.Count >= 1)
            {
                double[] majorPositions = ticks.Select(t => t.Position).ToArray();
                var minorGen = new LogMinorTickGenerator();
                foreach (double minorPos in minorGen.GetMinorTicks(majorPositions, range))
                {
                    if (!range.Contains(minorPos))
                        continue;
                    if (majorPositions.Any(m => Math.Abs(m - minorPos) <= epsilon))
                        continue;

                    ticks.Add(new Tick(minorPos, "", isMajor: false));
                }
            }

            Ticks = ticks.OrderBy(t => t.Position).ToArray();
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
