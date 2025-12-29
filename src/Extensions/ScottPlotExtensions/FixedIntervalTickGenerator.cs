using ScottPlot;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoChemistryNexus.Extensions.ScottPlotExtensions
{
    public class FixedIntervalTickGenerator : ITickGenerator
    {
        public double Interval { get; }
        public int MinorTickCount { get; }

        public FixedIntervalTickGenerator(double interval, int minorTickCount = 0)
        {
            if (interval <= 0)
                throw new ArgumentException("Interval must be positive", nameof(interval));

            Interval = interval;
            MinorTickCount = Math.Max(0, minorTickCount);
        }

        public Tick[] Ticks { get; set; } = Array.Empty<Tick>();

        public int MaxTickCount { get; set; } = 10000;

        public void Regenerate(CoordinateRange range, Edge edge, PixelLength size, SKPaint paint, LabelStyle labelStyle)
        {
            List<Tick> ticks = new();

            // 避免生成过多刻度导致性能问题或死循环
            if (range.Span / Interval > MaxTickCount)
            {
                Ticks = Array.Empty<Tick>();
                return;
            }

            double epsilon = 1e-10;

            // 使用整数索引循环以避免浮点累积误差
            long firstIndex = (long)Math.Ceiling((range.Min - epsilon) / Interval);
            long lastIndex = (long)Math.Floor((range.Max + epsilon) / Interval);

            // 生成主刻度
            for (long i = firstIndex; i <= lastIndex; i++)
            {
                double pos = i * Interval;
                
                // 格式化标签：对于常规间隔，舍入到10位小数以去除浮点噪声
                // 如果间隔非常小（小于1e-7），则保留原始精度或使用更多位
                string label;
                if (Interval < 1e-7)
                {
                     label = pos.ToString();
                }
                else
                {
                     label = Math.Round(pos, 10).ToString();
                }

                ticks.Add(new Tick(pos, label, isMajor: true));
            }

            // 生成次刻度
            if (MinorTickCount > 0)
            {
                double minorInterval = Interval / (MinorTickCount + 1);
                
                // 计算次刻度的索引范围
                long subFirstIndex = (long)Math.Ceiling((range.Min - epsilon) / minorInterval);
                long subLastIndex = (long)Math.Floor((range.Max + epsilon) / minorInterval);

                for (long j = subFirstIndex; j <= subLastIndex; j++)
                {
                    // 跳过与主刻度重叠的次刻度
                    if (j % (MinorTickCount + 1) == 0)
                    {
                        continue;
                    }

                    double pos = j * minorInterval;
                    ticks.Add(new Tick(pos, "", isMajor: false));
                }
            }
            
            Ticks = ticks.ToArray();
        }
    }
}
