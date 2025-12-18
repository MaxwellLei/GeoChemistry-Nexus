using ScottPlot;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoChemistryNexus.Helpers
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

            // 确保 start 包含在 range 内或者稍微在左边（用于 minor ticks），但主要刻度应该在视图内
            // ScottPlot 的 Range 通常是 View Range。
            
            // 为了安全起见，从稍微小于 Min 的位置开始尝试，直到大于 Max
            // 或者直接计算第一个大于等于 Min 的刻度
            
            // 第一个主刻度
            double firstMajor = Math.Ceiling(range.Min / Interval) * Interval;
            // 如果正好在边界上，可能因为浮点误差需要微调，但通常 Ceiling 是安全的。
            // 例如 Min=0, Interval=1 => firstMajor=0.
            
            // 生成主刻度
            for (double pos = firstMajor; pos <= range.Max + 1e-10; pos += Interval)
            {
                ticks.Add(new Tick(pos, pos.ToString(), isMajor: true));
            }

            // 生成次刻度
            if (MinorTickCount > 0)
            {
                double minorStep = Interval / (MinorTickCount + 1);
                
                // 我们需要覆盖整个范围，包括第一个主刻度之前和最后一个主刻度之后的部分
                double rangeStart = Math.Floor(range.Min / Interval) * Interval;
                double rangeEnd = Math.Ceiling(range.Max / Interval) * Interval;

                for (double majorPos = rangeStart; majorPos < rangeEnd + 1e-10; majorPos += Interval)
                {
                    for (int i = 1; i <= MinorTickCount; i++)
                    {
                        double minorPos = majorPos + i * minorStep;
                        if (minorPos >= range.Min - 1e-10 && minorPos <= range.Max + 1e-10)
                        {
                            // 避免和主刻度重叠（理论上不会，因为是等分的）
                             ticks.Add(new Tick(minorPos, "", isMajor: false));
                        }
                    }
                }
            }
            
            Ticks = ticks.ToArray();
        }
    }
}
