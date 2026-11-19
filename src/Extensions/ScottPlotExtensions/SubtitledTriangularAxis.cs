using ScottPlot;
using SkiaSharp;
using System;
using System.Linq;

namespace GeoChemistryNexus.Extensions.ScottPlotExtensions
{
    /// <summary>
    /// 在 ScottPlot 三角轴上补一条副标题。各边刻度数量可以不同，网格只在两边刻度数一致时绘制。
    /// </summary>
    public class SubtitledTriangularAxis : ScottPlot.Plottables.TriangularAxis
    {
        public string BottomSubText { get; set; } = string.Empty;
        public string LeftSubText { get; set; } = string.Empty;
        public string RightSubText { get; set; } = string.Empty;

        public LabelStyle BottomSubStyle { get; } = new() { FontSize = 14 };
        public LabelStyle LeftSubStyle { get; } = new() { FontSize = 14 };
        public LabelStyle RightSubStyle { get; } = new() { FontSize = 14 };

        public float BottomSubExtraX { get; set; }
        public float BottomSubExtraY { get; set; }
        public float LeftSubExtraX { get; set; }
        public float LeftSubExtraY { get; set; }
        public float RightSubExtraX { get; set; }
        public float RightSubExtraY { get; set; }

        public SubtitledTriangularAxis(bool clockwise) : base(clockwise)
        {
        }

        public LabelStyle? SubStyleFor(string axisType)
        {
            return axisType switch
            {
                "Bottom" => BottomSubStyle,
                "Left" => LeftSubStyle,
                "Right" => RightSubStyle,
                _ => null
            };
        }

        public override void Render(RenderPack rp)
        {
            using SKPaint paint = new();

            RenderBackground(rp, paint);
            RenderEdge(rp, paint, Bottom, BottomSubText, BottomSubStyle, BottomSubExtraX, BottomSubExtraY);
            RenderEdge(rp, paint, Left, LeftSubText, LeftSubStyle, LeftSubExtraX, LeftSubExtraY);
            RenderEdge(rp, paint, Right, RightSubText, RightSubStyle, RightSubExtraX, RightSubExtraY);
            RenderGridLines(rp, paint, Bottom, Right);
            RenderGridLines(rp, paint, Bottom, Left);
            RenderGridLines(rp, paint, Left, Right);
        }

        private void RenderBackground(RenderPack rp, SKPaint paint)
        {
            Coordinates[] corners = [Left.Start, Right.Start, Bottom.Start];
            var cornerPixels = corners.Select(Axes.GetPixel);
            Drawing.FillPath(rp.Canvas, paint, cornerPixels, FillStyle, rp.DataRect);
        }

        private void RenderEdge(RenderPack rp, SKPaint paint, TriangularAxisEdge edge, string subText, LabelStyle subStyle, float extraX, float extraY)
        {
            Drawing.DrawLine(rp.Canvas, paint, Axes.GetPixelLine(edge.Line), edge.EdgeLineStyle);

            foreach (var tick in edge.Ticks)
            {
                Pixel px1 = Axes.GetPixel(tick.Point);
                Pixel px2 = px1.WithOffset(edge.TickOffset);
                PixelLine tickLine = new(px1, px2);
                edge.TickMarkStyle.Render(rp.Canvas, paint, tickLine);
                edge.TickLabelStyle.Render(rp.Canvas, px2, paint, tick.Label);
            }

            Pixel center = Axes.GetPixel(edge.Line.Center);
            edge.LabelStyle.Render(rp.Canvas, center, paint, edge.LabelText);
            RenderSubtitle(rp, paint, edge, center, subText, subStyle, extraX, extraY);
        }

        private static void RenderSubtitle(RenderPack rp, SKPaint paint, TriangularAxisEdge edge, Pixel center, string text, LabelStyle style, float extraX, float extraY)
        {
            if (string.IsNullOrWhiteSpace(text) || style.FontSize <= 0)
                return;

            float ox = edge.LabelStyle.OffsetX;
            float oy = edge.LabelStyle.OffsetY;
            float len = MathF.Sqrt(ox * ox + oy * oy);
            float ux = len > 0.01f ? ox / len : 0f;
            float uy = len > 0.01f ? oy / len : 1f;
            float gap = style.FontSize + 4f;

            style.Rotation = edge.LabelStyle.Rotation;
            style.Alignment = edge.LabelStyle.Alignment;
            style.OffsetX = ox + ux * gap + extraX;
            style.OffsetY = oy + uy * gap + extraY;
            style.Render(rp.Canvas, center, paint, text);
        }

        private void RenderGridLines(RenderPack rp, SKPaint paint, TriangularAxisEdge edge1, TriangularAxisEdge edge2, bool reverse = true)
        {
            if (edge1.Ticks.Count == 0 || edge1.Ticks.Count != edge2.Ticks.Count)
                return;

            for (int i = 0; i < edge1.Ticks.Count; i++)
            {
                int i2 = reverse ? edge1.Ticks.Count - 1 - i : i;
                CoordinateLine tickLine = new(edge1.Ticks[i].Point, edge2.Ticks[i2].Point);
                PixelLine pxLine = Axes.GetPixelLine(tickLine);
                Drawing.DrawLine(rp.Canvas, paint, pxLine, GridLineStyle);
            }
        }
    }
}
